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
    static let categories = DesktopContract.categories
    static let categoryNames = DesktopContract.categoryNames
    static let layoutNames = Dictionary(uniqueKeysWithValues: zip(DesktopContract.layoutIds, DesktopContract.layoutNames))
    static func categoryName(_ id: String) -> String { categories.firstIndex(of: id).map { categoryNames[$0] } ?? id }
    static func load() throws -> [JournalTemplate] {
        let url = Bundle.main.url(forResource: "templates", withExtension: "json") ?? Bundle.module.url(forResource: "templates", withExtension: "json")
        guard let url else { throw WindowsCompatibility.fail("일기 형식 파일을 찾지 못했습니다.") }
        let templates = try JSONDecoder().decode([JournalTemplate].self, from: Data(contentsOf: url))
        guard templates.count == DesktopContract.templateCount, Set(templates.map(\.id)).count == DesktopContract.templateCount else { throw WindowsCompatibility.fail("일기 형식 목록이 손상되었습니다.") }
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
    @AppStorage("favorite-template-ids") private var favoriteIDs = ""
    @AppStorage("favorite-block-kinds") private var favoriteBlockKinds = ""
    @State private var templates: [JournalTemplate] = []
    @State private var category = "basic"
    @State private var query = ""
    @State private var selected: String?
    @State private var selectedBlock: BlockKind? = .text
    @State private var selectedFavorite: String?
    @State private var failure: String?
    private let basicBlocks: [BlockKind] = [.text,.todo,.habit,.emotion,.photo,.video]
    private var favoriteSet: Set<String> { Set(favoriteIDs.split(separator:",").map(String.init)) }
    private var favoriteBlockSet: Set<BlockKind> { Set(favoriteBlockKinds.split(separator:",").compactMap{BlockKind(rawValue:String($0))}) }
    private var favoriteBlocks:[BlockKind] { blockResults.filter{favoriteBlockSet.contains($0)} }
    private var results: [JournalTemplate] {
        guard category != "basic" else{return []}
        let found=TemplateCatalog.find(templates,category:TemplateCatalog.categories.contains(category) ? category : "",query:query)
        return category == "favorites" ? found.filter{favoriteSet.contains($0.id)} : found
    }
    private var blockResults:[BlockKind] {let q=query.trimmingCharacters(in:.whitespacesAndNewlines);return basicBlocks.filter{q.isEmpty || ($0.title+" "+blockDescription($0)).localizedCaseInsensitiveContains(q)}}
    private func favoriteKey(_ kind:BlockKind)->String{"block:\(kind.rawValue)"}
    private func favoriteKey(_ template:JournalTemplate)->String{"template:\(template.id)"}
    private var current: JournalTemplate? {
        if category=="favorites" {return results.first(where:{favoriteKey($0)==selectedFavorite})}
        return results.first(where: { $0.id == selected }) ?? results.first
    }
    private var currentBlock:BlockKind? {
        if category=="favorites" {return favoriteBlocks.first(where:{favoriteKey($0)==selectedFavorite})}
        guard category=="basic" else{return nil}
        return blockResults.first(where:{$0 == selectedBlock}) ?? blockResults.first
    }
    private func blockName(_ kind:BlockKind)->String {switch kind{case .text:return "글 일기";case .todo:return "할 일";case .habit:return "습관 체크";case .emotion:return "감정처리반";case .photo,.photoSlot:return "사진";case .video:return "영상"}}
    private func blockDescription(_ kind:BlockKind)->String {switch kind{case .text:return "떠오르는 생각과 하루를 자유롭게 기록해요";case .todo:return "오늘 할 일을 적고 완료 여부를 확인해요";case .habit:return "반복하고 싶은 습관을 기록하고 체크해요";case .emotion:return "지금 느끼는 감정을 판단 없이 적어보세요";case .photo,.photoSlot:return "사진과 함께 기억하고 싶은 이야기를 남겨요";case .video:return "영상과 함께 기억하고 싶은 이야기를 남겨요"}}
    private func toggleFavorite(_ id:String){var next=favoriteSet;if !next.insert(id).inserted{next.remove(id)};favoriteIDs=next.sorted().joined(separator:",")}
    private func toggleBlockFavorite(_ kind:BlockKind){var next=favoriteBlockSet;if !next.insert(kind).inserted{next.remove(kind)};favoriteBlockKinds=next.map(\.rawValue).sorted().joined(separator:",")}
    private func resetSelection(){
        if category=="basic"{selected=nil;selectedFavorite=nil;selectedBlock=blockResults.first}
        else if category=="favorites"{selected=nil;selectedBlock=nil;selectedFavorite=favoriteBlocks.first.map(favoriteKey) ?? results.first.map(favoriteKey)}
        else{selectedBlock=nil;selectedFavorite=nil;selected=results.first?.id}
    }
    var body: some View {
        VStack(alignment: .leading, spacing: 16) {
            HStack(alignment:.center) {
                VStack(alignment:.leading,spacing:4){
                    Text("일기 레이아웃 · 블록").font(.title.bold())
                    Text("필요한 블록을 추가하거나 오늘의 기록 방식을 골라보세요.").font(.callout).foregroundStyle(DesktopPalette.muted)
                }
                Spacer()
                Button("닫기") { dismiss() }.buttonStyle(PaperButtonStyle())
            }
            HStack {
                Picker("분류", selection: $category) {
                    Text("기본").tag("basic")
                    Text("즐겨찾기").tag("favorites")
                    Text("모든 레이아웃").tag("")
                    ForEach(TemplateCatalog.categories, id: \.self) { Text(TemplateCatalog.categoryName($0)).tag($0) }
                }.frame(width: 250)
                TextField(category=="basic" ? "블록 검색" : category=="favorites" ? "블록·레이아웃 검색" : "레이아웃·질문 검색", text: $query).textFieldStyle(.roundedBorder)
            }.padding(12).background(DesktopPalette.paper,in:RoundedRectangle(cornerRadius:14)).overlay(RoundedRectangle(cornerRadius:14).stroke(DesktopPalette.soft,lineWidth:1))
            HStack(alignment: .top, spacing: 16) {
                if category=="basic" {
                    List(blockResults,id:\.self,selection:$selectedBlock){kind in
                        HStack(spacing:8){VStack(alignment:.leading,spacing:4){Text(blockName(kind)).font(.headline);Text(blockDescription(kind)).font(.caption).foregroundStyle(.secondary).lineLimit(1)};Spacer();Button{toggleBlockFavorite(kind)}label:{Image(systemName:favoriteBlockSet.contains(kind) ? "star.fill":"star").foregroundStyle(favoriteBlockSet.contains(kind) ? .yellow:.secondary)}.buttonStyle(.borderless).help(favoriteBlockSet.contains(kind) ? "즐겨찾기 해제":"즐겨찾기 추가").accessibilityLabel(favoriteBlockSet.contains(kind) ? "\(blockName(kind)) 즐겨찾기 해제":"\(blockName(kind)) 즐겨찾기 추가")}.padding(.vertical,4).tag(kind)
                    }.frame(width:300).listStyle(.inset).scrollContentBackground(.hidden).background(DesktopPalette.paper,in:RoundedRectangle(cornerRadius:16)).clipShape(RoundedRectangle(cornerRadius:16)).overlay(RoundedRectangle(cornerRadius:16).stroke(DesktopPalette.soft,lineWidth:1))
                } else if category=="favorites" {
                    List(selection:$selectedFavorite){
                        if !favoriteBlocks.isEmpty {Section("기본 블록"){ForEach(favoriteBlocks,id:\.self){kind in
                            HStack(spacing:8){VStack(alignment:.leading,spacing:4){Text(blockName(kind)).font(.headline);Text(blockDescription(kind)).font(.caption).foregroundStyle(.secondary).lineLimit(1)};Spacer();Button{toggleBlockFavorite(kind)}label:{Image(systemName:"star.fill").foregroundStyle(.yellow)}.buttonStyle(.borderless).help("즐겨찾기 해제").accessibilityLabel("\(blockName(kind)) 즐겨찾기 해제")}.padding(.vertical,4).tag(favoriteKey(kind))
                        }}}
                        if !results.isEmpty {Section("레이아웃"){ForEach(results){template in
                            HStack(spacing:8){VStack(alignment:.leading,spacing:4){Text(template.name).font(.headline);Text("\(TemplateCatalog.categoryName(template.category)) · 약 \(template.minutes)분").font(.caption).foregroundStyle(.secondary)};Spacer();Button{toggleFavorite(template.id)}label:{Image(systemName:"star.fill").foregroundStyle(.yellow)}.buttonStyle(.borderless).help("즐겨찾기 해제").accessibilityLabel("\(template.name) 즐겨찾기 해제")}.padding(.vertical,4).tag(favoriteKey(template))
                        }}}
                    }.frame(width:300).listStyle(.inset).scrollContentBackground(.hidden).background(DesktopPalette.paper,in:RoundedRectangle(cornerRadius:16)).clipShape(RoundedRectangle(cornerRadius:16)).overlay(RoundedRectangle(cornerRadius:16).stroke(DesktopPalette.soft,lineWidth:1))
                } else {
                    List(selection:$selected){ForEach(results){template in
                        HStack(spacing:8){VStack(alignment:.leading,spacing:4){Text(template.name).font(.headline);Text("\(TemplateCatalog.categoryName(template.category)) · 약 \(template.minutes)분").font(.caption).foregroundStyle(.secondary)};Spacer();Button{toggleFavorite(template.id)}label:{Image(systemName:favoriteSet.contains(template.id) ? "star.fill":"star").foregroundStyle(favoriteSet.contains(template.id) ? .yellow:.secondary)}.buttonStyle(.borderless).help(favoriteSet.contains(template.id) ? "즐겨찾기 해제":"즐겨찾기 추가").accessibilityLabel(favoriteSet.contains(template.id) ? "\(template.name) 즐겨찾기 해제":"\(template.name) 즐겨찾기 추가")}.padding(.vertical,4).tag(template.id)
                    }}.frame(width:300).listStyle(.inset).scrollContentBackground(.hidden).background(DesktopPalette.paper,in:RoundedRectangle(cornerRadius:16)).clipShape(RoundedRectangle(cornerRadius:16)).overlay(RoundedRectangle(cornerRadius:16).stroke(DesktopPalette.soft,lineWidth:1))
                }
                Group {
                    if let block=currentBlock {
                        VStack(alignment:.leading,spacing:18){Text(blockName(block)).font(.title.bold());Text(blockDescription(block)).foregroundStyle(DesktopPalette.muted);RoundedRectangle(cornerRadius:14).fill(DesktopPalette.friendCard).overlay(Text(blockName(block)).font(.title2.bold()).foregroundStyle(DesktopPalette.ink)).frame(height:180);Text("선택한 블록 하나를 현재 일기 아래에 추가합니다.").font(.caption).foregroundStyle(DesktopPalette.muted);Spacer()}
                    } else if let current {
                        ScrollView {
                            VStack(alignment: .leading, spacing: 16) {
                                Text(current.name).font(.title.bold())
                                Text(current.description).foregroundStyle(DesktopPalette.muted)
                                Text("\(TemplateCatalog.layoutNames[current.layout] ?? current.layout) · \(current.sections.count)개 블록").font(.caption).foregroundStyle(DesktopPalette.accent)
                                TemplateDiagram(template: current).frame(height: 160).padding(10).background(DesktopPalette.sidebar,in:RoundedRectangle(cornerRadius:14))
                                ForEach(Array(current.sections.enumerated()), id: \.offset) { _, section in
                                    VStack(alignment: .leading, spacing: 6) {
                                        Text(section.title).font(.headline)
                                        Text(section.prompt).foregroundStyle(DesktopPalette.muted)
                                    }.padding(14).frame(maxWidth: .infinity, alignment: .leading).background(DesktopPalette.friendCard, in: RoundedRectangle(cornerRadius: 14))
                                }
                            }
                        }
                    } else { Text(category=="favorites" ? "즐겨찾기한 블록이나 레이아웃이 없습니다.":"검색 결과가 없습니다.").foregroundStyle(DesktopPalette.muted).frame(maxWidth:.infinity,maxHeight:.infinity) }
                }.padding(18).frame(maxWidth:.infinity,maxHeight:.infinity,alignment:.topLeading).background(DesktopPalette.paper,in:RoundedRectangle(cornerRadius:16)).overlay(RoundedRectangle(cornerRadius:16).stroke(DesktopPalette.soft,lineWidth:1))
            }.frame(maxHeight:.infinity)
            Text(category=="basic" ? "추가한 블록은 현재 일기와 앞으로의 새 일기 화면에 유지됩니다.":"선택한 형식은 현재 일기에 적용되고 앞으로의 새 일기 화면에도 유지됩니다. 기존에 작성한 날짜는 바꾸지 않습니다.").font(.caption).foregroundStyle(.secondary)
            if let failure { Text(failure).foregroundStyle(.red) }
            Divider()
            HStack {
                Text(category=="basic" ? "\(blockResults.count)개 기본 블록":category=="favorites" ? "\(favoriteBlocks.count + results.count)개 즐겨찾기":"\(results.count)개 레이아웃").foregroundStyle(.secondary)
                Spacer()
                Button(currentBlock != nil ? "이 블록 추가":"이 레이아웃 사용") {
                    if let block=currentBlock{store.addBlock(block)}else if let current{store.applyTemplate(current)}
                    if let error = store.error { failure = error } else { dismiss() }
                }.buttonStyle(.borderedProminent).tint(DesktopPalette.olive).controlSize(.large).disabled((currentBlock==nil && current==nil) || !store.loaded || store.entry.blocks.count>=DesktopContract.maxBlocks)
            }
        }.padding(24).frame(width: 820, height: 690)
        .background(DesktopPalette.sidebar).foregroundStyle(DesktopPalette.ink).tint(DesktopPalette.olive)
        .onAppear {
            do { templates = try TemplateCatalog.load(); resetSelection() }
            catch { failure = error.localizedDescription }
        }
        .onChange(of: query) { _ in resetSelection() }
        .onChange(of: category) { _ in query="";resetSelection() }
        .onChange(of: favoriteIDs){_ in if category=="favorites"{resetSelection()}}
        .onChange(of: favoriteBlockKinds){_ in if category=="favorites"{resetSelection()}}
    }
}

struct TemplateDiagram: View {
    let template: JournalTemplate
    var body: some View {
        GeometryReader { geometry in
            let positions = DiaryLayout.preset(template.layout, count: template.sections.count, width: 800)
            let height = positions.map(\.bottom).max() ?? 1
            let scale: CGFloat = min(geometry.size.width / CGFloat(800), geometry.size.height / CGFloat(height + 12))
            ZStack(alignment: .topLeading) {
                ForEach(positions.indices, id: \.self) { index in
                    TemplateDiagramSection(title: template.sections[index].title, rect: positions[index], scale: scale)
                }
            }
        }
    }
}

private struct TemplateDiagramSection: View {
    let title: String
    let rect: BlockRect
    let scale: CGFloat

    var body: some View {
        let width: CGFloat = CGFloat(rect.width) * scale
        let height: CGFloat = CGFloat(rect.height) * scale
        let x: CGFloat = CGFloat(rect.x) * scale
        let y: CGFloat = CGFloat(rect.y) * scale
        let fontSize: CGFloat = max(CGFloat(10), CGFloat(15) * scale)

        RoundedRectangle(cornerRadius: 8)
            .fill(Color.brown.opacity(0.12))
            .overlay {
                Text(title)
                    .font(.system(size: fontSize))
                    .lineLimit(2)
                    .padding(5)
            }
            .frame(width: width, height: height)
            .offset(x: x, y: y)
    }
}
