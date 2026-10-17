package com.myday.diary.diary

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import androidx.compose.runtime.*
import com.myday.diary.data.DiaryStore
import com.myday.diary.data.DiaryBook
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.cancel
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

/** Retain working edits, including failed saves, across rotation. Never replace a damaged book. */
class NotebookViewModel(application: Application): AndroidViewModel(application) {
    var controller by mutableStateOf<NotebookController?>(null);private set
    var loadError by mutableStateOf<String?>(null);private set
    private data class WriteRequest(val book: DiaryBook,val date: String?,val done: (Result<DiaryBook>)->Unit)
    private val requests=Channel<WriteRequest>(Channel.CONFLATED)
    private val writeScope=CoroutineScope(SupervisorJob()+Dispatchers.Main.immediate)
    private val repository=DiaryStore(application)
    init {
        writeScope.launch {
            for(request in requests) {
                val candidate=controller?.prepareSave(request.book,request.date) ?: request.book
                val result=withContext(Dispatchers.IO) {runCatching {repository.saveBook(candidate);candidate}}
                request.done(result)
            }
            writeScope.cancel()
        }
        reload()
    }
    fun reload() {viewModelScope.launch {loadError=null
        runCatching {withContext(Dispatchers.IO) {NotebookController(repository,persist={book,date,done->
            if(requests.trySend(WriteRequest(book,date,done)).isFailure) done(Result.failure(IllegalStateException("저장 요청을 처리하지 못했어요.")))
        })}}
            .onSuccess {controller=it}.onFailure {loadError=it.message ?: "기록을 읽지 못했어요."}
    }}
    override fun onCleared() {requests.close();super.onCleared()} // Drain the last edit even after the activity closes.
}
