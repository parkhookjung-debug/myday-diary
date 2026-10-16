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
    val today=controller.actualDay;val flags=book.activities[today.toString()] ?: 0
    val attended=flags and NotebookRules.ATTENDANCE!=0
    Column(Modifier.fillMaxSize().padding(horizontal=16.dp)) {
        Card(Modifier.fillMaxWidth(),colors=CardDefaults.cardColors(containerColor=Color(0xfffffcf8))) {
            Column(Modifier.padding(12.dp)) {
                Row(verticalAlignment=Alignment.CenterVertically) {
                    SangmonAvatar(book.characterStyle,book.equipment,modifier=Modifier.size(96.dp),haptics=haptics)
                    Column(Modifier.weight(1f).padding(start=8.dp)) {
                        Text("Lv.$level · 나의 상몬",style=MaterialTheme.typography.titleLarge)
                        Text("총 $xp XP · 기록 ${book.awardedDays.size}일",style=MaterialTheme.typography.bodySmall)
                        LinearProgressIndicator(progress={xp%50/50f},modifier=Modifier.fillMaxWidth().padding(vertical=8.dp))
                        Text("다음 레벨까지 ${50-xp%50} XP",style=MaterialTheme.typography.labelSmall)
                    }
                }
                Row(Modifier.fillMaxWidth(),verticalAlignment=Alignment.CenterVertically,horizontalArrangement=Arrangement.SpaceBetween) {
                    TextButton({missions=true}) {Text("오늘 ${NotebookRules.todayXp(book,today)}/40 XP · 미션")}
                    Button({attempt(TactileEvent.CHECK) {controller.checkIn()}},enabled=!attended && controller.saved) {Text(if(attended) "출석 완료" else "출석 +5 XP")}
                }
            }
        }
        Row(horizontalArrangement=Arrangement.spacedBy(8.dp)) {
            FilterChip(tab==0,{tab=0;haptics.perform(TactileEvent.SELECT)},label={Text("상몬 64종")})
            FilterChip(tab==1,{tab=1;haptics.perform(TactileEvent.SELECT)},label={Text("소품 300종")})
        }
        if(tab==0) LazyVerticalGrid(GridCells.Fixed(2),Modifier.weight(1f).testTag("sangmon-grid"),horizontalArrangement=Arrangement.spacedBy(10.dp),verticalArrangement=Arrangement.spacedBy(10.dp),contentPadding=PaddingValues(bottom=24.dp)) {
            items(catalog.characters,key={it.id}) {skin->val unlocked=level>=skin.level
                Card(Modifier.clickable(enabled=unlocked && !controller.saving) {attempt(TactileEvent.EQUIP) {controller.selectCharacter(skin.id,skin.level)}},colors=CardDefaults.cardColors(containerColor=if(book.characterStyle==skin.id) MaterialTheme.colorScheme.primaryContainer else Color.White)) {
                    Column(Modifier.fillMaxWidth().padding(12.dp),horizontalAlignment=Alignment.CenterHorizontally) {
                        AssetIcon("portraits/${skin.id}.png",skin.name,Modifier.size(80.dp))
                        Text(skin.name,style=MaterialTheme.typography.labelLarge)
                        Text(if(!unlocked) "Lv.${skin.level} 해금" else if(book.characterStyle==skin.id) "함께하는 중" else "함께하기",style=MaterialTheme.typography.labelSmall)
                    }
                }
            }
        } else {
            var slot by remember {mutableStateOf("weapon")};var query by remember {mutableStateOf("")}
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
            val matches=catalog.items.filter {(slot=="all" || it.slot==slot) && (element.isEmpty() || it.element==element) && (query.isBlank() || (it.name+it.description+it.id).contains(query.trim(),true))}
            Row(Modifier.fillMaxWidth(),verticalAlignment=Alignment.CenterVertically,horizontalArrangement=Arrangement.SpaceBetween) {
                Text("${matches.size}가지 · 소품 ${EquipmentCatalog.slots.count {book.equipment.get(it)!="none"}}개와 함께",style=MaterialTheme.typography.labelSmall)
                TextButton({attempt(TactileEvent.EQUIP) {controller.equip(slot,"none")}},enabled=slot!="all" && controller.saved) {Text("이 소품 벗기기")}
            }
            if(matches.isEmpty())Text("맞는 소품이 없어요. 이름이나 색감을 바꿔보세요.",style=MaterialTheme.typography.bodySmall)
            LazyVerticalGrid(GridCells.Fixed(2),Modifier.weight(1f).testTag("equipment-grid"),horizontalArrangement=Arrangement.spacedBy(10.dp),verticalArrangement=Arrangement.spacedBy(10.dp),contentPadding=PaddingValues(bottom=24.dp)) {
                items(matches,key={it.id}) {item->val equipped=item.id==book.equipment.get(item.slot)
                    Card(Modifier.testTag("equipment-${item.id}").clickable(enabled=!controller.saving) {attempt(TactileEvent.EQUIP) {controller.equip(item.slot,item.id)}},colors=CardDefaults.cardColors(containerColor=if(equipped) Color(0xfff0e5d9) else Color(0xfffffcf7))) {
                        Column(Modifier.fillMaxWidth().padding(10.dp),horizontalAlignment=Alignment.CenterHorizontally) {
                            AssetIcon("items/${item.id}.png",item.name,Modifier.height(96.dp));Text(item.name,style=MaterialTheme.typography.labelLarge)
                            Text(item.description,style=MaterialTheme.typography.labelSmall);Text(if(equipped) "함께하는 중" else "입혀 보기",style=MaterialTheme.typography.labelSmall)
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
            Text("각 항목 하루 한 번 · 최대 40 XP\n글자 수는 글·감정 블록의 공백을 제외해 합산해요. 저장에 성공한 활동만 지급해요. 50 XP마다 레벨 업하며 기존 경험치는 유지돼요.",style=MaterialTheme.typography.bodySmall)
        }
    },confirmButton={TextButton({missions=false}) {Text("닫기")}})
}
