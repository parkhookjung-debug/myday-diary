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
        let fixture=try WindowsCompatibility.decode(CompatibilityVerification.data(CompatibilityVerification.fixture()))
        let photo=fixture.days.values.first!.blocks.first {$0.kind == .photo}!
        var photoEntry=DiaryEntry();photoEntry.blocks=[photo]
        let photoRoot=root.appendingPathComponent("history-photo")
        try JournalHistory.beforeSave(photoRoot,date:"2024-02-29",previous:photoEntry,next:nil)
        let recovered=try JournalHistory.list(photoRoot).first!.entry.blocks.first!
        try MacVerification.check(recovered.photo==photo.photo && recovered.text==photo.text && recovered.width==photo.width,"deleted photo data and layout remain recoverable")
        let blocked=DiaryStore(directory:root.appendingPathComponent("blocked-history/MyDay"));blocked.update {$0=original}
        let before=try blocked.allDays();try Data("blocked".utf8).write(to:blocked.directory.appendingPathComponent("journal-history"))
        blocked.update {$0.blocks=[]}
        let retained=try blocked.allDays()
        try MacVerification.check(blocked.error != nil && retained.values.first?.blocks.first?.text==before.values.first?.blocks.first?.text,"history write failure retains original diary")
        print("Journal tools: history, recovery, report and XP invariants passed")
    }
}
