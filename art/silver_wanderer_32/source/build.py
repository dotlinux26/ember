"""Editable native-pixel reconstruction of the approved no-arms concept.

The character map is the source of truth. No runtime resize, image diffusion,
background removal, or lossy image import is used to render the deliverables.
"""
from pathlib import Path
from PIL import Image, ImageDraw
import json

ROOT = Path(__file__).resolve().parents[1]
PALETTE = {
 'O':'#09080e', 'a':'#252330', 'b':'#3a374b', 'c':'#514c64',
 'd':'#aaa0b7', 'e':'#d1c8da', 'f':'#f8f3f6',
 's':'#ffceb4', 't':'#ed9897', 'h':'#ffd8bd', 'm':'#d8546e',
 'r':'#a12543', 'R':'#601630', 'x':'#bb2634',
 'u':'#51617a', 'v':'#303b50', 'w':'#eae8ed',
}
# A 32 x 32 integer grid: . is transparent. Deliberately broad flat clusters.
ROWS = '''
...........OOOO.................
..........OcccOO................
.........ObcacccOOO.............
........ObbaaccccaaaOO..........
......OObbbacccaaaacccOOOOOOO...
....OOacbcbaccaabbbbaOcccccOO...
....OccccbaafeabbbOcccccOOarO...
....OcaacaafOabbaOcccccdORRrO...
....OcaaOaafweObbcccOedwrrO....
....OcaOOaaOffOabbbOOfeewOr.....
..OaacaOOrrrOaabbbOeeefffOO.....
..OaaaO.OOOrOabbcOfeffffffO.....
..Oa...ObOrabbOOfsfeffdffffO....
..OO..OabrrabOfffstefeseeefO....
.....ObbbrrOeffffhhsfdddedfO....
.....OOOOrOddffRfRfhsOfRedfO....
.....ORRRaOddfffffxhhhfxddfO....
......ORRdedddffthhsshtdOO......
........OeedfdfeOthmhtdfO.......
........OeddfdfeavavaudfO.......
......OdeddfdfeaabauwdadO.......
....OOOddffddfOuuuOaaauuO.......
...OffffffddddOabbaOwbaaO.......
..OffeeffddeddaabbaaabbO........
..fffedddeeddObbbaOaubbO........
..ffeeeeeddaObbbuaOaaubaO.......
..eOOeeeedOObbbuOaOOaOubO......
..O.OeeddOOuuuuOhhRshORuO......
....OeOcOOOOrROabbObaarrO......
.....OO.OO.RraOadOOadOrO.......
...........rO.OabO.OabbO........
..............OOOO.OOOO........
'''.strip().splitlines()

# Pixel cleanup at the reduced face: continuous skin plane, paired eyelids,
# two red irises, no isolated red pixels in the white hair.
FACE_ROWS = {
 14: (15, 'ffhhsfdddedfO'),
 15: (13, 'ffRRRhhhRR edfO'.replace(' ','')),
 16: (13, 'fftfxhhhf xddfO'.replace(' ','')),
 17: (15, 'thhhhh tdfO'.replace(' ','')),
 18: (16, 'OthmmtdfO'),
}
for y,(x,strip) in FACE_ROWS.items():
 row=list(ROWS[y].ljust(32,'.'))
 row[x:x+len(strip)]=strip
 ROWS[y]=''.join(row)

def make_master():
 im=Image.new('RGBA',(32,32))
 for y,row in enumerate(ROWS):
  assert len(row)<=32,(y,len(row))
  for x,ch in enumerate(row):
   if ch!='.': im.putpixel((x,y),tuple(bytes.fromhex(PALETTE[ch][1:]))+(255,))
 return im

def write_pix2(path,frames,columns,rows,clips=()):
 lines=['@pix2 1.1','@meta','  title: "Silver Wanderer / approved concept reconstruction"','  version: "1.1"','  color_mode: "RGBA"','  coordinate_origin: "top-left"','@grid',f'  columns: {columns}',f'  rows: {rows}','  default_cell: 32x32','  background: none','@palette']
 indices={c:i+1 for i,c in enumerate(PALETTE.values())}
 lines += [f'  {i}: {c}' for c,i in indices.items()]
 atlas=Image.new('RGBA',(columns*32,rows*32))
 for cid,name,row,col,im in frames:
  atlas.paste(im,(col*32,row*32))
  lines += [f'@cell {cid}',f'  name: "{name}"',f'  direction: {"left" if "left" in name else "right"}','  frame: 0',f'  grid_pos: {row},{col}']
  groups={key:[] for key in ('outline','hat_coat','silver_hair_metal','skin','wine_accents')}
  for y in range(32):
   for x in range(32):
    c=im.getpixel((x,y))
    if not c[3]:continue
    color='#%02x%02x%02x'%c[:3]
    key=next(k for k,v in PALETTE.items() if v==color)
    group='outline' if key=='O' else 'silver_hair_metal' if key in 'defw' else 'skin' if key in 'sthm' else 'wine_accents' if key in 'rRx' else 'hat_coat'
    groups[group].append(f'p {x},{y} {indices[color]}')
  for group,records in groups.items():
   lines += [f'@layer {cid}_{group}_layer',f'  cell: {cid}',f'  name: "{group}"',f'@region {cid}_{group}',f'  cell: {cid}',f'  parent: {cid}_{group}_layer',f'  tags: [body, no_arms, {group}]']
   for start in range(0,len(records),16): lines.append('  '+' '.join(records[start:start+16]))
 for name,ids in clips:
  lines += [f'@animation {name}',f'  name: "{name}"',f'  direction: {"left" if "left" in name else "right"}',f'  frames: [{", ".join(ids)}]','  fps: 5','  loop: true','  mode: loop']
 path.write_text('\n'.join(lines+['@end'])+'\n',encoding='utf8')
 atlas.save(path.with_suffix('.png'))
 metadata={'meta':{'image':path.with_suffix('.png').name,'cellWidth':32,'cellHeight':32,'size':{'w':columns*32,'h':rows*32}},'frames':[{'name':name,'x':col*32,'y':row*32,'w':32,'h':32,'pivotX':.5,'pivotY':1/32} for cid,name,row,col,im in frames],'animations':[]}
 path.with_suffix('.pix2.json').write_text(json.dumps(metadata,indent=2),encoding='utf8')
 return atlas

def main():
 ROOT.mkdir(parents=True,exist_ok=True)
 master=make_master()
 write_pix2(ROOT/'silver_wanderer_master.pix2',[('master_right','master_right',0,0,master)],1,1)
 left=master.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
 write_pix2(ROOT/'silver_wanderer_views.pix2',[('view_right','view_right',0,0,master),('view_left','view_left',1,0,left)],1,2)
 preview=Image.new('RGB',(760,480),'#525f7d');d=ImageDraw.Draw(preview)
 d.text((24,16),'SILVER WANDERER / NATIVE 32 x 32 / NO ARMS',fill='#ffffff')
 for i,im in enumerate((master,left)):
  enlarged=im.resize((320,320),Image.Resampling.NEAREST)
  preview.paste(enlarged,(30+i*370,55),enlarged)
  preview.paste(im,(170+i*370,405),im)
 d.text((70,450),'RIGHT / 10x + 1x',fill='#ffffff');d.text((440,450),'LEFT / 10x + 1x',fill='#ffffff')
 preview.save(ROOT/'preview.png')
 reference=Image.open(ROOT.parent/'silver_wanderer_concept/silver_wanderer_v2_no_arms.png').convert('RGB')
 comparison=Image.new('RGB',(820,520),'#525f7d');d=ImageDraw.Draw(comparison)
 d.text((30,20),'APPROVED CONCEPT',fill='#ffffff');d.text((440,20),'NATIVE 32 x 32 RECONSTRUCTION',fill='#ffffff')
 crop=reference.crop((150,180,985,1130));crop.thumbnail((330,370),Image.Resampling.NEAREST);comparison.paste(crop,(30+(340-crop.width)//2,55))
 big=master.resize((352,352),Image.Resampling.NEAREST);comparison.paste(big,(430,55),big)
 comparison.paste(master,(580,440),master)
 d.text((445,490),'11x display above / actual 1x below',fill='#ffffff')
 d.text((30,490),'Source preserved. No new arms or accessories.',fill='#ffffff')
 comparison.save(ROOT/'comparison.png')
 print('Native 32 x 32 masters written; original concept and gameplay assets unchanged.')
if __name__=='__main__':main()
