import Foundation

struct TodoItem: Equatable {
    var text: String
    var checked = false
    var importance = 0
}
enum TodoRules {
    static func items(_ block: DiaryBlock) throws -> [TodoItem] {
        var previous: [TodoItem] = []
        if let value=block.windowsFields?["TodoItems"], value != .null {
            guard let rows=value.array,rows.count<=DesktopContract.maxTodoItems else {throw WindowsCompatibility.fail("할 일 목록이 올바르지 않습니다.")}
            previous=try rows.map {value in
                guard let row=value.object,let text=row["Text"]?.string,let checked=row["Checked"]?.bool else {throw WindowsCompatibility.fail("할 일 항목이 올바르지 않습니다.")}
                if let value=row["Importance"],value != .null,value.int == nil {throw WindowsCompatibility.fail("할 일 중요도를 확인해 주세요.")}
                let importance=row["Importance"]?.int ?? 0
                guard (0...2).contains(importance) else {throw WindowsCompatibility.fail("할 일 중요도를 확인해 주세요.")}
                return TodoItem(text:text,checked:checked,importance:importance)
            }
        } else {previous=lines(block.text).map {TodoItem(text:$0,checked:block.checked)}}
        try validate(previous)
        if !previous.isEmpty,block.checked != previous.allSatisfy(\.checked) {previous=previous.map {var row=$0;row.checked=block.checked;return row}}
        if block.text==previous.map(\.text).joined(separator:"\n") {return previous}
        var remaining=previous
        let result=lines(block.text).map {text -> TodoItem in
            if let index=remaining.firstIndex(where:{$0.text==text}) {return remaining.remove(at:index)}
            return TodoItem(text:text,checked:previous.isEmpty && block.checked)
        }
        try validate(result);return result
    }
    private static func lines(_ text:String)->[String] {text.replacingOccurrences(of:"\r\n",with:"\n").split(separator:"\n",omittingEmptySubsequences:true).map(String.init)}
    static func validate(_ rows:[TodoItem]) throws {
        guard rows.count<=DesktopContract.maxTodoItems,rows.allSatisfy({(0...2).contains($0.importance)}),rows.reduce(max(0,rows.count-1),{$0+$1.text.utf16.count})<=DesktopContract.maxTextUtf16 else {throw WindowsCompatibility.fail("할 일은 100개, 전체 100,000자까지 저장할 수 있어요.")}
    }
    static func applying(_ rows:[TodoItem],to block:DiaryBlock) throws -> DiaryBlock {
        try validate(rows);var next=block
        next.text=rows.map(\.text).joined(separator:"\n");next.checked = !rows.isEmpty && rows.allSatisfy(\.checked)
        var fields=block.windowsFields ?? [:]
        fields["TodoItems"] = .array(rows.map {.object(["Text":.string($0.text),"Checked":.bool($0.checked),"Importance":.number(Double($0.importance))])})
        fields["TodoLayoutVersion"] = .number(1);next.windowsFields=fields
        if block.windowsFields?["TodoLayoutVersion"]?.int == nil {next.wide=true}
        return next
    }
    static func newlyCompleted(_ block:DiaryBlock,_ prior:DiaryBlock?)->Bool {
        guard let rows=try? items(block) else{return false}
        let old=prior.flatMap {try? items($0)} ?? []
        if rows.isEmpty {return block.checked && prior?.checked != true}
        return rows.enumerated().contains {i,item in item.checked && (i>=old.count || old[i].text != item.text || !old[i].checked)}
    }
}
