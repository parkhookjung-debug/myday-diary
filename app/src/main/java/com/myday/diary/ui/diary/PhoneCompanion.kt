package com.myday.diary.ui.diary

import android.graphics.BitmapFactory
import androidx.compose.foundation.Image
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.lazy.grid.*
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.platform.LocalSoftwareKeyboardController
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.unit.dp
import com.myday.diary.data.*
import com.myday.diary.diary.*
import com.myday.diary.platform.*
import com.myday.diary.ui.components.SangmonAvatar

@Composable fun AssetIcon(path: String,description: String,modifier: Modifier=Modifier) {
    val context=LocalContext.current
    val image=remember(path) {context.assets.open(path).use {BitmapFactory.decodeStream(it)}}
    DisposableEffect(image) {onDispose {image?.recycle()}}
    if(image!=null) Image(image.asImageBitmap(),description,modifier)
}

@Composable fun PhoneCompanion(controller: NotebookController,catalog: NotebookCatalog,haptics: DiaryHaptics,attempt: (TactileEvent?,()->Unit)->Unit) {
    var tab by remember {mutableIntStateOf(0)}
    var missions by remember {mutableStateOf(false)}
    val keyboard=LocalSoftwareKeyboardController.current
    val book=controller.book;val level=NotebookRules.level(book);val xp=NotebookRules.xp(book)
    val owned=remember(book) {CollectionRules.state(book).owned}
    val today=controller.actualDay;val flags=book.activities[today.toString()] ?: 0
    val attended=flags and NotebookRules.ATTENDANCE!=0
    Column(Modifier.fillMaxSize().padding(horizontal=16.dp)) {
        Card(Modifier.fillMaxWidth(),colors=CardDefaults.cardColors(containerColor=Color(0xfffffcf8))) {
            Column(Modifier.padding(12.dp)) {
                Row(verticalAlignment=Alignment.CenterVertically) {
                    SangmonAvatar(book.characterStyle,book.equipment,modifier=Modifier.size(96.dp),haptics=haptics)
                    Column(Modifier.weight(1f).padding(start=8.dp)) {
                        Text("Lv.$level · ${NotebookRules.chapter(level)}",style=MaterialTheme.typography.titleLarge)
                        Text("총 $xp XP · 기록 ${book.awardedDays.size}일",style=MaterialTheme.typography.bodySmall)
                        LinearProgressIndicator(progress={NotebookRules.intoLevel(book).toFloat()/NotebookRules.neededXp(level)},modifier=Modifier.fillMaxWidth().padding(vertical=8.dp))
                        Text(if(level>=NotebookRules.MAX_LEVEL) "만렙 달성! 이후 50 XP마다 클로버 +5" else "다음 레벨까지 ${NotebookRules.neededXp(level)-NotebookRules.intoLevel(book)} XP · 레벨 업 클로버 +5",style=MaterialTheme.typography.labelSmall)
                    }
                }
                Row(Modifier.fillMaxWidth(),verticalAlignment=Alignment.CenterVertically,horizontalArrangement=Arrangement.SpaceBetween) {
                    TextButton({missions=true}) {Text("오늘 ${NotebookRules.todayActivityXp(book,today)}/40 XP · 미션")}
                    Button({attempt(TactileEvent.CHECK) {controller.checkIn()}},enabled=!attended && controller.saved) {Text(if(attended) "출석 완료" else "출석 +5 XP")}
                }
                Text("만렙 Lv.100 · 3,590 XP · 매일 루틴 약 100일 / 주 5일 약 200일 / 여유롭게 약 300일",style=MaterialTheme.typography.labelSmall)
                Text("연속 7일 출석마다 +10 XP · 오늘 보너스 ${NotebookRules.todayBonusXp(book,today)} XP",style=MaterialTheme.typography.labelSmall)
            }
        }
        Row(horizontalArrangement=Arrangement.spacedBy(8.dp)) {
            FilterChip(tab==0,{tab=0;haptics.perform(TactileEvent.SELECT)},label={Text("상몬 64종")})
            FilterChip(tab==1,{tab=1;haptics.perform(TactileEvent.SELECT)},label={Text("소품 300종")})
            FilterChip(tab==2,{tab=2;haptics.perform(TactileEvent.SELECT)},label={Text("소품 뽑기")})
        }
        if(tab==0) LazyVerticalGrid(GridCells.Fixed(2),Modifier.weight(1f).testTag("sangmon-grid"),horizontalArrangement=Arrangement.spacedBy(10.dp),verticalArrangement=Arrangement.spacedBy(10.dp),contentPadding=PaddingValues(bottom=24.dp)) {
            items(catalog.characters,key={it.id}) {skin->val unlocked=xp>=skin.xp
                Card(Modifier.clickable(enabled=unlocked && !controller.saving) {attempt(TactileEvent.EQUIP) {controller.selectCharacter(skin.id,skin.level)}},colors=CardDefaults.cardColors(containerColor=if(book.characterStyle==skin.id) MaterialTheme.colorScheme.primaryContainer else Color.White)) {
                    Column(Modifier.fillMaxWidth().padding(12.dp),horizontalAlignment=Alignment.CenterHorizontally) {
                        AssetIcon("portraits/${skin.id}.png",skin.name,Modifier.size(80.dp))
                        Text(skin.name,style=MaterialTheme.typography.labelLarge)
                        Text(if(!unlocked) "Lv.${skin.level} · 총 ${skin.xp} XP 해금" else if(book.characterStyle==skin.id) "함께하는 중" else "함께하기",style=MaterialTheme.typography.labelSmall)
                    }
                }
            }
        } else if(tab==2) {
            PhoneCollection(controller,catalog,haptics,attempt,Modifier.weight(1f))
        } else {
            var slot by remember {mutableStateOf("weapon")};var query by remember {mutableStateOf("")}
            var ownedOnly by remember {mutableStateOf(false)}
            var element by remember {mutableStateOf("")};var menu by remember {mutableStateOf(false)}
            LazyRow(Modifier.testTag("equipment-slots"),horizontalArrangement=Arrangement.spacedBy(6.dp)) {
                items(EquipmentCatalog.slots+"all") {s->FilterChip(slot==s,{slot=s;haptics.perform(TactileEvent.SELECT)},label={Text(if(s=="all") "전체" else EquipmentCatalog.names[EquipmentCatalog.slots.indexOf(s)])})}
            }
            Row(verticalAlignment=Alignment.CenterVertically) {
                OutlinedTextField(query,{query=it},Modifier.weight(1f).testTag("equipment-search"),label={Text("소품 이름 검색")},singleLine=true,keyboardOptions=KeyboardOptions(imeAction=ImeAction.Done),keyboardActions=KeyboardActions(onDone={keyboard?.hide()}))
                Box {
                    TextButton({menu=true}) {Text(if(element.isEmpty()) "모든 색감" else mapOf("Solar" to "크림","Ember" to "살구","Frost" to "하늘","Storm" to "민트","Shadow" to "라벤더","Astral" to "딸기")[element] ?: element)}
                    DropdownMenu(menu,{menu=false}) {listOf("" to "모든 색감","Solar" to "크림","Ember" to "살구","Frost" to "하늘","Storm" to "민트","Shadow" to "라벤더","Astral" to "딸기").forEach {(id,name)->DropdownMenuItem(text={Text(name)},onClick={element=id;menu=false})}}
                }
            }
            FilterChip(ownedOnly,{ownedOnly=!ownedOnly},label={Text("보유 소품만")})
            val matches=catalog.items.filter {(!ownedOnly || it.id in owned) && (slot=="all" || it.slot==slot) && (element.isEmpty() || it.element==element) && (query.isBlank() || (it.name+it.description+it.id).contains(query.trim(),true))}
            Row(Modifier.fillMaxWidth(),verticalAlignment=Alignment.CenterVertically,horizontalArrangement=Arrangement.SpaceBetween) {
                Text("${matches.size}가지 · 소품 ${EquipmentCatalog.slots.count {book.equipment.get(it)!="none"}}개와 함께",style=MaterialTheme.typography.labelSmall)
                TextButton({attempt(TactileEvent.EQUIP) {controller.equip(slot,"none")}},enabled=slot!="all" && controller.saved) {Text("이 소품 벗기기")}
            }
            if(matches.isEmpty())Text("맞는 소품이 없어요. 이름이나 색감을 바꿔보세요.",style=MaterialTheme.typography.bodySmall)
            LazyVerticalGrid(GridCells.Fixed(2),Modifier.weight(1f).testTag("equipment-grid"),horizontalArrangement=Arrangement.spacedBy(10.dp),verticalArrangement=Arrangement.spacedBy(10.dp),contentPadding=PaddingValues(bottom=24.dp)) {
                items(matches,key={it.id}) {item->val equipped=item.id==book.equipment.get(item.slot)
                    Card(Modifier.testTag("equipment-${item.id}").clickable(enabled=controller.saved && item.id in owned) {attempt(TactileEvent.EQUIP) {controller.equip(item.slot,item.id)}},colors=CardDefaults.cardColors(containerColor=if(equipped) Color(0xfff0e5d9) else Color(0xfffffcf7))) {
                        Column(Modifier.fillMaxWidth().padding(10.dp),horizontalAlignment=Alignment.CenterHorizontally) {
                            AssetIcon("items/${item.id}.png",item.name,Modifier.height(96.dp));Text(item.name,style=MaterialTheme.typography.labelLarge)
                            Text(item.description,style=MaterialTheme.typography.labelSmall);Text(if(equipped) "함께하는 중" else if(item.id !in owned)"잠김 · 뽑기나 꽃잎 교환으로 얻어요" else if(item.id in CollectionRules.starters)"기본 소품 · 입혀 보기" else "보유 · 입혀 보기",style=MaterialTheme.typography.labelSmall)
                        }
                    }
                }
            }
        }
    }
    if(missions) AlertDialog(onDismissRequest={missions=false},title={Text("오늘의 성장 미션")},text={
        Column(verticalArrangement=Arrangement.spacedBy(10.dp)) {
            listOf(NotebookRules.ATTENDANCE to "출석 +5",0 to "첫 기록 저장 +10",NotebookRules.WRITING to "글 20자 이상 +10",NotebookRules.PHOTO to "사진 첨부 +5",NotebookRules.TODO to "할 일 완료 +5",NotebookRules.HABIT to "습관 완료 +5").forEach {(flag,name)->
                val done=if(flag==0)today.toString() in book.awardedDays else flags and flag!=0
                Text((if(done) "✓ " else "○ ")+name+" XP")
            }
            Text("각 항목 하루 한 번 · 기본 최대 40 XP · 연속 7일 출석마다 별도 +10 XP\n글자 수는 글·감정 블록의 공백을 제외해 합산해요. 저장에 성공한 활동만 지급해요. 새싹(1~9) 10 XP · 일상(10~29) 20 XP · 동행(30~49) 30 XP · 단짝(50~99) 50 XP씩 레벨 업해요. Lv.100은 3,590 XP이며 이후에도 기록과 클로버 보상은 계속돼요. 기존 경험치와 외형 해금 XP는 유지돼요.",style=MaterialTheme.typography.bodySmall)
        }
    },confirmButton={TextButton({missions=false}) {Text("닫기")}})
}
