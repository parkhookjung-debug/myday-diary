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
    @Published var showingLayouts = false
    @Published var windowsMetadata: WindowsMetadata?
    var metadataLoaded = false
    var pendingActivity = false
    var pendingBaseline: [DiaryBlock]?
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
        if pendingActivity && pendingBaseline == nil { pendingBaseline = before }
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
        if let index = CommandLine.arguments.firstIndex(of: "--desktop-contract"), CommandLine.arguments.count > index + 1 {
            do { try DesktopVerification.export(to: CommandLine.arguments[index + 1]); exit(0) }
            catch { fputs("Shared desktop verification failed: \(error)\n", stderr); exit(1) }
        }
        if let index = CommandLine.arguments.firstIndex(of: "--chat-test"), CommandLine.arguments.count > index + 2 {
            let address = CommandLine.arguments[index + 1], code = CommandLine.arguments[index + 2]
            Task { @MainActor in
                do { try await LoungeVerification.run(address: address, code: code); exit(0) }
                catch { fputs("Chat verification failed: \(error)\n", stderr); exit(1) }
            }
            dispatchMain()
        }
        if CommandLine.arguments.contains("--render-chat") {
            do { try LoungeVerification.render(); exit(0) }
            catch { fputs("Chat preview failed: \(error)\n", stderr); exit(1) }
        }
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
        .defaultSize(width: 1320, height: 860)
        .commands {
            CommandGroup(after: .newItem) {
                Button("기록 검색") { store.showingSearch = true }.keyboardShortcut("f")
                Button("일기 형식 100종") { store.showingTemplates = true }
                Button("Windows 백업 내보내기…") { store.exportWindowsBackup() }
                Button("기록 백업…") { store.exportBackup() }.keyboardShortcut("e", modifiers: [.command, .shift])
                Button("맥·Windows 백업 가져오기…") { store.importBackup() }
                Button("상몬 성장과 장비") { store.showingCompanion = true }
                Button("상몬 라운지 · 채팅") { LoungeWindow.shared.show() }
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
