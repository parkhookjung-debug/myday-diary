import Foundation

struct SharedSkin: Decodable, Identifiable { let id: String; let name: String; let level: Int; let xp: Int }
struct SharedItem: Decodable, Identifiable { let id: String; let name: String; let slot: String; let description: String }
struct SharedArtCatalog: Decodable {
    let characters: [SharedSkin]; let items: [SharedItem]
    static let slots = ["weapon", "charm", "head", "face", "neck", "back", "aura", "ground"]
    static let slotNames = ["손 소품", "작은 소품", "머리", "얼굴", "목", "등", "주변 장식", "발밑"]
    static let starters = ["frost-sword", "astral-orb", "solar-crown", "frost-glasses", "solar-scarf", "solar-wings", "solar-ring", "frost-cloud"]
    static var artURL: URL {
        if let url = Bundle.main.url(forResource: "Art", withExtension: nil) { return url }
        return Bundle.module.url(forResource: "Art", withExtension: nil)!
    }
    static let shared: SharedArtCatalog = {
        // A missing catalog is a packaging failure, not an empty wardrobe.
        do { return try JSONDecoder().decode(SharedArtCatalog.self, from: Data(contentsOf: artURL.appendingPathComponent("catalog.json"))) }
        catch { fatalError("MyDay shared art catalog: \(error)") }
    }()
    static func field(_ slot: String) -> String { slot.prefix(1).uppercased() + slot.dropFirst() + "Id" }
    static func grade(_ id: String) -> Int {
        if starters.contains(id) { return 0 }
        let form = String(id.split(separator: "-", maxSplits: 1).last ?? "")
        return CollectionPolicy.forms.firstIndex { $0.contains(form) } ?? 0
    }
}

struct SharedCollectionState {
    var owned = Set<String>(); var coins = 40; var petals = 0; var draws = 0
    var materials = [0, 0, 0, 0]; var milestones = Set<Int>()
    var syntheses = 0; var successes = 0; var crafts = 0
    mutating func milestonesIfNeeded() {
        for m in CollectionPolicy.milestones where owned.count >= m[0] && milestones.insert(m[0]).inserted { materials[m[1]] += m[2] }
    }
}

// Same version 8 wire fields and append-only economic ledger as Windows/Android.
// Unknown metadata stays in fields so newer optional fields survive a round trip.
struct SharedCompanion {
    var fields: [String: JSONValue]
    static let closetID = "00000000000000000000000000000007"
    init(_ fields: [String: JSONValue] = [:]) { self.fields = fields }
    var progress: [String: JSONValue] { fields["Progress"]?.object ?? [:] }
    var awarded: [String] { progress["AwardedDays"]?.array?.compactMap(\.string) ?? [] }
    var activities: [String: Int] {
        var result: [String: Int] = [:]
        for v in progress["Activities"]?.array ?? [] { if let d = v.object?["Date"]?.string, let f = v.object?["Flags"]?.int { result[d] = f } }
        return result
    }
    static func points(_ flags: Int) -> Int { (flags & 2 != 0 ? 10 : 0) + [1, 4, 8, 16].filter { flags & $0 != 0 }.count * 5 }
    static func dayAfter(_ key: String) -> String {
        let f = DateFormatter(); f.locale = Locale(identifier: "en_US_POSIX"); f.calendar = Calendar(identifier: .gregorian); f.dateFormat = "yyyy-MM-dd"
        guard let date = f.date(from: key), let next = f.calendar.date(byAdding: .day, value: 1, to: date) else { return "" }
        return f.string(from: next)
    }
    var weeks: Int {
        var run = 0, total = 0, previous = ""
        for day in activities.keys.sorted() where activities[day]! & 1 != 0 {
            run = Self.dayAfter(previous) == day ? run + 1 : 1
            if run % 7 == 0 { total += 1 }; previous = day
        }
        return total
    }
    var xp: Int { awarded.count * 10 + activities.values.reduce(0) { $0 + Self.points($1) } + weeks * DesktopContract.growthAttendanceWeekXP }
    static func currencyLevel(_ xp: Int) -> Int {
        if xp < 90 { return 1 + xp / 10 }; if xp < 490 { return 10 + (xp - 90) / 20 }; if xp < 1090 { return 30 + (xp - 490) / 30 }; return 50 + (xp - 1090) / 50
    }
    var level: Int { min(DesktopContract.growthMaxLevel, Self.currencyLevel(xp)) }
    var neededXP: Int { level < 10 ? 10 : level < 30 ? 20 : level < 50 ? 30 : 50 }
    var intoLevel: Int {
        let floor = level < 10 ? (level-1)*10 : level < 30 ? 90+(level-10)*20 : level < 50 ? 490+(level-30)*30 : 1090+(level-50)*50
        return level == 100 ? neededXP : xp - floor
    }
    var style: String { fields["CharacterStyle"]?.string ?? "original" }
    var equipment: [String: JSONValue] { fields["Equipment"]?.object ?? [:] }
    func item(_ slot: String) -> String { equipment[SharedArtCatalog.field(slot)]?.string ?? "none" }
    var actions: [[String: JSONValue]] { fields["Collection"]?.object?["Actions"]?.array?.compactMap(\.object) ?? [] }
    mutating func setProgress(awarded: [String], activities: [String: Int]) {
        var p = progress
        p["AwardedDays"] = .array(awarded.sorted().map(JSONValue.string))
        p["Activities"] = .array(activities.keys.sorted().map { .object(["Date": .string($0), "Flags": .number(Double(activities[$0]!))]) })
        fields["Progress"] = .object(p)
        fields["Version"] = .number(Double(max(5, fields["Version"]?.int ?? 4)))
    }
    mutating func addFlags(_ flags: Int, date: String, record: Bool = false) throws {
        guard MacBackup.validDate(date) else { throw WindowsCompatibility.fail("잘못된 활동 날짜입니다.") }
        var a = activities; var days = Set(awarded)
        a[date] = (a[date] ?? 0) | flags
        if a[date] == 0 { a.removeValue(forKey: date) }
        if record { days.insert(date) }
        guard a.count <= 50_000, days.count <= 50_000 else { throw WindowsCompatibility.fail("성장 기록 한도를 초과했습니다.") }
        setProgress(awarded: Array(days), activities: a)
    }
    mutating func prepare(legacy: CompanionProfile) throws {
        let missingGear = fields["Equipment"] == nil || fields["Equipment"] == .null
        var a = Set(awarded); a.formUnion(legacy.activityDays)
        if fields["Progress"] == nil || a.count != awarded.count { setProgress(awarded: Array(a), activities: activities) }
        if fields["CharacterStyle"] == nil { fields["CharacterStyle"] = .string("original") }
        if missingGear {
            fields["Equipment"] = .object(["WeaponId": .string(WindowsCompatibility.equipmentID(legacy.weapon, suffix: "sword")), "CharmId": .string(WindowsCompatibility.equipmentID(legacy.orb, suffix: "orb"))])
        }
        if (fields["Version"]?.int ?? 4) < 7 || missingGear {
            if !actions.contains(where: { $0["Kind"]?.string == "closet" }) { appendRaw("closet", "", id: Self.closetID) }
            var owned = try state().owned
            for slot in SharedArtCatalog.slots { let id = item(slot); if id != "none" && owned.insert(id).inserted { appendRaw("legacy", id) } }
            fields["Version"] = .number(Double(max(7, fields["Version"]?.int ?? 4)))
        }
        try validate()
    }
    func validate() throws {
        let p = progress
        let dates = try WindowsCompatibility.awardedDays(fields)
        guard dates.count <= 50_000 else { throw WindowsCompatibility.fail("성장 기록이 너무 큽니다.") }
        if let raw = p["Activities"], raw != .null, raw.array == nil { throw WindowsCompatibility.fail("활동 목록 형식 오류입니다.") }
        let raw = p["Activities"]?.array ?? []
        guard raw.count <= 50_000, activities.count == raw.count else { throw WindowsCompatibility.fail("활동 기록이 중복되거나 잘못되었습니다.") }
        for (date, flags) in activities { guard MacBackup.validDate(date), (1...31).contains(flags) else { throw WindowsCompatibility.fail("활동 날짜 또는 점수가 잘못되었습니다.") } }
        guard SharedArtCatalog.shared.characters.contains(where: { $0.id == style }) else { throw WindowsCompatibility.fail("알 수 없는 상몬입니다.") }
        for slot in SharedArtCatalog.slots { let id = item(slot); guard id == "none" || SharedArtCatalog.shared.items.contains(where: { $0.id == id && $0.slot == slot }) else { throw WindowsCompatibility.fail("장비 슬롯이 올바르지 않습니다.") } }
        for key in SharedArtCatalog.slots.map(SharedArtCatalog.field) { if let v = equipment[key], v != .null, v.string == nil { throw WindowsCompatibility.fail("소품 ID 형식 오류입니다.") } }
        if let collection = fields["Collection"], collection != .null {
            guard let array = collection.object?["Actions"]?.array, array.count == actions.count else { throw WindowsCompatibility.fail("수집 기록 형식이 올바르지 않습니다.") }
        }
        let replay = try state()
        if (fields["Version"]?.int ?? 4) >= 7 || actions.contains(where: { $0["Kind"]?.string == "closet" }) {
            guard actions.contains(where: { $0["Kind"]?.string == "closet" }), SharedArtCatalog.slots.allSatisfy({ item($0) == "none" || replay.owned.contains(item($0)) }) else { throw WindowsCompatibility.fail("아직 얻지 않은 소품을 착용한 기록이에요.") }
        }
    }
    func state() throws -> SharedCollectionState {
        var s = SharedCollectionState(); s.coins = 40 + xp + (Self.currencyLevel(xp) - 1) * 5 + weeks * 10
        var ids = Set<String>(); var closet = false
        guard actions.count <= 50_301 else { throw WindowsCompatibility.fail("수집 기록이 너무 큽니다.") }
        for a in actions {
            guard let id = a["Id"]?.string, id.range(of: "^[0-9a-fA-F]{32}$", options: .regularExpression) != nil, ids.insert(id.lowercased()).inserted,
                  let kind = a["Kind"]?.string, let item = a["ItemId"]?.string else { throw WindowsCompatibility.fail("수집 ID 또는 소품이 올바르지 않습니다.") }
            if kind == "closet" {
                guard !closet, id == Self.closetID, item.isEmpty else { throw WindowsCompatibility.fail("기본 소품 지급 오류입니다.") }
                closet = true; s.owned.formUnion(SharedArtCatalog.starters); s.milestonesIfNeeded(); continue
            }
            if kind == "synth" {
                let parts = item.split(separator: ":"); let grade = parts.first.flatMap { Int($0) }
                guard closet, parts.count == 2, let g = grade, (0...2).contains(g), String(g) == parts[0], ["up", "stay"].contains(String(parts[1])), s.materials[g] >= CollectionPolicy.synthesisCost else { throw WindowsCompatibility.fail("합성 기록 오류입니다.") }
                s.materials[g] -= CollectionPolicy.synthesisCost; s.syntheses += 1
                if parts[1] == "up" { s.materials[g + 1] += 1; s.successes += 1 } else { s.materials[g] += CollectionPolicy.failureRefund }; continue
            }
            guard SharedArtCatalog.shared.items.contains(where: { $0.id == item }) else { throw WindowsCompatibility.fail("알 수 없는 소품입니다.") }
            let duplicate = s.owned.contains(item), grade = SharedArtCatalog.grade(item)
            if kind == "legacy" { guard closet, !duplicate else { throw WindowsCompatibility.fail("이전 소품 보존 기록 오류입니다.") }; s.owned.insert(item); s.milestonesIfNeeded(); continue }
            if ["draw-tiered", "exchange-tiered", "craft"].contains(kind) && !closet { throw WindowsCompatibility.fail("기본 옷장 지급이 없습니다.") }
            switch kind {
            case "draw", "draw-tiered":
                guard s.coins >= 20, !closet || !SharedArtCatalog.starters.contains(item) else { throw WindowsCompatibility.fail("뽑기 재화 오류입니다.") }
                s.draws += 1
                guard s.draws % 10 != 0 || s.owned.count == 300 || !duplicate else { throw WindowsCompatibility.fail("10회 새 소품 보장 오류입니다.") }
                s.coins -= 20
                if kind == "draw-tiered" { s.materials[grade] += 1 }
                if duplicate { s.petals += kind == "draw-tiered" ? CollectionPolicy.duplicatePetals[grade] : 3 }
            case "exchange", "exchange-tiered":
                let cost = kind == "exchange-tiered" ? CollectionPolicy.exchangePetals[grade] : 15
                guard !duplicate, s.petals >= cost else { throw WindowsCompatibility.fail("꽃잎이 부족합니다.") }; s.petals -= cost
            case "craft":
                guard !duplicate, s.materials[grade] >= CollectionPolicy.craftCost else { throw WindowsCompatibility.fail("같은 등급 조각이 부족합니다.") }
                s.materials[grade] -= CollectionPolicy.craftCost; s.crafts += 1
            default: throw WindowsCompatibility.fail("알 수 없는 수집 기록입니다.")
            }
            s.owned.insert(item); s.milestonesIfNeeded()
        }
        return s
    }
    mutating func appendRaw(_ kind: String, _ item: String, id: String = UUID().uuidString.replacingOccurrences(of: "-", with: "").lowercased()) {
        var c = fields["Collection"]?.object ?? [:]
        c["Actions"] = .array((c["Actions"]?.array ?? []) + [.object(["Id": .string(id), "Kind": .string(kind), "ItemId": .string(item)])])
        fields["Collection"] = .object(c)
    }
    mutating func collect(_ kind: String, item: String = "", grade: Int = 0, choose: (Int) -> Int = { Int.random(in: 0..<$0) }) throws {
        let s = try state(); var result = item
        if kind == "draw-tiered" {
            guard s.coins >= 20 else { throw WindowsCompatibility.fail("클로버 20개가 필요해요.") }
            let pool = SharedArtCatalog.shared.items.filter { !SharedArtCatalog.starters.contains($0.id) && ((s.draws + 1) % 10 != 0 || s.owned.count == 300 || !s.owned.contains($0.id)) }
            let weights = (0...3).map { g in pool.contains { SharedArtCatalog.grade($0.id) == g } ? CollectionPolicy.weights[g] : 0 }
            var roll = choose(weights.reduce(0, +)); guard (0..<weights.reduce(0, +)).contains(roll) else { throw WindowsCompatibility.fail("난수 범위 오류") }
            var g = 0; while roll >= weights[g] { roll -= weights[g]; g += 1 }
            let selected = pool.filter { SharedArtCatalog.grade($0.id) == g }; let index = choose(selected.count)
            guard selected.indices.contains(index) else { throw WindowsCompatibility.fail("난수 범위 오류") }; result = selected[index].id
        } else if kind == "synth" {
            guard (0...2).contains(grade), s.materials[grade] >= CollectionPolicy.synthesisCost else { throw WindowsCompatibility.fail("같은 등급 조각 3개가 필요해요.") }
            let roll = choose(100); guard (0..<100).contains(roll) else { throw WindowsCompatibility.fail("난수 범위 오류") }
            result = "\(grade):" + (roll < CollectionPolicy.synthesisPercent[grade] ? "up" : "stay")
        } else if !["craft", "exchange-tiered"].contains(kind) { throw WindowsCompatibility.fail("지원하지 않는 작업입니다.") }
        var next = self; next.appendRaw(kind, result); next.fields["Version"] = .number(8); try next.validate(); self = next
    }
    static func mergeCollection(_ lhs: JSONValue?, _ rhs: JSONValue?) throws -> JSONValue? {
        let a = lhs?.object?["Actions"]?.array ?? [], b = rhs?.object?["Actions"]?.array ?? []
        func economic(_ all: [JSONValue]) -> [JSONValue] { all.filter { !["closet", "legacy"].contains($0.object?["Kind"]?.string ?? "") } }
        let x = economic(a), y = economic(b)
        guard Array(x.prefix(min(x.count, y.count))) == Array(y.prefix(min(x.count, y.count))) else { throw WindowsCompatibility.fail("두 기기에서 따로 뽑은 내역은 합칠 수 없어요. 같은 수집 기록에서 이어서 사용해 주세요.") }
        if lhs == nil && rhs == nil { return nil }
        var result = x.count >= y.count ? a : b
        if (a + b).contains(where: { $0.object?["Kind"]?.string == "closet" }) {
            if !result.contains(where: { $0.object?["Kind"]?.string == "closet" }) { result.append(.object(["Id": .string(closetID), "Kind": .string("closet"), "ItemId": .string("")])) }
            var owned = Set(SharedArtCatalog.starters + result.compactMap { $0.object?["ItemId"]?.string })
            for v in a + b where v.object?["Kind"]?.string == "legacy" { if let id = v.object?["ItemId"]?.string, owned.insert(id).inserted { result.append(v) } }
        }
        return .object(["Actions": .array(result)])
    }
}
