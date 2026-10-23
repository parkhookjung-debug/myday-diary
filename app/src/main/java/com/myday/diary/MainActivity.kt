package com.myday.diary

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import com.myday.diary.ui.diary.PhoneNotebookRoute

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        com.myday.diary.platform.JournalReminder.schedule(this)
        setContent { PhoneNotebookRoute() }
    }
}
