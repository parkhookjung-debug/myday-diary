import Foundation

@MainActor enum FeatureParityVerification {
    static func run(root:URL)throws {
        func check(_ ok:Bool,_ label:String)throws{try MacVerification.check(ok,label)}
        var b=try TodoRules.applying([TodoItem(text:"일기",checked:true,importance:1),TodoItem(text:"산책",importance:2)],to:DiaryBlock(kind:.todo))
        try check(!b.checked,"partly checked list keeps aggregate false")
        b.text="산책\n일기\n책 읽기"
        let items=try TodoRules.items(b);try check(items.map(\.importance)==[2,1,0] && items.map(\.checked)==[false,true,false],"legacy flat edits preserve matching priorities and checks")
        b=try TodoRules.applying(items,to:b)
        var page=DiaryEntry();page.blocks=[b]
        let backup=MacBackup(days:["2026-11-28":page],companion:CompanionProfile())
        let restored=try WindowsCompatibility.decode(WindowsCompatibility.encode(backup))
        try check(try TodoRules.items(restored.days["2026-11-28"]!.blocks[0])==items,"checklist portable backup round trip")
        var malformed=b;malformed.windowsFields?["TodoItems"] = .array([.object(["Text":.string("wrong"),"Checked":.bool(false),"Importance":.string("high")])])
        var rejected=false;do{_ = try TodoRules.items(malformed)}catch{rejected=true};try check(rejected,"malformed checklist importance rejected")
        let leap=Anniversary(Name:"윤년 생일",Date:"2024-02-29",RepeatYearly:true)
        try check(AnniversaryRules.matches(leap,"2028-02-29") && !AnniversaryRules.matches(leap,"2027-02-28"),"yearly leap anniversaries match actual calendar days")
        for path in ["C:\\Videos\\산책.mp4","/Users/test/산책.mov","content://media/external/video/media/5"] {
            var video=DiaryBlock(kind:.video,text:"영상 설명");video.videoPath=path;var entry=DiaryEntry();entry.blocks=[video]
            let raw=try WindowsCompatibility.encode(MacBackup(days:["2026-11-28":entry],companion:CompanionProfile()))
            let restored=try WindowsCompatibility.decode(raw);try check(restored.windowsMetadata?.fields["Version"]?.int==10 && restored.days["2026-11-28"]!.blocks[0].videoPath==path,"portable video reference preserves other platforms")
        }
        for path in ["https://example.com/a.mp4","//server/a.mp4","relative.mp4"] {var invalid=false;do{try WindowsCompatibility.checkVideoPath(path)}catch{invalid=true};try check(invalid,"network and relative video refs rejected")}
        print("Native feature parity: checklist, priorities, anniversaries, portable video: passed")
    }
}
