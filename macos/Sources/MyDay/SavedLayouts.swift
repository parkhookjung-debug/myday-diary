import SwiftUI

@MainActor
extension DiaryStore {
    var savedLayouts: [[String: JSONValue]] { windowsMetadata?.fields["Layouts"]?.array?.compactMap(\.object) ?? [] }
    func saveLayout(_ name: String, rename id: String? = nil) {
        save(); guard error == nil else { return }
        commitShared { s in
            let name = name.trimmingCharacters(in: .whitespacesAndNewlines)
            var list = s.fields["Layouts"]?.array?.compactMap(\.object) ?? []
            guard !name.isEmpty, name.utf16.count <= 40, !list.contains(where: { $0["Id"]?.string != id && $0["Name"]?.string?.lowercased() == name.lowercased() }) else { throw WindowsCompatibility.fail("중복되지 않는 이름을 40자 이내로 입력해 주세요.") }
            if let id {
                guard let index = list.firstIndex(where: { $0["Id"]?.string == id }) else { throw WindowsCompatibility.fail("이 레이아웃은 더 이상 없어요.") }
                list[index]["Name"] = .string(name)
            }
            else {
                guard !entry.blocks.isEmpty, list.count < 50 else { throw WindowsCompatibility.fail("빈 일기는 배치로 저장할 수 없고 최대 50개까지 보관해요.") }
                let positions = entry.layoutMode == "free" ? entry.blocks.map(DiaryLayout.bounds) : DiaryLayout.autoArrange(entry.blocks, width: 900)
                let blocks: [JSONValue] = entry.blocks.enumerated().map { index, b in
                    let r = positions[index]
                    return .object(["Kind": .string(b.kind == .photo ? "photo-slot" : b.kind == .video ? "text" : b.kind.rawValue), "Title": b.title.map(JSONValue.string) ?? .null, "Prompt": b.prompt.map(JSONValue.string) ?? .null, "Wide": .bool(b.wide), "X": .number(Double(r.x)), "Y": .number(Double(r.y)), "Width": .number(Double(r.width)), "Height": .number(Double(r.height))])
                }
                list.append(["Id": .string(UUID().uuidString.lowercased()), "Name": .string(name), "Mode": .string(entry.layoutMode), "Style": .string(entry.pageStyle), "Theme": .number(Double(entry.theme)), "Blocks": .array(blocks)])
            }
            s.fields["Layouts"] = .array(list.map(JSONValue.object))
        }
    }
    func deleteLayout(_ id: String) { save(); guard error == nil else { return }; commitShared { s in s.fields["Layouts"] = .array((s.fields["Layouts"]?.array ?? []).filter { $0.object?["Id"]?.string != id }) } }
    func applySavedLayout(_ layout: [String: JSONValue]) {
        do {
            try WindowsCompatibility.validateLayouts(["Layouts": .array([.object(layout)])])
            let raw = layout["Blocks"]!.array!
            guard raw.count + entry.blocks.count <= 200 else { throw WindowsCompatibility.fail("하루 최대 200개 블록을 넘습니다.") }
            var next = entry
            let empty = next.blocks.isEmpty
            if empty { next.layoutMode = layout["Mode"]!.string!; next.theme = layout["Theme"]!.int!; next.pageStyle = layout["Style"]!.string! }
            let offset = empty ? 0 : (next.blocks.map { DiaryLayout.bounds($0).bottom }.max() ?? 0) + 16
            for value in raw {
                let f = value.object!
                var b = DiaryBlock(kind: BlockKind(rawValue: f["Kind"]!.string!)!)
                b.title = f["Title"]?.string; b.prompt = f["Prompt"]?.string; b.wide = f["Wide"]?.bool ?? false
                b.x = f["X"]!.int!; b.y = f["Y"]!.int! + offset; b.width = f["Width"]!.int!; b.height = f["Height"]!.int!
                if next.layoutMode == "free" { try WindowsCompatibility.checkBounds(b.x, b.y, b.width, b.height) }
                else { b.x = 0; b.y = 0; b.width = 0 }
                next.blocks.append(b)
            }
            update { $0 = next }
        } catch { self.error = error.localizedDescription }
    }
}

struct SavedLayoutsView: View {
    @ObservedObject var store: DiaryStore
    @Environment(\.dismiss) var dismiss
    @State private var name = ""
    @State private var query = ""
    @State private var renaming: String?
    @State private var deleting: String?
    var body: some View {
        VStack(alignment: .leading, spacing: 16) {
            HStack { Text("내 레이아웃").font(.title); Spacer(); Button("닫기") { dismiss() } }
            Text("글·사진·체크 상태를 제외한 빈 배치만 저장해요. 기존 기록에 적용하면 아래에 추가돼요.").font(.caption)
            HStack { TextField("배치 이름", text: $name); Button(renaming == nil ? "현재 배치 저장" : "이름 변경") { store.saveLayout(name, rename: renaming); if store.error == nil { name = ""; renaming = nil } }; if renaming != nil { Button("취소") { renaming = nil; name = "" } } }
            TextField("이름 검색", text: $query)
            if let error = store.error { Text(error).foregroundStyle(.red) }
            List(Array(store.savedLayouts.filter { query.isEmpty || ($0["Name"]?.string ?? "").localizedCaseInsensitiveContains(query) }.enumerated()), id: \.offset) { _, layout in
                HStack {
                    VStack(alignment: .leading) { Text(layout["Name"]!.string!); Text("\(layout["Blocks"]!.array!.count)개 블록 · \(layout["Mode"]!.string == "free" ? "자유 배치" : "자동 정렬")").font(.caption) }
                    Spacer()
                    Button("적용") { store.applySavedLayout(layout); if store.error == nil { dismiss() } }
                    Button("이름") { renaming = layout["Id"]!.string; name = layout["Name"]!.string! }
                    Button("삭제") { deleting = layout["Id"]!.string }
                }
            }
        }.padding(24).frame(width: 670, height: 550).textFieldStyle(.roundedBorder)
            .alert("이 레이아웃을 삭제할까요?", isPresented: Binding(get: { deleting != nil }, set: { if !$0 { deleting = nil } })) { Button("취소", role: .cancel) { deleting = nil }; Button("삭제", role: .destructive) { if let id = deleting { store.deleteLayout(id) }; deleting = nil } } message: { Text("이미 작성한 일기는 그대로 유지됩니다.") }
    }
}
