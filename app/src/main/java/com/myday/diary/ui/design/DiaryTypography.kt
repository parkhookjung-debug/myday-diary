package com.myday.diary.ui.design

import androidx.compose.material3.Typography
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.unit.sp

private val baseline = Typography()
private val diaryFont = FontFamily.Default

/** Change the font family here; bundled font files can be placed in res/font. */
val DiaryTypography = baseline.copy(
    headlineSmall = baseline.headlineSmall.copy(fontFamily = diaryFont, fontSize = 24.sp, lineHeight = 32.sp),
    titleLarge = baseline.titleLarge.copy(fontFamily = diaryFont, fontSize = 22.sp, lineHeight = 28.sp),
    titleMedium = baseline.titleMedium.copy(fontFamily = diaryFont, fontSize = 16.sp, lineHeight = 24.sp),
    bodyMedium = baseline.bodyMedium.copy(fontFamily = diaryFont, fontSize = 14.sp, lineHeight = 20.sp),
    bodySmall = baseline.bodySmall.copy(fontFamily = diaryFont, fontSize = 12.sp, lineHeight = 16.sp),
    labelSmall = baseline.labelSmall.copy(fontFamily = diaryFont, fontSize = 11.sp, lineHeight = 16.sp),
    displayLarge = baseline.displayLarge.copy(fontFamily = diaryFont),
    displayMedium = baseline.displayMedium.copy(fontFamily = diaryFont),
    displaySmall = baseline.displaySmall.copy(fontFamily = diaryFont),
    headlineLarge = baseline.headlineLarge.copy(fontFamily = diaryFont),
    headlineMedium = baseline.headlineMedium.copy(fontFamily = diaryFont),
    titleSmall = baseline.titleSmall.copy(fontFamily = diaryFont),
    bodyLarge = baseline.bodyLarge.copy(fontFamily = diaryFont),
    labelLarge = baseline.labelLarge.copy(fontFamily = diaryFont),
    labelMedium = baseline.labelMedium.copy(fontFamily = diaryFont)
)
