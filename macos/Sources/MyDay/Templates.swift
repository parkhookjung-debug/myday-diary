import SwiftUI

struct JournalSection: Codable {
    var kind: BlockKind
    var title: String
    var prompt: String
}
struct JournalTemplate: Identifiable, Codable {
    var id: String
    var name: String
    var description: String
    var style: String
    var category: String
    var minutes: Int
    var layout: String
    var reference: String
    var sections: [JournalSection]
}
enum TemplateCatalog {
    static let categories = ["daily", "reflection", "gratitude", "emotions", "learning", "planning", "wellbeing", "relationships", "creativity", "memories"]
    static let categoryNames = ["일상 기록", "회고·성장", "감사·행복", "마음 정리", "학습·독서", "목표·계획", "생활·루틴", "관계·편지", "창작·취향", "여행·추억"]
    static let layoutNames = ["page": "긴 글", "cards": "질문 카드", "split": "본문 + 메모", "timeline": "타임라인", "letter": "편지", "dashboard": "플래너", "cornell": "코넬 노트", "compare": "나란히 비교"]
    static func categoryName(_ id: String) -> String { categories.firstIndex(of: id).map { categoryNames[$0] } ?? id }
    static func load() throws -> [JournalTemplate] {
        let url = Bundle.main.url(forResource: "templates", withExtension: "json") ?? Bundle.module.url(forResource: "templates", withExtension: "json")
        guard let url else { throw WindowsCompatibility.fail("일기 형식 파일을 찾지 못했습니다.") }
        let templates = try JSONDecoder().decode([JournalTemplate].self, from: Data(contentsOf: url))
        guard templates.count == 100, Set(templates.map(\.id)).count == 100 else { throw WindowsCompatibility.fail("일기 형식 목록이 손상되었습니다.") }
        return templates
    }
    static func find(_ templates: [JournalTemplate], category: String, query: String) -> [JournalTemplate] {
        let query = query.trimmingCharacters(in: .whitespacesAndNewlines)
        return templates.filter { template in
            let content = ([template.name, template.description, categoryName(template.category), layoutNames[template.layout] ?? ""] + template.sections.flatMap { [$0.title, $0.prompt] }).joined(separator: " ")
            return (category.isEmpty || template.category == category) && (query.isEmpty || content.localizedCaseInsensitiveContains(query))
        }
    }
}

struct TemplateGallery: View {
    @ObservedObject var store: DiaryStore
    @Environment(\.dismiss) var dismiss
    @State private var templates: [JournalTemplate] = []
    @State private var category = ""
    @State private var query = ""
    @State private var selected: String?
    @State private var failure: String?
    private var results: [JournalTemplate] { TemplateCatalog.find(templates, category: category, query: query) }
    private var current: JournalTemplate? { results.first(where: { $0.id == selected }) ?? results.first }
    var body: some View {
        VStack(alignment: .leading, spacing: 14) {
            HStack { Text("나에게 맞는 일기 형식 100종").font(.title2.bold()); Spacer(); Button("닫기") { dismiss() } }
            HStack {
                Picker("분야", selection: $category) {
                    Text("모든 분야").tag("")
                    ForEach(TemplateCatalog.categories, id: \.self) { Text(TemplateCatalog.categoryName($0)).tag($0) }
                }.frame(width: 240)
                TextField("제목·질문·배치 검색", text: $query).textFieldStyle(.roundedBorder)
            }
            HStack(alignment: .top, spacing: 20) {
                List(results, selection: $selected) { template in
                    VStack(alignment: .leading, spacing: 4) {
                        Text(template.name).font(.headline)
                        Text("\(TemplateCatalog.categoryName(template.category)) · 약 \(template.minutes)분").font(.caption).foregroundStyle(.secondary)
                    }.padding(.vertical, 4).tag(template.id)
                }.frame(width: 270)
                if let current {
                    ScrollView {
                        VStack(alignment: .leading, spacing: 16) {
                            Text(current.name).font(.title.bold())
                            Text(current.description).foregroundStyle(.secondary)
                            Text("\(TemplateCatalog.layoutNames[current.layout] ?? current.layout) · \(current.sections.count)개 블록").font(.caption)
                            TemplateDiagram(template: current).frame(height: 160)
                            ForEach(Array(current.sections.enumerated()), id: \.offset) { _, section in
                                VStack(alignment: .leading, spacing: 6) {
                                    Text(section.title).font(.headline)
                                    Text(section.prompt).foregroundStyle(.secondary)
                                }.padding(12).frame(maxWidth: .infinity, alignment: .leading).background(.quaternary, in: RoundedRectangle(cornerRadius: 12))
                            }
                        }.padding(4)
                    }.frame(maxWidth: .infinity)
                } else { Text("검색 결과가 없습니다.").frame(maxWidth: .infinity) }
            }
            Text("빈 페이지에는 미리보기 배치를 적용합니다. 기존 기록에는 원래 내용을 유지하고 빈 블록을 추가합니다.").font(.caption).foregroundStyle(.secondary)
            if let failure { Text(failure).foregroundStyle(.red) }
            HStack {
                Text("\(results.count)개 형식").foregroundStyle(.secondary)
                Spacer()
                Button("이 형식 사용하기") {
                    guard let current else { return }
                    store.applyTemplate(current)
                    if let error = store.error { failure = error } else { dismiss() }
                }.buttonStyle(.borderedProminent).disabled(current == nil || !store.loaded)
            }
        }.padding(24).frame(width: 820, height: 650)
        .background(Color(red: 1, green: 0.97, blue: 0.95))
        .onAppear {
            do { templates = try TemplateCatalog.load(); selected = templates.first?.id }
            catch { failure = error.localizedDescription }
        }
        .onChange(of: query) { _ in selected = results.first?.id }
        .onChange(of: category) { _ in selected = results.first?.id }
    }
}

struct TemplateDiagram: View {
    let template: JournalTemplate
    var body: some View {
        GeometryReader { geometry in
            let positions = DiaryLayout.preset(template.layout, count: template.sections.count, width: 800)
            let height = positions.map(\.bottom).max() ?? 1
            let scale = min(geometry.size.width / 800, geometry.size.height / CGFloat(height + 12))
            ZStack(alignment: .topLeading) {
                ForEach(Array(positions.enumerated()), id: \.offset) { index, rect in
                    RoundedRectangle(cornerRadius: 8).fill(Color.brown.opacity(0.12))
                        .overlay(Text(template.sections[index].title).font(.system(size: max(10, 15 * scale))).lineLimit(2).padding(5))
                        .frame(width: CGFloat(rect.width) * scale, height: CGFloat(rect.height) * scale)
                        .offset(x: CGFloat(rect.x) * scale, y: CGFloat(rect.y) * scale)
                }
            }
        }
    }
}
