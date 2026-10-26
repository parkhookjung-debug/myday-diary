import Foundation
import SwiftUI
import AppKit
import Security
import CommonCrypto
struct PinState:Codable {var Salt="";var Hash="";var Failures=0;var RetryAfter:Double=0}
struct PinVault {
 let root:URL
 var url:URL {root.appendingPathComponent("screen-lock.json")}
 func state()throws->PinState?{guard FileManager.default.fileExists(atPath:url.path)else{return nil};let bytes=try Data(contentsOf:url);guard bytes.count<=4096 else{throw WindowsCompatibility.fail("잠금 설정을 읽지 못했어요.")};let s=try JSONDecoder().decode(PinState.self,from:bytes);guard Data(base64Encoded:s.Salt)?.count==16,Data(base64Encoded:s.Hash)?.count==32,(0...5).contains(s.Failures),s.RetryAfter>=0 else{throw WindowsCompatibility.fail("잠금 설정이 손상되었어요. 기록을 열지 않았어요.")};return s}
 func enabled()throws->Bool{try state() != nil}
 static func valid(_ pin:String)->Bool{(6...12).contains(pin.utf8.count)&&pin.utf8.allSatisfy{(48...57).contains($0)}}
 static func derive(_ pin:String,_ salt:Data)throws->Data {let password=Array(pin.utf8);var output=Data(count:32);let status=password.withUnsafeBytes{p in salt.withUnsafeBytes{s in output.withUnsafeMutableBytes{o in CCKeyDerivationPBKDF(CCPBKDFAlgorithm(kCCPBKDF2),p.bindMemory(to:Int8.self).baseAddress,password.count,s.bindMemory(to:UInt8.self).baseAddress,salt.count,CCPseudoRandomAlgorithm(kCCPRFHmacAlgSHA256),100000,o.bindMemory(to:UInt8.self).baseAddress,32)}}};guard status==kCCSuccess else{throw WindowsCompatibility.fail("PIN을 확인하지 못했어요.")};return output}
 private func save(_ state:PinState)throws{try FileManager.default.createDirectory(at:root,withIntermediateDirectories:true);try JSONEncoder().encode(state).write(to:url,options:.atomic)}
 func verify(_ pin:String)throws->Bool{guard var s=try state()else{return true};guard Date().timeIntervalSince1970>=s.RetryAfter else{throw WindowsCompatibility.fail("5회 실패하면 30초 후 다시 시도할 수 있어요.")};let digest=try Self.derive(pin,Data(base64Encoded:s.Salt)!);let expected=Data(base64Encoded:s.Hash)!;var difference:UInt8=0;for i in 0..<32{difference |= digest[i]^expected[i]};if difference==0{s.Failures=0;s.RetryAfter=0;try save(s);return true};s.Failures+=1;if s.Failures>=5{s.Failures=0;s.RetryAfter=Date().timeIntervalSince1970+30};try save(s);return false}
 func set(old:String,pin:String)throws{guard try verify(old)else{throw WindowsCompatibility.fail("현재 PIN이 맞지 않아요.")};guard Self.valid(pin)else{throw WindowsCompatibility.fail("PIN은 숫자 6~12자리예요.")};var salt=Data(count:16);let status=salt.withUnsafeMutableBytes{SecRandomCopyBytes(kSecRandomDefault,16,$0.baseAddress!)};guard status==errSecSuccess else{throw WindowsCompatibility.fail("PIN 설정을 준비하지 못했어요.")};try save(PinState(Salt:salt.base64EncodedString(),Hash:try Self.derive(pin,salt).base64EncodedString()))}
 func disable(_ old:String)throws{guard try verify(old)else{throw WindowsCompatibility.fail("현재 PIN이 맞지 않아요.")};if FileManager.default.fileExists(atPath:url.path){try FileManager.default.removeItem(at:url)}}
}
@MainActor final class MacPrivacy:ObservableObject {
 static let shared=MacPrivacy()
 @Published var locked=false;@Published var enabled=false;@Published var error=""
 var touched=Date();private var monitor:Any?
 let vault:PinVault
 init(){vault=PinVault(root:FileManager.default.urls(for:.applicationSupportDirectory,in:.userDomainMask)[0].appendingPathComponent("MyDay",isDirectory:true));do{enabled=try vault.enabled();locked=enabled}catch{locked=true;self.error="잠금 설정을 읽지 못했어요. 기록을 열지 않았어요."}}
 func refresh()throws{enabled=try vault.enabled();touch()}
 func touch(){touched=Date()}
 func watch(){if monitor==nil{monitor=NSEvent.addLocalMonitorForEvents(matching:[.keyDown,.mouseMoved,.leftMouseDown,.rightMouseDown,.leftMouseDragged,.scrollWheel]){[weak self] e in self?.touch();return e}}}
 func tick(){if enabled&&Date().timeIntervalSince(touched)>=300{locked=true}}
 func authorize()->Bool{!locked}
}
struct PrivacyRoot:View {
 @ObservedObject var store:DiaryStore
 @ObservedObject private var privacy=MacPrivacy.shared
 @State private var pin="";@State private var busy=false
 private let clock=Timer.publish(every:2,on:.main,in:.common).autoconnect()
 var body:some View {Group{if privacy.locked{VStack(spacing:20){Text("나만의 하루를 열어 볼까요?").font(.title);SecureField("이 기기의 숫자 PIN",text:$pin).frame(width:280);Text(privacy.error).foregroundStyle(.red);Button(busy ? "확인 중…":"잠금 해제"){busy=true;let input=pin;let vault=privacy.vault;Task{do{let correct=try await Task.detached{try vault.verify(input)}.value;if correct{privacy.locked=false;privacy.error="";privacy.touch();pin=""}else{privacy.error="PIN이 맞지 않아요."}}catch{privacy.error=error.localizedDescription};busy=false}}.disabled(busy)}.frame(maxWidth:.infinity,maxHeight:.infinity).background(DesktopPalette.sidebar)}else if !store.loaded || !store.profileLoaded || !store.metadataLoaded{RecoveryView(store:store)}else{DiaryView(store:store)}}.onAppear{privacy.watch()}.onReceive(clock){_ in privacy.tick()}}
}
