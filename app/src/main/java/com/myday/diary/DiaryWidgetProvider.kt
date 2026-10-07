package com.myday.diary

import android.app.PendingIntent
import android.appwidget.AppWidgetManager
import android.appwidget.AppWidgetProvider
import android.content.ComponentName
import android.content.Context
import android.content.Intent
import android.widget.RemoteViews
import com.myday.diary.data.DiaryStore
import java.time.LocalDate

/** A lightweight home-screen widget: no service or background animation loop. */
class DiaryWidgetProvider : AppWidgetProvider() {
    override fun onUpdate(context: Context, manager: AppWidgetManager, ids: IntArray) {
        ids.forEach { render(context, manager, it) }
    }

    override fun onReceive(context: Context, intent: Intent) {
        super.onReceive(context, intent)
        if (intent.action == ACTION_CHEER) {
            val manager = AppWidgetManager.getInstance(context)
            val id = intent.getIntExtra(AppWidgetManager.EXTRA_APPWIDGET_ID, AppWidgetManager.INVALID_APPWIDGET_ID)
            // Ignore stale PendingIntents belonging to a deleted widget.
            if (id !in manager.getAppWidgetIds(ComponentName(context, DiaryWidgetProvider::class.java))) return
            val prefs = context.getSharedPreferences("widget", Context.MODE_PRIVATE)
            prefs.edit().putInt("cheer_$id", (prefs.getInt("cheer_$id", 0) + 1) % cheers.size).apply()
            render(context, manager, id)
        }
    }

    override fun onDeleted(context: Context, ids: IntArray) {
        val editor = context.getSharedPreferences("widget", Context.MODE_PRIVATE).edit()
        ids.forEach { editor.remove("cheer_$it") }
        editor.apply()
    }

    companion object {
        private const val ACTION_CHEER = "com.myday.diary.CHEER"
        private val cheers = listOf("작은 기록 하나면 충분해!", "오늘도 네 편이야 ♡", "한 걸음씩 같이 가자!", "잠깐 쉬어도 괜찮아.")

        fun refreshAll(context: Context) {
            val manager = AppWidgetManager.getInstance(context)
            manager.getAppWidgetIds(ComponentName(context, DiaryWidgetProvider::class.java)).forEach { render(context, manager, it) }
        }

        private fun render(context: Context, manager: AppWidgetManager, id: Int) {
            val today = LocalDate.now()
            val entry = DiaryStore(context).read(today.toString())
            val tasks = entry.blocks.filter { it.type == "todo" }
            val habits = entry.blocks.filter { it.type == "habit" }
            val cheer = context.getSharedPreferences("widget", Context.MODE_PRIVATE).getInt("cheer_$id", 0)
            val views = RemoteViews(context.packageName, R.layout.diary_widget)
            views.setTextViewText(R.id.widget_date, "${today.monthValue}월 ${today.dayOfMonth}일 · MY DAY")
            views.setTextViewText(R.id.widget_character, entry.character)
            views.setTextViewText(R.id.widget_message, cheers[cheer % cheers.size])
            views.setTextViewText(R.id.widget_progress, "할 일 ${tasks.count { it.checked }}/${tasks.size}  ·  습관 ${habits.count { it.checked }}/${habits.size}")
            val backgrounds = intArrayOf(R.drawable.widget_cream, R.drawable.widget_forest, R.drawable.widget_lavender)
            views.setInt(R.id.widget_root, "setBackgroundResource", backgrounds[entry.theme])
            val open = PendingIntent.getActivity(context, id,
                Intent(context, MainActivity::class.java).addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP),
                PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE)
            views.setOnClickPendingIntent(R.id.widget_open, open)
            val cheerIntent = Intent(context, DiaryWidgetProvider::class.java).setAction(ACTION_CHEER)
                .putExtra(AppWidgetManager.EXTRA_APPWIDGET_ID, id)
            val cheerClick = PendingIntent.getBroadcast(context, id, cheerIntent,
                PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE)
            views.setOnClickPendingIntent(R.id.widget_character, cheerClick)
            views.setOnClickPendingIntent(R.id.widget_message, cheerClick)
            manager.updateAppWidget(id, views)
        }
    }
}
