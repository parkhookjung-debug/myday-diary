package com.myday.diary

import android.webkit.WebView
import androidx.compose.ui.test.*
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.test.platform.app.InstrumentationRegistry
import com.myday.diary.data.*
import com.myday.diary.diary.*
import com.myday.diary.platform.rememberDiaryHaptics
import com.myday.diary.ui.design.MyDayTheme
import com.myday.diary.ui.diary.*
import org.junit.*
import org.junit.Assert.*
import java.io.File
import java.util.concurrent.atomic.AtomicReference
import org.json.JSONObject

class FeatureParityDeviceTest {
    @get:Rule val ui=createComposeRule()
    private val context=InstrumentationRegistry.getInstrumentation().targetContext
    @Test fun nativeChecklistEditsPartialCompletionAndPriority() {
        class Memory:NotebookRepository{var b=DiaryBook();override fun loadBook()=b;override fun saveBook(book:DiaryBook){b=book}}
        val c=NotebookController(Memory());c.add("todo")
        ui.setContent {MyDayTheme {PhoneTodoList(c.entry.blocks.single(),rememberDiaryHaptics(true),{c.updateBlock(it)})}}
        ui.onNodeWithTag("todo-input").performTextInput("산책")
        ui.onNodeWithTag("todo-add").performClick()
        ui.onNodeWithTag("todo-input").performTextInput("일기")
        ui.onNodeWithTag("todo-add").performClick()
        ui.onNodeWithTag("todo-priority-0-1").performClick();ui.onNodeWithTag("todo-check-0").performClick()
        ui.runOnIdle {val items=TodoRules.items(c.entry.blocks.single());assertEquals(2,items.size);assertEquals(1,items[0].importance);assertTrue(items[0].checked);assertFalse(c.entry.blocks.single().checked);assertTrue((c.book.activities[c.date.toString()]?:0) and NotebookRules.TODO!=0)}
        ui.onNodeWithTag("todo-all").performClick();ui.runOnIdle{assertTrue(c.entry.blocks.single().checked)}
    }
    @Test fun sevenBundledGamesRunInsideTheActualNativeWebView() {
        val folder=File(context.cacheDir,"playroom-device-${java.util.UUID.randomUUID()}")
        val view=AtomicReference<WebView>()
        ui.setContent {MyDayTheme {PhonePlayroom(onDismiss={},scoreDirectory=folder,onWebViewCreated={view.set(it)})}}
        fun js(source:String):String {val result=AtomicReference<String?>(null);ui.runOnIdle {view.get().evaluateJavascript(source){result.set(it)}};ui.waitUntil(10000){result.get()!=null};return result.get()!!}
        try {
            ui.waitUntil(10000){view.get()!=null}
            var ready=false
            repeat(100){if(!ready){ready=js("document.querySelectorAll('#cards .card').length")=="9";if(!ready)Thread.sleep(100)}}
            assertTrue("Bundled game hub loaded",ready)
            for((index,id) in GameScoreStore.ids.toList().withIndex()) {
                js("document.querySelectorAll('#cards .card')[$index].click()")
                assertEquals("\"$id\"",js("MyDayPlayroom.snapshot().id"))
                js("MyDayPlayroom.pause()")
                assertEquals("\"paused\"",js("MyDayPlayroom.snapshot().state"))
                js("window.confirm=()=>true;document.getElementById('back').click()")
            }
            js("MyDayNative.post(JSON.stringify({type:'score',token:'test',round:'device-test',id:'snack',score:30}));MyDayNative.post(JSON.stringify({type:'score',token:'test',round:'device-test',id:'snack',score:30}));true")
            ui.waitUntil(5000){File(folder,"games/scores.json").exists()}
            ui.runOnIdle {val record=GameScoreStore(folder).read().single();assertEquals(30,record.best);assertEquals(1,record.plays)}
            val previews=File(context.getExternalFilesDir(null),"previews");previews.mkdirs()
            val screenshot=InstrumentationRegistry.getInstrumentation().uiAutomation.takeScreenshot();File(previews,"android-playroom.png").outputStream().use {screenshot.compress(android.graphics.Bitmap.CompressFormat.PNG,100,it)};screenshot.recycle()
        } finally {folder.deleteRecursively()}
    }
    @Test fun importedVideoReferenceAsksForLocalReselection() {
        ui.setContent {MyDayTheme {PhoneVideo(DiaryBlock(id="foreign",type="video",videoPath="C:\\Videos\\sample.mp4"),{})}}
        ui.onNodeWithTag("video-play-foreign").performClick()
        ui.onNodeWithText("원본 영상을 찾지 못했어요. 이 기기의 영상을 다시 선택해 주세요.").assertExists()
    }
}
