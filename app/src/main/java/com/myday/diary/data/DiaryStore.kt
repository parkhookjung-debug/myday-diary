package com.myday.diary.data

import android.content.Context
import android.util.AtomicFile
import com.myday.diary.DiaryWidgetProvider
import java.io.File
import java.time.LocalDate

/** Migrate legacy preferences only after the canonical book is safely persisted. */
class DiaryStore(context: Context) : DiaryRepository, NotebookRepository {
    companion object {private val locks=java.util.concurrent.ConcurrentHashMap<String,Any>()}
    private val appContext=context.applicationContext
    private val prefs=context.getSharedPreferences("diary",Context.MODE_PRIVATE)
    private val file=AtomicFile(File(context.filesDir,"notebook.json"))
    private val lock=locks.computeIfAbsent(file.baseFile.absolutePath) {Any()}
    override fun loadBook(): DiaryBook = synchronized(lock) {
        if(file.baseFile.exists() || File(file.baseFile.path+".bak").exists()) return@synchronized file.openRead().use {input->
            require(input.available()<=NotebookJson.MAX_BYTES);NotebookJson.decode(input.readBytes().toString(Charsets.UTF_8)).also(NotebookPhoto::validate).also {runCatching {DiaryHomeCache(appContext).update(it)}}
        }
        val days=prefs.all.filterKeys {it.matches(Regex("\\d{4}-\\d{2}-\\d{2}"))}.mapValues {(_,raw)->NotebookJson.legacy(raw as String)}
        val previous=days[LocalDate.now().toString()]?.character
        val character=when(previous) {"🐱"->"cat";"🐰"->"rabbit";"🐻"->"bear";else->"original"}
        val migrated=DiaryBook(days=days,characterStyle=character)
        if(days.isNotEmpty()) saveBook(migrated)
        migrated
    }
    override fun saveBook(book: DiaryBook): Unit = synchronized(lock) {
        NotebookPhoto.validate(book)
        val encoded=NotebookJson.encode(book).toByteArray(Charsets.UTF_8)
        val output=file.startWrite()
        try {output.write(encoded);file.finishWrite(output)} catch(e: Exception) {file.failWrite(output);throw e}
        runCatching {DiaryHomeCache(appContext).update(book);DiaryWidgetProvider.refreshAll(appContext)}
        Unit
    }
    override fun read(date: String): DiaryEntry {val book=loadBook();return (book.days[date] ?: DiaryEntry()).copy(character=book.characterStyle)}
    override fun write(date: String,entry: DiaryEntry): Boolean = runCatching {val book=loadBook();saveBook(book.copy(days=book.days+(date to entry)));true}.getOrDefault(false)
}
