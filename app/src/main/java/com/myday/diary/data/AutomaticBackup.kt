package com.myday.diary.data

import java.io.File
import java.time.LocalDate
import java.time.LocalDateTime
import java.nio.file.Files
import java.nio.file.StandardCopyOption
import java.util.UUID
import org.json.JSONObject

data class AutomaticBackupState(val enabled: Boolean=true,val lastSuccess: String="",val error: String="")
class AutomaticBackup(private val root: File) {
    val directory=File(root,"automatic-backups")
    private val settings=File(root,"automatic-backup-state.json")
    companion object {private val failures=java.util.concurrent.ConcurrentHashMap<String,String>()}
    fun state(): AutomaticBackupState = runCatching {
        val json=if(settings.exists()) JSONObject(settings.readText()) else JSONObject()
        AutomaticBackupState(json.optBoolean("enabled",true),json.optString("lastSuccess"),failures[root.path] ?: json.optString("error"))
    }.getOrElse {AutomaticBackupState(enabled=false,error=it.message ?: "백업 설정을 읽지 못했어요.")}
    fun files()=directory.listFiles {f->f.name.matches(Regex("daily-\\d{4}-\\d{2}-\\d{2}\\.json"))}?.sortedByDescending {it.name} ?: emptyList()
    private fun saveState(state: AutomaticBackupState)=atomic(settings,JSONObject().put("enabled",state.enabled).put("lastSuccess",state.lastSuccess).put("error",state.error).toString().toByteArray())
    fun setEnabled(enabled: Boolean)=saveState(state().copy(enabled=enabled))
    fun capture(committed: ByteArray,force: Boolean=false,day: LocalDate=LocalDate.now()): Boolean {
        return try {
            val before=state();if(!force && !before.enabled)return false
            val file=File(directory,"daily-$day.json");if(!force && file.exists())return true
            require(committed.size<=NotebookJson.MAX_BYTES);NotebookJson.decode(String(committed,Charsets.UTF_8)).also(NotebookPhoto::validate)
            atomic(file,committed)
            val files=files();var bytes=files.sumOf {it.length()};var count=files.size
            for(old in files.asReversed()) {if(count<=JournalPolicy.automaticBackupCount && bytes<=JournalPolicy.automaticBackupLimitBytes)break;if(old==file)continue;val size=old.length();check(old.delete());bytes-=size;count--}
            saveState(before.copy(lastSuccess=LocalDateTime.now().toString(),error=""));failures.remove(root.path);true
        }catch(e: Exception) {val message=e.message ?: "자동 백업 실패";failures[root.path]=message;runCatching {saveState(state().copy(error=message))};false}
    }
    private fun atomic(file: File,bytes: ByteArray) {
        check(file.parentFile!!.isDirectory || file.parentFile!!.mkdirs());val pending=File(file.path+".${UUID.randomUUID()}.tmp")
        try {java.io.FileOutputStream(pending).use {it.write(bytes);it.fd.sync()};try {Files.move(pending.toPath(),file.toPath(),StandardCopyOption.ATOMIC_MOVE,StandardCopyOption.REPLACE_EXISTING)}catch(_:java.nio.file.AtomicMoveNotSupportedException){Files.move(pending.toPath(),file.toPath(),StandardCopyOption.REPLACE_EXISTING)}}finally {pending.delete()}
    }
}
