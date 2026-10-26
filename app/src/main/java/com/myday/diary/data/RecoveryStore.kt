package com.myday.diary.data

import android.util.AtomicFile
import java.io.File
import java.util.UUID

/** A validated prior generation, independent of AtomicFile's transient .bak file. */
object RecoveryStore {
    private fun decode(bytes: ByteArray)=NotebookJson.decode(bytes.toString(Charsets.UTF_8)).also(NotebookPhoto::validate)
    private fun read(file: File): ByteArray {require(file.length()<=NotebookJson.MAX_BYTES);return file.readBytes()}
    private fun snapshot(root: File)=AtomicFile(File(root,"notebook.previous.json"))
    fun available(root: File)=runCatching {snapshot(root).openRead().use {require(it.available()<=NotebookJson.MAX_BYTES);decode(it.readBytes())};true}.getOrDefault(false)
    private fun atomic(file: AtomicFile,bytes: ByteArray) {val stream=file.startWrite();try {stream.write(bytes);file.finishWrite(stream)}catch(e:Exception){file.failWrite(stream);throw e}}
    fun checkpoint(root: File,bytes: ByteArray) {decode(bytes);atomic(snapshot(root),bytes)}
    fun restore(root: File): File {
        val bytes=snapshot(root).openRead().use {require(it.available()<=NotebookJson.MAX_BYTES);it.readBytes()};decode(bytes)
        val current=File(root,"notebook.json");val archive=File(root,"notebook.damaged-${UUID.randomUUID()}")
        if(current.exists())current.copyTo(archive,overwrite=false)
        atomic(AtomicFile(current),bytes);return archive
    }
    fun current(root: File)=read(File(root,"notebook.json"))
}
