package com.myday.diary.data

import java.io.File
import java.time.LocalDate
import java.time.YearMonth
import java.time.Instant
import java.util.UUID
import org.json.JSONObject

data class JournalRevision(val file: File,val date: String,val captured: String,val reason: String,val entry: DiaryEntry)
class JournalHistory(root: File) {
    private val directory=File(root,"journal-history")
    fun list(): List<JournalRevision> = directory.listFiles {f->f.extension=="json"}?.sortedByDescending {it.name}?.map(::read) ?: emptyList()
    fun read(file: File): JournalRevision {
        require(file.length()<=NotebookJson.MAX_BYTES)
        val json=JSONObject(file.readText());val date=json.getString("date");LocalDate.parse(date)
        val book=NotebookJson.decode(json.getString("book")).also(NotebookPhoto::validate)
        return JournalRevision(file,date,json.getString("captured"),json.getString("reason"),book.days.getValue(date))
    }
    fun beforeSave(previous: DiaryBook,next: DiaryBook) {
        previous.days.forEach {(date,entry)->val target=next.days[date]
            if(entry!=target) {val destructive=target==null || entry.blocks.any {b->target.blocks.none {it.id==b.id && it.photo==b.photo && !(b.text.isNotBlank() && it.text.isBlank())}}
                capture(date,entry,if(destructive) "삭제·교체 전" else "수정 전",destructive)}
        }
    }
    fun capture(date: String,entry: DiaryEntry,reason: String,force: Boolean) {
        if(!JournalReport.meaningful(entry))return
        val files=directory.listFiles {f->f.extension=="json"}?.toList() ?: emptyList()
        val latest=files.filter {it.name.endsWith("-$date.json")}.maxByOrNull {it.name}
        if(latest!=null) {if(read(latest).entry==entry)return;if(!force && System.currentTimeMillis()-latest.lastModified()<JournalPolicy.historyIntervalSeconds*1000L)return}
        val book=DiaryBook(days=mapOf(date to entry))
        val bytes=JSONObject().put("date",date).put("captured",Instant.now().toString()).put("reason",reason).put("book",NotebookJson.encode(book)).toString().toByteArray()
        require(files.size<JournalPolicy.historyLimitFiles && files.sumOf {it.length()}+bytes.size<=JournalPolicy.historyLimitBytes) {"복구 보관함이 가득 찼어요. 필요 없는 이력을 정리한 뒤 다시 저장해 주세요."}
        check(directory.exists() || directory.mkdirs())
        val path=File(directory,"${System.currentTimeMillis()}-${UUID.randomUUID()}-$date.json");val temp=File(path.path+".tmp")
        try {java.io.FileOutputStream(temp).use {it.write(bytes);it.fd.sync()};check(temp.renameTo(path))}finally {temp.delete()}
    }
    fun remove(item: JournalRevision) {require(item.file.canonicalFile.parentFile==directory.canonicalFile && item.file.extension=="json");check(item.file.delete())}
}
data class JournalReport(val days: Int,val photos: Int,val habits: Int,val completed: Int,val streak: Int,val moods: Map<String,Int>) {
    companion object {
        fun meaningful(e: DiaryEntry)=e.blocks.any {it.text.isNotBlank() || it.photo!=null || it.checked}
        fun forMonth(book: DiaryBook,month: YearMonth): JournalReport {
            var days=0;var photos=0;var habits=0;var completed=0;var streak=0;var longest=0;val moods=linkedMapOf<String,Int>()
            for(i in 1..month.lengthOfMonth()) {val e=book.days[month.atDay(i).toString()]
                if(e==null || !meaningful(e)) {streak=0;continue}
                days++;streak++;longest=maxOf(longest,streak);photos+=e.blocks.count {it.photo!=null}
                val tasks=e.blocks.filter {it.type=="habit" && (it.text.isNotBlank() || it.checked)};habits+=tasks.size;completed+=tasks.count {it.checked};moods[e.mood]=(moods[e.mood] ?: 0)+1
            };return JournalReport(days,photos,habits,completed,longest,moods)
        }
    }
    fun summary()="기록한 날  ${days}일\n가장 길게 이어 쓴 날  ${streak}일\n사진  ${photos}장\n습관 실천  $completed / $habits"+(if(habits==0) "" else " (${100*completed/habits}%)")+"\n\n오늘의 마음\n"+moods.entries.sortedByDescending {it.value}.joinToString("\n") {"${it.key}  ${it.value}일"}+"\n\n빈 페이지는 제외해요. 감정은 직접 고른 값만 집계해요."
}
