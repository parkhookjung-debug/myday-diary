package com.myday.diary.ui.diary
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.unit.dp
import com.myday.diary.data.*
import com.myday.diary.diary.*
import com.myday.diary.platform.*
import com.myday.diary.ui.components.SangmonAvatar

@Composable fun PhoneCompanion(controller: NotebookController,catalog: NotebookCatalog,haptics: DiaryHaptics,attempt:(TactileEvent?,()->Unit)->Unit){
 val book=controller.book;val level=NotebookRules.level(book);val xp=NotebookRules.xp(book)
 val today=controller.actualDay;val flags=book.activities[today.toString()]?:0;val attended=flags and NotebookRules.ATTENDANCE!=0
 Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(20.dp).testTag("growth-screen"),verticalArrangement=Arrangement.spacedBy(16.dp)){
  SangmonAvatar("original",modifier=Modifier.size(130.dp),haptics=haptics)
  Text("Lv.$level · ${NotebookRules.chapter(level)}",style=MaterialTheme.typography.headlineSmall)
  Text("총 $xp XP · 기록 ${book.awardedDays.size}일")
  LinearProgressIndicator(progress={NotebookRules.intoLevel(book).toFloat()/NotebookRules.neededXp(level)},modifier=Modifier.fillMaxWidth())
  Text(if(level>=NotebookRules.MAX_LEVEL)"만렙 달성! 다음 기록도 함께해요." else "다음 레벨까지 ${NotebookRules.neededXp(level)-NotebookRules.intoLevel(book)} XP")
  Button({attempt(TactileEvent.CHECK){controller.checkIn()}},enabled=!attended&&controller.saved,modifier=Modifier.testTag("growth-checkin")){Text(if(attended)"출석 완료" else "출석 +5 XP")}
  Text("오늘의 성장 미션",style=MaterialTheme.typography.titleLarge)
  listOf(NotebookRules.ATTENDANCE to "출석 +5",0 to "첫 기록 저장 +10",NotebookRules.WRITING to "글 20자 이상 +10",NotebookRules.PHOTO to "사진 첨부 +5",NotebookRules.TODO to "할 일 완료 +5",NotebookRules.HABIT to "습관 완료 +5").forEach{(flag,name)->
   val done=if(flag==0)today.toString() in book.awardedDays else flags and flag!=0
   Text((if(done)"✓ " else "○ ")+name+" XP")
  }
  Text("오늘 ${NotebookRules.todayActivityXp(book,today)}/40 XP · 연속 7일 출석마다 +10 XP\n저장에 성공한 활동만 하루 한 번 지급해요.",style=MaterialTheme.typography.bodySmall)
  Text("만렙 Lv.100 · 누적 3,590 XP\n매일 루틴 약 100일 / 주 5일 약 200일 / 여유롭게 약 300일",style=MaterialTheme.typography.bodySmall)
 }
}
