import SwiftUI
import AppKit

enum BlockKind: String, Codable, CaseIterable {
    case text, todo, habit, emotion, photo
    case photoSlot = "photo-slot"
    var title: String {
        switch self {
        case .text: return "✍️ 글 일기"
        case .todo: return "☑️ 할 일"
        case .habit: return "🌱 습관 체크"
        case .emotion: return "💬 감정처리반"
        case .photo, .photoSlot: return "📷 사진"
        }
    }
}

@MainActor
final class DiaryStore: ObservableObject {
    @Published var date = Calendar.current.startOfDay(for: Date())
    @Published var entry = DiaryEntry()
    @Published var error: String?
    @Published var loaded = false
    @Published var companion = CompanionProfile()
    @Published var profileLoaded = false
    @Published var showingSearch = false
    @Published var showingCompanion = false
    @Published var showingTemplates = false
    @Published var windowsMetadata: WindowsMetadata?
    var metadataLoaded = false
    var pendingActivity = false
    let directory: URL

    init(directory customDirectory: URL? = nil) {
        directory = customDirectory ?? FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0]
            .appendingPathComponent("MyDay", isDirectory: true)
        load()
        loadCompanion()
        loadWindowsMetadata()
    }

    private func file(for date: Date) -> URL {
        let parts = Calendar(identifier: .gregorian).dateComponents([.year, .month, .day], from: date)
        return directory.appendingPathComponent(String(format: "%04d-%02d-%02d.json", parts.year!, parts.month!, parts.day!))
    }

    func load() {
        do {
            let url = file(for: date)
            entry = FileManager.default.fileExists(atPath: url.path)
                ? try JSONDecoder().decode(DiaryEntry.self, from: Data(contentsOf: url)) : DiaryEntry()
            try MacBackup(days: ["2000-01-01": entry], companion: CompanionProfile()).validate()
            loaded = true
            error = nil
        } catch {
            loaded = false
            self.error = "기록을 읽지 못했습니다. 기존 파일을 확인한 뒤 다시 불러와 주세요. \(error.localizedDescription)"
        }
    }

    func save() {
        guard loaded else { return }
        guard profileLoaded, metadataLoaded else {
            error = "상몬 정보를 읽지 못했습니다. 다시 불러온 뒤 저장해 주세요."
            return
        }
        do {
            try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
            let encoder = JSONEncoder()
            encoder.outputFormatting = [.prettyPrinted, .sortedKeys]
            try MacBackup(days: ["2000-01-01": entry], companion: companion, windowsMetadata: windowsMetadata).validate()
            try encoder.encode(entry).write(to: file(for: date), options: .atomic)
            error = nil
            rewardActivity()
        } catch {
            self.error = "저장하지 못했습니다. 다시 저장해 주세요. \(error.localizedDescription)"
        }
    }

    func select(_ next: Date) {
        if loaded { save() }
        guard error == nil else { return }
        date = Calendar.current.startOfDay(for: next)
        load()
    }

    func update(_ change: (inout DiaryEntry) -> Void) {
        guard loaded else { return }
        let before = entry.blocks
        change(&entry)
        pendingActivity = pendingActivity || entry.blocks.contains { block in
            let old = before.first { $0.id == block.id }
            let textChanged = old?.text != block.text && !block.text.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty
            let photoChanged = old?.photo != block.photo && block.photo != nil
            let checked = (block.kind == .todo || block.kind == .habit) && block.checked && old?.checked != true
            return textChanged || photoChanged || checked
        }
        save()
    }

    func move(_ id: UUID, by offset: Int) {
        guard let index = entry.blocks.firstIndex(where: { $0.id == id }),
              entry.blocks.indices.contains(index + offset) else { return }
        update { $0.blocks.swapAt(index, index + offset) }
    }
}

@main
struct MyDayApp: App {
    @NSApplicationDelegateAdaptor(MyDayLifecycle.self) private var lifecycle
    @StateObject private var store = DiaryStore()
    init() {
        if CommandLine.arguments.contains("--render-preview") {
            do { try MacVerification.renderPreview(); exit(0) }
            catch { fputs("Preview failed: \(error)\n", stderr); exit(1) }
        }
        if CommandLine.arguments.contains("--self-test") {
            do { try MacVerification.run(); print("MyDay macOS: all checks passed"); exit(0) }
            catch { fputs("Verification failed: \(error)\n", stderr); exit(1) }
        }
    }
    var body: some Scene {
        WindowGroup("마이데이", id: "diary") {
            DiaryView(store: store)
                .frame(minWidth: 620, minHeight: 640)
                .onAppear { lifecycle.store = store }
                .onReceive(NotificationCenter.default.publisher(for: NSApplication.willTerminateNotification)) { _ in store.save() }
        }
        .defaultSize(width: 920, height: 860)
        .commands {
            CommandGroup(after: .newItem) {
                Button("기록 검색") { store.showingSearch = true }.keyboardShortcut("f")
                Button("일기 형식 100종") { store.showingTemplates = true }
                Button("Windows 백업 내보내기…") { store.exportWindowsBackup() }
                Button("기록 백업…") { store.exportBackup() }.keyboardShortcut("e", modifiers: [.command, .shift])
                Button("맥·Windows 백업 가져오기…") { store.importBackup() }
                Button("상몬 성장과 장비") { store.showingCompanion = true }
                Button("바탕화면 상몬 보기") { DesktopCompanion.shared.show(store: store) }
            }
            CommandGroup(replacing: .saveItem) {
                Button("저장") { store.save() }.keyboardShortcut("s")
            }
        }
        MenuBarExtra("마이데이", systemImage: "leaf") {
            DiaryMenu(store: store)
        }
    }
}

struct DiaryView: View {
    @ObservedObject var store: DiaryStore
    private let backgrounds: [Color] = [Color(red: 1, green: 0.97, blue: 0.95), Color(red: 0.94, green: 0.96, blue: 0.93), Color(red: 0.95, green: 0.94, blue: 0.98)]

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 20) {
                HStack {
                    Button("달력 · 검색") { store.showingSearch = true }
                    Button("일기 형식") { store.showingTemplates = true }
                    Button("상몬 · 장비") { store.showingCompanion = true }
                    Button("바탕화면 친구") { DesktopCompanion.shared.show(store: store) }
                    Spacer()
                    Menu("기록 관리") {
                        Button("맥 백업 내보내기…") { store.exportBackup() }
                        Button("Windows 백업 내보내기…") { store.exportWindowsBackup() }
                        Button("맥·Windows 백업 가져오기…") { store.importBackup() }
                        Button("저장 폴더 열기") { NSWorkspace.shared.open(store.directory) }
                    }.fixedSize()
                }
                Text("MY DAY").font(.caption.bold()).foregroundStyle(.brown)
                Text("내 마음대로, 나의 하루").font(.largeTitle.bold())
                Text("필요한 블록을 골라 나만의 일기를 만들어 보세요.").foregroundStyle(.secondary)
                HStack {
                    Button("‹ 이전") { shift(-1) }
                    Spacer()
                    DatePicker("날짜", selection: Binding(get: { store.date }, set: { store.select($0) }), displayedComponents: .date)
                        .labelsHidden().fixedSize()
                    Button("오늘") { store.select(Date()) }
                    Spacer()
                    Button("다음 ›") { shift(1) }
                }.disabled(store.error != nil)

                if let error = store.error {
                    VStack(alignment: .leading, spacing: 8) {
                        Text(error).foregroundStyle(.red).textSelection(.enabled)
                        Button(store.loaded ? "다시 저장" : "다시 불러오기") {
                            if !store.profileLoaded { store.loadCompanion() }
                            else if !store.metadataLoaded { store.loadWindowsMetadata() }
                            else if store.loaded { store.save() } else { store.load() }
                        }
                        Button("저장 폴더 열기") { NSWorkspace.shared.open(store.directory) }
                    }.fixedSize()
                } else {
                    Text("맥에 저장됨 · 자동 저장").font(.caption).foregroundStyle(.secondary)
                }

                VStack(alignment: .leading, spacing: 14) {
                    Text("오늘의 분위기").font(.headline)
                    HStack {
                        Text("기분")
                        TextField("오늘의 기분", text: Binding(get: { store.entry.mood }, set: { value in store.update { $0.mood = value } })).textFieldStyle(.roundedBorder)
                    }
                    Picker("배경", selection: Binding(get: { store.entry.theme }, set: { value in store.update { $0.theme = value } })) {
                        Text("크림").tag(0)
                        Text("숲").tag(1)
                        Text("라벤더").tag(2)
                    }.pickerStyle(.segmented)
                    Picker("캐릭터", selection: Binding(get: { store.entry.character }, set: { value in store.update { $0.character = value } })) {
                        ForEach(["🐰", "🐱", "🐻"], id: \.self) { Text($0).tag($0) }
                    }.pickerStyle(.segmented)
                }.card()

                HStack(spacing: 20) {
                    SangmonView(profile: store.companion).frame(width: 100, height: 100)
                    VStack(alignment: .leading, spacing: 6) {
                        Text("\(store.entry.character) 상몬 Lv.\(store.companion.level) · 오늘도 네 편이야").font(.title3.bold())
                        Text("할 일 \(completed(.todo)) · 습관 \(completed(.habit))").foregroundStyle(.secondary)
                    }
                }.frame(maxWidth: .infinity, alignment: .leading).card()

                if store.loaded {
                    if store.entry.blocks.isEmpty {
                        VStack(alignment: .leading, spacing: 8) {
                            Text("아직 비어 있는 하루").font(.title2)
                            Text("아래 버튼으로 첫 블록을 추가해 보세요.")
                        }.frame(maxWidth: .infinity, alignment: .leading).card()
                    }
                    DiaryBoard(store: store)
                }
                Text("감정처리반은 감정을 적어 보관하는 공간입니다. AI 대화는 제공하지 않습니다.")
                    .font(.caption).foregroundStyle(.secondary)
            }.padding(30).frame(maxWidth: 1060)
                .frame(maxWidth: .infinity)
        }
        .sheet(isPresented: $store.showingSearch) { JournalSearch(store: store) }
        .sheet(isPresented: $store.showingCompanion) { CompanionWardrobe(store: store) }
        .sheet(isPresented: $store.showingTemplates) { TemplateGallery(store: store) }
        .background(backgrounds[store.entry.theme])
        .preferredColorScheme(.light)

    }

    private func shift(_ days: Int) {
        if let next = Calendar.current.date(byAdding: .day, value: days, to: store.date) { store.select(next) }
    }

    private func completed(_ kind: BlockKind) -> String {
        let blocks = store.entry.blocks.filter { $0.kind == kind }
        return "\(blocks.filter(\.checked).count)/\(blocks.count)"
    }


}

private extension View {
    func card() -> some View {
        padding(20).background(.white.opacity(0.85), in: RoundedRectangle(cornerRadius: 20))
    }
}
