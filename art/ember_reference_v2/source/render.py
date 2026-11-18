from pathlib import Path
from PIL import Image,ImageDraw
import json,shutil
root=Path(__file__).resolve().parents[1]
roles=['bell_wanderer','moon_mender','bomb_trapper','rune_warden','space_wraith']
plate=Image.new('RGB',(1280,720),'#343c55');d=ImageDraw.Draw(plate)
for i,slug in enumerate(roles):
 folder=root/slug;pixels=json.loads((folder/'pixels.json').read_text());atlas=Image.new('RGBA',(128,256))
 for cid,points in pixels.items():
  _,r,c=cid.split('_');r,c=int(r),int(c)
  for xy,color in points.items():
   x,y=map(int,xy.split(','));values=tuple(bytes.fromhex(color[1:]));atlas.putpixel((c*32+x,r*32+y),values if len(values)==4 else (*values,255))
 atlas.save(folder/(slug+'.png'))
 eyes=Image.new('RGBA',(128,256))
 for cid,points in json.loads((folder/'eyes_pixels.json').read_text()).items():
  _,r,c=cid.split('_');r,c=int(r),int(c)
  for xy,color in points.items():
   x,y=map(int,xy.split(','));values=tuple(bytes.fromhex(color[1:]));eyes.putpixel((c*32+x,r*32+y),values if len(values)==4 else (*values,255))
 eyes.save(folder/(slug+'_eyes.png'))
 for side in range(2):
  for s,state in enumerate(['idle','run','cast','hurt']):
   frames=[]
   for f in range(4):
    tile=atlas.crop((f*32,(side*4+s)*32,f*32+32,(side*4+s+1)*32)).resize((192,192),Image.Resampling.NEAREST)
    bg=Image.new('RGB',tile.size,'#343c55');bg.paste(tile,(0,0),tile);frames.append(bg)
   frames[0].save(folder/(state+('_left' if side else '_right')+'.gif'),save_all=True,append_images=frames[1:],duration=100 if s==1 else 160,loop=0 if s<2 else 1,disposal=2)
   if s==0:
    plate.paste(frames[0],(i*256+side*40,40+side*290))
 d.text((i*256+12,15),slug,fill='#fff2d8')
 active=root.parents[1]/'My project/Assets/Resources/Ember/Active';active.mkdir(parents=True,exist_ok=True)
 shutil.copy2(folder/(slug+'.png'),active/(slug+'.png'))
 shutil.copy2(folder/(slug+'_eyes.png'),active/(slug+'_eyes.png'))
plate.save(root/'design_plate.png')
print('Rendered active native 32px reference-derived character sheets.')
