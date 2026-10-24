package com.myday.diary.ui.diary

import android.graphics.BitmapFactory
import android.util.Base64
import androidx.compose.foundation.Image
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.grid.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.unit.dp
import com.myday.diary.data.*
import java.time.LocalDate
import java.time.YearMonth
import kotlinx.coroutines.*

@Composable fun PhonePhotoAlbum(book: DiaryBook,date: LocalDate,onSelect: (LocalDate)->Unit) {
    var month by remember{mutableStateOf(YearMonth.from(date))}
    val photos=remember(book,month){book.days.filterKeys{it.startsWith(month.toString()+"-")}.toSortedMap(reverseOrder()).flatMap{(key,entry)->entry.blocks.filter{it.type=="photo" && it.photo!=null}.map{key to it}}}
    Column(Modifier.fillMaxSize().padding(16.dp)) {
        Row(Modifier.fillMaxWidth(),horizontalArrangement=Arrangement.SpaceBetween){TextButton({month=month.minusMonths(1)},enabled=month.year>1 || month.monthValue>1){Text("‹")};Text("$month · 사진 ${photos.size}장",style=MaterialTheme.typography.titleMedium);TextButton({month=month.plusMonths(1)},enabled=month.year<9999 || month.monthValue<12){Text("›")}}
        if(photos.isEmpty())Text("이번 달은 아직 사진이 없어요. 일기에 사진을 추가해 보세요.",Modifier.padding(24.dp))
        LazyVerticalGrid(GridCells.Adaptive(140.dp),Modifier.weight(1f).testTag("photo-album-grid"),horizontalArrangement=Arrangement.spacedBy(10.dp),verticalArrangement=Arrangement.spacedBy(10.dp)){
            items(photos,key={it.first+"/"+it.second.id}){(key,block)->Card(Modifier.clickable{onSelect(LocalDate.parse(key))}.testTag("album-${block.id}")){Column(Modifier.padding(8.dp)){
                val thumbnail by produceState<ImageBitmap?>(null,block.photo){value=withContext(Dispatchers.IO){runCatching{val bytes=Base64.decode(block.photo,Base64.DEFAULT);val info=BitmapFactory.Options().apply{inJustDecodeBounds=true};BitmapFactory.decodeByteArray(bytes,0,bytes.size,info);var sample=1;while(maxOf(info.outWidth,info.outHeight)/sample>320)sample*=2;BitmapFactory.decodeByteArray(bytes,0,bytes.size,BitmapFactory.Options().apply{inSampleSize=sample})?.asImageBitmap()}.getOrNull()}}
                thumbnail?.let{Image(it,"$key 일기의 사진",Modifier.fillMaxWidth().height(145.dp),contentScale=ContentScale.Crop)} ?: Box(Modifier.height(145.dp)){Text("사진을 준비하고 있어요")}
                Text(key,style=MaterialTheme.typography.labelMedium);if(block.text.isNotBlank())Text(block.text,maxLines=2,style=MaterialTheme.typography.bodySmall)
            }}}
        }
    }
}
