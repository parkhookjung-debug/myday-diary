package com.myday.diary.ui.components

import android.os.SystemClock
import androidx.compose.animation.core.*
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.size
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.nativeCanvas
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.semantics.*
import androidx.compose.ui.unit.dp
import com.myday.diary.data.Equipment
import com.myday.diary.platform.*
import com.myday.diary.ui.character.SangmonSprites

@Composable fun SangmonAvatar(id: String,equipment: Equipment=Equipment(),modifier: Modifier=Modifier,haptics: DiaryHaptics?=null) {
    val context=LocalContext.current;val sprites=remember(context) {SangmonSprites(context)}
    var firedAt by remember {mutableLongStateOf(-1L)}
    val clock by rememberInfiniteTransition(label="sangmon").animateFloat(0f,42000f,infiniteRepeatable(tween(42000,easing=LinearEasing)),label="sangmon-clock")
    Canvas(modifier.size(148.dp).semantics {contentDescription="상몬, 누르면 잠깐 불을 뿜어요"}.clickable {firedAt=SystemClock.uptimeMillis();haptics?.perform(TactileEvent.SELECT)}) {
        val age=if(firedAt<0) -1 else SystemClock.uptimeMillis()-firedAt
        sprites.draw(drawContext.canvas.nativeCanvas,id,equipment,size.width/2,size.height/2,size.minDimension*.45f,clock.toLong(),age)
    }
}
