package com.myday.diary.platform

import android.app.WallpaperManager
import android.appwidget.AppWidgetManager
import android.content.ActivityNotFoundException
import android.content.ComponentName
import android.content.Context
import android.content.Intent
import android.widget.Toast
import com.myday.diary.CharacterWallpaperService
import com.myday.diary.DiaryWidgetProvider

class HomeScreenActions(private val context: Context) {
    fun pinWidget() {
        val manager = AppWidgetManager.getInstance(context)
        val accepted = manager.isRequestPinAppWidgetSupported && manager.requestPinAppWidget(ComponentName(context, DiaryWidgetProvider::class.java), null, null)
        Toast.makeText(context, if (accepted) "홈 화면의 위젯 추가 창에서 확인해 주세요" else "홈 화면을 길게 눌러 위젯 → 마이데이를 선택해 주세요", Toast.LENGTH_LONG).show()
    }

    fun setWallpaper() {
        try {
            context.startActivity(Intent(WallpaperManager.ACTION_CHANGE_LIVE_WALLPAPER)
                .putExtra(WallpaperManager.EXTRA_LIVE_WALLPAPER_COMPONENT, ComponentName(context, CharacterWallpaperService::class.java)))
        } catch (_: ActivityNotFoundException) {
            Toast.makeText(context, "휴대폰 배경화면 설정에서 마이데이 · 움직이는 친구를 선택해 주세요", Toast.LENGTH_LONG).show()
        }
    }
}
