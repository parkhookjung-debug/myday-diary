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
    fun record(entry: DiaryEntry) = entry.blocks.filter(::meaningful).associate {it.id to listOf(it.type,it.text,it.checked,it.photo)}
    const val ATTENDANCE=1;const val WRITING=2;const val PHOTO=4;const val TODO=8;const val HABIT=16
    val goals=listOf(ATTENDANCE,WRITING,PHOTO,TODO,HABIT)
    fun points(flag: Int)=if(flag==WRITING) 10 else 5
    fun bonus(flags: Int)=goals.filter {flags and it!=0}.sumOf(::points)
    fun levelFromXp(xp: Int)=when {xp<90->1+xp/10;xp<490->10+(xp-90)/20;xp<1090->30+(xp-490)/30;else->50+(xp-1090)/50}
    fun level(book: DiaryBook)=levelFromXp(xp(book))
    fun neededXp(level: Int)=when {level<10->10;level<30->20;level<50->30;else->50}
    fun floorXp(level: Int)=when {level<10->(level-1)*10;level<30->90+(level-10)*20;level<50->490+(level-30)*30;else->1090+(level-50)*50}
    fun intoLevel(book: DiaryBook)=xp(book)-floorXp(level(book))
    fun chapter(level: Int)=when {level<10->"새싹 친구";level<30->"일상 친구";level<50->"다정한 동행";else->"오래된 단짝"}
    fun attendanceWeeks(book: DiaryBook): Int {
        var previous: LocalDate?=null;var run=0;var weeks=0
        book.activities.filterValues {it and ATTENDANCE!=0}.keys.sorted().forEach {
            val day=LocalDate.parse(it);run=if(previous?.plusDays(1)==day)run+1 else 1
            if(run%7==0)weeks++;previous=day
        };return weeks
    }
    fun xp(book: DiaryBook) = book.awardedDays.size * 10+book.activities.values.sumOf(::bonus)
    fun todayXp(book: DiaryBook,day: LocalDate)= (if(day.toString() in book.awardedDays) 10 else 0)+bonus(book.activities[day.toString()] ?: 0)
    fun award(candidate: DiaryBook,prior: DiaryEntry,next: DiaryEntry,day: LocalDate): DiaryBook {
        if(record(prior)==record(next) || next.blocks.none(::meaningful))return candidate
        val old=prior.blocks.associateBy {it.id};var flags=0
        val text=next.blocks.filter {it.type in setOf("text","emotion")}
        if(text.sumOf {it.text.count {c->!c.isWhitespace()}}>=20 && text.any {old[it.id]?.text!=it.text || old[it.id]?.type!=it.type})flags=flags or WRITING
        if(next.blocks.any {it.type=="photo" && !it.photo.isNullOrEmpty() && old[it.id]?.photo!=it.photo})flags=flags or PHOTO
        if(next.blocks.any {it.type=="todo" && it.checked && (old[it.id]?.checked!=true || old[it.id]?.type!=it.type)})flags=flags or TODO
        if(next.blocks.any {it.type=="habit" && it.checked && (old[it.id]?.checked!=true || old[it.id]?.type!=it.type)})flags=flags or HABIT
        val key=day.toString();val awarded=if(candidate.awardedDays.size<50000) candidate.awardedDays+key else candidate.awardedDays
        val activities=if(flags!=0 && (key in candidate.activities || candidate.activities.size<50000))candidate.activities+(key to ((candidate.activities[key] ?: 0) or flags)) else candidate.activities
        return candidate.copy(awardedDays=awarded,activities=activities)
    }
    fun bound(b: DiaryBlock) = b.copy(x=b.x.coerceIn(0,MAX_POSITION),y=b.y.coerceIn(0,MAX_POSITION),
        width=(if(b.width==0) 350 else b.width).coerceIn(MIN_WIDTH,MAX_WIDTH),height=(if(b.height==0) when(b.type) {"photo","photo-slot"->360;"emotion"->288;else->250} else b.height).coerceIn(MIN_HEIGHT,MAX_HEIGHT))
    fun arrange(entry: DiaryEntry): DiaryEntry {
        var y=12
        return entry.copy(blocks=entry.blocks.map { b -> bound(b.copy(x=12,y=y,width=if(b.wide) 876 else 420)).also {y+=it.height+16} })
    }
    fun free(entry: DiaryEntry): DiaryEntry {
        val columns=maxOf(1,(entry.blocks.size+23)/24)
        return entry.copy(layoutMode="free",blocks=entry.blocks.mapIndexed {i,b->
            if(b.width==0 || b.height==0 || b.y>MAX_POSITION) bound(b.copy(x=12+(i%columns)*900,y=12+(i/columns)*400,width=if(b.wide) 876 else 420)) else bound(b)
        })
    }
    fun append(entry: DiaryEntry, blocks: List<DiaryBlock>): DiaryEntry {
        require(entry.blocks.size+blocks.size<=MAX_BLOCKS) {"한 날짜에는 블록을 200개까지 추가할 수 있어요."}
        val offset=if(entry.layoutMode=="free" && entry.blocks.isNotEmpty()) entry.blocks.maxOf {bound(it).y+bound(it).height}+16 else 0
        require(entry.layoutMode!="free" || blocks.all { it.y+offset<=MAX_POSITION }) {"아래에 배치를 추가할 공간이 부족해요."}
        return entry.copy(blocks=entry.blocks+blocks.map {if(entry.layoutMode=="free") bound(it.copy(y=it.y+offset)) else it.copy(x=0,y=0,width=0)})
    }
    fun capture(entry: DiaryEntry, name: String): SavedLayout {
        require(name.trim().isNotEmpty() && name.trim().length<=40 && name.none {it.isISOControl()}) {"레이아웃 이름은 1~40자로 적어주세요."}
        require(entry.blocks.isNotEmpty()) {"블록을 추가한 뒤 배치를 저장해 주세요."}
        var y=12
        val positions=if(entry.layoutMode=="free") entry.copy(blocks=entry.blocks.map(::bound)) else entry.copy(blocks=entry.blocks.map {b->
            bound(b).copy(x=12,y=y,width=if(b.wide) 876 else 420).also {y+=it.height+16}
        })
        return SavedLayout(name=name.trim(),mode=entry.layoutMode,style=entry.pageStyle,theme=entry.theme,
            blocks=positions.blocks.map {it.copy(id="",type=if(it.type=="photo") "photo-slot" else it.type,text="",checked=false,photo=null)})
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
            while(layouts.any {it.name.equals(name,true)}) {val tail=" (${suffix++})";val prefix=item.name.take(40-tail.length);name=(if(prefix.lastOrNull()?.isHighSurrogate()==true) prefix.dropLast(1) else prefix)+tail}
            layouts+=item.copy(name=name)
        }
        require(layouts.size<=MAX_LAYOUTS) {"내 레이아웃은 최대 50개까지 저장할 수 있어요."}
        val merged=current.copy(days=current.days+incoming.days,layouts=layouts,characterStyle=incoming.characterStyle,
            awardedDays=current.awardedDays+incoming.awardedDays,equipment=if(incoming.version>=5 && incoming.hasEquipment) incoming.equipment else if(incoming.version>=4 && incoming.hasEquipment)current.equipment.copy(weaponId=incoming.equipment.weaponId,charmId=incoming.equipment.charmId) else current.equipment,
            hasEquipment=current.hasEquipment || incoming.hasEquipment,activities=(current.activities.keys+incoming.activities.keys).associateWith {(current.activities[it] ?: 0) or (incoming.activities[it] ?: 0)},version=maxOf(current.version,incoming.version),collection=CollectionRules.merge(current.collection,incoming.collection))
        var collection=merged.collection
        if(current.version<7)collection=CollectionRules.preserveLegacy(collection,current.equipment,merged)
        if(incoming.version<7)collection=CollectionRules.preserveLegacy(collection,incoming.equipment,merged)
        return merged.copy(collection=collection,version=maxOf(7,merged.version))
    }
    fun validate(book: DiaryBook) {
        require(book.version in 1..7 && book.days.size<=50000 && book.layouts.size<=MAX_LAYOUTS && book.awardedDays.size<=50000 && book.activities.size<=50000) {"지원하지 않거나 너무 큰 기록 파일이에요."}
        require(book.characterStyle in SangmonIdentity.levels && xp(book)>=SangmonUnlockXP.values.getValue(book.characterStyle))
        book.awardedDays.forEach {require(LocalDate.parse(it).year in 1..9999)}
        book.activities.forEach {(date,flags)->require(LocalDate.parse(date).year in 1..9999 && flags in 1..31)}
        val collection=CollectionRules.state(book)
        if(book.version>=7 || collection.closet)require(collection.closet && EquipmentCatalog.slots.all {book.equipment.get(it)=="none" || book.equipment.get(it) in collection.owned}) {"아직 얻지 않은 소품을 착용한 기록이에요."}
        EquipmentCatalog.slots.forEach {slot->val id=book.equipment.get(slot);require(id=="none" || EquipmentCatalog.items[id]==slot)}
        book.days.forEach { (date,entry) ->
            require(LocalDate.parse(date).year in 1..9999);require(entry.theme in 0..2 && entry.blocks.size<=MAX_BLOCKS)
            require(entry.layoutMode in setOf("cards","free") && entry.pageStyle in setOf("plain","paper","dots"))
            require(entry.blocks.map {it.id}.distinct().size==entry.blocks.size)
            entry.blocks.forEach {b ->
                require(b.id.isNotBlank() && b.id.length<=64 && b.type in kinds && b.text.length<=100000 && b.title.length<=80 && b.prompt.length<=300)
                require(b.x in 0..MAX_POSITION && b.y in 0..MAX_POSITION)
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
