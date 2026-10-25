package com.myday.diary.data

import com.myday.diary.diary.NotebookRules
import org.json.JSONArray
import org.json.JSONObject

/** Windows DataContract JSON uses capitalized fields and Key/Value dictionary arrays. */
object NotebookJson {
    const val MAX_BYTES=128*1024*1024
    private fun integer(o: JSONObject,key: String,default: Int=0): Int {
        if(!o.has(key) || o.isNull(key)) return default
        val value=o.get(key);require(value is Number && value.toDouble()==value.toInt().toDouble()) {"백업의 $key 숫자가 올바르지 않아요."};return value.toInt()
    }
    private fun boolean(o: JSONObject,key: String,default: Boolean=false): Boolean {
        if(!o.has(key) || o.isNull(key)) return default
        return o.get(key).also {require(it is Boolean) {"백업의 $key 값이 올바르지 않아요."}} as Boolean
    }
    private fun blocks(array: JSONArray, layout: Boolean=false): List<DiaryBlock> = (0 until array.length()).map {i ->
        val o=array.getJSONObject(i)
        DiaryBlock(id=if(layout) "" else o.getString("Id"),type=o.getString("Kind"),text=o.optString("Text").takeUnless {it=="null"} ?: "",checked=boolean(o,"Checked"),
            title=o.optString("Title").takeUnless {it=="null"} ?: "",prompt=o.optString("Prompt").takeUnless {it=="null"} ?: "",
            photo=if(o.isNull("Photo")) null else o.getString("Photo"),wide=boolean(o,"Wide"),x=integer(o,"X"),y=integer(o,"Y"),width=integer(o,"Width"),height=integer(o,"Height"))
    }
    fun decode(raw: String): DiaryBook {
        require(raw.toByteArray(Charsets.UTF_8).size<=MAX_BYTES) {"백업은 128MB 이하로 선택해 주세요."}
        val o=JSONObject(raw);val version=integer(o,"Version");require(version in 1..9) {"지원하지 않는 백업 버전이에요."}
        val days=linkedMapOf<String,DiaryEntry>()
        fun put(date: String,e: JSONObject) {
            require(!days.containsKey(date)) {"백업에 같은 날짜가 중복돼 있어요."}
            days[date]=DiaryEntry(theme=integer(e,"Theme"),character=e.optString("Character",DEFAULT_CHARACTER),blocks=blocks(e.getJSONArray("Blocks")),
                mood=e.optString("Mood","평온해요").takeUnless {it=="null"} ?: "평온해요",layoutMode=if(e.optString("LayoutMode")=="free") "free" else "cards",pageStyle=e.optString("PageStyle").takeIf {it in setOf("plain","paper","dots")} ?: "plain")
        }
        when(val source=o.get("Days")) {
            is JSONArray -> for(i in 0 until source.length()) {val pair=source.getJSONObject(i);put(pair.getString("Key"),pair.getJSONObject("Value"))}
            is JSONObject -> source.keys().forEach {put(it,source.getJSONObject(it))}
            else -> error("백업 날짜 목록이 올바르지 않아요.")
        }
        val layouts=o.optJSONArray("Layouts") ?: JSONArray()
        val awarded=o.optJSONObject("Progress")?.getJSONArray("AwardedDays") ?: JSONArray()
        val awardedList=(0 until awarded.length()).map {awarded.getString(it)};require(awardedList.distinct().size==awardedList.size)
        val equipment=o.optJSONObject("Equipment")
        val collection=if(o.isNull("Collection"))null else o.getJSONObject("Collection").getJSONArray("Actions").let {a->CollectionBook((0 until a.length()).map {i->val x=a.getJSONObject(i);CollectionAction(x.getString("Id"),x.getString("Kind"),x.getString("ItemId"))})}
        val activities=o.optJSONObject("Progress")?.optJSONArray("Activities") ?: JSONArray()
        val activityMap=linkedMapOf<String,Int>()
        for(i in 0 until activities.length()) {val a=activities.getJSONObject(i);val date=a.getString("Date");require(date !in activityMap);activityMap[date]=integer(a,"Flags")}
        var gear=Equipment()
        EquipmentCatalog.slots.forEach {slot->val key=slot.replaceFirstChar {it.uppercaseChar()}+"Id";gear=gear.equip(slot,if(equipment==null || equipment.isNull(key)) "none" else equipment.getString(key))}
        return DiaryBook(version,days,o.optString("CharacterStyle","original"),
            (0 until layouts.length()).map {i-> val l=layouts.getJSONObject(i);SavedLayout(l.getString("Id"),l.getString("Name"),l.getString("Mode"),l.getString("Style"),integer(l,"Theme"),blocks(l.getJSONArray("Blocks"),true))},
            awardedList.toSet(),gear,
            o.optJSONObject("Android")?.optBoolean("Haptics",true) ?: true,equipment!=null,activityMap,collection,if(o.isNull("Studio"))null else StudioData.decode(o.getJSONObject("Studio"))).let {book->
                val style=mapOf("droplet" to "fin","puddle" to "shell","pill" to "tailed","cube" to "crystal","cloud" to "furry","twin" to "flower")[book.characterStyle] ?: book.characterStyle
                val required=SangmonUnlockXP.values[style]
                book.copy(characterStyle=if(required!=null && NotebookRules.xp(book)>=required) style else "original")
            }.also(NotebookRules::validate)
    }
    private fun block(b: DiaryBlock, layout: Boolean=false): JSONObject = JSONObject().apply {
        if(!layout) {put("Id",b.id);put("Text",b.text);put("Checked",b.checked);b.photo?.let {put("Photo",it)}}
        put("Kind",b.type);put("Title",b.title);put("Prompt",b.prompt);put("Wide",b.wide)
        put("X",b.x);put("Y",b.y);put("Width",b.width);put("Height",b.height)
    }
    fun encode(book: DiaryBook): String {
        NotebookRules.validate(book)
        return JSONObject().apply {
            val expanded=book.activities.isNotEmpty() || EquipmentCatalog.slots.any {val id=book.equipment.get(it);id!="none" && !id.endsWith("-sword") && !id.endsWith("-orb")}
            put("Version",maxOf(if(book.studio!=null)9 else book.version,if(book.collection!=null) {if(book.collection.actions.any {com.myday.diary.diary.CollectionRules.needsV8(it.kind)})8 else if(com.myday.diary.diary.CollectionRules.state(book).closet)7 else 6} else if(expanded)5 else 4));put("CharacterStyle",book.characterStyle)
            put("Days",JSONArray().apply {book.days.toSortedMap().forEach {(date,e)->put(JSONObject().put("Key",date).put("Value",JSONObject()
                .put("Theme",e.theme).put("Mood",e.mood).put("Character",e.character).put("LayoutMode",e.layoutMode).put("PageStyle",e.pageStyle)
                .put("Blocks",JSONArray(e.blocks.map {block(it)}))))}})
            put("Layouts",JSONArray(book.layouts.map {l->JSONObject().put("Id",l.id).put("Name",l.name).put("Mode",l.mode).put("Style",l.style).put("Theme",l.theme).put("Blocks",JSONArray(l.blocks.map {block(it,true)}))}))
            put("Progress",JSONObject().put("AwardedDays",JSONArray(book.awardedDays.sorted())).put("Activities",JSONArray(book.activities.toSortedMap().map {(date,flags)->JSONObject().put("Date",date).put("Flags",flags)})))
            if(book.hasEquipment) put("Equipment",JSONObject().apply {EquipmentCatalog.slots.forEach {slot->put(slot.replaceFirstChar {it.uppercaseChar()}+"Id",book.equipment.get(slot))}})
            book.collection?.let {put("Collection",JSONObject().put("Actions",JSONArray(it.actions.map {a->JSONObject().put("Id",a.id).put("Kind",a.kind).put("ItemId",a.itemId)})))}
            book.studio?.let {put("Studio",it.json())}
            put("Android",JSONObject().put("Haptics",book.haptics))
        }.toString().also {require(it.toByteArray().size<=MAX_BYTES) {"전체 기록이 128MB를 넘었어요. 백업 후 사진을 정리해 주세요."}}
    }
    fun legacy(raw: String): DiaryEntry {
        val o=JSONObject(raw);val a=o.getJSONArray("blocks")
        return DiaryEntry(o.optInt("theme").coerceIn(0,2),o.optString("character",DEFAULT_CHARACTER),
            (0 until a.length()).map {i->val b=a.getJSONObject(i);DiaryBlock(b.getString("id"),b.getString("type"),b.optString("text"),b.optBoolean("checked"))})
    }
}
