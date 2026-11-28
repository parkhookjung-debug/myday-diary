package com.myday.diary.data
import org.json.JSONArray
import org.json.JSONObject
import java.time.LocalDate
import java.util.Locale
import java.util.UUID
data class RoomItem(val id:String=UUID.randomUUID().toString(),val kind:String="plant",val x:Int=400,val y:Int=500)
data class StudioNote(val date:String,val tags:List<String> = emptyList(),val favourite:Boolean=false)
data class Anniversary(val id:String=UUID.randomUUID().toString(),val name:String,val date:String,val repeatYearly:Boolean=true)
data class StudioData(val roomTheme:Int=0,val furniture:List<RoomItem> = emptyList(),val pages:List<StudioNote> = emptyList(),val anniversaries:List<Anniversary> = emptyList()) {
 fun validate(){validateAnniversaries();require(roomTheme in 0..2&&furniture.size<=24&&pages.size<=50000);require(furniture.map{it.id}.distinct().size==furniture.size);furniture.forEach{require(it.id.isNotBlank()&&it.id.length<=64&&it.kind in StudioCatalog.furnitureKinds&&it.x in 0..900&&it.y in 0..900)};require(pages.map{it.date}.distinct().size==pages.size);pages.forEach{require(LocalDate.parse(it.date).year in 1..9999&&it.tags.size<=10&&it.tags.distinctBy{t->t.lowercase(Locale.ROOT)}.size==it.tags.size);require(it.tags.all{t->t.isNotBlank()&&t.length<=20&&t.none{c->c.isISOControl()}})}}
 private fun validateAnniversaries(){require(anniversaries.size<=StudioCatalog.anniversaryLimit&&anniversaries.map{it.id}.distinct().size==anniversaries.size);anniversaries.forEach{require(it.id.isNotBlank()&&it.id.length<=64&&it.id.none{c->c.isISOControl()}&&it.name.isNotBlank()&&it.name.length<=StudioCatalog.anniversaryNameLength&&it.name.none{c->c.isISOControl()}&&Regex("[0-9]{4}-[0-9]{2}-[0-9]{2}").matches(it.date)&&LocalDate.parse(it.date).year in 1..9999)}}
 fun note(date:String)=pages.find{it.date==date} ?: StudioNote(date)
 fun setNote(note:StudioNote)=copy(pages=pages.filter{it.date!=note.date}+note).also{it.validate()}
 fun json():JSONObject {validate();return JSONObject().put("RoomTheme",roomTheme).put("Furniture",JSONArray(furniture.map{JSONObject().put("Id",it.id).put("Kind",it.kind).put("X",it.x).put("Y",it.y)})).put("Anniversaries",JSONArray(anniversaries.map{JSONObject().put("Id",it.id).put("Name",it.name).put("Date",it.date).put("RepeatYearly",it.repeatYearly)})).put("Pages",JSONArray(pages.map{JSONObject().put("Date",it.date).put("Tags",JSONArray(it.tags)).put("Favourite",it.favourite)}))}
 companion object {
  private fun int(o:JSONObject,k:String):Int {val n=o.get(k);require(n is Number&&n.toDouble()==n.toInt().toDouble());return n.toInt()}
  private fun anniversaries(o:JSONObject):List<Anniversary>{if(!o.has("Anniversaries")||o.isNull("Anniversaries"))return emptyList();val rows=o.getJSONArray("Anniversaries");require(rows.length()<=StudioCatalog.anniversaryLimit);return (0 until rows.length()).map{val a=rows.getJSONObject(it);require(a.get("RepeatYearly") is Boolean);Anniversary(a.getString("Id"),a.getString("Name"),a.getString("Date"),a.getBoolean("RepeatYearly"))}}
  fun decode(o:JSONObject):StudioData {val f=o.getJSONArray("Furniture");val p=o.getJSONArray("Pages");require(f.length()<=24&&p.length()<=50000);return StudioData(int(o,"RoomTheme"),(0 until f.length()).map{val x=f.getJSONObject(it);RoomItem(x.getString("Id"),x.getString("Kind"),int(x,"X"),int(x,"Y"))},(0 until p.length()).map{val n=p.getJSONObject(it);val tags=n.getJSONArray("Tags");require(tags.length()<=10&&n.get("Favourite") is Boolean);StudioNote(n.getString("Date"),(0 until tags.length()).map{tags.getString(it)},n.getBoolean("Favourite"))},anniversaries(o)).also{it.validate()}}
  fun merge(a:StudioData?,b:StudioData?):StudioData? {if(b==null)return a;a?.validate();b.validate();return b.copy(anniversaries=((a?.anniversaries ?: emptyList()).associateBy{it.id}+b.anniversaries.associateBy{it.id}).values.toList(),pages=((a?.pages ?: emptyList()).associateBy{it.date}+b.pages.associateBy{it.date}).values.sortedBy{it.date}).also{it.validate()}}
  fun question(date:LocalDate)=StudioCatalog.questions[Math.floorMod(date.toEpochDay()+719162,StudioCatalog.questions.size.toLong()).toInt()]
 }
}
