package com.myday.diary.diary

import androidx.compose.runtime.*
import com.myday.diary.data.*
import java.time.LocalDate

/** Keep working edits immediate; an optional serialized writer persists large books off the UI. */
class NotebookController(private val repository: NotebookRepository,private val today: ()->LocalDate=LocalDate::now,
    private val persist: ((DiaryBook,String?,(Result<DiaryBook>)->Unit)->Unit)?=null) {
    var book by mutableStateOf(CollectionRules.migrate(repository.loadBook()));private set
    var date by mutableStateOf(today());private set
    var entry by mutableStateOf(book.days[date.toString()] ?: DiaryEntry());private set
    var error by mutableStateOf<String?>(null);private set
    var saving by mutableStateOf(false);private set
    var reaction by mutableStateOf("");private set
    var reactionTick by mutableLongStateOf(0L);private set
    private var reactionAt=0L
    private var pending: DiaryBook?=null
    private var revision=0L
    val saved get()=pending==null && !saving
    val actualDay get()=today()
    fun clearError() {error=null}
    fun report(message: String) {error=message}
    fun prepareSave(candidate: DiaryBook,recordDate: String?): DiaryBook {
        val awarded=if(recordDate==null) book.awardedDays+candidate.awardedDays else book.awardedDays
        val next=recordDate?.let {candidate.days[it]}
        val activities=(book.activities.keys+candidate.activities.keys).associateWith {(book.activities[it] ?: 0) or (candidate.activities[it] ?: 0)}
        val prepared=candidate.copy(awardedDays=awarded,activities=activities)
        return if(next!=null) NotebookRules.award(prepared,book.days[recordDate] ?: DiaryEntry(),next,today()) else prepared
    }
    private fun commit(candidate: DiaryBook,draft: Boolean=false,onSuccess: ()->Unit={}): Boolean {
        val generation=++revision
        try {
            NotebookRules.validate(candidate);saving=true;pending=candidate;error=null
            val done: (Result<DiaryBook>)->Unit = {result->result.onSuccess {savedBook->
                val old=book.days[date.toString()];val next=savedBook.days[date.toString()]
                if(draft && next!=null && System.currentTimeMillis()-reactionAt>4000) {
                    val habit=next.blocks.any {b->b.type=="habit" && b.checked && old?.blocks?.any {it.id==b.id && it.checked}!=true}
                    val text=next.blocks.any {b->(b.text.isNotBlank() || b.photo!=null) && old?.blocks?.any {it.id==b.id && it.text==b.text && it.photo==b.photo}!=true}
                    if(habit || text) {reaction=if(habit) JournalPolicy.habitReaction else JournalPolicy.recordReaction;reactionTick++;reactionAt=System.currentTimeMillis()}
                }
                book=savedBook
                if(generation==revision) {pending=null;saving=false;error=null;onSuccess()}
            }.onFailure {e->if(generation==revision) {saving=false;pending=if(draft) candidate else null;error=e.message ?: "저장하지 못했어요."}}}
            if(persist!=null) persist.invoke(candidate,if(draft) date.toString() else null,done)
            else done(runCatching {prepareSave(candidate,if(draft) date.toString() else null).also {repository.saveBook(it)}})
        } catch(e: Exception) {saving=false;pending=if(draft) candidate else null;error=e.message ?: "저장하지 못했어요."}
        return saved && error==null
    }
    fun edit(next: DiaryEntry,@Suppress("UNUSED_PARAMETER") contentEdit: Boolean=false) {
        entry=next;commit(book.copy(days=book.days+(date.toString() to next)),true)
    }
    fun retry() {val candidate=pending ?: return;edit(candidate.days[date.toString()] ?: entry)}
    fun selectDate(target: LocalDate) {require(target.year in 1..9999);if(!saved) {error=if(saving) "저장이 끝나면 날짜를 바꿀 수 있어요." else "날짜를 바꾸기 전에 다시 저장해 주세요.";return};date=target;entry=book.days[target.toString()] ?: DiaryEntry()}
    fun updateBlock(block: DiaryBlock,contentEdit: Boolean=true)=edit(entry.copy(blocks=entry.blocks.map {if(it.id==block.id) block else it}),contentEdit)
    fun add(type: String) {require(type in NotebookRules.kinds);edit(NotebookRules.append(entry,listOf(DiaryBlock(type=type))))}
    fun delete(id: String)=edit(entry.copy(blocks=entry.blocks.filterNot {it.id==id}))
    fun move(id: String,delta: Int) {val blocks=entry.blocks.toMutableList();val index=blocks.indexOfFirst {it.id==id};val target=index+delta
        if(index !in blocks.indices || target !in blocks.indices) return;blocks.add(target,blocks.removeAt(index));edit(entry.copy(blocks=blocks))}
    fun configure(change: (DiaryBook)->DiaryBook): Boolean {if(!saved) {error=if(saving) "저장 중이에요. 잠시 후 다시 선택해 주세요." else "먼저 일기를 다시 저장해 주세요.";return false};return commit(change(book))}
    fun selectCharacter(id: String,requiredLevel: Int): Boolean {if(NotebookRules.xp(book)<(SangmonUnlockXP.values[id] ?: Int.MAX_VALUE)) {error="Lv.$requiredLevel · 총 ${SangmonUnlockXP.values[id]} XP에서 사용할 수 있어요.";return false};return configure {it.copy(characterStyle=id)}}
    fun equip(slot: String,id: String): Boolean {require(slot in EquipmentCatalog.slots && (id=="none" || EquipmentCatalog.items[id]==slot));if(!CollectionRules.canUse(book,id)){error="뽑기나 꽃잎 교환으로 먼저 얻어주세요.";return false};return configure {it.copy(equipment=it.equipment.equip(slot,id),hasEquipment=true)}}
    fun draw(): Boolean=configure {CollectionRules.draw(it)}
    fun exchange(itemId: String): Boolean=configure {CollectionRules.exchange(it,itemId)}
    fun synthesize(grade: Int): Boolean=configure {CollectionRules.synthesize(it,grade)}
    fun craft(itemId: String): Boolean=configure {CollectionRules.craft(it,itemId)}
    fun checkIn(): Boolean=configure {val key=today().toString();require(key in it.activities || it.activities.size<50000);it.copy(activities=it.activities+(key to ((it.activities[key] ?: 0) or NotebookRules.ATTENDANCE)))}
    fun saveLayout(name: String) {val layout=NotebookRules.capture(entry,name);require(book.layouts.size<NotebookRules.MAX_LAYOUTS);configure {it.copy(layouts=it.layouts+layout)}}
    fun renameLayout(id: String,name: String) {require(name.trim().length in 1..40 && name.none {it.isISOControl()});configure {it.copy(layouts=it.layouts.map {l->if(l.id==id) l.copy(name=name.trim()) else l})}}
    fun deleteLayout(id: String)=configure {it.copy(layouts=it.layouts.filterNot {l->l.id==id})}
    fun applyLayout(layout: SavedLayout)=edit(NotebookRules.apply(entry,layout))
    fun applyTemplate(template: DiaryTemplate) {val base=if(entry.blocks.isEmpty()) entry.copy(layoutMode="free",pageStyle=template.style) else entry
        edit(NotebookRules.append(base,template.blocks.map {it.copy(id=java.util.UUID.randomUUID().toString())}))}
    fun restoreRevision(date: String,old: DiaryEntry): Boolean {if(!saved){error="먼저 저장을 완료해 주세요.";return false};return commit(book.copy(days=book.days+(date to old)),onSuccess={entry=book.days[this.date.toString()] ?: DiaryEntry()})}
    fun deleteDay(): Boolean {if(!saved){error="먼저 저장을 완료해 주세요.";return false};return commit(book.copy(days=book.days-date.toString()),onSuccess={entry=DiaryEntry()})}
    fun importBook(incoming: DiaryBook): Boolean {if(!saved) {error="먼저 일기를 다시 저장해 주세요.";return false}
        return commit(NotebookRules.merge(book,incoming),onSuccess={entry=book.days[date.toString()] ?: DiaryEntry()})}
}
