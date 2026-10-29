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
    var version = 3
    var days: [String: DiaryEntry]
    var companion: CompanionProfile
    var windowsMetadata: WindowsMetadata?

    func validate() throws {
        guard format == "myday-macos", (1...3).contains(version), days.count <= DesktopContract.maxDays,
              (0...7).contains(companion.color), (0...6).contains(companion.weapon),
              (0...6).contains(companion.orb), companion.activityDays.count <= 50_000,
              companion.color <= min(7, companion.level - 1) else { throw CocoaError(.fileReadCorruptFile) }
        try WindowsCompatibility.validateMetadata(windowsMetadata)
        for key in Array(days.keys) + Array(companion.activityDays) {
            guard Self.validDate(key) else { throw CocoaError(.fileReadCorruptFile) }
        }
        for day in days.values {
            guard DesktopPalette.backgrounds.indices.contains(day.theme), day.blocks.count <= DesktopContract.maxBlocks,
                  Set(day.blocks.map(\.id)).count == day.blocks.count else { throw CocoaError(.fileReadCorruptFile) }
            guard ["cards", "free"].contains(day.layoutMode), DesktopContract.pageStyles.contains(day.pageStyle) else { throw CocoaError(.fileReadCorruptFile) }
            for block in day.blocks {
                try WindowsCompatibility.checkBounds(block.x, block.y, block.width, block.height)
                guard block.text.utf16.count <= DesktopContract.maxTextUtf16, (block.title?.utf16.count ?? 0) <= DesktopContract.maxTitleUtf16, (block.prompt?.utf16.count ?? 0) <= DesktopContract.maxPromptUtf16,
                      block.photo == nil || block.kind == .photo, block.videoPath == nil || block.kind == .video else { throw CocoaError(.fileReadCorruptFile) }
                if let photo = block.photo {
                    guard photo.count <= 10_000_000, NSImage(data: photo) != nil else { throw CocoaError(.fileReadCorruptFile) }
                }
                if let path = block.videoPath { try WindowsCompatibility.checkVideoPath(path) }
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
            if !FileManager.default.fileExists(atPath:url.path),FileManager.default.fileExists(atPath:url.appendingPathExtension("previous").path){throw CocoaError(.fileReadNoSuchFile)}
            let profile = FileManager.default.fileExists(atPath: url.path)
                ? try JSONDecoder().decode(CompanionProfile.self, from: Data(contentsOf: url)) : CompanionProfile()
            try MacBackup(days: [:], companion: profile).validate()
            companion = profile
            profileLoaded = true
        } catch {
            profileLoaded=false
            self.error = "상몬 정보를 읽지 못했습니다. 저장 폴더의 companion.json을 확인해 주세요. \(error.localizedDescription)"
        }
    }

    func persistCompanion(_ next: CompanionProfile) throws {
        guard profileLoaded else { throw CocoaError(.fileReadCorruptFile) }
        try MacBackup(days: [:], companion: next).validate()
        try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
        try SafeStorage.write(JSONEncoder().encode(next),to:directory.appendingPathComponent("companion.json")){data in
            try MacBackup(days:[:],companion:JSONDecoder().decode(CompanionProfile.self,from:data)).validate()
        }
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
        let prior = Dictionary(uniqueKeysWithValues: (pendingBaseline ?? []).map { ($0.id, $0) })
        var flags = 0
        let text = entry.blocks.filter { $0.kind == .text || $0.kind == .emotion }
        if text.reduce(0, { $0 + $1.text.unicodeScalars.filter { !CharacterSet.whitespacesAndNewlines.contains($0) }.reduce(0) { $0 + ($1.value > 0xffff ? 2 : 1) } }) >= 20 && text.contains(where: { prior[$0.id]?.text != $0.text }) { flags |= 2 }
        if entry.blocks.contains(where: { $0.photo != nil && prior[$0.id]?.photo != $0.photo }) { flags |= 4 }
        if entry.blocks.contains(where: { $0.kind == .todo && $0.checked && prior[$0.id]?.checked != true }) { flags |= 8 }
        if entry.blocks.contains(where: { $0.kind == .habit && $0.checked && prior[$0.id]?.checked != true }) { flags |= 16 }
        commitShared { try $0.addFlags(flags, date: Self.todayKey(), record: true) }
        if error == nil { pendingActivity = false; pendingBaseline = nil }
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

    func chooseVideo(_ id: UUID) {
        let panel = NSOpenPanel()
        panel.allowedContentTypes = [.movie, .mpeg4Movie, .quickTimeMovie]
        panel.allowsMultipleSelection = false
        guard panel.runModal() == .OK, let url = panel.url else { return }
        do {
            let values = try url.resourceValues(forKeys: [.isRegularFileKey, .fileSizeKey])
            guard values.isRegularFile == true, (values.fileSize ?? 0) <= 2_000_000_000 else { throw CocoaError(.fileReadTooLarge) }
            let path = url.standardizedFileURL.path
            try WindowsCompatibility.checkVideoPath(path)
            update { entry in
                if let index = entry.blocks.firstIndex(where: { $0.id == id }) { entry.blocks[index].videoPath = path; entry.blocks[index].kind = .video }
            }
        } catch { self.error = "영상 파일을 읽지 못했습니다. 2GB 이하의 MP4·MOV 영상을 선택해 주세요." }
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
        guard MacPrivacy.shared.authorize() else{return}
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
        guard MacPrivacy.shared.authorize() else{return}
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
        for (key,previous) in try allDays() { try JournalHistory.beforeSave(directory,date: key,previous: previous,next: backup.days[key]) }
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
            SharedSangmonView(companion: store.sharedCompanion,reactionTick: store.reactionTick).frame(width: 126, height: 126)
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
