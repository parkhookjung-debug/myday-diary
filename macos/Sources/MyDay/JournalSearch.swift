import SwiftUI

struct JournalSearch: View {
    @ObservedObject var store: DiaryStore
    @Environment(\.dismiss) private var dismiss
    @State private var query = ""
    @State private var days: [String: DiaryEntry] = [:]
    @State private var failure: String?
    @State private var selected = Date()
    @State private var month = Date()
    @State private var showEmpty = false
    @State private var newestFirst = true

    private var calendar: Calendar { var value=Calendar(identifier:.gregorian);value.locale=Locale(identifier:"ko_KR");value.timeZone=TimeZone.current;return value }
    private var trimmedQuery:String {query.trimmingCharacters(in:.whitespacesAndNewlines)}
    private var monthStart:Date {calendar.date(from:calendar.dateComponents([.year,.month],from:month)) ?? month}
    private var calendarDates:[Date] {
        let offset=calendar.component(.weekday,from:monthStart)-1
        let start=calendar.date(byAdding:.day,value:-offset,to:monthStart) ?? monthStart
        return (0..<42).compactMap{calendar.date(byAdding:.day,value:$0,to:start)}
    }
    private var visibleKeys:[String] {
        let keys:[String]
        if !trimmedQuery.isEmpty {
            keys=days.keys.filter{key in
                guard let entry=days[key] else{return false}
                guard hasContent(entry) else{return false}
                let note=store.studio.note(key)
                return entry.mood.localizedCaseInsensitiveContains(trimmedQuery) || note.Tags.contains{$0.localizedCaseInsensitiveContains(trimmedQuery)} || entry.blocks.contains{block in (block.title ?? "").localizedCaseInsensitiveContains(trimmedQuery) || block.text.localizedCaseInsensitiveContains(trimmedQuery)}
            }
        } else if showEmpty {
            let count=calendar.range(of:.day,in:.month,for:monthStart)?.count ?? 0
            keys=(0..<count).compactMap{calendar.date(byAdding:.day,value:$0,to:monthStart)}.map(key)
        } else {
            keys=days.keys.filter{guard let entry=days[$0],hasContent(entry),let date=date($0) else{return false};return calendar.isDate(date,equalTo:monthStart,toGranularity:.month)}
        }
        return keys.sorted{newestFirst ? $0>$1:$0<$1}
    }

    var body: some View {
        VStack(alignment:.leading,spacing:18){
            HStack(spacing:14){
                Text("my day").font(.system(size:25,weight:.semibold,design:.serif)).italic().foregroundStyle(DesktopPalette.olive)
                Divider().frame(height:26)
                Text(AppShell.tagline).foregroundStyle(DesktopPalette.muted)
                Spacer()
                Button{dismiss()}label:{HStack(spacing:7){Text("닫기");Image(systemName:"xmark")}}.buttonStyle(.plain).font(.headline)
            }
            VStack(alignment:.leading,spacing:5){Text("달력 · 일기 검색").font(.largeTitle.bold());Text("차곡차곡 쌓인 나의 하루를 찾아보세요.").font(.title3).foregroundStyle(DesktopPalette.muted)}
            HStack(spacing:12){Image(systemName:"magnifyingglass").font(.title2).foregroundStyle(DesktopPalette.olive);TextField("일기 내용과 사진 설명 검색",text:$query).textFieldStyle(.plain).font(.title3)}
                .padding(.horizontal,18).frame(height:54).background(DesktopPalette.paper,in:RoundedRectangle(cornerRadius:14)).overlay(RoundedRectangle(cornerRadius:14).stroke(DesktopPalette.soft,lineWidth:1))
            if let failure {Text(failure).foregroundStyle(.red)}
            HStack(alignment:.top,spacing:14){calendarPanel;recordsPanel}.frame(maxHeight:.infinity)
        }.padding(26).frame(width:940,height:700).background(DesktopPalette.sidebar).foregroundStyle(DesktopPalette.ink).tint(DesktopPalette.olive)
        .onAppear{
            store.save();if let error=store.error{failure=error;return}
            do{days=try store.allDays();selected=store.date;month=store.date}catch{failure="기록을 읽지 못했습니다. \(error.localizedDescription)"}
        }
    }

    private var calendarPanel:some View {
        VStack(alignment:.leading,spacing:14){
            HStack{Text(monthStart.formatted(.dateTime.year().month())).font(.title2.bold());Spacer();Button{shiftMonth(-1)}label:{Image(systemName:"chevron.left")};Button{shiftMonth(1)}label:{Image(systemName:"chevron.right")};Button("오늘"){selected=Date();month=Date()}}
            LazyVGrid(columns:Array(repeating:GridItem(.flexible(),spacing:4),count:7),spacing:5){
                ForEach(["일","월","화","수","목","금","토"],id:\.self){Text($0).font(.caption.weight(.semibold)).foregroundStyle(DesktopPalette.muted).frame(height:24)}
                ForEach(calendarDates,id:\.self){date in
                    let inMonth=calendar.isDate(date,equalTo:monthStart,toGranularity:.month)
                    let isSelected=calendar.isDate(date,inSameDayAs:selected)
                    let hasEntry=days[key(date)].map(hasContent) ?? false
                    Button{selected=date;if !inMonth{month=date}}label:{VStack(spacing:3){Text("\(calendar.component(.day,from:date))").font(.body.weight(isSelected ? .bold:.regular)).frame(width:34,height:28).foregroundStyle(isSelected ? Color.white:inMonth ? DesktopPalette.ink:DesktopPalette.muted.opacity(0.45)).background(isSelected ? DesktopPalette.olive:Color.clear,in:Circle());Circle().fill(hasEntry ? DesktopPalette.olive:Color.clear).frame(width:5,height:5)}}.buttonStyle(.plain)
                }
            }
            HStack(spacing:8){Circle().fill(DesktopPalette.olive).frame(width:7,height:7);Text("기록이 있는 날").font(.caption).foregroundStyle(DesktopPalette.muted)}
            Divider()
            Text(selected.formatted(.dateTime.month().day().weekday(.wide))).font(.headline)
            Button{open(selected)}label:{HStack{Spacer();Text("이 날의 일기 열기");Image(systemName:"arrow.up.right");Spacer()}.frame(height:38).foregroundStyle(.white).background(DesktopPalette.olive,in:RoundedRectangle(cornerRadius:10))}.buttonStyle(.plain)
        }.padding(20).frame(width:360).frame(maxHeight:.infinity,alignment:.top).background(DesktopPalette.paper,in:RoundedRectangle(cornerRadius:18)).overlay(RoundedRectangle(cornerRadius:18).stroke(DesktopPalette.soft,lineWidth:1))
    }

    private var recordsPanel:some View {
        VStack(alignment:.leading,spacing:12){
            HStack(alignment:.top){VStack(alignment:.leading,spacing:3){Text(trimmedQuery.isEmpty ? "\(calendar.component(.month,from:monthStart))월의 기록":"검색 결과").font(.title2.bold());Text("\(visibleKeys.count)일의 기록").font(.caption).foregroundStyle(DesktopPalette.muted)};Spacer();Toggle("빈 기록 표시",isOn:$showEmpty).toggleStyle(.switch).controlSize(.small).disabled(!trimmedQuery.isEmpty);Divider().frame(height:28);Button(newestFirst ? "최신순 ↓":"오래된순 ↑"){newestFirst.toggle()}}
            if visibleKeys.isEmpty {VStack(spacing:10){Image(systemName:"book.closed").font(.largeTitle).foregroundStyle(DesktopPalette.muted);Text(trimmedQuery.isEmpty ? "이 달에는 기록이 없어요.":"검색 결과가 없어요.").foregroundStyle(DesktopPalette.muted)}.frame(maxWidth:.infinity,maxHeight:.infinity)}
            else {ScrollView{LazyVStack(spacing:10){ForEach(visibleKeys,id:\.self){recordCard($0)}}}.scrollIndicators(.visible)}
            Text(trimmedQuery.isEmpty ? (showEmpty ? "빈 날짜를 포함해 보여드려요.":"기록이 있는 날짜만 모아 보여드려요."):"본문·사진 설명·감정·태그에서 찾았어요.").font(.caption).foregroundStyle(DesktopPalette.muted)
        }.padding(18).frame(maxWidth:.infinity,maxHeight:.infinity,alignment:.top).background(DesktopPalette.paper,in:RoundedRectangle(cornerRadius:18)).overlay(RoundedRectangle(cornerRadius:18).stroke(DesktopPalette.soft,lineWidth:1))
    }

    private func recordCard(_ dateKey:String)->some View {
        let entry=days[dateKey] ?? DiaryEntry()
        let dateValue=date(dateKey) ?? selected
        let term=trimmedQuery
        let block=entry.blocks.first{term.isEmpty ? !$0.text.trimmingCharacters(in:.whitespacesAndNewlines).isEmpty : ($0.title ?? "").localizedCaseInsensitiveContains(term) || $0.text.localizedCaseInsensitiveContains(term)}
        let title=entry.blocks.first{$0.title?.isEmpty==false}?.title ?? (entry.blocks.isEmpty ? "빈 기록":"나의 하루 기록")
        let summary=block?.text.trimmingCharacters(in:.whitespacesAndNewlines).isEmpty==false ? block!.text:entry.blocks.contains(where:{$0.photo != nil}) ? "사진이 담긴 기록이에요.":entry.blocks.contains(where:{$0.videoPath != nil}) ? "영상이 연결된 기록이에요.":"아직 작성한 내용이 없어요."
        let tags=store.studio.note(dateKey).Tags
        return Button{open(dateValue)}label:{
            HStack(spacing:14){
                VStack(spacing:1){Text("\(calendar.component(.day,from:dateValue))").font(.title.bold());Text(dateValue.formatted(.dateTime.weekday(.short))).font(.caption).foregroundStyle(DesktopPalette.olive)}.frame(width:56,height:66).background(DesktopPalette.selection,in:RoundedRectangle(cornerRadius:12))
                VStack(alignment:.leading,spacing:6){Text(title).font(.headline).lineLimit(1);Text(summary).font(.callout).foregroundStyle(DesktopPalette.muted).lineLimit(1);HStack(spacing:6){HStack(spacing:5){MoodSticker(mood:entry.mood,size:22);Text(entry.mood).font(.caption)}.padding(.horizontal,7).padding(.vertical,3).background(DesktopPalette.selection,in:Capsule());ForEach(Array(tags.prefix(2)),id:\.self){Text("#\($0)").font(.caption).padding(.horizontal,8).padding(.vertical,4).background(DesktopPalette.sidebar,in:Capsule())};if entry.blocks.contains(where:{$0.photo != nil}){Image(systemName:"photo").font(.caption).foregroundStyle(DesktopPalette.olive)}}}
                Spacer();Image(systemName:"chevron.right").foregroundStyle(DesktopPalette.muted)
            }.padding(12).frame(maxWidth:.infinity,alignment:.leading).background(DesktopPalette.paper,in:RoundedRectangle(cornerRadius:14)).overlay(RoundedRectangle(cornerRadius:14).stroke(DesktopPalette.soft,lineWidth:1))
        }.buttonStyle(.plain)
    }

    private func key(_ date:Date)->String {DiaryStore.todayKey(date)}
    private func hasContent(_ entry:DiaryEntry)->Bool {entry.blocks.contains{block in block.photo != nil || block.videoPath != nil || block.checked || !block.text.trimmingCharacters(in:.whitespacesAndNewlines).isEmpty}}
    private func date(_ key:String)->Date? {let formatter=DateFormatter();formatter.calendar=Calendar(identifier:.gregorian);formatter.locale=Locale(identifier:"en_US_POSIX");formatter.dateFormat="yyyy-MM-dd";return formatter.date(from:key)}
    private func shiftMonth(_ amount:Int){if let next=calendar.date(byAdding:.month,value:amount,to:monthStart){month=next;selected=next}}
    private func open(_ date:Date){store.select(date);if store.error==nil{dismiss()}else{failure=store.error}}
}
