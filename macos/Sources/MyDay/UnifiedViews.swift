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
    @State private var fireAt = Date.distantPast
    var body: some View {
        TimelineView(.animation(minimumInterval: 1.0 / 24)) { timeline in
            Canvas { context, size in
                let time = timeline.date.timeIntervalSinceReferenceDate
                let age = timeline.date.timeIntervalSince(fireAt)
                let phase = time.truncatingRemainder(dividingBy: 42)
                let row = age < 0.7 ? 2 : (30..<31.6).contains(phase) ? 3 : (34..<36.4).contains(phase) ? 6 : 0
                let frame = row == 2 ? min(6, Int(age * 10)) : row == 3 ? Int((phase - 30) * 5) : Int(time * (row == 6 ? 3.33 : 1.9)) % 8
                let side = min(size.width, size.height), bounds = CGRect(x: (size.width - side) / 2, y: (size.height - side) / 2, width: side, height: side)
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
            .accessibilityLabel("상몬 \(companion.style), 레벨 \(companion.level). 누르면 불꽃 반응")
    }
}

struct DiaryView: View {
    @ObservedObject var store: DiaryStore
    @State private var tab = 0
    @StateObject private var lounge = LoungeModel()
    var body: some View {
        GeometryReader { geometry in
            HStack(spacing: 0) {
                sidebar.frame(width: geometry.size.width < 850 ? 170 : 222)
                Group {
                    switch tab {
                    case 2: CompanionContent(store: store)
                    case 3: LoungeView(model: lounge)
                    case 4: settings
                    default: diary(showFriend: geometry.size.width < 1120)
                    }
                }.frame(maxWidth: .infinity, maxHeight: .infinity)
                if tab == 0 && geometry.size.width >= 1120 { ScrollView { friend(vertical: true).padding(18) }.frame(width: 246).background(DesktopPalette.sidebar) }
            }.background(DesktopPalette.backgrounds[store.entry.theme]).foregroundStyle(DesktopPalette.ink)
        }.preferredColorScheme(.light).tint(DesktopPalette.olive).buttonStyle(PaperButtonStyle())
            .sheet(isPresented: $store.showingSearch) { JournalSearch(store: store) }
            .sheet(isPresented: $store.showingCompanion) { CompanionWardrobe(store: store) }
            .sheet(isPresented: $store.showingTemplates) { TemplateGallery(store: store) }
            .sheet(isPresented: $store.showingLayouts) { SavedLayoutsView(store: store) }
            .onDisappear { Task { await lounge.disconnect() } }
    }
    private var sidebar: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 16) {
                Text(AppShell.brand).font(.system(size: 26, weight: .bold)).foregroundStyle(DesktopPalette.olive)
                Text(AppShell.tagline).font(.caption).foregroundStyle(.secondary)
                Text("MY SPACE").font(.caption2).foregroundStyle(.secondary).padding(.top, 20)
                ForEach(0..<5) { i in
                    Button {
                        store.save(); guard store.error == nil else { return }
                        if i == 1 { store.showingSearch = true } else { tab = i }
                    } label: {
                        Text(AppShell.symbols[i] + "  " + AppShell.titles[i]).frame(maxWidth: .infinity, alignment: .leading).padding(10)
                    }.buttonStyle(.plain).background(tab == i ? DesktopPalette.selection : .clear, in: RoundedRectangle(cornerRadius: 10))
                }
                Button("오늘로 돌아가기 ↗") { store.select(Date()); if store.error == nil { tab = 0 } }
                DatePicker("기록 날짜", selection: Binding(get: { store.date }, set: { store.select($0); if store.error == nil { tab = 0 } }), displayedComponents: .date).datePickerStyle(.field).labelsHidden()
                Divider()
                Text("소중한 하루 보관하기").font(.headline)
                Button("↓ 백업 다운로드") { store.exportWindowsBackup() }
                Button("↥ 백업 복원") { store.importBackup() }
                Button("바탕화면 친구") { DesktopCompanion.shared.show(store: store) }
            }.padding(16)
        }.background(DesktopPalette.sidebar)
    }
    private func diary(showFriend: Bool) -> some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 18) {
                Text("A LITTLE SPACE FOR YOUR DAY").font(.caption).foregroundStyle(DesktopPalette.accent)
                Text(store.date.formatted(.dateTime.month().day())).font(.callout).foregroundStyle(DesktopPalette.accent)
                Text(AppShell.greeting).font(.system(size: 29, design: .serif))
                Text(AppShell.subtitle).font(.caption).foregroundStyle(.secondary)
                HStack {
                    Button("‹ 이전") { shift(-1) }; Button("오늘") { store.select(Date()) }; Button("다음 ›") { shift(1) }
                    Spacer(); Text("기기에 자동 저장").font(.caption).foregroundStyle(.secondary)
                }
                errorBanner
                HStack {
                    TextField("오늘의 마음", text: Binding(get: { store.entry.mood }, set: { v in store.update { $0.mood = v } })).textFieldStyle(.roundedBorder)
                    Button("일기 형식 100종") { store.showingTemplates = true }
                    Button("내 레이아웃") { store.showingLayouts = true }
                }
                if showFriend { friend(vertical: false) }
                DiaryBoard(store: store)
                Text("감정처리반은 마음을 적어 보관하는 공간이에요.").font(.caption).foregroundStyle(.secondary)
            }.padding(24)
        }
    }
    private func shift(_ amount: Int) { if let d = Calendar.current.date(byAdding: .day, value: amount, to: store.date) { store.select(d) } }
    private func friend(vertical: Bool) -> some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("YOUR LITTLE FRIEND").font(.caption2).foregroundStyle(.secondary)
            let layout = vertical ? AnyLayout(VStackLayout(alignment: .leading)) : AnyLayout(HStackLayout())
            layout {
                SharedSangmonView(companion: store.sharedCompanion).frame(width: 116, height: 120)
                VStack(alignment: .leading, spacing: 6) {
                    Text(AppShell.friendTitle).font(.system(size: 19, design: .serif))
                    Text("Lv.\(store.sharedCompanion.level) · \(store.sharedCompanion.xp) XP").font(.caption)
                }
            }
            Button("상몬·공방 열기") { tab = 2 }
            ForEach([BlockKind.todo, .habit], id: \.rawValue) { kind in
                let tasks = store.entry.blocks.filter { $0.kind == kind }
                Text("\(kind == .todo ? "할 일" : "습관")   \(tasks.filter(\.checked).count) / \(tasks.count)").font(.caption)
                ProgressView(value: Double(tasks.filter(\.checked).count), total: Double(max(1, tasks.count))).tint(DesktopPalette.progress)
            }
        }.padding(18).frame(maxWidth: .infinity, alignment: .leading).background(DesktopPalette.friendCard, in: RoundedRectangle(cornerRadius: 20))
    }
    private var errorBanner: some View {
        Group { if let error = store.error { VStack(alignment: .leading) { Text(error).foregroundStyle(.red); Button(store.loaded ? "다시 저장" : "다시 불러오기") { store.retrySaveOrLoad() } }.padding(12).background(DesktopPalette.tint, in: RoundedRectangle(cornerRadius: 12)) } }
    }
    private var settings: some View {
        ScrollView { VStack(alignment: .leading, spacing: 20) {
            Text(AppShell.titles[4]).font(.largeTitle)
            errorBanner
            Text("오늘의 분위기").font(.headline)
            Picker("배경", selection: Binding(get: { store.entry.theme }, set: { v in store.update { $0.theme = v } })) { Text("크림").tag(0); Text("숲").tag(1); Text("라벤더").tag(2) }.pickerStyle(.segmented)
            Text("소중한 하루 보관하기").font(.headline)
            Text("일기·사진·배치·성장·소품·뽑기 기록을 Windows·Mac·Android 사이에서 JSON 파일로 옮겨요. 자동 동기화는 하지 않아요.")
            Button("↓ 공통 백업 다운로드") { store.exportWindowsBackup() }
            Button("↥ 백업 복원") { store.importBackup() }
            Button("기존 Mac 전용 백업 다운로드") { store.exportBackup() }
            Button("저장 폴더 열기") { NSWorkspace.shared.open(store.directory) }
            Divider()
            Text("바탕화면의 작은 친구").font(.headline)
            Button("상몬 표시") { DesktopCompanion.shared.show(store: store) }; Button("상몬 숨기기") { DesktopCompanion.shared.hide() }
        }.padding(28) }
    }
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
            Text("클로버 \(owned.coins) · 꽃잎 \(owned.petals) · 수집 \(owned.owned.count)/300").font(.headline)
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
