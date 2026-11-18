"""Ten native 48px shape studies. Editable integer-pixel sources, no upscaled 32px art."""
from pathlib import Path
from PIL import Image,ImageDraw
import json,hashlib,itertools

ROOT=Path(__file__).resolve().parents[1]
P={'O':'#09080e','a':'#24222f','b':'#363345','c':'#4b465d','j':'#615a73',
   'd':'#a99db6','e':'#d5cbdc','f':'#fbf7fa','s':'#ffcaad','t':'#ed9997',
   'h':'#ffddc1','m':'#ce6177','r':'#a12543','R':'#631630','x':'#bd2d40',
   'u':'#52617c','v':'#303b50','w':'#eae8ed','k':'#efb4ad','l':'#df7388'}
ROWS='''
................................................
................................................
...............OOOOOO...........................
..............OcccccOOO.........................
.............OcccacccccOOO......................
............OcccaacccccccvOOO...................
...........OOcccabccccccaaaaaOO.................
.........OOOcccaaccccaaaaaacccOOOOOOOOOOO.......
.......OOOcccccaacccaaaaccccaOOOcccccccbO.......
......OcccccccaaaaaaOcccccOOacccccccOOOOO.......
......OcccccccaaadeOcccccOccccccccOORRRRO.......
......OccaaccbaOdfOaccaaOcccccccOOORRRrRO.......
......OcaaaaaaaOdwaaOOOObcccccOOdddORrrO........
......OcaaaOOaaOdwjaeOabbccccOjjdeeOrrrO........
......OcaaOOOaOrddwedavvvbcOOeedeeefOrO.........
....OOOcaaO.OarrROOOavvvvvOeeeweefffOOa.........
...OaaacaaOOOrrOrOaaavvvvOeeeefffffffO..........
...OaaaaaO.OOOOrrOaavvvbOeeeefffdffffO..........
...OaaOOO..OaarrOOavvvOdweeeeffddffeffO.........
...OaO....OabOrrOvvvOOdwhweefffddffeffO.........
...OO....OabOrrrObOOwwwestweffdsdeeeefO.........
........OabbOrrrOjjwwwwhhhefwdhhdeeedfO.........
.......OabbaOrOOdddwRRRRRRwwdhORRReedfO.........
.......OaOOROOOdddwwweRfRRshhhaRReeddO..........
.......OORRROjjdddwwwwlfxhhhhhxxdedddO..........
........OORROdddddddwwlthhhhhhhtdddOO...........
..........OOOdeddedddwwthhhmmhhlddO.............
...........OdeeddwdewduulhhhhhOaeeO.............
...........OeedddwdwwdOucalOaOuOwfO.............
.........OOeedddwddwwOaauOaaauadwwO.............
.........OdedddwwdwwOabbauOjwOaOwO..............
.........OdddwwwwdwjOuuuuOaaauuuO...............
.....OOOOfwwwwwwdddcOOOOaaajwaOOO...............
....OfwfwffffwddddddOabbbbOOObbaO...............
...OwfeeeeffdddeeddOabbbbbOabbbbO...............
..OwfdjjeeddeeeeddjObbbbbuOaubbbbO..............
..OffjjjjeeeeedddjObbbbbuOOaOubbbOO.............
..OfjjjeeeeeeddjjOabbbbuOaOaavubbaO.............
..OwOOOeeeeeddjjObbbbbuOaaOaaaaubba.............
...O..OeeejdjjjObbbbburshhRlhhOuubb.............
...O..OeejjdjjOuuuuuuOkhhRRshhOROuu.............
......OedOjjjOOOOrrrObbbbOObbbOrrrO.............
.......OdOOOOO.OrrrrOaddaOObdjOrrO..............
........OO.bbv.OrrOOOaaaO.OaaaOrO...............
........cc.....OrO..OabbO.OabbOO................
...............OO...OOOOO.OOOOO.................
................................................
................................................
'''.strip().splitlines()

DESIGNS=[
 ('01_crescent','CRESCENT WITCH','original','flow','split','smile'),
 ('02_curled_hat','CURL-TIP WITCH','curl','bob','poncho','smile'),
 ('03_moon_beret','MOON BERET','beret','braid','short','smile'),
 ('04_lunar_hood','LUNAR HOOD','hood','locks','poncho','smile'),
 ('05_droop_cap','SLEEPY MAGE','droop','pony','split','soft'),
 ('06_twintail','TWIN-TAIL WITCH','smallwitch','twins','short','wink'),
 ('07_moon_crown','MOON PRINCESS','crown','bob','bell','smile'),
 ('08_traveler','NIGHT TRAVELER','travel','braid','long','soft'),
 ('09_ribbon','RIBBON SCOUT','ribbon','pony','short','smile'),
 ('10_fox_hood','MOON FOX','fox','locks','bell','wink'),
]

def poly(d,pts,c,outline=None):d.polygon(pts,fill=P[c],outline=P[outline] if outline else None)
def rect(d,box,c):d.rectangle(box,fill=P[c])
def line(d,pts,c,w=1):d.line(pts,fill=P[c],width=w)

def raw():
 im=Image.new('RGBA',(48,48))
 assert len(ROWS)==48
 for y,row in enumerate(ROWS):
  assert len(row)==48,(y,len(row))
  for x,k in enumerate(row):
   if k!='.':im.putpixel((x,y),tuple(bytes.fromhex(P[k][1:]))+(255,))
 return im

def moon(d,x,y):
 # Deliberate 5 x 6 crescent on the hat: no anti-alias or subpixel shading.
 for dy,row in enumerate(('..ff.','.fe..','ff...','fe...','.fe..','..fff')):
  for dx,k in enumerate(row):
   if k!='.':rect(d,(x+dx,y+dy,x+dx,y+dy),k)

def hair(im,kind):
 d=ImageDraw.Draw(im)
 if kind=='flow':return
 # Clear the long trailing silhouette before creating a different hairstyle.
 d.rectangle((0,26,20,47),fill=(0,0,0,0))
 if kind=='bob':
  poly(d,[(19,20),(23,21),(24,27),(22,32),(17,31),(15,29),(16,25)],'d','O')
  poly(d,[(19,22),(21,23),(21,28),(18,30),(17,29),(18,25)],'f')
  line(d,[(22,25),(22,29),(20,30)],'e')
 elif kind=='braid':
  poly(d,[(19,21),(23,22),(21,27),(18,30),(16,33),(14,36),(12,36),(12,33),(15,29),(17,26)],'e','O')
  for x,y in ((18,27),(16,30),(14,33)):
   poly(d,[(x,y-1),(x+2,y),(x+1,y+2),(x-1,y+1)],'f','d')
  poly(d,[(12,35),(15,36),(14,38),(12,39),(11,38)],'r','O')
 elif kind=='pony':
  poly(d,[(17,17),(20,19),(18,24),(18,28),(15,30),(13,34),(9,35),(10,32),(12,30),(12,26),(14,22)],'d','O')
  poly(d,[(16,19),(18,20),(16,24),(16,28),(13,30),(11,33),(13,28),(13,25)],'f')
  line(d,[(17,23),(17,27),(14,29)],'e')
  rect(d,(16,19,19,20),'r')
 elif kind=='twins':
  for flip in (False,True):
   pts=[(17,20),(20,22),(18,26),(19,31),(16,33),(14,31),(15,27),(14,24)]
   if flip:pts=[(51-x,y) for x,y in pts]
   poly(d,pts,'e','O')
   pts=[(16,23),(17,24),(16,27),(17,30),(16,31),(15,30)]
   if flip:pts=[(51-x,y) for x,y in pts]
   poly(d,pts,'f')
  rect(d,(16,21,19,22),'r');rect(d,(32,21,35,22),'r')
 else:
  poly(d,[(19,20),(23,21),(23,25),(21,29),(20,34),(17,35),(16,33),(18,29),(17,25)],'d','O')
  poly(d,[(20,22),(21,23),(21,26),(19,30),(19,33),(18,33),(19,28)],'f')
  poly(d,[(33,21),(36,21),(37,27),(35,31),(35,34),(33,33),(32,29)],'e','O')
  line(d,[(35,23),(35,27),(34,30)],'f')

def coat(im,kind):
 d=ImageDraw.Draw(im)
 if kind not in ('split','long'):
  d.rectangle((19,29,38,40),fill=(0,0,0,0))
  if kind=='poncho':
   poly(d,[(24,28),(31,28),(33,31),(36,38),(32,40),(28,39),(24,40),(18,38),(21,32)],'a','O')
   poly(d,[(22,31),(24,30),(26,32),(22,37),(20,37)],'v')
   line(d,[(29,30),(32,36),(34,37)],'u')
  elif kind=='bell':
   poly(d,[(24,29),(31,29),(32,33),(35,39),(32,40),(29,39),(27,40),(24,39),(21,40),(19,39),(22,34)],'a','O')
   poly(d,[(23,34),(25,34),(23,38),(21,38)],'v')
   poly(d,[(29,34),(31,34),(33,38),(30,38)],'v')
   line(d,[(23,33),(31,33)],'d')
  else:
   poly(d,[(24,28),(31,28),(33,31),(32,35),(34,38),(30,39),(27,37),(24,39),(21,38),(22,34),(21,31)],'a','O')
   poly(d,[(23,30),(25,30),(25,33),(23,35),(22,34)],'v')
   poly(d,[(30,30),(31,31),(31,34),(33,37),(30,37)],'u')
   line(d,[(24,34),(30,34)],'d')
 # Collar and the same silver fastening language bind the whole family.
 poly(d,[(24,27),(27,28),(30,27),(33,28),(31,30),(28,29),(25,30),(23,29)],'v','O')
 line(d,[(24,28),(26,29)],'u');line(d,[(31,28),(30,29)],'u')
 rect(d,(28,30,29,31),'w');rect(d,(29,30,29,30),'f')
 if kind in ('split','long'):rect(d,(28,33,29,34),'w')
 # Short chibi feet. No long exposed shins, no tall heavy boots.
 d.rectangle((20,40,34,47),fill=(0,0,0,0))
 for x in (22,29):
  rect(d,(x+1,39,x+3,40),'s')
  poly(d,[(x,41),(x+4,41),(x+5,43),(x+4,44),(x,44),(x-1,43)],'a','O')
  rect(d,(x+1,41,x+3,41),'c');rect(d,(x+1,42,x+2,42),'b')
  rect(d,(x+3,43,x+4,43),'v')

def face(im,expression):
 d=ImageDraw.Draw(im)
 # Replace the gaunt, over-quantized face with a contiguous rounded cheek.
 poly(d,[(24,20),(28,19),(32,20),(34,22),(34,25),(32,27),(30,28),(27,28),(24,26),(23,23)],'s')
 poly(d,[(25,21),(30,20),(32,21),(33,23),(32,26),(29,27),(26,26),(24,24)],'h')
 # Fringe stays asymmetric and overlaps the forehead only.
 poly(d,[(24,18),(27,17),(29,18),(28,20),(26,22),(24,22),(23,23),(22,22)],'f')
 poly(d,[(31,18),(34,18),(35,20),(35,25),(33,27),(32,26),(33,23),(32,22),(30,22),(30,20)],'e')
 line(d,[(34,20),(34,24),(33,25)],'f')
 line(d,[(29,19),(28,21),(27,22)],'d')
 # Friendly eyes: raised outside corners, red irises and a white glint.
 line(d,[(23,22),(24,23),(26,23)],'R')
 if expression=='wink':
  line(d,[(30,24),(31,23),(33,23)],'R')
 else:
  line(d,[(30,23),(32,23),(33,22)],'R')
  rect(d,(31,24,32,26),'x');rect(d,(31,24,31,24),'f');rect(d,(32,26,32,26),'m')
 rect(d,(24,24,25,26),'x');rect(d,(24,24,24,24),'f');rect(d,(25,26,25,26),'m')
 rect(d,(23,26,24,26),'k');rect(d,(32,26,33,26),'k')
 if expression=='soft':rect(d,(28,27,29,27),'m')
 else:line(d,[(27,26),(28,27),(29,27),(30,26)],'m')

def headgear(im,kind):
 if kind=='original':return
 d=ImageDraw.Draw(im)
 # Erase the existing hat, retaining its adjacent hair and face.
 d.rectangle((0,0,47,14),fill=(0,0,0,0))
 for y,right in ((15,24),(16,24),(17,23),(18,21),(19,20),(20,18),(21,17),(22,15),(23,13),(24,12),(25,11)):
  d.line([(0,y),(right,y)],fill=(0,0,0,0))
 # A continuous silver cap beneath the replacement hat avoids orphaned
 # hair spikes left over from the reference's much wider original brim.
 poly(d,[(22,20),(23,17),(26,15),(30,14),(34,16),(36,19),(36,23),(34,25),(34,21),(31,20),(29,21),(27,20),(25,22),(23,22)],'e','O')
 poly(d,[(24,18),(27,16),(30,16),(32,17),(30,18),(28,18),(26,21),(24,21)],'f')
 if kind in ('curl','smallwitch','droop'):
  pts=([(12,10),(14,5),(19,4),(23,7),(24,11),(29,15),(34,16),(36,19),(30,20),(22,19),(16,20),(10,19),(8,17),(10,15)] if kind=='curl' else
       [(13,14),(16,10),(19,6),(23,8),(24,12),(29,15),(33,17),(32,19),(21,20),(11,19),(9,17)] if kind=='smallwitch' else
       [(10,14),(12,8),(16,5),(23,4),(29,6),(32,9),(35,10),(36,13),(33,14),(29,12),(26,12),(29,17),(33,18),(32,20),(21,21),(10,20),(8,18)])
  poly(d,pts,'b','O')
  poly(d,[(14,12),(17,8),(19,7),(21,10),(22,14),(25,16),(17,16),(12,17)],'c')
  poly(d,[(10,17),(18,16),(26,16),(32,18),(25,19),(17,18)],'a')
  line(d,[(15,15),(23,15),(27,16)],'r')
  if kind=='curl':poly(d,[(14,6),(10,5),(10,2),(13,1),(15,2),(13,3),(14,4),(18,5)],'c','O')
  moon(d,18,10)
 elif kind=='beret':
  poly(d,[(12,12),(14,8),(19,6),(26,6),(31,8),(34,12),(33,16),(28,18),(18,19),(11,17),(10,15)],'b','O')
  poly(d,[(14,11),(19,8),(25,8),(28,9),(26,11),(19,12),(14,14)],'c')
  line(d,[(13,16),(22,17),(31,15)],'a',2);moon(d,27,9)
 elif kind in ('hood','fox'):
  poly(d,[(15,18),(16,11),(20,6),(27,4),(32,6),(36,11),(39,19),(38,28),(35,31),(33,29),(34,24),(34,19),(30,16),(24,17),(20,21),(20,29),(17,32),(14,29)],'a','O')
  poly(d,[(18,18),(19,12),(23,8),(28,7),(32,9),(35,14),(36,20),(34,18),(30,15),(23,15)],'b')
  line(d,[(18,23),(18,28),(16,29)],'u');line(d,[(36,23),(36,28),(35,29)],'u')
  if kind=='fox':
   poly(d,[(18,11),(16,3),(18,2),(24,7)],'b','O');poly(d,[(30,7),(35,2),(37,2),(36,12)],'b','O')
   poly(d,[(18,5),(19,9),(21,7)],'d');poly(d,[(34,6),(32,8),(35,10)],'d')
  else:moon(d,24,8)
 elif kind=='crown':
  # Round cap of hair exposed above a small crescent tiara.
  poly(d,[(16,20),(17,13),(20,9),(25,7),(30,8),(34,11),(36,16),(35,21),(32,18),(24,18),(20,21)],'e','O')
  poly(d,[(19,14),(22,10),(27,9),(30,11),(28,13),(23,13),(20,17)],'f')
  poly(d,[(20,12),(20,8),(23,10),(26,6),(28,10),(32,8),(32,12),(27,14),(23,14)],'d','O')
  line(d,[(21,11),(25,12),(30,11)],'w');rect(d,(26,10,27,11),'r')
 elif kind=='travel':
  poly(d,[(14,15),(16,9),(22,6),(29,6),(32,10),(33,14),(39,15),(40,17),(34,20),(24,21),(14,20),(9,18),(9,16)],'b','O')
  poly(d,[(17,12),(20,8),(26,8),(29,10),(30,13),(22,14),(16,15)],'c')
  line(d,[(15,15),(23,16),(32,14)],'r',2);moon(d,23,9)
 elif kind=='ribbon':
  poly(d,[(15,20),(16,14),(20,9),(26,7),(31,8),(35,11),(37,16),(36,23),(34,23),(32,19),(24,18),(20,21)],'d','O')
  poly(d,[(18,15),(21,11),(27,9),(31,10),(29,13),(25,14),(21,18),(18,19)],'f')
  poly(d,[(15,11),(11,8),(10,10),(11,14),(15,15),(18,13),(20,10),(19,8)],'r','O')
  rect(d,(15,11,17,13),'w');poly(d,[(14,14),(12,20),(15,18),(17,21),(18,14)],'R','O')

def make(spec):
 ident,label,hat,hairkind,coatkind,expression=spec
 im=raw();hair(im,hairkind);coat(im,coatkind);face(im,expression);headgear(im,hat)
 # Source artifact removal: disjoint samples beneath the trailing hair.
 for y in range(43,48):
  for x in range(0,14):im.putpixel((x,y),(0,0,0,0))
 # Keep the connected silhouette; small isolated import-analysis specks
 # are not character features. Diagonal 8-connectivity preserves curls.
 remaining={(x,y) for y in range(48) for x in range(48) if im.getpixel((x,y))[3]}
 components=[]
 while remaining:
  start=remaining.pop();part={start};queue=[start]
  while queue:
   x,y=queue.pop()
   for dx,dy in itertools.product((-1,0,1),repeat=2):
    point=(x+dx,y+dy)
    if point in remaining:remaining.remove(point);part.add(point);queue.append(point)
  components.append(part)
 largest=max(components,key=len)
 for part in components:
  if part is not largest:
   for point in part:im.putpixel(point,(0,0,0,0))
 return im

def export(path,frames,cols,rows):
 colors={c:i+1 for i,c in enumerate(P.values())}
 lines=['@pix2 1.1','@meta','  title: "Silver Wanderer / ten 48px shape studies"','  version: "1.1"','  color_mode: "RGBA"','  coordinate_origin: "top-left"','@grid',f'  columns: {cols}',f'  rows: {rows}','  default_cell: 48x48','  background: none','@palette']+[f'  {i}: {c}' for c,i in colors.items()]
 atlas=Image.new('RGBA',(cols*48,rows*48));meta=[]
 for cid,name,row,col,im in frames:
  atlas.paste(im,(col*48,row*48));meta.append(dict(name=name,x=col*48,y=row*48,w=48,h=48,pivotX=.5,pivotY=3/48))
  lines += [f'@cell {cid}',f'  name: "{name}"',f'  direction: {"left" if name.endswith("left") else "right"}','  frame: 0',f'  grid_pos: {row},{col}']
  groups={k:[] for k in ('outline','dark_fabric','silver','skin','wine')}
  for y in range(48):
   for x in range(48):
    c=im.getpixel((x,y))
    if not c[3]:continue
    rgb='#%02x%02x%02x'%c[:3];k=next(k for k,v in P.items() if v==rgb)
    g='outline' if k=='O' else 'silver' if k in 'defw' else 'skin' if k in 'sthmkl' else 'wine' if k in 'rRx' else 'dark_fabric'
    groups[g].append(f'p {x},{y} {colors[rgb]}')
  for group,records in groups.items():
   lines += [f'@layer {cid}_{group}_layer',f'  cell: {cid}',f'  name: "{group}"',f'@region {cid}_{group}',f'  cell: {cid}',f'  parent: {cid}_{group}_layer',f'  tags: [body, no_arms, {group}]']
   lines += ['  '+' '.join(records[i:i+16]) for i in range(0,len(records),16)]
 path.write_text('\n'.join(lines+['@end'])+'\n',encoding='utf8');atlas.save(path.with_suffix('.png'))
 path.with_suffix('.pix2.json').write_text(json.dumps(dict(meta=dict(image=path.with_suffix('.png').name,cellWidth=48,cellHeight=48,size=dict(w=cols*48,h=rows*48)),frames=meta,animations=[]),indent=2))

def main():
 ROOT.mkdir(exist_ok=True,parents=True);frames=[];images=[]
 plate=Image.new('RGB',(1250,690),'#525f7d');d=ImageDraw.Draw(plate)
 d.text((24,15),'SILVER HAIR / BLACK CLOTHES / 10 NATIVE 48 x 48 SHAPE STUDIES',fill='#ffffff')
 d.text((24,34),'Short chibi boots, hidden arms. Top: 4x; lower miniature: actual 1x. Static design studies.',fill='#d5cbdc')
 for i,spec in enumerate(DESIGNS):
  ident,label,*_=spec;im=make(spec);left=im.transpose(Image.Transpose.FLIP_LEFT_RIGHT);images.append(im)
  dest=ROOT/ident;dest.mkdir(exist_ok=True)
  export(dest/(ident+'.pix2'),[(ident+'_right',ident+'_right',0,0,im),(ident+'_left',ident+'_left',1,0,left)],1,2)
  im.save(dest/'master_right.png');left.save(dest/'master_left.png')
  frames.append((ident,ident+'_right',i//5,i%5,im))
  x=10+(i%5)*248;y=65+(i//5)*310
  d.rounded_rectangle((x,y,x+236,y+294),radius=8,fill='#48546f')
  big=im.resize((192,192),Image.Resampling.NEAREST);plate.paste(big,(x+22,y+7),big)
  d.text((x+15,y+205),f'{i+1:02}  {label}',fill='#ffffff')
  plate.paste(im,(x+45,y+234),im);plate.paste(left,(x+137,y+234),left)
  solo=Image.new('RGB',(500,330),'#525f7d');sd=ImageDraw.Draw(solo);sd.text((18,12),label+' / 48px',fill='#ffffff')
  for side,pic in enumerate((im,left)):
   big=pic.resize((240,240),Image.Resampling.NEAREST);solo.paste(big,(10+240*side,35),big);solo.paste(pic,(105+240*side,280),pic)
  solo.save(dest/'preview.png')
 plate.save(ROOT/'designs_10.png');export(ROOT/'all_10.pix2',frames,5,2)
 assert len({im.tobytes() for im in images})==10
 distances={f'{a+1:02}-{b+1:02}':sum(p!=q for p,q in zip(images[a].get_flattened_data(),images[b].get_flattened_data())) for a,b in itertools.combinations(range(10),2)}
 (ROOT/'geometry_evidence.json').write_text(json.dumps({'nativeCell':[48,48],'designs':10,'uniqueImages':10,'pairwiseDifferentPixels':distances,'minimumDifferentPixels':min(distances.values()),'staticPoses':True},indent=2))
 print('10 native 48px designs, each with 2 directions. Geometry differences:',min(distances.values()),'pixels minimum. No gameplay assets replaced.')
if __name__=='__main__':main()
