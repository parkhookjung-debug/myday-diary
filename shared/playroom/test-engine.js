/* Compare native C# and the mobile/Mac engines, including deterministic dealing. */
'use strict';
const assert=require('node:assert/strict'),fs=require('node:fs'),G=require('./engine.js');
const rows=JSON.parse(fs.readFileSync(process.argv[2],'utf8'));
function equal(expected,actual,path){if(typeof expected==='number'){assert(Math.abs(expected-actual)<=1e-7,`${path}: ${expected} != ${actual}`);return;}if(Array.isArray(expected)){assert(Array.isArray(actual),path);assert.equal(actual.length,expected.length,path+'.length');expected.forEach((value,i)=>equal(value,actual[i],path+'['+i+']'));return;}assert.equal(actual,expected,path);}
let checked=0;
for(const seed of [...new Set(rows.map(row=>row.seed))]){
 const games=Object.fromEntries(G.ids.map(id=>[id,G.create(id,seed)]));const {snack,jump,fish,sling,bake,blocks,'block-puzzle':puzzle}=games;
 for(let i=0;i<3600;i++){
  const axis=i%240<80?-1:i%240<160?1:0;
  if(i%90===0)jump.action();if(i%37===0)fish.action();if(i%180===0){sling.begin(225,450);sling.aim(110,510);sling.release();}if(i%120===0)bake.begin();if(i%120===90)bake.release();
  if(i%17===0)blocks.move(i%2===0?-1:1);if(i%31===0)blocks.rotate(1);if(i%53===0)blocks.soft();if(i%101===0)blocks.hard();
  if(i%60===0&&puzzle.state!=='finished'){let placed=false;for(let slot=0;slot<3&&!placed;slot++)for(let y=0;y<8&&!placed;y++)for(let x=0;x<8&&!placed;x++)if(puzzle.can(puzzle.hand[slot],x,y)){puzzle.place(slot,x,y);placed=true;}}
  snack.step(1/60,axis);jump.step(1/60);fish.step(1/60);sling.step(1/60);bake.step(1/60);blocks.step(1/60);
  if(i%120!==0&&i!==3599)continue;
  const row=rows.find(row=>row.seed===seed&&row.frame===i);assert(row,'missing snapshot');
  const actual={snack:[snack.score,snack.lives,snack.x,snack.eaten,snack.combo,snack.state,snack.drops.map(d=>[d.kind,d.x,d.y,d.speed])],jump:[jump.score,jump.lives,jump.height,jump.eaten,jump.cleared,jump.state],fish:[fish.score,fish.catches,fish.perfects,fish.zone,fish.indicator,fish.phase],sling:[sling.score,sling.shots,sling.goals,sling.x,sling.y,sling.flying],bake:[bake.score,bake.completed,bake.cooked,bake.perfects,bake.heat,bake.index],blocks:[blocks.score,blocks.lines,blocks.pieces,blocks.x,blocks.y,blocks.rotation,blocks.kind,blocks.ghost,blocks.board,blocks.next],puzzle:[puzzle.score,puzzle.lines,puzzle.moves,puzzle.combo,puzzle.state==='finished',puzzle.board,puzzle.hand.map(p=>p?[p.color,p.cells]:null)]};
  for(const id of Object.keys(actual))equal(row[id],actual[id],`${seed}/${i}/${id}`);checked++;
 }
}
// User-facing lifecycle: no background progress, no extra heat/drag release after focus loss.
for(const id of G.ids){const g=G.create(id,1);g.pause();const before=JSON.stringify(g);g.step(30);assert.equal(JSON.stringify(g),before,id+' paused');g.resume();assert.equal(g.state,'playing');g.step(Number.NaN);assert(Number.isFinite(g.score));}
const baking=G.create('bake',1);baking.begin();baking.step(.2);baking.pause();baking.resume();baking.release();assert.equal(baking.completed,0);
const sling=G.create('sling',1);sling.begin(225,450);sling.aim(110,510);sling.pause();sling.resume();sling.release();assert.equal(sling.shots,0);
assert.equal(G.dailySeed('2026-11-28'),(Math.imul(20261128,397)^0x53414e47)|0);
const fetch=G.create('fetch',1);fetch.begin(440,500);fetch.step(.05);fetch.aim(400,450);fetch.step(.05);fetch.aim(350,400);assert(fetch.scaleX!==0);fetch.release();assert(Math.hypot(fetch.ball.vx,fetch.ball.vy)>140);for(let i=0;i<1000;i++)fetch.step(1/120);assert(fetch.ball.x>=18&&fetch.ball.x<=942);fetch.pause();const parked=JSON.stringify(fetch);fetch.step(1);assert.equal(JSON.stringify(fetch),parked);
console.log(`Seven games: ${checked} native replay snapshots match; pause/cancel and daily seed verified.`);
