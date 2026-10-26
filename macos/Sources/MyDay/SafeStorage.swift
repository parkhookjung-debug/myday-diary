import Foundation

enum SafeStorage {
    static func write(_ data:Data,to url:URL,validate:(Data)throws->Void)throws {
        try validate(data)
        if FileManager.default.fileExists(atPath:url.path) {
            let old=try Data(contentsOf:url);try validate(old)
            try old.write(to:url.appendingPathExtension("previous"),options:.atomic)
        }
        try data.write(to:url,options:.atomic)
    }
    static func available(_ url:URL,validate:(Data)throws->Void)->Bool {
        do {let data=try Data(contentsOf:url.appendingPathExtension("previous"));try validate(data);return true}catch{return false}
    }
    static func restore(_ url:URL,validate:(Data)throws->Void)throws {
        let bytes=try Data(contentsOf:url.appendingPathExtension("previous"));try validate(bytes)
        if FileManager.default.fileExists(atPath:url.path) {try FileManager.default.copyItem(at:url,to:url.appendingPathExtension("damaged-"+UUID().uuidString))}
        try bytes.write(to:url,options:.atomic)
    }
}
@MainActor extension DiaryStore {
    private var recoverableFiles:[(URL,(Data)throws->Void)] {
        [(directory.appendingPathComponent(Self.todayKey(date)+".json"),{data in
            let entry=try JSONDecoder().decode(DiaryEntry.self,from:data);try MacBackup(days:["2000-01-01":entry],companion:CompanionProfile()).validate()
        }),(directory.appendingPathComponent("companion.json"),{data in
            try MacBackup(days:[:],companion:JSONDecoder().decode(CompanionProfile.self,from:data)).validate()
        }),(directory.appendingPathComponent("windows-metadata.json"),{data in
            try WindowsCompatibility.validateMetadata(JSONDecoder().decode(WindowsMetadata.self,from:data))
        })]
    }
    private var brokenFiles:[(URL,(Data)throws->Void)] {recoverableFiles.filter{url,validate in
        guard FileManager.default.fileExists(atPath:url.path) else{return FileManager.default.fileExists(atPath:url.appendingPathExtension("previous").path)}
        do {try validate(Data(contentsOf:url));return false}catch{return true}
    }}
    var recoveryAvailable:Bool {let files=brokenFiles;return !files.isEmpty && files.allSatisfy{SafeStorage.available($0.0,validate:$0.1)}}
    func recoverPrevious() {
        do {
            let files=brokenFiles;guard !files.isEmpty,files.allSatisfy({SafeStorage.available($0.0,validate:$0.1)}) else{throw CocoaError(.fileReadCorruptFile)}
            for (url,validate) in files {try SafeStorage.restore(url,validate:validate)}
            load();loadCompanion();loadWindowsMetadata()
        }catch{self.error="복구하지 못했어요. 기존 파일은 보존돼요. \(error.localizedDescription)"}
    }
}
