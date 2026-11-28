package com.myday.diary.diary

import com.myday.diary.data.Anniversary
import java.time.LocalDate

object AnniversaryRules {
    fun matches(item: Anniversary,day: LocalDate): Boolean {
        val saved=LocalDate.parse(item.date)
        return if(item.repeatYearly) saved.month==day.month && saved.dayOfMonth==day.dayOfMonth else saved==day
    }
}
