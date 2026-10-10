package com.myday.diary.ui.character

import org.junit.Assert.*
import org.junit.Test
import com.myday.diary.data.DEFAULT_CHARACTER
import com.myday.diary.data.DiaryEntry

class CharacterAnimationTest {
    @Test
    fun referenceMonsterKeepsStableIdentityAndLoopingFirePulse() {
        assertEquals(CharacterKind.MONSTER, CharacterKind.fromStoredValue(DEFAULT_CHARACTER))
        assertEquals("sketch-monster", DiaryEntry().character)
        assertNotEquals(CharacterAnimation.pose(0).flamePulse, CharacterAnimation.pose(150).flamePulse)
        for (time in 0L..16800L step 120L) {
            assertTrue(CharacterAnimation.pose(time, happy = true).flamePulse in 0f..1f)
        }
        assertEquals(CharacterAnimation.pose(0).flamePulse,
            CharacterAnimation.pose(CharacterAnimation.LOOP_MILLIS.toLong()).flamePulse, 0.001f)
    }

    @Test
    fun blinkingIsBriefAndRepeats() {
        assertFalse(CharacterAnimation.pose(3999).eyesClosed)
        assertTrue(CharacterAnimation.pose(4000).eyesClosed)
        assertTrue(CharacterAnimation.pose(4119).eyesClosed)
        assertFalse(CharacterAnimation.pose(4120).eyesClosed)
        assertTrue(CharacterAnimation.pose(8200).eyesClosed)
    }

    @Test
    fun walkAlternatesLegsWhileIdleKeepsThemStill() {
        val forward = CharacterAnimation.pose(200, moving = true)
        val backward = CharacterAnimation.pose(600, moving = true)
        assertTrue(forward.leftFoot > 0f)
        assertTrue(backward.leftFoot < 0f)
        assertEquals(-forward.leftFoot, forward.rightFoot, 0.001f)
        assertEquals(0f, CharacterAnimation.pose(200).leftFoot, 0f)
        assertTrue(CharacterAnimation.pose(200).bodyScale != CharacterAnimation.pose(800).bodyScale)
    }

    @Test
    fun dragOverridesWalkingAndGreeting() {
        val held = CharacterAnimation.pose(4000, moving = true, dragging = true, happy = true)
        assertTrue(held.held)
        assertFalse(held.happy)
        assertFalse(held.eyesClosed)
        assertEquals(0f, held.bob, 0f)
        assertEquals(0f, held.leftFoot, 0f)
        assertEquals(0f, held.rightFoot, 0f)
    }

    @Test
    fun greetingMovesArmsAndPreviewLoopIsContinuous() {
        assertTrue(CharacterAnimation.pose(200, happy = true).happy)
        assertNotEquals(CharacterAnimation.pose(200, happy = true).leftArm, CharacterAnimation.pose(600, happy = true).leftArm)
        val start = CharacterAnimation.pose(0, moving = true)
        val end = CharacterAnimation.pose(CharacterAnimation.LOOP_MILLIS.toLong(), moving = true)
        assertEquals(start.bob, end.bob, 0.001f)
        assertEquals(start.earAngle, end.earAngle, 0.001f)
        assertEquals(start.bodyScale, end.bodyScale, 0.001f)
        assertEquals(start.leftFoot, end.leftFoot, 0.001f)
        assertEquals(CharacterKind.RABBIT, CharacterKind.fromStoredValue("🐰"))
        assertEquals(CharacterKind.CAT, CharacterKind.fromStoredValue("🐱"))
        assertEquals(CharacterKind.BEAR, CharacterKind.fromStoredValue("🐻"))
    }
}
