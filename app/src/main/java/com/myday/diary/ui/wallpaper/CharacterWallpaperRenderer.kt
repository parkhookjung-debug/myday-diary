package com.myday.diary.ui.wallpaper

import android.content.Context
import android.graphics.Canvas
import android.graphics.Paint
import com.myday.diary.R
import com.myday.diary.ui.design.DiaryOptions

/** Change artwork here without editing motion, touch handling or frame scheduling. */
class CharacterWallpaperRenderer(private val context: Context) {
    private val paint = Paint(Paint.ANTI_ALIAS_FLAG)
    private val resources = context.resources
    val characterRadius = resources.getDimension(R.dimen.wallpaper_character_radius)
    private val dotInset = resources.getDimension(R.dimen.wallpaper_dot_inset)
    private val dotSpacing = resources.getDimension(R.dimen.wallpaper_dot_spacing).coerceAtLeast(1f)
    private val dotRadius = resources.getDimension(R.dimen.wallpaper_dot_radius)
    private val heartSize = resources.getDimension(R.dimen.wallpaper_heart_size)
    private val heartGap = resources.getDimension(R.dimen.wallpaper_heart_gap)
    private val heartTop = resources.getDimension(R.dimen.wallpaper_heart_top)

    fun draw(canvas: Canvas, theme: Int, character: String, x: Float, y: Float, radius: Float, happy: Boolean) {
        canvas.drawColor(context.getColor(DiaryOptions.backgroundResource(theme)))
        paint.color = context.getColor(R.color.myday_wallpaper_dots)
        var dotX = dotInset
        while (dotX < canvas.width) {
            var dotY = dotInset
            while (dotY < canvas.height) { canvas.drawCircle(dotX, dotY, dotRadius, paint); dotY += dotSpacing }
            dotX += dotSpacing
        }
        paint.color = context.getColor(R.color.myday_character_circle)
        canvas.drawCircle(x, y, radius, paint)
        paint.color = context.getColor(R.color.myday_text)
        paint.textAlign = Paint.Align.CENTER
        paint.textSize = radius * 1.35f
        val baseline = y - (paint.fontMetrics.ascent + paint.fontMetrics.descent) / 2f
        canvas.drawText(character, x, baseline, paint)
        if (happy) {
            paint.textSize = heartSize
            paint.color = context.getColor(R.color.myday_primary)
            canvas.drawText("♡", x, (y - radius - heartGap).coerceAtLeast(heartTop), paint)
        }
    }
}
