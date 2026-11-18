from pathlib import Path
from PIL import Image
import json
root=Path(__file__).resolve().parents[1]
for slug in ('bell_wanderer','moon_mender','rune_warden'):
    atlas=Image.open(root/slug/(slug+'.png')).convert('RGBA')
    pixels=json.loads((root/slug/'parser_pixels.json').read_text())
    for cid,expected in pixels.items():
        _,row,col=cid.split('_'); row,col=int(row),int(col)
        for y in range(32):
            for x in range(32):
                rgba=atlas.getpixel((col*32+x,row*32+y))
                value=expected.get(f'{x},{y}')
                if value:
                    assert rgba==tuple(bytes.fromhex(value[1:])) if len(value)==9 else rgba==(*bytes.fromhex(value[1:]),255)
                else: assert rgba[3]==0
    print(slug+': all 32768 atlas pixels match official PIX2 compositor')
