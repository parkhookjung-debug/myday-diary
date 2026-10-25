package com.myday.diary.ui.character

import android.content.Context
import android.graphics.*
import android.util.LruCache
import com.myday.diary.data.*
import kotlin.math.*

/** Assets are generated from the actual Windows vector renderer, never third-party art. */
class SangmonSprites(context: Context) {
    private val context=context.applicationContext
    companion object {private val images=object: LruCache<String,Bitmap>(48*1024) {override fun sizeOf(key: String,value: Bitmap)=value.byteCount/1024}}
    private val paint=Paint(Paint.ANTI_ALIAS_FLAG or Paint.FILTER_BITMAP_FLAG)
    fun image(path: String): Bitmap = images[path] ?: context.assets.open(path).use {BitmapFactory.decodeStream(it) ?: error("캐릭터 그림을 읽지 못했어요.")}.also {images.put(path,it)}
    fun draw(canvas: Canvas,id: String,equipment: Equipment,x: Float,y: Float,radius: Float,time: Long,fireAge: Long=-1,moving: Boolean=false,facingLeft: Boolean=true,dragging: Boolean=false,sleepy: Boolean=false) {
        val safe=id.takeIf {it.matches(Regex("[a-z-]+"))} ?: "original"
        val atlas=runCatching {image("sangmon/$safe.png")}.getOrElse {image("sangmon/original.png")}
        val yawn=time%42000 in 30000..31599
        val phase=time%42000
        val row=if(dragging) 7 else if(fireAge in 0..699) 2 else if(sleepy) 6 else if(yawn) 3 else if(moving && phase in 10000..11999) 4 else if(phase in 16000..17999) 5 else if(phase in 34000..36399) 6 else if(moving) 1 else 0
        val frame=when(row) {2->(fireAge/100).toInt();3->((phase-30000)/200).toInt();1->((time%800)/100).toInt();4->((time%650)*8/650).toInt();5,6,7->((time%2400)/300).toInt();else->((time%4200)/525).toInt()}.coerceIn(0,7)
        val saved=canvas.save();if(!facingLeft) canvas.scale(-1f,1f,x,y)
        if(listOf("head","back","aura").any {equipment.get(it)!="none"})canvas.scale(.9f,.9f,x,y-5f/96*radius)
        val age=when(row) {0->floatArrayOf(0f,.3f,.8f,1.2f,2.4f,3.2f,3.86f,4.05f)[frame];1,2->frame*.1f;3->frame*.2f;4->frame*.65f/8;else->frame*.3f}
        fun overlay(slot: String,body: Boolean) {
            val item=equipment.get(slot);if(item=="none" || EquipmentCatalog.items[item]!=slot)return
            val state=canvas.save();val unit=radius/96f*172/555
            val stride=sin(age*PI*2/.8).toFloat();val breath=sin(age*PI*2/2.4).toFloat();val leap=abs(sin(age*PI/.65)).toFloat()
            val mouth=if(row==3 && age<1.4f)sin(age/1.4*PI).toFloat() else 0f
            val squash=if(row==4)cos(age*PI*2/.65).toFloat()*.08f else if(row==1)stride*.045f else breath*.025f
            val bob=when(row) {7->0f;4->-leap*11;1,2->-abs(stride)*4;else->breath*1.5f}
            canvas.translate(0f,bob*5.8f*unit)
            val index=SangmonIdentity.levels.keys.indexOf(safe)
            val shrink=if(safe=="mini") .82f else if(safe=="winged" || index>=8) .86f else 1f
            canvas.scale(shrink,shrink,x,y+(35*172f/555-5)/96*radius)
            if(body) {
                val pivotY=y+(115*172f/555-5)/96*radius
                val tilt=when(row) {7->7f;4->-leap*5;1->stride*3;5->sin(age*2)*3;6->-5f;3->-mouth*4;else->breath*1.5f}+(if(safe=="speedy") -8 else 0)
                var sy=when(row) {7->1.08f;6->.91f+breath*.018f;else->1+squash+mouth*.045f}
                var sx=when(row) {7->.94f;6->1.08f;else->1-squash*.65f}
                if(safe=="speedy"){sx*=1.05f;sy*=.9f}
                if(safe=="original"){if(row==6){sx*=1.06f;sy*=.76f};if(row==7)sy*=1.1f}
                canvas.rotate(tilt,x,pivotY);canvas.scale(sx,sy,x,pivotY)
            }
            runCatching {canvas.drawBitmap(image("equipment/$item.png"),null,RectF(x-radius,y-radius,x+radius,y+radius),paint)}
            canvas.restoreToCount(state)
        }
        listOf("ground","aura","back").forEach {overlay(it,true)}
        canvas.drawBitmap(atlas,Rect(frame*192,row*192,(frame+1)*192,(row+1)*192),RectF(x-radius,y-radius,x+radius,y+radius),paint)
        listOf("neck","head","face").forEach {overlay(it,true)}
        listOf("weapon","charm").forEach {overlay(it,false)}
        canvas.restoreToCount(saved)
    }
}
