import Foundation
import SwiftUI
import AppKit

@MainActor
enum MacVerification {
    enum Failure: Error { case check(String) }
    static func check(_ condition: @autoclosure () -> Bool, _ name: String) throws {
        if !condition() { throw Failure.check(name) }
    }
    static func renderPreview() throws {
        _ = NSApplication.shared
        let root = FileManager.default.temporaryDirectory.appendingPathComponent("MyDay-preview-\(UUID())")
        defer { try? FileManager.default.removeItem(at: root) }
        let store = DiaryStore(directory: root.appendingPathComponent("MyDay"))
        store.update { entry in
            entry.blocks = [DiaryBlock(kind: .text, text: "맥에서 시작하는 나의 작은 하루.\n오늘은 산책을 하고 좋아하는 음악을 들었어요."), DiaryBlock(kind: .todo, text: "나를 위한 시간 10분", checked: true)]
        }
        try render(DiaryView(store: store), name: "preview", width: 920, height: 860)
        try render(DiaryView(store: store), name: "preview-narrow", width: 620, height: 800)
        let templates = try TemplateCatalog.load()
        store.entry = DiaryEntry()
        store.applyTemplate(templates.first { $0.id == "cornell-notes" }!)
        store.update { entry in
            entry.blocks[0].text = "질문하고 싶은 개념은?"
            entry.blocks[1].text = "오늘 배운 내용을 나의 언어로 적어 보세요.\n\n오른쪽 아래 손잡이로 크기를 조절할 수 있어요."
            entry.blocks[2].text = "배운 내용을 한 문장으로 정리하기."
        }
        try render(DiaryBoard(store: store, startsEditing: true).padding(20), name: "preview-free-layout", width: 880, height: 650)
        try render(TemplateGallery(store: store), name: "preview-templates", width: 820, height: 650)
        print("Previews: macos/build/preview*.png")
    }
    static func render<V: View>(_ content: V, name: String, width: CGFloat, height: CGFloat) throws {
        let view = NSHostingView(rootView: content.background(Color(red: 1, green: 0.97, blue: 0.95)).preferredColorScheme(.light))
        let window = NSWindow(contentRect: NSRect(x: 0, y: 0, width: width, height: height), styleMask: [.titled], backing: .buffered, defer: false)
        window.contentView = view
        view.frame = NSRect(x: 0, y: 0, width: width, height: height)
        view.layoutSubtreeIfNeeded()
        RunLoop.main.run(until: Date().addingTimeInterval(0.5))
        guard let bitmap = view.bitmapImageRepForCachingDisplay(in: view.bounds) else { throw Failure.check("preview bitmap") }
        view.cacheDisplay(in: view.bounds, to: bitmap)
        guard let png = bitmap.representation(using: .png, properties: [:]) else { throw Failure.check("preview PNG") }
        try png.write(to: URL(fileURLWithPath: "macos/build/" + name + ".png"))
    }
    static func run() throws {
        let root = FileManager.default.temporaryDirectory.appendingPathComponent("MyDay-check-\(UUID())")
        defer { try? FileManager.default.removeItem(at: root) }
        let directory = root.appendingPathComponent("MyDay")
        let store = DiaryStore(directory: directory)
        try check(store.loaded && store.profileLoaded, "empty store")
        store.update { $0.theme = 2; $0.blocks.append(DiaryBlock(kind: .text)) }
        try check(store.companion.xp == 0, "empty block does not grant XP")
        store.update { $0.blocks[0].text = "맥에서 쓴 하루" }
        try check(store.error == nil && store.companion.xp == 10, "save and reward")
        store.update { $0.blocks[0].text += " 다시 작성" }
        try check(store.companion.xp == 10, "daily XP limit")
        let reopened = DiaryStore(directory: directory)
        try check(reopened.entry.blocks.first?.text == "맥에서 쓴 하루 다시 작성", "reopen diary")
        try check(reopened.companion.xp == 10 && !reopened.pendingActivity, "reopen growth")
        reopened.equip(weapon: 2, orb: 3)
        try check(reopened.companion.weapon == 2 && reopened.companion.orb == 3, "independent equipment")
        let backup = MacBackup(days: try reopened.allDays(), companion: reopened.companion)
        let encoded = try JSONEncoder().encode(backup)
        let decoded = try JSONDecoder().decode(MacBackup.self, from: encoded)
        try decoded.validate()
        reopened.update { $0.blocks[0].text = "changed" }
        try reopened.restore(decoded)
        reopened.load()
        try check(reopened.entry.blocks.first?.text == "맥에서 쓴 하루 다시 작성", "backup restore")
        try check(MacBackup.validDate("2024-02-29") && !MacBackup.validDate("2025-02-29") && !MacBackup.validDate("../../bad"), "date validation")
        var corrupt = backup
        corrupt.version = 99
        do { try corrupt.validate(); throw Failure.check("future backup accepted") }
        catch is CocoaError { }
        corrupt = backup
        corrupt.companion.weapon = 99
        do { try corrupt.validate(); throw Failure.check("invalid gear accepted") }
        catch is CocoaError { }
        // Existing SwiftUI diaries have no photo field; they must remain readable.
        let legacy = Data("{\"theme\":0,\"character\":\"🐰\",\"blocks\":[{\"id\":\"00000000-0000-0000-0000-000000000001\",\"kind\":\"text\",\"text\":\"old\",\"checked\":false}]}".utf8)
        let entry = try JSONDecoder().decode(DiaryEntry.self, from: legacy)
        try check(entry.blocks[0].photo == nil && entry.blocks[0].text == "old", "legacy diary")
        try CompatibilityVerification.run(root: root)
        // A failed write must retain the unsaved editor and prevent date navigation.
        let blocked = root.appendingPathComponent("blocked")
        try Data().write(to: blocked)
        let broken = DiaryStore(directory: blocked)
        let originalDate = broken.date
        broken.update { $0.blocks.append(DiaryBlock(kind: .text, text: "keep me")) }
        broken.select(originalDate.addingTimeInterval(86400))
        try check(broken.error != nil && broken.date == originalDate && broken.entry.blocks.first?.text == "keep me", "save failure retention")
    }
}
