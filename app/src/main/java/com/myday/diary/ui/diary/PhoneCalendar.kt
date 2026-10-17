package com.myday.diary.ui.diary

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import com.myday.diary.data.DiaryBook
import java.time.LocalDate
import java.time.YearMonth

@Composable fun PhoneCalendar(book: DiaryBook,date: LocalDate,onDate: (LocalDate)->Unit) {
    var month by remember(date) {mutableStateOf(YearMonth.from(date))}
    var query by remember {mutableStateOf("")}
    var page by remember(month,query) {mutableIntStateOf(0)}
    val rows=remember(book.days,month,query) {book.days.entries.filter {(key,e)->if(query.isBlank()) YearMonth.from(LocalDate.parse(key))==month else e.blocks.any {it.text.contains(query.trim(),true)}}.sortedByDescending {it.key}}
    LazyColumn(Modifier.fillMaxSize().padding(horizontal=20.dp),verticalArrangement=Arrangement.spacedBy(12.dp),contentPadding=PaddingValues(bottom=24.dp)) {
        item {Text("차곡차곡 쌓인 기록",style=MaterialTheme.typography.headlineSmall);Text("기록한 날과 사진을 달력에서 찾아요.")}
        item {Card(colors=CardDefaults.cardColors(containerColor=Color.White)) {Column(Modifier.padding(12.dp)) {
            Row(Modifier.fillMaxWidth(),verticalAlignment=Alignment.CenterVertically,horizontalArrangement=Arrangement.SpaceBetween) {
                TextButton({month=month.minusMonths(1)},enabled=month.year>1 || month.monthValue>1) {Text("‹")}
                Text("${month.year}년 ${month.monthValue}월",style=MaterialTheme.typography.titleMedium)
                TextButton({month=month.plusMonths(1)},enabled=month.year<9999 || month.monthValue<12) {Text("›")}
            }
            Row {listOf("일","월","화","수","목","금","토").forEach {Text(it,Modifier.weight(1f).wrapContentWidth(Alignment.CenterHorizontally),style=MaterialTheme.typography.labelSmall)}}
            val first=month.atDay(1);val offset=first.dayOfWeek.value%7
            repeat(6) {week->Row {
                repeat(7) {column->val number=week*7+column-offset+1
                    if(number !in 1..month.lengthOfMonth()) Spacer(Modifier.weight(1f).height(52.dp))
                    else {val day=month.atDay(number);val saved=book.days[day.toString()];Surface(Modifier.weight(1f).height(52.dp).padding(2.dp).clickable {onDate(day)},shape=MaterialTheme.shapes.small,
                        color=if(day==date) MaterialTheme.colorScheme.primaryContainer else Color.Transparent) {Column(horizontalAlignment=Alignment.CenterHorizontally,verticalArrangement=Arrangement.Center) {
                        Text(number.toString(),color=if(column==0) Color(0xffad5b4e) else MaterialTheme.colorScheme.onSurface)
                        Text(if(saved?.blocks?.any {it.type=="photo"}==true) "▧" else if(saved!=null) "•" else "",style=MaterialTheme.typography.labelSmall,color=MaterialTheme.colorScheme.primary)
                    }}}
                }
            }}
        }}}
        item {OutlinedTextField(query,{query=it},label={Text("본문·사진 설명 검색")},modifier=Modifier.fillMaxWidth(),singleLine=true)}
        item {Text("${rows.size}일의 기록",style=MaterialTheme.typography.labelLarge)}
        if(rows.isEmpty()) item {Text("찾은 기록이 없어요. 날짜를 눌러 새로운 하루를 시작해 보세요.")}
        items(rows.drop(page*50).take(50),key={it.key}) {(day,e)->Card(Modifier.fillMaxWidth().clickable {onDate(LocalDate.parse(day))},colors=CardDefaults.cardColors(containerColor=Color.White)) {
            Column(Modifier.padding(16.dp)) {Text(day,style=MaterialTheme.typography.titleMedium);Text(e.blocks.firstOrNull {it.text.isNotBlank() && (query.isBlank() || it.text.contains(query,true))}?.text?.take(120) ?: "기록 ${e.blocks.size}개",maxLines=2)
                if(e.blocks.any {it.type=="photo"}) Text("사진 있음",style=MaterialTheme.typography.labelSmall,color=MaterialTheme.colorScheme.primary)}
        }}
        if(rows.size>50) item {Row(Modifier.fillMaxWidth(),horizontalArrangement=Arrangement.SpaceBetween) {TextButton({page--},enabled=page>0) {Text("최근 기록")};Text("${page+1} / ${(rows.size+49)/50}");TextButton({page++},enabled=(page+1)*50<rows.size) {Text("이전 기록")}}}
    }
}
