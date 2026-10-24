import SwiftUI
import AppKit
import CoreImage
import UniformTypeIdentifiers

struct NativeDeviceLinkView:View {
    @ObservedObject var store:DiaryStore
    @State private var addresses:[String]=[]
    @State private var host=""
    @State private var address=""
    @State private var server:MacDeviceLink?
    @State private var invitation=""
    @State private var message="연결할 때만 같은 Wi-Fi에 임시로 열려요."
    @State private var busy=false
    var body:some View {
        ScrollView {VStack(alignment:.leading,spacing:16){
            Text("같은 Wi-Fi에서 기기 연결").font(.title2)
            Text("앱에서 QR을 만들거나 연결 주소를 입력해요. Python 설치 없이 10분 동안 연결합니다.")
            if addresses.isEmpty{Text("Wi-Fi의 사설 IPv4 주소를 찾지 못했어요.").foregroundStyle(.secondary)}
            Picker("이 기기 주소",selection:$host){ForEach(addresses,id:\.self){Text($0).tag($0)}}.disabled(server != nil)
            Button("이 기기의 연결 QR 만들기"){start()}.disabled(busy || host.isEmpty)
            if !invitation.isEmpty {
                if let image=qr(invitation){Image(nsImage:image).interpolation(.none).resizable().frame(width:235,height:235).accessibilityLabel("기기 연결 QR")}
                Text(invitation).font(.caption).textSelection(.enabled)
                TimelineView(.periodic(from:Date(),by:1)){_ in Text("남은 연결 시간 \(server?.remaining ?? 0)초")}
                HStack{Button("주소 복사"){NSPasteboard.general.clearContents();NSPasteboard.general.setString(invitation,forType:.string)};Button("연결 닫기"){stop()}}
                Button("도착한 기록 확인"){if let bytes=server?.incoming{store.importLinkedData(bytes);message=store.error ?? "가져오기를 확인했어요."}else{message="아직 도착한 기록이 없어요."}}
            }
            Divider();Text("다른 기기의 연결 주소로 받기").font(.headline)
            TextField("http://192.168.…/#…",text:$address).textFieldStyle(.roundedBorder)
            HStack{Button("주소 붙여넣기"){address=NSPasteboard.general.string(forType:.string) ?? ""};Button("QR 이미지 읽기"){readQR()}}
            Button("이 주소에서 기록 받기"){let text=address;busy=true;Task{@MainActor in defer{busy=false};do{let data=try await DeviceAddress.download(text);store.importLinkedData(data);message=store.error ?? "가져오기를 확인했어요."}catch{message=error.localizedDescription}}}.disabled(busy)
            Text(message).font(.callout).foregroundStyle(DesktopPalette.olive)
            Text("연결 창을 닫으면 전송도 닫혀요. 신뢰하는 같은 Wi-Fi에서 사용하세요.").font(.caption)
        }.padding(24).frame(maxWidth:.infinity,alignment:.leading)}.onAppear{addresses=DeviceAddress.addresses();host=addresses.first ?? ""}.onDisappear{stop()}
    }
    private func start(){store.save();guard store.error==nil else{message=store.error!;return};stop();busy=true
        Task{@MainActor in defer{busy=false};do{let data=try WindowsCompatibility.encode(MacBackup(days:store.allDays(),companion:store.companion,windowsMetadata:store.windowsMetadata));let candidate=try MacDeviceLink(host:host,data:data);server=candidate;invitation=try await candidate.start();message="다른 기기에서 QR을 스캔하거나 주소를 입력해 주세요."}catch{stop();message=error.localizedDescription}}
    }
    private func stop(){server?.close();server=nil;invitation=""}
    private func qr(_ text:String)->NSImage?{guard let filter=CIFilter(name:"CIQRCodeGenerator",parameters:["inputMessage":Data(text.utf8),"inputCorrectionLevel":"M"]),let image=filter.outputImage?.transformed(by:CGAffineTransform(scaleX:8,y:8)),let cg=CIContext().createCGImage(image,from:image.extent) else{return nil};return NSImage(cgImage:cg,size:NSSize(width:CGFloat(cg.width),height:CGFloat(cg.height)))}
    private func readQR(){let panel=NSOpenPanel();panel.allowedContentTypes=[.image];guard panel.runModal() == .OK,let url=panel.url else{return};do{let bytes=try Data(contentsOf:url);guard bytes.count<=20*1024*1024,let image=CIImage(data:bytes),image.extent.width*image.extent.height<=50_000_000,let detector=CIDetector(ofType:CIDetectorTypeQRCode,context:CIContext(),options:[CIDetectorAccuracy:CIDetectorAccuracyHigh]),let feature=detector.features(in:image).first as? CIQRCodeFeature,let text=feature.messageString else{throw WindowsCompatibility.fail("MyDay QR을 찾지 못했어요.")};_=try DeviceAddress.parse(text);address=text;message="QR 주소를 읽었어요. 기록 받기를 눌러 주세요."}catch{message=error.localizedDescription}}
}

struct AlbumPhoto:Identifiable {let date:String;let block:DiaryBlock;var id:String{date+"/"+block.id.uuidString}}
struct PhotoAlbumView:View {
    @ObservedObject var store:DiaryStore
    var onOpen:(Date)->Void
    @Environment(\.dismiss) var dismiss
    @State private var month=Date()
    @State private var photos:[AlbumPhoto]=[]
    @State private var error=""
    private let columns=[GridItem(.adaptive(minimum:160),spacing:14)]
    var body:some View {
        VStack(alignment:.leading,spacing:16){
            HStack{Text("사진 모아보기").font(.title2);Spacer();Button("닫기"){dismiss()}}
            HStack{Button("‹ 이전 달"){shift(-1)};Text(month.formatted(.dateTime.year().month())).font(.headline);Button("다음 달 ›"){shift(1)};Spacer();Text("사진 \(photos.count)장")}
            if !error.isEmpty{Text(error).foregroundStyle(.red)}
            if photos.isEmpty{Text("이번 달은 아직 사진이 없어요. 일기에 사진을 추가해 보세요.").frame(maxWidth:.infinity,maxHeight:.infinity)}
            else{ScrollView{LazyVGrid(columns:columns,spacing:14){ForEach(photos){item in Button{let f=DateFormatter();f.locale=Locale(identifier:"en_US_POSIX");f.calendar=Calendar(identifier:.gregorian);f.dateFormat="yyyy-MM-dd";if let date=f.date(from:item.date){onOpen(date);if store.error==nil{dismiss()}}}label:{VStack(alignment:.leading,spacing:8){if let data=item.block.photo,let image=NSImage(data:data){Image(nsImage:image).resizable().scaledToFill().frame(height:145).clipped()};Text(item.date).font(.caption);Text(item.block.text).lineLimit(2).font(.caption)}.padding(10).frame(maxWidth:.infinity,alignment:.leading).background(DesktopPalette.paper,in:RoundedRectangle(cornerRadius:16))}.buttonStyle(.plain)}}}}
        }.padding(22).frame(minWidth:700,minHeight:580).background(DesktopPalette.sidebar).onAppear{month=store.date;refresh()}
    }
    private func shift(_ amount:Int){if let next=Calendar(identifier:.gregorian).date(byAdding:.month,value:amount,to:month),Calendar(identifier:.gregorian).component(.year,from:next)>0{month=next;refresh()}}
    private func refresh(){do{let f=DateFormatter();f.calendar=Calendar(identifier:.gregorian);f.locale=Locale(identifier:"en_US_POSIX");f.dateFormat="yyyy-MM";let prefix=f.string(from:month)+"-";photos=try store.allDays().filter{$0.key.hasPrefix(prefix)}.sorted{$0.key>$1.key}.flatMap{key,entry in entry.blocks.filter{$0.kind == .photo && $0.photo != nil}.map{AlbumPhoto(date:key,block:$0)}}}catch{self.error=error.localizedDescription}}
}

struct AutomaticBackupView:View {
    @ObservedObject var store:DiaryStore
    @State private var state=AutomaticBackupState()
    @State private var files:[URL]=[]
    @State private var error=""
    var body:some View {
        ScrollView{VStack(alignment:.leading,spacing:18){
            Text("전체 기록 자동 백업").font(.title2)
            Toggle("저장한 날 하루 한 번 전체 백업",isOn:Binding(get:{state.enabled},set:{v in do{try AutomaticBackup.setEnabled(store.directory,v);refresh()}catch{self.error=error.localizedDescription}}))
            Text("최근 3일분을 기기 안에 보관해요. 앱을 종료한 동안에는 실행하지 않아요. 백업 실패가 일기 저장을 막지 않아요.")
            Text("마지막 백업: \(state.lastSuccess.isEmpty ? "아직 없음" : state.lastSuccess)\n보관 \(files.count) / 3일분")
            if !state.error.isEmpty{Text("백업 실패: "+state.error).foregroundStyle(.red)}
            if !error.isEmpty{Text(error).foregroundStyle(.red)}
            Button("지금 전체 백업"){store.save();if store.error==nil{store.captureAutomaticBackup(force:true)};refresh()}
            Button("백업 폴더 열기"){do{try FileManager.default.createDirectory(at:AutomaticBackup.folder(store.directory),withIntermediateDirectories:true);NSWorkspace.shared.open(AutomaticBackup.folder(store.directory))}catch{self.error=error.localizedDescription}}
            ForEach(files,id:\.path){file in HStack{Text(file.deletingPathExtension().lastPathComponent);Spacer();Button("복원 검토"){do{let data=try Data(contentsOf:file);store.importLinkedData(data);refresh()}catch{self.error=error.localizedDescription}}}}
        }.padding(24).frame(maxWidth:.infinity,alignment:.leading)}.onAppear{refresh()}
    }
    private func refresh(){state=AutomaticBackup.state(store.directory);do{files=try AutomaticBackup.files(store.directory)}catch{self.error=error.localizedDescription}}
}
