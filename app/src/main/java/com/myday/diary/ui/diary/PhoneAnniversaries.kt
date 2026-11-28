package com.myday.diary.ui.diary

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.Alignment
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.unit.dp
import com.myday.diary.data.*
import java.time.LocalDate
import java.util.UUID

@Composable fun PhoneAnniversaries(rows: List<Anniversary>,day: LocalDate,save: (List<Anniversary>)->Boolean) {
    var name by remember {mutableStateOf("")}
    var date by remember {mutableStateOf(day.toString())}
    var yearly by remember {mutableStateOf(true)}
    var error by remember {mutableStateOf<String?>(null)}
    Card {Column(Modifier.fillMaxWidth().padding(14.dp),verticalArrangement=Arrangement.spacedBy(8.dp)) {
        Text("생일 · 기념일",style=MaterialTheme.typography.titleMedium)
        OutlinedTextField(name,{name=it.take(StudioCatalog.anniversaryNameLength)},label={Text("기념일 이름")},singleLine=true,modifier=Modifier.fillMaxWidth().testTag("anniversary-name"))
        OutlinedTextField(date,{date=it.take(10)},label={Text("날짜 YYYY-MM-DD")},singleLine=true,modifier=Modifier.fillMaxWidth().testTag("anniversary-date"))
        Row(verticalAlignment=Alignment.CenterVertically) {Checkbox(yearly,{yearly=it});Text("매년 같은 날짜에 표시")}
        Button({
            runCatching {val value=LocalDate.parse(date);require(value.year in 1..9999 && value.toString()==date);require(name.trim().isNotEmpty());require(rows.size<StudioCatalog.anniversaryLimit)
                rows+Anniversary(UUID.randomUUID().toString(),name.trim(),date,yearly)
            }.onSuccess {if(save(it)){name="";error=null}}.onFailure {error="이름과 올바른 날짜를 입력해 주세요. 기념일은 ${StudioCatalog.anniversaryLimit}개까지예요."}
        },modifier=Modifier.testTag("anniversary-add")) {Text("기념일 추가")}
        error?.let {Text(it,color=MaterialTheme.colorScheme.error)}
        rows.sortedWith(compareBy({it.date},{it.name})).forEach {item -> Row(Modifier.fillMaxWidth(),verticalAlignment=Alignment.CenterVertically) {
            Column(Modifier.weight(1f)) {Text(item.name);Text(item.date+if(item.repeatYearly) " · 매년" else " · 한 번",style=MaterialTheme.typography.bodySmall)}
            TextButton({save(rows.filterNot {it.id==item.id})}) {Text("삭제")}
        }}
    }}
}
