import Foundation
import AppKit

struct AutomaticBackupState: Codable {
    var enabled=true
    var lastSuccess=""
    var error=""
}
enum AutomaticBackup {
    private static var failures: [String:String]=[:]
    static func folder(_ root: URL)->URL {root.appendingPathComponent("automatic-backups",isDirectory:true)}
    static func state(_ root: URL)->AutomaticBackupState {
        let path=root.appendingPathComponent("automatic-backup-state.json")
        do {var state=FileManager.default.fileExists(atPath:path.path) ? try JSONDecoder().decode(AutomaticBackupState.self,from:Data(contentsOf:path)) : AutomaticBackupState();if let error=failures[root.path]{state.error=error};return state}
        catch {var state=AutomaticBackupState();state.enabled=false;state.error=error.localizedDescription;return state}
    }
    static func saveState(_ root: URL,_ state: AutomaticBackupState)throws {try FileManager.default.createDirectory(at:root,withIntermediateDirectories:true);try JSONEncoder().encode(state).write(to:root.appendingPathComponent("automatic-backup-state.json"),options:.atomic)}
    static func setEnabled(_ root: URL,_ enabled: Bool)throws {var value=state(root);value.enabled=enabled;try saveState(root,value)}
    static func files(_ root: URL)throws->[URL] {
        let dir=folder(root);guard FileManager.default.fileExists(atPath:dir.path) else{return []}
        return try FileManager.default.contentsOfDirectory(at:dir,includingPropertiesForKeys:[.fileSizeKey]).filter{$0.lastPathComponent.range(of:"^daily-[0-9]{4}-[0-9]{2}-[0-9]{2}\\.json$",options:.regularExpression) != nil}.sorted{$0.lastPathComponent>$1.lastPathComponent}
    }
    @discardableResult static func capture(_ root: URL,data: () throws->Data,force: Bool=false,day: String?=nil)->Bool {
        do {
            var state=state(root);if !force && !state.enabled{return false}
            let format=DateFormatter();format.calendar=Calendar(identifier:.gregorian);format.locale=Locale(identifier:"en_US_POSIX");format.dateFormat="yyyy-MM-dd"
            let key=day ?? format.string(from:Date());guard MacBackup.validDate(key) else{throw CocoaError(.fileWriteInvalidFileName)}
            let file=folder(root).appendingPathComponent("daily-"+key+".json")
            if !force && FileManager.default.fileExists(atPath:file.path){return true}
            let bytes=try data();guard bytes.count<=WindowsCompatibility.maxBytes else{throw CocoaError(.fileWriteOutOfSpace)}
            _=try WindowsCompatibility.decode(bytes)
            try FileManager.default.createDirectory(at:folder(root),withIntermediateDirectories:true);try bytes.write(to:file,options:.atomic)
            let saved=try files(root);var count=saved.count
            var total=try saved.reduce(0){try $0+($1.resourceValues(forKeys:[.fileSizeKey]).fileSize ?? 0)}
            for old in saved.reversed(){if count<=JournalPolicy.automaticBackupCount && total<=JournalPolicy.automaticBackupLimitBytes{break};if old==file{continue};let size=try old.resourceValues(forKeys:[.fileSizeKey]).fileSize ?? 0;try FileManager.default.removeItem(at:old);total-=size;count-=1}
            state.lastSuccess=ISO8601DateFormatter().string(from:Date());state.error="";try saveState(root,state);failures.removeValue(forKey:root.path);return true
        }catch {failures[root.path]=error.localizedDescription;var state=state(root);state.error=error.localizedDescription;try? saveState(root,state);return false}
    }
}

extension DiaryStore {
    func captureAutomaticBackup(force: Bool=false) {
        AutomaticBackup.capture(directory,data:{try WindowsCompatibility.encode(MacBackup(days:allDays(),companion:companion,windowsMetadata:windowsMetadata))},force:force)
    }
    func importLinkedData(_ data: Data) {
        save();guard error==nil else{return}
        do {
            let incoming=try WindowsCompatibility.decode(data,companion:companion)
            let current=MacBackup(days:try allDays(),companion:companion,windowsMetadata:windowsMetadata)
            let conflicts=incoming.days.keys.filter{current.days[$0] != nil}.count
            let photos=incoming.days.values.reduce(0){$0+$1.blocks.filter{$0.photo != nil}.count}
            let merged=try WindowsCompatibility.merge(current:current,incoming:incoming)
            let alert=NSAlert();alert.messageText="도착한 기록을 가져올까요?";alert.informativeText="\(incoming.days.count)일 · 사진 \(photos)장\n겹치는 날짜 \(conflicts)일은 받은 내용으로 바뀝니다. 성장·소품은 기존 병합 규칙으로 확인하고 현재 기록은 복구 사본에 보관합니다.";alert.addButton(withTitle:"취소");alert.addButton(withTitle:"가져오기")
            guard alert.runModal() == .alertSecondButtonReturn else{return}
            try restore(merged);load();loadCompanion();loadWindowsMetadata();pendingActivity=false;pendingBaseline=nil
        }catch{self.error="기록을 가져오지 못했어요. "+error.localizedDescription}
    }
}
