import SwiftUI

enum AnniversaryRules {
    static func matches(_ item:Anniversary,_ key:String)->Bool {
        item.RepeatYearly ? String(item.Date.suffix(5))==String(key.suffix(5)) : item.Date==key
    }
}
struct AnniversaryView:View {
    @ObservedObject var store:DiaryStore
    @Environment(\.dismiss) private var dismiss
    @State private var name=""
    @State private var date=DiaryStore.todayKey()
    @State private var yearly=true
    @State private var failure:String?
    var body:some View {
        VStack(alignment:.leading,spacing:14) {
            HStack {Text("생일 · 기념일").font(.title.bold());Spacer();Button("닫기"){dismiss()}}
            TextField("이름",text:$name).textFieldStyle(.roundedBorder).accessibilityIdentifier("anniversary-name")
            TextField("날짜 YYYY-MM-DD",text:$date).textFieldStyle(.roundedBorder).accessibilityIdentifier("anniversary-date")
            Toggle("매년 같은 날짜에 표시",isOn:$yearly)
            Button("기념일 추가") {
                var next=store.studio
                next.Anniversaries=(next.Anniversaries ?? [])+[Anniversary(Name:name.trimmingCharacters(in:.whitespacesAndNewlines),Date:date,RepeatYearly:yearly)]
                do {try next.validate();store.commitStudio(next);failure=store.error;if failure==nil{name=""}}
                catch {failure=error.localizedDescription}
            }.accessibilityIdentifier("anniversary-add")
            if let failure {Text(failure).foregroundStyle(.red)}
            ScrollView {VStack(alignment:.leading,spacing:12) {ForEach((store.studio.Anniversaries ?? []).sorted {($0.Date,$0.Name)<($1.Date,$1.Name)}) {item in
                HStack {VStack(alignment:.leading) {Text(item.Name).font(.headline);Text(item.Date+(item.RepeatYearly ? " · 매년":" · 한 번")).font(.caption)};Spacer();Button("삭제",role:.destructive) {var next=store.studio;next.Anniversaries=(next.Anniversaries ?? []).filter{$0.Id != item.Id};store.commitStudio(next);failure=store.error}}
            }}}
        }.padding(24).frame(width:530,height:550)
    }
}
