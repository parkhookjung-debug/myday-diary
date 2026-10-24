package com.myday.diary.platform

import android.Manifest
import android.app.*
import android.content.*
import android.content.pm.PackageManager
import android.os.Build
import com.myday.diary.MainActivity
import java.time.ZonedDateTime

object JournalReminder {
    fun preferences(context: Context)=context.getSharedPreferences("journal-reminder",Context.MODE_PRIVATE)
    fun allowed(context: Context)=Build.VERSION.SDK_INT<33 || context.checkSelfPermission(Manifest.permission.POST_NOTIFICATIONS)==PackageManager.PERMISSION_GRANTED
    private fun pending(context: Context)=PendingIntent.getBroadcast(context,920,Intent(context,JournalReminderReceiver::class.java).setAction("com.myday.diary.REMIND"),PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE)
    fun configure(context: Context,enabled: Boolean,minute: Int) {
        require(minute in 0..1439)
        require(!enabled || allowed(context)) {"시스템 설정에서 알림을 허용해 주세요."}
        check(preferences(context).edit().putBoolean("enabled",enabled).putInt("minute",minute).commit())
        schedule(context)
    }
    fun nextTime(now: ZonedDateTime,minute: Int): ZonedDateTime {
        require(minute in 0..1439)
        val candidate=now.withHour(minute/60).withMinute(minute%60).withSecond(0).withNano(0)
        return if(candidate.isAfter(now)) candidate else candidate.plusDays(1)
    }
    fun schedule(context: Context) {
        val manager=context.getSystemService(AlarmManager::class.java);manager.cancel(pending(context))
        val p=preferences(context);if(!p.getBoolean("enabled",false) || !allowed(context))return
        manager.setAndAllowWhileIdle(AlarmManager.RTC_WAKEUP,nextTime(ZonedDateTime.now(),p.getInt("minute",1200)).toInstant().toEpochMilli(),pending(context))
    }
}
class JournalReminderReceiver: BroadcastReceiver() {
    override fun onReceive(context: Context,intent: Intent) {
        if(intent.action=="com.myday.diary.REMIND" && JournalReminder.preferences(context).getBoolean("enabled",false) && JournalReminder.allowed(context)) {
            val manager=context.getSystemService(NotificationManager::class.java)
            manager.createNotificationChannel(NotificationChannel("journal-daily","한 줄 일기 알림",NotificationManager.IMPORTANCE_DEFAULT))
            val open=PendingIntent.getActivity(context,921,Intent(context,MainActivity::class.java),PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE)
            val notification=Notification.Builder(context,"journal-daily").setSmallIcon(android.R.drawable.ic_menu_edit).setContentTitle("상몬과 한 줄 일기").setContentText("오늘의 마음을 잠깐 남겨 볼까?").setContentIntent(open).setAutoCancel(true).build()
            try {manager.notify(920,notification)}catch(_: SecurityException) { /* Permission may have changed since scheduling. */ }
        }
        JournalReminder.schedule(context)
    }
}
