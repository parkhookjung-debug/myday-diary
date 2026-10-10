package com.myday.diary.ui.character

import android.content.Context
import android.graphics.Canvas
import android.graphics.Paint
import android.graphics.Path
import com.myday.diary.R

/** Original vector artwork with independently animated ears, limbs, body and eyes. */
class AnimatedCharacterPainter(context: Context) {
    private val paint = Paint(Paint.ANTI_ALIAS_FLAG).apply { strokeCap = Paint.Cap.ROUND; strokeJoin = Paint.Join.ROUND }
    private val outline = context.getColor(R.color.character_outline)
    private val inner = context.getColor(R.color.character_inner)
    private val belly = context.getColor(R.color.character_belly)
    private val shadow = context.getColor(R.color.character_shadow)
    private val bunny = context.getColor(R.color.character_rabbit)
    private val cat = context.getColor(R.color.character_cat)
    private val bear = context.getColor(R.color.character_bear)
    private val monster = SketchMonsterPainter(context)

    private fun fill(color: Int) { paint.color = color; paint.style = Paint.Style.FILL }
    private fun stroke(width: Float = 1.8f) { paint.color = outline; paint.style = Paint.Style.STROKE; paint.strokeWidth = width }
    private fun oval(canvas: Canvas, left: Float, top: Float, right: Float, bottom: Float, color: Int, bordered: Boolean = true) {
        fill(color); canvas.drawOval(left, top, right, bottom, paint)
        if (bordered) { stroke(); canvas.drawOval(left, top, right, bottom, paint) }
    }

    fun draw(canvas: Canvas, kind: CharacterKind, x: Float, y: Float, radius: Float, pose: CharacterPose, facingLeft: Boolean = kind == CharacterKind.MONSTER) {
        val checkpoint = canvas.save()
        try {
            canvas.translate(x, y)
            canvas.scale(radius / 50f, radius / 50f)
            if (kind == CharacterKind.MONSTER) {
                monster.draw(canvas, pose, facingLeft)
                return
            }
            oval(canvas, -19f, 39f, 19f, 44f, shadow, bordered = false)
            canvas.translate(0f, pose.bob)
            if (facingLeft) canvas.scale(-1f, 1f)
            val fur = when (kind) { CharacterKind.RABBIT -> bunny; CharacterKind.CAT -> cat; CharacterKind.BEAR -> bear; CharacterKind.MONSTER -> bunny }
            if (kind == CharacterKind.CAT) {
                val tail = Path().apply { moveTo(12f, 23f); cubicTo(32f, 26f, 35f, 9f + pose.earAngle, 27f, 9f + pose.earAngle) }
                stroke(7f); canvas.drawPath(tail, paint)
                paint.color = fur; paint.strokeWidth = 4f; canvas.drawPath(tail, paint)
            }
            // Limbs are rendered before the body so the attachment points remain hidden.
            for (side in listOf(-1f, 1f)) {
                val foot = if (side < 0) pose.leftFoot else pose.rightFoot
                oval(canvas, side * 10f - 7f, 27f + foot, side * 10f + 7f, 38f + foot, fur)
                val armSave = canvas.save()
                canvas.rotate(if (side < 0) pose.leftArm else pose.rightArm, side * 16f, 9f)
                oval(canvas, side * 20f - 5f, 7f, side * 20f + 5f, 24f, fur)
                canvas.restoreToCount(armSave)
            }
            val bodySave = canvas.save()
            canvas.scale(1f, pose.bodyScale, 0f, 28f)
            oval(canvas, -17f, 0f, 17f, 33f, fur)
            oval(canvas, -10f, 11f, 10f, 29f, belly, bordered = false)
            canvas.restoreToCount(bodySave)
            for (side in listOf(-1f, 1f)) {
                val earSave = canvas.save()
                canvas.rotate(side * pose.earAngle, side * 13f, -19f)
                when (kind) {
                    CharacterKind.RABBIT -> {
                        oval(canvas, side * 12f - 5f, -47f, side * 12f + 5f, -15f, fur)
                        oval(canvas, side * 12f - 2.2f, -41f, side * 12f + 2.2f, -20f, inner, bordered = false)
                    }
                    CharacterKind.CAT -> {
                        val ear = Path().apply { moveTo(side * 22f, -13f); lineTo(side * 20f, -35f); lineTo(side * 7f, -20f); close() }
                        fill(fur); canvas.drawPath(ear, paint); stroke(); canvas.drawPath(ear, paint)
                        val center = Path().apply { moveTo(side * 18f, -19f); lineTo(side * 17f, -28f); lineTo(side * 11f, -21f); close() }
                        fill(inner); canvas.drawPath(center, paint)
                    }
                    CharacterKind.BEAR -> {
                        oval(canvas, side * 17f - 8f, -33f, side * 17f + 8f, -17f, fur)
                        oval(canvas, side * 17f - 4f, -29f, side * 17f + 4f, -21f, inner, bordered = false)
                    }
                    CharacterKind.MONSTER -> Unit
                }
                canvas.restoreToCount(earSave)
            }
            oval(canvas, -23f, -24f, 23f, 15f, fur)
            oval(canvas, -17f, -2f, -9f, 3f, inner, bordered = false)
            oval(canvas, 9f, -2f, 17f, 3f, inner, bordered = false)
            for (eyeX in listOf(-8f, 8f)) {
                if (pose.eyesClosed) { stroke(2f); canvas.drawArc(eyeX - 3.5f, -8f, eyeX + 3.5f, -2f, 180f, 180f, false, paint) }
                else { fill(outline); canvas.drawOval(eyeX - 2f, -8f, eyeX + 2f, -3f, paint) }
            }
            fill(outline)
            val nose = Path().apply { moveTo(-2.5f, 0f); lineTo(2.5f, 0f); lineTo(0f, 3f); close() }
            canvas.drawPath(nose, paint)
            stroke(1.5f)
            if (pose.held) canvas.drawOval(-2.5f, 5f, 2.5f, 10f, paint)
            else {
                val smile = Path().apply { moveTo(-6f, 5f); quadTo(-3f, 10f, 0f, 5f); quadTo(3f, 10f, 6f, 5f) }
                canvas.drawPath(smile, paint)
            }
            if (kind == CharacterKind.CAT) {
                for (side in listOf(-1f, 1f)) {
                    canvas.drawLine(side * 14f, 3f, side * 25f, 1f, paint)
                    canvas.drawLine(side * 14f, 6f, side * 25f, 8f, paint)
                }
            }
        } finally { canvas.restoreToCount(checkpoint) }
    }
}
