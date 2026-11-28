package com.myday.diary.diary

import kotlin.math.*

/** Logical pixels, independent of WallpaperService. Mirrored by the native desktop pet. */
class PetMotion {
    var x=0.0;var y=0.0;var vx=0.0;var vy=0.0
    var held=false;private set
    var flying=false;private set
    var scaleX=1.0;private set
    var scaleY=1.0;private set
    private var tx=0.0;private var ty=0.0;private var sampledX=0.0;private var sampledY=0.0;private var sampledAt=0.0;private var movedAt=0.0
    var left=0.0;var top=0.0;var right=0.0;var bottom=0.0
    fun begin(now:Double){held=true;flying=false;vx=0.0;vy=0.0;tx=x;ty=y;sampledX=x;sampledY=y;sampledAt=now;movedAt=now}
    fun drag(px:Double,py:Double,now:Double){if(!held||!px.isFinite()||!py.isFinite())return;tx=px;ty=py;val dt=now-sampledAt;if(dt<.001)return;val dx=px-sampledX;val dy=py-sampledY;val blend=1-exp(-dt*24);vx+=((dx/dt).coerceIn(-12000.0,12000.0)-vx)*blend;vy+=((dy/dt).coerceIn(-12000.0,12000.0)-vy)*blend;if(abs(dx)+abs(dy)>.1)movedAt=now;sampledX=px;sampledY=py;sampledAt=now}
    fun release(now:Double){if(!held)return;held=false;val gain=exp(-max(0.0,now-sampledAt)*24)*SharedPet.PetThrowGainPercent/100;vx*=gain;vy*=gain;val speed=hypot(vx,vy);flying=now-movedAt<.12&&speed>=SharedPet.PetMinThrowSpeed;if(!flying){x=tx;y=ty;vx=0.0;vy=0.0}else if(speed>SharedPet.PetMaxThrowSpeed){vx*=SharedPet.PetMaxThrowSpeed/speed;vy*=SharedPet.PetMaxThrowSpeed/speed};constrain()}
    fun cancel(){held=false;flying=false;vx=0.0;vy=0.0;constrain()}
    fun constrain(){x=x.coerceIn(left,max(left,right));y=y.coerceIn(top,max(top,bottom))}
    fun step(seconds:Double){if(!seconds.isFinite()||seconds<=0)return;val dt=min(.1,seconds);var sx=1.0;var sy=1.0
        if(held){val f=1-exp(-dt*16);x+=(tx-x)*f;y+=(ty-y)*f;val amount=min(.75,hypot(vx,vy)/1800+hypot(tx-x,ty-y)/180);if(abs(vx)>abs(vy)){sx+=amount;sy-=amount*.4}else{sy+=amount;sx-=amount*.4}}
        else if(flying){var remaining=dt;while(remaining>1e-6){val h=min(1.0/120,remaining);remaining-=h;vx*=exp(-h*.42);vy+=SharedPet.PetGravity*h;x+=vx*h;y+=vy*h;val bounce=SharedPet.PetRestitutionPercent/100.0
            if(x<left&&vx<0||x>right&&vx>0){vx*= -bounce;sx=.7;sy=1.25}
            if(y<top&&vy<0){vy*= -bounce;sx=1.25;sy=.7}
            if(y>=bottom&&vy>=0){if(vy>150){vy*= -bounce;sx=1.3;sy=.7}else{vy=0.0;vx*=exp(-h*8)}};constrain();if(vy==0.0&&abs(vx)<24&&y>=bottom){flying=false;vx=0.0;vy=0.0;break}
        }}
        val blend=1-exp(-dt*28);scaleX+=((sx.coerceIn(.65,1.8))-scaleX)*blend;scaleY+=((sy.coerceIn(.65,1.8))-scaleY)*blend
    }
}
