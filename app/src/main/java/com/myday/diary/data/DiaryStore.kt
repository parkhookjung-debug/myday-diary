package com.myday.diary.data

import android.content.Context
import com.myday.diary.DiaryWidgetProvider
import org.json.JSONArray
import org.json.JSONObject
import java.time.LocalDate

/** The preference file, date keys and JSON format are unchanged by the UI refactor. */
class DiaryStore(context: Context) : DiaryRepository {
    private val appContext = context.applicationContext
    private val prefs = context.getSharedPreferences("diary", Context.MODE_PRIVATE)

    override fun read(date: String): DiaryEntry {
        val raw = prefs.getString(date, null) ?: return DiaryEntry()
        val json = JSONObject(raw)
        val array = json.getJSONArray("blocks")
        return DiaryEntry(json.optInt("theme").coerceIn(0, 2), json.optString("character", DEFAULT_CHARACTER),
            (0 until array.length()).map {
                val block = array.getJSONObject(it)
                DiaryBlock(block.getString("id"), block.getString("type"), block.optString("text"), block.optBoolean("checked"))
            })
    }

    override fun write(date: String, entry: DiaryEntry): Boolean {
        val previous = read(date)
        val blocks = JSONArray()
        entry.blocks.forEach { block ->
            blocks.put(JSONObject().put("id", block.id).put("type", block.type)
                .put("text", block.text).put("checked", block.checked))
        }
        val saved = prefs.edit().putString(date, JSONObject().put("theme", entry.theme)
            .put("character", entry.character).put("blocks", blocks).toString()).commit()
        fun progress(value: DiaryEntry) = value.blocks.filter { it.type in listOf("todo", "habit") }.map { it.type to it.checked }
        if (saved && date == LocalDate.now().toString() &&
            (previous.theme != entry.theme || previous.character != entry.character || progress(previous) != progress(entry))) {
            DiaryWidgetProvider.refreshAll(appContext)
        }
        return saved
    }
}
