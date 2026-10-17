import SwiftUI
import AppKit
import UniformTypeIdentifiers

struct CompanionProfile: Codable {
    var activityDays: Set<String> = []
    var color = 0
    var weapon = 0
    var orb = 0
    var level: Int { min(10, activityDays.count / 5 + 1) }
    var xp: Int { activityDays.count * 10 }
}

struct MacBackup: Codable {
    var format = "myday-macos"
    var version = 2
    var days: [String: DiaryEntry]
    var companion: CompanionProfile
    var windowsMetadata: WindowsMetadata?

    func validate() throws {
        guard format == "myday-macos", (1...2).contains(version), days.count <= 50_000,
              (0...7).contains(companion.color), (0...6).contains(companion.weapon),
              (0...6).contains(companion.orb), companion.activityDays.count <= 50_000,
              companion.color <= min(7, companion.level - 1) else { throw CocoaError(.fileReadCorruptFile) }
        try WindowsCompatibility.validateMetadata(windowsMetadata)
        for key in Array(days.keys) + Array(companion.activityDays) {
            guard Self.validDate(key) else { throw CocoaError(.fileReadCorruptFile) }
        }
        for day in days.values {
            guard (0...2).contains(day.theme), day.blocks.count <= 200,
                  Set(day.blocks.map(\.id)).count == day.blocks.count else { throw CocoaError(.fileReadCorruptFile) }
            guard ["cards", "free"].contains(day.layoutMode), ["plain", "paper", "dots"].contains(day.pageStyle) else { throw CocoaError(.fileReadCorruptFile) }
            for block in day.blocks {
                try WindowsCompatibility.checkBounds(block.x, block.y, block.width, block.height)
                guard block.text.utf16.count <= 100_000, (block.title?.utf16.count ?? 0) <= 80, (block.prompt?.utf16.count ?? 0) <= 300,
                      block.photo == nil || block.kind == .photo else { throw CocoaError(.fileReadCorruptFile) }
                if let photo = block.photo {
                    guard photo.count <= 10_000_000, NSImage(data: photo) != nil else { throw CocoaError(.fileReadCorruptFile) }
                }
            }
        }
    }

    static func validDate(_ value: String) -> Bool {
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.calendar = Calendar(identifier: .gregorian)
        formatter.dateFormat = "yyyy-MM-dd"
        formatter.isLenient = false
        return value.count == 10 && formatter.date(from: value).map { formatter.string(from: $0) == value } == true
    }
}

extension DiaryStore {
    func loadCompanion() {
        do {
            let url = directory.appendingPathComponent("companion.json")
            let profile = FileManager.default.fileExists(atPath: url.path)
                ? try JSONDecoder().decode(CompanionProfile.self, from: Data(contentsOf: url)) : CompanionProfile()
            try MacBackup(days: [:], companion: profile).validate()
            companion = profile
            profileLoaded = true
        } catch {
            self.error = "상몬 정보를 읽지 못했습니다. 저장 폴더의 companion.json을 확인해 주세요. \(error.localizedDescription)"
        }
    }

    func persistCompanion(_ next: CompanionProfile) throws {
        guard profileLoaded else { throw CocoaError(.fileReadCorruptFile) }
        try MacBackup(days: [:], companion: next).validate()
        try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
        try JSONEncoder().encode(next).write(to: directory.appendingPathComponent("companion.json"), options: .atomic)
        companion = next
    }

    func equip(color: Int? = nil, weapon: Int? = nil, orb: Int? = nil) {
        save()
        guard error == nil else { return }
        var next = companion
        if let color { next.color = color }
        if let weapon { next.weapon = weapon }
        if let orb { next.orb = orb }
        do { try persistCompanion(next) }
        catch { self.error = "상몬 설정을 저장하지 못했습니다. \(error.localizedDescription)" }
    }

    func rewardActivity() {
        guard profileLoaded, pendingActivity else { return }
        let active = entry.blocks.contains { !$0.text.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty || $0.photo != nil || (($0.kind == .todo || $0.kind == .habit) && $0.checked) }
        guard active else { return }
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.calendar = Calendar(identifier: .gregorian)
        formatter.dateFormat = "yyyy-MM-dd"
        let today = formatter.string(from: Date())
        guard !companion.activityDays.contains(today) else { pendingActivity = false; return }
        var next = companion
        next.activityDays.insert(today)
        do { try persistCompanion(next); pendingActivity = false }
        catch { self.error = "일기는 저장했지만 성장 저장에 실패했습니다. 다시 저장해 주세요. \(error.localizedDescription)" }
    }

    func choosePhoto(_ id: UUID) {
        let panel = NSOpenPanel()
        panel.allowedContentTypes = [.jpeg, .png, .heic, .tiff]
        panel.allowsMultipleSelection = false
        guard panel.runModal() == .OK, let url = panel.url else { return }
        do {
            let size = try url.resourceValues(forKeys: [.fileSizeKey]).fileSize ?? 0
            guard size <= 40_000_000, let image = NSImage(contentsOf: url), image.size.width > 0, image.size.height > 0 else { throw CocoaError(.fileReadCorruptFile) }
            let ratio = min(1, 1600 / max(image.size.width, image.size.height))
            let width = max(1, Int(image.size.width * ratio)), height = max(1, Int(image.size.height * ratio))
            guard let bitmap = NSBitmapImageRep(bitmapDataPlanes: nil, pixelsWide: width, pixelsHigh: height, bitsPerSample: 8, samplesPerPixel: 4, hasAlpha: true, isPlanar: false, colorSpaceName: .deviceRGB, bytesPerRow: 0, bitsPerPixel: 0), let context = NSGraphicsContext(bitmapImageRep: bitmap) else { throw CocoaError(.fileReadCorruptFile) }
            NSGraphicsContext.saveGraphicsState()
            NSGraphicsContext.current = context
            NSColor.white.setFill()
            NSBezierPath(rect: NSRect(x: 0, y: 0, width: width, height: height)).fill()
            image.draw(in: NSRect(x: 0, y: 0, width: width, height: height))
            NSGraphicsContext.restoreGraphicsState()
            guard let data = bitmap.representation(using: .jpeg, properties: [.compressionFactor: 0.85]) else { throw CocoaError(.fileReadCorruptFile) }
            update { entry in
                if let index = entry.blocks.firstIndex(where: { $0.id == id }) { entry.blocks[index].photo = data; entry.blocks[index].kind = .photo }
            }
        } catch { self.error = "사진을 읽지 못했습니다. 40MB 이하의 이미지로 다시 선택해 주세요." }
    }

    func allDays() throws -> [String: DiaryEntry] {
        guard FileManager.default.fileExists(atPath: directory.path) else { return [:] }
        var days: [String: DiaryEntry] = [:]
        for file in try FileManager.default.contentsOfDirectory(at: directory, includingPropertiesForKeys: nil) {
            let key = file.deletingPathExtension().lastPathComponent
            if file.pathExtension == "json", MacBackup.validDate(key) {
                days[key] = try JSONDecoder().decode(DiaryEntry.self, from: Data(contentsOf: file))
            }
        }
        return days
    }

    func exportBackup() {
        save()
        guard error == nil, profileLoaded, loaded else { return }
        let panel = NSSavePanel()
        panel.allowedContentTypes = [.json]
        panel.nameFieldStringValue = "MyDay-Mac-backup.json"
        guard panel.runModal() == .OK, let url = panel.url else { return }
        do {
            let backup = MacBackup(days: try allDays(), companion: companion, windowsMetadata: windowsMetadata)
            try backup.validate()
            let encoder = JSONEncoder()
            encoder.outputFormatting = [.prettyPrinted, .sortedKeys]
            let data = try encoder.encode(backup)
            guard data.count <= 128 * 1024 * 1024 else { throw CocoaError(.fileWriteUnknown) }
            try data.write(to: url, options: .atomic)
        } catch { self.error = "백업하지 못했습니다. \(error.localizedDescription)" }
    }

    func importBackup() {
        save()
        guard error == nil, loaded, profileLoaded else { return }
        let panel = NSOpenPanel()
        panel.allowedContentTypes = [.json]
        guard panel.runModal() == .OK, let url = panel.url else { return }
        do {
            guard (try url.resourceValues(forKeys: [.fileSizeKey]).fileSize ?? 0) <= 128 * 1024 * 1024 else { throw CocoaError(.fileReadTooLarge) }
            let data = try Data(contentsOf: url)
            let root = try JSONDecoder().decode(JSONValue.self, from: data)
            let isWindows = root.object?["Version"] != nil && root.object?["Days"] != nil
            let backup: MacBackup
            let message: String
            if isWindows {
                let incoming = try WindowsCompatibility.decode(data, companion: companion)
                let current = MacBackup(days: try allDays(), companion: companion, windowsMetadata: windowsMetadata)
                let conflicts = incoming.days.keys.filter { current.days[$0] != nil }.count
                backup = try WindowsCompatibility.merge(current: current, incoming: incoming)
                message = "Windows 기록 \(incoming.days.count)일을 합칩니다. 같은 날짜 \(conflicts)일은 가져온 기록으로 교체하고 나머지 맥 기록은 유지합니다. 성장 날짜는 합치고 장비는 백업에 있으면 적용합니다."
            } else {
                backup = try JSONDecoder().decode(MacBackup.self, from: data)
                message = "맥 백업 \(backup.days.count)일의 기록과 상몬 정보를 복원합니다. 현재 맥 기록 전체가 교체됩니다."
            }
            try backup.validate()
            let alert = NSAlert()
            alert.messageText = isWindows ? "Windows 기록을 가져올까요?" : "맥 백업을 복원할까요?"
            alert.informativeText = message + " 교체 전 기록은 복구 폴더에도 보관합니다."
            alert.addButton(withTitle: "취소")
            alert.addButton(withTitle: "가져오기")
            guard alert.runModal() == .alertSecondButtonReturn else { return }
            try restore(backup)
            load()
            loadCompanion()
            loadWindowsMetadata()
            pendingActivity = false
        } catch { self.error = "가져오지 못했습니다. MyDay 맥 또는 Windows 백업인지 확인해 주세요. \(error.localizedDescription)" }
    }

    func restore(_ backup: MacBackup) throws {
        try backup.validate()
        let manager = FileManager.default
        let parent = directory.deletingLastPathComponent()
        let staged = parent.appendingPathComponent("MyDay-restore-\(UUID().uuidString)")
        let previous = parent.appendingPathComponent("MyDay-previous-\(UUID().uuidString)")
        try manager.createDirectory(at: parent, withIntermediateDirectories: true)
        defer { try? manager.removeItem(at: staged) }
        if manager.fileExists(atPath: directory.path) { try manager.copyItem(at: directory, to: staged) }
        else { try manager.createDirectory(at: staged, withIntermediateDirectories: true) }
        for file in try manager.contentsOfDirectory(at: staged, includingPropertiesForKeys: nil) {
            if file.pathExtension == "json", MacBackup.validDate(file.deletingPathExtension().lastPathComponent) { try manager.removeItem(at: file) }
        }
        for (key, entry) in backup.days {
            try JSONEncoder().encode(entry).write(to: staged.appendingPathComponent(key + ".json"), options: .atomic)
        }
        try JSONEncoder().encode(backup.companion).write(to: staged.appendingPathComponent("companion.json"), options: .atomic)
        let metadataFile = staged.appendingPathComponent("windows-metadata.json")
        if let metadata = backup.windowsMetadata {
            try JSONEncoder().encode(metadata).write(to: metadataFile, options: .atomic)
        } else if manager.fileExists(atPath: metadataFile.path) { try manager.removeItem(at: metadataFile) }
        let existed = manager.fileExists(atPath: directory.path)
        if existed { try manager.moveItem(at: directory, to: previous) }
        do { try manager.moveItem(at: staged, to: directory) }
        catch {
            if existed { try manager.moveItem(at: previous, to: directory) }
            throw error
        }
        // Keep the pre-restore directory as an additional recovery copy.
    }
}

struct SangmonView: View {
    var profile: CompanionProfile
    private let colors: [Color] = [.orange, .pink, .mint, .cyan, .purple, .yellow, .indigo, .green]
    private let gear: [Color] = [.clear, .yellow, .red, .cyan, .yellow, .purple, .mint]
    var body: some View {
        TimelineView(.animation(minimumInterval: 1.0 / 24)) { time in
            let wave = sin(time.date.timeIntervalSinceReferenceDate * 2.5)
            ZStack {
                Ellipse().fill(.black.opacity(0.09)).frame(width: 62, height: 9).offset(y: 37)
                ZStack {
                    RoundedRectangle(cornerRadius: 29).fill(colors[profile.color].gradient).frame(width: 69, height: 72)
                    HStack(spacing: 16) {
                        Capsule().fill(.white).frame(width: 13, height: 23).overlay(Circle().fill(.black).frame(width: 6).offset(y: 2))
                        Capsule().fill(.white).frame(width: 13, height: 23).overlay(Circle().fill(.black).frame(width: 6).offset(y: 2))
                    }.offset(y: -6)
                    Capsule().fill(.brown).frame(width: 13, height: 4).rotationEffect(.degrees(-12)).offset(x: 20, y: 17)
                    if profile.weapon > 0 {
                        Image(systemName: "wand.and.stars").font(.system(size: 35)).foregroundStyle(gear[profile.weapon]).shadow(color: .brown, radius: 1).rotationEffect(.degrees(-25)).offset(x: -36, y: 10)
                    }
                    if profile.orb > 0 {
                        Circle().fill(gear[profile.orb].gradient).frame(width: 20).overlay(Circle().stroke(.white, lineWidth: 2)).shadow(color: gear[profile.orb], radius: 7).offset(x: 43, y: -10 + wave * 4)
                    }
                }.rotationEffect(.degrees(wave * 3)).offset(y: wave * 3)
            }.frame(maxWidth: .infinity, maxHeight: .infinity)
        }.accessibilityLabel("상몬 레벨 \(profile.level)")
    }
}

struct CompanionWardrobe: View {
    @ObservedObject var store: DiaryStore
    @Environment(\.dismiss) var dismiss
    let names = ["해제", "태양", "화염", "빙결", "번개", "그림자", "별빛"]
    var body: some View {
        VStack(alignment: .leading, spacing: 18) {
            HStack { Text("나의 작은 친구, 상몬").font(.title.bold()); Spacer(); Button("닫기") { dismiss() } }
            SangmonView(profile: store.companion).frame(height: 140)
            Text("Lv.\(store.companion.level) · \(store.companion.xp) XP · 기록 \(store.companion.activityDays.count)일").font(.headline)
            ProgressView(value: Double(store.companion.activityDays.count % 5), total: 5)
            Text("기록을 저장한 실제 날짜에 하루 10 XP. 5일마다 레벨이 올라요. 쉬어도 성장은 유지됩니다.").foregroundStyle(.secondary)
            Picker("상몬 색", selection: Binding(get: { store.companion.color }, set: { store.equip(color: $0) })) {
                ForEach(0..<8) { index in
                    Text(["살구", "장미", "민트", "하늘", "라벤더", "햇살", "밤하늘", "숲"][index] + (index > min(7, store.companion.level - 1) ? " · Lv.\(index + 1)" : ""))
                        .tag(index).disabled(index > min(7, store.companion.level - 1))
                }
            }
            Picker("마법 무기", selection: Binding(get: { store.companion.weapon }, set: { store.equip(weapon: $0) })) {
                ForEach(0..<names.count, id: \.self) { Text(names[$0]).tag($0) }
            }
            Picker("마법 보주", selection: Binding(get: { store.companion.orb }, set: { store.equip(orb: $0) })) {
                ForEach(0..<names.count, id: \.self) { Text(names[$0]).tag($0) }
            }
            Text("맥 전용 상몬과 장비 디자인입니다. 장비 선택은 경험치를 주지 않습니다.").font(.caption).foregroundStyle(.secondary)
            if let error = store.error { Text(error).foregroundStyle(.red) }
        }.padding(28).frame(width: 520).disabled(!store.profileLoaded)
    }
}

@MainActor
final class DesktopCompanion {
    static let shared = DesktopCompanion()
    private var panel: NSPanel?
    func show(store: DiaryStore) {
        if panel == nil {
            let window = NSPanel(contentRect: NSRect(x: 100, y: 100, width: 150, height: 170), styleMask: [.borderless, .nonactivatingPanel], backing: .buffered, defer: false)
            window.backgroundColor = .clear
            window.isOpaque = false
            window.hasShadow = false
            window.level = .floating
            window.isMovableByWindowBackground = true
            window.collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary]
            window.contentView = NSHostingView(rootView: DesktopPetView(store: store))
            panel = window
        }
        panel?.orderFrontRegardless()
    }
    func hide() { panel?.orderOut(nil) }
}

struct DesktopPetView: View {
    @ObservedObject var store: DiaryStore
    @Environment(\.openWindow) private var openWindow
    var body: some View {
        VStack(spacing: 0) {
            SangmonView(profile: store.companion)
            Button("일기 열기") {
                NSApp.activate(ignoringOtherApps: true)
                openWindow(id: "diary")
            }.buttonStyle(.borderless).padding(8).background(.regularMaterial, in: Capsule())
        }.padding(12).contextMenu {
            Button("성장 · 장비") { store.showingCompanion = true; NSApp.activate(ignoringOtherApps: true) }
            Button("숨기기") { DesktopCompanion.shared.hide() }
        }
    }
}

struct DiaryMenu: View {
    @ObservedObject var store: DiaryStore
    @Environment(\.openWindow) private var openWindow
    var body: some View {
        Button("일기 열기") { openWindow(id: "diary"); NSApp.activate(ignoringOtherApps: true) }
        Button("바탕화면 상몬 보기") { DesktopCompanion.shared.show(store: store) }
        Button("바탕화면 상몬 숨기기") { DesktopCompanion.shared.hide() }
        Divider()
        Button("종료") {
            store.save()
            guard store.error == nil else { openWindow(id: "diary"); NSApp.activate(ignoringOtherApps: true); return }
            NSApp.terminate(nil)
        }
    }
}

@MainActor
final class MyDayLifecycle: NSObject, NSApplicationDelegate {
    weak var store: DiaryStore?
    func applicationShouldTerminate(_ sender: NSApplication) -> NSApplication.TerminateReply {
        guard let store else { return .terminateNow }
        store.save()
        guard let error = store.error else { return .terminateNow }
        let alert = NSAlert()
        alert.messageText = "기록을 저장하지 못해 종료를 중단했습니다."
        alert.informativeText = error
        alert.addButton(withTitle: "돌아가기")
        sender.activate(ignoringOtherApps: true)
        alert.runModal()
        return .terminateCancel
    }
}
