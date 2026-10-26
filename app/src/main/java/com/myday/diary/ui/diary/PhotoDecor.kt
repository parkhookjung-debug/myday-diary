package com.myday.diary.ui.diary
import android.graphics.*
import android.util.Base64
import java.io.ByteArrayOutputStream
import kotlin.math.*
object PhotoDecor {
 val colors=intArrayOf(Color.rgb(240,224,213),Color.rgb(219,210,236),Color.rgb(220,231,211))
 fun decode(value:String):Bitmap {
  require(value.length<=com.myday.diary.data.NotebookPhoto.MAX_CHARACTERS)
  val bytes=Base64.decode(value,Base64.DEFAULT);require(bytes.size<=com.myday.diary.data.NotebookPhoto.MAX_BYTES)
  val bounds=BitmapFactory.Options().apply{inJustDecodeBounds=true};BitmapFactory.decodeByteArray(bytes,0,bytes.size,bounds)
  require(bounds.outWidth in 1..1600&&bounds.outHeight in 1..1600)
  val source=requireNotNull(BitmapFactory.decodeByteArray(bytes,0,bytes.size))
  try{return requireNotNull(source.copy(Bitmap.Config.ARGB_8888,true))}finally{source.recycle()}
 }
 fun encode(image:Bitmap):String {val ratio=min(1f,1600f/max(image.width,image.height));val b=Bitmap.createBitmap(max(1,(image.width*ratio).toInt()),max(1,(image.height*ratio).toInt()),Bitmap.Config.ARGB_8888);try{Canvas(b).apply{drawColor(Color.WHITE);drawBitmap(image,null,Rect(0,0,b.width,b.height),Paint(3))};val out=ByteArrayOutputStream();check(b.compress(Bitmap.CompressFormat.JPEG,88,out));require(out.size()<=2*1024*1024);return Base64.encodeToString(out.toByteArray(),Base64.NO_WRAP)}finally{b.recycle()}}
 fun crop(image:Bitmap,ratio:Double):Bitmap {var w=image.width;var h=image.height;if(w.toDouble()/h>ratio)w=max(1,(h*ratio).toInt())else h=max(1,(w/ratio).toInt());return Bitmap.createBitmap(image,(image.width-w)/2,(image.height-h)/2,w,h)}
 fun rotate(image:Bitmap,degrees:Float):Bitmap=Bitmap.createBitmap(image,0,0,image.width,image.height,Matrix().apply{postRotate(degrees)},true)
 fun frame(image:Bitmap,color:Int):Bitmap {val edge=max(6,min(image.width,image.height)/16);return Bitmap.createBitmap(image.width+edge*2,image.height+edge*2,Bitmap.Config.ARGB_8888).also{Canvas(it).apply{drawColor(colors[color]);drawBitmap(image,edge.toFloat(),edge.toFloat(),Paint(3))}}.let{if(max(it.width,it.height)<=1600)it else{val encoded=encode(it);it.recycle();decode(encoded)}}}
 fun stamp(canvas:Canvas,kind:Int,x:Float,y:Float,side:Float){canvas.save();canvas.translate(x-side/2,y-side/2);canvas.scale(side/100,side/100);val p=Paint(3);when(kind){0->{p.color=Color.rgb(227,156,168);repeat(6){val a=it*Math.PI/3;canvas.drawOval(32+cos(a).toFloat()*23,32+sin(a).toFloat()*23,68+cos(a).toFloat()*23,68+sin(a).toFloat()*23,p)};p.color=Color.rgb(239,205,125);canvas.drawCircle(50f,50f,15f,p)};1->{p.color=Color.rgb(227,156,168);val shape=Path().apply{moveTo(50f,85f);cubicTo(0f,50f,2f,10f,28f,14f);cubicTo(42f,14f,49f,24f,50f,29f);cubicTo(55f,4f,87f,6f,92f,30f);cubicTo(100f,50f,70f,73f,50f,85f);close()};canvas.drawPath(shape,p)};2->{p.color=Color.rgb(239,205,125);val shape=Path();repeat(10){val a=it*Math.PI/5-Math.PI/2;val r=if(it%2==0)44 else 20;val xx=50+cos(a).toFloat()*r;val yy=50+sin(a).toFloat()*r;if(it==0)shape.moveTo(xx,yy)else shape.lineTo(xx,yy)};shape.close();canvas.drawPath(shape,p)};else->{canvas.rotate(-8f);p.color=Color.rgb(122,148,110);canvas.drawRect(8f,35f,88f,65f,p);p.color=Color.rgb(221,231,207);p.strokeWidth=3f;for(i in 15..85 step 15)canvas.drawLine(i.toFloat(),36f,i+9f,64f,p)}};canvas.restore()}
 fun sticker(kind:Int):Bitmap=Bitmap.createBitmap(320,240,Bitmap.Config.ARGB_8888).also{Canvas(it).apply{drawColor(Color.rgb(250,248,243));stamp(this,kind,160f,120f,200f)}}
}
