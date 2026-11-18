"""Lyra: an original cute fantasy adventurer, native 32px, row-major animation.
No bell template pixels are used. Integer geometry, filled semantic layers.
"""
from pathlib import Path
from PIL import Image,ImageDraw
import json,shutil
root=Path(__file__).resolve().parents[1]
colors=['#19182c','#33304e','#575477','#8581ac','#b8b9de','#e8e5f5','#fff8e7','#d89996','#f2b8a4','#ffddbd','#f483a0','#ad496d','#175064','#267c86','#54b9b3','#a5e8cf','#aa783e','#e9ba66','#ffe3a3','#72dded','#edffff','#8d67ac']
C={i+1:c for i,c in enumerate(colors)}
clips=[('idle',6,True),('run',10,True),('cast',12,False),('hurt',10,False)]

def frame(state,f):
 parts=[]
 bob=([0,0,0,-1,-1,0,0,0][f] if state=='idle' else [0,0,-1,-1,0,0,-1,-1][f] if state=='run' else [0,0,-1,-1,-1,-1,0,0][f] if state=='cast' else [0,1,1,0,0,0,0,0][f])
 sway=[0,0,1,1,0,0,-1,-1][(f-1)%8]
 def layer(name,paint,dx=0,dy=0,body=True):
  im=Image.new('RGBA',(32,32));d=ImageDraw.Draw(im)
  def p(points,c,o=None):d.polygon(points,fill=C[c],outline=C[o] if o else None)
  def l(points,c,w=1):d.line(points,fill=C[c],width=w)
  def b(bounds,c):d.rectangle(bounds,fill=C[c])
  paint(p,l,b)
  if body:dy+=bob
  shifted=Image.new('RGBA',(32,32));shifted.paste(im,(dx,dy));parts.append((name,shifted))
 # Long hair frames the head and remains behind the costume, delayed by one phase.
 layer('hair_back',lambda p,l,b:(p([(10,5),(16,3),(22,5),(25,9),(25,17),(26,21),(23,24),(20,22),(20,17),(13,17),(12,22),(9,25),(5,24),(7,20),(6,17),(7,10)],3,1),p([(10,7),(13,6),(12,13),(10,17),(10,21),(7,23),(8,17),(8,11)],5),l([(10,8),(9,13),(9,17)],6),p([(22,8),(24,10),(23,17),(24,21),(22,22),(22,16)],4)),dx=sway if state=='run' else 0)
 layer('cape',lambda p,l,b:(p([(11,18),(20,17),(23,20),(24,25),(22,27),(18,25),(13,27),(8,26),(9,23)],13,1),p([(11,20),(14,20),(13,24),(10,25)],14),p([(20,20),(21,23),(23,25),(20,25),(18,23)],15)),dx=0,dy=sway//2)
 # Boots are independent from the body and have two clear ground-contact poses.
 if state=='run':
  rear_x=[12,13,14,14,12,12,13,13][f];rear_y=[27,26,25,25,27,27,27,26][f]
  front_x=[19,19,18,18,19,18,17,17][f];front_y=[27,27,27,26,27,26,25,25][f]
 else:rear_x,rear_y,front_x,front_y=12,27,19,27
 layer('rear_boot',lambda p,l,b:(p([(rear_x,rear_y),(rear_x+3,rear_y),(rear_x+3,rear_y+2),(rear_x-1,rear_y+2),(rear_x-1,rear_y+1)],2,1),b((rear_x,rear_y,rear_x+2,rear_y),4)),body=False)
 layer('front_boot',lambda p,l,b:(p([(front_x,front_y),(front_x+3,front_y),(front_x+3,front_y+2),(front_x-1,front_y+2),(front_x-1,front_y+1)],2,1),b((front_x,front_y,front_x+2,front_y),18)),body=False)
 layer('dress',lambda p,l,b:(p([(12,18),(19,18),(21,21),(20,23),(23,26),(19,27),(16,26),(13,27),(10,26),(11,23),(10,21)],14,1),p([(13,19),(18,19),(19,22),(17,24),(13,23),(12,21)],7),p([(12,24),(15,24),(14,26),(11,25)],15),p([(17,24),(20,24),(21,26),(18,26)],15),l([(13,23),(19,23)],18),b((16,23,17,23),19),l([(18,20),(18,21)],16)))
 layer('face',lambda p,l,b:(p([(13,8),(20,8),(23,10),(24,13),(23,16),(20,18),(16,18),(13,16),(12,12)],9,1),p([(15,9),(20,9),(22,11),(23,13),(22,15),(20,17),(16,17),(14,15),(14,11)],10),l([(13,12),(13,15),(15,17)],8),b((15,15,15,15),11),b((22,15,22,15),11),b((19,16,19,16),12)))
 layer('hair_fringe',lambda p,l,b:(p([(11,4),(15,3),(20,3),(23,5),(24,8),(22,10),(19,9),(17,11),(15,10),(13,13),(12,16),(10,15),(9,11),(8,10),(9,6)],4,1),p([(11,5),(16,4),(20,4),(22,6),(20,7),(18,7),(16,9),(14,8),(12,11),(11,14),(10,12),(10,8)],5),p([(12,5),(16,4),(19,4),(18,6),(15,6),(14,8),(12,8)],6),p([(20,6),(22,7),(21,9),(19,8)],6),l([(13,10),(12,12),(12,14)],4)))
 def eyes(p,l,b):
  blink=state=='idle' and f==6 or state=='cast' and f in (2,3)
  if blink:l([(15,13),(17,13)],2);l([(21,12),(23,12)],2)
  else:
   l([(15,11),(17,11)],2);b((16,12,17,14),13);b((16,12,16,12),21);b((17,13,17,13),20)
   l([(21,10),(23,11)],2);b((21,12,23,14),13);b((22,12,22,13),20);b((21,12,21,12),21)
 layer('eyes',eyes)
 layer('moon_clip',lambda p,l,b:(p([(23,4),(24,5),(23,7),(25,7),(24,9),(22,8),(21,6)],17,1),p([(23,5),(22,6),(23,8),(24,8)],19),b((24,4,24,4),7)))
 layer('collar',lambda p,l,b:(p([(13,17),(17,18),(20,17),(22,18),(20,20),(17,20),(15,19),(12,19)],13,1),l([(14,18),(17,19),(20,18)],16),b((17,19,18,20),18),b((18,19,18,19),7)))
 if state=='cast' and f in (2,3,4,5):
  height=[0,0,0,-1,-2,-1,0,0][f]
  layer('casting_glove',lambda p,l,b:(p([(23,20+height),(25,19+height),(27,20+height),(27,22+height),(25,23+height),(23,22+height)],6,1),b((24,20+height,25,21+height),7)))
  layer('cast_moon',lambda p,l,b:(p([(27,10),(29,12),(28,14),(29,15),(27,16),(25,15),(24,13),(25,11)],15,1),p([(27,11),(25,13),(26,15),(28,15),(27,13),(28,11)],21),b((30,10,30,10),19),b((24,9,24,9),20)))
 if state=='hurt' and f in (1,2):layer('impact',lambda p,l,b:(l([(27,9),(29,7)],11),l([(28,12),(30,12)],19)),body=False)
 return parts

def main():
 root.mkdir(exist_ok=True);doc=['@pix2 1.1','@meta','  title: "Lyra / Moon Scout / original female character"','  author: "Ember"','  version: "1.1"','  color_mode: "RGBA"','  coordinate_origin: "top-left"','@grid','  columns: 8','  rows: 8','  default_cell: 32x32','  background: none','@palette']+[f'  {k}: {c}' for k,c in C.items()]
 atlas=Image.new('RGBA',(256,256));eyes_atlas=Image.new('RGBA',(256,256));animations=[];frame_meta=[];clip_meta=[]
 for side in range(2):
  direction='right' if side==0 else 'left'
  for s,(state,fps,loop) in enumerate(clips):
   r=side*4+s;ids=[];gif=[]
   for f in range(8):
    cid=f'cell_{r}_{f}';ids.append(cid);im=Image.new('RGBA',(32,32));eye=Image.new('RGBA',(32,32))
    doc += [f'@cell {cid}',f'  name: "{state}_{direction}_{f}"',f'  direction: {direction}',f'  frame: {f}',f'  grid_pos: {r},{f}']
    for name,part in frame(state,f):
     if side:part=part.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
     im=Image.alpha_composite(im,part)
     if name=='eyes':eye=Image.alpha_composite(eye,part)
     lid=cid+'_'+name+'_layer';rid=cid+'_'+name
     doc += [f'@layer {lid}',f'  cell: {cid}',f'  name: "{name}"','  opacity: 1','  blend: normal',f'@region {rid}',f'  cell: {cid}',f'  parent: {lid}',f'  tags: [{name}]']
     for y in range(32):
      records=[]
      for x in range(32):
       c=part.getpixel((x,y))
       if c[3]:records.append(f'p {x},{y} {colors.index("#%02x%02x%02x"%c[:3])+1}')
      if records:doc.append('  '+' '.join(records))
    atlas.paste(im,(f*32,r*32));eyes_atlas.paste(eye,(f*32,r*32));frame_meta.append(dict(name=f'{state}_{direction}_{f}',x=f*32,y=r*32,w=32,h=32,pivotX=.5,pivotY=1/32))
    bg=Image.new('RGB',(192,192),'#343c55');big=im.resize((192,192),Image.Resampling.NEAREST);bg.paste(big,(0,0),big);gif.append(bg)
   gif[0].save(root/f'{state}_{direction}.gif',save_all=True,append_images=gif[1:],duration=round(1000/fps),loop=0 if loop else 1,disposal=2)
   animations += [f'@animation {state}_{direction}',f'  name: "{state}_{direction}"',f'  direction: {direction}',f'  frames: [{", ".join(ids)}]',f'  fps: {fps}',f'  loop: {str(loop).lower()}',f'  mode: {"loop" if loop else "once"}']
   clip_meta.append(dict(clipName=f'{state}_{direction}',fps=fps,loop=loop,frameNames=[f'{state}_{direction}_{f}' for f in range(8)]))
 (root/'lyra_moonscout.pix2').write_text('\n'.join(doc+animations+['@end'])+'\n',encoding='utf8');atlas.save(root/'lyra_moonscout.png');eyes_atlas.save(root/'lyra_moonscout_eyes.png')
 meta=dict(meta=dict(image='lyra_moonscout.png',cellWidth=32,cellHeight=32,size=dict(w=256,h=256)),frames=frame_meta,animations=clip_meta)
 (root/'lyra_moonscout.pix2.json').write_text(json.dumps(meta,indent=2));plate=Image.new('RGB',(960,610),'#343c55');d=ImageDraw.Draw(plate)
 d.text((24,16),'LYRA / MOON SCOUT - ORIGINAL 32px FEMALE CHARACTER',fill='#fff8e7')
 for side in range(2):
  im=atlas.crop((0,side*128,32,side*128+32));big=im.resize((256,256),Image.Resampling.NEAREST);plate.paste(big,(50+side*330,45),big);plate.paste(im,(70+side*330,325),im)
 for r in range(4):
  d.text((15,390+r*48),clips[r][0].upper(),fill='#a5e8cf')
  for f in range(8):
   im=atlas.crop((f*32,r*32,f*32+32,r*32+32)).resize((48,48),Image.Resampling.NEAREST);plate.paste(im,(130+f*75,375+r*48),im)
 plate.save(root/'design_plate.png')
 destination=root.parents[1]/'My project/Assets/Resources/Ember/Active';destination.mkdir(parents=True,exist_ok=True)
 for name in ['lyra_moonscout.png','lyra_moonscout_eyes.png','lyra_moonscout.pix2.json']:shutil.copy2(root/name,destination/name)
 print('Lyra: 8 columns x 8 rows. Each clip occupies exactly one horizontal row.')
if __name__=='__main__':main()
