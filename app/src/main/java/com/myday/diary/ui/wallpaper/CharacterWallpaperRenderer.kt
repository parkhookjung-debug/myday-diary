package com.myday.diary.ui.wallpaper

import android.content.Context
import android.graphics.Canvas
import android.graphics.Paint
import com.myday.diary.R
import com.myday.diary.ui.design.DiaryOptions
import com.myday.diary.ui.character.AnimatedCharacterPainter
import com.myday.diary.ui.character.CharacterAnimation
import com.myday.diary.ui.character.CharacterKind

/** Change artwork here without editing motion, touch handling or frame scheduling. */
class CharacterWallpaperRenderer(private val context: Context) {
    private val paint = Paint(Paint.ANTI_ALIAS_FLAG)
    private val characterPainter = AnimatedCharacterPainter(context)
    private val resources = context.resources
    val characterRadius = resources.getDimension(R.dimen.wallpaper_character_radius)
    private val dotInset = resources.getDimension(R.dimen.wallpaper_dot_inset)
    private val dotSpacing = resources.getDimension(R.dimen.wallpaper_dot_spacing).coerceAtLeast(1f)
    private val dotRadius = resources.getDimension(R.dimen.wallpaper_dot_radius)
    private val heartSize = resources.getDimension(R.dimen.wallpaper_heart_size)
    private val heartGap = resources.getDimension(R.dimen.wallpaper_heart_gap)
    private val heartTop = resources.getDimension(R.dimen.wallpaper_heart_top)

    fun draw(canvas: Canvas, theme: Int, character: String, x: Float, y: Float, radius: Float, happy: Boolean,
        timeMillis: Long, moving: Boolean, dragging: Boolean, facingLeft: Boolean) {
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
        val kind = CharacterKind.fromStoredValue(character)
        characterPainter.draw(canvas, kind, x, y, radius * 0.88f,
            CharacterAnimation.pose(timeMillis, moving, dragging, happy), facingLeft)
        if (happy && kind != CharacterKind.MONSTER) {
            paint.textAlign = Paint.Align.CENTER
            paint.textSize = heartSize
            paint.color = context.getColor(R.color.myday_primary)
            canvas.drawText("♡", x, (y - radius - heartGap).coerceAtLeast(heartTop), paint)
        }
    }
}
