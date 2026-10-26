import Foundation
@MainActor enum QualityVerification {
 static func run(_ root:URL)throws {
    let directory=root.appendingPathComponent("quality");let store=DiaryStore(directory:directory)
    store.update{$0.blocks.append(DiaryBlock(kind:.text,text:"직전 기록"))};store.update{$0.blocks[0].text="새 기록"}
    let file=directory.appendingPathComponent(DiaryStore.todayKey(store.date)+".json")
    let previous=try Data(contentsOf:file.appendingPathExtension("previous"));try Data("damaged".utf8).write(to:file)
    let broken=DiaryStore(directory:directory);try MacVerification.check(!broken.loaded && broken.recoveryAvailable,"corrupt diary blocks writes and offers validated recovery")
    broken.recoverPrevious();try MacVerification.check(broken.loaded && broken.entry.blocks[0].text=="직전 기록","explicit recovery restores previous entry")
    let recovered=try Data(contentsOf:file);try MacVerification.check(recovered==previous,"recovery retains exact snapshot without awarding XP")
    let archives=try FileManager.default.contentsOfDirectory(at:directory,includingPropertiesForKeys:nil).filter{$0.lastPathComponent.contains(".damaged-")}
    let archived=try Data(contentsOf:archives[0]);try MacVerification.check(archives.count==1 && archived==Data("damaged".utf8),"damaged original preserved separately")
    try Data("bad prior".utf8).write(to:file.appendingPathExtension("previous"));try Data("bad current".utf8).write(to:file)
    let invalid=DiaryStore(directory:directory);try MacVerification.check(!invalid.recoveryAvailable,"invalid prior snapshot cannot be offered")
    invalid.recoverPrevious();let untouched=try Data(contentsOf:file);try MacVerification.check(untouched==Data("bad current".utf8),"invalid recovery does not replace original")
    try MacVerification.check(ExperienceCatalog.titles.count==4 && ExperienceCatalog.bodies[2].contains("65%"),"shared onboarding and transparent economy help loaded")
 }
}
