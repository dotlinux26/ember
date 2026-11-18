from pathlib import Path
from PIL import Image
import json
reports=json.loads(Path('Builds/pix2-raster-evidence.json').read_text(encoding='utf8'))
for report in reports:
 image=Image.open(report['png']).convert('RGBA');w,h=report['size']['w'],report['size']['h']
 assert image.size==(report['grid']['cols']*w,report['grid']['rows']*h)
 for cid,cell in report['raster'].items():
  for y in range(h):
   for x in range(w):
    actual=image.getpixel((cell['col']*w+x,cell['row']*h+y));expected=cell['pixels'].get(f'{x},{y}')
    if expected:
     channels=tuple(bytes.fromhex(expected[1:]));channels=channels if len(channels)==4 else (*channels,255)
     assert actual==channels,(report['png'],cid,x,y,actual,channels)
    else:assert actual[3]==0,(report['png'],cid,x,y,'unexpected opaque background')
 print(Path(report['png']).name+': every pixel matches official PIX2 compositor, physical rows verified')
