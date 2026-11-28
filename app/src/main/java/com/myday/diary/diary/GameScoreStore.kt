package com.myday.diary.diary

import android.util.AtomicFile
import org.json.JSONArray
import org.json.JSONObject
import java.io.File
import java.time.LocalDate

/** Separate from diary/XP: an unfinished round never earns or overwrites a score. */
class GameScoreStore(directory: File) {
    private val file=AtomicFile(File(directory,"games/scores.json"))
    private val rounds=linkedSetOf<String>()
    data class Entry(val id:String,val best:Int,val plays:Int,val last:Int)
    companion object {
        val ids=setOf("snack","sling","fish","jump","bake","blocks","block-puzzle")
        fun validId(id:String):Boolean = id in ids || (id.startsWith("snack-daily-v1:") && runCatching { val key=id.removePrefix("snack-daily-v1:");val day=LocalDate.parse(key);day.year in 1..9999 && day.toString()==key }.getOrDefault(false))
    }
    @Synchronized fun read():List<Entry> {
        if(!file.baseFile.exists() && !File(file.baseFile.path+".bak").exists())return emptyList()
        val bytes=file.openRead().use { input->val output=java.io.ByteArrayOutputStream();val buffer=ByteArray(4096);while(true){val n=input.read(buffer);if(n<0)break;require(output.size()+n<=65536){"게임 기록 파일이 너무 커요."};output.write(buffer,0,n)};output.toByteArray() }
        val root=JSONObject(bytes.toString(Charsets.UTF_8));require(root.getInt("Version")==1)
        val list=root.getJSONArray("Entries");require(list.length()<=40)
        val rows=(0 until list.length()).map { index->val row=list.getJSONObject(index);Entry(row.getString("Id"),row.getInt("Best"),row.getInt("Plays"),row.getInt("Last")) }
        require(rows.map {it.id}.distinct().size==rows.size && rows.all {validId(it.id)&&it.best in 0..1000000&&it.last in 0..it.best&&it.plays in 1..1000000}){"게임 기록 형식을 확인해 주세요."}
        return rows
    }
    @Synchronized fun record(round:String,id:String,score:Int) {
        require(round.length in 1..100 && round.matches(Regex("[a-zA-Z0-9-]+")) && validId(id) && score in 0..1000000)
        if(round in rounds)return
        val rows=read().toMutableList();val old=rows.firstOrNull {it.id==id};rows.removeAll {it.id==id}
        rows.add(Entry(id,maxOf(old?.best?:0,score),minOf(1000000,(old?.plays?:0)+1),score))
        val keep=rows.filter {it.id in ids}+rows.filter {it.id !in ids}.sortedByDescending {it.id}.take(31)
        val root=JSONObject().put("Version",1).put("Entries",JSONArray().apply {keep.forEach {put(JSONObject().put("Id",it.id).put("Best",it.best).put("Plays",it.plays).put("Last",it.last))}})
        file.baseFile.parentFile!!.mkdirs();var output:java.io.FileOutputStream?=null
        try { output=file.startWrite();output.write(root.toString().toByteArray(Charsets.UTF_8));file.finishWrite(output) }
        catch(e:Exception) {file.failWrite(output);throw e}
        rounds.add(round);while(rounds.size>128)rounds.remove(rounds.first())
    }
}
