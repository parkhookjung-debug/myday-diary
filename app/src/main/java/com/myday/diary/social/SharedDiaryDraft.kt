package com.myday.diary.social

import com.myday.diary.data.DiaryBlock
import com.myday.diary.data.DiaryEntry
import org.json.JSONArray
import org.json.JSONObject

/** Explicit projection: never serialize a private DiaryEntry or DiaryBook for upload. */
data class SharedDiaryDraft(val title: String, val mood: String, val blocks: List<DiaryBlock>) {
    fun json(): JSONObject = JSONObject().put("title", title).put("mood", mood).put("blocks", JSONArray().apply {
        blocks.forEach { b -> put(JSONObject().put("id", b.id).put("type", b.type)
            .put("title", b.title).put("text", b.text).put("checked", b.checked).apply {
                b.photo?.let { put("photo", it) }
            }) }
    })

    companion object {
        fun hasContent(b: DiaryBlock) = b.text.isNotBlank() || b.photo != null || b.checked
        fun select(entry: DiaryEntry, ids: Set<String>, title: String, includeMood: Boolean = false): SharedDiaryDraft {
            val blocks = entry.blocks.filter { it.id in ids }
            require(blocks.size in 1..30 && blocks.map { it.id }.toSet() == ids) { "공개할 기록을 1~30개 골라 주세요." }
            require(title.length <= 80) { "공개 제목은 80자까지 적을 수 있어요." }
            require(blocks.count { it.photo != null } <= 6) { "한 번에 사진 6장까지 공개할 수 있어요." }
            require(blocks.all { it.type in setOf("text", "todo", "habit", "emotion", "photo") && hasContent(it) }) { "내용이 있는 기록을 선택해 주세요." }
            require(blocks.all { it.text.length <= 10000 }) { "공개 글은 기록 하나에 10,000자까지예요." }
            return SharedDiaryDraft(title.trim(), if (includeMood) entry.mood else "", blocks)
        }
    }
}
