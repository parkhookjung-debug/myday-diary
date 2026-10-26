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
    var seen by remember {mutableStateOf(book.collection?.actions?.lastOrNull {it.kind in setOf("draw","exchange","draw-tiered","exchange-tiered","craft")}?.id)}
    var reveal by remember {mutableStateOf<CollectionAction?>(null)}
    var exchange by remember {mutableStateOf<SangmonItem?>(null)};var rules by remember {mutableStateOf(false)}
    var atelier by remember {mutableStateOf(false)}
    val last=book.collection?.actions?.lastOrNull {it.kind in setOf("draw","exchange","draw-tiered","exchange-tiered","craft")}
    LaunchedEffect(last?.id) {if(last!=null && last.id!=seen){seen=last.id;if(last.kind=="craft")atelier=false;reveal=last;haptics.perform(TactileEvent.EQUIP)}}
    Column(modifier,verticalArrangement=Arrangement.spacedBy(6.dp)) {
        Text("상몬의 선물 상자",style=MaterialTheme.typography.titleMedium)
        Text("클로버 ${state.coins} · ${state.coins/CollectionRules.DRAW_COST}회 가능 · 꽃잎 ${state.petals} · 보유 ${state.owned.size}/300",Modifier.testTag("collection-balance"))
        if(state.coins<CollectionRules.DRAW_COST)Text("다음 선물까지 클로버 ${CollectionRules.DRAW_COST-state.coins}개 · 출석하거나 일기를 남겨 보세요.",style=MaterialTheme.typography.bodySmall)
        Row(Modifier.fillMaxWidth(),verticalAlignment=Alignment.CenterVertically) {
            Button({attempt(null) {controller.draw()}},Modifier.testTag("collection-draw"),enabled=controller.saved && state.coins>=20 && (book.collection?.actions?.size ?: 0)<CollectionRules.MAX_ACTIONS) {Text("클로버 20 · 선물 뽑기")}
            TextButton({rules=true}) {Text("받는 방법·확률")}
        }
        TextButton({atelier=true}) {Text("조각 합성 · 성취 도감")}
        Text((0..3).joinToString(" · ") {SharedCollection.names[it]+" "+state.materials[it]},style=MaterialTheme.typography.labelSmall)
        Text(if(state.owned.size==catalog.items.size)"모든 소품 수집 완료 · 일반 뽑기" else "${10-state.draws%10}회 후 새 소품 보장 · 기본 소품 8종은 무료",style=MaterialTheme.typography.labelSmall)
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
                        Text(CollectionRules.rarityName(item.id),style=MaterialTheme.typography.labelSmall)
                        TextButton({exchange=item},Modifier.testTag("exchange-${item.id}"),enabled=!owned && state.petals>=CollectionRules.exchangePrice(item.id) && controller.saved && (book.collection?.actions?.size ?: 0)<CollectionRules.MAX_ACTIONS) {Text(if(owned)"수집 완료" else "꽃잎 ${CollectionRules.exchangePrice(item.id)}개로 교환")}
                    }
                }
            }
        }
    }
    reveal?.let {action->
        val item=catalog.items.first {it.id==action.itemId}
        val prior=CollectionRules.state(book.copy(collection=CollectionBook(book.collection!!.actions.takeWhile {it.id!=action.id})))
        val duplicate=action.kind in setOf("draw","draw-tiered") && action.itemId in prior.owned
        AlertDialog(onDismissRequest={reveal=null},title={Text(if(action.kind=="craft")"직접 만들었어요!" else if(action.kind in setOf("exchange","exchange-tiered"))"꽃잎으로 데려왔어요" else if(duplicate)"또 만났어요 · 꽃잎 +${if(action.kind=="draw")3 else SharedCollection.duplicatePetals[CollectionRules.rarity(item.id)]}" else "새 소품을 만났어요!")},text={Column(horizontalAlignment=Alignment.CenterHorizontally) {AssetIcon("items/${item.id}.png",item.name,Modifier.fillMaxWidth().height(150.dp));Text("[${CollectionRules.rarityName(item.id)}] ${item.name}");val achieved=state.milestones-prior.milestones;if(achieved.isNotEmpty())Text("${achieved.max()}종 수집 달성 · 조각 보상!");Text("옷장에서 이 소품을 입힐 수 있어요.")}},confirmButton={TextButton({reveal=null},Modifier.testTag("collection-result-close")) {Text("도감에 보관")}})
    }
    exchange?.let {item->AlertDialog(onDismissRequest={exchange=null},title={Text("${item.name} 데려오기")},text={Text("꽃잎 ${CollectionRules.exchangePrice(item.id)}개로 교환할까요? 교환은 뽑기 횟수에 포함하지 않아요.")},confirmButton={TextButton({exchange=null;attempt(null) {controller.exchange(item.id)}}) {Text("교환하기")}},dismissButton={TextButton({exchange=null}) {Text("닫기")}})}
    if(atelier)PhoneAtelier(controller,catalog,haptics,attempt) {atelier=false}
    if(rules)AlertDialog(onDismissRequest={rules=false},title={Text("선물 상자 안내")},text={Text("1회 클로버 20개 · 기본 8종 무료\n등급 확률: ${CollectionRules.oddsText()}\n등급 안에서는 소품마다 같은 확률이에요. 10번째는 미수집만 뽑고 비어 있는 등급의 확률은 남은 등급에 비례 배분해요.\n\n소품과 같은 등급 조각 1개를 함께 받아요. 중복 꽃잎은 등급별 ${SharedCollection.duplicatePetals.joinToString("/")}개, 선택 교환은 ${SharedCollection.exchangePetals.joinToString("/")}개예요.\n조각 ${SharedCollection.synthesisCost}개 합성: ${SharedCollection.synthesisPercent.joinToString("/")}%로 상위 조각 1개. 실패하면 원래 조각 ${SharedCollection.failureRefund}개 반환. 조각 ${SharedCollection.craftCost}개로 같은 등급 소품을 골라 만들어요.\n\n수집 10/25/50/100/200/300종 성취 보상은 자동 지급해요. 현금 결제는 없어요.")},confirmButton={TextButton({rules=false}) {Text("닫기")}})
}
