import Foundation

struct DiaryFormatPreset:Codable {
    var format="myday-diary-format"
    var version=1
    var blocks:[DiaryBlock]
    var layoutMode:String
    var pageStyle:String

    init(entry:DiaryEntry){
        blocks=entry.blocks.map(Self.emptyCopy)
        layoutMode=entry.layoutMode
        pageStyle=entry.pageStyle
    }

    func validate()throws{
        guard format=="myday-diary-format",version==1 else{throw CocoaError(.fileReadCorruptFile)}
        let candidate=makeEntry()
        try MacBackup(days:["2000-01-01":candidate],companion:CompanionProfile()).validate()
    }

    func makeEntry()->DiaryEntry{
        var result=DiaryEntry()
        result.blocks=blocks.map(Self.emptyCopy)
        result.layoutMode=layoutMode
        result.pageStyle=pageStyle
        return result
    }

    func applying(to source:DiaryEntry)throws->DiaryEntry{
        var result=makeEntry()
        result.theme=source.theme
        result.character=source.character
        result.mood=source.mood
        result.windowsFields=source.windowsFields
        var remaining=source.blocks
        for index in result.blocks.indices{
            guard let match=remaining.firstIndex(where:{Self.sameSlot($0,result.blocks[index])})else{continue}
            let saved=remaining.remove(at:match)
            result.blocks[index].text=saved.text
            result.blocks[index].checked=saved.checked
            result.blocks[index].photo=saved.photo
            result.blocks[index].videoPath=saved.videoPath
            result.blocks[index].windowsID=saved.windowsID
            result.blocks[index].windowsFields=saved.windowsFields
        }
        for block in remaining where Self.hasContent(block){try DiaryLayout.append(block,to:&result)}
        return result
    }

    private static func emptyCopy(_ source:DiaryBlock)->DiaryBlock{
        var block=DiaryBlock(kind:source.kind)
        block.title=source.title
        block.prompt=source.prompt
        block.wide=source.wide
        block.x=source.x
        block.y=source.y
        block.width=source.width
        block.height=source.height
        return block
    }
    private static func sameSlot(_ lhs:DiaryBlock,_ rhs:DiaryBlock)->Bool{lhs.kind==rhs.kind && lhs.title==rhs.title && lhs.prompt==rhs.prompt}
    private static func hasContent(_ block:DiaryBlock)->Bool{!block.text.trimmingCharacters(in:.whitespacesAndNewlines).isEmpty || block.checked || block.photo != nil || block.videoPath != nil}
}

@MainActor extension DiaryStore {
    private var formatURL:URL{directory.appendingPathComponent("diary-format.json")}

    func savedFormat()throws->DiaryFormatPreset?{
        guard FileManager.default.fileExists(atPath:formatURL.path)else{return nil}
        let preset=try JSONDecoder().decode(DiaryFormatPreset.self,from:Data(contentsOf:formatURL))
        try preset.validate()
        return preset
    }

    func rememberCurrentFormat(){
        rememberFormat(DiaryFormatPreset(entry:entry))
    }

    func rememberTemplate(_ template:JournalTemplate,width:Int=800){
        do{
            var templateEntry=DiaryEntry()
            try DiaryLayout.apply(template,to:&templateEntry,width:width)
            rememberFormat(DiaryFormatPreset(entry:templateEntry))
        }catch{self.error="기본 일기 형식을 저장하지 못했어요. \(error.localizedDescription)"}
    }

    private func rememberFormat(_ preset:DiaryFormatPreset){
        do{
            try preset.validate()
            try FileManager.default.createDirectory(at:directory,withIntermediateDirectories:true)
            let encoder=JSONEncoder();encoder.outputFormatting=[.prettyPrinted,.sortedKeys]
            try SafeStorage.write(encoder.encode(preset),to:formatURL){data in
                let decoded=try JSONDecoder().decode(DiaryFormatPreset.self,from:data)
                try decoded.validate()
            }
            error=nil
        }catch{self.error="기본 일기 형식을 저장하지 못했어요. \(error.localizedDescription)"}
    }
}
