package com.myday.diary.platform

import android.os.Build
import android.os.SystemClock
import android.view.HapticFeedbackConstants
import android.view.View
import androidx.compose.runtime.*
import androidx.compose.ui.platform.LocalView

enum class TactileEvent { SELECT, CHECK, GRAB, DROP, EQUIP, REJECT }
class DiaryHaptics(private val view: View, var enabled: Boolean) {
    private var last=0L
    fun perform(event: TactileEvent) {
        val now=SystemClock.uptimeMillis();if(!enabled || now-last<60) return;last=now
        val feedback=when(event) {
            TactileEvent.GRAB->HapticFeedbackConstants.LONG_PRESS
            TactileEvent.CHECK,TactileEvent.EQUIP->if(Build.VERSION.SDK_INT>=30) HapticFeedbackConstants.CONFIRM else HapticFeedbackConstants.VIRTUAL_KEY
            TactileEvent.DROP->HapticFeedbackConstants.CONTEXT_CLICK
            TactileEvent.REJECT->if(Build.VERSION.SDK_INT>=30) HapticFeedbackConstants.REJECT else HapticFeedbackConstants.LONG_PRESS
            TactileEvent.SELECT->HapticFeedbackConstants.CLOCK_TICK
        }
        // No bypass flags: device settings and hardware fallback remain authoritative.
        view.performHapticFeedback(feedback)
    }
}
@Composable fun rememberDiaryHaptics(enabled: Boolean): DiaryHaptics {
    val view=LocalView.current;return remember(view) {DiaryHaptics(view,enabled)}.also {it.enabled=enabled}
}
