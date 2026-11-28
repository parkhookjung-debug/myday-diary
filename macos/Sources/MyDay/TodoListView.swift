import SwiftUI

struct TodoListView:View {
    @ObservedObject var store:DiaryStore
    let id:UUID
    @State private var draft=""
    private var block:DiaryBlock {store.entry.blocks.first{$0.id==id} ?? DiaryBlock(kind:.todo)}
    var body:some View {
        switch Result(catching:{try TodoRules.items(block)}) {
        case .success(let items):content(items)
        case .failure(let error):Text(error.localizedDescription).foregroundStyle(.red)
        }
    }
    private func write(_ items:[TodoItem]) {
        do {let next=try TodoRules.applying(items,to:block);store.update {entry in if let index=entry.blocks.firstIndex(where:{$0.id==id}) {entry.blocks[index]=next}}}
        catch {store.error=error.localizedDescription}
    }
    private func content(_ items:[TodoItem])->some View {
        VStack(alignment:.leading,spacing:10) {
            HStack {TextField("새 할 일",text:$draft).textFieldStyle(.roundedBorder).onSubmit {add(items)}.accessibilityIdentifier("todo-input");Button("추가"){add(items)}.disabled(draft.trimmingCharacters(in:.whitespacesAndNewlines).isEmpty || items.count>=DesktopContract.maxTodoItems).accessibilityIdentifier("todo-add")}
            Text("할 일 \(items.filter(\.checked).count) / \(items.count) · 중요도 상·중·하").font(.caption).foregroundStyle(.secondary)
            ScrollView {VStack {ForEach(Array(items.enumerated()),id:\.offset) {index,item in
                HStack {
                    Toggle("완료",isOn:Binding(get:{item.checked},set:{value in var next=items;next[index].checked=value;write(next)})).labelsHidden().accessibilityLabel("\(item.text) 완료")
                    TextField("할 일",text:Binding(get:{item.text},set:{value in var next=items;next[index].text=value;write(next)})).textFieldStyle(.roundedBorder)
                    Picker("중요도",selection:Binding(get:{item.importance},set:{value in var next=items;next[index].importance=value;write(next)})) {Text("상").tag(1);Text("중").tag(0);Text("하").tag(2)}.labelsHidden().frame(width:65)
                    Button("삭제",role:.destructive) {var next=items;next.remove(at:index);write(next)}
                }
            }}}.frame(minHeight:40,maxHeight:220)
            if !items.isEmpty {Toggle("목록 모두 완료",isOn:Binding(get:{items.allSatisfy(\.checked)},set:{value in write(items.map {var row=$0;row.checked=value;return row})}))}
        }
    }
    private func add(_ items:[TodoItem]) {let text=draft.trimmingCharacters(in:.whitespacesAndNewlines);guard !text.isEmpty else{return};write(items+[TodoItem(text:text)]);if store.error==nil{draft=""}}
}
