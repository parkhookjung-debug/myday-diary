package com.myday.diary

import android.graphics.Bitmap
import android.util.Base64
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.test.*
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.test.platform.app.InstrumentationRegistry
import com.myday.diary.data.*
import com.myday.diary.diary.*
import com.myday.diary.platform.*
import com.myday.diary.ui.design.MyDayTheme
import com.myday.diary.ui.diary.PhoneNotebookScreen
import org.junit.*
import org.junit.Assert.*
import java.time.LocalDate
import java.io.File
import java.io.ByteArrayOutputStream

/** Uses an in-memory notebook. Never clear or overwrite a user's actual diary. */
class PhoneNotebookDeviceTest {
    @get:Rule val ui=createComposeRule()
    private val context=InstrumentationRegistry.getInstrumentation().targetContext
    private val day=LocalDate.of(2026,10,13)
    private class Memory(var book: DiaryBook): NotebookRepository {override fun loadBook()=book;override fun saveBook(book: DiaryBook) {this.book=book}}
    private lateinit var c: NotebookController
    private lateinit var catalog: NotebookCatalog
    private fun start(free: Boolean=false) {
        catalog=NotebookCatalog(context)
        val entry=DiaryEntry(layoutMode=if(free) "free" else "cards",blocks=listOf(
            DiaryBlock(id="text",type="text",text="예시 기록",width=420,height=280,x=12,y=12),
            DiaryBlock(id="todo",type="todo",text="일기 한 줄 쓰기",width=420,height=250,x=12,y=310),
            DiaryBlock(id="habit",type="habit",text="산책 20분",width=420,height=250,x=448,y=310)))
        c=NotebookController(Memory(DiaryBook(days=mapOf(day.toString() to entry))),today={day})
        ui.setContent {MyDayTheme {PhoneNotebookScreen(c,catalog,rememberDiaryHaptics(true),HomeScreenActions(context),false,
            onPhoto={id->val image=Bitmap.createBitmap(16,8,Bitmap.Config.ARGB_8888);val bytes=ByteArrayOutputStream();image.compress(Bitmap.CompressFormat.JPEG,85,bytes);image.recycle()
                val data=Base64.encodeToString(bytes.toByteArray(),Base64.NO_WRAP)
                if(id==null) c.edit(NotebookRules.append(c.entry,listOf(DiaryBlock(type="photo",photo=data,height=360))),true)
                else c.updateBlock(c.entry.blocks.first {it.id==id}.copy(type="photo",photo=data))},onExport={},onImport={})}}
    }
    private fun screenshot(name: String) {
        ui.waitForIdle()
        Thread.sleep(500) // Let the Android render thread publish the composed frame before capture.
        val image=InstrumentationRegistry.getInstrumentation().uiAutomation.takeScreenshot()
        val folder=File(context.getExternalFilesDir(null),"previews");folder.mkdirs()
        File(folder,"$name.png").outputStream().use {image.compress(Bitmap.CompressFormat.PNG,100,it)};image.recycle()
    }
    @Test fun phoneDiaryPhotoCalendarAndThemes() {
        start();screenshot("android-diary")
        ui.onNodeWithTag("diary-list").performScrollToNode(hasText("예시 기록"))
        ui.onNodeWithText("예시 기록").performTextReplacement("휴대폰에서 쓴 기록")
        ui.runOnIdle {assertEquals("휴대폰에서 쓴 기록",c.book.days.getValue(day.toString()).blocks.first().text);assertEquals(10,NotebookRules.xp(c.book))}
        ui.onNodeWithTag("add-block").performClick();ui.onNodeWithText("사진").performClick()
        ui.onNodeWithTag("diary-list").performScrollToNode(hasContentDescription("일기에 첨부한 사진"));screenshot("android-photo")
        ui.onNodeWithText("달력").performClick();screenshot("android-calendar")
        ui.onNodeWithText("본문·사진 설명 검색").performTextInput("휴대폰")
        ui.onNodeWithText("휴대폰에서 쓴 기록").assertExists()
        ui.onNodeWithText("설정").performClick();ui.onNodeWithText("라벤더").performClick();screenshot("android-settings")
        ui.runOnIdle {assertEquals(2,c.entry.theme);NotebookPhoto.validate(c.book)}
    }
    @Test fun exactCharacterEquipmentAndGrowthMenus() {
        start();ui.onNodeWithText("상몬").performClick();screenshot("android-sangmon")
        ui.onNodeWithTag("sangmon-grid").performScrollToNode(hasText("빙결 상몬"));ui.onNodeWithText("빙결 상몬").performClick()
        ui.runOnIdle {assertEquals("frost",c.book.characterStyle)}
        ui.onNodeWithText("소품 300종").performClick();ui.onNodeWithTag("equipment-grid").performScrollToNode(hasTestTag("equipment-frost-sword"));ui.onNodeWithTag("equipment-frost-sword").performClick()
        ui.onNodeWithText("작은 소품").performClick();ui.onNodeWithTag("equipment-grid").performScrollToNode(hasTestTag("equipment-astral-orb"));ui.onNodeWithTag("equipment-astral-orb").performClick();screenshot("android-equipment")
        ui.runOnIdle {assertEquals(Equipment("frost-sword","astral-orb"),c.book.equipment);assertEquals(0,NotebookRules.xp(c.book));assertEquals(64,catalog.characters.size);assertEquals(300,catalog.items.size)}
    }
    @Test fun freeBoardDragTemplateAndSavedLayoutsRetainContent() {
        start(true)
        ui.onNodeWithTag("diary-list").performScrollToNode(hasText("배치 편집"));ui.onNodeWithText("배치 편집").performClick()
        ui.onNodeWithTag("diary-list").performScrollToNode(hasTestTag("free-block-text"))
        ui.onNodeWithTag("free-block-text").performTouchInput {down(center);advanceEventTime(650);moveBy(Offset(50f,60f));advanceEventTime(30);up()}
        ui.runOnIdle {assertTrue(c.entry.blocks.first().x>12);assertEquals("예시 기록",c.entry.blocks.first().text)};screenshot("android-free-layout")
        ui.onNodeWithTag("diary-list").performScrollToNode(hasText("일기 형식 100종"));ui.onNodeWithText("일기 형식 100종").performClick()
        ui.onNodeWithText("분야·이름·질문 검색").performTextInput("코넬")
        val template=catalog.templates.first {it.layout=="cornell"}
        ui.onNodeWithText(template.name).performClick();screenshot("android-templates")
        ui.onNodeWithText("이 형식으로 기록하기").performScrollTo().performClick()
        ui.runOnIdle {assertEquals(3+template.blocks.size,c.entry.blocks.size);assertEquals("예시 기록",c.entry.blocks.first().text)}
        ui.onNodeWithTag("diary-list").performScrollToNode(hasText("내 레이아웃"));ui.onNodeWithText("내 레이아웃").performClick();ui.onNodeWithText("현재 배치 저장").performClick()
        ui.onNodeWithText("이름 (1~40자)").performTextInput("휴대폰 배치");ui.onNodeWithText("저장",substring=false).performClick();screenshot("android-my-layouts")
        ui.runOnIdle {assertEquals("휴대폰 배치",c.book.layouts.single().name);assertTrue(c.book.layouts.single().blocks.all {it.text.isEmpty() && !it.checked})}
    }
    @Test fun attendanceAndSearchableExtraSlotsKeepIndependentSelections() {
        start();ui.onNodeWithText("상몬").performClick();ui.onNodeWithText("출석 +5 XP").performClick()
        ui.onNodeWithText("출석 완료").assertIsNotEnabled();ui.runOnIdle {assertEquals(5,NotebookRules.xp(c.book))}
        ui.onNodeWithText("소품 300종").performClick()
        listOf(Triple("머리","크림 리본 머리띠","solar-crown"),Triple("얼굴","하늘 동그란 안경","frost-glasses"),Triple("발밑","하늘 폭신 구름","frost-cloud")).forEach {(slot,name,id)->
            ui.onNodeWithTag("equipment-slots").performScrollToNode(hasText(slot));ui.onNodeWithText(slot).performClick()
            ui.onNodeWithTag("equipment-search").performTextClearance();ui.onNodeWithTag("equipment-search").performTextInput(name)
            ui.onNodeWithTag("equipment-search").performImeAction()
            ui.onNodeWithTag("equipment-grid").performScrollToNode(hasTestTag("equipment-$id"))
            ui.onNodeWithTag("equipment-$id").performClick()
        }
        ui.runOnIdle {assertEquals("solar-crown",c.book.equipment.headId);assertEquals("frost-glasses",c.book.equipment.faceId);assertEquals("frost-cloud",c.book.equipment.groundId);assertEquals(5,NotebookRules.xp(c.book))}
        screenshot("android-wardrobe-growth")
    }
}
