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
    @Test fun offlineFriendPreviewKeepsLocalLoungeAvailable() {
        start()
        ui.onNodeWithTag("nav-3").performClick()
        ui.onNodeWithText("친구 일기 공유 미리보기").assertExists()
        ui.onNodeWithText("카카오로 로그인").assertDoesNotExist()
        screenshot("android-offline-friends")
        ui.onNodeWithText("채팅 라운지").performClick()
        ui.onNodeWithText("서버 주소").assertExists()
    }

    @Test fun sharedGuideOpensNavigatesAndStartsWriting() {
        start();ui.onNodeWithTag("nav-4").performClick();ui.onNodeWithTag("guide-open").performScrollTo().performClick()
        ui.onNodeWithTag("guide-tab-2").performClick();ui.onNodeWithTag("guide-tab-2").assertIsSelected();screenshot("android-welcome-rewards")
        ui.onNodeWithTag("guide-tab-0").performClick();ui.onNodeWithTag("guide-action-0").performScrollTo().performClick()
        ui.runOnIdle{assertEquals(4,c.entry.blocks.size)}
    }
    private fun screenshot(name: String) {
        ui.waitForIdle()
        Thread.sleep(500) // Let the Android render thread publish the composed frame before capture.
        val image=InstrumentationRegistry.getInstrumentation().uiAutomation.takeScreenshot()
        val folder=File(context.getExternalFilesDir(null),"previews");folder.mkdirs()
        File(folder,"$name.png").outputStream().use {image.compress(Bitmap.CompressFormat.PNG,100,it)};image.recycle()
    }
    @Test fun commonNavigationAndLoungeValidateBeforeNetwork() {
        start();ui.onNodeWithTag("nav-3").performClick();ui.onNodeWithText("채팅 라운지").performClick()
        ui.onNodeWithTag("chat-address").performTextInput("http://example.org/")
        ui.onNodeWithTag("chat-nickname").performTextInput("상몬친구")
        ui.onNodeWithTag("chat-code").performTextInput("local-test-only")
        ui.onNodeWithTag("chat-join").performScrollTo().performClick()
        ui.onNodeWithText("HTTP는 로컬·사설 IP에서만 사용할 수 있어요.").assertExists()
        screenshot("android-lounge")
        ui.onNodeWithTag("nav-4").performClick();ui.onNodeWithText("소중한 하루 보관하기").assertExists()
        ui.onNodeWithTag("nav-0").performClick()
        ui.runOnIdle {assertEquals("예시 기록",c.entry.blocks.first().text);assertEquals(0,NotebookRules.xp(c.book))}
    }
    @Test fun journalToolsMonthlyReportAndRecoveryNavigation() {
        start();ui.onNodeWithTag("nav-4").performClick();ui.onNodeWithTag("journal-tools-open").performClick()
        ui.onNodeWithText("1. 복구").assertExists();ui.onNodeWithText("2. 리포트").performClick()
        ui.onNodeWithText("기록한 날  1일",substring=true).assertExists();screenshot("android-monthly-report")
        ui.onNodeWithText("3. 알림").performClick();ui.onNodeWithText("알림 설정 적용").assertExists();screenshot("android-reminder")
        ui.onNodeWithText("4. Wi-Fi·QR").performClick();ui.onNodeWithText("같은 Wi-Fi에서 기기 연결").assertExists();screenshot("android-device-link")
        ui.onNodeWithText("6. 업데이트").performClick();ui.onNodeWithText("현재 버전 ${JournalPolicy.appVersion}").assertExists();screenshot("android-journal-updates")
    }
    @Test fun deletedPhotoCanBeRecoveredWithCaptionAndPlacement() {
        val root=File(context.cacheDir,"journal-history-test-${java.util.UUID.randomUUID()}");root.mkdirs()
        try {
            val image=Bitmap.createBitmap(16,8,Bitmap.Config.ARGB_8888);val bytes=ByteArrayOutputStream();image.compress(Bitmap.CompressFormat.JPEG,85,bytes);image.recycle()
            val data=Base64.encodeToString(bytes.toByteArray(),Base64.NO_WRAP)
            val original=DiaryEntry(blocks=listOf(DiaryBlock(type="photo",text="사진 설명",photo=data,x=80,width=420,height=320)))
            val archive=JournalHistory(root);val book=DiaryBook(days=mapOf(day.toString() to original));archive.beforeSave(book,book.copy(days=emptyMap()))
            val recovered=archive.list().single().entry.blocks.single()
            assertEquals(data,recovered.photo);assertEquals("사진 설명",recovered.text);assertEquals(80,recovered.x);assertEquals(420,recovered.width)
        }finally {root.deleteRecursively()}
    }
    @Test fun photoAlbumReturnsToTheOriginalDiaryAndBackupsStayLocal(){
        start();ui.runOnIdle{val image=Bitmap.createBitmap(20,16,Bitmap.Config.ARGB_8888);val bytes=ByteArrayOutputStream();image.compress(Bitmap.CompressFormat.JPEG,85,bytes);image.recycle();c.edit(c.entry.copy(blocks=c.entry.blocks+DiaryBlock(id="album-photo",type="photo",text="산책 사진",photo=Base64.encodeToString(bytes.toByteArray(),Base64.NO_WRAP))))}
        ui.onNodeWithTag("nav-4").performClick();ui.onNodeWithTag("journal-tools-open").performClick();ui.onNodeWithText("사진 모아보기",useUnmergedTree=true).performClick()
        ui.onNodeWithTag("album-album-photo").assertExists();screenshot("android-photo-album");ui.onNodeWithTag("album-album-photo").performClick()
        ui.onNodeWithTag("nav-0").assertIsSelected();ui.runOnIdle{assertEquals(day,c.date);assertEquals("산책 사진",c.entry.blocks.last().text)}
        ui.onNodeWithTag("nav-4").performClick();ui.onNodeWithTag("journal-tools-open").performClick();ui.onNodeWithText("자동 백업",useUnmergedTree=true).performClick();ui.onNodeWithText("지금 전체 백업").assertExists();screenshot("android-automatic-backup")
    }


    @Test fun phoneDiaryPhotoCalendarAndThemes() {
        start();screenshot("android-diary")
        ui.onNodeWithTag("diary-list").performScrollToNode(hasText("예시 기록"))
        ui.onNodeWithText("예시 기록").performTextReplacement("휴대폰에서 쓴 기록")
        ui.runOnIdle {assertEquals("휴대폰에서 쓴 기록",c.book.days.getValue(day.toString()).blocks.first().text);assertEquals(10,NotebookRules.xp(c.book))}
        ui.onNodeWithTag("diary-list").performScrollToNode(hasTestTag("add-block"));ui.onNodeWithTag("add-block").performClick();ui.onNodeWithText("사진").performClick()
        ui.onNodeWithTag("diary-list").performScrollToNode(hasContentDescription("일기에 첨부한 사진"));screenshot("android-photo")
        ui.onNodeWithTag("nav-1").performClick();screenshot("android-calendar")
        ui.onNodeWithText("본문·사진 설명 검색").performTextInput("휴대폰")
        ui.onNodeWithText("본문·사진 설명 검색").performImeAction()
        ui.onNodeWithTag("calendar-list").performScrollToNode(hasText("휴대폰에서 쓴 기록"));ui.onNodeWithText("휴대폰에서 쓴 기록").assertExists()
        ui.onNodeWithTag("nav-4").performClick();ui.onNodeWithText("라벤더").performClick();screenshot("android-settings")
        ui.runOnIdle {assertEquals(2,c.entry.theme);NotebookPhoto.validate(c.book)}
    }

    @Test fun freeBoardDragTemplateAndSavedLayoutsRetainContent() {
        start(true)
        ui.onNodeWithTag("diary-list").performScrollToNode(hasText("배치 편집"));ui.onNodeWithText("배치 편집").performClick()
        ui.onNodeWithTag("diary-list").performScrollToNode(hasTestTag("free-block-text"))
        ui.onNodeWithTag("free-block-text").performTouchInput {down(center);advanceEventTime(650);moveBy(Offset(50f,60f));advanceEventTime(30);up()}
        ui.runOnIdle {assertTrue(c.entry.blocks.first().x>12);assertEquals("예시 기록",c.entry.blocks.first().text)};screenshot("android-free-layout")
        ui.onNodeWithTag("diary-list").performScrollToNode(hasText("일기 형식 · 블록 추가"));ui.onNodeWithText("일기 형식 · 블록 추가").performClick()
        ui.onNodeWithText("분야·이름·질문 검색").performTextInput("코넬")
        val template=catalog.templates.first {it.layout=="cornell"}
        ui.onNodeWithText(template.name).performClick();screenshot("android-templates")
        ui.onNodeWithTag("apply-template").assertIsDisplayed().performClick()
        ui.runOnIdle {assertEquals(3+template.blocks.size,c.entry.blocks.size);assertEquals("예시 기록",c.entry.blocks.first().text)}
        ui.onNodeWithTag("diary-list").performScrollToNode(hasText("내 레이아웃"));ui.onNodeWithText("내 레이아웃").performClick();ui.onNodeWithText("현재 배치 저장").performClick()
        ui.onNodeWithText("이름 (1~40자)").performTextInput("휴대폰 배치");ui.onNodeWithText("저장",substring=false).performClick();screenshot("android-my-layouts")
        ui.runOnIdle {assertEquals("휴대폰 배치",c.book.layouts.single().name);assertTrue(c.book.layouts.single().blocks.all {it.text.isEmpty() && !it.checked})}
    }


    @Test fun studioRoomTagsAndDailyPromptPersist() {
        start()
        ui.onNodeWithText("하루 꾸미기").performClick()
        ui.onNodeWithText("1 사진·스티커").assertExists();screenshot("android-studio-photos")
        ui.onNodeWithText("2 상몬의 방").performClick()
        ui.onNodeWithText("화분 +").performClick()
        ui.runOnIdle {assertEquals(9,c.book.version);assertEquals("plant",c.book.studio!!.furniture.single().kind)}
        screenshot("android-studio-room")
        ui.onNodeWithText("3 감정 달력").performClick();screenshot("android-studio-moods")
        ui.onNodeWithText("4 태그·즐겨찾기").performScrollTo().performClick()
        ui.onNodeWithTag("studio-tags").performTextInput("산책, 학교")
        ui.onNodeWithText("이날의 분류 저장").performClick()
        ui.runOnIdle {assertEquals(listOf("산책","학교"),c.book.studio!!.note(day.toString()).tags)}
        screenshot("android-studio-tags")
        ui.onNodeWithText("5 오늘의 질문").performScrollTo().performClick();screenshot("android-studio-question")
        ui.onNodeWithText("이 질문으로 일기 쓰기").performClick()
        ui.runOnIdle {assertTrue(c.entry.blocks.any {it.prompt==com.myday.diary.data.StudioData.question(day)})}
    }
    @Test fun studioPhotoToolsProduceValidPortableImages() {
        val image=Bitmap.createBitmap(400,200,Bitmap.Config.ARGB_8888)
        val cropped=com.myday.diary.ui.diary.PhotoDecor.crop(image,1.0)
        assertEquals(200,cropped.width);assertEquals(200,cropped.height);assertEquals(400,image.width)
        val framed=com.myday.diary.ui.diary.PhotoDecor.frame(cropped,0)
        assertTrue(framed.width>cropped.width)
        val encoded=com.myday.diary.ui.diary.PhotoDecor.encode(framed)
        val book=com.myday.diary.data.DiaryBook(days=mapOf(day.toString() to com.myday.diary.data.DiaryEntry(blocks=listOf(com.myday.diary.data.DiaryBlock(type="photo",photo=encoded)))))
        com.myday.diary.data.NotebookPhoto.validate(book)
        framed.recycle();if(cropped!==image)cropped.recycle();image.recycle()
        start();ui.onNodeWithText("하루 꾸미기").performClick()
        ui.onNodeWithText("꽃 +").performClick();ui.waitForIdle()
        ui.waitUntil(5000){c.entry.blocks.any{it.title=="꽃 스티커"}}
        ui.runOnIdle {com.myday.diary.data.NotebookPhoto.validate(c.book)}
        val original=c.entry.blocks.single {it.title=="꽃 스티커"}.photo
        ui.onNodeWithText("사진 꾸미기 · 사진").performClick()
        screenshot("android-photo-editor-open")
        ui.waitUntil(10000){ui.onAllNodesWithText("정사각 자르기").fetchSemanticsNodes().isNotEmpty()}
        ui.onNodeWithText("정사각 자르기").performScrollTo().performClick()
        ui.onNodeWithText("라벤더 테두리").performScrollTo().performClick()
        ui.onNodeWithText("별").performScrollTo().performClick()
        ui.onNodeWithContentDescription("편집 미리보기").performScrollTo().performTouchInput {click(center)}
        screenshot("android-photo-editor")
        ui.onNodeWithText("이 사진 적용").performScrollTo().performClick()
        ui.runOnIdle {assertNotEquals(original,c.entry.blocks.single {it.title=="꽃 스티커"}.photo);com.myday.diary.data.NotebookPhoto.validate(c.book)}
        ui.onNodeWithText("6 일기 잠금").performScrollTo().performClick();screenshot("android-studio-privacy")
        ui.onNodeWithText("현재 꺼짐").assertExists()
    }

    @Test fun focusedGrowthRetainsLegacyBackupAndOnlyBundlesOriginalArt() {
        start()
        val previousStyle=c.book.characterStyle;val previousEquipment=c.book.equipment
        ui.onNodeWithTag("nav-2").performClick();ui.onNodeWithTag("growth-screen").assertExists()
        ui.onNodeWithText("상몬 64종").assertDoesNotExist();ui.onNodeWithText("소품 300종").assertDoesNotExist();ui.onNodeWithText("소품 뽑기").assertDoesNotExist()
        ui.onNodeWithTag("growth-checkin").performClick();ui.onNodeWithTag("growth-checkin").assertIsNotEnabled()
        ui.runOnIdle {assertEquals(5,NotebookRules.xp(c.book));assertEquals(previousStyle,c.book.characterStyle);assertEquals(previousEquipment,c.book.equipment)
            val assets=androidx.test.platform.app.InstrumentationRegistry.getInstrumentation().targetContext.assets
            assertEquals(listOf("original.png"),assets.list("sangmon")!!.toList())
            assertTrue(assets.list("equipment").isNullOrEmpty());assertTrue(assets.list("items").isNullOrEmpty())
        }
        screenshot("android-focused-growth")
        ui.onNodeWithTag("nav-3").performClick();ui.onNodeWithText("채팅 라운지").performClick();ui.onNodeWithText("서버 주소").assertExists()
    }
}