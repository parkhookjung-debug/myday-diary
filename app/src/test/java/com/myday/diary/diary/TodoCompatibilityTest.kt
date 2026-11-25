package com.myday.diary.diary

import com.myday.diary.data.*
import org.junit.Assert.*
import org.junit.Test

class TodoCompatibilityTest {
    private val block=DiaryBlock(type="todo",text="공부\n산책",todoItems=listOf(
        DiaryTodoItem("공부",true,1),DiaryTodoItem("산책",false,2)),todoLayoutVersion=1)
    private fun book(b: DiaryBlock)=DiaryBook(days=mapOf("2026-11-25" to DiaryEntry(blocks=listOf(b))))

    @Test fun windowsTodoMetadataSurvivesAndroidBackupAndFlatEdits() {
        val loaded=NotebookJson.decode(NotebookJson.encode(book(block))).days.getValue("2026-11-25").blocks.single()
        assertEquals(block,loaded)
        val edited=loaded.copy(text="산책\n공부\n새 할 일",checked=true)
        val restored=NotebookJson.decode(NotebookJson.encode(book(edited))).days.getValue("2026-11-25").blocks.single()
        assertEquals(edited,restored)
    }
    @Test fun savedLayoutsNeverCopyTaskContentOrChecks() {
        val layout=NotebookRules.capture(DiaryEntry(blocks=listOf(block)),"할 일 배치")
        assertNull(layout.blocks.single().todoItems)
        val applied=NotebookRules.apply(DiaryEntry(),layout).blocks.single()
        assertEquals("",applied.text);assertFalse(applied.checked);assertNull(applied.todoItems)
    }
    @Test fun malformedPriorityAndOversizedListsAreRejected() {
        for(b in listOf(block.copy(todoItems=List(101) {DiaryTodoItem("할 일")}),block.copy(todoItems=listOf(DiaryTodoItem("할 일",importance=3))))) {
            try {NotebookJson.encode(book(b));fail("Invalid todo metadata was accepted")} catch(expected: IllegalArgumentException) { }
        }
    }
}
