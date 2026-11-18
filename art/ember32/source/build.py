"""Compact, deliberately clustered sprite designs, authored at native 32px.
The 64px tracing experiment is archived and is not used here.
"""
from pathlib import Path
import importlib.util
from PIL import Image, ImageDraw
import shutil
ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('exporter',ROOT.parents[1]/'art/bell_vessels/source/build_sprites.py')
exporter=importlib.util.module_from_spec(spec);spec.loader.exec_module(exporter)
PALETTE=['111222','26253e','454664','757e9b','bacbd5','e3e6dc','fff2d2','a36b42','e0a354','ffe09a','963f61','dc506c','ff858e','ffd1b2','1d7586','36cbd2','9af3e4','f5fff0','174b59','267e83','5eb7a6','c1a7e2','8766b1','4b396f']
COL={i+1:'#'+v for i,v in enumerate(PALETTE)}
# Each string is a row of integer-grid cells. Single outline, no antialiasing.
INK={'.':0,'o':1,'d':2,'s':3,'m':4,'g':5,'i':6,'w':7,'b':8,'a':9,'h':10,'r':11,'c':12,'p':13,'t':14,'e':15,'q':16,'l':17,'x':18,'v':19,'u':20,'j':21,'z':22,'k':23,'n':24}
HEADS=[
 [
  '........oooo......',
  '.......oahhao.....',
  '.......oaoodo.....',
  '.....ooowwwioo....',
  '....owwwwwwiigo...',
  '...owwwwwwiiiggo..',
  '..owwwwwwiiiiggo..',
  '.owwwwwiiiissggo..',
  'oowwwiiiiooooodoo.',
  '.oodddddddddddoo..',
  '..odllddddqxldo...',
  '..odllddddqlldo...',
  '...odddddddddo....',
  '....ooooooooo.....',
 ],
 [
  '..........oo......',
  '.........ojjo.....',
  '.......oojjjjo....',
  '......ojjjjuujo...',
  '.....ojjjjuuujo...',
  '....ojjjjuuuuugo..',
  '...ojjjjuuuugggo..',
  '..ojjjjuuuuggggo..',
  '.oouuuuuuuaaaaaoo.',
  '..ooiiitttttttooo.',
  '...oiitdttttdto...',
  '...oiitdttttdto...',
  '....oitttrttto....',
  '.....oooooooo.....',
 ],
 [
  '........oo........',
  '.......ohho.......',
  '....oooohhoooo....',
  '...ogggohhmmmgo...',
  '..ogggmohhmmmgg o.'.replace(' ',''),
  '..ogmmsoaasmmmggo.',
  '..ommmsoaasmmmmgo.',
  '..omsssoaasmmmmgo.',
  '..oaaaaaaaahaaaao.',
  '..oadddddddddddao.',
  '..oadqxldqxl ddao.'.replace(' ',''),
  '..oadqlldqlldddao.',
  '...oaddddddddao...',
  '....oooooooooo....',
 ]]

def sprite(role,state,f):
    parts=[];bob=[0,-1,0,0][f] if state=='run' else [0,0,-1,0][f] if state=='idle' else [0,1,-1,0][f] if state=='cast' else [0,1,0,0][f]
    def raster(name,rows,x,y,dx=0,dy=0):
        im=Image.new('RGBA',(32,32))
        for j,row in enumerate(rows):
            for i,key in enumerate(row):
                c=INK[key]
                if c:
                    xx,yy=x+i+dx,y+j+dy
                    assert 0<=xx<32 and 0<=yy<32,(name,xx,yy)
                    im.putpixel((xx,yy),tuple(bytes.fromhex(PALETTE[c-1]))+(255,))
        parts.append((name,im))
    # A six-pixel trailing ribbon, shifted one phase behind movement.
    tail=[0,1,0,-1][(f-1)%4]
    ribbon=['...oo..','..ocpo.','.ocpo..','ocpo...','ooo....']
    if role==1:ribbon=[s.replace('c','u').replace('p','j') for s in ribbon]
    if role==2:ribbon=[s.replace('c','a').replace('p','h') for s in ribbon]
    raster('scarf_tail',ribbon,4,18+tail)
    # Feet animate independently; both contact poses share y=28.
    step=[0,1,0,-1][f] if state=='run' else 0
    raster('rear_boot',['oooo','osso','oooo'],11,24-max(step,0))
    raster('front_boot',['oooo','osso','oooo'],18,24-max(-step,0))
    body=['..ooooooo..','.osssssddo.','osssmmssddo','odssmssdddo','oosssosddoo','.ooos.ooo..']
    if role==1:body=[s.replace('s','i').replace('m','u') for s in body]
    if role==2:body=['.ooooooooo.','ogssmassgdo','ogssmassgdo','odssssssddo','ooosssosdoo','..ooos.ooo.']
    raster('coat',body,10,19,dy=bob)
    head=list(HEADS[role])
    if state=='idle' and f==3:
        # Blink only cyan eyes; highlights do not become stray surface noise.
        head=[s.replace('l','d').replace('q','d') if j in (10,11) else s for j,s in enumerate(head)]
    raster('head',head,7,4,dy=bob)
    scarf=['.ooooooo.','ocpppppco','.occcco..','..oooo...']
    if role==1:scarf=[s.replace('p','l').replace('c','u') for s in scarf]
    if role==2:scarf=[s.replace('p','h').replace('c','a') for s in scarf]
    raster('collar',scarf,11,17,dy=bob)
    # No attached hands / weapons in base art. Runtime floating gloves are modular.
    if state=='cast' and f in (1,2):
        raster('cast_sigil',['..l..','.lxl.','lxlxl','.lxl.','..l..'],24,17,dy=-1 if f==2 else 0)
    if state=='hurt' and f==1:raster('impact',['p..','.p.','..p'],25,12)
    return parts

def main():
    exporter.ROOT=ROOT;exporter.PAL=PALETTE;exporter.COL=COL;exporter.sprite=sprite
    exporter.main()
    # Standalone design plate with native size and integer zoom.
    plate=Image.new('RGB',(1296,430),'#343e59');d=ImageDraw.Draw(plate)
    for role,(slug,title) in enumerate(exporter.ROLES):
        atlas=Image.open(ROOT/slug/(slug+'.png'))
        for side in range(2):
            im=atlas.crop((0,side*128,32,side*128+32));big=im.resize((192,192),Image.Resampling.NEAREST)
            plate.paste(big,(role*432+side*204+8,65),big)
            plate.paste(im,(role*432+side*60+30,290),im)
        d.text((role*432+15,25),title,fill='#fff1cd')
        d.text((role*432+15,350),'32px cell / native pixel clusters / 6x + 1x',fill='#dce4d9')
        active=ROOT.parents[1]/'My project/Assets/Resources/Ember/Active';active.mkdir(parents=True,exist_ok=True)
        shutil.copy2(ROOT/slug/(slug+'.png'),active/(slug+'.png'))
    plate.save(ROOT/'design_plate.png')
if __name__=='__main__':main()
