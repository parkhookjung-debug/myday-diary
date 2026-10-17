package com.myday.diary.diary

import com.myday.diary.data.*
import org.junit.Assert.*
import org.junit.Test
import org.json.JSONObject
import java.io.File
import java.time.LocalDate

class NotebookTest {
    private class Memory: NotebookRepository {
        var value=DiaryBook();var fail=false
        override fun loadBook()=value
        override fun saveBook(book: DiaryBook) {if(fail) error("disk full");value=book}
    }
    @Test fun windowsFixtureRoundTripsIntoRealWindowsInteropExport() {
        val raw=javaClass.classLoader!!.getResourceAsStream("windows-v4.json")!!.bufferedReader().use {it.readText()}
        val original=NotebookJson.decode(raw);val day=original.days.getValue("2026-10-08")
        val modified=original.copy(days=original.days+("2026-10-08" to day.copy(blocks=day.blocks.map {when(it.type) {"text"->it.copy(text="안드로이드에서 수정한 글");"todo"->it.copy(checked=false);else->it}})))
        val encoded=NotebookJson.encode(modified);assertEquals(modified.copy(version=4),NotebookJson.decode(encoded))
        assertEquals(original.days.getValue("2026-10-08").blocks.single {it.type=="photo"}.photo,NotebookJson.decode(encoded).days.getValue("2026-10-08").blocks.single {it.type=="photo"}.photo)
        val output=File(System.getProperty("myday.interop.output") ?: "build/interop/android-to-windows.json");output.parentFile?.mkdirs();output.writeText(encoded)
    }
    @Test fun contentAwardsOnceOnActualDayAndSettingsNeverAward() {
        val memory=Memory();val today=LocalDate.of(2026,10,13);val c=NotebookController(memory,today={today})
        c.add("text");c.edit(c.entry.copy(theme=2));c.equip("weapon","frost-sword");assertTrue(c.book.awardedDays.isEmpty())
        c.updateBlock(c.entry.blocks.single().copy(text="기록"));assertEquals(setOf(today.toString()),c.book.awardedDays)
        c.selectDate(today.minusDays(1));c.add("habit");c.updateBlock(c.entry.blocks.single().copy(checked=true));assertEquals(15,NotebookRules.xp(c.book))
        assertFalse(c.selectCharacter("growth-hero",10));assertEquals("original",c.book.characterStyle)
    }
    @Test fun failedSaveRetainsDraftBlocksNavigationAndRollsBackRewardAndEquipment() {
        val memory=Memory();val c=NotebookController(memory,today={LocalDate.of(2026,10,13)});c.add("text");memory.fail=true
        c.updateBlock(c.entry.blocks.single().copy(text="보존해야 해요"));assertFalse(c.saved);assertTrue(c.book.awardedDays.isEmpty())
        c.selectDate(c.date.plusDays(1));assertEquals(LocalDate.of(2026,10,13),c.date);assertFalse(c.equip("charm","astral-orb"));assertEquals("none",c.book.equipment.charmId)
        memory.fail=false;c.retry();assertTrue(c.saved);assertEquals("보존해야 해요",memory.value.days.getValue(c.date.toString()).blocks.single().text);assertEquals(10,NotebookRules.xp(c.book))
    }
    @Test fun savingLayoutCopiesStructureWithoutUserTextPhotosOrChecks() {
        val entry=DiaryEntry(theme=2,layoutMode="free",pageStyle="dots",blocks=listOf(DiaryBlock(type="photo",photo="secret",text="비밀",width=340,height=250,x=1520,y=1040),DiaryBlock(type="todo",text="원문",checked=true,width=340,height=250)))
        val layout=NotebookRules.capture(entry,"기억");val applied=NotebookRules.apply(DiaryEntry(),layout)
        assertEquals(listOf("photo-slot","todo"),applied.blocks.map {it.type});assertTrue(applied.blocks.all {it.text.isEmpty() && it.photo==null && !it.checked})
        assertEquals(1520,applied.blocks.first().x);assertEquals(1040,applied.blocks.first().y);assertEquals(2,applied.theme);assertEquals("free",applied.layoutMode)
        val existing=NotebookRules.apply(DiaryEntry(blocks=listOf(DiaryBlock(type="text",text="기존"))),layout);assertEquals("기존",existing.blocks.first().text);assertEquals(0,existing.theme)
    }
    @Test fun mergeDeduplicatesGrowthRenamesLayoutsAndKeepsEquipmentFromLegacyBackup() {
        val l=SavedLayout(name="같은 이름",mode="free",style="plain",theme=0,blocks=listOf(NotebookRules.bound(DiaryBlock(type="text"))))
        val current=DiaryBook(layouts=listOf(l),awardedDays=setOf("2026-10-12"),equipment=Equipment("frost-sword","astral-orb"))
        val incoming=DiaryBook(version=3,layouts=listOf(l.copy(id="other")),awardedDays=setOf("2026-10-12","2026-10-13"))
        val merged=NotebookRules.merge(current,incoming);assertEquals(listOf("같은 이름","같은 이름 (2)"),merged.layouts.map {it.name});assertEquals(20,NotebookRules.xp(merged));assertEquals(current.equipment,merged.equipment)
    }
    @Test fun malformedBackupAndOversizedAppendCannotModifyOriginalBook() {
        val memory=Memory();val original=memory.value
        val malformed=JSONObject().put("Version",99).put("Days",org.json.JSONArray()).toString()
        assertThrows(IllegalArgumentException::class.java) {NotebookJson.decode(malformed)};assertEquals(original,memory.value)
        val full=DiaryEntry(blocks=(0 until 200).map {DiaryBlock(type="text")});assertThrows(IllegalArgumentException::class.java) {NotebookRules.append(full,listOf(DiaryBlock(type="habit")))};assertEquals(200,full.blocks.size)
        val free=NotebookRules.free(full);NotebookRules.validate(DiaryBook(days=mapOf("2026-10-13" to free)));assertEquals(200,free.blocks.map {it.x to it.y}.distinct().size)
        val layout=NotebookRules.capture(full,"많은 카드");val reapplied=NotebookRules.apply(DiaryEntry(),layout)
        NotebookRules.validate(DiaryBook(days=mapOf("2026-10-13" to reapplied),layouts=listOf(layout)));assertEquals(200,reapplied.blocks.size)
        assertThrows(IllegalArgumentException::class.java) {NotebookRules.validate(DiaryBook(equipment=Equipment("astral-orb","none")))}
    }
    @Test fun legacyAndroidContentAndCheckedStateSurviveModelExtension() {
        val entry=NotebookJson.legacy("""{"theme":2,"character":"🐱","blocks":[{"id":"old","type":"todo","text":"이전 기록","checked":true}]}""")
        assertEquals("🐱",entry.character);assertTrue(entry.blocks.single().checked)
        assertEquals(entry,NotebookJson.decode(NotebookJson.encode(DiaryBook(days=mapOf("2026-10-08" to entry)))).days.getValue("2026-10-08"))
    }
    @Test fun queuedEditsUseTheLastPersistedRewardAndActualWriteDay() {
        val memory=Memory();var day=LocalDate.of(2026,10,13)
        data class Request(val book: DiaryBook,val date: String?,val done: (Result<DiaryBook>)->Unit)
        val queue=mutableListOf<Request>()
        val c=NotebookController(memory,today={day},persist={book,date,done->queue+=Request(book,date,done)})
        fun finish(request: Request) {val prepared=c.prepareSave(request.book,request.date);memory.saveBook(prepared);request.done(Result.success(prepared))}
        c.add("text");c.updateBlock(c.entry.blocks.single().copy(text="첫 글"))
        val first=queue.last();queue.clear();finish(first)
        assertTrue(c.saved);assertEquals(setOf("2026-10-13"),c.book.awardedDays)
        c.updateBlock(c.entry.blocks.single().copy(text="둘째 글"));c.edit(c.entry.copy(theme=2))
        day=day.plusDays(1);finish(queue.last())
        assertEquals(setOf("2026-10-13","2026-10-14"),c.book.awardedDays);assertEquals("둘째 글",c.book.days.getValue("2026-10-13").blocks.single().text)
        c.edit(c.entry.copy(theme=1));finish(queue.last());assertEquals(20,NotebookRules.xp(c.book))
    }
    @Test fun malformedNumbersAreRejectedAndOldCharacterAliasesMigrate() {
        val raw=NotebookJson.encode(DiaryBook(days=mapOf("2026-10-13" to DiaryEntry(blocks=listOf(DiaryBlock(type="text"))))))
        val broken=JSONObject(raw);broken.getJSONArray("Days").getJSONObject(0).getJSONObject("Value").getJSONArray("Blocks").getJSONObject(0).put("X","oops")
        assertThrows(IllegalArgumentException::class.java) {NotebookJson.decode(broken.toString())}
        val alias=JSONObject(raw).put("CharacterStyle","cube");assertEquals("crystal",NotebookJson.decode(alias.toString()).characterStyle)
    }
    @Test fun reorderingMeaningfulBlocksOnAnotherDayDoesNotAwardGrowth() {
        val memory=Memory();var day=LocalDate.of(2026,10,13);val c=NotebookController(memory,today={day})
        c.add("text");c.add("todo");c.updateBlock(c.entry.blocks[0].copy(text="첫 기록"));c.updateBlock(c.entry.blocks[1].copy(text="할 일"))
        day=day.plusDays(1);c.move(c.entry.blocks[0].id,1)
        assertEquals(setOf("2026-10-13"),c.book.awardedDays);assertEquals("할 일",c.entry.blocks[0].text)
    }
}
