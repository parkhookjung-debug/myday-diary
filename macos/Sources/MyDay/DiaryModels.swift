import Foundation

// Keep unfamiliar Windows fields in backups instead of erasing them during a round trip.
indirect enum JSONValue: Codable, Equatable {
    case object([String: JSONValue]), array([JSONValue]), string(String), number(Double), bool(Bool), null
    init(from decoder: Decoder) throws {
        let value = try decoder.singleValueContainer()
        if value.decodeNil() { self = .null }
        else if let bool = try? value.decode(Bool.self) { self = .bool(bool) }
        else if let number = try? value.decode(Double.self) { self = .number(number) }
        else if let string = try? value.decode(String.self) { self = .string(string) }
        else if let array = try? value.decode([JSONValue].self) { self = .array(array) }
        else { self = .object(try value.decode([String: JSONValue].self)) }
    }
    func encode(to encoder: Encoder) throws {
        var value = encoder.singleValueContainer()
        switch self {
        case .object(let object): try value.encode(object)
        case .array(let array): try value.encode(array)
        case .string(let string): try value.encode(string)
        case .number(let number): try value.encode(number)
        case .bool(let bool): try value.encode(bool)
        case .null: try value.encodeNil()
        }
    }
    var object: [String: JSONValue]? { if case .object(let value) = self { return value }; return nil }
    var array: [JSONValue]? { if case .array(let value) = self { return value }; return nil }
    var string: String? { if case .string(let value) = self { return value }; return nil }
    var int: Int? {
        if case .number(let value) = self, value.isFinite, value >= Double(Int32.min), value <= Double(Int32.max), value.rounded() == value { return Int(value) }
        return nil
    }
    var bool: Bool? { if case .bool(let value) = self { return value }; return nil }
}

struct DiaryBlock: Identifiable, Codable {
    var id = UUID()
    var kind: BlockKind
    var text = ""
    var checked = false
    var photo: Data?
    var title: String?
    var prompt: String?
    var wide = false
    var x = 0
    var y = 0
    var width = 0
    var height = 0
    var windowsID: String?
    var windowsFields: [String: JSONValue]?
    init(kind: BlockKind, text: String = "", checked: Bool = false) {
        self.kind = kind; self.text = text; self.checked = checked
    }
    enum CodingKeys: String, CodingKey { case id, kind, text, checked, photo, title, prompt, wide, x, y, width, height, windowsID, windowsFields }
    init(from decoder: Decoder) throws {
        let c = try decoder.container(keyedBy: CodingKeys.self)
        id = try c.decode(UUID.self, forKey: .id)
        kind = try c.decode(BlockKind.self, forKey: .kind)
        text = try c.decode(String.self, forKey: .text)
        checked = try c.decode(Bool.self, forKey: .checked)
        photo = try c.decodeIfPresent(Data.self, forKey: .photo)
        title = try c.decodeIfPresent(String.self, forKey: .title)
        prompt = try c.decodeIfPresent(String.self, forKey: .prompt)
        wide = try c.decodeIfPresent(Bool.self, forKey: .wide) ?? false
        x = try c.decodeIfPresent(Int.self, forKey: .x) ?? 0
        y = try c.decodeIfPresent(Int.self, forKey: .y) ?? 0
        width = try c.decodeIfPresent(Int.self, forKey: .width) ?? 0
        height = try c.decodeIfPresent(Int.self, forKey: .height) ?? 0
        windowsID = try c.decodeIfPresent(String.self, forKey: .windowsID)
        windowsFields = try c.decodeIfPresent([String: JSONValue].self, forKey: .windowsFields)
    }
}

struct DiaryEntry: Codable {
    var theme = 0
    var character = "🐰"
    var blocks: [DiaryBlock] = []
    var mood = "평온해요"
    var layoutMode = "cards"
    var pageStyle = "plain"
    var windowsFields: [String: JSONValue]?
    init() { }
    enum CodingKeys: String, CodingKey { case theme, character, blocks, mood, layoutMode, pageStyle, windowsFields }
    init(from decoder: Decoder) throws {
        let c = try decoder.container(keyedBy: CodingKeys.self)
        theme = try c.decode(Int.self, forKey: .theme)
        character = try c.decode(String.self, forKey: .character)
        blocks = try c.decode([DiaryBlock].self, forKey: .blocks)
        mood = try c.decodeIfPresent(String.self, forKey: .mood) ?? "평온해요"
        layoutMode = try c.decodeIfPresent(String.self, forKey: .layoutMode) ?? "cards"
        pageStyle = try c.decodeIfPresent(String.self, forKey: .pageStyle) ?? "plain"
        windowsFields = try c.decodeIfPresent([String: JSONValue].self, forKey: .windowsFields)
    }
}
