import Foundation
struct RoomItem:Codable,Identifiable {var Id=UUID().uuidString;var Kind="plant";var X=400;var Y=500;var id:String{Id}}
struct StudioNote:Codable,Identifiable {var Date="";var Tags:[String]=[];var Favourite=false;var id:String{Date}}
struct Anniversary:Codable,Identifiable {var Id=UUID().uuidString;var Name="";var Date="";var RepeatYearly=true;var id:String{Id}}
struct StudioData:Codable {
 var RoomTheme=0;var Furniture:[RoomItem]=[];var Pages:[StudioNote]=[];var Anniversaries:[Anniversary]?=nil
 private func validateAnniversaries()throws {let rows=Anniversaries ?? [];guard rows.count<=StudioCatalog.anniversaryLimit,Set(rows.map(\.Id)).count==rows.count,rows.allSatisfy({!$0.Id.trimmingCharacters(in:.whitespacesAndNewlines).isEmpty&&$0.Id.utf16.count<=64&&!$0.Id.unicodeScalars.contains(where:{$0.value<32 || (127...159).contains($0.value)}) && !$0.Name.trimmingCharacters(in:.whitespacesAndNewlines).isEmpty&&$0.Name.utf16.count<=StudioCatalog.anniversaryNameLength&&!$0.Name.unicodeScalars.contains(where:{$0.value<32 || (127...159).contains($0.value)})&&MacBackup.validDate($0.Date)})else{throw WindowsCompatibility.fail("기념일 이름이나 날짜를 확인해 주세요.")}}
 func validate()throws {try validateAnniversaries();guard (0...2).contains(RoomTheme),Furniture.count<=24,Pages.count<=50000,Set(Furniture.map(\.Id)).count==Furniture.count,Set(Pages.map(\.Date)).count==Pages.count else{throw WindowsCompatibility.fail("꾸미기 자료의 크기나 배경이 올바르지 않아요.")};for f in Furniture{guard !f.Id.isEmpty,f.Id.utf16.count<=64,StudioCatalog.furnitureKinds.contains(f.Kind),(0...900).contains(f.X),(0...900).contains(f.Y)else{throw WindowsCompatibility.fail("가구의 종류나 위치가 올바르지 않아요.")}};for p in Pages{guard MacBackup.validDate(p.Date),p.Tags.count<=10,Set(p.Tags.map{$0.lowercased()}).count==p.Tags.count,p.Tags.allSatisfy({!$0.trimmingCharacters(in:.whitespacesAndNewlines).isEmpty&&$0.utf16.count<=20&&$0.rangeOfCharacter(from:.controlCharacters)==nil})else{throw WindowsCompatibility.fail("태그는 최대 10개·각 20자예요.")}}}
 func value()throws->JSONValue{try validate();return try JSONDecoder().decode(JSONValue.self,from:JSONEncoder().encode(self))}
 static func decode(_ value:JSONValue?)throws->StudioData? {guard let value,value != .null else{return nil};let result=try JSONDecoder().decode(StudioData.self,from:JSONEncoder().encode(value));try result.validate();return result}
 static func merge(_ a:StudioData?,_ b:StudioData?)throws->StudioData?{guard var b else{return a};try a?.validate();try b.validate();var anniversaries=Dictionary(uniqueKeysWithValues:(a?.Anniversaries ?? []).map{($0.Id,$0)});for item in b.Anniversaries ?? []{anniversaries[item.Id]=item};b.Anniversaries=anniversaries.values.sorted{$0.Id<$1.Id};var pages=Dictionary(uniqueKeysWithValues:(a?.Pages ?? []).map{($0.Date,$0)});for p in b.Pages{pages[p.Date]=p};b.Pages=pages.values.sorted{$0.Date<$1.Date};try b.validate();return b}
 func note(_ date:String)->StudioNote{Pages.first{$0.Date==date} ?? StudioNote(Date:date)}
 mutating func setNote(_ note:StudioNote)throws{var next=self;next.Pages.removeAll{$0.Date==note.Date};next.Pages.append(note);try next.validate();self=next}
 static func question(_ date:Date)->String{
  var calendar=Calendar(identifier:.gregorian);calendar.timeZone=TimeZone.current
  let parts=calendar.dateComponents([.year,.month,.day],from:date)
  guard let year=parts.year,let month=parts.month,let dateDay=parts.day,(1...12).contains(month)else{return StudioCatalog.questions[0]}
  let prior=year-1,monthStarts=[0,31,59,90,120,151,181,212,243,273,304,334]
  let leap=month>2 && (year%4==0 && (year%100 != 0 || year%400==0)) ? 1 : 0
  let day=prior*365+prior/4-prior/100+prior/400+monthStarts[month-1]+leap+dateDay-1
  return StudioCatalog.questions[day%StudioCatalog.questions.count]
 }
}
@MainActor extension DiaryStore {
 var studio:StudioData {(try? StudioData.decode(windowsMetadata?.fields["Studio"])) ?? StudioData()}
 func commitStudio(_ studio:StudioData){save();guard error==nil else{return};commitShared{s in s.fields["Studio"]=try studio.value();s.fields["Version"] = .number(Double(max(9,s.fields["Version"]?.int ?? 4)))}}
}
