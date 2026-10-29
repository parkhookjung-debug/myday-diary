package com.myday.diary.data

import android.content.Context
import org.json.JSONObject

data class SangmonSkin(val id: String,val name: String,val level: Int,val xp: Int)
data class SangmonItem(val id: String,val name: String,val slot: String,val description: String,val element: String)
data class DiaryTemplate(val id: String,val name: String,val description: String,val category: String,val style: String,val layout: String,val minutes: Int,val blocks: List<DiaryBlock>) {
    val categoryName get()=mapOf("daily" to "일상","reflection" to "회고","gratitude" to "감사","emotions" to "감정","learning" to "학습","planning" to "계획","wellbeing" to "자기 돌봄","relationships" to "관계","creativity" to "창작","memories" to "추억")[category] ?: category
    val layoutName get()=mapOf("page" to "긴 글","cards" to "질문 카드","split" to "본문 + 메모","timeline" to "타임라인","letter" to "편지","dashboard" to "플래너","cornell" to "코넬 노트","compare" to "나란히 비교")[layout] ?: "긴 글"
}
class NotebookCatalog(context: Context) {
    private val json=context.assets.open("catalog.json").bufferedReader().use {JSONObject(it.readText())}
    val featuredTemplateIds=json.optJSONArray("featuredTemplateIds")?.let {a->(0 until a.length()).map{a.getString(it)}.toSet()} ?: emptySet()
    val characters=json.getJSONArray("characters").let {a->(0 until a.length()).map {val o=a.getJSONObject(it);SangmonSkin(o.getString("id"),o.getString("name"),o.getInt("level"),o.getInt("xp"))}}
    val items=json.getJSONArray("items").let {a->(0 until a.length()).map {val o=a.getJSONObject(it);SangmonItem(o.getString("id"),o.getString("name"),o.getString("slot"),o.getString("description"),o.getString("element"))}}
    val templates=json.getJSONArray("templates").let {a->(0 until a.length()).map {val o=a.getJSONObject(it);val s=o.getJSONArray("sections")
        DiaryTemplate(o.getString("id"),o.getString("name"),o.getString("description"),o.getString("category"),o.getString("style"),o.getString("layout"),o.getInt("minutes"),
            (0 until s.length()).map {val b=s.getJSONObject(it);DiaryBlock(type=b.getString("kind"),title=b.getString("title"),prompt=b.getString("prompt"),x=b.getInt("x"),y=b.getInt("y"),width=b.getInt("width"),height=b.getInt("height"))})
    }}
}
