import Foundation
import AppKit

@MainActor
enum UnifiedVerification {
    static func check(_ value: @autoclosure () -> Bool, _ name: String) throws { try MacVerification.check(value(), name) }
    static func run(root: URL) throws {
        try JournalToolsVerification.run(root: root)
        try DeviceToolsVerification.run(root: root)
        let catalog = SharedArtCatalog.shared
        try check(catalog.characters.count == 64 && catalog.items.count == 300, "shared catalog completeness")
        for skin in catalog.characters { try check(SangmonImages.image("portraits/\(skin.id).png") != nil && SangmonImages.image("sangmon/\(skin.id).png") != nil, "shared character artwork \(skin.id)") }
        for item in catalog.items { try check(SangmonImages.image("items/\(item.id).png") != nil && SangmonImages.image("equipment/\(item.id).png") != nil, "shared item artwork \(item.id)") }
        let fixture = try Data(contentsOf: SharedArtCatalog.artURL.appendingPathComponent("fixtures/windows-v8.json"))
        let incoming = try WindowsCompatibility.decode(fixture)
        var s = SharedCompanion(incoming.windowsMetadata!.fields)
        try s.validate(); let before = try s.state()
        try check(s.xp == 10000 && s.level == 100 && before.materials == [1, 4, 0, 0] && before.coins == 10975 && before.owned.count == 11, "Windows v8 actual serializer import")
        try s.collect("synth", grade: 1, choose: { _ in 0 }); let after = try s.state()
        try check(after.materials == [1, 1, 1, 0] && after.syntheses == 2 && after.successes == 2 && after.coins == before.coins && after.owned == before.owned, "same Android/Windows synthesis result")
        var edited = incoming; edited.windowsMetadata = WindowsMetadata(fields: s.fields)
        let exported = try WindowsCompatibility.encode(edited)
        let restored = try WindowsCompatibility.decode(exported)
        for (key, value) in s.fields { try check(restored.windowsMetadata?.fields[key] == value, "v8 original field retained: \(key)") }
        let roundTripState = try SharedCompanion(restored.windowsMetadata!.fields).state()
        try check(roundTripState.materials == after.materials && roundTripState.owned == after.owned && roundTripState.coins == after.coins && roundTripState.petals == after.petals && roundTripState.syntheses == after.syntheses, "v8 economy round trip")
        if let path = ProcessInfo.processInfo.environment["MYDAY_COMPAT_EXPORT"] { try exported.write(to: URL(fileURLWithPath: path).deletingLastPathComponent().appendingPathComponent("mac-v8-to-windows.json"), options: .atomic) }
        var localGear = CompanionProfile(); localGear.weapon = 5
        let gearless = try WindowsCompatibility.decode(fixture, companion: localGear)
        let retained = try WindowsCompatibility.decode(WindowsCompatibility.encode(gearless))
        let retainedState = SharedCompanion(retained.windowsMetadata!.fields)
        let retainedItems = try retainedState.state()
        try check(retainedState.item("weapon") == "shadow-sword" && retainedItems.owned.contains("shadow-sword") && retainedState.xp == s.xp, "gearless import retains legacy worn item and ownership")
        var locked = s; locked.fields["Equipment"] = .object(["WeaponId": .string("astral-bottle")])
        do { try locked.validate(); throw MacVerification.Failure.check("unowned equipped item accepted") } catch is CompatibilityError { }
        for profile in DesktopFixtures.growthProfiles {
            var growth = SharedCompanion(); var day = "2025-01-01"; var count = 0
            while growth.level < 100 && count < 1000 {
                let mask = profile[1 + count % 7]; try growth.addFlags(mask & 31, date: day, record: mask & 32 != 0)
                count += 1; day = SharedCompanion.dayAfter(day)
            }
            try check(count == profile[0], "common 100/200/300 day growth profile")
        }
        var legacy = CompanionProfile(); legacy.weapon = 2; legacy.orb = 3; legacy.activityDays = ["2026-01-01"]
        var migrated = SharedCompanion(); try migrated.prepare(legacy: legacy); let state = try migrated.state()
        try check(migrated.xp == 10 && state.owned.contains("ember-sword") && state.owned.contains("frost-orb") && state.owned.count == 10, "legacy Mac XP and gear retained")
        let snapshot = migrated.fields
        do { try migrated.collect("synth", grade: 3); throw MacVerification.Failure.check("invalid synthesis accepted") } catch is CompatibilityError { }
        try check(migrated.fields == snapshot, "economy rollback")
        var fork = s; fork.appendRaw("draw-tiered", "solar-bottle"); var other = s; other.appendRaw("draw-tiered", "solar-bottle")
        do { _ = try SharedCompanion.mergeCollection(fork.fields["Collection"], other.fields["Collection"]); throw MacVerification.Failure.check("divergent ledger accepted") } catch is CompatibilityError { }
        let store = DiaryStore(directory: root.appendingPathComponent("unified/MyDay"))
        store.update { $0.blocks = [DiaryBlock(kind: .text, text: "This private text must not appear in a reusable layout.")]; $0.blocks[0].height = 400; $0.pageStyle = "dots" }
        store.saveLayout("나의 배치"); try check(store.error == nil && store.savedLayouts.count == 1, "save personal layout")
        let layout = store.savedLayouts[0]; let json = try JSONEncoder().encode(JSONValue.object(layout))
        try check(!String(decoding: json, as: UTF8.self).contains("private text"), "layout excludes private content")
        store.select(Date().addingTimeInterval(86400)); let xp = store.sharedCompanion.xp; store.applySavedLayout(layout)
        try check(store.entry.blocks.count == 1 && store.entry.blocks[0].text.isEmpty && store.entry.blocks[0].height == 400 && store.sharedCompanion.xp == xp, "reusable blank layout and card size without XP")
        store.update { $0.pageStyle = "paper" }; store.applySavedLayout(layout)
        try check(store.entry.pageStyle == "paper" && store.entry.blocks.count == 2, "append layout keeps existing page settings")
        let metadata = store.windowsMetadata; let file = store.directory.appendingPathComponent("windows-metadata.json")
        try FileManager.default.removeItem(at: file); try FileManager.default.createDirectory(at: file, withIntermediateDirectories: false)
        store.collect("draw-tiered"); try check(store.error != nil && store.windowsMetadata == metadata, "failed metadata write retains currency and ownership")
        print("Shared art / growth profiles / v8 economy / reusable layouts / write rollback: passed")
    }
}
