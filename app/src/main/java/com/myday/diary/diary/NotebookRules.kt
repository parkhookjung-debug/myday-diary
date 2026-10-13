package com.myday.diary.diary

import com.myday.diary.data.*
import java.time.LocalDate
import java.util.UUID

/** Shared Windows rules. Coordinates are logical units, independent of display density. */
object NotebookRules {
    const val MAX_BLOCKS = 200
    const val MAX_LAYOUTS = 50
    const val MAX_POSITION = 10000
    const val MIN_WIDTH = 300
    const val MAX_WIDTH = 1600
    const val MIN_HEIGHT = 180
    const val MAX_HEIGHT = 1400
    val kinds = setOf("text", "todo", "habit", "emotion", "photo", "photo-slot")
    fun meaningful(b: DiaryBlock) = b.text.isNotBlank() || b.type == "photo" && !b.photo.isNullOrEmpty() || b.type in setOf("todo", "habit") && b.checked
    fun record(entry: DiaryEntry) = entry.blocks.filter(::meaningful).map { listOf(it.id, it.type, it.text, it.checked, it.photo) }
    fun level(book: DiaryBook) = 1 + book.awardedDays.size / 5
    fun xp(book: DiaryBook) = book.awardedDays.size * 10
    fun bound(b: DiaryBlock) = b.copy(x=b.x.coerceIn(0,MAX_POSITION),y=b.y.coerceIn(0,MAX_POSITION),
        width=(if(b.width==0) 350 else b.width).coerceIn(MIN_WIDTH,MAX_WIDTH),height=(if(b.height==0) when(b.type) {"photo","photo-slot"->360;"emotion"->288;else->250} else b.height).coerceIn(MIN_HEIGHT,MAX_HEIGHT))
    fun arrange(entry: DiaryEntry): DiaryEntry {
        var y=12
        return entry.copy(blocks=entry.blocks.map { b -> bound(b.copy(x=12,y=y,width=if(b.wide) 876 else 420)).also {y+=it.height+16} })
    }
    fun append(entry: DiaryEntry, blocks: List<DiaryBlock>): DiaryEntry {
        require(entry.blocks.size+blocks.size<=MAX_BLOCKS) {"한 날짜에는 블록을 200개까지 추가할 수 있어요."}
        val offset=if(entry.layoutMode=="free" && entry.blocks.isNotEmpty()) entry.blocks.maxOf {bound(it).y+bound(it).height}+16 else 0
        require(blocks.all { it.y+offset<=MAX_POSITION }) {"아래에 배치를 추가할 공간이 부족해요."}
        return entry.copy(blocks=entry.blocks+blocks.map {if(entry.layoutMode=="free") bound(it.copy(y=it.y+offset)) else it})
    }
    fun capture(entry: DiaryEntry, name: String): SavedLayout {
        require(name.trim().isNotEmpty() && name.trim().length<=40 && name.none {it.isISOControl()}) {"레이아웃 이름은 1~40자로 적어주세요."}
        require(entry.blocks.isNotEmpty()) {"블록을 추가한 뒤 배치를 저장해 주세요."}
        val positions=if(entry.layoutMode=="free") entry else arrange(entry)
        return SavedLayout(name=name.trim(),mode=entry.layoutMode,style=entry.pageStyle,theme=entry.theme,
            blocks=positions.blocks.map { bound(it.copy(id="",type=if(it.type=="photo") "photo-slot" else it.type,text="",checked=false,photo=null)) })
    }
    fun apply(entry: DiaryEntry, layout: SavedLayout): DiaryEntry {
        val fresh=entry.blocks.isEmpty()
        val target=if(fresh) entry.copy(theme=layout.theme,pageStyle=layout.style,layoutMode=layout.mode) else entry
        return append(target,layout.blocks.map {it.copy(id=UUID.randomUUID().toString(),text="",checked=false,photo=null)})
    }
    fun merge(current: DiaryBook, incoming: DiaryBook): DiaryBook {
        val layouts=current.layouts.toMutableList()
        incoming.layouts.forEach { item ->
            layouts.removeAll {it.id==item.id}
            var name=item.name;var suffix=2
            while(layouts.any {it.name.equals(name,true)}) {val tail=" (${suffix++})";name=item.name.take(40-tail.length)+tail}
            layouts+=item.copy(name=name)
        }
        require(layouts.size<=MAX_LAYOUTS) {"내 레이아웃은 최대 50개까지 저장할 수 있어요."}
        return current.copy(days=current.days+incoming.days,layouts=layouts,characterStyle=incoming.characterStyle,
            awardedDays=current.awardedDays+incoming.awardedDays,equipment=if(incoming.version>=4 && incoming.hasEquipment) incoming.equipment else current.equipment,
            hasEquipment=current.hasEquipment || incoming.hasEquipment)
    }
    fun validate(book: DiaryBook) {
        require(book.version in 1..4 && book.days.size<=50000 && book.layouts.size<=MAX_LAYOUTS && book.awardedDays.size<=50000) {"지원하지 않거나 너무 큰 기록 파일이에요."}
        require(book.characterStyle in SangmonIdentity.levels && level(book)>=SangmonIdentity.levels.getValue(book.characterStyle))
        book.awardedDays.forEach {require(LocalDate.parse(it).year in 1..9999)}
        val elements=setOf("solar","ember","frost","storm","shadow","astral")
        require(book.equipment.weaponId=="none" || book.equipment.weaponId in elements.map {"$it-sword"})
        require(book.equipment.charmId=="none" || book.equipment.charmId in elements.map {"$it-orb"})
        book.days.forEach { (date,entry) ->
            require(LocalDate.parse(date).year in 1..9999);require(entry.theme in 0..2 && entry.blocks.size<=MAX_BLOCKS)
            require(entry.layoutMode in setOf("cards","free") && entry.pageStyle in setOf("plain","paper","dots"))
            require(entry.blocks.map {it.id}.distinct().size==entry.blocks.size)
            entry.blocks.forEach {b ->
                require(b.id.isNotBlank() && b.id.length<=64 && b.type in kinds && b.text.length<=100000 && b.title.length<=80 && b.prompt.length<=300)
                require(b.x in 0..MAX_POSITION && b.y in 0..(if(entry.layoutMode=="free") MAX_POSITION else 282400))
                require(b.width==0 || b.width in MIN_WIDTH..MAX_WIDTH);require(b.height==0 || b.height in MIN_HEIGHT..MAX_HEIGHT)
                require(b.type!="photo" || !b.photo.isNullOrEmpty()); require(b.type=="photo" || b.photo==null)
            }
        }
        require(book.layouts.map {it.id}.distinct().size==book.layouts.size)
        require(book.layouts.map {it.name.lowercase()}.distinct().size==book.layouts.size)
        book.layouts.forEach {l -> require(l.id.isNotBlank() && l.id.length<=64 && l.name==l.name.trim() && l.name.length in 1..40 && l.name.none {it.isISOControl()})
            require(l.mode in setOf("cards","free") && l.style in setOf("plain","paper","dots") && l.theme in 0..2 && l.blocks.size in 1..MAX_BLOCKS)
            l.blocks.forEach {b ->require(b.type in kinds-setOf("photo") && b.x in 0..MAX_POSITION && b.y in 0..(if(l.mode=="free") MAX_POSITION else 282400) && b.width in MIN_WIDTH..MAX_WIDTH && b.height in MIN_HEIGHT..MAX_HEIGHT && b.title.length<=80 && b.prompt.length<=300)}
        }
    }
}
