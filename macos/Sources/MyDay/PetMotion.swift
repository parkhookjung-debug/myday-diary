import Foundation
import Combine

@MainActor final class PetMotion:ObservableObject {
    var x=0.0,y=0.0,vx=0.0,vy=0.0
    var held=false,flying=false
    @Published var scaleX=1.0
    @Published var scaleY=1.0
    @Published var moving=false
    var left=0.0,top=0.0,right=0.0,bottom=0.0
    private var tx=0.0,ty=0.0,sampledX=0.0,sampledY=0.0,sampledAt=0.0,movedAt=0.0
    func begin(_ now:Double){held=true;flying=false;vx=0;vy=0;tx=x;ty=y;sampledX=x;sampledY=y;sampledAt=now;movedAt=now}
    func drag(_ px:Double,_ py:Double,_ now:Double){guard held,px.isFinite,py.isFinite else{return};tx=px;ty=py;let dt=now-sampledAt;guard dt>=0.001 else{return};let dx=px-sampledX,dy=py-sampledY,blend=1-exp(-dt*24);vx+=(max(-12000,min(12000,dx/dt))-vx)*blend;vy+=(max(-12000,min(12000,dy/dt))-vy)*blend;if abs(dx)+abs(dy)>0.1{movedAt=now};sampledX=px;sampledY=py;sampledAt=now}
    func release(_ now:Double){guard held else{return};held=false;let gain=exp(-max(0,now-sampledAt)*24)*Double(DesktopContract.petThrowGainPercent)/100;vx*=gain;vy*=gain;let speed=hypot(vx,vy);flying=now-movedAt<0.12 && speed>=Double(DesktopContract.petMinThrowSpeed);if !flying{x=tx;y=ty;vx=0;vy=0}else if speed>Double(DesktopContract.petMaxThrowSpeed){vx*=Double(DesktopContract.petMaxThrowSpeed)/speed;vy*=Double(DesktopContract.petMaxThrowSpeed)/speed};constrain()}
    func cancel(){held=false;flying=false;vx=0;vy=0;constrain()}
    func constrain(){x=max(left,min(right,x));y=max(top,min(bottom,y))}
    func step(_ seconds:Double){guard seconds.isFinite,seconds>0 else{return};let dt=min(0.1,seconds);var sx=1.0,sy=1.0
        if held{let f=1-exp(-dt*16);x+=(tx-x)*f;y+=(ty-y)*f;let amount=min(0.75,hypot(vx,vy)/1800+hypot(tx-x,ty-y)/180);if abs(vx)>abs(vy){sx+=amount;sy-=amount*0.4}else{sy+=amount;sx-=amount*0.4}}
        else if flying{var remaining=dt;while remaining>0.000001{let h=min(1.0/120,remaining);remaining-=h;vx*=exp(-h*0.42);vy+=Double(DesktopContract.petGravity)*h;x+=vx*h;y+=vy*h;let bounce=Double(DesktopContract.petRestitutionPercent)/100
            if x<left && vx<0 || x>right && vx>0{vx*=(-bounce);sx=0.7;sy=1.25}
            if y<top && vy<0{vy*=(-bounce);sx=1.25;sy=0.7}
            if y>=bottom && vy>=0{if vy>150{vy*=(-bounce);sx=1.3;sy=0.7}else{vy=0;vx*=exp(-h*8)}};constrain();if vy==0 && abs(vx)<24 && y>=bottom{flying=false;vx=0;vy=0;break}
        }}
        let blend=1-exp(-dt*28);scaleX+=(max(0.65,min(1.8,sx))-scaleX)*blend;scaleY+=(max(0.65,min(1.8,sy))-scaleY)*blend
    }
}
