import SwiftUI
import WebKit
import UniformTypeIdentifiers
import ImageIO

private struct GameScore:Codable {var Id:String;var Best:Int;var Plays:Int;var Last:Int}
private struct GameScoreBook:Codable {var Version=1;var Entries:[GameScore]=[]}

final class PlayroomBridge:NSObject,WKScriptMessageHandler,WKNavigationDelegate {
    weak var web:WKWebView?
    let directory:URL
    var seen=Set<String>()
    var acceptedInitialNavigation=false
    static let ids=Set(["snack","sling","fish","jump","bake","blocks","block-puzzle"])
    init(directory:URL){self.directory=directory}
    private func valid(_ id:String)->Bool {if Self.ids.contains(id){return true};guard id.hasPrefix("snack-daily-v1:") else{return false};let key=String(id.dropFirst(15));return MacBackup.validDate(key)}
    private var file:URL{directory.appendingPathComponent("games/scores.json")}
    private func read() throws->GameScoreBook {
        guard FileManager.default.fileExists(atPath:file.path) else{return GameScoreBook()}
        let attributes=try FileManager.default.attributesOfItem(atPath:file.path);guard ((attributes[.size] as? NSNumber)?.intValue ?? Int.max) <= 65536 else {throw WindowsCompatibility.fail("게임 기록 파일이 너무 커요.")}
        let book=try JSONDecoder().decode(GameScoreBook.self,from:Data(contentsOf:file))
        guard book.Version==1,book.Entries.count<=40,Set(book.Entries.map(\.Id)).count==book.Entries.count,book.Entries.allSatisfy({valid($0.Id) && (0...1000000).contains($0.Best) && (0...$0.Best).contains($0.Last) && (1...1000000).contains($0.Plays)}) else{throw WindowsCompatibility.fail("게임 기록 형식을 확인해 주세요.")}
        return book
    }
    private func reply(_ token:String,_ ok:Bool,_ message:String){let args:[Any]=[token,ok,message];guard let json=try? JSONSerialization.data(withJSONObject:args),let value=String(data:json,encoding:.utf8) else{return};web?.evaluateJavaScript("MyDayPlayroom.reply(...\(value))",completionHandler:nil)}
    func userContentController(_ controller:WKUserContentController,didReceive message:WKScriptMessage){
        guard message.frameInfo.isMainFrame,let text=message.body as? String,text.utf8.count<=2100000,let bytes=text.data(using:.utf8),let msg=(try? JSONSerialization.jsonObject(with:bytes)) as? [String:Any] else{return}
        let token=msg["token"] as? String ?? "";guard token.count<=150 else{return}
        do {switch msg["type"] as? String {
        case "haptic":NSHapticFeedbackManager.defaultPerformer.perform(.alignment,performanceTime:.now)
        case "ready":
            let book=try read();let records=Dictionary(uniqueKeysWithValues:book.Entries.map{($0.Id,$0.Best)})
            guard let atlas=SangmonImages.image("sangmon/original.png"),let cg=atlas.cgImage(forProposedRect:nil,context:nil,hints:nil),let png=NSBitmapImageRep(cgImage:cg).representation(using:.png,properties:[:]) else{throw WindowsCompatibility.fail("상몬 이미지를 열지 못했어요.")}
            let data=try JSONSerialization.data(withJSONObject:["records":records,"atlas":"data:image/png;base64,"+png.base64EncodedString()]);let bootstrap=String(decoding:data,as:UTF8.self)
            web?.evaluateJavaScript("MyDayPlayroom.initialize(\(bootstrap))",completionHandler:nil)
        case "score":
            guard let round=msg["round"] as? String,round.count<=100,!round.isEmpty,round.range(of:"^[A-Za-z0-9-]+$",options:.regularExpression) != nil,let id=msg["id"] as? String,valid(id),let number=msg["score"] as? NSNumber,CFGetTypeID(number) != CFBooleanGetTypeID(),number.doubleValue.rounded()==number.doubleValue,(0...1000000).contains(number.doubleValue) else {throw WindowsCompatibility.fail("게임 점수를 확인해 주세요.")}
            if !seen.contains(round){let score=number.intValue;var book=try read();let old=book.Entries.first{$0.Id==id};book.Entries.removeAll{$0.Id==id};book.Entries.append(GameScore(Id:id,Best:max(score,old?.Best ?? 0),Plays:min(1000000,(old?.Plays ?? 0)+1),Last:score))
                book.Entries=book.Entries.filter{Self.ids.contains($0.Id)}+Array(book.Entries.filter{!Self.ids.contains($0.Id)}.sorted{$0.Id>$1.Id}.prefix(31))
                try FileManager.default.createDirectory(at:file.deletingLastPathComponent(),withIntermediateDirectories:true);try JSONEncoder().encode(book).write(to:file,options:.atomic);if seen.count>=128{seen.removeAll()};seen.insert(round)
            };reply(token,true,"점수를 저장했어요.")
        case "image":
            guard let raw=msg["png"] as? String,raw.utf8.count<=2000000,let data=Data(base64Encoded:raw),let source=CGImageSourceCreateWithData(data as CFData,nil),let properties=CGImageSourceCopyPropertiesAtIndex(source,0,nil) as? [CFString:Any],properties[kCGImagePropertyPixelWidth] as? Int==960,properties[kCGImagePropertyPixelHeight] as? Int==720,CGImageSourceGetType(source) as String? == UTType.png.identifier else{throw WindowsCompatibility.fail("결과 이미지 형식을 확인해 주세요.")}
            let panel=NSSavePanel();panel.allowedContentTypes=[.png];panel.nameFieldStringValue="상몬-게임-결과.png"
            if panel.runModal() == .OK,let target=panel.url{try data.write(to:target,options:.atomic);reply(token,true,"결과 이미지를 저장했어요.")}else{reply(token,false,"이미지 저장을 취소했어요.")}
        default:throw WindowsCompatibility.fail("알 수 없는 게임 요청이에요.")
        }}catch{reply(token,false,error.localizedDescription);if token.isEmpty{let json=String(decoding:(try? JSONSerialization.data(withJSONObject:[error.localizedDescription])) ?? Data("[]".utf8),as:UTF8.self);web?.evaluateJavaScript("document.getElementById('status').textContent=\(json)[0]",completionHandler:nil)}}
    }
    func webView(_ webView:WKWebView,decidePolicyFor action:WKNavigationAction,decisionHandler:@escaping(WKNavigationActionPolicy)->Void){if !acceptedInitialNavigation,action.targetFrame?.isMainFrame==true,action.request.url?.absoluteString=="about:blank"{acceptedInitialNavigation=true;decisionHandler(.allow)}else{decisionHandler(.cancel)}}
}

struct PlayroomWeb:NSViewRepresentable {
    let directory:URL
    func makeCoordinator()->PlayroomBridge{PlayroomBridge(directory:directory)}
    func makeNSView(context:Context)->WKWebView {
        Self.makeWebView(context.coordinator)
    }
    static func makeWebView(_ bridge:PlayroomBridge)->WKWebView {
        let settings=WKWebViewConfiguration();settings.websiteDataStore = .nonPersistent();settings.preferences.javaScriptCanOpenWindowsAutomatically=false;settings.userContentController.add(bridge,name:"myday")
        let web=WKWebView(frame:.zero,configuration:settings);bridge.web=web;web.navigationDelegate=bridge
        let url=Bundle.main.url(forResource:"playroom",withExtension:"html") ?? Bundle.module.url(forResource:"playroom",withExtension:"html")
        if let url,let html=try? String(contentsOf:url,encoding:.utf8){web.loadHTMLString(html,baseURL:nil)}
        return web
    }
    func updateNSView(_ view:WKWebView,context:Context){}
    static func dismantleNSView(_ view:WKWebView,coordinator:PlayroomBridge){view.evaluateJavaScript("MyDayPlayroom.pause()",completionHandler:nil);view.stopLoading();view.configuration.userContentController.removeScriptMessageHandler(forName:"myday");view.navigationDelegate=nil;coordinator.web=nil}
}

struct PlayroomView:View {
    @ObservedObject var store:DiaryStore
    @Environment(\.dismiss) var dismiss
    var body:some View{VStack(spacing:0){HStack{Text("상몬 놀이터").font(.headline);Spacer();Button("닫기"){dismiss()}}.padding(12);PlayroomWeb(directory:store.directory)}.frame(minWidth:780,minHeight:650)}
}
