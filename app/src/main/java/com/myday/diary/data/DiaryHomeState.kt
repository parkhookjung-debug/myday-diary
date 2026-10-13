package com.myday.diary.data

import android.content.Context
import java.time.LocalDate

data class DiaryHomeState(val character: String="original",val equipment: Equipment=Equipment(),val haptics: Boolean=true,
    val theme: Int=0,val tasksDone: Int=0,val tasksTotal: Int=0,val habitsDone: Int=0,val habitsTotal: Int=0)

/** A derived cache with counts only. The launcher never reads diary text or photo data. */
class DiaryHomeCache(context: Context) {
    private val prefs=context.getSharedPreferences("diary-home",Context.MODE_PRIVATE)
    fun read(date: LocalDate=LocalDate.now()): DiaryHomeState {
        val counts=prefs.getString(date.toString(),null)?.split(',')?.mapNotNull {it.toIntOrNull()}
        return DiaryHomeState(prefs.getString("character","original") ?: "original",
            Equipment(prefs.getString("weapon","none") ?: "none",prefs.getString("charm","none") ?: "none"),prefs.getBoolean("haptics",true),
            counts?.getOrNull(0)?.coerceIn(0,2) ?: 0,counts?.getOrNull(1) ?: 0,counts?.getOrNull(2) ?: 0,counts?.getOrNull(3) ?: 0,counts?.getOrNull(4) ?: 0)
    }
    fun update(book: DiaryBook) {
        val edit=prefs.edit().putString("character",book.characterStyle).putString("weapon",book.equipment.weaponId)
            .putString("charm",book.equipment.charmId).putBoolean("haptics",book.haptics)
        book.days.forEach {(date,entry)->
            val tasks=entry.blocks.filter {it.type=="todo"};val habits=entry.blocks.filter {it.type=="habit"}
            val counts=listOf(entry.theme,tasks.count {it.checked},tasks.size,habits.count {it.checked},habits.size).joinToString(",")
            if(prefs.getString(date,null)!=counts) edit.putString(date,counts)
        }
        prefs.all.keys.filter {it.matches(Regex("\\d{4}-\\d{2}-\\d{2}")) && it !in book.days}.forEach(edit::remove)
        edit.apply()
    }
}
