package com.myday.diary.ui.diary

import android.graphics.BitmapFactory
import android.graphics.Bitmap
import androidx.compose.foundation.Image
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.grid.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.unit.dp
import com.myday.diary.data.*
import com.myday.diary.diary.*
import com.myday.diary.platform.*
import com.myday.diary.ui.components.SangmonAvatar

@Composable fun AssetIcon(path: String,description: String,modifier: Modifier=Modifier,atlas: Boolean=false) {
    val context=LocalContext.current
    val image=remember(path) {context.assets.open(path).use {BitmapFactory.decodeStream(it)}.let {source->if(atlas) Bitmap.createBitmap(source,0,0,192,192).also {source.recycle()} else source}}
    DisposableEffect(image) {onDispose {image?.recycle()}}
    if(image!=null) Image(image.asImageBitmap(),description,modifier)
}

@Composable fun PhoneCompanion(controller: NotebookController,catalog: NotebookCatalog,haptics: DiaryHaptics,attempt: (TactileEvent?,()->Unit)->Unit) {
    var tab by remember {mutableIntStateOf(0)}
    val book=controller.book;val level=NotebookRules.level(book);val xp=NotebookRules.xp(book)
    val next=catalog.characters.filter {it.level>level}.minByOrNull {it.level}
    Column(Modifier.fillMaxSize().padding(horizontal=20.dp)) {
        Card(Modifier.fillMaxWidth(),colors=CardDefaults.cardColors(containerColor=Color(0xfffffcf8))) {
            Column(Modifier.fillMaxWidth().padding(16.dp),horizontalAlignment=Alignment.CenterHorizontally) {
                Text("YOUR LITTLE FRIEND",style=MaterialTheme.typography.labelSmall,color=MaterialTheme.colorScheme.primary)
                SangmonAvatar(book.characterStyle,book.equipment,haptics=haptics)
                Text("오늘도 네 편이야",style=MaterialTheme.typography.headlineSmall)
                Text("Lv.$level · $xp XP · 기록 ${book.awardedDays.size}일",style=MaterialTheme.typography.bodyMedium)
                LinearProgressIndicator(progress={book.awardedDays.size%5/5f},modifier=Modifier.fillMaxWidth().padding(vertical=10.dp))
                Text(next?.let {"다음 보상: ${it.name} · Lv.${it.level}"} ?: "모든 성장 외형을 만났어요!",style=MaterialTheme.typography.bodySmall)
                Text("하루 기록 +10 XP · 50 XP마다 레벨 업",style=MaterialTheme.typography.labelSmall)
            }
        }
        Row(Modifier.fillMaxWidth(),horizontalArrangement=Arrangement.spacedBy(8.dp)) {
            FilterChip(tab==0,{tab=0;haptics.perform(TactileEvent.SELECT)},label={Text("상몬 64종")})
            FilterChip(tab==1,{tab=1;haptics.perform(TactileEvent.SELECT)},label={Text("장비 12종")})
        }
        if(tab==0) LazyVerticalGrid(GridCells.Fixed(2),Modifier.weight(1f).testTag("sangmon-grid"),horizontalArrangement=Arrangement.spacedBy(10.dp),verticalArrangement=Arrangement.spacedBy(10.dp),contentPadding=PaddingValues(bottom=24.dp)) {
            items(catalog.characters,key={it.id}) {skin->val unlocked=level>=skin.level
                Card(Modifier.clickable(enabled=unlocked && !controller.saving) {attempt(TactileEvent.EQUIP) {controller.selectCharacter(skin.id,skin.level)}},colors=CardDefaults.cardColors(containerColor=if(book.characterStyle==skin.id) MaterialTheme.colorScheme.primaryContainer else Color.White)) {
                    Column(Modifier.fillMaxWidth().padding(12.dp),horizontalAlignment=Alignment.CenterHorizontally) {
                        AssetIcon("sangmon/${skin.id}.png",skin.name,Modifier.size(90.dp),true)
                        Text(skin.name,style=MaterialTheme.typography.labelLarge)
                        Text(if(!unlocked) "Lv.${skin.level} 해금" else if(book.characterStyle==skin.id) "함께하는 중" else "함께하기",style=MaterialTheme.typography.labelSmall)
                    }
                }
            }
        } else {
            var slot by remember {mutableStateOf("weapon")}
            Row(Modifier.fillMaxWidth(),horizontalArrangement=Arrangement.SpaceBetween) {
                FilterChip(slot=="weapon",{slot="weapon"},label={Text("검")});FilterChip(slot=="charm",{slot="charm"},label={Text("보주")})
                TextButton({attempt(TactileEvent.EQUIP) {controller.equip(slot,"none")}}) {Text("선택 슬롯 해제")}
            }
            Text("검: ${catalog.items.find {it.id==book.equipment.weaponId}?.name ?: "없음"}\n보주: ${catalog.items.find {it.id==book.equipment.charmId}?.name ?: "없음"}",style=MaterialTheme.typography.bodySmall)
            LazyVerticalGrid(GridCells.Fixed(2),Modifier.weight(1f).testTag("equipment-grid"),horizontalArrangement=Arrangement.spacedBy(10.dp),verticalArrangement=Arrangement.spacedBy(10.dp),contentPadding=PaddingValues(top=12.dp,bottom=24.dp)) {
                items(catalog.items.filter {it.slot==slot},key={it.id}) {item->val equipped=item.id in listOf(book.equipment.weaponId,book.equipment.charmId)
                    Card(Modifier.clickable(enabled=!controller.saving) {attempt(TactileEvent.EQUIP) {controller.equip(item.slot,item.id)}},colors=CardDefaults.cardColors(containerColor=if(equipped) MaterialTheme.colorScheme.primaryContainer else Color(0xffeeeeff))) {
                        Column(Modifier.fillMaxWidth().padding(12.dp),horizontalAlignment=Alignment.CenterHorizontally) {AssetIcon("items/${item.id}.png",item.name,Modifier.height(128.dp));Text(item.name,style=MaterialTheme.typography.labelLarge);Text(item.description,style=MaterialTheme.typography.labelSmall);Text(if(equipped) "장착 중" else "장착하기",style=MaterialTheme.typography.labelSmall)}
                    }
                }
            }
        }
    }
}
