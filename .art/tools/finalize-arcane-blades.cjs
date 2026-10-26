// 이미지 생성 결과를 분할하고 공통 배율과 발 기준으로 런타임 캔버스에 맞춘다.
const sharp = require('sharp');
const fs = require('fs');
const path = require('path');
const root = path.resolve(__dirname, '../..');
const dir = path.join(root, '.art/concept/arcane-infantry-restyle-20261026');
function bounds(data, w, h, minY = 0) {
  const b = [w, h, 0, 0];
  for (let y = minY; y < h; y++) for (let x = 0; x < w; x++) {
    if (data[(y*w+x)*4+3] <= 128) continue;
    b[0] = Math.min(b[0],x); b[1] = Math.min(b[1],y);
    b[2] = Math.max(b[2],x+1); b[3] = Math.max(b[3],y+1);
  }
  return b;
}
async function main() {
  const source = process.argv[2] || 'C-production-pair.png';
  const names = process.argv[3] === 'walk' ? ['ArcaneInfantryWalkLeft','ArcaneInfantryWalkRight'] : ['ArcaneInfantry','ArcaneInfantryAttacking'];
  const {data,info} = await sharp(path.join(dir,source)).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  // 중앙의 투명한 열로 나눠 생성기가 셀 너비를 지키지 않아도 몸을 자르지 않는다.
  let split = Math.round(info.width/2), best = Infinity;
  for (let x=Math.floor(info.width*.4);x<info.width*.6;x++) {
    let count=0; for(let y=0;y<info.height;y++) if(data[(y*info.width+x)*4+3]>8) count++;
    const score=count*info.width+Math.abs(x-info.width/2);
    if(score<best){best=score;split=x;}
  }
  const frames=[];
  for(let i=0;i<2;i++) {
    const left=i===0?0:split, width=i===0?split:info.width-split;
    const raw=await sharp(data,{raw:info}).extract({left,top:0,width,height:info.height}).raw().toBuffer();
    const box=bounds(raw,width,info.height);
    const feet=bounds(raw,width,info.height,box[3]-Math.round((box[3]-box[1])*.08));
    frames.push({raw,width,box,anchor:(feet[0]+feet[2])/2});
    await sharp(raw,{raw:{width,height:info.height,channels:4}}).png().toFile(path.join(dir,names[i]+'-source.png'));
  }
  const height=Math.max(...frames.map(f=>f.box[3]-f.box[1]));
  const factor=81/height;
  let headTarget;
  if (process.argv[3] === 'walk') {
    const idle = await sharp(path.join(root,'Assets/Resources/Game/sprites/ArcaneInfantry.png')).ensureAlpha().raw().toBuffer();
    const head = bounds(idle,128,Math.round(81*.24));
    headTarget = (head[0]+head[2])/2;
  }
  const report=[];
  for(let i=0;i<2;i++) {
    const f=frames[i], b=f.box;
    const width=Math.round((b[2]-b[0])*factor), h=Math.round((b[3]-b[1])*factor);
    let left=Math.round(64-(f.anchor-b[0])*factor), top=81-h;
    if (headTarget !== undefined) {
      // 걷는 발의 위치가 바뀌어도 머리와 몸통의 수평 중심은 움직이지 않는다.
      const head=bounds(f.raw,f.width,b[1]+Math.round((b[3]-b[1])*.24));
      left=Math.round(headTarget-((head[0]+head[2])/2-b[0])*factor);
    }
    if(left<0||left+width>128) throw Error('공격 폭 때문에 몸을 줄이지 말고 자세를 재생성해야 한다.');
    const cut=await sharp(f.raw,{raw:{width:f.width,height:info.height,channels:4}}).extract({left:b[0],top:b[1],width:b[2]-b[0],height:b[3]-b[1]}).resize(width,h).png().toBuffer();
    const out=path.join(root,'Assets/Resources/Game/sprites',names[i]+'.png');
    await sharp({create:{width:128,height:81,channels:4,background:'#00000000'}}).composite([{input:cut,left,top}]).png().toFile(out);
    const result=await sharp(out).raw().toBuffer();
    const outBox=bounds(result,128,81);
    const transparent=Array.from({length:128*81},(_,p)=>result[p*4+3]===0).filter(Boolean).length/(128*81);
    if(transparent<.2||outBox[3]!==81)throw Error('투명 배경 또는 발 기준 검사 실패');
    report.push({name:names[i],sourceBox:b,footAnchor:f.anchor,factor,canvas:[128,81],box:outBox,transparent});
  }
  fs.writeFileSync(path.join(dir,process.argv[3]==='walk'?'walk-geometry.json':'production-geometry.json'),JSON.stringify(report,null,2)+'\n');
  console.log(JSON.stringify(report,null,2));
}
main().catch(e=>{console.error(e);process.exit(1);});
