import Foundation

@MainActor
extension DiaryStore {
    var sharedCompanion: SharedCompanion {
        var result = SharedCompanion(windowsMetadata?.fields ?? [:])
        // All inputs have already been validated on load/restore. Keep failures visible there.
        try? result.prepare(legacy: companion)
        return result
    }
    func commitShared(_ change: (inout SharedCompanion) throws -> Void) {
        guard loaded, profileLoaded, metadataLoaded else { return }
        do {
            var next = sharedCompanion
            try change(&next); try next.validate()
            let metadata = WindowsMetadata(fields: next.fields)
            try WindowsCompatibility.validateMetadata(metadata)
            try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
            try JSONEncoder().encode(metadata).write(to: directory.appendingPathComponent("windows-metadata.json"), options: .atomic)
            windowsMetadata = metadata
            companion.activityDays.formUnion(next.awarded)
            error = nil
        } catch { self.error = "저장하지 못했어요. 소품과 재료는 유지됩니다. \(error.localizedDescription)" }
    }
    static func todayKey(_ date: Date = Date()) -> String {
        let f = DateFormatter(); f.locale = Locale(identifier: "en_US_POSIX"); f.calendar = Calendar(identifier: .gregorian); f.dateFormat = "yyyy-MM-dd"; return f.string(from: date)
    }
    func checkIn() { save(); guard error == nil else { return }; commitShared { try $0.addFlags(1, date: Self.todayKey()) } }
    func selectSkin(_ id: String) {
        save(); guard error == nil else { return }
        commitShared { s in
            guard let skin = SharedArtCatalog.shared.characters.first(where: { $0.id == id }), s.xp >= skin.xp else { throw WindowsCompatibility.fail("아직 해금되지 않은 상몬이에요.") }
            s.fields["CharacterStyle"] = .string(id)
        }
    }
    func selectItem(_ id: String, slot: String) {
        save(); guard error == nil else { return }
        commitShared { s in
            let owned = try s.state().owned
            guard SharedArtCatalog.slots.contains(slot), id == "none" || owned.contains(id) && SharedArtCatalog.shared.items.contains(where: { $0.id == id && $0.slot == slot }) else { throw WindowsCompatibility.fail("먼저 이 소품을 수집해 주세요.") }
            var e = s.equipment; e[SharedArtCatalog.field(slot)] = .string(id); s.fields["Equipment"] = .object(e)
        }
    }
    func collect(_ kind: String, item: String = "", grade: Int = 0) { save(); guard error == nil else { return }; commitShared { try $0.collect(kind, item: item, grade: grade) } }
}
