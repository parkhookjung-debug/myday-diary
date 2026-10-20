package com.myday.diary.ui.diary

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.myday.diary.data.NotebookCatalog
import com.myday.diary.diary.*
import com.myday.diary.platform.*

@Composable fun PhoneAtelier(controller: NotebookController,catalog: NotebookCatalog,haptics: DiaryHaptics,attempt: (TactileEvent?,()->Unit)->Unit,close: ()->Unit) {
    val book=controller.book;val state=CollectionRules.state(book)
    var grade by remember {mutableIntStateOf(0)};var selected by remember {mutableStateOf("")};var menu by remember {mutableStateOf(false)};var result by remember {mutableStateOf("")}
    var seen by remember {mutableStateOf(book.collection?.actions?.lastOrNull()?.id)}
    AlertDialog(onDismissRequest=close,title={Text("작은 조각 공방")},text={Column(Modifier.heightIn(max=440.dp).verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(8.dp)) {
        Text((0..3).joinToString(" · ") {SharedCollection.names[it]+" 조각 "+state.materials[it]})
        Text("같은 등급 조각 ${SharedCollection.synthesisCost}개를 합성해요. 성공하면 상위 1개, 실패하면 원래 ${SharedCollection.failureRefund}개를 돌려줘요. 소품과 장비는 그대로예요.")
        for(g in 0..2)Button(onClick={attempt(null) {controller.synthesize(g)}},enabled=controller.saved && state.materials[g]>=SharedCollection.synthesisCost && (book.collection?.actions?.size ?: 0)<CollectionRules.MAX_ACTIONS) {Text("${SharedCollection.names[g]} → ${SharedCollection.names[g+1]} · ${SharedCollection.synthesisPercent[g]}%")}
        val last=book.collection?.actions?.lastOrNull()
        LaunchedEffect(last?.id) {if(last!=null && last.id!=seen){seen=last.id;if(last.kind=="synth"){result=if(last.itemId.endsWith(":up"))"상위 조각을 만들었어요!" else "이번에는 원래 조각 ${SharedCollection.failureRefund}개를 돌려받았어요.";haptics.perform(TactileEvent.EQUIP)}}}
        if(result.isNotEmpty())Text(result)
        HorizontalDivider();Text("같은 등급 조각 ${SharedCollection.craftCost}개로 원하는 소품 제작")
        Row {for(g in 0..3)TextButton({grade=g;selected=""}) {Text(SharedCollection.names[g])}}
        val targets=catalog.items.filter {it.id !in state.owned && CollectionRules.rarity(it.id)==grade}
        val target=targets.firstOrNull {it.id==selected} ?: targets.firstOrNull()
        Box {TextButton({menu=true},enabled=target!=null) {Text(target?.name ?: "이 등급을 모두 수집했어요")}
            DropdownMenu(menu,{menu=false}) {targets.forEach {item->DropdownMenuItem(text={Text(item.name)},onClick={selected=item.id;menu=false})}}
        }
        Button({target?.let {attempt(TactileEvent.EQUIP) {controller.craft(it.id)}}},enabled=target!=null && state.materials[grade]>=SharedCollection.craftCost && controller.saved && (book.collection?.actions?.size ?: 0)<CollectionRules.MAX_ACTIONS) {Text("조각 ${SharedCollection.craftCost}개로 제작")}
        HorizontalDivider();Text("차곡차곡, 수집 성취")
        SharedCollection.milestones.forEach {m->Text((if(m[0] in state.milestones)"✓ 달성" else "○ ${state.owned.size}/${m[0]}")+" · ${m[0]}종 → ${SharedCollection.names[m[1]]} 조각 +${m[2]}")}
        Text("합성 ${state.syntheses}회 · 성공 ${state.synthesisSuccesses}회 · 제작 ${state.crafts}회")
    }},confirmButton={TextButton(close) {Text("닫기")}})
}
