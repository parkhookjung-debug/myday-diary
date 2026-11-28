package com.myday.diary.diary

import com.myday.diary.data.*
import org.junit.Assert.*
import org.junit.Test
import java.time.LocalDate
import org.json.JSONObject

class FeatureParityTest {
    @Test fun checklistKeepsPrioritiesThroughLegacyEditsAndBackup() {
        val old=TodoRules.update(DiaryBlock(type="todo"),listOf(DiaryTodoItem("일기",true,1),DiaryTodoItem("산책",false,2)))
        assertFalse(old.checked)
        val reordered=old.copy(text="산책\n일기\n책 읽기")
        assertEquals(listOf(2,1,0),TodoRules.items(reordered).map {it.importance})
        assertEquals(listOf(false,true,false),TodoRules.items(reordered).map {it.checked})
        val updated=TodoRules.update(reordered,TodoRules.items(reordered))
        val book=DiaryBook(days=mapOf("2026-11-28" to DiaryEntry(blocks=listOf(updated))))
        assertEquals(updated,NotebookJson.decode(NotebookJson.encode(book)).days.values.single().blocks.single())
        assertTrue(TodoRules.items(updated.copy(checked=true)).all {it.checked})
        val blank=NotebookRules.apply(DiaryEntry(),NotebookRules.capture(DiaryEntry(blocks=listOf(updated)),"할 일 형식")).blocks.single()
        assertTrue(blank.text.isEmpty() && blank.todoItems==null && !blank.checked)
    }
    @Test fun individualTodoCompletionAwardsOnceAndPriorityAloneNeverAwards() {
        class Memory:NotebookRepository {var book=DiaryBook();override fun loadBook()=book;override fun saveBook(book:DiaryBook){this.book=book}}
        var day=LocalDate.of(2026,11,28);val c=NotebookController(Memory(),today={day});c.add("todo")
        c.updateBlock(TodoRules.update(c.entry.blocks.single(),listOf(DiaryTodoItem("물 마시기"),DiaryTodoItem("산책"))))
        val before=NotebookRules.xp(c.book)
        c.updateBlock(TodoRules.update(c.entry.blocks.single(),listOf(DiaryTodoItem("물 마시기",true),DiaryTodoItem("산책"))))
        assertEquals(before+5,NotebookRules.xp(c.book));assertFalse(c.entry.blocks.single().checked)
        c.updateBlock(TodoRules.update(c.entry.blocks.single(),listOf(DiaryTodoItem("물 마시기",true),DiaryTodoItem("산책",true))))
        assertEquals(before+5,NotebookRules.xp(c.book))
        day=day.plusDays(1);val unchanged=NotebookRules.xp(c.book)
        c.updateBlock(TodoRules.update(c.entry.blocks.single(),TodoRules.items(c.entry.blocks.single()).map {it.copy(importance=1)}))
        assertEquals(unchanged,NotebookRules.xp(c.book))
    }
    @Test fun anniversariesUseActualLeapDayAndOneTimeDates() {
        val yearly=Anniversary("leap","윤년 생일","2024-02-29",true)
        assertTrue(AnniversaryRules.matches(yearly,LocalDate.of(2028,2,29)))
        assertFalse(AnniversaryRules.matches(yearly,LocalDate.of(2027,2,28)))
        assertFalse(AnniversaryRules.matches(yearly.copy(repeatYearly=false),LocalDate.of(2028,2,29)))
    }
    @Test fun portableVideoReferencePreservesAllThreePlatformsAndRejectsNetworkLaunches() {
        for(path in listOf("C:\\Videos\\산책.mp4","/Users/test/산책.mov","content://media/external/video/media/5")) {
            val block=DiaryBlock(type="video",text="영상 설명",videoPath=path,width=340,height=360)
            val book=DiaryBook(days=mapOf("2026-11-28" to DiaryEntry(blocks=listOf(block))))
            val raw=NotebookJson.encode(book);assertEquals(10,JSONObject(raw).getInt("Version"))
            assertEquals(block,NotebookJson.decode(raw).days.values.single().blocks.single())
            val layout=NotebookRules.capture(book.days.values.single(),"영상 형식");assertNull(layout.blocks.single().videoPath)
        }
        for(path in listOf("https://example.com/a.mp4","file://server/a.mp4","\\\\server\\a.mp4","//server/a.mp4","relative.mp4","/tmp/a\u0000.mp4"))assertFalse(path,VideoRules.validReference(path))
        val raw=NotebookJson.encode(DiaryBook(days=mapOf("2026-11-28" to DiaryEntry(blocks=listOf(DiaryBlock(type="text",text="old"))))))
        val root=JSONObject(raw);val b=root.getJSONArray("Days").getJSONObject(0).getJSONObject("Value").getJSONArray("Blocks").getJSONObject(0);b.put("MacVideoPath","/tmp/old.mp4").put("MacVideoText","이전 설명")
        val migrated=NotebookJson.decode(root.toString()).days.values.single().blocks.single();assertEquals("video",migrated.type);assertEquals("이전 설명",migrated.text);assertEquals("/tmp/old.mp4",migrated.videoPath)
    }
}
