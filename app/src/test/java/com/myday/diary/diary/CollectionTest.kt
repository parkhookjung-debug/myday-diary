package com.myday.diary.diary

import com.myday.diary.data.*
import org.junit.Assert.*
import org.junit.Test
import java.time.LocalDate
import java.io.File

class CollectionTest {
    private fun rich()=DiaryBook(awardedDays=(0 until 20).map {LocalDate.of(2026,1,1).plusDays(it.toLong()).toString()}.toSet())
    @Test fun granularBoundariesAndOriginalRewardThresholds() {
        assertEquals(listOf(1,1,2,9,10,10,11,29,30,49,50,51),listOf(0,9,10,89,90,109,110,489,490,1089,1090,1140).map(NotebookRules::levelFromXp))
        (1..200).forEach {assertEquals(it,NotebookRules.levelFromXp(NotebookRules.floorXp(it)));assertEquals(it,NotebookRules.levelFromXp(NotebookRules.floorXp(it)+NotebookRules.neededXp(it)-1))}
        assertEquals(100,SangmonUnlockXP.values.getValue("growth-bookmark"))
    }
    @Test fun welcomeDrawsDuplicatesAndInsufficientFunds() {
        val one=CollectionRules.draw(DiaryBook()){0};val two=CollectionRules.draw(one){0};val state=CollectionRules.state(two)
        assertEquals(0,state.coins);assertEquals(3,state.petals);assertEquals(9,state.owned.size)
        assertThrows(IllegalArgumentException::class.java){CollectionRules.draw(two){0}};assertEquals(3,two.collection!!.actions.size)
    }
    @Test fun pityExchangeAndPrefixMergesPreserveLedgerAndCurrency() {
        var book=rich();repeat(10){book=CollectionRules.draw(book){0}}
        val state=CollectionRules.state(book);assertEquals(10,state.owned.size);assertEquals(24,state.petals)
        val id=EquipmentCatalog.items.keys.first {it !in state.owned};val exchanged=CollectionRules.exchange(book,id);val after=CollectionRules.state(exchanged)
        assertEquals(9,after.petals);assertEquals(11,after.owned.size);assertEquals(state.coins,after.coins);assertEquals(10,after.draws)
        assertEquals(exchanged.collection,CollectionRules.merge(book.collection,exchanged.collection))
        assertEquals(exchanged.collection,CollectionRules.merge(exchanged.collection,exchanged.collection))
        assertEquals(exchanged.collection,CollectionRules.merge(exchanged.collection,null))
        assertThrows(IllegalArgumentException::class.java){CollectionRules.exchange(exchanged,id)}
        assertThrows(IllegalArgumentException::class.java){CollectionRules.merge(CollectionRules.draw(DiaryBook()){0}.collection,CollectionRules.draw(DiaryBook()){1}.collection)}
        assertEquals(exchanged,NotebookJson.decode(NotebookJson.encode(exchanged)))
    }
    @Test fun consecutiveAttendanceBonusesDoNotRewardGaps() {
        val day=LocalDate.of(2026,1,1)
        val book=DiaryBook(activities=(0 until 14).associate {day.plusDays(it.toLong()).toString() to NotebookRules.ATTENDANCE})
        assertEquals(2,NotebookRules.attendanceWeeks(book));assertEquals(165,CollectionRules.earned(book))
        assertEquals(0,NotebookRules.attendanceWeeks(book.copy(activities=(0..5).plus(7).associate {day.plusDays(it.toLong()).toString() to NotebookRules.ATTENDANCE})))
    }
    @Test fun failedAndQueuedDrawsNeverConsumeCommittedCurrencyTwice() {
        class Memory: NotebookRepository {var value=DiaryBook();var fail=false;override fun loadBook()=value;override fun saveBook(book: DiaryBook){if(fail)error("disk full");value=book}}
        val memory=Memory();val c=NotebookController(memory);memory.fail=true;assertFalse(c.draw());assertEquals(0,CollectionRules.state(c.book).draws);assertEquals(40,CollectionRules.state(c.book).coins)
        memory.fail=false;assertTrue(c.draw());assertEquals(20,CollectionRules.state(c.book).coins)
        data class Pending(val book: DiaryBook,val done: (Result<DiaryBook>)->Unit)
        val queue=mutableListOf<Pending>();val queued=NotebookController(Memory(),persist={book,_,done->queue+=Pending(book,done)})
        queued.draw();assertFalse(queued.draw());assertEquals(1,queue.size);assertEquals(0,CollectionRules.state(queued.book).draws)
        val request=queue.single();request.done(Result.success(request.book));assertEquals(20,CollectionRules.state(queued.book).coins)
    }
    @Test fun v6FixtureRoundTripsForActualWindowsSerializer() {
        val raw=javaClass.classLoader!!.getResourceAsStream("windows-v6.json")!!.bufferedReader().use {it.readText()}
        val book=NotebookJson.decode(raw);val state=CollectionRules.state(book)
        assertEquals(6,book.version);assertEquals(2,state.draws);assertEquals(3,state.petals);assertEquals(90,state.coins)
        val entry=book.days.getValue("2026-10-08")
        val changed=book.copy(days=book.days+("2026-10-08" to entry.copy(blocks=entry.blocks.map {when(it.type){"text"->it.copy(text="안드로이드에서 수정한 글");"todo"->it.copy(checked=false);else->it}})))
        val encoded=NotebookJson.encode(changed);assertEquals(changed,NotebookJson.decode(encoded))
        val output=File("build/interop/android-v6-to-windows.json");output.parentFile?.mkdirs();output.writeText(encoded)
    }
}
