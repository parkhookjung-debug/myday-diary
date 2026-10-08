package com.myday.diary.ui.diary

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.colorResource
import com.myday.diary.R
import com.myday.diary.data.DiaryEntry
import com.myday.diary.ui.components.DiaryActionButton
import com.myday.diary.ui.components.DiaryCard
import com.myday.diary.ui.components.CharacterAvatar
import com.myday.diary.ui.character.CharacterKind
import com.myday.diary.ui.design.DiaryDimensions
import com.myday.diary.ui.design.DiaryOptions
import java.time.LocalDate

@Composable
fun DiaryHeader() {
    Text("MY DAY", style = MaterialTheme.typography.labelLarge, color = MaterialTheme.colorScheme.primary)
    Text("내 마음대로, 나의 하루", style = MaterialTheme.typography.headlineSmall)
    Text("필요한 블록을 골라 나만의 일기를 만들어 보세요.", style = MaterialTheme.typography.bodyMedium)
}

@Composable
fun DiaryDateBar(date: LocalDate, saved: Boolean, actions: DiaryScreenActions) {
    Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
        TextButton(onClick = actions.onPreviousDay, enabled = saved) { Text("‹ 이전") }
        TextButton(onClick = actions.onChooseDate, enabled = saved) { Text(date.toString()) }
        TextButton(onClick = actions.onNextDay, enabled = saved) { Text("다음 ›") }
    }
    Text(if (saved) "휴대폰에 저장됨 · 자동 저장" else "저장 실패 · 날짜를 바꾸기 전에 다시 저장해 주세요", style = MaterialTheme.typography.labelSmall)
    if (!saved) TextButton(onClick = actions.onRetrySave) { Text("다시 저장") }
}

@Composable
fun DiaryAppearancePicker(entry: DiaryEntry, actions: DiaryScreenActions) {
    DiaryCard {
        Column(Modifier.padding(DiaryDimensions.cardPadding)) {
            Text("오늘의 분위기", style = MaterialTheme.typography.titleMedium)
            CharacterAvatar(entry.character)
            Row(horizontalArrangement = Arrangement.spacedBy(DiaryDimensions.controlGap)) {
                DiaryOptions.themes.forEach { theme ->
                    FilterChip(selected = entry.theme == theme.id, onClick = { actions.onThemeChange(theme.id) }, label = { Text(theme.name) })
                }
            }
            Row(horizontalArrangement = Arrangement.spacedBy(DiaryDimensions.controlGap)) {
                DiaryOptions.characters.forEach { character ->
                    FilterChip(selected = entry.character == character, onClick = { actions.onCharacterChange(character) }, label = { Text(CharacterKind.fromStoredValue(character).displayName) })
                }
            }
        }
    }
}

@Composable
fun HomeScreenControls(actions: DiaryScreenActions) {
    DiaryCard {
        Column(Modifier.padding(DiaryDimensions.cardPadding), verticalArrangement = Arrangement.spacedBy(DiaryDimensions.contentGap)) {
            Text("홈 화면의 작은 친구", style = MaterialTheme.typography.titleMedium)
            Text("위젯으로 오늘의 할 일과 습관을 확인해요.", style = MaterialTheme.typography.bodyMedium)
            DiaryActionButton("홈 화면에 일기 위젯 추가", actions.onPinWidget, outlined = true)
            Text("움직이는 친구를 배경화면으로 설정하면 홈 화면을 돌아다녀요. 캐릭터를 누르거나 끌어 보세요.", style = MaterialTheme.typography.bodyMedium)
            DiaryActionButton("움직이는 캐릭터 배경화면 설정", actions.onSetWallpaper)
            Text("오늘 날짜에서 고른 캐릭터와 배경색이 적용돼요. 시스템 미리보기에서 설정을 확정하세요.", style = MaterialTheme.typography.bodySmall)
        }
    }
}

@Composable
fun DiaryEmptyState(character: String) {
    Column(Modifier.fillMaxWidth().background(colorResource(R.color.myday_card).copy(alpha = DiaryDimensions.emptyStateOpacity),
        RoundedCornerShape(DiaryDimensions.cardCorner)).padding(DiaryDimensions.emptyStatePadding)) {
        CharacterAvatar(character)
        Text("아직 비어 있는 하루", style = MaterialTheme.typography.titleLarge)
        Text("아래 버튼으로 첫 블록을 추가해 보세요.")
    }
}
