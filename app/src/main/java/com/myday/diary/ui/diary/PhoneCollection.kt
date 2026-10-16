package com.myday.diary.ui.diary

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.grid.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.unit.dp
import com.myday.diary.data.*
import com.myday.diary.diary.*
import com.myday.diary.platform.*

@Composable fun PhoneCollection(controller: NotebookController,catalog: NotebookCatalog,haptics: DiaryHaptics,attempt: (TactileEvent?,()->Unit)->Unit,modifier: Modifier=Modifier) {
    val book=controller.book;val state=remember(book) {CollectionRules.state(book)}
    var query by remember {mutableStateOf("")};var ownedOnly by remember {mutableStateOf(false)}
    var seen by remember {mutableStateOf(book.collection?.actions?.lastOrNull()?.id)}
    var reveal by remember {mutableStateOf<CollectionAction?>(null)}
    var exchange by remember {mutableStateOf<SangmonItem?>(null)};var rules by remember {mutableStateOf(false)}
    val last=book.collection?.actions?.lastOrNull()
    LaunchedEffect(last?.id) {if(last!=null && last.id!=seen){seen=last.id;reveal=last;haptics.perform(TactileEvent.EQUIP)}}
    Column(modifier,verticalArrangement=Arrangement.spacedBy(6.dp)) {
        Text("상몬의 선물 상자",style=MaterialTheme.typography.titleMedium)
        Text("클로버 ${state.coins} · 꽃잎 ${state.petals} · 수집 ${state.owned.size}/300",Modifier.testTag("collection-balance"))
        Row(Modifier.fillMaxWidth(),verticalAlignment=Alignment.CenterVertically) {
            Button({attempt(null) {controller.draw()}},Modifier.testTag("collection-draw"),enabled=controller.saved && state.coins>=20 && (book.collection?.actions?.size ?: 0)<CollectionRules.MAX_ACTIONS) {Text("클로버 20 · 선물 뽑기")}
            TextButton({rules=true}) {Text("받는 방법·확률")}
        }
        Text(if(state.owned.size==catalog.items.size)"모든 소품 수집 완료 · 일반 뽑기" else "${10-state.draws%10}회 후 새 소품 보장 · 자유 꾸미기는 그대로",style=MaterialTheme.typography.labelSmall)
        Row(verticalAlignment=Alignment.CenterVertically) {
            OutlinedTextField(query,{query=it},Modifier.weight(1f).testTag("collection-search"),label={Text("수집 소품 검색")},singleLine=true)
            FilterChip(ownedOnly,{ownedOnly=!ownedOnly},label={Text("수집만")})
        }
        val matches=catalog.items.filter {(!ownedOnly || it.id in state.owned) && (it.name+it.id).contains(query.trim(),true)}
        if(matches.isEmpty())Text("아직 수집한 소품이 없거나 검색 결과가 없어요.")
        LazyVerticalGrid(GridCells.Fixed(2),Modifier.weight(1f).testTag("collection-grid"),horizontalArrangement=Arrangement.spacedBy(8.dp),verticalArrangement=Arrangement.spacedBy(8.dp),contentPadding=PaddingValues(bottom=20.dp)) {
            items(matches,key={it.id}) {item->val owned=item.id in state.owned
                Card(colors=CardDefaults.cardColors(containerColor=if(owned)Color(0xffeff5e8) else Color(0xfffffcf7))) {
                    Column(Modifier.fillMaxWidth().padding(10.dp),horizontalAlignment=Alignment.CenterHorizontally) {
                        AssetIcon("items/${item.id}.png",item.name,Modifier.height(76.dp));Text(item.name,style=MaterialTheme.typography.labelLarge)
                        TextButton({exchange=item},Modifier.testTag("exchange-${item.id}"),enabled=!owned && state.petals>=15 && controller.saved && (book.collection?.actions?.size ?: 0)<CollectionRules.MAX_ACTIONS) {Text(if(owned)"수집 완료" else "꽃잎 15개로 교환")}
                    }
                }
            }
        }
    }
    reveal?.let {action->
        val item=catalog.items.first {it.id==action.itemId}
        val prior=CollectionRules.state(book.copy(collection=CollectionBook(book.collection!!.actions.dropLast(1))))
        val duplicate=action.kind=="draw" && action.itemId in prior.owned
        AlertDialog(onDismissRequest={reveal=null},title={Text(if(action.kind=="exchange")"꽃잎으로 데려왔어요" else if(duplicate)"또 만났어요 · 꽃잎 +3" else "새 소품을 만났어요!")},text={Column(horizontalAlignment=Alignment.CenterHorizontally) {AssetIcon("items/${item.id}.png",item.name,Modifier.fillMaxWidth().height(150.dp));Text(item.name);Text("옷장에서 자유롭게 입혀볼 수 있어요.")}},confirmButton={TextButton({reveal=null},Modifier.testTag("collection-result-close")) {Text("도감에 보관")}})
    }
    exchange?.let {item->AlertDialog(onDismissRequest={exchange=null},title={Text("${item.name} 데려오기")},text={Text("꽃잎 15개로 교환할까요? 교환은 뽑기 횟수에 포함하지 않아요.")},confirmButton={TextButton({exchange=null;attempt(null) {controller.exchange(item.id)}}) {Text("교환하기")}},dismissButton={TextButton({exchange=null}) {Text("닫기")}})}
    if(rules)AlertDialog(onDismissRequest={rules=false},title={Text("선물 상자 안내")},text={Text("환영 클로버 40개\n1 XP = 클로버 1개\n레벨 업마다 +5개\n연속 출석 7일마다 +10개\n\n1회 클로버 20개. 일반 뽑기는 300종 각각 1/300이에요. 10번째마다 미수집 소품 중 동일 확률로 뽑아요. 전부 모았다면 일반 확률로 돌아가요.\n\n중복은 꽃잎 3개, 꽃잎 15개로 원하는 미수집 소품을 교환해요. 현금 결제는 없고 기존 300종 자유 꾸미기는 유지돼요.")},confirmButton={TextButton({rules=false}) {Text("닫기")}})
}
