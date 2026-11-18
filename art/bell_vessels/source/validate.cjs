const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const PIX2=require('../../../tools/noiradix-studio/js/pix2-format.js');
const root=path.resolve(__dirname,'..');
const report=[];
for(const slug of ['bell_wanderer','moon_mender','rune_warden']){
  const file=path.join(root,slug,slug+'.pix2');
  const doc=PIX2.parseValidated(fs.readFileSync(file,'utf8'),{profile:'studio',strict:true});
  const result=PIX2.validate(doc,{profile:'studio',strict:true});
  assert.equal(result.valid,true,JSON.stringify(result));
  assert.equal(Object.keys(doc.cells).length,32);
  const reloaded=PIX2.parseValidated(PIX2.serialize(doc),{profile:'studio',strict:true});
  const rendered={};
  for(const [id,cell] of Object.entries(doc.cells)){
    rendered[id]=PIX2.compositeCellLayers(cell,{w:32,h:32});
    assert.deepEqual(PIX2.compositeCellLayers(reloaded.cells[id],{w:32,h:32}),rendered[id]);
    for(const coord of Object.keys(rendered[id])){
      const [x,y]=coord.split(',').map(Number);
      assert(x>=0&&x<32&&y>=0&&y<32);
    }
  }
  for(let row=0;row<4;row++)for(let f=0;f<4;f++){
    const r=rendered[`cell_${row}_${f}`],l=rendered[`cell_${row+4}_${f}`];
    const mirrored=Object.fromEntries(Object.entries(r).map(([xy,c])=>{const [x,y]=xy.split(',').map(Number);return [`${31-x},${y}`,c]}));
    assert.deepEqual(l,mirrored);
  }
  fs.writeFileSync(path.join(root,slug,'parser_pixels.json'),JSON.stringify(rendered));
  report.push({slug,frames:32,strictValidation:true,roundTripPixels:true,mirroring:true,warnings:result.warnings});
}
fs.writeFileSync(path.join(root,'validation.json'),JSON.stringify(report,null,2));
console.log(JSON.stringify(report,null,2));
