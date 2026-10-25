import Foundation
import AppKit
@MainActor enum StudioVerification {
 static func check(_ value:Bool,_ message:String)throws{try MacVerification.check(value,message)}
 static func run(root:URL)throws{
  let bytes=try Data(contentsOf:SharedArtCatalog.artURL.appendingPathComponent("fixtures/windows-studio.json"));let book=try WindowsCompatibility.decode(bytes);let studio=try StudioData.decode(book.windowsMetadata?.fields["Studio"])!;try check(studio.RoomTheme==1&&studio.Furniture.count==6&&studio.note("2026-10-25").Tags==["산책","학교"]&&studio.note("2026-10-25").Favourite,"version-nine studio import keeps all metadata")
  let encoded=try WindowsCompatibility.encode(book);if let export=ProcessInfo.processInfo.environment["MYDAY_STUDIO_EXPORT"]{try encoded.write(to:URL(fileURLWithPath:export))};let round=try WindowsCompatibility.decode(encoded);try check(try StudioData.decode(round.windowsMetadata?.fields["Studio"])?.Furniture[5].X==600,"studio round trip keeps normalized furniture positions")
  var prior=StudioData();prior.Pages=[StudioNote(Date:"2026-10-24",Tags:["이전"])];try check(try StudioData.merge(prior,studio)?.Pages.count==2,"studio merge retains notes on other dates")
  var invalid=studio;invalid.Furniture[0].X=901;var rejected=false;do{try invalid.validate()}catch{rejected=true};try check(rejected,"invalid room position rejected")
  let pin=PinVault(root:root.appendingPathComponent("studio-pin"));try check(try !pin.enabled(),"screen lock is opt-in");try pin.set(old:"",pin:"123456");try check(try pin.verify("123456"),"correct PIN unlocks");try check(try !pin.verify("654321"),"wrong PIN rejected");try check(!(try String(contentsOf:pin.url)).contains("123456"),"PIN is never stored as plaintext");try pin.disable("123456");try check(try !pin.enabled(),"PIN removal requires verification")
  let hash=try PinVault.derive("123456",Data(0..<16)).map{String(format:"%02x",$0)}.joined();try check(hash=="3e3d2422f00f2cc1d1bad045819bfb8360117d59c588035c4294f3403ac097a5","PIN matches independent PBKDF2 reference")
  for i in 0..<4{let data=try PhotoDecor.encode(PhotoDecor.sticker(i));try WindowsCompatibility.checkPhoto(data);try check(data.count>0,"portable offline sticker \(i)")}
  let image=PhotoDecor.sticker(0);let crop=PhotoDecor.crop(image,ratio:1);try check(crop.size.width==crop.size.height&&image.size.width==320,"photo crop retains source")
 }
}
