// 런타임 스프라이트를 밝고 어두운 배경, 실제 크기와 확대 크기로 비교한다.
const sharp = require('sharp');
const path = require('path');
const fs = require('fs');
const root = path.resolve(__dirname, '../..');
const media = path.join(root, 'docs/pr-media/372');
const art = path.join(root, '.art/concept/arcane-infantry-restyle-20261026');
const sprites = path.join(root, 'Assets/Resources/Game/sprites');
async function main() {
  fs.mkdirSync(media,{recursive:true});
  const names=['Before-idle','ArcaneInfantry','ArcaneInfantryAttacking','ArcaneInfantryWalkLeft','ArcaneInfantryWalkRight'];
  const layers=[];
  for(let row=0;row<2;row++) for(let i=0;i<names.length;i++) {
    const source=path.join(i===0?art:sprites,names[i]+'.png');
    const input=await sharp(source).resize(256,162,{kernel:'nearest'}).png().toBuffer();
    layers.push({input,left:i*256,top:row*270+38});
    layers.push({input:await sharp(source).resize({height:64}).png().toBuffer(),left:i*256+77,top:row*270+204});
  }
  const bg=Buffer.from('<svg width="1280" height="540"><rect width="1280" height="270" fill="#e0e0e0"/><rect y="270" width="1280" height="270" fill="#36414c"/><g font-family="Arial" font-size="18" fill="#111">'+['Before','Idle','Attack','Step A','Step B'].map((s,i)=>`<text x="${i*256+90}" y="25">${s}</text>`).join('')+'</g></svg>');
  await sharp(bg).composite(layers).png({palette:true}).toFile(path.join(media,'infantry-frames.png'));
  const frames=[];
  for(const name of ['ArcaneInfantryWalkLeft','ArcaneInfantryWalkRight']) {
    frames.push(await sharp(path.join(sprites,name+'.png')).resize(512,324,{kernel:'nearest'}).flatten({background:'#36414c'}).ensureAlpha().raw().toBuffer());
  }
  await sharp(Buffer.concat(frames),{raw:{width:512,height:648,channels:4,pageHeight:324}})
    .gif({delay:[160,160],loop:0}).toFile(path.join(media,'infantry-walk.gif'));
}
main().catch(e=>{console.error(e);process.exit(1);});
