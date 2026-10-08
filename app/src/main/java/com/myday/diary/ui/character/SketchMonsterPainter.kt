package com.myday.diary.ui.character

import android.content.Context
import android.graphics.Canvas
import android.graphics.Paint
import android.graphics.Path
import com.myday.diary.R

/** Traced from the supplied sketch: raised eyes, arched body, side mouth and fire puff. */
class SketchMonsterPainter(context: Context) {
    private val paint = Paint(Paint.ANTI_ALIAS_FLAG).apply { strokeCap = Paint.Cap.ROUND; strokeJoin = Paint.Join.ROUND }
    private val ink = context.getColor(R.color.character_monster_outline)
    private val bodyColor = context.getColor(R.color.character_monster_body)
    private val mouthColor = context.getColor(R.color.character_monster_mouth)
    private val flameColor = context.getColor(R.color.character_monster_flame)
    private val coreColor = context.getColor(R.color.character_monster_flame_core)

    private fun fill(color: Int) { paint.color = color; paint.style = Paint.Style.FILL }
    private fun stroke(width: Float = 5.5f) { paint.color = ink; paint.style = Paint.Style.STROKE; paint.strokeWidth = width }
    private fun shape(canvas: Canvas, path: Path, color: Int) {
        fill(color); canvas.drawPath(path, paint); stroke(); canvas.drawPath(path, paint)
    }

    fun draw(canvas: Canvas, pose: CharacterPose, facingLeft: Boolean) {
        val save = canvas.save()
        try {
            // Photo coordinates are retained to make the original outline easy to edit.
            // The sketch faces left. Mirror it only when travelling to the right.
            if (!facingLeft) canvas.scale(-1f, 1f)
            canvas.translate(0f, pose.bob)
            canvas.scale(1f / 5.8f, 1f / 5.8f)
            canvas.translate(-275f, -615f)
            val bodySave = canvas.save()
            canvas.scale(1f, pose.bodyScale, 275f, 740f)
            val leftToe = pose.leftFoot * 1.5f
            val rightToe = pose.rightFoot * 1.5f
            val body = Path().apply {
                moveTo(215f, 513f)
                cubicTo(219f, 482f, 231f, 451f, 248f, 439f)
                cubicTo(261f, 433f, 268f, 455f, 275f, 491f)
                cubicTo(281f, 466f, 288f, 430f, 307f, 433f)
                cubicTo(326f, 435f, 337f, 469f, 342f, 498f)
                cubicTo(383f, 500f, 405f, 516f, 425f, 535f)
                cubicTo(474f, 576f, 511f, 641f, 518f, 697f)
                cubicTo(523f, 721f, 519f, 736f + rightToe, 514f, 744f + rightToe)
                quadTo(505f, 746f + rightToe, 490f, 731f + rightToe)
                quadTo(494f, 745f + rightToe, 483f, 752f + rightToe)
                quadTo(473f, 758f + rightToe, 461f, 747f + rightToe)
                cubicTo(438f, 718f, 409f, 679f, 381f, 668f)
                cubicTo(357f, 654f, 334f, 672f, 318f, 684f)
                cubicTo(287f, 708f, 260f, 742f, 238f, 765f + leftToe)
                cubicTo(225f, 783f + leftToe, 213f, 778f + leftToe, 214f, 755f + leftToe)
                cubicTo(196f, 767f + leftToe, 177f, 761f + leftToe, 170f, 749f + leftToe)
                cubicTo(154f, 732f, 169f, 702f, 180f, 679f)
                lineTo(205f, 632f)
                cubicTo(181f, 637f, 150f, 627f, 133f, 612f)
                cubicTo(112f, 598f, 113f, 582f, 120f, 568f)
                cubicTo(137f, 542f, 165f, 519f, 190f, 515f)
                quadTo(207f, 511f, 215f, 513f)
                close()
            }
            shape(canvas, body, bodyColor)

            val mouth = Path().apply {
                moveTo(120f, 586f)
                quadTo(154f, 567f, 185f, 561f)
                cubicTo(205f, 557f, 222f, 565f, 227f, 579f)
                cubicTo(232f, 593f, 220f, 614f, 209f, 625f)
                quadTo(168f, 643f, 133f, 611f)
                quadTo(123f, 601f, 120f, 586f)
                close()
            }
            fill(mouthColor); canvas.drawPath(mouth, paint)
            val lip = Path().apply {
                moveTo(120f, 586f); quadTo(154f, 567f, 185f, 561f)
                cubicTo(205f, 557f, 222f, 565f, 227f, 579f)
                cubicTo(232f, 593f, 220f, 614f, 209f, 625f)
                moveTo(185f, 629f); quadTo(148f, 630f, 124f, 602f)
            }
            stroke(); canvas.drawPath(lip, paint)

            if (pose.eyesClosed) {
                val eyes = Path().apply {
                    moveTo(235f, 480f); quadTo(245f, 489f, 256f, 478f)
                    moveTo(295f, 478f); quadTo(306f, 488f, 316f, 475f)
                }
                stroke(6.5f); canvas.drawPath(eyes, paint)
            } else {
                for (pupilX in listOf(246f, 305f)) {
                    val eyeSave = canvas.save()
                    canvas.rotate(18f, pupilX, 476f)
                    fill(ink); canvas.drawOval(pupilX - 9f, 459f, pupilX + 9f, 491f, paint)
                    canvas.restoreToCount(eyeSave)
                }
            }
            canvas.restoreToCount(bodySave)

            if (!pose.held) {
                val fireSave = canvas.save()
                val size = if (pose.happy) 0.96f + pose.flamePulse * 0.13f else 0.75f + pose.flamePulse * 0.025f
                canvas.scale(size, size, 136f, 636f)
                canvas.rotate((pose.flamePulse - 0.5f) * 7f, 136f, 636f)
                if (pose.happy) {
                    val stream = Path().apply { moveTo(121f, 607f); quadTo(103f, 611f, 106f, 624f); lineTo(133f, 639f); quadTo(129f, 620f, 121f, 607f); close() }
                    fill(flameColor); canvas.drawPath(stream, paint)
                }
                val fire = Path().apply {
                    moveTo(130f, 630f)
                    cubicTo(115f, 613f, 95f, 609f, 81f, 617f)
                    cubicTo(63f, 610f, 42f, 625f, 32f, 638f)
                    quadTo(40f, 646f, 56f, 644f)
                    cubicTo(37f, 658f, 22f, 677f, 30f, 695f)
                    cubicTo(33f, 708f, 43f, 708f, 57f, 696f)
                    cubicTo(51f, 711f, 52f, 724f, 63f, 725f)
                    cubicTo(73f, 726f, 82f, 706f, 83f, 701f)
                    cubicTo(79f, 718f, 82f, 732f, 91f, 729f)
                    cubicTo(102f, 725f, 108f, 708f, 110f, 704f)
                    cubicTo(106f, 720f, 111f, 730f, 120f, 721f)
                    cubicTo(135f, 705f, 150f, 672f, 139f, 650f)
                    quadTo(136f, 638f, 130f, 630f)
                    close()
                }
                shape(canvas, fire, if (pose.happy) flameColor else bodyColor)
                val core = Path().apply {
                    moveTo(83f, 620f); quadTo(68f, 632f, 77f, 640f)
                    quadTo(84f, 635f, 88f, 635f); quadTo(79f, 650f, 91f, 657f)
                    quadTo(98f, 650f, 100f, 642f); quadTo(95f, 664f, 106f, 665f)
                    quadTo(116f, 653f, 116f, 646f); quadTo(115f, 669f, 125f, 661f)
                    quadTo(139f, 640f, 119f, 625f); quadTo(100f, 616f, 83f, 620f); close()
                }
                shape(canvas, core, if (pose.happy) coreColor else mouthColor)
                canvas.restoreToCount(fireSave)
            }
        } finally { canvas.restoreToCount(save) }
    }
}
