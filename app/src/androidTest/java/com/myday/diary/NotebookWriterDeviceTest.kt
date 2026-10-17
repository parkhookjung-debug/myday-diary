package com.myday.diary

import android.app.Application
import android.content.Context
import android.content.SharedPreferences
import androidx.lifecycle.ViewModelStore
import androidx.test.platform.app.InstrumentationRegistry
import com.myday.diary.data.*
import com.myday.diary.diary.NotebookViewModel
import org.junit.Test
import org.junit.Assert.*
import java.io.File
import java.util.UUID

/** Exercise the real async writer with private test storage, never the user's book or widget. */
class NotebookWriterDeviceTest {
    @Test fun rapidEditsFailureRetryAndClosingActivityPreserveTheLastWrite() {
        val instrumentation=InstrumentationRegistry.getInstrumentation()
        val target=instrumentation.targetContext
        val identifier="myday-writer-test-${UUID.randomUUID()}"
        val directory=File(target.cacheDir,identifier).apply {mkdirs()}
        val application=object: Application() {
            init {attachBaseContext(target)}
            override fun getApplicationContext(): Context=this
            override fun getFilesDir(): File=directory
            override fun getSharedPreferences(name: String,mode: Int): SharedPreferences=target.getSharedPreferences("$identifier-$name",mode)
            override fun getSystemService(name: String): Any?=if(name==Context.APPWIDGET_SERVICE) null else super.getSystemService(name)
        }
        val store=ViewModelStore()
        lateinit var model: NotebookViewModel
        fun main(work: ()->Unit)=instrumentation.runOnMainSync(work)
        fun await(condition: ()->Boolean) {val deadline=System.currentTimeMillis()+15000;while(!condition()) {if(System.currentTimeMillis()>deadline) error("Writer did not finish");Thread.sleep(25)}}
        try {
            main {model=NotebookViewModel(application);store.put("notebook",model)}
            await {model.controller!=null || model.loadError!=null};assertNull(model.loadError)
            val c=model.controller!!
            main {c.add("text");repeat(80) {i->c.updateBlock(c.entry.blocks.single().copy(text="연속 입력 $i"))}}
            await {c.saved};assertNull(c.error)
            val disk=DiaryStore(application);val day=c.date.toString()
            assertEquals("연속 입력 79",disk.loadBook().days.getValue(day).blocks.single().text)
            assertEquals(1,disk.loadBook().awardedDays.size)
            val previous=File(directory,"notebook.json").readText();directory.setWritable(false,false)
            main {c.updateBlock(c.entry.blocks.single().copy(text="실패한 입력도 보존"))}
            await {!c.saving && c.error!=null};assertFalse(c.saved);assertEquals(previous,File(directory,"notebook.json").readText())
            directory.setWritable(true,false);main {c.retry()};await {c.saved};assertNull(c.error)
            assertEquals("실패한 입력도 보존",disk.loadBook().days.getValue(day).blocks.single().text)
            main {c.updateBlock(c.entry.blocks.single().copy(text="종료 직전 마지막 입력"));store.clear()}
            // A separate reader shares the file lock, including while the final request drains.
            await {disk.loadBook().days[day]?.blocks?.single()?.text=="종료 직전 마지막 입력"}
            assertEquals(1,disk.loadBook().awardedDays.size)
        } finally {
            main {store.clear()};directory.setWritable(true,false)
            require(directory.canonicalFile.parentFile==target.cacheDir.canonicalFile && directory.name.startsWith("myday-writer-test-"))
            directory.deleteRecursively()
            listOf("diary","diary-home").forEach {target.getSharedPreferences("$identifier-$it",Context.MODE_PRIVATE).edit().clear().commit()}
        }
    }
}
