import Foundation

@MainActor
enum JournalToolsVerification {
    static func run(root: URL) throws {
        var original=DiaryEntry();original.mood="행복해요";original.blocks=[DiaryBlock(kind: .text,text: "보관할 하루"),DiaryBlock(kind: .habit,text: "산책",checked: true)]
        let path=root.appendingPathComponent("history-test")
        try JournalHistory.beforeSave(path,date:"2024-02-28",previous:original,next:original)
        let empty=try JournalHistory.list(path);try MacVerification.check(empty.isEmpty,"unchanged diary has no revision")
        var changed=original;changed.blocks[0].text="바뀐 내용"
        try JournalHistory.beforeSave(path,date:"2024-02-28",previous:original,next:changed)
        let first=try JournalHistory.list(path)
        try MacVerification.check(first.count==1 && first[0].entry.blocks[0].text=="보관할 하루","history retains old content")
        try JournalHistory.beforeSave(path,date:"2024-02-28",previous:changed,next:nil)
        let removed=try JournalHistory.list(path);try MacVerification.check(removed.count==2,"deletion bypasses checkpoint window")
        let report=JournalReport.month(["2024-02-27":DiaryEntry(),"2024-02-28":original,"2024-02-29":original,"2024-03-01":original],date: ISO8601DateFormatter().date(from:"2024-02-20T12:00:00Z")!)
        try MacVerification.check(report.days==2 && report.streak==2 && report.habits==2 && report.completed==2 && report.moods["행복해요"]==2,"monthly leap year, blanks, streak and habits")
        let store=DiaryStore(directory:root.appendingPathComponent("restore-history/MyDay"));store.update {$0=original}
        let xp=store.sharedCompanion.xp
        store.deleteCurrentDay();try MacVerification.check(store.error==nil && store.entry.blocks.isEmpty,"whole day removed with recovery retained")
        let revision=try JournalHistory.list(store.directory).first!
        store.restoreRevision(revision)
        let days=try store.allDays()
        try MacVerification.check(store.error==nil && days[revision.date]?.blocks[0].text=="보관할 하루" && store.sharedCompanion.xp==xp,"restore retains diary without awarding XP")
        try JournalHistory.remove(path,item:first[0]);let purged=try JournalHistory.list(path);try MacVerification.check(purged.count==1,"explicit history purge")
        print("Journal tools: history, recovery, report and XP invariants passed")
    }
}
