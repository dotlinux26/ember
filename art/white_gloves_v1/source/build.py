from pathlib import Path
from PIL import Image,ImageDraw
import json,shutil
root=Path(__file__).resolve().parents[1]
palette={'o':'#171a2c','w':'#fffaf0','i':'#e1e8ed','g':'#aebbd1'}
def glove(grip=False):
 im=Image.new('RGBA',(16,16));d=ImageDraw.Draw(im)
 # Northwest cuff; rounded back of hand and curled fingers point southeast.
 d.polygon([(3,1),(7,1),(9,4),(8,7),(4,8),(1,4)],fill=palette['o'])
 d.polygon([(3,2),(6,2),(8,4),(7,6),(4,7),(2,4)],fill=palette['i'])
 d.polygon([(6,4),(10,4),(12,6),(12,8),(14,9),(14,11),(12,12),(11,14),(9,14),(8,15),(6,14),(4,14),(3,12),(2,10),(2,7),(4,5)],fill=palette['o'])
 d.polygon([(6,5),(10,5),(11,7),(11,9),(13,10),(13,11),(11,11),(10,13),(8,13),(8,14),(6,13),(4,13),(4,11),(3,10),(3,7),(5,6)],fill=palette['w'])
 # Four knuckles have short creases; the thumb remains separate on the right.
 d.line([(4,11),(5,12),(5,13)],fill=palette['g']);d.line([(6,12),(7,13)],fill=palette['g']);d.line([(9,11),(10,12)],fill=palette['g']);d.line([(11,8),(10,9),(11,10)],fill=palette['g']);d.line([(3,8),(3,10),(4,11)],fill=palette['i'])
 if grip:d.line([(8,8),(9,9),(11,9)],fill=palette['g']);d.point((11,10),fill=palette['o']);d.point((8,13),fill=palette['o'])
 return im
left=glove(False);right=glove(True)
sheet=Image.new('RGBA',(16,32));sheet.paste(left,(0,0));sheet.paste(right,(0,16));sheet.save(root/'white_gloves.png')
doc=['@pix2 1.1','@meta','  title: "White gloves / 1 column, 2 rows / five fingers"','  version: "1.1"','  color_mode: "RGBA"','  coordinate_origin: "top-left"','@grid','  columns: 1','  rows: 2','  default_cell: 16x16','  background: none','@palette']+[f'  {i+1}: {c}' for i,c in enumerate(palette.values())]
frames=[]
for r,(name,im) in enumerate([('glove_relaxed',left),('glove_grip',right)]):
 doc += [f'@cell {name}',f'  name: "{name}"','  direction: right','  frame: 0',f'  grid_pos: {r},0',f'@layer {name}_layer',f'  cell: {name}',f'@region {name}_fingers',f'  cell: {name}',f'  parent: {name}_layer','  tags: [hand, glove, fingers]']
 for y in range(16):
  pixels=[]
  for x in range(16):
   c=im.getpixel((x,y))
   if c[3]:pixels.append(f'p {x},{y} {list(palette.values()).index("#%02x%02x%02x"%c[:3])+1}')
  if pixels:doc.append('  '+' '.join(pixels))
 frames.append(dict(name=name,x=0,y=r*16,w=16,h=16,pivotX=.5,pivotY=.5))
(root/'white_gloves.pix2').write_text('\n'.join(doc+['@end'])+'\n')
(root/'white_gloves.pix2.json').write_text(json.dumps(dict(meta=dict(image='white_gloves.png',cellWidth=16,cellHeight=16,size=dict(w=16,h=32)),frames=frames,animations=[]),indent=2))
preview=Image.new('RGB',(480,520),'#343c55');d=ImageDraw.Draw(preview)
for row,im in enumerate([left,right]):
 big=im.resize((192,192),Image.Resampling.NEAREST);preview.paste(big,(24,40+row*230),big)
 d.text((245,110+row*230),'RELAXED / BACK' if row==0 else 'GRIP / FRONT',fill='#fffaf0');preview.paste(im,(245,145+row*230),im)
preview.save(root/'preview.png')
dest=root.parents[1]/'My project/Assets/Resources/Ember/Active';dest.mkdir(parents=True,exist_ok=True);shutil.copy2(root/'white_gloves.png',dest/'white_gloves.png')
print('Gloves: 1 column x 2 rows. Shared 3/4 direction, relaxed and grip poses.')
