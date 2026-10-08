package com.myday.diary.ui.components

import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.size
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.drawscope.drawIntoCanvas
import androidx.compose.ui.graphics.nativeCanvas
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.unit.dp
import com.myday.diary.ui.character.AnimatedCharacterPainter
import com.myday.diary.ui.character.CharacterAnimation
import com.myday.diary.ui.character.CharacterKind
import kotlinx.coroutines.delay

@Composable
fun CharacterAvatar(character: String, modifier: Modifier = Modifier, animate: Boolean = true, moving: Boolean = false, greeting: Boolean = false) {
    val context = LocalContext.current
    val painter = remember(context) { AnimatedCharacterPainter(context) }
    val kind = CharacterKind.fromStoredValue(character)
    var taps by remember { mutableIntStateOf(0) }
    var happy by remember { mutableStateOf(false) }
    LaunchedEffect(taps) {
        if (taps > 0) { happy = true; delay(1800); happy = false }
    }
    val time = if (animate) rememberInfiniteTransition(label = "character").animateFloat(
        initialValue = 0f, targetValue = CharacterAnimation.LOOP_MILLIS.toFloat(),
        animationSpec = infiniteRepeatable(tween(CharacterAnimation.LOOP_MILLIS, easing = LinearEasing)), label = "pose-clock") else null
    val reaction = if (kind == CharacterKind.MONSTER) "누르면 불을 뿜어요" else "누르면 인사해요"
    Canvas(modifier.size(96.dp).semantics { contentDescription = "${kind.displayName} 캐릭터, $reaction" }.clickable { taps++ }) {
        val pose = CharacterAnimation.pose(time?.value?.toLong() ?: 0L, moving = moving, happy = happy || greeting)
        drawIntoCanvas { painter.draw(it.nativeCanvas, kind, size.width / 2f, size.height / 2f, size.minDimension * 0.46f, pose) }
    }
}
