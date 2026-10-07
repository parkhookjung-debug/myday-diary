package com.myday.diary

import android.app.DatePickerDialog
import android.app.WallpaperManager
import android.appwidget.AppWidgetManager
import android.content.ActivityNotFoundException
import android.content.ComponentName
import android.content.Intent
import android.os.Bundle
import android.widget.Toast
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import org.json.JSONArray
import org.json.JSONObject
import java.time.LocalDate
import java.util.UUID

data class DiaryBlock(
    val id: String = UUID.randomUUID().toString(),
    val type: String,
    val text: String = "",
    val checked: Boolean = false
)
data class DiaryEntry(val theme: Int = 0, val character: String = "🐰", val blocks: List<DiaryBlock> = emptyList())

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent { DiaryApp() }
    }
}

// Keep the prototype offline. One JSON entry per date, with synchronous writes
// so switching days or closing the activity cannot discard pending edits.
class DiaryStore(context: android.content.Context) {
    private val appContext = context.applicationContext
    private val prefs = context.getSharedPreferences("diary", android.content.Context.MODE_PRIVATE)
    fun read(date: String): DiaryEntry {
        val raw = prefs.getString(date, null) ?: return DiaryEntry()
        val json = JSONObject(raw)
        val array = json.getJSONArray("blocks")
        return DiaryEntry(json.optInt("theme").coerceIn(0, 2), json.optString("character", "🐰"),
            (0 until array.length()).map {
                val block = array.getJSONObject(it)
                DiaryBlock(block.getString("id"), block.getString("type"), block.optString("text"), block.optBoolean("checked"))
            })
    }
    fun write(date: String, entry: DiaryEntry): Boolean {
        val previous = read(date)
        val blocks = JSONArray()
        entry.blocks.forEach { block ->
            blocks.put(JSONObject().put("id", block.id).put("type", block.type)
                .put("text", block.text).put("checked", block.checked))
        }
        val saved = prefs.edit().putString(date, JSONObject().put("theme", entry.theme)
            .put("character", entry.character).put("blocks", blocks).toString()).commit()
        // Text typing does not redraw every widget. Only update information shown there.
        fun progress(value: DiaryEntry) = value.blocks.filter { it.type in listOf("todo", "habit") }.map { it.type to it.checked }
        if (saved && date == LocalDate.now().toString() &&
            (previous.theme != entry.theme || previous.character != entry.character || progress(previous) != progress(entry))) {
            DiaryWidgetProvider.refreshAll(appContext)
        }
        return saved
    }
}

@Composable
fun DiaryApp() {
    val context = LocalContext.current
    val store = remember { DiaryStore(context) }
    var date by remember { mutableStateOf(LocalDate.now()) }
    var entry by remember(date) { mutableStateOf(store.read(date.toString())) }
    var saved by remember(date) { mutableStateOf(true) }
    var showAdd by remember { mutableStateOf(false) }
    var deleting by remember { mutableStateOf<String?>(null) }
    val backgrounds = listOf(Color(0xFFFFF8F3), Color(0xFFF0F5EF), Color(0xFFF3F0FA))
    fun update(next: DiaryEntry) {
        entry = next
        saved = store.write(date.toString(), next)
    }
    fun updateBlock(block: DiaryBlock) = update(entry.copy(blocks = entry.blocks.map { if (it.id == block.id) block else it }))
    fun move(index: Int, delta: Int) {
        val next = entry.blocks.toMutableList()
        val destination = index + delta
        if (destination in next.indices) {
            next.add(destination, next.removeAt(index))
            update(entry.copy(blocks = next))
        }
    }
    MaterialTheme(colorScheme = lightColorScheme(primary = Color(0xFF805C49), secondary = Color(0xFF66745D))) {
        Scaffold(containerColor = backgrounds[entry.theme]) { padding ->
            LazyColumn(Modifier.fillMaxSize().padding(padding).imePadding().padding(horizontal = 20.dp),
                verticalArrangement = Arrangement.spacedBy(14.dp), contentPadding = PaddingValues(vertical = 20.dp)) {
                item {
                    Text("MY DAY", style = MaterialTheme.typography.labelLarge, color = MaterialTheme.colorScheme.primary)
                    Text("내 마음대로, 나의 하루", style = MaterialTheme.typography.headlineSmall)
                    Text("필요한 블록을 골라 나만의 일기를 만들어 보세요.", style = MaterialTheme.typography.bodyMedium)
                }
                item {
                    Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                        TextButton(onClick = { if (saved) date = date.minusDays(1) }, enabled = saved) { Text("‹ 이전") }
                        TextButton(onClick = {
                            DatePickerDialog(context, { _, year, month, day -> date = LocalDate.of(year, month + 1, day) },
                                date.year, date.monthValue - 1, date.dayOfMonth).show()
                        }, enabled = saved) { Text(date.toString()) }
                        TextButton(onClick = { if (saved) date = date.plusDays(1) }, enabled = saved) { Text("다음 ›") }
                    }
                    Text(if (saved) "휴대폰에 저장됨 · 자동 저장" else "저장 실패 · 날짜를 바꾸기 전에 다시 저장해 주세요",
                        style = MaterialTheme.typography.labelSmall)
                    if (!saved) TextButton(onClick = { saved = store.write(date.toString(), entry) }) { Text("다시 저장") }
                }
                item {
                    Card(shape = RoundedCornerShape(20.dp)) {
                        Column(Modifier.padding(16.dp)) {
                            Text("오늘의 분위기", style = MaterialTheme.typography.titleMedium)
                            Row(horizontalArrangement = Arrangement.spacedBy(6.dp)) {
                                listOf("크림", "숲", "라벤더").forEachIndexed { index, name ->
                                    FilterChip(selected = entry.theme == index, onClick = { update(entry.copy(theme = index)) }, label = { Text(name) })
                                }
                            }
                            Row(horizontalArrangement = Arrangement.spacedBy(6.dp)) {
                                listOf("🐰", "🐱", "🐻").forEach { character ->
                                    FilterChip(selected = entry.character == character, onClick = { update(entry.copy(character = character)) }, label = { Text(character) })
                                }
                            }
                        }
                    }
                }
                item {
                    Card(shape = RoundedCornerShape(20.dp)) {
                        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                            Text("홈 화면의 작은 친구", style = MaterialTheme.typography.titleMedium)
                            Text("위젯으로 오늘의 할 일과 습관을 확인해요.", style = MaterialTheme.typography.bodyMedium)
                            OutlinedButton(onClick = {
                                val manager = AppWidgetManager.getInstance(context)
                                val accepted = manager.isRequestPinAppWidgetSupported && manager.requestPinAppWidget(
                                    ComponentName(context, DiaryWidgetProvider::class.java), null, null)
                                Toast.makeText(context, if (accepted) "홈 화면의 위젯 추가 창에서 확인해 주세요" else "홈 화면을 길게 눌러 위젯 → 마이데이를 선택해 주세요", Toast.LENGTH_LONG).show()
                            }, modifier = Modifier.fillMaxWidth()) { Text("홈 화면에 일기 위젯 추가") }
                            Text("움직이는 친구를 배경화면으로 설정하면 홈 화면을 돌아다녀요. 캐릭터를 누르거나 끌어 보세요.", style = MaterialTheme.typography.bodyMedium)
                            Button(onClick = {
                                try {
                                    context.startActivity(Intent(WallpaperManager.ACTION_CHANGE_LIVE_WALLPAPER)
                                        .putExtra(WallpaperManager.EXTRA_LIVE_WALLPAPER_COMPONENT,
                                            ComponentName(context, CharacterWallpaperService::class.java)))
                                } catch (_: ActivityNotFoundException) {
                                    Toast.makeText(context, "휴대폰 배경화면 설정에서 마이데이 · 움직이는 친구를 선택해 주세요", Toast.LENGTH_LONG).show()
                                }
                            }, modifier = Modifier.fillMaxWidth()) { Text("움직이는 캐릭터 배경화면 설정") }
                            Text("오늘 날짜에서 고른 캐릭터와 배경색이 적용돼요. 시스템 미리보기에서 설정을 확정하세요.", style = MaterialTheme.typography.bodySmall)
                        }
                    }
                }
                if (entry.blocks.isEmpty()) item {
                    Column(Modifier.fillMaxWidth().background(Color.White.copy(alpha = 0.7f), RoundedCornerShape(20.dp)).padding(24.dp)) {
                        Text(entry.character, style = MaterialTheme.typography.displayMedium)
                        Text("아직 비어 있는 하루", style = MaterialTheme.typography.titleLarge)
                        Text("아래 버튼으로 첫 블록을 추가해 보세요.")
                    }
                }
                itemsIndexed(entry.blocks, key = { _, block -> block.id }) { index, block ->
                    Card(shape = RoundedCornerShape(20.dp), colors = CardDefaults.cardColors(containerColor = Color.White)) {
                        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                            Text(when (block.type) { "todo" -> "☑ 할 일"; "habit" -> "🌱 습관 체크"; "emotion" -> "${entry.character} 감정처리반"; else -> "✍ 오늘의 일기" },
                                style = MaterialTheme.typography.titleMedium)
                            if (block.type == "emotion") Text("판단 없이 들어줄게. 지금 마음을 들려줘.", style = MaterialTheme.typography.bodyMedium)
                            OutlinedTextField(value = block.text, onValueChange = { updateBlock(block.copy(text = it)) },
                                modifier = Modifier.fillMaxWidth(), minLines = if (block.type in listOf("text", "emotion")) 3 else 1,
                                label = { Text(when (block.type) { "todo" -> "해야 할 일"; "habit" -> "오늘 실천할 습관"; "emotion" -> "내 마음 기록"; else -> "오늘은 어떤 하루였나요?" }) })
                            if (block.type in listOf("todo", "habit")) {
                                Row {
                                    Checkbox(checked = block.checked, onCheckedChange = { updateBlock(block.copy(checked = it)) })
                                    Text(if (block.checked) "오늘 완료했어요" else "완료하면 체크해 주세요", modifier = Modifier.padding(top = 14.dp))
                                }
                            }
                            Row {
                                TextButton(onClick = { move(index, -1) }, enabled = index > 0) { Text("↑ 위로") }
                                TextButton(onClick = { move(index, 1) }, enabled = index < entry.blocks.lastIndex) { Text("↓ 아래로") }
                                Spacer(Modifier.weight(1f))
                                TextButton(onClick = { deleting = block.id }) { Text("삭제") }
                            }
                        }
                    }
                }
                item { Button(onClick = { showAdd = true }, modifier = Modifier.fillMaxWidth()) { Text("+ 블록 추가") } }
                item { Text("감정처리반은 감정을 적어 보관하는 공간입니다. AI 대화는 아직 제공하지 않습니다.", style = MaterialTheme.typography.bodySmall) }
            }
        }
        if (showAdd) AlertDialog(onDismissRequest = { showAdd = false }, title = { Text("어떤 블록을 넣을까요?") },
            text = { Column {
                listOf("text" to "✍ 글 일기", "todo" to "☑ 할 일", "habit" to "🌱 습관 체크", "emotion" to "${entry.character} 감정처리반").forEach { (type, title) ->
                    TextButton(onClick = { update(entry.copy(blocks = entry.blocks + DiaryBlock(type = type))); showAdd = false }, modifier = Modifier.fillMaxWidth()) { Text(title) }
                }
            } }, confirmButton = { TextButton(onClick = { showAdd = false }) { Text("닫기") } })
        if (deleting != null) AlertDialog(onDismissRequest = { deleting = null }, title = { Text("블록을 삭제할까요?") },
            text = { Text("이 블록에 작성한 내용도 함께 삭제됩니다.") },
            confirmButton = { TextButton(onClick = { update(entry.copy(blocks = entry.blocks.filterNot { it.id == deleting })); deleting = null }) { Text("삭제") } },
            dismissButton = { TextButton(onClick = { deleting = null }) { Text("취소") } })
    }
}
