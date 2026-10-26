package com.myday.diary.ui.diary

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.unit.dp
import com.myday.diary.data.*
import com.myday.diary.diary.*
import com.myday.diary.ui.design.AppPalette

@Composable fun PhoneWelcome(book:DiaryBook,onClose:()->Unit,onAction:(Int)->Unit) {
    var section by remember{mutableIntStateOf(0)}
    val scroll=rememberScrollState()
    LaunchedEffect(section){scroll.scrollTo(0)}
    PhoneFullDialog(ExperienceCatalog.title,onClose,null) {
        Column(Modifier.fillMaxSize()) {
            ScrollableTabRow(selectedTabIndex=section,containerColor=AppPalette.sidebar){ExperienceCatalog.titles.forEachIndexed{i,title->Tab(selected=section==i,onClick={section=i},modifier=Modifier.testTag("guide-tab-$i"),text={Text(title)})}}
            Column(Modifier.weight(1f).verticalScroll(scroll).padding(24.dp),verticalArrangement=Arrangement.spacedBy(18.dp)) {
                Text(ExperienceCatalog.titles[section],style=MaterialTheme.typography.headlineSmall)
                if(section==2){val state=CollectionRules.state(book);val xp=NotebookRules.xp(book)
                    Card(colors=CardDefaults.cardColors(containerColor=AppPalette.friendCard)){Text("Lv.${NotebookRules.level(book)} · $xp XP · 만렙까지 ${maxOf(0,3590-xp)} XP\n클로버 ${state.coins}개 · ${state.coins/CollectionRules.DRAW_COST}회 뽑기 가능 · 다음 1회까지 ${maxOf(0,CollectionRules.DRAW_COST-state.coins)}개\n"+(if(state.owned.size==300)"소품 300종 수집 완료" else "${10-state.draws%10}회 후 새 소품 보장")+" · 보유 ${state.owned.size} / 300종",Modifier.padding(16.dp),style=MaterialTheme.typography.bodyMedium)}
                }
                Text(ExperienceCatalog.bodies[section],style=MaterialTheme.typography.bodyLarge)
                if(section==0)listOf("글 일기 쓰기","사진 추가","하루 꾸미기").forEachIndexed{i,title->OutlinedButton({onAction(i)},Modifier.fillMaxWidth().testTag("guide-action-$i")){Text(title)}}
            }
            Button(onClose,Modifier.fillMaxWidth().padding(16.dp).testTag("guide-close")){Text("시작하기")}
        }
    }
}
