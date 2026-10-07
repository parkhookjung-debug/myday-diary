package com.myday.diary.ui.design

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.res.colorResource
import com.myday.diary.R

@Composable
fun MyDayTheme(content: @Composable () -> Unit) {
    MaterialTheme(
        colorScheme = lightColorScheme(
            primary = colorResource(R.color.myday_primary),
            secondary = colorResource(R.color.myday_secondary)
        ),
        typography = DiaryTypography,
        content = content
    )
}
