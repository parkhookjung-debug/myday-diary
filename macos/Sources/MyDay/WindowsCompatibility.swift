import Foundation
import AppKit
import ImageIO

struct CompatibilityError: LocalizedError {
    var message: String
    var errorDescription: String? { message }
}

struct WindowsMetadata: Codable, Equatable {
    var fields: [String: JSONValue]
}

enum WindowsCompatibility {
    static let maxBytes = 128 * 1024 * 1024
    static let elements = ["solar", "ember", "frost", "storm", "shadow", "astral"]
    static func fail(_ text: String) -> CompatibilityError { CompatibilityError(message: text) }
    static func integer(_ value: JSONValue?, fallback: Int = 0) throws -> Int {
        guard let value, value != .null else { return fallback }
        guard let number = value.int else { throw fail("정수 필드가 올바르지 않습니다.") }
        return number
    }
    static func string(_ value: JSONValue?, fallback: String? = nil) throws -> String {
        guard let value, value != .null else {
            if let fallback { return fallback }
            throw fail("필수 문자열이 없습니다.")
        }
        guard let result = value.string else { throw fail("문자열 필드가 올바르지 않습니다.") }
        return result
    }
    static func optionalString(_ value: JSONValue?) throws -> String? {
        guard let value, value != .null else { return nil }
        return try string(value)
    }
    static func boolean(_ value: JSONValue?) throws -> Bool {
        guard let value, value != .null else { return false }
        guard let result = value.bool else { throw fail("체크 값이 올바르지 않습니다.") }
        return result
    }
    static func remainder(_ fields: [String: JSONValue], excluding keys: [String]) -> [String: JSONValue] {
        fields.filter { !keys.contains($0.key) }
    }
    static func checkBounds(_ x: Int, _ y: Int, _ width: Int, _ height: Int, maxY: Int = 10_000) throws {
        guard (0...10_000).contains(x), (0...maxY).contains(y),
              width == 0 || (300...1600).contains(width), height == 0 || (180...1400).contains(height) else {
            throw fail("블록 위치 또는 크기가 허용 범위를 벗어났습니다.")
        }
    }
    static func checkPhoto(_ photo: Data) throws {
        guard photo.count <= 2 * 1024 * 1024, photo.starts(with: [255, 216, 255]),
              let source = CGImageSourceCreateWithData(photo as CFData, nil),
              let attributes = CGImageSourceCopyPropertiesAtIndex(source, 0, nil) as? [String: Any],
              let width = attributes[kCGImagePropertyPixelWidth as String] as? Int,
              let height = attributes[kCGImagePropertyPixelHeight as String] as? Int,
              (1...1600).contains(width), (1...1600).contains(height),
              CGImageSourceCreateImageAtIndex(source, 0, nil) != nil else {
            throw fail("Windows 사진은 긴 변 1,600픽셀 이하, 2MB 이하의 JPEG여야 합니다.")
        }
    }
    static func awardedDays(_ fields: [String: JSONValue]) throws -> Set<String> {
        guard let progress = fields["Progress"], progress != .null else { return [] }
        guard let object = progress.object, let array = object["AwardedDays"]?.array, array.count <= 50_000 else { throw fail("성장 기록이 올바르지 않습니다.") }
        let values = try array.map { try string($0) }
        guard Set(values).count == values.count, values.allSatisfy(MacBackup.validDate) else { throw fail("성장 날짜가 잘못되었거나 중복됩니다.") }
        return Set(values)
    }
    static func validateLayouts(_ fields: [String: JSONValue]) throws {
        guard let value = fields["Layouts"], value != .null else { return }
        guard let layouts = value.array, layouts.count <= 50 else { throw fail("저장한 레이아웃은 최대 50개입니다.") }
        var ids = Set<String>(), names = Set<String>()
        for value in layouts {
            guard let layout = value.object else { throw fail("저장 레이아웃 형식이 올바르지 않습니다.") }
            let id = try string(layout["Id"]), name = try string(layout["Name"])
            let mode = try string(layout["Mode"]), style = try string(layout["Style"])
            guard !id.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty, id.utf16.count <= 64, ids.insert(id).inserted,
                  !name.isEmpty, name.utf16.count <= 40, name == name.trimmingCharacters(in: .whitespacesAndNewlines),
                  !name.unicodeScalars.contains(where: CharacterSet.controlCharacters.contains), names.insert(name.lowercased()).inserted,
                  ["free", "cards"].contains(mode), ["plain", "paper", "dots"].contains(style),
                  (0...2).contains(try integer(layout["Theme"])), let blocks = layout["Blocks"]?.array, (1...200).contains(blocks.count) else { throw fail("저장 레이아웃 이름 또는 구성이 올바르지 않습니다.") }
            for value in blocks {
                guard let block = value.object, let kind = block["Kind"]?.string,
                      ["text", "todo", "habit", "emotion", "photo-slot"].contains(kind) else { throw fail("저장 레이아웃 블록이 올바르지 않습니다.") }
                try checkText(block)
                let width = try integer(block["Width"]), height = try integer(block["Height"])
                guard width > 0, height > 0 else { throw fail("저장 레이아웃의 크기가 없습니다.") }
                try checkBounds(try integer(block["X"]), try integer(block["Y"]), width, height, maxY: mode == "cards" ? 282_400 : 10_000)
                _ = try boolean(block["Wide"])
            }
        }
    }
    static func checkText(_ block: [String: JSONValue]) throws {
        guard (try optionalString(block["Title"])?.utf16.count ?? 0) <= 80,
              (try optionalString(block["Prompt"])?.utf16.count ?? 0) <= 300 else { throw fail("블록 제목 또는 질문이 너무 깁니다.") }
    }
    static func validateMetadata(_ metadata: WindowsMetadata?) throws {
        guard let metadata else { return }
        let version = try integer(metadata.fields["Version"], fallback: 1)
        guard (1...4).contains(version) else { throw fail("Windows 기록 버전 1~4만 지원합니다.") }
        try validateLayouts(metadata.fields)
        _ = try awardedDays(metadata.fields)
        if let equipment = metadata.fields["Equipment"], equipment != .null, equipment.object == nil { throw fail("장비 형식이 올바르지 않습니다.") }
        _ = try optionalString(metadata.fields["CharacterStyle"])
    }
    static func decode(_ data: Data, companion existing: CompanionProfile = CompanionProfile()) throws -> MacBackup {
        guard data.count <= maxBytes else { throw fail("백업은 128MB 이하만 가져올 수 있습니다.") }
        guard let root = try JSONDecoder().decode(JSONValue.self, from: data).object else { throw fail("Windows 백업 형식이 아닙니다.") }
        guard let version = root["Version"]?.int, (1...4).contains(version), let rawDays = root["Days"] else { throw fail("Windows 기록 버전 1~4만 지원합니다.") }
        var pairs: [(String, JSONValue)] = []
        if let array = rawDays.array {
            for value in array {
                guard let pair = value.object, let key = pair["Key"]?.string, let entry = pair["Value"] else { throw fail("Windows 날짜 목록이 올바르지 않습니다.") }
                pairs.append((key, entry))
            }
        } else if let object = rawDays.object { pairs = Array(object) }
        else { throw fail("Windows 날짜 목록이 올바르지 않습니다.") }
        guard pairs.count <= 50_000 else { throw fail("기록은 최대 50,000일입니다.") }
        var days: [String: DiaryEntry] = [:]
        var photoCharacters = 0
        for (key, value) in pairs {
            guard MacBackup.validDate(key), days[key] == nil, let object = value.object,
                  let blocks = object["Blocks"]?.array, blocks.count <= 200 else { throw fail("날짜 또는 일기 구성이 올바르지 않습니다.") }
            var day = DiaryEntry()
            day.theme = try integer(object["Theme"])
            day.mood = try string(object["Mood"], fallback: "평온해요")
            day.layoutMode = object["LayoutMode"]?.string == "free" ? "free" : "cards"
            day.pageStyle = ["plain", "paper", "dots"].contains(object["PageStyle"]?.string ?? "") ? object["PageStyle"]!.string! : "plain"
            day.windowsFields = remainder(object, excluding: ["Theme", "Mood", "Blocks", "LayoutMode", "PageStyle"])
            var ids = Set<String>()
            for value in blocks {
                guard let fields = value.object, let kind = fields["Kind"]?.string.flatMap(BlockKind.init(rawValue:)) else { throw fail("알 수 없는 Windows 블록입니다.") }
                let windowsID = try string(fields["Id"])
                guard !windowsID.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty, ids.insert(windowsID).inserted else { throw fail("블록 ID가 비어 있거나 중복됩니다.") }
                var block = DiaryBlock(kind: kind, text: try string(fields["Text"]), checked: try boolean(fields["Checked"]))
                block.windowsID = windowsID
                block.id = UUID() // Windows IDs can be arbitrary strings; keep them separately without UUID collisions.
                block.title = try optionalString(fields["Title"])
                block.prompt = try optionalString(fields["Prompt"])
                block.wide = try boolean(fields["Wide"])
                block.x = try integer(fields["X"]); block.y = try integer(fields["Y"])
                block.width = try integer(fields["Width"]); block.height = try integer(fields["Height"])
                try checkBounds(block.x, block.y, block.width, block.height)
                try checkText(fields)
                guard block.text.utf16.count <= 100_000 else { throw fail("블록 본문은 100,000자까지 지원합니다.") }
                if kind == .photo {
                    let encoded = try string(fields["Photo"])
                    guard encoded.utf8.count <= 2_796_204, let data = Data(base64Encoded: encoded.components(separatedBy: .whitespacesAndNewlines).joined()) else { throw fail("사진 데이터가 올바르지 않습니다.") }
                    try checkPhoto(data)
                    block.photo = data
                    photoCharacters += encoded.utf16.count
                } else if let photo = fields["Photo"], photo != .null { throw fail("사진 데이터는 사진 블록에만 넣을 수 있습니다.") }
                block.windowsFields = remainder(fields, excluding: ["Id", "Kind", "Text", "Checked", "Title", "Prompt", "Wide", "X", "Y", "Width", "Height", "Photo"])
                day.blocks.append(block)
            }
            guard photoCharacters <= 64 * 1024 * 1024 else { throw fail("전체 사진 용량이 Windows 제한을 초과했습니다.") }
            days[key] = day
        }
        let metadata = WindowsMetadata(fields: remainder(root, excluding: ["Days"]))
        try validateMetadata(metadata)
        var profile = existing
        profile.activityDays.formUnion(try awardedDays(root))
        if let gear = root["Equipment"]?.object {
            profile.weapon = equipmentIndex(gear["WeaponId"]?.string, suffix: "sword")
            profile.orb = equipmentIndex(gear["CharmId"]?.string, suffix: "orb")
        }
        let result = MacBackup(days: days, companion: profile, windowsMetadata: metadata)
        try result.validate()
        return result
    }
    static func equipmentIndex(_ id: String?, suffix: String) -> Int {
        guard let id, let index = elements.firstIndex(where: { id == $0 + "-" + suffix }) else { return 0 }
        return index + 1
    }
    static func equipmentID(_ index: Int, suffix: String) -> String {
        (1...6).contains(index) ? elements[index - 1] + "-" + suffix : "none"
    }
    static func encode(_ backup: MacBackup) throws -> Data {
        try backup.validate()
        var root = backup.windowsMetadata?.fields ?? [:]
        root["Version"] = .number(4)
        if root["CharacterStyle"] == nil { root["CharacterStyle"] = .string("original") }
        if root["Layouts"] == nil { root["Layouts"] = .array([]) }
        var progress = root["Progress"]?.object ?? [:]
        let awarded = try awardedDays(root).union(backup.companion.activityDays)
        progress["AwardedDays"] = .array(awarded.sorted().map(JSONValue.string))
        root["Progress"] = .object(progress)
        var gear = root["Equipment"]?.object ?? [:]
        gear["WeaponId"] = .string(equipmentID(backup.companion.weapon, suffix: "sword"))
        gear["CharmId"] = .string(equipmentID(backup.companion.orb, suffix: "orb"))
        root["Equipment"] = .object(gear)
        var totalPhotos = 0
        root["Days"] = .array(try backup.days.keys.sorted().map { key in
            let entry = backup.days[key]!
            var day = entry.windowsFields ?? [:]
            day["Theme"] = .number(Double(entry.theme)); day["Mood"] = .string(entry.mood)
            day["LayoutMode"] = .string(entry.layoutMode); day["PageStyle"] = .string(entry.pageStyle)
            var ids = Set<String>()
            day["Blocks"] = .array(try entry.blocks.map { block in
                var fields = block.windowsFields ?? [:]
                let id = block.windowsID ?? block.id.uuidString.replacingOccurrences(of: "-", with: "").lowercased()
                guard !id.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty, ids.insert(id).inserted else { throw fail("Windows 블록 ID가 중복됩니다.") }
                fields["Id"] = .string(id)
                fields["Kind"] = .string(block.kind == .photo && block.photo == nil ? "photo-slot" : block.kind.rawValue)
                fields["Text"] = .string(block.text); fields["Checked"] = .bool(block.checked)
                fields["Wide"] = .bool(block.wide)
                fields["X"] = .number(Double(block.x)); fields["Y"] = .number(Double(block.y))
                fields["Width"] = .number(Double(block.width)); fields["Height"] = .number(Double(block.height))
                fields["Title"] = block.title.map(JSONValue.string) ?? .null
                fields["Prompt"] = block.prompt.map(JSONValue.string) ?? .null
                guard block.text.utf16.count <= 100_000 else { throw fail("Windows로 내보낼 본문은 블록당 100,000자 이하여야 합니다.") }
                try checkText(fields)
                if let photo = block.photo {
                    guard block.kind == .photo else { throw fail("사진 블록의 종류가 올바르지 않습니다.") }
                    try checkPhoto(photo)
                    let encoded = photo.base64EncodedString()
                    totalPhotos += encoded.utf16.count
                    fields["Photo"] = .string(encoded)
                } else { fields.removeValue(forKey: "Photo") }
                return .object(fields)
            })
            return .object(["Key": .string(key), "Value": .object(day)])
        })
        guard totalPhotos <= 64 * 1024 * 1024 else { throw fail("전체 사진 용량이 Windows 제한을 초과했습니다.") }
        let encoder = JSONEncoder()
        encoder.outputFormatting = [.sortedKeys, .prettyPrinted, .withoutEscapingSlashes]
        let data = try encoder.encode(JSONValue.object(root))
        guard data.count <= maxBytes else { throw fail("Windows 백업은 128MB까지 저장할 수 있습니다.") }
        return data
    }
    static func merge(current: MacBackup, incoming: MacBackup) throws -> MacBackup {
        try current.validate()
        try incoming.validate()
        var days = current.days
        for (key, entry) in incoming.days { days[key] = entry }
        var profile = current.companion
        profile.activityDays.formUnion(incoming.companion.activityDays)
        if incoming.windowsMetadata?.fields["Equipment"]?.object != nil {
            profile.weapon = incoming.companion.weapon
            profile.orb = incoming.companion.orb
        }
        let merged = MacBackup(days: days, companion: profile,
                               windowsMetadata: try mergeMetadata(current.windowsMetadata, incoming.windowsMetadata))
        try merged.validate()
        return merged
    }
    static func mergeMetadata(_ current: WindowsMetadata?, _ incoming: WindowsMetadata?) throws -> WindowsMetadata? {
        guard let incoming else { return current }
        var fields = current?.fields ?? [:]
        for (key, value) in incoming.fields where key != "Layouts" && key != "Progress" { fields[key] = value }
        var layouts = current?.fields["Layouts"]?.array ?? []
        for value in incoming.fields["Layouts"]?.array ?? [] {
            var layout = value.object!
            layouts.removeAll { $0.object?["Id"] == layout["Id"] }
            let originalName = layout["Name"]!.string!
            var name = originalName, suffix = 2
            while layouts.contains(where: { $0.object?["Name"]?.string?.lowercased() == name.lowercased() }) {
                let tail = " (\(suffix))"; suffix += 1
                var prefix = originalName
                while (prefix + tail).utf16.count > 40 { prefix.removeLast() }
                name = prefix + tail
            }
            layout["Name"] = .string(name)
            layouts.append(.object(layout))
        }
        fields["Layouts"] = .array(layouts)
        var progress = incoming.fields["Progress"]?.object ?? current?.fields["Progress"]?.object ?? [:]
        let days = try awardedDays(current?.fields ?? [:]).union(awardedDays(incoming.fields))
        progress["AwardedDays"] = .array(days.sorted().map(JSONValue.string))
        fields["Progress"] = .object(progress)
        fields["Version"] = .number(4)
        let metadata = WindowsMetadata(fields: fields)
        try validateMetadata(metadata)
        return metadata
    }
}

extension DiaryStore {
    func loadWindowsMetadata() {
        do {
            let url = directory.appendingPathComponent("windows-metadata.json")
            let metadata = FileManager.default.fileExists(atPath: url.path)
                ? try JSONDecoder().decode(WindowsMetadata.self, from: Data(contentsOf: url)) : nil
            try WindowsCompatibility.validateMetadata(metadata)
            windowsMetadata = metadata
            metadataLoaded = true
        } catch {
            metadataLoaded = false
            self.error = "Windows 호환 정보를 읽지 못했습니다. windows-metadata.json을 확인해 주세요. \(error.localizedDescription)"
        }
    }
    func exportWindowsBackup() {
        save()
        guard error == nil, loaded, profileLoaded, metadataLoaded else { return }
        do {
            // Validate and serialize before showing a destination or touching a file.
            let data = try WindowsCompatibility.encode(MacBackup(days: allDays(), companion: companion, windowsMetadata: windowsMetadata))
            let panel = NSSavePanel()
            panel.allowedContentTypes = [.json]
            panel.nameFieldStringValue = "MyDay-Windows-backup.json"
            guard panel.runModal() == .OK, let url = panel.url else { return }
            try data.write(to: url, options: .atomic)
        } catch { self.error = "Windows 백업을 내보내지 못했습니다. \(error.localizedDescription)" }
    }
}
