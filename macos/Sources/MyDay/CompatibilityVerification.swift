import Foundation
import AppKit

@MainActor
enum CompatibilityVerification {
    static func check(_ value: @autoclosure () -> Bool, _ name: String) throws { try MacVerification.check(value(), name) }
    static func data(_ fields: [String: JSONValue]) throws -> Data { try JSONEncoder().encode(JSONValue.object(fields)) }
    static func fixture() throws -> [String: JSONValue] {
        guard let bitmap = NSBitmapImageRep(bitmapDataPlanes: nil, pixelsWide: 24, pixelsHigh: 18, bitsPerSample: 8, samplesPerPixel: 3, hasAlpha: false, isPlanar: false, colorSpaceName: .deviceRGB, bytesPerRow: 0, bitsPerPixel: 0), let pixels = bitmap.bitmapData else { throw MacVerification.Failure.check("fixture image") }
        pixels.initialize(repeating: 180, count: bitmap.bytesPerRow * bitmap.pixelsHigh)
        guard let photo = bitmap.representation(using: .jpeg, properties: [.compressionFactor: 0.85]) else { throw MacVerification.Failure.check("fixture JPEG") }
        let kinds: [String] = ["text", "todo", "habit", "emotion", "photo", "photo-slot"]
        let blocks: [JSONValue] = kinds.enumerated().map { index, kind in
            var fields: [String: JSONValue] = ["Id": .string("windows-id-\(index)"), "Kind": .string(kind), "Text": .string(index == 4 ? "산책 사진 설명" : "Windows 기록 \(index)"), "Checked": .bool(index == 1 || index == 2), "Title": .string("제목 \(index)"), "Prompt": .string("질문 \(index)?"), "Wide": .bool(index == 0), "X": .number(Double(index * 380)), "Y": .number(Double(index * 260)), "Width": .number(340), "Height": .number(250), "CustomBlock": .string("preserved")]
            if kind == "photo" { fields["Photo"] = .string(photo.base64EncodedString()) }
            return .object(fields)
        }
        let day: JSONValue = .object(["Theme": .number(2), "Mood": .string("설레요"), "LayoutMode": .string("free"), "PageStyle": .string("dots"), "Blocks": .array(blocks), "CustomDay": .array([.string("keep"), .bool(true)])])
        let layout: JSONValue = .object(["Id": .string("saved-layout-1"), "Name": .string("사진과 한 줄"), "Mode": .string("free"), "Style": .string("paper"), "Theme": .number(1), "Blocks": .array([.object(["Kind": .string("photo-slot"), "Title": .string("사진 자리"), "Prompt": .string("순간을 담아요"), "Wide": .bool(false), "X": .number(12), "Y": .number(12), "Width": .number(340), "Height": .number(360)])])])
        return ["Version": .number(4), "Days": .array([.object(["Key": .string("2024-02-29"), "Value": day])]), "CharacterStyle": .string("winged"), "Progress": .object(["AwardedDays": .array([.string("2024-02-29"), .string("2024-03-01")]), "CustomProgress": .string("keep")]), "Equipment": .object(["WeaponId": .string("frost-sword"), "CharmId": .string("astral-orb")]), "Layouts": .array([layout]), "CustomBook": .object(["hello": .string("한글 ✓")])]
    }
    static func reject(_ fields: [String: JSONValue], _ reason: String) throws {
        do { _ = try WindowsCompatibility.decode(data(fields)); throw MacVerification.Failure.check("accepted invalid Windows backup: " + reason) }
        catch is CompatibilityError { }
        catch is CocoaError { }
    }
    static func run(root: URL) throws {
        let fields = try fixture()
        let imported = try WindowsCompatibility.decode(data(fields))
        let day = imported.days["2024-02-29"]!
        try check(day.blocks.count == 6 && day.mood == "설레요" && day.layoutMode == "free" && day.pageStyle == "dots", "Windows entry fields")
        try check(day.blocks[1].checked && day.blocks[4].photo != nil && day.blocks[5].kind == .photoSlot, "Windows block kinds")
        try check(imported.companion.weapon == 3 && imported.companion.orb == 6 && imported.companion.activityDays.count == 2, "Windows growth and equipment")
        var edited = imported
        edited.days["2024-02-29"]!.blocks[0].text = "맥에서 수정한 글"
        let output = try WindowsCompatibility.encode(edited)
        guard let rootFields = try JSONDecoder().decode(JSONValue.self, from: output).object else { throw MacVerification.Failure.check("export root") }
        try check(rootFields["Version"] == .number(4) && rootFields["Days"]?.array?.count == 1, ".NET dictionary wire format")
        try check(rootFields["Layouts"] == fields["Layouts"] && rootFields["CharacterStyle"] == fields["CharacterStyle"] && rootFields["CustomBook"] == fields["CustomBook"], "Windows metadata preservation")
        try check(rootFields["Progress"]?.object?["CustomProgress"] == .string("keep"), "growth extra fields preservation")
        let roundTrip = try WindowsCompatibility.decode(output)
        let after = roundTrip.days["2024-02-29"]!
        try check(after.blocks[0].text == "맥에서 수정한 글" && after.windowsFields == day.windowsFields, "editable Windows round trip")
        for index in day.blocks.indices {
            let before = day.blocks[index], after = after.blocks[index]
            try check(before.windowsID == after.windowsID && before.title == after.title && before.prompt == after.prompt && before.checked == after.checked && before.photo == after.photo && before.wide == after.wide && before.windowsFields == after.windowsFields && DiaryLayout.bounds(before) == DiaryLayout.bounds(after), "block field round trip \(index)")
        }
        // Both .NET dictionary arrays and simple JSON dictionary objects are accepted.
        var objectForm = fields
        objectForm["Days"] = .object(["2024-02-29": fields["Days"]!.array![0].object!["Value"]!])
        let objectBackup = try WindowsCompatibility.decode(data(objectForm))
        try check(objectBackup.days.count == 1, "simple dictionary format")
        for version in 1...4 {
            var legacy = fields
            legacy["Version"] = .number(Double(version))
            if version < 4 { legacy.removeValue(forKey: "Equipment") }
            if version < 3 { legacy.removeValue(forKey: "Progress") }
            if version < 2 { legacy.removeValue(forKey: "Layouts") }
            _ = try WindowsCompatibility.decode(data(legacy))
        }
        var bad = fields
        bad["Version"] = .number(5)
        try reject(bad, "future version")
        bad = fields
        bad["Days"] = .array(fields["Days"]!.array! + fields["Days"]!.array!)
        try reject(bad, "duplicate dates")
        bad = fields
        bad["Progress"] = .object(["AwardedDays": .array([.string("2025-02-29")])])
        try reject(bad, "invalid growth date")
        bad = fields
        var pair = fields["Days"]!.array![0].object!
        var corruptDay = pair["Value"]!.object!
        var blocks = corruptDay["Blocks"]!.array!
        var first = blocks[0].object!
        first["Width"] = .number(299)
        blocks[0] = .object(first)
        corruptDay["Blocks"] = .array(blocks)
        pair["Value"] = .object(corruptDay)
        bad["Days"] = .array([.object(pair)])
        try reject(bad, "invalid bounds")
        first["Width"] = .number(340)
        first["Kind"] = .string("mystery")
        blocks[0] = .object(first)
        corruptDay["Blocks"] = .array(blocks); pair["Value"] = .object(corruptDay); bad["Days"] = .array([.object(pair)])
        try reject(bad, "unknown block kind")
        first["Kind"] = .string("text")
        first["Id"] = blocks[1].object!["Id"]
        blocks[0] = .object(first)
        corruptDay["Blocks"] = .array(blocks); pair["Value"] = .object(corruptDay); bad["Days"] = .array([.object(pair)])
        try reject(bad, "duplicate block IDs")
        first["Id"] = .string("new-id")
        blocks[0] = .object(first)
        var photoBlock = blocks[4].object!
        photoBlock["Photo"] = .string(Data("not a JPEG".utf8).base64EncodedString())
        blocks[4] = .object(photoBlock)
        corruptDay["Blocks"] = .array(blocks); pair["Value"] = .object(corruptDay); bad["Days"] = .array([.object(pair)])
        try reject(bad, "bad photo")
        var currentProfile = CompanionProfile()
        currentProfile.activityDays = ["2024-03-02"]
        currentProfile.weapon = 2; currentProfile.orb = 4
        var oldBackup = fields
        oldBackup.removeValue(forKey: "Equipment")
        let mergedProfile = try WindowsCompatibility.decode(data(oldBackup), companion: currentProfile).companion
        try check(mergedProfile.weapon == 2 && mergedProfile.orb == 4 && mergedProfile.activityDays.count == 3, "legacy equipment retention and growth union")
        var oldMetadata = imported.windowsMetadata!
        var otherLayout = oldMetadata.fields["Layouts"]!.array![0].object!
        otherLayout["Id"] = .string("different-id")
        oldMetadata.fields["Layouts"] = .array([.object(otherLayout)])
        let metadata = try WindowsCompatibility.mergeMetadata(oldMetadata, imported.windowsMetadata)
        try check(metadata?.fields["Layouts"]?.array?.count == 2 && metadata?.fields["Layouts"]?.array?[1].object?["Name"] == .string("사진과 한 줄 (2)"), "layout merge names")
        var existingDay = DiaryEntry()
        existingDay.blocks = [DiaryBlock(kind: .text, text: "맥에만 있는 기록")]
        let current = MacBackup(days: ["2024-02-29": existingDay, "2024-03-02": existingDay], companion: currentProfile)
        let merged = try WindowsCompatibility.merge(current: current, incoming: imported)
        try check(merged.days.count == 2 && merged.days["2024-03-02"]!.blocks[0].text == "맥에만 있는 기록" && merged.days["2024-02-29"]!.blocks.count == 6, "merge replaces conflicts and retains other dates")
        try check(merged.companion.activityDays.count == 3 && merged.companion.weapon == 3 && merged.companion.orb == 6, "merge growth and incoming equipment")
        let store = DiaryStore(directory: root.appendingPathComponent("interop/MyDay"))
        try store.restore(imported)
        store.loadWindowsMetadata(); store.loadCompanion()
        let saved = MacBackup(days: try store.allDays(), companion: store.companion, windowsMetadata: store.windowsMetadata)
        let reopened = try JSONDecoder().decode(MacBackup.self, from: JSONEncoder().encode(saved))
        try check(reopened.windowsMetadata == imported.windowsMetadata && reopened.days["2024-02-29"]?.blocks[4].photo == day.blocks[4].photo, "Mac backup retains Windows data")
        var invalidRestore = reopened
        invalidRestore.days["2024-02-29"]!.blocks[0].x = -1
        do { try store.restore(invalidRestore); throw MacVerification.Failure.check("invalid restore accepted") }
        catch is CompatibilityError { }
        let unchangedDays = try store.allDays()
        try check(unchangedDays["2024-02-29"]?.blocks[0].text == day.blocks[0].text, "invalid restore leaves files intact")
        try templatesAndLayout()
        // Leave a synthetic round-trip fixture for the optional Windows serializer check.
        if let path = ProcessInfo.processInfo.environment["MYDAY_COMPAT_EXPORT"] { try output.write(to: URL(fileURLWithPath: path), options: .atomic) }
        print("Windows v1–v4 / photos / metadata / merge / 100 templates / layout: passed")
    }
    static func templatesAndLayout() throws {
        let templates = try TemplateCatalog.load()
        try check(templates.count == 100 && Set(templates.map(\.category)) == Set(TemplateCatalog.categories), "complete template catalog")
        try check(TemplateCatalog.find(templates, category: "", query: "코넬").contains { $0.id == "cornell-notes" }, "template search")
        for template in templates {
            var page = DiaryEntry()
            try DiaryLayout.apply(template, to: &page, width: 800)
            try check(page.layoutMode == "free" && page.pageStyle == template.style && page.blocks.count == template.sections.count, "fresh template " + template.id)
            try MacBackup(days: ["2024-01-01": page], companion: CompanionProfile()).validate()
            let firstIDs = Set(page.blocks.map(\.id))
            var second = DiaryEntry()
            try DiaryLayout.apply(template, to: &second, width: 800)
            try check(firstIDs.isDisjoint(with: second.blocks.map(\.id)) && page.blocks.allSatisfy { $0.text.isEmpty && !$0.checked && $0.photo == nil }, "independent empty template " + template.id)
            page.blocks[0].text = "보존할 일기"
            let original = page.blocks
            try DiaryLayout.apply(template, to: &page, width: 800)
            try check(page.blocks[0].text == "보존할 일기" && page.blocks.prefix(original.count).map(\.id) == original.map(\.id) && zip(page.blocks.prefix(original.count), original).allSatisfy { DiaryLayout.bounds($0.0) == DiaryLayout.bounds($0.1) }, "append preserves original " + template.id)
            let exported = try WindowsCompatibility.encode(MacBackup(days: ["2024-01-01": page], companion: CompanionProfile()))
            let decoded = try WindowsCompatibility.decode(exported)
            try check(decoded.days["2024-01-01"]!.blocks.map(\.title) == page.blocks.map(\.title), "template Windows export " + template.id)
        }
        let cornell = DiaryLayout.preset("cornell", count: 3, width: 800)
        try check(cornell == [BlockRect(x: 12, y: 12, width: 300, height: 250), BlockRect(x: 328, y: 12, width: 448, height: 250), BlockRect(x: 12, y: 278, width: 764, height: 200)], "Windows Cornell preset geometry")
        let rect = BlockRect(x: 12, y: 18, width: 340, height: 250)
        try check(DiaryLayout.dragged(rect, dx: 40, dy: 22, resizing: false) == BlockRect(x: 52, y: 40, width: 340, height: 250), "drag logical coordinates")
        try check(DiaryLayout.dragged(rect, dx: -900, dy: 20_000, resizing: false) == BlockRect(x: 0, y: 10_000, width: 340, height: 250), "drag limits")
        try check(DiaryLayout.dragged(rect, dx: -900, dy: 20_000, resizing: true) == BlockRect(x: 12, y: 18, width: 300, height: 1400), "resize limits")
        var full = DiaryEntry()
        full.blocks = (0..<200).map { _ in DiaryBlock(kind: .text) }
        let original = full.blocks.map(\.id)
        do { try DiaryLayout.apply(templates[0], to: &full, width: 800); throw MacVerification.Failure.check("201 blocks accepted") }
        catch is CompatibilityError { }
        try check(full.blocks.map(\.id) == original, "failed template leaves page intact")
        var noSpace = DiaryEntry()
        noSpace.layoutMode = "free"
        var bottom = DiaryBlock(kind: .text, text: "keep")
        DiaryLayout.setBounds(BlockRect(x: 0, y: 10_000, width: 340, height: 250), on: &bottom)
        noSpace.blocks = [bottom]
        do { try DiaryLayout.apply(templates[0], to: &noSpace, width: 800); throw MacVerification.Failure.check("overflow accepted") }
        catch is CompatibilityError { }
        try check(noSpace.blocks.count == 1 && noSpace.blocks[0].text == "keep", "overflow leaves page intact")
    }
}
