const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const PIX2=require('./noiradix-studio/js/pix2-format.js');
const report=[];
for(const argument of process.argv.slice(2)){
 const file=path.resolve(argument),text=fs.readFileSync(file,'utf8'),doc=PIX2.parseValidated(text,{strict:true,profile:'studio'}),round=PIX2.parseValidated(PIX2.serialize(doc),{strict:true,profile:'studio'});
 const size=doc.meta.cell_size,grid=doc.meta.grid,occupied=new Set(),ids=new Set(),raster={};
 function node(n){assert(!ids.has(n.id),'Duplicate structural ID '+n.id);ids.add(n.id);for(const child of n.children||[])node(child);}
 for(const cell of Object.values(doc.cells)){
  assert(Number.isInteger(cell.row)&&Number.isInteger(cell.col)&&cell.row>=0&&cell.row<grid.rows&&cell.col>=0&&cell.col<grid.cols,`Cell ${cell.id} outside ${grid.rows} rows x ${grid.cols} columns: ${cell.row},${cell.col}`);
  const at=cell.row+','+cell.col;assert(!occupied.has(at),'Two cells occupy '+at);occupied.add(at);for(const layer of cell.layers)node(layer);
  const pixels=PIX2.compositeCellLayers(cell,size);assert.deepEqual(PIX2.compositeCellLayers(round.cells[cell.id],size),pixels);
  assert.equal(round.cells[cell.id].row,cell.row);assert.equal(round.cells[cell.id].col,cell.col);
  for(const key of Object.keys(pixels)){const [x,y]=key.split(',').map(Number);assert(x>=0&&y>=0&&x<size.w&&y<size.h);}
  raster[cell.id]={row:cell.row,col:cell.col,pixels};
 }
 for(const clip of Object.values(doc.animations)){
  const cells=clip.frames.map(id=>doc.cells[id]);assert(cells.every(c=>c.row===cells[0].row),'Animation crosses rows: '+clip.id);assert(cells.every((c,i)=>c.col===i),'Animation does not run horizontally from column 0: '+clip.id);
 }
 const entry={file,png:file.replace(/\.pix2$/,'.png'),size,grid,cells:Object.keys(doc.cells).length,clips:Object.keys(doc.animations).length,strict:true,rowMajor:true,roundTrip:true,raster};report.push(entry);
 console.log(path.relative(process.cwd(),file)+': strict / layout / row-major clips / pixel round trip PASS');
}
fs.mkdirSync('Builds',{recursive:true});fs.writeFileSync('Builds/pix2-raster-evidence.json',JSON.stringify(report));
