import SwiftUI
import AppKit

struct DiaryBoard: View {
    @ObservedObject var store: DiaryStore
    var startsEditing = false
    @State private var editing = false
    @State private var zoom = 0.9
    @State private var availableWidth = 800
    @State private var confirmingArrange = false
    private var free: Bool { store.entry.layoutMode == "free" }
    private func positions(width: Int) -> [BlockRect] {
        free ? store.entry.blocks.map(DiaryLayout.bounds) : DiaryLayout.autoArrange(store.entry.blocks, width: width)
    }
    private var canvasHeight: CGFloat {
        CGFloat((positions(width: availableWidth).map(\.bottom).max() ?? 80) + 24) * (free ? zoom : 1)
    }
    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                Picker("배치", selection: Binding(get: { store.entry.layoutMode }, set: { value in
                    store.changeLayout(value, width: availableWidth)
                    if value == "cards" { editing = false }
                })) {
                    Text("자동 정렬").tag("cards")
                    Text("자유 배치").tag("free")
                }.pickerStyle(.segmented).frame(width: 240)
                Picker("종이", selection: Binding(get: { store.entry.pageStyle }, set: { value in store.update { $0.pageStyle = value } })) {
                    Text("기본").tag("plain"); Text("줄 노트").tag("paper"); Text("도트").tag("dots")
                }.frame(width: 180)
                Spacer()
            }
            if free {
                HStack {
                    Button(editing ? "편집 완료" : "배치 편집") { editing.toggle() }
                    Button("자동으로 다시 정렬…") { confirmingArrange = true }
                    Spacer()
                    Text("\(Int(zoom * 100))%").monospacedDigit().font(.caption)
                    Slider(value: $zoom, in: 0.5...1.25).frame(width: 110).accessibilityLabel("배치 화면 확대")
                }
                Text(editing ? "제목을 드래그해 이동하고 오른쪽 아래 ↘를 끌어 크기를 바꾸세요. 놓으면 저장됩니다." : "가로 스크롤과 확대/축소로 넓은 페이지를 살펴보세요. 배치 편집을 누르면 이동할 수 있습니다.")
                    .font(.caption).foregroundStyle(.secondary)
            }
            GeometryReader { geometry in
                let rects = positions(width: Int(geometry.size.width))
                let width = free ? max(800, (rects.map { $0.x + $0.width }.max() ?? 0) + 24) : Int(geometry.size.width)
                let height = (rects.map(\.bottom).max() ?? 80) + 24
                ScrollView(.horizontal) {
                    ZStack(alignment: .topLeading) {
                        PaperBackground(style: store.entry.pageStyle)
                        ForEach(Array(store.entry.blocks.enumerated()), id: \.element.id) { index, block in
                            if rects.indices.contains(index) {
                                PositionedDiaryCard(store: store, block: block, rect: rects[index], index: index,
                                                    editing: editing && free, zoom: free ? zoom : 1)
                                    .zIndex(Double(index))
                            }
                        }
                    }
                    .frame(width: CGFloat(width), height: CGFloat(height), alignment: .topLeading)
                    .scaleEffect(free ? zoom : 1, anchor: .topLeading)
                    .frame(width: CGFloat(width) * (free ? zoom : 1), height: CGFloat(height) * (free ? zoom : 1), alignment: .topLeading)
                }
                .onAppear { availableWidth = Int(geometry.size.width) }
                .onChange(of: geometry.size.width) { availableWidth = Int($0) }
            }.frame(height: canvasHeight)
            Menu {
                ForEach(BlockKind.allCases.filter { $0 != .photoSlot }, id: \.self) { kind in
                    Button(kind.title) { store.addBlock(kind) }.disabled(store.entry.blocks.count >= 200)
                }
            } label: {
                Text("+ 블록 추가").frame(maxWidth: .infinity).padding(10)
            }.menuStyle(.borderlessButton).padding(8).background(.white.opacity(0.8), in: RoundedRectangle(cornerRadius: 16))
        }
        .onAppear { editing = startsEditing }
        .onChange(of: store.date) { _ in editing = false }
        .alert("현재 위치를 자동 정렬할까요?", isPresented: $confirmingArrange) {
            Button("취소", role: .cancel) { }
            Button("다시 정렬") { store.arrangeBlocks(width: availableWidth) }
        } message: { Text("글과 사진은 유지하며 블록의 위치와 크기를 다시 배치합니다.") }
    }
}

struct PositionedDiaryCard: View {
    @ObservedObject var store: DiaryStore
    let block: DiaryBlock
    let rect: BlockRect
    let index: Int
    let editing: Bool
    let zoom: Double
    @GestureState private var dragOffset = CGSize.zero
    @GestureState private var resizeOffset = CGSize.zero
    @State private var deleting = false
    private var displayed: BlockRect {
        let moved = DiaryLayout.dragged(rect, dx: Int(dragOffset.width / zoom), dy: Int(dragOffset.height / zoom), resizing: false)
        return DiaryLayout.dragged(moved, dx: Int(resizeOffset.width / zoom), dy: Int(resizeOffset.height / zoom), resizing: true)
    }
    private var text: Binding<String> {
        Binding(get: { store.entry.blocks.first(where: { $0.id == block.id })?.text ?? "" }, set: { value in
            store.update { entry in
                if let index = entry.blocks.firstIndex(where: { $0.id == block.id }) { entry.blocks[index].text = value }
            }
        })
    }
    private func commit(_ translation: CGSize, resizing: Bool) {
        guard editing else { return }
        let updated = DiaryLayout.dragged(rect, dx: Int(translation.width / zoom), dy: Int(translation.height / zoom), resizing: resizing)
        store.update { entry in
            if let index = entry.blocks.firstIndex(where: { $0.id == block.id }) { DiaryLayout.setBounds(updated, on: &entry.blocks[index]) }
        }
    }
    private var moveGesture: some Gesture {
        DragGesture(minimumDistance: 2, coordinateSpace: .global)
            .updating($dragOffset) { value, state, _ in state = value.translation }
            .onEnded { commit($0.translation, resizing: false) }
    }
    private var resizeGesture: some Gesture {
        DragGesture(minimumDistance: 2, coordinateSpace: .global)
            .updating($resizeOffset) { value, state, _ in state = value.translation }
            .onEnded { commit($0.translation, resizing: true) }
    }
    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            HStack(spacing: 8) {
                if editing { Image(systemName: "arrow.up.and.down.and.arrow.left.and.right").foregroundStyle(.brown) }
                Text(block.title ?? block.kind.title).font(.headline).lineLimit(2)
                Spacer(minLength: 0)
            }.padding(6).frame(maxWidth: .infinity, alignment: .leading)
                .background(editing ? Color.brown.opacity(0.1) : .clear, in: RoundedRectangle(cornerRadius: 8))
                .contentShape(Rectangle()).gesture(moveGesture, including: editing ? .all : .none)
                .accessibilityLabel(editing ? "블록 이동: \(block.title ?? block.kind.title)" : block.title ?? block.kind.title)
            ScrollView {
                VStack(alignment: .leading, spacing: 8) {
                    if let prompt = block.prompt, !prompt.isEmpty { Text(prompt).font(.caption).foregroundStyle(.secondary).fixedSize(horizontal: false, vertical: true) }
                    if block.kind == .emotion && block.prompt == nil { Text("판단 없이 들어줄게. 지금 마음을 들려줘.").font(.caption).foregroundStyle(.secondary) }
                    if block.kind == .photo || block.kind == .photoSlot {
                        if let data = block.photo, let image = NSImage(data: data) {
                            Image(nsImage: image).resizable().scaledToFit().frame(maxHeight: min(220, CGFloat(displayed.height) * 0.5))
                        }
                        Button(block.photo == nil ? "사진 선택…" : "사진 바꾸기…") { store.choosePhoto(block.id) }
                    }
                    TextEditor(text: text).font(.body).scrollContentBackground(.hidden)
                        .frame(height: max(70, CGFloat(displayed.height) - (block.prompt == nil ? 110 : 145) - (block.kind == .photo ? 170 : 0)))
                        .padding(6).background(Color.black.opacity(0.025), in: RoundedRectangle(cornerRadius: 8))
                        .accessibilityLabel(block.kind == .photo || block.kind == .photoSlot ? "사진 설명" : "\(block.title ?? block.kind.title) 본문")
                    if block.kind == .todo || block.kind == .habit {
                        Toggle("오늘 완료했어요", isOn: Binding(get: { store.entry.blocks.first(where: { $0.id == block.id })?.checked ?? false }, set: { value in
                            store.update { entry in
                                if let index = entry.blocks.firstIndex(where: { $0.id == block.id }) { entry.blocks[index].checked = value }
                            }
                        }))
                    }
                }.padding(.horizontal, 4)
            }.allowsHitTesting(!editing)
            HStack(spacing: 8) {
                Button("↑") { store.move(block.id, by: -1) }.disabled(index == 0 || editing).help("순서 앞으로")
                Button("↓") { store.move(block.id, by: 1) }.disabled(index == store.entry.blocks.count - 1 || editing).help("순서 뒤로")
                Spacer()
                Button("삭제", role: .destructive) { deleting = true }.disabled(editing)
                if editing {
                    Text("↘").font(.title2.bold()).frame(width: 30, height: 24).contentShape(Rectangle())
                        .gesture(resizeGesture).accessibilityLabel("블록 크기 조절")
                }
            }.font(.caption)
        }
        .padding(12).frame(width: CGFloat(displayed.width), height: CGFloat(displayed.height))
        .background(.white.opacity(0.96), in: RoundedRectangle(cornerRadius: 16))
        .overlay(RoundedRectangle(cornerRadius: 16).stroke(editing ? Color.brown.opacity(0.5) : Color.brown.opacity(0.09), lineWidth: editing ? 2 : 1))
        .offset(x: CGFloat(displayed.x), y: CGFloat(displayed.y))
        .alert("블록을 삭제할까요?", isPresented: $deleting) {
            Button("취소", role: .cancel) { }
            Button("삭제", role: .destructive) { store.update { $0.blocks.removeAll { $0.id == block.id } } }
        } message: { Text("작성한 내용과 첨부 사진도 함께 삭제됩니다.") }
    }
}

struct PaperBackground: View {
    var style: String
    var body: some View {
        Canvas { context, size in
            if style == "paper" {
                var path = Path()
                for y in stride(from: 26.0, to: size.height, by: 26) { path.move(to: CGPoint(x: 0, y: y)); path.addLine(to: CGPoint(x: size.width, y: y)) }
                context.stroke(path, with: .color(.brown.opacity(0.14)), lineWidth: 0.7)
            } else if style == "dots" {
                var path = Path()
                for y in stride(from: 12.0, to: size.height, by: 24) {
                    for x in stride(from: 12.0, to: size.width, by: 24) { path.addEllipse(in: CGRect(x: x, y: y, width: 2, height: 2)) }
                }
                context.fill(path, with: .color(.brown.opacity(0.2)))
            }
        }.background(Color.white.opacity(style == "plain" ? 0 : 0.3)).allowsHitTesting(false)
    }
}
