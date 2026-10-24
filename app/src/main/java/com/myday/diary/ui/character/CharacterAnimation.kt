package com.myday.diary.ui.character

import kotlin.math.PI
import kotlin.math.abs
import kotlin.math.sin

enum class CharacterKind(val storedValue: String, val displayName: String) {
    MONSTER("sketch-monster", "불꽃 몬스터"), RABBIT("🐰", "토끼"), CAT("🐱", "고양이"), BEAR("🐻", "곰");
    companion object {
        fun fromStoredValue(value: String) = entries.firstOrNull { it.storedValue == value } ?: MONSTER
    }
}

data class CharacterPose(
    val bob: Float,
    val bodyScale: Float,
    val earAngle: Float,
    val leftFoot: Float,
    val rightFoot: Float,
    val leftArm: Float,
    val rightArm: Float,
    val eyesClosed: Boolean,
    val happy: Boolean,
    val held: Boolean,
    val flamePulse: Float = 0f
)

/** Pure pose calculation shared by the wallpaper, app avatar and widget still image. */
object CharacterAnimation {
    // A common period for walking (0.8s), breathing (2.4s), ears and blinking (4.2s).
    const val LOOP_MILLIS = 16_800

    fun pose(timeMillis: Long, moving: Boolean = false, dragging: Boolean = false, happy: Boolean = false): CharacterPose {
        val seconds = timeMillis / 1000.0
        val stride = sin(seconds * 2 * PI / 0.8).toFloat()
        val breath = sin(seconds * 2 * PI / 2.4).toFloat()
        val ears = sin(seconds * 2 * PI / 4.2).toFloat()
        val blink = Math.floorMod(timeMillis, 4200L) in 4000L until 4120L
        return CharacterPose(
            bob = when { dragging -> 0f; happy -> -abs(stride) * 7f; moving -> -abs(stride) * 3f; else -> breath },
            bodyScale = if (dragging) 1f else 1f + breath * 0.025f,
            earAngle = if (happy) 14f else ears * 5f,
            leftFoot = if (moving && !dragging && !happy) stride * 4f else 0f,
            rightFoot = if (moving && !dragging && !happy) -stride * 4f else 0f,
            leftArm = when { dragging -> 65f; happy -> 95f + stride * 12f; moving -> stride * 22f; else -> 0f },
            rightArm = when { dragging -> -65f; happy -> -95f - stride * 12f; moving -> -stride * 22f; else -> 0f },
            eyesClosed = !dragging && (happy || blink),
            happy = happy && !dragging,
            held = dragging,
            flamePulse = ((sin(seconds * 2 * PI / 0.6) + 1.0) / 2.0).toFloat()
        )
    }
}
