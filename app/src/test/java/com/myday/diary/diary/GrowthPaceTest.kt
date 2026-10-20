package com.myday.diary.diary

import com.myday.diary.data.*
import org.junit.Assert.*
import org.junit.Test
import java.time.LocalDate

class GrowthPaceTest {
    private val start=LocalDate.of(2026,1,1)
    @Test fun realSavedActivitiesReachTheThreeExpectedCalendarDays() {
        SharedGrowth.profiles.forEach {profile->
            var book=DiaryBook();var days=0;var previous=0
            while(NotebookRules.level(book)<NotebookRules.MAX_LEVEL && days<1000) {
                val mask=profile[1+days%7];val day=start.plusDays(days.toLong());days++;previous=NotebookRules.xp(book)
                if(mask and NotebookRules.ATTENDANCE!=0)book=book.copy(activities=book.activities+(day.toString() to NotebookRules.ATTENDANCE))
                if(mask and 32!=0) {
                    val blocks=mutableListOf(DiaryBlock(id="text",type="text",text=if(mask and NotebookRules.WRITING!=0)"오늘은 산책을 하고 하루를 돌아보며 스무 글자가 넘는 기록을 남겼어요" else "작은 기록"))
                    if(mask and NotebookRules.PHOTO!=0)blocks+=DiaryBlock(id="photo",type="photo",photo="fixture")
                    if(mask and NotebookRules.TODO!=0)blocks+=DiaryBlock(id="todo",type="todo",checked=true)
                    if(mask and NotebookRules.HABIT!=0)blocks+=DiaryBlock(id="habit",type="habit",checked=true)
                    book=NotebookRules.award(book,DiaryEntry(blocks=emptyList()),DiaryEntry(blocks=blocks),day)
                }
            }
            assertEquals(profile[0],days);assertTrue(previous<NotebookRules.MAX_XP);assertTrue(NotebookRules.xp(book)>=NotebookRules.MAX_XP)
        }
    }
    @Test fun maximumRetainsHistoricalXpAndCloversAndContinuesEarning() {
        assertEquals(99,NotebookRules.levelFromXp(3589));assertEquals(100,NotebookRules.levelFromXp(3590));assertEquals(100,NotebookRules.levelFromXp(Int.MAX_VALUE))
        val old=DiaryBook(awardedDays=(0 until 1000).map {start.plusDays(it.toLong()).toString()}.toSet())
        assertEquals(10000,NotebookRules.xp(old));assertEquals(11175,CollectionRules.earned(old));assertEquals(50,NotebookRules.intoLevel(old))
        val next=old.copy(activities=mapOf(start.plusDays(1100).toString() to NotebookRules.ATTENDANCE))
        assertEquals(100,NotebookRules.level(next));assertTrue(CollectionRules.earned(next)>CollectionRules.earned(old))
        assertEquals(old,NotebookJson.decode(NotebookJson.encode(old)))
    }
    @Test fun weeklyBonusIsSeparateIdempotentAndRetainedAfterAGap() {
        val week=DiaryBook(activities=(0 until 7).associate {start.plusDays(it.toLong()).toString() to NotebookRules.ATTENDANCE})
        assertEquals(45,NotebookRules.xp(week));assertEquals(15,NotebookRules.todayXp(week,start.plusDays(6)));assertEquals(5,NotebookRules.todayActivityXp(week,start.plusDays(6)))
        assertEquals(45,NotebookRules.xp(NotebookRules.merge(week,week)))
        val broken=week.copy(activities=week.activities+(start.plusDays(8).toString() to NotebookRules.ATTENDANCE))
        assertEquals(50,NotebookRules.xp(broken));assertEquals(1,NotebookRules.streak(broken,start.plusDays(8)))
    }
}
