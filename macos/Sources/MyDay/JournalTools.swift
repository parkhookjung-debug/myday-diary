import SwiftUI
import AppKit
import UserNotifications

struct JournalRevision: Codable, Identifiable {
    var id: String
    var date: String
    var captured: Date
    var reason: String
    var entry: DiaryEntry
}
enum JournalHistory {
    static func folder(_ root: URL) -> URL { root.appendingPathComponent("journal-history", isDirectory: true) }
    static func encoded(_ entry: DiaryEntry) throws -> Data { let encoder=JSONEncoder(); encoder.outputFormatting = [.sortedKeys]; return try encoder.encode(entry) }
    static func list(_ root: URL) throws -> [JournalRevision] {
        let path=folder(root)
        guard FileManager.default.fileExists(atPath: path.path) else { return [] }
        return try FileManager.default.contentsOfDirectory(at: path, includingPropertiesForKeys: nil).filter { $0.pathExtension == "json" }.map { url in
            let data=try Data(contentsOf: url); guard data.count<=WindowsCompatibility.maxBytes else { throw CocoaError(.fileReadTooLarge) }
            let item=try JSONDecoder().decode(JournalRevision.self, from: data)
            guard item.id==url.deletingPathExtension().lastPathComponent, MacBackup.validDate(item.date) else { throw CocoaError(.fileReadCorruptFile) }
            try MacBackup(days: [item.date:item.entry], companion: CompanionProfile()).validate()
            return item
        }.sorted { $0.captured > $1.captured }
    }
    static func beforeSave(_ root: URL,date: String,previous: DiaryEntry,next: DiaryEntry?) throws {
        if let next, try encoded(previous)==encoded(next) { return }
        let destructive=next==nil || previous.blocks.contains { old in
            next?.blocks.contains { $0.id==old.id && $0.photo==old.photo && !(old.text.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty==false && $0.text.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty) } != true
        }
        try capture(root,date: date,entry: previous,reason: destructive ? "삭제·교체 전" : "수정 전",force: destructive)
    }
    static func capture(_ root: URL,date: String,entry: DiaryEntry,reason: String,force: Bool) throws {
        guard JournalReport.meaningful(entry) else { return }
        let items=try list(root)
        if let last=items.first(where: {$0.date==date}) {
            if try encoded(last.entry)==encoded(entry) { return }
            if !force && Date().timeIntervalSince(last.captured)<Double(JournalPolicy.historyIntervalSeconds) { return }
        }
        let item=JournalRevision(id: UUID().uuidString,date: date,captured: Date(),reason: reason,entry: entry)
        let data=try JSONEncoder().encode(item), path=folder(root)
        let urls=(try? FileManager.default.contentsOfDirectory(at: path,includingPropertiesForKeys: [.fileSizeKey])) ?? []
        let size=try urls.reduce(0) { try $0+($1.resourceValues(forKeys: [.fileSizeKey]).fileSize ?? 0) }
        guard items.count<JournalPolicy.historyLimitFiles, size+data.count<=JournalPolicy.historyLimitBytes else {
            throw NSError(domain: "MyDay",code: 1,userInfo: [NSLocalizedDescriptionKey:"복구 보관함이 가득 찼어요. 필요 없는 이력을 정리한 뒤 다시 저장해 주세요."])
        }
        try FileManager.default.createDirectory(at: path,withIntermediateDirectories: true)
        try data.write(to: path.appendingPathComponent(item.id+".json"),options: .atomic)
    }
    static func remove(_ root: URL,item: JournalRevision) throws {
        guard UUID(uuidString: item.id) != nil else { throw CocoaError(.fileReadCorruptFile) }
        try FileManager.default.removeItem(at: folder(root).appendingPathComponent(item.id+".json"))
    }
}
struct JournalReport {
    var days=0, photos=0, habits=0, completed=0, streak=0
    var moods: [String:Int]=[:]
    static func meaningful(_ entry: DiaryEntry) -> Bool { entry.blocks.contains { !$0.text.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty || $0.photo != nil || $0.checked } }
    static func month(_ days: [String:DiaryEntry],date: Date) -> JournalReport {
        let calendar=Calendar(identifier: .gregorian), parts=calendar.dateComponents([.year,.month],from: date)
        var result=JournalReport(), run=0
        for day in 1...(calendar.range(of: .day,in: .month,for: date)?.count ?? 28) {
            let key=String(format: "%04d-%02d-%02d",parts.year!,parts.month!,day)
            guard let entry=days[key], meaningful(entry) else { run=0; continue }
            result.days+=1; run+=1; result.streak=max(result.streak,run)
            result.photos+=entry.blocks.filter {$0.photo != nil}.count
            let tasks=entry.blocks.filter {$0.kind == .habit && (!$0.text.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty || $0.checked)}
            result.habits+=tasks.count; result.completed+=tasks.filter(\.checked).count
            result.moods[entry.mood,default: 0]+=1
        }
        return result
    }
    var summary: String {
        "기록한 날  \(days)일\n가장 길게 이어 쓴 날  \(streak)일\n사진  \(photos)장\n습관 실천  \(completed) / \(habits)" + (habits==0 ? "" : " (\(100*completed/habits)%)") + "\n\n오늘의 마음\n" + moods.sorted {$0.value>$1.value}.map {"\($0.key)  \($0.value)일"}.joined(separator: "\n") + "\n\n빈 페이지는 제외해요. 감정은 직접 고른 값만 집계해요."
    }
}
extension DiaryStore {
    func restoreRevision(_ item: JournalRevision) {
        save(); guard error==nil else { return }
        do {
            var days=try allDays()
            if let current=days[item.date] { try JournalHistory.capture(directory,date: item.date,entry: current,reason: "복원 전",force: true) }
            days[item.date]=item.entry
            try restore(MacBackup(days: days,companion: companion,windowsMetadata: windowsMetadata)); load()
        } catch { self.error=error.localizedDescription }
    }
    func deleteCurrentDay() {
        save(); guard error==nil else { return }
        do {
            let calendar=Calendar(identifier: .gregorian), p=calendar.dateComponents([.year,.month,.day],from: date)
            let key=String(format:"%04d-%02d-%02d",p.year!,p.month!,p.day!)
            try JournalHistory.capture(directory,date: key,entry: entry,reason: "일기 삭제 전",force: true)
            let url=directory.appendingPathComponent(key+".json")
            if FileManager.default.fileExists(atPath: url.path) { try FileManager.default.removeItem(at: url) }
            entry=DiaryEntry(); pendingActivity=false; pendingBaseline=nil
        } catch { self.error=error.localizedDescription }
    }
}

struct JournalToolsView: View {
    @ObservedObject var store: DiaryStore
    @Environment(\.dismiss) var dismiss
    @State private var items: [JournalRevision]=[]
    @State private var selected: JournalRevision?
    @State private var month=Date()
    @State private var report=""
    @State private var message=""
    @AppStorage("journal-reminder-enabled") private var enabled=false
    @AppStorage("journal-reminder-hour") private var hour=20
    @AppStorage("journal-reminder-minute") private var minute=0
    @State private var pendingDelete: JournalRevision?
    @State private var deletingDay=false
    @State private var restoring=false
    var body: some View {
        VStack {
            HStack { Text("나의 기록 도구 · \(JournalPolicy.appVersion)").font(.title2); Spacer(); Button("닫기") { dismiss() } }.padding()
            if !message.isEmpty { Text(message).foregroundStyle(DesktopPalette.olive).textSelection(.enabled).padding(.horizontal) }
            TabView {
                VStack(alignment: .leading) {
                    Text("수정 전 기록을 5분 간격으로 보관해요. 블록·사진 삭제 전에는 바로 보관해요.").font(.caption)
                    List(items) { item in Button { selected=item } label: { Text("\(item.date) · \(item.reason) · \(item.captured.formatted())") } }
                    if let item=selected {
                        ScrollView { Text(item.entry.blocks.map {$0.text+($0.photo == nil ? "" : " [사진 보관됨]")}.joined(separator: "\n")).textSelection(.enabled).frame(maxWidth: .infinity,alignment: .leading) }.frame(height: 100)
                        HStack { Button("이 버전 복원") { restoring=true }; Button("이력 영구 삭제") { pendingDelete=item } }
                    }
                    Button("현재 날짜의 일기를 휴지통으로") { deletingDay=true }
                }.padding().tabItem { Text("1. 휴지통·이력") }
                VStack(alignment: .leading,spacing: 20) {
                    DatePicker("집계할 달",selection: $month,displayedComponents: .date).onChange(of: month) { _ in refreshReport() }
                    Text(report).font(.title3).textSelection(.enabled); Spacer()
                }.padding(24).tabItem { Text("2. 월간 리포트") }
                VStack(alignment: .leading,spacing: 20) {
                    Toggle("매일 일기 알림",isOn: $enabled)
                    HStack { Stepper("\(hour)시",value: $hour,in: 0...23); Stepper("\(minute)분",value: $minute,in: 0...59) }
                    Button("알림 설정 적용") { applyReminder() }
                    Text("사용자가 켠 시간에 하루 한 번 알려줘요. macOS 알림 허용이 필요해요. 앱이 종료돼도 OS가 예약한 알림을 표시해요.")
                    Spacer()
                }.padding(24).tabItem { Text("3. 알림") }
                VStack(alignment: .leading,spacing: 20) {
                    Text("같은 Wi-Fi에서 기록 이동").font(.title2)
                    Text("공통 백업을 저장한 뒤 tools/lan-transfer.py를 실행하세요. QR 또는 임시 주소로 다른 기기에서 백업을 받고, 설정·백업에서 가져오세요. 10분 후 전송이 닫혀요.")
                    Button("전송할 공통 백업 저장") { store.exportWindowsBackup() }
                    Button("Wi-Fi 전송 도구 안내") { openGuide() }; Spacer()
                }.padding(24).tabItem { Text("4. Wi-Fi · QR") }
                VStack(alignment: .leading,spacing: 20) {
                    Text("현재 버전 \(JournalPolicy.appVersion)").font(.title2)
                    Text("휴지통·변경 이력, 월간 리포트, 알림, Wi-Fi 전송 도구, 상몬 반응을 추가했어요.")
                    Button("최신 정식 버전 확인") { Task { await checkUpdate() } }
                    Button("1. 업데이트 전 백업 저장") { store.exportWindowsBackup() }
                    Button("2. 다운로드·설치 페이지 열기") { NSWorkspace.shared.open(URL(string:"https://github.com/parkhookjung-debug/myday-diary/releases")!) }
                    Text("새 앱을 Applications에 교체해도 Application Support의 개인 기록은 유지해요. 개발 빌드는 Actions에서 받아요."); Spacer()
                }.padding(24).tabItem { Text("6. 업데이트") }
            }
        }.frame(minWidth: 760,minHeight: 600).background(DesktopPalette.sidebar).onAppear { refresh(); refreshReport() }
        .alert("선택한 복구 사본을 영구 삭제할까요?",isPresented: Binding(get: {pendingDelete != nil},set: {if !$0 {pendingDelete=nil}})) {
            Button("삭제",role: .destructive) { if let item=pendingDelete { do { try JournalHistory.remove(store.directory,item: item); selected=nil; refresh() } catch {message=error.localizedDescription} } }; Button("취소",role: .cancel) {}
        }
        .alert("현재 날짜의 일기를 휴지통으로 옮길까요?",isPresented: $deletingDay) { Button("옮기기",role: .destructive) {store.deleteCurrentDay(); refresh(); refreshReport()}; Button("취소",role: .cancel) {} }
        .alert("이 버전으로 복원할까요? 현재 내용도 이력에 보관해요.",isPresented: $restoring) { Button("복원") {if let item=selected {store.restoreRevision(item); refresh(); refreshReport()}}; Button("취소",role: .cancel) {} }
    }
    private func refresh() { do {items=try JournalHistory.list(store.directory); if let error=store.error {message=error}} catch {message=error.localizedDescription} }
    private func refreshReport() { do {report=JournalReport.month(try store.allDays(),date: month).summary} catch {message=error.localizedDescription} }
    private func openGuide() { NSWorkspace.shared.open(URL(string:"https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/JOURNAL-TOOLS.md")!) }
    private func applyReminder() {
        let center=UNUserNotificationCenter.current(); center.removePendingNotificationRequests(withIdentifiers: ["journal-daily"])
        guard enabled else {message="알림을 껐어요."; return}
        Task { @MainActor in do {
            guard try await center.requestAuthorization(options: [.alert,.sound]) else {enabled=false; message="시스템 설정에서 MyDay 알림을 허용해 주세요.";return}
            var components=DateComponents(); components.hour=hour; components.minute=minute
            let content=UNMutableNotificationContent(); content.title="상몬과 한 줄 일기";content.body="오늘의 마음을 잠깐 남겨 볼까?";content.sound = .default
            try await center.add(UNNotificationRequest(identifier: "journal-daily",content: content,trigger: UNCalendarNotificationTrigger(dateMatching: components,repeats: true)))
            message="알림을 예약했어요."
        } catch {enabled=false; message=error.localizedDescription} }
    }
    private func checkUpdate() async {
        do {
            var request=URLRequest(url: URL(string:"https://api.github.com/repos/parkhookjung-debug/myday-diary/releases/latest")!);request.timeoutInterval=15;request.setValue("MyDay/"+JournalPolicy.appVersion,forHTTPHeaderField: "User-Agent")
            let (data,response)=try await URLSession.shared.data(for: request)
            if (response as? HTTPURLResponse)?.statusCode==404 {message="아직 정식 릴리스가 없어요. 개발 빌드는 Actions에 있어요.";return}
            guard (response as? HTTPURLResponse)?.statusCode==200, data.count<1_048_576,let root=try JSONSerialization.jsonObject(with: data) as? [String:Any],let tag=root["tag_name"] as? String else {throw CocoaError(.fileReadCorruptFile)}
            let version=tag.hasPrefix("v") ? String(tag.dropFirst()) : tag
            message=version.compare(JournalPolicy.appVersion,options: .numeric) == .orderedDescending ? "새 버전 \(tag)\n\(root["body"] as? String ?? "")" : "새로운 정식 릴리스가 없어요."
        } catch {message="버전을 확인하지 못했어요. 연결을 확인하고 다시 시도해 주세요."}
    }
}
