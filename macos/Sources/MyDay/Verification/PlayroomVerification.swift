import AppKit
import WebKit

@MainActor enum PlayroomVerification {
    static func script(_ web:WKWebView,_ source:String)async throws->Any? {try await withCheckedThrowingContinuation {continuation in web.evaluateJavaScript(source){result,error in if let error{continuation.resume(throwing:error)}else{continuation.resume(returning:result)}}}}
    static func run()async throws {
        let root=FileManager.default.temporaryDirectory.appendingPathComponent("MyDay-playroom-\(UUID())")
        defer{try? FileManager.default.removeItem(at:root)}
        let bridge=PlayroomBridge(directory:root);let web=PlayroomWeb.makeWebView(bridge)
        let window=NSWindow(contentRect:NSRect(x:0,y:0,width:1000,height:800),styleMask:[.titled],backing:.buffered,defer:false);window.contentView=web;window.makeKeyAndOrderFront(nil)
        defer{web.configuration.userContentController.removeScriptMessageHandler(forName:"myday");window.orderOut(nil)}
        var ready=false
        for _ in 0..<100 {try await Task.sleep(nanoseconds:100_000_000);if (try? await script(web,"document.querySelectorAll('#cards .card').length")) as? Int==9{ready=true;break}}
        try MacVerification.check(ready,"native WKWebView offline playroom loads its nine cards")
        for (index,id) in ["snack","sling","fish","jump","bake","blocks","block-puzzle"].enumerated(){
            _ = try await script(web,"document.querySelectorAll('#cards .card')[\(index)].click()")
            let current=try await script(web,"MyDayPlayroom.snapshot().id") as? String;try MacVerification.check(current==id,"native game opens: "+id)
            _ = try await script(web,"MyDayPlayroom.pause()")
            let paused=try await script(web,"MyDayPlayroom.snapshot().state") as? String
            try MacVerification.check(paused=="paused","native game pauses on focus loss")
            _ = try await script(web,"window.confirm=()=>true;document.getElementById('back').click()")
        }
        // Exercise the actual persistent bridge twice with one round token.
        _ = try await script(web,"window.webkit.messageHandlers.myday.postMessage(JSON.stringify({type:'score',token:'test',round:'native-test',id:'snack',score:30}));window.webkit.messageHandlers.myday.postMessage(JSON.stringify({type:'score',token:'test',round:'native-test',id:'snack',score:30}));")
        for _ in 0..<40 {if FileManager.default.fileExists(atPath:root.appendingPathComponent("games/scores.json").path){break};try await Task.sleep(nanoseconds:50_000_000)}
        let raw=try JSONSerialization.jsonObject(with:Data(contentsOf:root.appendingPathComponent("games/scores.json"))) as! [String:Any];let rows=raw["Entries"] as! [[String:Any]]
        try MacVerification.check(rows.count==1 && rows[0]["Best"] as? Int==30 && rows[0]["Plays"] as? Int==1,"native bridge persists a finished round exactly once")
        try FileManager.default.createDirectory(atPath:"macos/build",withIntermediateDirectories:true)
        let image:NSImage=try await withCheckedThrowingContinuation{continuation in web.takeSnapshot(with:nil){image,error in if let image{continuation.resume(returning:image)}else{continuation.resume(throwing:error ?? MacVerification.Failure.check("playroom snapshot"))}}}
        guard let cg=image.cgImage(forProposedRect:nil,context:nil,hints:nil),let png=NSBitmapImageRep(cgImage:cg).representation(using:.png,properties:[:]) else{throw MacVerification.Failure.check("native game image")}
        try png.write(to:URL(fileURLWithPath:"macos/build/preview-playroom.png"))
        print("Native Mac playroom: seven games, focus pause and persisted scores passed")
    }
}
