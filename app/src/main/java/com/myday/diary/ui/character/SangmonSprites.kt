package com.myday.diary.ui.character

import android.content.Context
import android.graphics.*
import android.util.LruCache
import com.myday.diary.data.Equipment

/** Assets are generated from the actual Windows vector renderer, never third-party art. */
class SangmonSprites(context: Context) {
    private val context=context.applicationContext
    companion object {private val images=object: LruCache<String,Bitmap>(48*1024) {override fun sizeOf(key: String,value: Bitmap)=value.byteCount/1024}}
    private val paint=Paint(Paint.ANTI_ALIAS_FLAG or Paint.FILTER_BITMAP_FLAG)
    fun image(path: String): Bitmap = images[path] ?: context.assets.open(path).use {BitmapFactory.decodeStream(it) ?: error("캐릭터 그림을 읽지 못했어요.")}.also {images.put(path,it)}
    fun draw(canvas: Canvas,id: String,equipment: Equipment,x: Float,y: Float,radius: Float,time: Long,fireAge: Long=-1,moving: Boolean=false,facingLeft: Boolean=true,dragging: Boolean=false) {
        val safe=id.takeIf {it.matches(Regex("[a-z-]+"))} ?: "original"
        val atlas=runCatching {image("sangmon/$safe.png")}.getOrElse {image("sangmon/original.png")}
        val yawn=time%42000 in 30000..31599
        val phase=time%42000
        val row=if(dragging) 7 else if(fireAge in 0..699) 2 else if(yawn) 3 else if(moving && phase in 10000..11999) 4 else if(phase in 16000..17999) 5 else if(phase in 34000..36399) 6 else if(moving) 1 else 0
        val frame=when(row) {2->(fireAge/100).toInt();3->((phase-30000)/200).toInt();1->((time%800)/100).toInt();4->((time%650)*8/650).toInt();5,6,7->((time%2400)/300).toInt();else->((time%4200)/525).toInt()}.coerceIn(0,7)
        val saved=canvas.save();if(!facingLeft) canvas.scale(-1f,1f,x,y)
        canvas.drawBitmap(atlas,Rect(frame*192,row*192,(frame+1)*192,(row+1)*192),RectF(x-radius,y-radius,x+radius,y+radius),paint)
        if(equipment.weaponId!="none") runCatching {canvas.drawBitmap(image("items/${equipment.weaponId}.png"),null,RectF(x+radius*.32f,y-radius*.75f,x+radius*1.06f,y+radius*.95f),paint)}
        if(equipment.charmId!="none") runCatching {canvas.drawBitmap(image("items/${equipment.charmId}.png"),null,RectF(x-radius*.96f,y-radius*.25f,x-radius*.4f,y+radius*.42f),paint)}
        canvas.restoreToCount(saved)
    }
}
