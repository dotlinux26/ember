const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),assert=require('node:assert/strict');
const PIX2=require('../../../tools/noiradix-studio/js/pix2-format.js');
const root=path.resolve(__dirname,'..');
const original=path.resolve(root,'../bell_wanderer_kha_on_mauthamkhao_khóngua.pix2');
const expected='ec87d5efde18fbc62bf434b268cab4c88f422e1972d09c086a26308b08c69d6a';
const digest=()=>crypto.createHash('sha256').update(fs.readFileSync(original)).digest('hex');
assert.equal(digest(),expected,'Reference changed externally; never overwrite it.');
const baseline=PIX2.parseValidated(fs.readFileSync(path.join(root,'bell_wanderer_reference_clone.pix2'),'utf8'),{strict:true});
const variants=[
 {slug:'bell_wanderer',name:'Lữ Hành Chuông',colors:{}},
 {slug:'moon_mender',name:'Y Sư Ánh Trăng',colors:{'#fa7180':'#75d9c5','#d64a65':'#42a99e','#ffab99':'#c4f4df','#514975':'#426c7a','#706792':'#729caa','#eadcce':'#dce7ed','#fff2d8':'#f8fff0','#a89bb3':'#93afbe'}},
 {slug:'bomb_trapper',name:'Kẻ Đặt Bẫy Hỏa Ngục',colors:{'#fa7180':'#f8b252','#d64a65':'#ca733b','#ffab99':'#ffe3a0','#eadcce':'#a78a65','#fff2d8':'#e1bc7a','#d1c1cb':'#7c6c67','#a89bb3':'#665b63','#23bad5':'#f49c42','#85edee':'#ffdb88','#e9ffff':'#fff2cc','#514975':'#556852','#706792':'#8a9860'}},
 {slug:'rune_warden',name:'Hộ Vệ Cổ Ngữ',colors:{'#fa7180':'#dcb65e','#d64a65':'#9f7945','#ffab99':'#fff0b7','#eadcce':'#a9b7ce','#fff2d8':'#e5e7e2','#d1c1cb':'#8192b1','#a89bb3':'#647698','#514975':'#46566e','#706792':'#7388a5'}},
 {slug:'space_wraith',name:'Bóng Ma Không Gian',colors:{'#fa7180':'#bd86e5','#d64a65':'#865bb7','#ffab99':'#e8c1ff','#eadcce':'#514e77','#fff2d8':'#79799f','#d1c1cb':'#393b60','#a89bb3':'#303254','#706792':'#726da5','#23bad5':'#a686ec','#85edee':'#e2bbff','#e9ffff':'#fff2ff'}},
];
let report=[];
for(let role=0;role<variants.length;role++){
 const v=variants[role],doc=JSON.parse(JSON.stringify(baseline));doc.meta.title='Ember reference-derived / '+v.name;
 for(const key of Object.keys(doc.palette))doc.palette[key]=v.colors[doc.palette[key]]||doc.palette[key];
 const pixels={},eyes={};
 for(const [id,cell] of Object.entries(doc.cells)){
  // PIX2 grid_pos is ROW,COLUMN, not x,y. Repair layout in derived files only.
  const [,row,col]=id.split('_').map(Number);cell.row=row;cell.col=col;
  const left=cell.direction==='left';
  function recolor(node){for(const key of Object.keys(node.pixels||{}))node.pixels[key]=v.colors[node.pixels[key]]||node.pixels[key];for(const c of node.children||[])recolor(c);}
  for(const layer of cell.layers)recolor(layer);
  if(role>0){
   const lid=id+'_role_layer',rid=id+'_role_emblem';const coords={};
   function p(x,y,color){if(left)x=31-x;assert(x>=0&&x<32&&y>=0&&y<32);coords[x+','+y]=color;}
   // Accessories align to the existing anatomy. Base silhouette stays intact.
   if(role===1){for(const [x,y] of [[13,8],[12,9],[12,10],[13,11],[14,11]])p(x,y,'#f3e7a3');p(13,9,'#fff8da');}
   if(role===2){for(let x=11;x<=18;x++)p(x,16,'#302739');for(let x=12;x<=14;x++)p(x,17,'#f6ca69');for(let x=17;x<=19;x++)p(x,15,'#f6ca69');p(13,17,'#fff0b4');p(18,15,'#fff0b4');}
   if(role===3){for(const [x,y] of [[14,8],[13,9],[15,9],[14,10]])p(x,y,'#5ee6df');p(14,9,'#efffff');for(let y=22;y<=24;y++){p(11,y,'#d9c184');p(20,y,'#a58b58');}}
   if(role===4){for(const [x,y] of [[17,8],[18,9],[17,10],[16,9]])p(x,y,'#b6ecff');p(17,9,'#f3ffff');}
   cell.layers.push({type:'layer',id:lid,cell:id,name:'Role emblem',visible:true,opacity:1,blend:'normal',children:[{type:'region',id:rid,cell:id,parent:lid,name:'Role emblem',tags:['role','equipment'],visible:true,opacity:1,pixels:coords,primitives:[]}]});
  }
  pixels[id]=PIX2.compositeCellLayers(cell,{w:32,h:32});
  const eyeLayers=cell.layers.filter(l=>l.id.includes('glowing_eyes'));eyes[id]=PIX2.compositeCellLayers({layers:eyeLayers},{w:32,h:32});
  if(role===0)assert.deepEqual(pixels[id],PIX2.compositeCellLayers(baseline.cells[id],{w:32,h:32}),'Base clone must preserve reference artwork');
 }
 const folder=path.join(root,v.slug);fs.mkdirSync(folder,{recursive:true});
 const source=PIX2.serialize(doc);const round=PIX2.parseValidated(source,{strict:true});
 for(const id of Object.keys(pixels))assert.deepEqual(PIX2.compositeCellLayers(round.cells[id],{w:32,h:32}),pixels[id]);
 for(const animation of Object.values(round.animations)){
  const cells=animation.frames.map(id=>round.cells[id]);assert(cells.every(c=>c.row===cells[0].row));assert(cells.every((c,i)=>c.col===i));assert(cells.every(c=>c.row>=0&&c.row<8&&c.col>=0&&c.col<4));
 }
 fs.writeFileSync(path.join(folder,v.slug+'.pix2'),source);fs.writeFileSync(path.join(folder,'pixels.json'),JSON.stringify(pixels));
 fs.writeFileSync(path.join(folder,'eyes_pixels.json'),JSON.stringify(eyes));
 const frames=Object.values(doc.cells).map(c=>{const [,r,col]=c.id.split('_').map(Number);return {name:c.name,x:col*32,y:r*32,w:32,h:32,pivotX:.5,pivotY:1/32};});
 const clips=Object.values(doc.animations).map(a=>({clipName:a.id,fps:a.fps,loop:a.loop,frameNames:a.frames.map(id=>doc.cells[id].name)}));
 fs.writeFileSync(path.join(folder,v.slug+'.pix2.json'),JSON.stringify({meta:{image:v.slug+'.png',cellWidth:32,cellHeight:32,size:{w:128,h:256}},frames,animations:clips},null,2));
 report.push({slug:v.slug,strict:true,roundTrip:true,frames:frames.length,basePixelsUnchanged:role===0?true:undefined});
}
assert.equal(digest(),expected);fs.writeFileSync(path.join(root,'validation.json'),JSON.stringify({referenceSHA256:expected,originalUnchanged:true,variants:report},null,2));
console.log('5 sheets validated; 160 frames. Original reference SHA256 unchanged.');
