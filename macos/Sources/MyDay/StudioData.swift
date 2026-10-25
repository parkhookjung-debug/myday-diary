import Foundation
struct RoomItem:Codable,Identifiable {var Id=UUID().uuidString;var Kind="plant";var X=400;var Y=500;var id:String{Id}}
struct StudioNote:Codable,Identifiable {var Date="";var Tags:[String]=[];var Favourite=false;var id:String{Date}}
struct StudioData:Codable {
 var RoomTheme=0;var Furniture:[RoomItem]=[];var Pages:[StudioNote]=[]
 func validate()throws {guard (0...2).contains(RoomTheme),Furniture.count<=24,Pages.count<=50000,Set(Furniture.map(\.Id)).count==Furniture.count,Set(Pages.map(\.Date)).count==Pages.count else{throw WindowsCompatibility.fail("꾸미기 자료의 크기나 배경이 올바르지 않아요.")};for f in Furniture{guard !f.Id.isEmpty,f.Id.utf16.count<=64,StudioCatalog.furnitureKinds.contains(f.Kind),(0...900).contains(f.X),(0...900).contains(f.Y)else{throw WindowsCompatibility.fail("가구의 종류나 위치가 올바르지 않아요.")}};for p in Pages{guard MacBackup.validDate(p.Date),p.Tags.count<=10,Set(p.Tags.map{$0.lowercased()}).count==p.Tags.count,p.Tags.allSatisfy({!$0.trimmingCharacters(in:.whitespacesAndNewlines).isEmpty&&$0.utf16.count<=20&&$0.rangeOfCharacter(from:.controlCharacters)==nil})else{throw WindowsCompatibility.fail("태그는 최대 10개·각 20자예요.")}}}
 func value()throws->JSONValue{try validate();return try JSONDecoder().decode(JSONValue.self,from:JSONEncoder().encode(self))}
 static func decode(_ value:JSONValue?)throws->StudioData? {guard let value,value != .null else{return nil};let result=try JSONDecoder().decode(StudioData.self,from:JSONEncoder().encode(value));try result.validate();return result}
 static func merge(_ a:StudioData?,_ b:StudioData?)throws->StudioData?{guard var b else{return a};var pages=Dictionary(uniqueKeysWithValues:(a?.Pages ?? []).map{($0.Date,$0)});for p in b.Pages{pages[p.Date]=p};b.Pages=pages.values.sorted{$0.Date<$1.Date};try b.validate();return b}
 func note(_ date:String)->StudioNote{Pages.first{$0.Date==date} ?? StudioNote(Date:date)}
 mutating func setNote(_ note:StudioNote)throws{var next=self;next.Pages.removeAll{$0.Date==note.Date};next.Pages.append(note);try next.validate();self=next}
 static func question(_ date:Date)->String{var calendar=Calendar(identifier:.gregorian);calendar.timeZone=TimeZone.current;let day=(calendar.ordinality(of:.day,in:.era,for:date) ?? 1)-1;return StudioCatalog.questions[day%StudioCatalog.questions.count]}
}
@MainActor extension DiaryStore {
 var studio:StudioData {(try? StudioData.decode(windowsMetadata?.fields["Studio"])) ?? StudioData()}
 func commitStudio(_ studio:StudioData){save();guard error==nil else{return};commitShared{s in s.fields["Studio"]=try studio.value();s.fields["Version"] = .number(Double(max(9,s.fields["Version"]?.int ?? 4)))}}
}
