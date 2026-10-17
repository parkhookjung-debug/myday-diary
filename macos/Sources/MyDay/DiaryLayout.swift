import Foundation

struct BlockRect: Equatable {
    var x: Int
    var y: Int
    var width: Int
    var height: Int
    var bottom: Int { y + height }
}

enum DiaryLayout {
    static func clamp(_ value: Int, _ lower: Int, _ upper: Int) -> Int { min(upper, max(lower, value)) }
    static func defaultHeight(_ block: DiaryBlock) -> Int {
        block.kind == .photo || block.kind == .photoSlot ? 360 : block.kind == .emotion ? 288 : 250
    }
    static func bounds(_ block: DiaryBlock) -> BlockRect {
        BlockRect(x: block.x, y: block.y, width: block.width > 0 ? block.width : 340, height: block.height > 0 ? block.height : defaultHeight(block))
    }
    static func setBounds(_ rect: BlockRect, on block: inout DiaryBlock) {
        block.x = clamp(rect.x, 0, 10_000); block.y = clamp(rect.y, 0, 10_000)
        block.width = clamp(rect.width, 300, 1600); block.height = clamp(rect.height, 180, 1400)
    }
    static func dragged(_ original: BlockRect, dx: Int, dy: Int, resizing: Bool) -> BlockRect {
        resizing ? BlockRect(x: original.x, y: original.y, width: clamp(original.width + dx, 300, 1600), height: clamp(original.height + dy, 180, 1400))
            : BlockRect(x: clamp(original.x + dx, 0, 10_000), y: clamp(original.y + dy, 0, 10_000), width: original.width, height: original.height)
    }
    static func autoArrange(_ blocks: [DiaryBlock], width: Int) -> [BlockRect] {
        let available = max(312, min(1628, width) - 16)
        let two = available >= 624
        var x = 0, y = 4, rowHeight = 0
        return blocks.map { block in
            let cardWidth = block.wide || !two ? available - 12 : (available - 24) / 2
            let height = block.height > 0 ? block.height : defaultHeight(block)
            if x > 0 && x + cardWidth > available { x = 0; y += rowHeight + 12; rowHeight = 0 }
            let rect = BlockRect(x: x, y: y, width: cardWidth, height: height)
            x += cardWidth + 12; rowHeight = max(rowHeight, height)
            return rect
        }
    }
    static func enableFree(_ entry: inout DiaryEntry, width: Int) throws {
        let positions = autoArrange(entry.blocks, width: width)
        for index in entry.blocks.indices where entry.blocks[index].width == 0 || entry.blocks[index].height == 0 {
            guard positions[index].y <= 10_000 else { throw WindowsCompatibility.fail("블록이 너무 많아 자유 배치 공간을 넘습니다. 블록 수를 줄여 주세요.") }
            setBounds(positions[index], on: &entry.blocks[index])
        }
        entry.layoutMode = "free"
    }
    static func arrangeFree(_ entry: inout DiaryEntry, width: Int) throws {
        let positions = autoArrange(entry.blocks, width: width)
        guard positions.allSatisfy({ $0.y <= 10_000 }) else { throw WindowsCompatibility.fail("자동 정렬 결과가 자유 배치 범위를 넘습니다.") }
        for index in entry.blocks.indices { setBounds(positions[index], on: &entry.blocks[index]) }
    }
    static func append(_ block: DiaryBlock, to entry: inout DiaryEntry) throws {
        guard entry.blocks.count < 200 else { throw WindowsCompatibility.fail("한 날짜에 최대 200개 블록을 추가할 수 있습니다.") }
        var block = block
        if entry.layoutMode == "free" {
            let bottom = entry.blocks.map { bounds($0).bottom }.max() ?? 0
            guard bottom + 16 <= 10_000 else { throw WindowsCompatibility.fail("아래에 블록을 추가할 공간이 없습니다.") }
            setBounds(BlockRect(x: 12, y: bottom + 16, width: 340, height: defaultHeight(block)), on: &block)
        }
        entry.blocks.append(block)
    }
    static func preset(_ layout: String, count: Int, width: Int) -> [BlockRect] {
        let available = max(300, min(1600, width - 36)), half = (available - 16) / 2
        let two = half >= 300
        if layout == "split", count > 1, two {
            let side = max(300, available * 2 / 5), main = available - side - 16
            return [BlockRect(x: 12, y: 12, width: main, height: max(300, (count - 1) * 216 - 16))] + (1..<count).map { BlockRect(x: 12 + main + 16, y: 12 + ($0 - 1) * 216, width: side, height: 200) }
        }
        if layout == "cornell", count == 3, two {
            return [BlockRect(x: 12, y: 12, width: 300, height: 250), BlockRect(x: 328, y: 12, width: available - 316, height: 250), BlockRect(x: 12, y: 278, width: available, height: 200)]
        }
        if layout == "compare", count >= 2, two {
            return [BlockRect(x: 12, y: 12, width: half, height: 280), BlockRect(x: 28 + half, y: 12, width: available - half - 16, height: 280)] + (2..<count).map { BlockRect(x: 12, y: 308 + ($0 - 2) * 216, width: available, height: 200) }
        }
        if layout == "dashboard", count > 1, two {
            return [BlockRect(x: 12, y: 12, width: available, height: 180)] + (1..<count).map { BlockRect(x: 12 + (($0 - 1) % 2) * (half + 16), y: 208 + (($0 - 1) / 2) * 216, width: half, height: 200) }
        }
        if layout == "cards", two {
            return (0..<count).map { BlockRect(x: 12 + ($0 % 2) * (half + 16), y: 12 + ($0 / 2) * 236, width: half, height: 220) }
        }
        var y = 12
        return (0..<count).map { index in
            let height = layout == "letter" && index == 0 ? 340 : layout == "page" ? 280 : 200
            let result = BlockRect(x: 12, y: y, width: available, height: height)
            y += height + 16
            return result
        }
    }
    static func apply(_ template: JournalTemplate, to entry: inout DiaryEntry, width: Int) throws {
        let fresh = entry.blocks.isEmpty || (entry.blocks.count == 1 && entry.layoutMode == "cards" && entry.blocks[0].kind == .text && entry.blocks[0].text.isEmpty && !entry.blocks[0].checked && entry.blocks[0].title == nil && entry.blocks[0].prompt == nil && entry.blocks[0].photo == nil && entry.blocks[0].width == 0)
        let original = fresh ? [] : entry.blocks
        guard original.count + template.sections.count <= 200 else { throw WindowsCompatibility.fail("한 날짜에 최대 200개 블록을 추가할 수 있습니다.") }
        let positions = preset(template.layout, count: template.sections.count, width: width)
        let bottom = original.map { bounds($0).bottom }.max().map { $0 + 16 } ?? 0
        var added: [DiaryBlock] = []
        for (index, section) in template.sections.enumerated() {
            var block = DiaryBlock(kind: section.kind)
            block.title = section.title; block.prompt = section.prompt
            block.wide = ["page", "letter", "timeline"].contains(template.layout)
            if fresh || entry.layoutMode == "free" {
                var rect = positions[index]
                rect.y += fresh ? 0 : bottom
                guard rect.y <= 10_000 else { throw WindowsCompatibility.fail("형식을 추가할 자유 배치 공간이 없습니다.") }
                setBounds(rect, on: &block)
            }
            added.append(block)
        }
        entry.blocks = original + added
        if fresh { entry.layoutMode = "free" }
        entry.pageStyle = template.style
    }
}

extension DiaryStore {
    func changeLayout(_ mode: String, width: Int) {
        var next = entry
        do {
            if mode == "free" { try DiaryLayout.enableFree(&next, width: width) }
            else { next.layoutMode = "cards" }
            update { $0 = next }
        } catch { self.error = error.localizedDescription }
    }
    func arrangeBlocks(width: Int) {
        var next = entry
        do { try DiaryLayout.arrangeFree(&next, width: width); update { $0 = next } }
        catch { self.error = error.localizedDescription }
    }
    func addBlock(_ kind: BlockKind) {
        var next = entry
        do { try DiaryLayout.append(DiaryBlock(kind: kind), to: &next); update { $0 = next } }
        catch { self.error = error.localizedDescription }
    }
    func applyTemplate(_ template: JournalTemplate, width: Int = 800) {
        var next = entry
        do { try DiaryLayout.apply(template, to: &next, width: width); update { $0 = next } }
        catch { self.error = error.localizedDescription }
    }
}
