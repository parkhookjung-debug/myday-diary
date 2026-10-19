import SwiftUI
import AppKit

@MainActor
final class LoungeModel: ObservableObject {
    @Published var address = "http://127.0.0.1:8765/"
    @Published var nickname = ""
    @Published var invitation = ""
    @Published var composer = ""
    @Published var messages: [LoungeMessage] = []
    @Published var users: [String] = []
    @Published var status = "서버 주소·닉네임·초대코드로 입장하세요."
    @Published var connected = false
    @Published var busy = false
    @Published var sending = false
    private var active: LoungeClient?
    private var connection: Task<Void, Never>?
    private var generation = UUID()
    private var pendingText = "", pendingID = ""

    func connect() {
        guard connection == nil else { return }
        do {
            let client = try LoungeClient(address: address.trimmingCharacters(in: .whitespacesAndNewlines))
            let id = UUID(); generation = id
            active = client; busy = true; status = "채팅방에 입장하고 있어요…"
            connection = Task { await receive(client, id: id) }
        } catch { status = error.localizedDescription }
    }
    private func append(_ page: LoungePage) {
        if page.reset { messages.removeAll() }
        let last = messages.last?.id ?? 0
        messages.append(contentsOf: page.messages.filter { $0.id > last })
        if messages.count > DesktopContract.chatVisibleHistory { messages.removeFirst(messages.count - DesktopContract.chatVisibleHistory) }
        users = page.users
    }
    private func receive(_ client: LoungeClient, id: UUID) async {
        do {
            let joined = try await client.join(nickname: nickname, code: invitation)
            try Task.checkCancellation()
            nickname = joined.nickname; connected = true; busy = false
            messages.removeAll(); users.removeAll(); pendingID = ""; pendingText = ""
            status = "\(joined.room)에 들어왔어요. ⌘ + Enter로 전송해요."
            var cursor: Int64 = 0, failures = 0
            var first = true
            while !Task.isCancelled {
                do {
                    let page = try await client.poll(after: cursor, wait: first ? 0 : 20)
                    try Task.checkCancellation()
                    append(page); cursor = page.cursor; first = false
                    if failures > 0 { status = "서버에 다시 연결됐어요." }
                    failures = 0
                    if let delay = page.pollAfterMs, delay > 0 {
                        try await Task.sleep(nanoseconds: UInt64(delay) * 1_000_000)
                    }
                } catch {
                    try Task.checkCancellation()
                    if let failure = error as? LoungeFailure, [401, 403, 503].contains(failure.status) { throw error }
                    failures += 1
                    if failures >= 3 { throw error }
                    status = "연결을 다시 확인하고 있어요…"
                    try await Task.sleep(nanoseconds: 5_000_000_000)
                }
            }
        } catch {
            if !Task.isCancelled { status = error.localizedDescription }
        }
        // The old generation always leaves its own session before reconnect is enabled.
        await client.leave()
        if generation == id {
            connected = false; busy = false; sending = false
            active = nil; connection = nil; users.removeAll()
        }
    }
    func disconnect() async {
        busy = true
        connection?.cancel()
        await connection?.value
        connected = false; busy = false
        status = "채팅방에서 나왔어요."
    }
    func send() async {
        guard let client = active, connected, !sending, !composer.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty,
              composer.utf16.count <= DesktopContract.chatMaxText else { return }
        let text = composer, id = generation
        if text != pendingText || pendingID.isEmpty { pendingText = text; pendingID = UUID().uuidString.replacingOccurrences(of: "-", with: "").lowercased() }
        sending = true
        do {
            let message = try await client.send(text: text, id: pendingID)
            if generation == id && connected {
                // Polling preserves server order; do not advance the receive cursor on send.
                status = "메시지를 보냈어요."
                if composer == text { composer = ""; pendingText = ""; pendingID = "" }
                _ = message
            }
        } catch { if generation == id { status = error.localizedDescription } }
        if generation == id { sending = false }
    }
}

@MainActor
struct LoungeView: View {
    @ObservedObject var model: LoungeModel
    private let mint = DesktopPalette.chatMine
    var body: some View {
        VStack(alignment: .leading, spacing: 18) {
            HStack {
                Image(systemName: "bubble.left.and.bubble.right.fill").font(.title).foregroundStyle(DesktopPalette.olive)
                VStack(alignment: .leading) {
                    Text("함께 쓰는 작은 이야기").font(.title2.bold())
                    Text("상몬 라운지 · Mac과 Windows 친구들이 만나는 곳").font(.caption).foregroundStyle(.secondary)
                }
                Spacer()
            }
            HStack(alignment: .bottom) {
                field("서버 주소") { TextField("https://…workers.dev/", text: $model.address) }
                field("닉네임") { TextField("2~20자", text: $model.nickname) }.frame(width: 140)
                field("초대코드") { SecureField("초대받은 코드", text: $model.invitation) }.frame(width: 180)
                if model.connected || model.busy {
                    Button("나가기") { Task { await model.disconnect() } }.disabled(model.busy && !model.connected)
                } else {
                    Button("입장하기") { model.connect() }.buttonStyle(.borderedProminent).tint(DesktopPalette.olive)
                }
            }
            HStack(alignment: .top, spacing: 16) {
                ScrollViewReader { reader in
                    ScrollView {
                        LazyVStack(alignment: .leading, spacing: 12) {
                            if model.messages.isEmpty {
                                Text(model.connected ? "오늘의 첫 이야기를 남겨 보세요." : "입장하면 친구들의 이야기가 보여요.")
                                    .foregroundStyle(.secondary).frame(maxWidth: .infinity).padding(.top, 60)
                            }
                            ForEach(model.messages) { message in
                                let mine = message.nickname == model.nickname
                                HStack {
                                    if mine { Spacer(minLength: 45) }
                                    VStack(alignment: .leading, spacing: 5) {
                                        Text(mine ? "나" : message.nickname).font(.caption).foregroundStyle(.secondary)
                                        Text(message.text).textSelection(.enabled).fixedSize(horizontal: false, vertical: true)
                                    }.padding(12).background(mine ? mint : DesktopPalette.chatOther, in: RoundedRectangle(cornerRadius: 16))
                                    if !mine { Spacer(minLength: 45) }
                                }.id(message.id)
                            }
                        }.padding(14)
                    }.background(Color.white.opacity(0.4), in: RoundedRectangle(cornerRadius: 18))
                        .onChange(of: model.messages.last?.id) { value in if let value { reader.scrollTo(value, anchor: .bottom) } }
                }
                VStack(alignment: .leading, spacing: 10) {
                    Text("함께 있는 사람").font(.headline)
                    ForEach(model.users, id: \.self) { name in Text(name + (name == model.nickname ? " (나)" : "")) }
                }.frame(width: 130, alignment: .leading)
            }
            HStack(alignment: .bottom) {
                TextEditor(text: $model.composer).font(.body).frame(height: 72).padding(6)
                    .background(.white, in: RoundedRectangle(cornerRadius: 12)).disabled(!model.connected)
                Button(model.sending ? "전송 중…" : "보내기") { Task { await model.send() } }
                    .buttonStyle(.borderedProminent).tint(DesktopPalette.olive).keyboardShortcut(.return, modifiers: .command)
                    .disabled(!model.connected || model.sending || model.composer.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty || model.composer.utf16.count > DesktopContract.chatMaxText)
            }
            HStack {
                Text(model.status).foregroundStyle(.secondary)
                Spacer()
                Text("\(model.composer.utf16.count) / \(DesktopContract.chatMaxText)").foregroundStyle(model.composer.utf16.count > DesktopContract.chatMaxText ? .red : .secondary)
            }.font(.caption)
        }.padding(24).frame(minWidth: 760, minHeight: 580)
            .background(DesktopPalette.backgrounds[0]).preferredColorScheme(.light)
            .onDisappear { Task { await model.disconnect() } }
    }
    private func field<V: View>(_ label: String, @ViewBuilder input: () -> V) -> some View {
        VStack(alignment: .leading, spacing: 5) { Text(label).font(.caption); input().textFieldStyle(.roundedBorder).disabled(model.connected || model.busy) }
    }
}

@MainActor
final class LoungeWindow: NSObject, NSWindowDelegate {
    static let shared = LoungeWindow()
    private var window: NSWindow?
    private var model: LoungeModel?
    func show() {
        if let window { window.makeKeyAndOrderFront(nil); return }
        let window = NSWindow(contentRect: NSRect(x: 0, y: 0, width: 920, height: 720),
            styleMask: [.titled, .closable, .miniaturizable, .resizable], backing: .buffered, defer: false)
        window.title = "MyDay · 상몬 라운지"
        let model = LoungeModel()
        self.model = model
        window.contentView = NSHostingView(rootView: LoungeView(model: model))
        window.minSize = NSSize(width: 800, height: 640)
        window.isReleasedWhenClosed = false; window.delegate = self
        self.window = window; window.center(); window.makeKeyAndOrderFront(nil)
        NSApp.activate(ignoringOtherApps: true)
    }
    func windowWillClose(_ notification: Notification) {
        if let model { Task { await model.disconnect() } }
        model = nil; window = nil
    }
}
