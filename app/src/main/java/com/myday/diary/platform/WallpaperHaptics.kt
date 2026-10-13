package com.myday.diary.platform

import android.content.Context
import android.os.*
import android.provider.Settings

/** A WallpaperService has no View; use supported device presets, respecting touch settings. */
object WallpaperHaptics {
    fun tap(context: Context,enabled: Boolean) {
        if(!enabled || Build.VERSION.SDK_INT<29 || Settings.System.getInt(context.contentResolver,Settings.System.HAPTIC_FEEDBACK_ENABLED,1)==0) return
        val vibrator=if(Build.VERSION.SDK_INT>=31) context.getSystemService(VibratorManager::class.java).defaultVibrator else context.getSystemService(Vibrator::class.java)
        if(vibrator?.hasVibrator()==true) vibrator.vibrate(VibrationEffect.createPredefined(VibrationEffect.EFFECT_CLICK))
    }
}
