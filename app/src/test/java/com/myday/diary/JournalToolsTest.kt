package com.myday.diary

import com.myday.diary.data.*
import com.myday.diary.diary.NotebookController
import com.myday.diary.platform.JournalReminder
import org.junit.Assert.*
import org.junit.Test
import java.time.*
import java.nio.file.Files

class JournalToolsTest {
    @Test fun leapMonthBlankDaysAndHabitDenominator() {
        val page=DiaryEntry(blocks=listOf(DiaryBlock(type="text",text="한 줄"),DiaryBlock(type="habit",text="산책",checked=true),DiaryBlock(type="habit")),mood="행복해요")
        val book=DiaryBook(days=mapOf("2024-02-27" to DiaryEntry(),"2024-02-28" to page,"2024-02-29" to page,"2024-03-01" to page))
        val report=JournalReport.forMonth(book,YearMonth.of(2024,2))
        assertEquals(2,report.days);assertEquals(2,report.streak);assertEquals(2,report.habits);assertEquals(2,report.completed);assertEquals(2,report.moods["행복해요"])
        assertEquals(0,JournalReport.forMonth(book,YearMonth.of(2023,2)).days)
    }
    @Test fun archiveBeforeRemovalAndNoGrowthOnRestore() {
        val root=Files.createTempDirectory("myday-history").toFile()
        try {
            val archive=JournalHistory(root);val original=DiaryEntry(blocks=listOf(DiaryBlock(type="text",text="복구할 하루")))
            val book=DiaryBook(days=mapOf("2024-02-29" to original))
            archive.beforeSave(book,book);assertTrue(archive.list().isEmpty())
            archive.beforeSave(book,book.copy(days=emptyMap()));val revision=archive.list().single();assertEquals(original,revision.entry)
            archive.beforeSave(book,book.copy(days=emptyMap()));assertEquals(1,archive.list().size)
            val repo=object: NotebookRepository {var state=DiaryBook();override fun loadBook()=state;override fun saveBook(book: DiaryBook){state=book}}
            val controller=NotebookController(repo,{LocalDate.of(2024,2,29)});val xp=com.myday.diary.diary.NotebookRules.xp(controller.book)
            assertTrue(controller.restoreRevision(revision.date,revision.entry));assertEquals(xp,com.myday.diary.diary.NotebookRules.xp(controller.book));assertEquals("",controller.reaction)
            assertEquals(original,controller.book.days[revision.date]);archive.remove(revision);assertTrue(archive.list().isEmpty())
        }finally {root.deleteRecursively()}
    }
    @Test fun reminderSchedulesOneFutureLocalTimeAcrossDst() {
        val now=ZonedDateTime.of(2026,3,7,21,0,0,0,ZoneId.of("America/New_York"))
        val next=JournalReminder.nextTime(now,20*60);assertEquals(20,next.hour);assertEquals(8,next.dayOfMonth);assertTrue(next.isAfter(now))
        assertEquals(20,JournalReminder.nextTime(now.minusHours(2),20*60).hour)
    }
}
