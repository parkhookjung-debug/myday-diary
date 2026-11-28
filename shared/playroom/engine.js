/* MyDay offline games. Rules match windows/Core; no storage, UI, diary XP or network here. */
(function(root) {
  'use strict';
  const M = root.MyDayMiniRules || (typeof require === 'function' ? require('../mini-games.json') : {});
  const S = root.MyDaySnackRules || (typeof require === 'function' ? require('../snack-game.json') : {});
  const clamp = (n,a,b) => Math.max(a,Math.min(b,n));
  class Random {
    constructor(seed) { this.state=(seed>>>0)||0x6d2b79f5; }
    double() { let x=this.state; x^=x<<13; x^=x>>>17; x^=x<<5; this.state=x>>>0; return this.state/4294967296; }
    next(max) { return Math.floor(this.double()*max); }
  }
  function dailySeed(key) { return (Math.imul(Number(key.replace(/-/g,'')),397)^0x53414e47)|0; }
  class Round {
    constructor(seed,seconds=0) { this.random=new Random(seed); this.seconds=seconds; this.elapsed=0; this.score=0; this.state='playing'; this.remainder=0; this.message=''; }
    pause() { if(this.state==='playing') { this.cancel(); this.state='paused'; } this.remainder=0; }
    resume() { if(this.state==='paused') this.state='playing'; }
    cancel() {}
    step(seconds,axis=0) {
      if(this.state!=='playing'||!Number.isFinite(seconds)||seconds<=0) return;
      this.remainder+=Math.min(.25,seconds); const h=1/120;
      while(this.remainder+1e-10>=h && this.state==='playing') {
        this.remainder-=h; this.elapsed=this.seconds ? Math.min(this.seconds,this.elapsed+h):this.elapsed+h;
        this.advance(h,Number.isFinite(axis)?clamp(axis,-1,1):0);
        if(this.seconds && this.elapsed>=this.seconds-1e-8) { this.elapsed=this.seconds; this.cancel(); this.state='finished'; }
      }
    }
    add(n) { this.score=Math.min(1000000,this.score+n); }
    get left() { return Math.max(0,this.seconds-this.elapsed); }
  }
  class Snack extends Round {
    constructor(seed) { super(seed,S.roundSeconds); this.x=this.target=480; this.lives=S.startingLives; this.drops=[]; this.spawn=.4; this.invulnerable=this.combo=this.maxCombo=this.eaten=0; }
    aim(x) { if(Number.isFinite(x)) this.target=clamp(x,60,900); }
    advance(h,axis) {
      this.invulnerable=Math.max(0,this.invulnerable-h);
      if(axis) { this.x=clamp(this.x+axis*S.playerSpeed*h,60,900); this.target=this.x; }
      else { const d=this.target-this.x; this.x+=Math.sign(d)*Math.min(Math.abs(d),S.playerSpeed*h); }
      const difficulty=this.elapsed/S.roundSeconds; this.spawn-=h;
      if(this.spawn<=0) {
        const kind=this.random.double()<S.hazardProbability ? 3+this.random.next(2):this.random.next(3);
        this.drops.push({kind,x:60+this.random.double()*840,y:96,speed:160+difficulty*135+this.random.double()*50}); this.spawn+=.68-difficulty*.29;
      }
      for(let i=this.drops.length-1;i>=0;i--) {
        const d=this.drops[i]; d.y+=d.speed*h;
        if((d.x-this.x)**2+(d.y-490)**2<=46**2) {
          this.drops.splice(i,1);
          if(d.kind<3) { this.combo++; this.maxCombo=Math.max(this.maxCombo,this.combo); this.eaten++; this.add(S.foodPoints+(this.combo%S.comboLength===0?S.comboBonus:0)); }
          else if(this.invulnerable<=0) { this.lives--; this.combo=0; this.invulnerable=S.invulnerableSeconds; if(!this.lives) { this.state='finished'; break; } }
        } else if(d.y>630) { if(d.kind<3)this.combo=0; this.drops.splice(i,1); }
      }
      this.message=`간식 ${this.eaten}개 · 연속 ${this.combo} · 하트 ${this.lives}`;
    }
  }
  class Sling extends Round {
    constructor(seed) { super(seed,M.SlingSeconds); this.goals=this.shots=0; this.reset(); }
    reset() { this.x=225;this.y=450;this.vx=this.vy=this.age=this.bounces=0;this.flying=this.aiming=false; }
    get target() { return [[610,330],[780,220],[660,420],[805,340],[735,185]][Math.min(4,this.goals)]; }
    begin(x,y) { if(this.state!=='playing'||!Number.isFinite(x)||!Number.isFinite(y)||this.flying||this.shots>=M.SlingShots||(x-this.x)**2+(y-this.y)**2>80**2)return;this.aiming=true;this.offset=[x-this.x,y-this.y]; }
    aim(x,y) { if(!this.aiming||!Number.isFinite(x)||!Number.isFinite(y))return;let dx=x-this.offset[0]-225,dy=Math.min(95,y-this.offset[1]-450);const length=Math.hypot(dx,dy);if(length>M.SlingMaxPull){dx*=M.SlingMaxPull/length;dy*=M.SlingMaxPull/length;}this.x=225+dx;this.y=450+dy; }
    release() { if(!this.aiming)return;this.aiming=false;const dx=225-this.x,dy=450-this.y;if(Math.hypot(dx,dy)<12){this.reset();return;}this.vx=dx*M.SlingStrength;this.vy=dy*M.SlingStrength;this.flying=true;this.shots++; }
    cancel() { if(this.aiming)this.reset(); }
    advance(h) {
      if(this.flying) {
        this.age+=h;this.vy+=M.SlingGravity*h;this.x+=this.vx*h;this.y+=this.vy*h;
        const d=(this.x-this.target[0])**2+(this.y-this.target[1])**2;
        if(d<=44**2) { this.add(d<18**2?150:100);this.goals++;this.reset();if(this.goals===5||this.shots>=M.SlingShots)this.state='finished'; }
        else {
          if((this.x<85&&this.vx<0)||(this.x>875&&this.vx>0)){this.x=clamp(this.x,85,875);this.vx*= -.65;this.bounces++;}
          if(this.y<135&&this.vy<0){this.y=135;this.vy*= -.65;this.bounces++;}
          if(this.y>530&&this.vy>0){this.y=530;this.vy*= -.6;this.vx*= .8;this.bounces++;}
          if(this.age>3.8||this.bounces>=4){this.reset();if(this.shots>=M.SlingShots)this.state='finished';}
        }
      }
      this.message=`과녁 ${this.goals}/5 · 발사 ${this.shots}/${M.SlingShots} · 상몬을 뒤로 당겨 놓아요`;
    }
  }
  class Fish extends Round {
    constructor(seed) { super(seed,M.FishSeconds);this.phase='idle';this.phaseTime=0;this.catches=this.perfects=0;this.zone=0;this.message='눌러서 낚싯줄을 던져요'; }
    get indicator(){return .5+Math.sin(this.phaseTime*3.7)*.47;}
    action() {
      if(this.state!=='playing')return;
      if(this.phase==='idle'||this.phase==='caught'){this.phase='waiting';this.wait=M.FishWaitMin+this.random.double()*(M.FishWaitMax-M.FishWaitMin);this.message='찌를 보며 조금만 기다려요…';}
      else if(this.phase==='waiting'){this.phase='idle';this.message='조금 빨랐어요. 다시 던져요';}
      else if(this.phase==='bite'){this.phase='reeling';this.zone=.25+this.random.double()*.5;this.message='표시가 초록 칸에 오면 한 번 더!';}
      else if(this.phase==='reeling'){
        const accuracy=Math.abs(this.indicator-this.zone);
        if(accuracy<=M.FishZoneWidth/2){const perfect=accuracy<=.035;this.add(perfect?50:30);this.catches++;if(perfect)this.perfects++;this.phase='caught';this.message=['통통 붕어','반짝 송사리','작은 잉어'][this.random.next(3)]+(perfect?' · 완벽 +50':' · +30');}
        else{this.phase='idle';this.message='아깝다! 다시 도전해요';}
      }
      this.phaseTime=0;
    }
    advance(h){this.phaseTime+=h;
      if(this.phase==='waiting'&&this.phaseTime>=this.wait){this.phase='bite';this.phaseTime=0;this.message='입질! 지금 눌러요!';}
      else if((this.phase==='bite'&&this.phaseTime>=M.FishBiteWindow)||(this.phase==='reeling'&&this.phaseTime>=4)||(this.phase==='caught'&&this.phaseTime>=1.4)){this.phase='idle';this.phaseTime=0;this.message='눌러서 다시 던져요';}
    }
  }
  class Jump extends Round {
    constructor(seed){super(seed,M.JumpSeconds);this.height=this.velocity=0;this.lives=M.JumpLives;this.spawn=.9;this.items=[];this.invulnerable=this.cleared=this.eaten=0;}
    action(){if(this.state==='playing'&&this.height<=.001&&this.velocity<=0)this.velocity=M.JumpLaunch;}
    advance(h){
      this.invulnerable=Math.max(0,this.invulnerable-h);
      if(this.height>0||this.velocity>0){this.velocity-=M.JumpGravity*h;this.height+=this.velocity*h;if(this.height<0){this.height=0;this.velocity=0;}}
      this.spawn-=h;const speed=M.JumpSpeed+130*this.elapsed/M.JumpSeconds;
      if(this.spawn<=0){this.items.push({x:1010,width:34+this.random.next(15),height:40+this.random.next(19),food:false,resolved:false});this.items.push({x:1145,width:32,height:105+this.random.next(26),food:true,resolved:false});this.spawn+=1.85+this.random.double()*.5;}
      for(let i=this.items.length-1;i>=0;i--){const d=this.items[i];d.x-=speed*h;
        if(!d.resolved){
          if(d.food){const px=clamp(d.x,154,206),foodY=525-d.height,py=clamp(foodY,525-this.height-70,525-this.height);if((d.x-px)**2+(foodY-py)**2<=18**2){d.resolved=true;this.eaten++;this.add(10);}}
          else if(d.x<206&&d.x+d.width>154&&this.height<d.height){d.resolved=true;if(this.invulnerable<=0){this.lives--;this.invulnerable=1.2;if(!this.lives){this.state='finished';break;}}}
          else if(d.x+d.width<154){d.resolved=true;this.cleared++;this.add(20);}
        }
        if(d.x+d.width< -40||(d.food&&d.resolved))this.items.splice(i,1);
      }
      this.message=`장애물 ${this.cleared}개 · 간식 ${this.eaten}개 · 하트 ${this.lives}`;
    }
  }
  class Bake extends Round {
    constructor(seed){super(seed,M.BakeSeconds);this.index=this.completed=this.cooked=this.perfects=this.heat=0;this.phase='ready';this.outcome='';}
    get target(){return [68,74,62,78,66,72,58,76][this.index%8];}
    get recipe(){return ['쿠키','식빵','마시멜로'][this.index%3];}
    begin(){if(this.state==='playing'&&this.phase==='ready'){this.phase='heating';this.heat=0;}}
    release(){if(this.state!=='playing'||this.phase!=='heating')return;const d=Math.abs(this.heat-this.target);this.finish(this.heat>this.target+M.BakeTargetWidth/2?'burnt':d<=M.BakePerfectWidth/2?'perfect':d<=M.BakeTargetWidth/2?'good':'under');}
    finish(outcome){this.outcome=outcome;const n=outcome==='perfect'?M.BakePerfectPoints:outcome==='good'?M.BakeGoodPoints:0;this.add(n);this.completed++;if(n)this.cooked++;if(outcome==='perfect')this.perfects++;this.phase='feedback';this.feedback=M.BakeFeedbackSeconds;this.message={perfect:'딱 좋아요! +50',good:'노릇노릇 +30',under:'조금 덜 익었어요',burnt:'앗, 너무 뜨거워요'}[outcome];}
    cancel(){if(this.phase==='heating'){this.phase='ready';this.heat=0;}}
    advance(h){if(this.phase==='heating'){this.heat=Math.min(100,this.heat+(M.BakeHeatRate+(this.index*7)%9-4)*h);if(this.heat>=100-1e-8){this.heat=100;this.finish('burnt');}}
      else if(this.phase==='feedback'){this.feedback-=h;if(this.feedback<=0){if(this.completed>=M.BakeOrders)this.state='finished';else{this.index=this.completed;this.heat=0;this.phase='ready';this.message='다음 간식도 구워봐요';}}}
    }
  }
  const tetrominos=[[[0,1],[1,1],[2,1],[3,1]],[[0,0],[0,1],[1,1],[2,1]],[[2,0],[0,1],[1,1],[2,1]],[[1,0],[2,0],[1,1],[2,1]],[[1,0],[2,0],[0,1],[1,1]],[[1,0],[0,1],[1,1],[2,1]],[[0,0],[1,0],[1,1],[2,1]]];
  class Blocks extends Round {
    constructor(seed){super(seed);this.board=Array.from({length:M.BlocksRows},()=>Array(M.BlocksColumns).fill(0));this.next=[];this.lines=this.pieces=0;this.spawn();}
    cells(kind=this.kind,rotation=this.rotation){let cells=tetrominos[kind].map(p=>p.slice());if(kind===3)return cells;for(let i=0;i<rotation;i++)cells=cells.map(([x,y])=>[(kind===0?4:3)-1-y,x]);return cells;}
    fits(kind,rotation,x,y){return this.cells(kind,rotation).every(([cx,cy])=>{const px=x+cx,py=y+cy;return px>=0&&px<M.BlocksColumns&&py<M.BlocksRows&&py>=-4&&(py<0||this.board[py][px]===0);});}
    fill(){const bag=[0,1,2,3,4,5,6];for(let i=6;i>0;i--){const j=this.random.next(i+1);[bag[i],bag[j]]=[bag[j],bag[i]];}this.next.push(...bag);}
    spawn(){if(!this.next.length)this.fill();this.kind=this.next.shift();if(this.next.length<3)this.fill();this.x=3;this.y=-1;this.rotation=0;this.fallClock=this.lockClock=this.lockResets=0;if(!this.fits(this.kind,0,this.x,this.y))this.state='finished';}
    get ghost(){let y=this.y;while(this.fits(this.kind,this.rotation,this.x,y+1))y++;return y;}
    get level(){return Math.min(20,1+Math.floor(this.lines/M.BlocksLinesPerLevel));}
    get speed(){return Math.max(M.BlocksMinFallSeconds,M.BlocksFallSeconds-(Math.min(20,1+Math.floor(this.pieces/M.BlocksPiecesPerSpeed))-1)*M.BlocksFallStep);}
    resetLock(grounded){if(grounded&&this.lockResets<15){this.lockClock=0;this.lockResets++;}}
    move(dx){if(this.state!=='playing'||![-1,1].includes(dx)||!this.fits(this.kind,this.rotation,this.x+dx,this.y))return;const grounded=!this.fits(this.kind,this.rotation,this.x,this.y+1);this.x+=dx;this.resetLock(grounded);}
    rotate(dx){if(this.state!=='playing'||this.kind===3)return;const r=(this.rotation+dx+4)%4,grounded=!this.fits(this.kind,this.rotation,this.x,this.y+1);for(const [x,y] of [[0,0],[-1,0],[1,0],[-2,0],[2,0],[0,-1],[-1,-1],[1,-1],[0,-2]]){if(this.fits(this.kind,r,this.x+x,this.y+y)){this.rotation=r;this.x+=x;this.y+=y;this.resetLock(grounded);return;}}}
    soft(){if(this.state==='playing'&&this.fits(this.kind,this.rotation,this.x,this.y+1)){this.y++;this.add(1);this.fallClock=0;}}
    hard(){if(this.state!=='playing')return;const y=this.ghost;this.add((y-this.y)*2);this.y=y;this.lock();}
    lock(){if(this.cells().some(([,y])=>y+this.y<0)){this.state='finished';return;}for(const[x,y]of this.cells())this.board[y+this.y][x+this.x]=this.kind+1;this.pieces++;
      const remaining=this.board.filter(row=>row.some(n=>!n));const n=M.BlocksRows-remaining.length;
      if(n){this.add([0,100,300,500,800][n]*this.level);this.lines+=n;this.board=[...Array.from({length:n},()=>Array(M.BlocksColumns).fill(0)),...remaining];}this.spawn();this.message=`${this.lines}줄 · Lv.${this.level} · 다음 ${this.next.slice(0,3).map(i=>'IJLOSTZ'[i]).join(' ')}`;
    }
    advance(h){this.fallClock+=h;while(this.fallClock>=this.speed){this.fallClock-=this.speed;if(this.fits(this.kind,this.rotation,this.x,this.y+1))this.y++;}if(this.fits(this.kind,this.rotation,this.x,this.y+1))this.lockClock=0;else{this.lockClock+=h;if(this.lockClock>=M.BlocksLockSeconds-1e-8)this.lock();}}
  }
  const puzzleShapes=[];
  for(let n=1;n<=5;n++){puzzleShapes.push(Array.from({length:n},(_,i)=>[i,0]));if(n>1)puzzleShapes.push(Array.from({length:n},(_,i)=>[0,i]));}
  for(const[w,h]of[[2,2],[3,3],[3,2],[2,3]])puzzleShapes.push(Array.from({length:w*h},(_,i)=>[i%w,Math.floor(i/w)]));
  for(const pattern of ['10/11','01/11','11/10','11/01','100/111','001/111','111/100','111/001','010/111','111/010','10/11/10','01/11/01','011/110','110/011']){const cells=[];pattern.split('/').forEach((row,y)=>Array.from(row).forEach((v,x)=>{if(v==='1')cells.push([x,y]);}));puzzleShapes.push(cells);}
  class Puzzle extends Round {
    constructor(seed){super(seed);this.board=Array.from({length:8},()=>Array(8).fill(0));this.hand=[];this.lines=this.moves=this.combo=this.lastGain=this.lastLines=0;this.deal();}
    can(piece,x,y){return !!piece&&piece.cells.every(([cx,cy])=>{const px=x+cx,py=y+cy;return px>=0&&py>=0&&px<8&&py<8&&this.board[py][px]===0;});}
    fits(piece){if(!piece)return false;for(let y=0;y<8;y++)for(let x=0;x<8;x++)if(this.can(piece,x,y))return true;return false;}
    deal(){const fitting=puzzleShapes.filter(cells=>this.fits({cells}));this.hand=Array.from({length:3},(_,i)=>({cells:(i===0&&fitting.length?fitting[this.random.next(fitting.length)]:puzzleShapes[this.random.next(puzzleShapes.length)]).map(p=>p.slice()),color:this.random.next(6)}));}
    place(slot,x,y){const piece=this.hand[slot];if(this.state!=='playing'||!Number.isInteger(x)||!Number.isInteger(y)||!this.can(piece,x,y))return false;for(const[cx,cy]of piece.cells)this.board[y+cy][x+cx]=piece.color+1;this.hand[slot]=null;this.moves++;
      const rows=this.board.map(row=>row.every(Boolean)),cols=Array.from({length:8},(_,x)=>this.board.every(row=>!!row[x]));const n=rows.filter(Boolean).length+cols.filter(Boolean).length;this.lastLines=n;this.lines+=n;this.combo=n?this.combo+1:0;
      for(let y=0;y<8;y++)for(let x=0;x<8;x++)if(rows[y]||cols[x])this.board[y][x]=0;
      const perfect=n>0&&this.board.every(row=>row.every(v=>!v));this.lastGain=Math.min(1000000-this.score,piece.cells.length+10*n*n*this.combo+(perfect?50:0));this.add(this.lastGain);
      if(this.hand.every(p=>!p))this.deal();if(!this.hand.some(p=>this.fits(p)))this.state='finished';this.message=`${this.moves}번 배치 · ${this.lines}줄 · 연속 ${this.combo} · +${this.lastGain}`;return true;
    }
    advance(){}
  }
  const constructors={snack:Snack,sling:Sling,fish:Fish,jump:Jump,bake:Bake,blocks:Blocks,'block-puzzle':Puzzle};
  const api={Random,dailySeed,Snack,Sling,Fish,Jump,Bake,Blocks,Puzzle,create:(id,seed)=>new constructors[id](seed),ids:Object.keys(constructors)};
  if(typeof module!=='undefined'&&module.exports)module.exports=api;else root.MyDayGames=api;
})(typeof globalThis!=='undefined'?globalThis:this);
