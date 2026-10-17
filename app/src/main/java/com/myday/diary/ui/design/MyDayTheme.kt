package com.myday.diary.ui.design

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.res.colorResource
import androidx.compose.ui.graphics.Color
import com.myday.diary.R

@Composable
fun MyDayTheme(content: @Composable () -> Unit) {
    MaterialTheme(
        colorScheme = lightColorScheme(
            primary = colorResource(R.color.myday_primary),
            secondary = colorResource(R.color.myday_secondary),
            primaryContainer=Color(0xfff3e6df),onPrimaryContainer=Color(0xff433d36),
            secondaryContainer=Color(0xffe9ecdf),onSecondaryContainer=Color(0xff60694e),
            surface=Color(0xfffffcf8),onSurface=Color(0xff433d36),onSurfaceVariant=Color(0xff716b60),
            surfaceVariant=Color(0xfff3e6df),outline=Color(0xffb6aa9c)
        ),
        typography = DiaryTypography,
        content = content
    )
}
