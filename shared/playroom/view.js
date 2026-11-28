/* Bundled UI only. A native bridge owns persistent scores and explicit PNG export. */
(function(){
 'use strict';
 const G=MyDayGames,$=id=>document.getElementById(id),canvas=$('stage'),ctx=canvas.getContext('2d');
 const titles=['간식 산책','상몬 슬링샷','느긋한 낚시','통통 점프','불꽃 요리','상몬 블록 쌓기','상몬 블록 퍼즐'];
 const descriptions=['간식은 먹고 돌과 가시는 피해요','상몬을 당겨 과녁으로 날려요','입질을 기다려 초록 칸에 맞춰요','장애물을 넘고 간식을 모아요','꾹 누르고 알맞을 때 놓아요','상몬 블록을 돌려 한 줄을 채워요','8×8 보드에 가로·세로줄을 채워요'];
 const colors=['#b9d7d0','#e1aaba','#c5b5df','#ebd290','#a7cba8','#a9c8e4','#e3b59e'];
 let game=null,id='',key='',round='',reported=false,records={},axis=0,previous=0,selected=0,hover=null,held=null,heldClock=0,daily=false,initialized=false;
 let atlas=new Image();
 const pending=new Map();
 let confirmation=null;
 function ask(message,work){const wasPlaying=game&&game.state==='playing';pause();confirmation={work,wasPlaying,focus:document.activeElement};$('confirmation-text').textContent=message;$('confirmation').classList.remove('hidden');$('confirmation-cancel').focus();}
 function answer(yes){if(!confirmation)return;const task=confirmation;confirmation=null;$('confirmation').classList.add('hidden');if(yes)task.work();else{if(task.wasPlaying)resume();if(task.focus)task.focus.focus();}}
 $('confirmation-ok').onclick=()=>answer(true);$('confirmation-cancel').onclick=()=>answer(false);
 function bridge(message){const text=JSON.stringify(message);if(window.MyDayNative)window.MyDayNative.post(text);else if(window.webkit&&window.webkit.messageHandlers.myday)window.webkit.messageHandlers.myday.postMessage(text);else $('status').textContent='앱 안에서 열면 점수와 이미지를 저장할 수 있어요.';}
 window.MyDayPlayroom={
   initialize(data){initialized=true;records=data.records||{};if(data.atlas)atlas.src=data.atlas;cards();},
   pause(){pause();},
   reply(token,ok,message){const action=pending.get(token);if(!action)return;pending.delete(token);$('status').textContent=message||'';if(ok&&action.kind==='score'){records[action.key]=Math.max(records[action.key]||0,action.score);best();cards();}},
   snapshot(){return {id,state:game?game.state:'hub',score:game?game.score:0};}
 };
 function today(){const d=new Date();return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;}
 function cards(){const parent=$('cards');parent.textContent='';G.ids.forEach((value,i)=>{const button=document.createElement('button');button.className='card';const strong=document.createElement('strong');strong.textContent=titles[i];const detail=document.createElement('span');detail.textContent=descriptions[i];const score=document.createElement('span');score.textContent=`최고 ${records[value]||0}점`;button.append(strong,detail,score);button.onclick=()=>open(value,false);parent.append(button);});const button=document.createElement('button');button.className='card';const strong=document.createElement('strong');strong.textContent='오늘의 간식 도전';const detail=document.createElement('span');detail.textContent=`${today()} · 같은 날은 같은 순서`;button.append(strong,detail);button.onclick=()=>open('snack',true);parent.append(button);const toy=document.createElement("button");toy.className="card";toy.textContent="상몬과 공놀이 · 잡고 던져보기";toy.onclick=()=>open("fetch",false);parent.append(toy);}
 function open(value,isDaily){id=value;daily=isDaily;key=daily?'snack-daily-v1:'+today():id;const bytes=new Uint32Array(1);if(window.crypto&&crypto.getRandomValues)crypto.getRandomValues(bytes);else bytes[0]=Date.now();game=G.create(id,daily?G.dailySeed(today()):bytes[0]);round=Date.now().toString(36)+'-'+bytes[0].toString(36);reported=false;axis=0;selected=0;hover=null;held=null;$('hub').classList.add('hidden');$('game').classList.remove('hidden');$('intro').textContent=id==='fetch'?'공을 당겨 던지면 상몬이 가져와요':descriptions[G.ids.indexOf(id)];$('title').textContent=daily?'오늘의 간식 도전':id==='fetch'?'상몬과 공놀이':titles[G.ids.indexOf(id)];$('veil').classList.add('hidden');$('export').disabled=true;$('pause').textContent='일시 정지';canvas.classList.toggle('tall',id==='blocks');canvas.width=window.innerWidth<600&&['blocks','block-puzzle'].includes(id)?480:960;canvas.height=canvas.width===480?(id==='blocks'?720:630):600;$('status').textContent='';controls();best();}
 function best(){$('best').textContent=`최고 ${records[key]||0}점`;}
 function pause(){axis=0;held=null;hover=null;if(!game||game.state!=='playing')return;game.pause();$('pause').textContent='계속';veil('잠깐 쉬는 중','계속 버튼을 누르면 이어서 할 수 있어요.');}
 function resume(){if(!game||game.state!=='paused')return;game.resume();$('veil').classList.add('hidden');$('pause').textContent='일시 정지';previous=0;}
 function veil(title,text){const v=$('veil');v.textContent='';const h=document.createElement('h2');h.textContent=title;const p=document.createElement('p');p.textContent=text;v.append(h,p);if(game.state==='paused'){const b=document.createElement('button');b.textContent='계속';b.className='primary';b.onclick=resume;v.append(b);}v.classList.remove('hidden');}
 function finish(){if(reported)return;reported=true;axis=0;held=null;$('export').disabled=false;veil('오늘도 즐거웠어요',`${game.score}점 · 최고 ${Math.max(records[key]||0,game.score)}점`);const token=round+'-score';pending.set(token,{kind:'score',key,score:game.score});bridge({type:'score',token,round,id:key,score:game.score});}
 function controls(){const p=$('controls');p.textContent='';const button=(title,down,up,repeat=false)=>{const b=document.createElement('button');b.textContent=title;b.onpointerdown=e=>{e.preventDefault();b.setPointerCapture(e.pointerId);down();bridge({type:"haptic"});if(repeat){held=down;heldClock=-.18;}};b.onpointerup=e=>{e.preventDefault();held=null;if(up)up();};b.onpointercancel=()=>{held=null;axis=0;if(game)game.cancel();};p.append(b);};
   if(id==='snack'){button('◀',()=>axis=-1,()=>axis=0);button('▶',()=>axis=1,()=>axis=0);}
   if(id==='fish'||id==='jump')button(id==='fish'?'낚시하기':'점프!',()=>game.action());
   if(id==='bake')button('꾹 눌러 불 뿜기',()=>game.begin(),()=>game.release());
   if(id==='blocks'){button('◀',()=>game.move(-1),null,true);button('↺',()=>game.rotate(-1));button('↻',()=>game.rotate(1));button('▶',()=>game.move(1),null,true);button('▼',()=>game.soft(),null,true);button('바로 내리기',()=>game.hard());}
   if(id==='block-puzzle'){for(let i=0;i<3;i++)button('블록 '+(i+1),()=>selected=i);}
 }
 function point(e){const r=canvas.getBoundingClientRect();/* object-fit letterboxing for the tall block board */const scale=Math.min(r.width/canvas.width,r.height/canvas.height),w=canvas.width*scale,h=canvas.height*scale;return [(e.clientX-r.left-(r.width-w)/2)/scale,(e.clientY-r.top-(r.height-h)/2)/scale];}
 canvas.onpointerdown=e=>{if(!game||game.state!=='playing')return;e.preventDefault();canvas.setPointerCapture(e.pointerId);bridge({type:"haptic"});const[x,y]=point(e);if(id==='snack')game.aim(x);else if(id==='sling'||id==='fetch')game.begin(x,y);else if(id==='bake')game.begin();else if(id==='fish'||id==='jump')game.action();else if(id==='block-puzzle'){if(y>=480){selected=Math.max(0,Math.min(2,Math.floor((x-(canvas.width===480?4:180))/(canvas.width===480?158:210))));}else{hover=[Math.floor((x-(canvas.width===480?32:272))/52),Math.floor((y-42)/52)];}}}; 
 canvas.onpointermove=e=>{if(!game||game.state!=='playing')return;const[x,y]=point(e);if(id==='snack')game.aim(x);if((id==='sling'&&game.aiming)||(id==='fetch'&&game.held))game.aim(x,y);if(id==='block-puzzle')hover=[Math.floor((x-(canvas.width===480?32:272))/52),Math.floor((y-42)/52)];};
 canvas.onpointerup=e=>{if(!game||game.state!=='playing')return;const[x,y]=point(e);if(id==='sling'||id==='fetch')game.release();if(id==='bake')game.release();if(id==='block-puzzle'&&y<480){game.place(selected,Math.floor((x-(canvas.width===480?32:272))/52),Math.floor((y-42)/52));if(!game.hand[selected])selected=Math.max(0,game.hand.findIndex(Boolean));hover=null;}};
 canvas.onpointercancel=()=>{if(game)game.cancel();hover=null;axis=0;};
 window.addEventListener('keydown',e=>{if(confirmation){if(e.key==='Escape'){e.preventDefault();answer(false);}else if(e.key==='Tab'){e.preventDefault();(document.activeElement===$('confirmation-cancel')?$('confirmation-ok'):$('confirmation-cancel')).focus();}return;}if(!game||/INPUT|TEXTAREA/.test(document.activeElement.tagName))return;if(['ArrowLeft','ArrowRight','ArrowUp','ArrowDown',' ','z','x','p','Escape'].includes(e.key))e.preventDefault();if(e.key==='p'||e.key==='Escape'){if(!e.repeat)(game.state==='paused'?resume:pause)();return;}if(game.state!=='playing')return;
   if(id==='snack'){if(e.key==='ArrowLeft')axis=-1;if(e.key==='ArrowRight')axis=1;}
   if(id==='blocks'){if(e.key==='ArrowLeft')game.move(-1);if(e.key==='ArrowRight')game.move(1);if(e.key==='ArrowDown')game.soft();if(!e.repeat){if(e.key==='ArrowUp'||e.key==='x')game.rotate(1);if(e.key==='z')game.rotate(-1);if(e.key===' ')game.hard();}}
   if(e.key===' '&&!e.repeat){if(id==='jump'||id==='fish')game.action();if(id==='bake')game.begin();}
 });
 window.addEventListener('keyup',e=>{if(e.key==='ArrowLeft'||e.key==='ArrowRight')axis=0;if(e.key===' '&&game&&id==='bake')game.release();});
 window.addEventListener('blur',pause);document.addEventListener('visibilitychange',()=>{if(document.hidden)pause();});
 $('pause').onclick=()=>game.state==='paused'?resume():pause();
 $('restart').onclick=()=>{const restart=()=>open(id,daily);if(game.state==='finished')restart();else ask('점수를 초기화하고 다시 시작할까요?',restart);};
 $('back').onclick=()=>{const close=()=>{game=null;axis=0;$('hub').classList.remove('hidden');$('game').classList.add('hidden');$('intro').textContent='잠깐 쉬어가는 일곱 가지 놀이. 기록과 경험치는 그대로예요.';};if(game.state==='finished')close();else ask('진행 중인 게임을 닫을까요?',close);};
 $('export').onclick=()=>{if(!game||game.state!=='finished')return;const output=document.createElement('canvas');output.width=960;output.height=720;const c=output.getContext('2d');c.fillStyle='#f7f5ee';c.fillRect(0,0,960,720);const scale=Math.min(960/canvas.width,600/canvas.height);c.drawImage(canvas,(960-canvas.width*scale)/2,(600-canvas.height*scale)/2,canvas.width*scale,canvas.height*scale);c.fillStyle='#343b30';c.font='bold 28px sans-serif';c.fillText('상몬 · '+$('title').textContent,30,645);c.font='24px sans-serif';c.fillText(`${game.score}점 · ${today()}`,30,688);const token=round+'-image-'+Date.now();pending.set(token,{kind:'image'});bridge({type:'image',token,png:output.toDataURL('image/png').split(',')[1]});};
 function rect(x,y,w,h,color,r=12){ctx.fillStyle=color;ctx.beginPath();ctx.moveTo(x+r,y);ctx.lineTo(x+w-r,y);ctx.quadraticCurveTo(x+w,y,x+w,y+r);ctx.lineTo(x+w,y+h-r);ctx.quadraticCurveTo(x+w,y+h,x+w-r,y+h);ctx.lineTo(x+r,y+h);ctx.quadraticCurveTo(x,y+h,x,y+h-r);ctx.lineTo(x,y+r);ctx.quadraticCurveTo(x,y,x+r,y);ctx.fill();}
 function text(value,x,y,size=22,color='#47583a'){ctx.fillStyle=color;ctx.font=`${size}px system-ui,sans-serif`;ctx.fillText(value,x,y);}
 function circle(x,y,r,color){ctx.fillStyle=color;ctx.beginPath();ctx.arc(x,y,r,0,Math.PI*2);ctx.fill();}
 function pet(x,y,size=140,row=0){if(atlas.complete&&atlas.naturalWidth){ctx.drawImage(atlas,Math.floor(performance.now()/160)%8*192,row*192,192,192,x-size/2,y-size/2,size,size);}else{rect(x-size*.4,y-size*.24,size*.8,size*.55,'#fff');circle(x-size*.17,y-size*.22,size*.12,'#fff');circle(x+size*.04,y-size*.22,size*.12,'#fff');circle(x-size*.17,y-size*.24,4,'#343b30');circle(x+size*.04,y-size*.24,4,'#343b30');}}
 function cell(x,y,size,value,alpha=1){ctx.globalAlpha=alpha;rect(x+1,y+1,size-2,size-2,colors[(value-1)%colors.length],size*.18);/* eyes keep the blocks recognizably Sangmon */circle(x+size*.35,y+size*.3,Math.max(1.2,size*.045),'#3d4935');circle(x+size*.58,y+size*.3,Math.max(1.2,size*.045),'#3d4935');ctx.globalAlpha=1;}
 function scene(){ctx.clearRect(0,0,canvas.width,canvas.height);ctx.fillStyle='#fcfbf5';ctx.fillRect(0,0,canvas.width,canvas.height);if(!game)return;
   if(id==='fetch'){rect(20,550,920,20,'#dce5ca');pet(game.pet.x,game.pet.y,140,game.phase==='chasing'||game.phase==='returning'?1:0);ctx.save();ctx.translate(game.ball.x,game.ball.y);ctx.scale(game.scaleX,game.scaleY);circle(0,0,18,'#d6ac80');circle(-5,-5,5,'#f7e7d0');ctx.restore();text('공을 잡고 던져요',35,90);}
   if(id==='snack'){rect(20,530,920,50,'#e1eacb');for(const d of game.drops){circle(d.x,d.y,20,d.kind<3?['#e78a75','#dba0b6','#d6b885'][d.kind]:'#8b9383');text(d.kind<3?['●','♥','✦'][d.kind]:['◆','✹'][d.kind-3],d.x-11,d.y+8,25,d.kind<3?'#fff':'#343b30');}pet(game.x,490,145,axis?1:0);}
   if(id==='sling'){rect(35,550,890,15,'#d5dfbd');circle(...game.target,44,'#e1bd9a');circle(...game.target,18,'#f9edcf');if(game.aiming){ctx.strokeStyle='#87965a';ctx.lineWidth=8;ctx.beginPath();ctx.moveTo(225,450);ctx.lineTo(game.x,game.y);ctx.stroke();}pet(game.x,game.y,120,game.aiming?7:0);text('당기기 → 놓기',60,100);}
   if(id==='fish'){rect(30,300,900,250,'#cee2df');pet(220,280,180);ctx.strokeStyle='#71885b';ctx.lineWidth=4;ctx.beginPath();ctx.moveTo(265,240);ctx.lineTo(440,170);ctx.lineTo(600,365);ctx.stroke();circle(600,370+Math.sin(game.phaseTime*5)*7,12,game.phase==='bite'?'#e99468':'#f7f0cf');if(game.phase==='reeling'){rect(200,460,560,34,'#edf1e2');rect(200+(game.zone-.125)*560,460,.25*560,34,'#93b578');rect(200+game.indicator*560-4,448,8,58,'#48543b',3);}text(game.phase==='bite'?'입질! 지금!':game.phase==='waiting'?'… 기다리는 중 …':game.phase==='reeling'?'초록 칸에 맞춰요':'눌러서 낚시하기',330,95,30);}
   if(id==='jump'){rect(0,525,960,45,'#e0e9cb');for(const d of game.items){if(d.food&&!d.resolved)circle(d.x,525-d.height,18,'#dc9e9b');if(!d.food)rect(d.x-d.width/2,525-d.height,d.width,d.height,d.resolved?'#b6bdaa':'#809570',8);}pet(180,490-game.height,130,game.height>2?4:1);}
   if(id==='bake'){pet(235,320,210,game.phase==='heating'?2:0);circle(650,290,80,game.outcome==='burnt'?'#646151':'#dfba8c');text(game.recipe,600,300,28,'#4b503a');rect(220,475,560,35,'#e4e9d7');rect(220+(game.target-9)*5.6,475,18*5.6,35,'#d6b862');rect(220,520,game.heat*5.6,10,'#ba8262',5);rect(220+game.heat*5.6-4,463,8,58,'#53653f',3);text(`${game.completed}/${MyDayMiniRules.BakeOrders} 접시 · ${Math.round(game.heat)}°`,360,425);}
   if(id==='blocks'){
    const phone=canvas.width===480,size=phone?30:26,ox=phone?28:350,oy=35;
    rect(ox-5,oy-5,size*10+10,size*20+10,'#e4e8dc');
    for(let y=0;y<20;y++)for(let x=0;x<10;x++){const n=game.board[y][x];if(n)cell(ox+x*size,oy+y*size,size,n);else rect(ox+x*size+1,oy+y*size+1,size-2,size-2,'#f7f8f0',3);}
    for(const[x,y]of game.cells()){if(y+game.ghost>=0)cell(ox+(game.x+x)*size,oy+(game.ghost+y)*size,size,game.kind+1,.23);if(y+game.y>=0)cell(ox+(game.x+x)*size,oy+(game.y+y)*size,size,game.kind+1);}
    text('다음 상몬',phone?345:650,70,phone?18:22);
    game.next.slice(0,3).forEach((kind,i)=>game.cells(kind,0).forEach(([x,y])=>cell((phone?345:655)+x*22,100+i*110+y*22,22,kind+1)));
    pet(phone?400:175,phone?510:420,phone?110:150);text(`${game.lines}줄 · Lv.${game.level}`,phone?345:90,phone?610:535,phone?17:22);
   }
   if(id==='block-puzzle'){
    const phone=canvas.width===480,size=52,ox=phone?32:272,oy=42;
    rect(ox-5,oy-5,426,426,'#e0e6d5');
    for(let y=0;y<8;y++)for(let x=0;x<8;x++){const n=game.board[y][x];if(n)cell(ox+x*size,oy+y*size,size,n);else rect(ox+x*size+2,oy+y*size+2,size-4,size-4,'#f8f9f2',8);}
    const piece=game.hand[selected];if(hover&&piece){const valid=game.can(piece,...hover);for(const[x,y]of piece.cells){const bx=x+hover[0],by=y+hover[1];if(bx>=0&&by>=0&&bx<8&&by<8)cell(ox+bx*size,oy+by*size,size,valid?piece.color+1:1,.35);}}
    game.hand.forEach((p,i)=>{const hx=phone?4+i*158:180+i*210;rect(hx,480,phone?150:190,106,i===selected?'#e6ecd8':'#f1f2e8');if(p)p.cells.forEach(([x,y])=>cell(hx+15+x*18,494+y*18,18,p.color+1,game.fits(p)?1:.35));});
    if(phone)text('블록을 고르고 보드를 눌러요',32,615,19);else{text('블록 선택 → 보드 터치',20,60,18);pet(125,290,150);}
   }
 }
 function frame(now){const dt=previous?Math.min(.25,(now-previous)/1000):0;previous=now;if(game){game.step(dt,axis);if(held&&game.state==='playing'){heldClock+=dt;if(heldClock>=.075){heldClock=0;held();}}scene();$('score').textContent=game.score+'점';$('clock').textContent=game.seconds?Math.ceil(game.left)+'초':'';$('hint').textContent=game.message||descriptions[G.ids.indexOf(id)];if(game.state==='finished')finish();}requestAnimationFrame(frame);}
 cards();requestAnimationFrame(frame);bridge({type:'ready'});setTimeout(()=>{if(!initialized)$('status').textContent='게임 저장소 연결을 확인하고 있어요.';},5000);
})();
