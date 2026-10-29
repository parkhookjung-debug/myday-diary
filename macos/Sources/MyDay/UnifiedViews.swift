import SwiftUI
import AppKit

struct PaperButtonStyle: ButtonStyle {
    func makeBody(configuration: Configuration) -> some View {
        configuration.label.padding(.horizontal, 12).padding(.vertical, 8).foregroundStyle(DesktopPalette.olive)
            .background(configuration.isPressed ? DesktopPalette.selection : DesktopPalette.paper, in: RoundedRectangle(cornerRadius: 10))
            .overlay(RoundedRectangle(cornerRadius: 10).stroke(DesktopPalette.soft, lineWidth: 1))
    }
}

enum SangmonImages {
    static let cache = NSCache<NSString, NSImage>()
    static func image(_ path: String) -> NSImage? {
        if let found = cache.object(forKey: path as NSString) { return found }
        guard let image = NSImage(contentsOf: SharedArtCatalog.artURL.appendingPathComponent(path)) else { return nil }
        cache.totalCostLimit = 48 * 1024 * 1024
        cache.setObject(image, forKey: path as NSString, cost: Int(image.size.width * image.size.height * 4))
        return image
    }
}

struct SharedSangmonView: View {
    var companion: SharedCompanion
    var reactionTick=0
    var sleepy=false
    @State private var fireAt = Date.distantPast
    @State private var hopAt=Date.distantPast
    var body: some View {
        TimelineView(.animation(minimumInterval: 1.0 / 24)) { timeline in
            Canvas { context, size in
                let time = timeline.date.timeIntervalSinceReferenceDate
                let age = timeline.date.timeIntervalSince(fireAt)
                let phase = time.truncatingRemainder(dividingBy: 42)
                let row = age < 0.7 ? 2 : sleepy ? 6 : (30..<31.6).contains(phase) ? 3 : (34..<36.4).contains(phase) ? 6 : 0
                let frame = row == 2 ? min(6, Int(age * 10)) : row == 3 ? Int((phase - 30) * 5) : Int(time * (row == 6 ? 3.33 : 1.9)) % 8
                let hopAge=timeline.date.timeIntervalSince(hopAt)
                let offset:CGFloat=hopAge<0.9 ? CGFloat(-abs(sin(hopAge*Double.pi*2/0.9))*9) : 0
                let side = min(size.width, size.height), bounds = CGRect(x: (size.width - side) / 2, y: (size.height - side) / 2+offset, width: side, height: side)
                func overlay(_ slot: String) {
                    let id = companion.item(slot)
                    if id != "none", let image = SangmonImages.image("equipment/\(id).png") { context.draw(Image(nsImage: image), in: bounds) }
                }
                for slot in ["ground", "aura", "back"] { overlay(slot) }
                if let atlas = SangmonImages.image("sangmon/\(companion.style).png"), let cg = atlas.cgImage(forProposedRect: nil, context: nil, hints: nil), let cell = cg.cropping(to: CGRect(x: frame * 192, y: row * 192, width: 192, height: 192)) {
                    context.draw(Image(decorative: cell, scale: 1), in: bounds)
                }
                for slot in ["neck", "head", "face", "weapon", "charm"] { overlay(slot) }
            }
        }.contentShape(Rectangle()).onTapGesture { fireAt = Date() }
            .onChange(of: reactionTick) { _ in hopAt=Date() }
            .accessibilityLabel("상몬 \(companion.style), 레벨 \(companion.level). 누르면 불꽃 반응")
    }
}

struct DiaryView: View {
    @ObservedObject var store: DiaryStore
    @State private var tab = 0
    @State private var tools=false
    @State private var studio=false
    @State private var welcome=false
    @State private var boardEditing=false
    @State private var boardZoom=0.9
    @StateObject private var lounge = LoungeModel()
    var body: some View {
        GeometryReader { geometry in
            HStack(spacing: 0) {
                sidebar.frame(width:geometry.size.width < 850 ? 200:222)
                Group {
                    switch tab {
                    case 2: CompanionContent(store: store)
                    case 3: LoungeView(model: lounge)
                    case 4: settings
                    default: diary(showFriend:geometry.size.width < 1000)
                    }
                }.frame(maxWidth: .infinity, maxHeight: .infinity)
            }.background(DesktopPalette.backgrounds[store.entry.theme]).foregroundStyle(DesktopPalette.ink)
        }.preferredColorScheme(.light).tint(DesktopPalette.olive).buttonStyle(PaperButtonStyle())
            .sheet(isPresented: $store.showingSearch) { JournalSearch(store: store) }
            .sheet(isPresented: $store.showingCompanion) { CompanionWardrobe(store: store) }
            .sheet(isPresented: $store.showingTemplates) { TemplateGallery(store: store) }
            .sheet(isPresented: $store.showingLayouts) { SavedLayoutsView(store: store) }
            .onAppear{if (try? store.allDays().isEmpty)==true && !FileManager.default.fileExists(atPath:store.directory.appendingPathComponent("welcome-complete.txt").path){welcome=true}}
            .sheet(isPresented:$welcome,onDismiss:{do{try FileManager.default.createDirectory(at:store.directory,withIntermediateDirectories:true);try Data("1".utf8).write(to:store.directory.appendingPathComponent("welcome-complete.txt"),options:.atomic)}catch{store.error=error.localizedDescription}}){ExperienceView(store:store,action:{i in tab=0;if i==0{store.addBlock(.text)}else if i==1{store.addBlock(.photo)}else{DispatchQueue.main.asyncAfter(deadline:.now()+0.25){studio=true}}})}
            .sheet(isPresented:$studio){StudioView(store:store,onOpen:{tab=0})}
            .sheet(isPresented: $tools) { JournalToolsView(store: store,onOpenDay:{_ in tab=0}) }
            .onDisappear { Task { await lounge.disconnect() } }
    }
    private var sidebar: some View {
        let icons=["book.closed","calendar","sparkles","bubble.left.and.bubble.right","gearshape"]
        return VStack(alignment:.leading,spacing:0){
            HStack(alignment:.top,spacing:8){
                Image(systemName:"book").font(.system(size:38,weight:.light)).frame(width:46,height:50)
                VStack(alignment:.leading,spacing:1){Text("my day").font(.system(size:37,weight:.bold,design:.serif)).tracking(-0.8).lineLimit(1).minimumScaleFactor(0.88);Text(AppShell.tagline).font(.system(size:14)).foregroundStyle(DesktopPalette.muted).lineLimit(1).minimumScaleFactor(0.8)}.layoutPriority(1)
            }.foregroundStyle(DesktopPalette.olive)
            Text("MY SPACE").font(.caption2.weight(.semibold)).tracking(1.2).foregroundStyle(DesktopPalette.muted).padding(.top,32).padding(.bottom,12)
            ForEach(0..<5){i in
                Button{
                    store.save();guard store.error==nil else{return}
                    if i==1{store.showingSearch=true}else{tab=i}
                }label:{
                    HStack(spacing:12){Image(systemName:icons[i]).font(.system(size:16,weight:.medium)).frame(width:22);Text(AppShell.titles[i]).font(.body.weight(tab==i ? .semibold:.regular));Spacer()}
                    .foregroundStyle(tab==i ? DesktopPalette.olive:DesktopPalette.ink).padding(.horizontal,12).frame(height:46)
                }.buttonStyle(.plain).background(tab==i ? DesktopPalette.selection:.clear,in:RoundedRectangle(cornerRadius:12))
            }
            Spacer(minLength:24)
            Divider().padding(.bottom,16)
            Text(store.date.formatted(.dateTime.year().month().day())).font(.callout).foregroundStyle(DesktopPalette.muted)
            Button{store.select(Date());if store.error==nil{tab=0}}label:{HStack{Image(systemName:"arrow.up.right");Text("오늘");Spacer()}}.padding(.top,8)
        }.padding(18).background(DesktopPalette.sidebar).overlay(alignment:.trailing){Rectangle().fill(DesktopPalette.soft).frame(width:1)}
    }
    private func diary(showFriend: Bool) -> some View {
        ScrollView {
            VStack(alignment:.leading,spacing:14){
                ZStack(alignment:.topTrailing){
                    VStack(spacing:7){
                        Text(AppShell.greeting).font(.system(size:36,weight:.medium,design:.serif))
                        Text(AppShell.subtitle).font(.callout).foregroundStyle(DesktopPalette.muted)
                    }.frame(maxWidth:.infinity).padding(.top,8)
                    Label("기기에 자동 저장",systemImage:"checkmark").font(.caption).foregroundStyle(DesktopPalette.muted)
                }
                HStack(spacing:10){
                    Button("‹ 이전"){shift(-1)};Button("오늘"){store.select(Date())};Button("다음 ›"){shift(1)}
                    Spacer()
                    Label(store.date.formatted(.dateTime.year().month().day().weekday(.wide)),systemImage:"calendar").font(.callout.weight(.medium)).lineLimit(1).fixedSize()
                    Spacer()
                    Button{store.showingTemplates=true}label:{Label("일기 형식 · 블록 추가",systemImage:"plus")}
                    Button{store.showingLayouts=true}label:{Label("내 레이아웃",systemImage:"square.grid.2x2")}
                }
                errorBanner
                let mood=Binding(get:{store.entry.mood},set:{v in store.update{$0.mood=v}})
                MoodStickerDashboard(mood:mood)
                if showFriend{VStack(spacing:12){friendSummary;boardTools;practiceSummary}}
                else{HStack(alignment:.top,spacing:12){VStack(spacing:12){friendSummary;boardTools};practiceSummary.frame(width:360)}}
                VStack(alignment:.leading,spacing:12){
                    Label("오늘의 기록",systemImage:"pencil").font(.headline).foregroundStyle(DesktopPalette.olive)
                    DiaryBoard(store:store,editingState:$boardEditing,zoomState:$boardZoom,showsControls:false)
                }.padding(16).background(DesktopPalette.paper.opacity(0.82),in:RoundedRectangle(cornerRadius:16)).overlay(RoundedRectangle(cornerRadius:16).stroke(DesktopPalette.soft,lineWidth:1))
            }.padding(20)
        }
    }
    private func shift(_ amount: Int) { if let d = Calendar.current.date(byAdding: .day, value: amount, to: store.date) { store.select(d) } }
    private var boardTools:some View{DiaryBoardTools(store:store,editing:$boardEditing,zoom:$boardZoom).padding(14).background(DesktopPalette.paper.opacity(0.82),in:RoundedRectangle(cornerRadius:16)).overlay(RoundedRectangle(cornerRadius:16).stroke(DesktopPalette.soft,lineWidth:1))}
    private var friendSummary:some View{
        VStack(alignment:.leading,spacing:4){
            Label("YOUR LITTLE FRIEND",systemImage:"leaf.fill").font(.caption).foregroundStyle(DesktopPalette.olive).lineLimit(1)
            HStack(spacing:16){
                SharedSangmonView(companion:store.sharedCompanion,reactionTick:store.reactionTick).frame(width:130,height:125)
                VStack(alignment:.leading,spacing:9){Text(AppShell.friendTitle).font(.system(size:20,design:.serif)).lineLimit(1).minimumScaleFactor(0.8);Text("Lv.\(store.sharedCompanion.level) · \(store.sharedCompanion.xp) XP").font(.callout);if !store.reaction.isEmpty{Text(store.reaction).font(.caption).foregroundStyle(DesktopPalette.olive)}}
                Spacer();Button{tab=2}label:{HStack(spacing:5){Text("상몬·공방 열기").lineLimit(1);Image(systemName:"arrow.up.right")}}.fixedSize()
            }
        }.padding(18).frame(maxWidth:.infinity,minHeight:180,alignment:.leading).background(DesktopPalette.friendCard,in:RoundedRectangle(cornerRadius:16)).overlay(RoundedRectangle(cornerRadius:16).stroke(DesktopPalette.soft,lineWidth:1))
    }
    private var practiceSummary:some View{
        VStack(alignment:.leading,spacing:13){
            Label("오늘의 작은 실천",systemImage:"flag.fill").font(.headline).foregroundStyle(DesktopPalette.olive)
            ForEach([BlockKind.todo,.habit],id:\.rawValue){kind in
                let tasks=store.entry.blocks.filter{$0.kind==kind};let done=tasks.filter(\.checked).count
                HStack{Text(kind == .todo ? "할 일":"습관").font(.callout);Spacer();Text("\(done) / \(tasks.count)").font(.callout).monospacedDigit()}
                GeometryReader{geometry in ZStack(alignment:.leading){Capsule().fill(DesktopPalette.soft);Capsule().fill(DesktopPalette.progress).frame(width:max(6,geometry.size.width*CGFloat(done)/CGFloat(max(1,tasks.count))))}}.frame(height:7)
            }
            if store.entry.blocks.allSatisfy({$0.kind != .habit}){Text("등록된 습관이 없어요.").font(.caption).foregroundStyle(DesktopPalette.muted)}
        }.padding(18).frame(maxWidth:520,minHeight:180,alignment:.topLeading).background(DesktopPalette.paper.opacity(0.82),in:RoundedRectangle(cornerRadius:16)).overlay(RoundedRectangle(cornerRadius:16).stroke(DesktopPalette.soft,lineWidth:1))
    }
    private var errorBanner: some View {
        Group { if let error = store.error { VStack(alignment: .leading) { Text(error).foregroundStyle(.red); Button(store.loaded ? "다시 저장" : "다시 불러오기") { store.retrySaveOrLoad() } }.padding(12).background(DesktopPalette.tint, in: RoundedRectangle(cornerRadius: 12)) } }
    }
    private var settings: some View {
        ScrollView{VStack(alignment:.leading,spacing:20){
            VStack(alignment:.leading,spacing:5){Text("설정 · 백업").font(.largeTitle.bold());Text("내 기록을 안전하게, 내 공간을 나답게").foregroundStyle(DesktopPalette.muted)}
            errorBanner
            VStack(alignment:.leading,spacing:16){
                settingsHeading(icon:"externaldrive",title:"백업 및 복원",subtitle:"소중한 하루를 안전하게 보관하세요.")
                HStack(spacing:12){
                    Button{store.exportWindowsBackup()}label:{Label("백업 다운로드",systemImage:"arrow.down.to.line").padding(.horizontal,10).frame(height:38).foregroundStyle(.white).background(DesktopPalette.olive,in:RoundedRectangle(cornerRadius:10))}.buttonStyle(.plain)
                    Button{store.importBackup()}label:{Label("백업 복원",systemImage:"arrow.up.to.line")}.controlSize(.large)
                }
                HStack(spacing:16){Button("Mac 전용 백업"){store.exportBackup()};Button("저장 폴더 열기"){NSWorkspace.shared.open(store.directory)}}.font(.caption)
                Text("Windows·Mac·Android 사이에서 JSON 파일로 옮겨요. 자동 동기화는 하지 않아요.").font(.caption).foregroundStyle(DesktopPalette.muted)
            }.padding(20).frame(maxWidth:.infinity,alignment:.leading).background(DesktopPalette.paper,in:RoundedRectangle(cornerRadius:16)).overlay(RoundedRectangle(cornerRadius:16).stroke(DesktopPalette.soft,lineWidth:1))
            VStack(spacing:0){
                settingsRow(icon:"photo.on.rectangle.angled",title:"꾸미기",subtitle:"사진 · 방 · 태그 · 잠금"){store.save();if store.error==nil{studio=true}}
                Divider().padding(.leading,64)
                settingsRow(icon:"doc.text",title:"기록 관리",subtitle:"복구 · 리포트 · 알림 · 기기 이동"){store.save();if store.error==nil{tools=true}}
                Divider().padding(.leading,64)
                HStack(spacing:14){Image(systemName:"person.2").font(.title3).foregroundStyle(DesktopPalette.olive).frame(width:44,height:44).background(DesktopPalette.friendCard,in:Circle());VStack(alignment:.leading,spacing:3){Text("바탕화면 친구").font(.headline);Text("친구 표시 및 관리").font(.callout).foregroundStyle(DesktopPalette.muted)};Spacer();Button("표시"){DesktopCompanion.shared.show(store:store)};Button("숨기기"){DesktopCompanion.shared.hide()}}.padding(.horizontal,18).frame(minHeight:72)
                Divider().padding(.leading,64)
                settingsRow(icon:"questionmark.circle",title:"사용 안내",subtitle:"처음 시작하기와 주요 기능 보기"){welcome=true}
            }.background(DesktopPalette.paper,in:RoundedRectangle(cornerRadius:16)).overlay(RoundedRectangle(cornerRadius:16).stroke(DesktopPalette.soft,lineWidth:1))
            VStack(alignment:.leading,spacing:12){settingsHeading(icon:"paintpalette",title:"오늘의 분위기",subtitle:"일기 화면의 배경을 골라보세요.");Picker("배경",selection:Binding(get:{store.entry.theme},set:{v in store.update{$0.theme=v}})){Text("크림").tag(0);Text("숲").tag(1);Text("라벤더").tag(2)}.pickerStyle(.segmented).labelsHidden()}.padding(20).frame(maxWidth:.infinity,alignment:.leading).background(DesktopPalette.paper,in:RoundedRectangle(cornerRadius:16)).overlay(RoundedRectangle(cornerRadius:16).stroke(DesktopPalette.soft,lineWidth:1))
        }.padding(32).frame(maxWidth:900,alignment:.leading)}
    }
    private func settingsHeading(icon:String,title:String,subtitle:String)->some View{HStack(spacing:14){Image(systemName:icon).font(.title2).foregroundStyle(DesktopPalette.olive).frame(width:48,height:48).background(DesktopPalette.friendCard,in:Circle());VStack(alignment:.leading,spacing:4){Text(title).font(.headline);Text(subtitle).font(.callout).foregroundStyle(DesktopPalette.muted)}}}
    private func settingsRowLabel(icon:String,title:String,subtitle:String)->some View{HStack(spacing:14){Image(systemName:icon).font(.title3).foregroundStyle(DesktopPalette.olive).frame(width:44,height:44).background(DesktopPalette.friendCard,in:Circle());VStack(alignment:.leading,spacing:3){Text(title).font(.headline);Text(subtitle).font(.callout).foregroundStyle(DesktopPalette.muted)};Spacer();Image(systemName:"chevron.right").foregroundStyle(DesktopPalette.muted)}.padding(.horizontal,18).frame(minHeight:72).contentShape(Rectangle())}
    private func settingsRow(icon:String,title:String,subtitle:String,action:@escaping()->Void)->some View{Button(action:action){settingsRowLabel(icon:icon,title:title,subtitle:subtitle)}.buttonStyle(.plain)}
}

struct CompanionWardrobe: View {
    @ObservedObject var store: DiaryStore
    @Environment(\.dismiss) var dismiss
    var body: some View { VStack { HStack { Text(AppShell.titles[2]).font(.title); Spacer(); Button("닫기") { dismiss() } }.padding(); CompanionContent(store: store) }.frame(width: 780, height: 700) }
}

struct CompanionContent: View {
    @ObservedObject var store: DiaryStore
    @State private var tab = 0
    @State private var query = ""
    @State private var slot = "weapon"
    @State private var result = ""
    private var s: SharedCompanion { store.sharedCompanion }
    private var owned: SharedCollectionState { (try? s.state()) ?? SharedCollectionState() }
    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 18) {
                HStack {
                    SharedSangmonView(companion: s).frame(width: 130, height: 140)
                    VStack(alignment: .leading, spacing: 8) {
                        Text("Lv.\(s.level) · 나의 작은 친구").font(.title2)
                        Text("총 \(s.xp) XP · 만렙 100 · 3,590 XP").font(.caption)
                        ProgressView(value: Double(s.intoLevel), total: Double(s.neededXP)).tint(DesktopPalette.progress)
                        Text(s.level == 100 ? "만렙 달성!" : "다음 레벨까지 \(s.neededXP - s.intoLevel) XP").font(.caption)
                        Button(s.activities[DiaryStore.todayKey(), default: 0] & 1 != 0 ? "출석 완료" : "출석 +5 XP") { store.checkIn() }.disabled(s.activities[DiaryStore.todayKey(), default: 0] & 1 != 0)
                    }
                }.padding(16).background(DesktopPalette.friendCard, in: RoundedRectangle(cornerRadius: 20))
                Text("출석 5 · 첫 기록 10 · 글 20자 10 · 사진 5 · 할 일 5 · 습관 5 XP").font(.caption)
                Text("연속 7일 출석 +10 XP · 매일 루틴 약 100일 / 주 5일 약 200일 / 여유롭게 약 300일").font(.caption).foregroundStyle(.secondary)
                if let error = store.error { Text(error).foregroundStyle(.red); Button("다시 저장·불러오기") { store.retrySaveOrLoad() } }
                Picker("상몬 메뉴", selection: $tab) { Text("상몬 64종").tag(0); Text("소품 300종").tag(1); Text("소품 뽑기").tag(2); Text("조각 합성·성취").tag(3) }.pickerStyle(.segmented)
                if tab < 2 { TextField("이름·종류 검색", text: $query).textFieldStyle(.roundedBorder) }
                if tab == 0 { skins }
                else if tab == 1 { wardrobe }
                else if tab == 2 { gifts }
                else { atelier }
            }.padding(24)
        }.tint(DesktopPalette.olive).buttonStyle(PaperButtonStyle())
    }
    private var skins: some View {
        LazyVGrid(columns: [GridItem(.adaptive(minimum: 130))], spacing: 12) {
            ForEach(SharedArtCatalog.shared.characters.filter { query.isEmpty || $0.name.localizedCaseInsensitiveContains(query) }) { skin in
                Button { store.selectSkin(skin.id) } label: { VStack {
                    art("portraits/\(skin.id).png", height: 96)
                    Text(skin.name).font(.caption)
                    Text(s.xp >= skin.xp ? (s.style == skin.id ? "함께하는 중" : "선택하기") : "총 \(skin.xp) XP에 해금").font(.caption2)
                }.frame(maxWidth: .infinity).padding(10).background(s.style == skin.id ? DesktopPalette.selection : DesktopPalette.paper, in: RoundedRectangle(cornerRadius: 16)) }.buttonStyle(.plain).disabled(s.xp < skin.xp)
            }
        }
    }
    private var wardrobe: some View {
        VStack(alignment: .leading, spacing: 12) {
            Picker("꾸미는 위치", selection: $slot) { ForEach(Array(SharedArtCatalog.slots.enumerated()), id: \.element) { i, value in Text(SharedArtCatalog.slotNames[i]).tag(value) } }
            Button("이 위치 소품 해제") { store.selectItem("none", slot: slot) }
            LazyVGrid(columns: [GridItem(.adaptive(minimum: 125))], spacing: 12) {
                ForEach(SharedArtCatalog.shared.items.filter { $0.slot == slot && (query.isEmpty || ($0.name + $0.description).localizedCaseInsensitiveContains(query)) }) { item in
                    Button { store.selectItem(item.id, slot: item.slot) } label: { VStack {
                        art("items/\(item.id).png", height: 94)
                        Text(item.name).font(.caption); Text(CollectionPolicy.names[SharedArtCatalog.grade(item.id)] + (owned.owned.contains(item.id) ? " · 보유" : " · 미수집")).font(.caption2)
                    }.frame(maxWidth: .infinity).padding(10).background(s.item(slot) == item.id ? DesktopPalette.selection : DesktopPalette.paper, in: RoundedRectangle(cornerRadius: 16)) }.buttonStyle(.plain).disabled(!owned.owned.contains(item.id))
                }
            }
        }
    }
    private var gifts: some View {
        VStack(alignment: .leading, spacing: 16) {
            Text("클로버 \(owned.coins) · \(owned.coins/20)회 가능 · 꽃잎 \(owned.petals) · 수집 \(owned.owned.count)/300").font(.headline)
            if owned.coins<20 {Text("다음 선물까지 클로버 \(20-owned.coins)개 · 출석하거나 일기를 남겨 보세요.").font(.caption)}
            Text((0...3).map { "\(CollectionPolicy.names[$0]) \(Double(CollectionPolicy.weights[$0]) / 100)%" }.joined(separator: " / ")).font(.caption)
            Text("10회마다 미수집 소품 보장 · 중복은 등급별 꽃잎 · 뽑을 때 해당 등급 조각 +1").font(.caption)
            Button("선물 열기 · 클로버 20") {
                store.collect("draw-tiered")
                if store.error == nil, let id = store.sharedCompanion.actions.last?["ItemId"]?.string { result = SharedArtCatalog.shared.items.first { $0.id == id }?.name ?? id }
            }.buttonStyle(.borderedProminent).disabled(owned.coins < 20)
            if !result.isEmpty { Text("선물 도착! \(result)").font(.title3).padding().background(DesktopPalette.selection, in: RoundedRectangle(cornerRadius: 16)) }
            Text("꽃잎으로 원하는 소품 고르기").font(.headline)
            exchangeItems(craft: false)
        }
    }
    private var atelier: some View {
        VStack(alignment: .leading, spacing: 16) {
            Text("나의 조각").font(.headline)
            ForEach(0..<4) { g in Text("\(CollectionPolicy.names[g]) 조각 · \(owned.materials[g])개") }
            ForEach(0..<3) { g in
                HStack { VStack(alignment: .leading) { Text("\(CollectionPolicy.names[g]) → \(CollectionPolicy.names[g+1])"); Text("조각 \(CollectionPolicy.synthesisCost)개 · 성공 \(CollectionPolicy.synthesisPercent[g])% · 실패 시 \(CollectionPolicy.failureRefund)개 반환").font(.caption) }
                    Spacer(); Button("합성") { store.collect("synth", grade: g) }.disabled(owned.materials[g] < CollectionPolicy.synthesisCost) }
            }
            Text("같은 등급 조각 \(CollectionPolicy.craftCost)개로 소품 제작").font(.headline)
            exchangeItems(craft: true)
            Text("수집 성취 도감 · \(owned.owned.count)/300").font(.headline)
            ForEach(CollectionPolicy.milestones, id: \.self) { m in Text("\(owned.milestones.contains(m[0]) ? "✓" : "○") \(m[0])종 수집 · \(CollectionPolicy.names[m[1]]) 조각 +\(m[2])") }
        }
    }
    private func exchangeItems(craft: Bool) -> some View {
        VStack {
            Picker("소품 위치", selection: $slot) { ForEach(Array(SharedArtCatalog.slots.enumerated()), id: \.element) { i, value in Text(SharedArtCatalog.slotNames[i]).tag(value) } }
            ForEach(SharedArtCatalog.shared.items.filter { $0.slot == slot && !owned.owned.contains($0.id) }) { item in
                let grade = SharedArtCatalog.grade(item.id), price = CollectionPolicy.exchangePetals[grade]
                HStack { art("items/\(item.id).png", height: 44).frame(width: 44); Text(item.name).font(.caption); Spacer()
                    Button(craft ? "조각 \(CollectionPolicy.craftCost) · 제작" : "꽃잎 \(price) · 교환") { store.collect(craft ? "craft" : "exchange-tiered", item: item.id) }.disabled(craft ? owned.materials[grade] < CollectionPolicy.craftCost : owned.petals < price) }
            }
        }
    }
    private func art(_ path: String, height: CGFloat) -> some View { Group { if let image = SangmonImages.image(path) { Image(nsImage: image).resizable().scaledToFit() } }.frame(height: height) }
}
