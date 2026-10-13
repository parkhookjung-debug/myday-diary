package com.myday.diary.data

import android.content.Context
import android.graphics.*
import android.media.ExifInterface
import android.net.Uri
import android.util.Base64
import java.io.ByteArrayInputStream
import java.io.ByteArrayOutputStream
import java.util.WeakHashMap
import kotlin.math.max
import kotlin.math.roundToInt

object NotebookPhoto {
    const val MAX_INPUT=20*1024*1024
    const val MAX_BYTES=2*1024*1024
    const val MAX_CHARACTERS=((MAX_BYTES+2)/3)*4
    const val MAX_BOOK_CHARACTERS=64L*1024*1024
    private val validated=WeakHashMap<String,Boolean>()
    fun read(context: Context,uri: Uri): String {
        val bytes=context.contentResolver.openInputStream(uri)?.use {input->
            val output=ByteArrayOutputStream();val buffer=ByteArray(8192);var total=0
            while(true) {val count=input.read(buffer);if(count<0) break;total+=count;require(total<=MAX_INPUT) {"사진은 20MB 이하로 선택해 주세요."};output.write(buffer,0,count)};output.toByteArray()
        } ?: error("사진을 읽지 못했어요.")
        val info=BitmapFactory.Options().apply {inJustDecodeBounds=true};BitmapFactory.decodeByteArray(bytes,0,bytes.size,info)
        require(info.outWidth in 1..12000 && info.outHeight in 1..12000 && info.outWidth.toLong()*info.outHeight<=25000000) {"사진은 2,500만 화소 이하로 선택해 주세요."}
        var sample=1;while(max(info.outWidth,info.outHeight)/sample>3200) sample*=2
        val source=BitmapFactory.decodeByteArray(bytes,0,bytes.size,BitmapFactory.Options().apply {inSampleSize=sample}) ?: error("지원하지 않는 사진이에요.")
        val orientation=runCatching {ExifInterface(ByteArrayInputStream(bytes)).getAttributeInt(ExifInterface.TAG_ORIENTATION,1)}.getOrDefault(1)
        val matrix=Matrix().apply {when(orientation) {2->setScale(-1f,1f);3->setRotate(180f);4->{setRotate(180f);postScale(-1f,1f)};5->{setRotate(90f);postScale(-1f,1f)};6->setRotate(90f);7->{setRotate(270f);postScale(-1f,1f)};8->setRotate(270f)}}
        val rotated=Bitmap.createBitmap(source,0,0,source.width,source.height,matrix,true)
        val scale=minOf(1f,1600f/max(rotated.width,rotated.height))
        val target=Bitmap.createBitmap(max(1,(rotated.width*scale).roundToInt()),max(1,(rotated.height*scale).roundToInt()),Bitmap.Config.ARGB_8888)
        try {
            Canvas(target).apply {drawColor(Color.WHITE);drawBitmap(rotated,null,Rect(0,0,target.width,target.height),Paint(Paint.ANTI_ALIAS_FLAG or Paint.FILTER_BITMAP_FLAG))}
            val output=ByteArrayOutputStream();require(target.compress(Bitmap.CompressFormat.JPEG,85,output));require(output.size()<=MAX_BYTES) {"사진 용량이 너무 커요."}
            return Base64.encodeToString(output.toByteArray(),Base64.NO_WRAP)
        } finally {target.recycle();if(rotated!==source) rotated.recycle();source.recycle()}
    }
    @Synchronized fun validate(book: DiaryBook) {
        var total=0L
        book.days.values.flatMap {it.blocks}.forEach {b->b.photo?.let {data->
            total+=data.length;require(total<=MAX_BOOK_CHARACTERS) {"전체 사진 저장 공간이 가득 찼어요."}
            require(data.length<=MAX_CHARACTERS)
            if(validated[data]!=true) {
                val bytes=Base64.decode(data,Base64.DEFAULT);require(bytes.size in 3..MAX_BYTES && bytes[0]==(-1).toByte() && bytes[1]==(-40).toByte() && bytes[2]==(-1).toByte())
                val info=BitmapFactory.Options().apply {inJustDecodeBounds=true};BitmapFactory.decodeByteArray(bytes,0,bytes.size,info)
                require(info.outWidth in 1..1600 && info.outHeight in 1..1600)
                val bitmap=BitmapFactory.decodeByteArray(bytes,0,bytes.size,BitmapFactory.Options().apply {inSampleSize=4}) ?: error("사진 데이터가 손상됐어요.")
                bitmap.recycle();validated[data]=true
            }
        }}
    }
}
