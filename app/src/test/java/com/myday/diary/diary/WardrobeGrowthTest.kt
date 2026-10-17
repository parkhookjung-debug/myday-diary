package com.myday.diary.diary

import com.myday.diary.data.*
import org.junit.Assert.*
import org.junit.Test
import java.time.LocalDate
import java.io.File

class WardrobeGrowthTest {
    private class Memory: NotebookRepository {
        var book=DiaryBook();var fail=false
        override fun loadBook()=book
        override fun saveBook(book: DiaryBook) {if(fail)error("disk full");this.book=book}
    }
    @Test fun all300ItemsRoundTripAndEightSlotsRemainIndependent() {
        assertEquals(300,EquipmentCatalog.items.size);assertEquals(8,EquipmentCatalog.slots.size)
        var gear=Equipment()
        EquipmentCatalog.items.forEach {(id,slot)->
            val prior=gear;gear=gear.equip(slot,id)
            EquipmentCatalog.slots.filter {it!=slot}.forEach {assertEquals(prior.get(it),gear.get(it))}
            val decoded=NotebookJson.decode(NotebookJson.encode(DiaryBook(equipment=gear)))
            assertEquals(gear,decoded.equipment);assertEquals(0,NotebookRules.xp(decoded))
        }
        EquipmentCatalog.slots.forEach {assertNotEquals("none",gear.get(it))}
        val legacy=NotebookRules.merge(DiaryBook(equipment=gear),DiaryBook(version=4,equipment=Equipment("frost-sword")))
        assertEquals("frost-sword",legacy.equipment.weaponId);EquipmentCatalog.slots.drop(2).forEach {assertEquals(gear.get(it),legacy.equipment.get(it))}
    }
    @Test fun attendanceAndRealActivitiesAwardFortyAtMostAndMergeWithoutDuplication() {
        val day=LocalDate.of(2026,10,16);val memory=Memory();val c=NotebookController(memory,today={day})
        assertEquals(day,c.actualDay)
        c.checkIn();c.checkIn();assertEquals(5,NotebookRules.xp(c.book))
        val entry=DiaryEntry(blocks=listOf(DiaryBlock(id="text",type="text",text="스무 글자가 넘는 충분한 오늘의 일기를 작성하고 점수를 확인합니다"),DiaryBlock(id="photo",type="photo",photo="fixture"),DiaryBlock(id="todo",type="todo",checked=true),DiaryBlock(id="habit",type="habit",checked=true)))
        c.edit(entry);assertEquals(40,NotebookRules.xp(c.book));c.edit(entry);assertEquals(40,NotebookRules.xp(c.book))
        assertEquals(40,NotebookRules.xp(NotebookRules.merge(c.book,c.book)))
        val tomorrow=NotebookRules.award(c.book,entry,entry.copy(blocks=entry.blocks.map {if(it.id=="text")it.copy(text=it.text+" 다음 날 글") else it}),day.plusDays(1))
        assertEquals(60,NotebookRules.xp(tomorrow));assertEquals(NotebookRules.WRITING,tomorrow.activities[day.plusDays(1).toString()])
    }
    @Test fun failedAttendanceDoesNotChangeCommittedProgressAndRetryIsIdempotent() {
        val memory=Memory();val c=NotebookController(memory);memory.fail=true
        assertFalse(c.checkIn());assertEquals(0,NotebookRules.xp(c.book));assertTrue(memory.book.activities.isEmpty())
        memory.fail=false;assertTrue(c.checkIn());assertTrue(c.checkIn());assertEquals(5,NotebookRules.xp(c.book))
    }
    @Test fun v5WindowsFixtureRetainsAllEightSlotsAndMissionFlags() {
        val raw=javaClass.classLoader!!.getResourceAsStream("windows-v5.json")!!.bufferedReader().use {it.readText()}
        val book=NotebookJson.decode(raw);assertEquals(5,book.version);assertEquals(31,book.activities["2026-10-16"])
        EquipmentCatalog.slots.forEach {assertNotEquals("none",book.equipment.get(it))}
        val entry=book.days.getValue("2026-10-08")
        val changed=book.copy(days=book.days+("2026-10-08" to entry.copy(blocks=entry.blocks.map {when(it.type){"text"->it.copy(text="안드로이드에서 수정한 글");"todo"->it.copy(checked=false);else->it}})))
        val encoded=NotebookJson.encode(changed);assertEquals(changed,NotebookJson.decode(encoded))
        val output=File("build/interop/android-v5-to-windows.json");output.parentFile?.mkdirs();output.writeText(encoded)
    }
    @Test fun malformedMissionFlagsAndDuplicateDatesAreRejected() {
        assertThrows(IllegalArgumentException::class.java){NotebookRules.validate(DiaryBook(activities=mapOf("2026-10-16" to 32)))}
        val raw="""{"Version":5,"Days":[],"Progress":{"AwardedDays":[],"Activities":[{"Date":"2026-10-16","Flags":1},{"Date":"2026-10-16","Flags":2}]}}"""
        assertThrows(IllegalArgumentException::class.java){NotebookJson.decode(raw)}
    }
}
