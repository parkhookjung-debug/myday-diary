package com.myday.diary.ui.design

import com.myday.diary.R

data class DiaryThemeOption(val id: Int, val name: String, val colorResource: Int)

object DiaryOptions {
    // IDs are stored in existing diary entries. Recolor or rename, but do not reorder IDs.
    val themes = listOf(
        DiaryThemeOption(0, "크림", R.color.myday_cream),
        DiaryThemeOption(1, "숲", R.color.myday_forest),
        DiaryThemeOption(2, "라벤더", R.color.myday_lavender)
    )
    val characters = listOf("🐰", "🐱", "🐻")
    fun backgroundResource(theme: Int) = themes.firstOrNull { it.id == theme }?.colorResource ?: R.color.myday_cream
}
