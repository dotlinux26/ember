"""Deterministic 32px, region-separated PIX2 artwork. Run with Python + Pillow."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import json, shutil

ROOT = Path(__file__).resolve().parents[1]
PAL = ['101020','24223c','393455','514975','706792','a89bb3','d1c1cb','eadcce','fff2d8','b27e60','e4b980','9a334f','d64a65','fa7180','ffab99','18708a','23bad5','85edee','e9ffff','325a64','488a83','86c5aa','f2d487','99794c']
COL = {i+1: '#'+v for i,v in enumerate(PAL)}
ROLES = [('bell_wanderer','LU HANH CHUONG'),('moon_mender','Y SU ANH TRANG'),('rune_warden','HO VE CO NGU')]
STATES = [('idle',6,True),('run',10,True),('cast',9,False),('hurt',8,False)]

def sprite(role,state,f):
    if role==0:
        return faithful_bell(state,f)
    parts=[]
    bob = ([0,-1,0,0][f] if state=='run' else [0,0,-1,0][f] if state=='idle' else [0,1,-1,0][f] if state=='cast' else [0,1,1,0][f])
    shift = -1 if state=='hurt' and f in (1,2) else 0
    def region(name, paint, moving=True):
        im=Image.new('RGBA',(32,32)); d=ImageDraw.Draw(im)
        def poly(points,c,outline=1): d.polygon(points,fill=COL[c],outline=COL[outline] if outline else None)
        def line(points,c,w=1): d.line(points,fill=COL[c],width=w)
        def box(bounds,c): d.rectangle(bounds,fill=COL[c])
        paint(poly,line,box)
        if moving:
            moved=Image.new('RGBA',(32,32)); moved.paste(im,(shift,bob)); im=moved
        parts.append((name,im))
    cloth = 4 if role!=1 else 20
    bright = 5 if role!=1 else 21
    scarf = 14 if role==0 else 22 if role==1 else 23
    # Secondary cloth trails one phase behind torso.
    tail=[0,1,0,-1][(f-1)%4]
    region('scarf_tail',lambda p,l,b:(p([(11,19),(8,19),(5,21+tail),(3,24+tail),(7,24+tail),(9,22),(12,22)],scarf),l([(5,23+tail),(8,21+tail)],15 if role==0 else 9)))
    def boots(p,l,b):
        step=([0,-2,0,2][f] if state=='run' else 0)
        p([(11,26),(15,26),(15,29-max(step,0)),(14,30-max(step,0)),(10,30-max(step,0)),(10,28)],2)
        p([(18,26),(21,26),(22,29-max(-step,0)),(21,30-max(-step,0)),(17,30-max(-step,0)),(17,28)],2)
        l([(11,28-max(step,0)),(13,28-max(step,0))],5)
        l([(18,28-max(-step,0)),(20,28-max(-step,0))],5)
    region('boots',boots,False)
    region('robe',lambda p,l,b:(p([(12,19),(19,19),(21,22),(23,28),(19,27),(17,29),(15,27),(10,28),(11,25),(9,26)],cloth),p([(15,21),(17,21),(19,26),(16,27),(14,25)],bright,0),l([(12,23),(11,26)],3),l([(19,22),(21,26)],bright)))
    region('arms',lambda p,l,b:(p([(10,21),(12,22),(11,25),(8,26),(8,24)],cloth),l([(9,24),(10,23)],bright),p([(20,21),(22,22),(23,25),(21,26),(19,24)],cloth)))
    # Face is offset to the right; unequal eyes establish the 3/4 angle.
    region('face',lambda p,l,b:(p([(9,12),(21,11),(24,14),(23,19),(20,21),(12,20),(9,18)],2),p([(11,14),(19,13),(23,15),(22,18),(18,20),(11,18)],1,0)))
    def helmet(p,l,b):
        p([(13,1),(17,1),(19,3),(19,5),(17,6),(12,6),(11,4),(11,3)],10)
        l([(13,2),(16,2),(17,3),(17,4)],11)
        b((14,3,16,5),1)
        p([(11,5),(18,5),(21,7),(23,10),(23,12),(26,16),(23,17),(20,15),(16,14),(12,13),(7,14),(6,16),(4,15),(6,11),(8,8)],8 if role!=2 else 24)
        p([(18,6),(21,8),(22,11),(22,13),(24,15),(21,14),(19,12)],7 if role!=2 else 10,0)
        p([(10,7),(14,6),(17,6),(17,8),(13,8),(10,10),(8,12),(6,13),(8,9)],9 if role!=2 else 23,0)
        p([(17,9),(18,10),(17,12),(18,14),(16,14),(15,12)],6 if role!=2 else 10,0)
        l([(13,7),(14,9),(16,10),(17,12),(20,13)],5)
        p([(9,10),(9,12),(11,12)],3,0)
        if role==1:
            l([(18,7),(19,8),(18,9)],18); b((17,8,17,8),19)
        if role==2:
            p([(8,7),(7,4),(9,5),(11,7)],23)
            p([(20,7),(22,4),(22,8)],23)
            l([(12,9),(13,11),(12,12)],17)
    region('bell_helmet',helmet)
    def eyes(p,l,b):
        blink=state=='idle' and f==3
        if blink:
            l([(14,17),(15,17)],17); l([(20,16),(22,16)],17)
        else:
            b((14,16,15,18),17); b((14,16,14,17),19)
            b((20,14,22,17),17); b((21,14,21,17),18); b((21,15,21,16),19)
    region('glowing_eyes',eyes)
    region('scarf_collar',lambda p,l,b:(p([(10,19),(15,20),(20,19),(23,18),(23,21),(20,23),(16,24),(12,22)],scarf),l([(12,20),(16,21),(21,20)],15 if role==0 else 9),l([(14,23),(17,23),(20,22)],13 if role==0 else 21 if role==1 else 10)))
    def equip(p,l,b):
        if role==0:
            l([(23,23),(25,23),(25,25)],11)
            p([(23,24),(27,24),(28,28),(23,28)],24)
            b((24,25,26,27),17); b((25,25,25,26),19)
        elif role==1:
            l([(25,20),(24,28)],10,2)
            p([(25,16),(28,19),(25,22),(23,19)],17)
            l([(25,17),(25,20)],19)
        else:
            p([(21,22),(25,21),(28,23),(27,28),(24,30),(21,27)],24)
            p([(23,23),(25,23),(26,24),(25,27),(24,28),(23,26)],20)
            l([(24,24),(25,25),(24,26)],18)
    region('equipment',equip)
    if state=='cast' and f in (1,2):
        def fx(p,l,b):
            l([(27,14),(29,16),(29,20),(28,21)],18)
            b((29,12,29,13),19); b((26,10,26,10),17)
            if f==2: l([(27,22),(29,23),(29,25)],17); b((25,9,25,9),19)
        region('cast_sparks',fx)
    if state=='hurt' and f==1:
        region('impact',lambda p,l,b:(l([(4,16),(2,14)],14),l([(4,18),(1,18)],15)),False)
    return parts

def faithful_bell(state,f):
    """Hand-authored pixel geometry following the supplied bell character, facing left.

    No lantern: the reference has one cyan slit, one ivory point-eye, a side
    bell tab, a broken bell shell, broad coral collar and a split violet robe.
    """
    parts=[]
    bob=([0,-1,0,0][f] if state=='run' else [0,0,-1,0][f] if state=='idle' else [0,1,-1,0][f] if state=='cast' else [0,1,1,0][f])
    dx=1 if state=='hurt' and f in (1,2) else 0
    def region(name,paint,moving=True):
        im=Image.new('RGBA',(32,32)); d=ImageDraw.Draw(im)
        def p(points,c,o=1): d.polygon(points,fill=COL[c],outline=COL[o] if o else None)
        def l(points,c,w=1): d.line(points,fill=COL[c],width=w)
        def b(bounds,c): d.rectangle(bounds,fill=COL[c])
        paint(p,l,b)
        if moving:
            shifted=Image.new('RGBA',(32,32)); shifted.paste(im,(dx,bob)); im=shifted
        # The production pipeline mirrors right into left, so return right here.
        parts.append((name,im.transpose(Image.Transpose.FLIP_LEFT_RIGHT)))
    lag=[0,0,1,-1][f]
    region('scarf_tail',lambda p,l,b:(p([(20,21),(22,21),(23,22),(24,22),(24,23),(27,24+lag),(29,24+lag),(29,26+lag),(27,26+lag),(27,28+lag),(25,28+lag),(24,26),(22,25),(21,23)],14),l([(22,22),(23,23),(25,24+lag)],13),l([(24,25),(26,26+lag)],14)))
    def feet(p,l,b):
        a=[0,1,0,-1][f] if state=='run' else 0
        p([(12,27),(15,27),(15,30-max(a,0)),(12,30-max(a,0)),(11,29-max(a,0))],2)
        p([(18,27),(21,27),(21,30-max(-a,0)),(18,30-max(-a,0))],2)
        l([(12,29-max(a,0)),(13,29-max(a,0))],3)
        l([(19,29-max(-a,0)),(20,29-max(-a,0))],3)
    region('boots',feet,False)
    region('robe',lambda p,l,b:(p([(12,22),(19,21),(21,23),(22,25),(24,29),(21,28),(19,28),(18,30),(15,29),(14,28),(11,29),(11,27),(9,28),(10,25)],3),p([(17,23),(19,23),(20,25),(21,28),(18,28),(17,29),(15,28),(15,27)],4,0),p([(12,24),(14,25),(13,27),(11,28),(12,26)],4,0),l([(18,24),(20,27),(22,28)],5),p([(13,23),(17,23),(15,25),(14,26)],4,0),l([(12,25),(13,27)],1)))
    region('arms',lambda p,l,b:(p([(11,23),(12,24),(11,25),(10,26),(9,28),(8,28),(8,26),(9,25)],4),l([(9,26),(9,27)],5)))
    region('face',lambda p,l,b:(p([(8,13),(15,13),(20,15),(23,17),(23,19),(21,20),(19,21),(11,21),(8,20),(7,18),(7,15)],1),p([(7,15),(8,15),(8,18),(9,19),(9,20),(8,19),(7,18)],4,0),p([(20,17),(22,18),(22,19),(20,19)],3,0)))
    def shell(p,l,b):
        # Raised bronze hanging ring, with a dark hollow center.
        p([(14,1),(17,1),(19,2),(19,4),(17,5),(12,5),(12,2)],10)
        l([(14,2),(16,2),(18,3),(18,4)],11)
        b((14,3,16,4),1); b((15,2,16,2),1)
        # Broad asymmetrical bell, flared snout and hanging back lip.
        p([(11,4),(17,4),(19,5),(21,5),(21,6),(23,7),(24,9),(24,11),(25,12),(24,14),(23,14),(24,17),(25,19),(23,19),(21,18),(19,17),(17,16),(15,15),(12,14),(9,14),(8,13),(6,14),(4,14),(4,13),(6,11),(7,9),(8,7),(10,6)],8)
        p([(17,5),(20,6),(22,7),(23,9),(23,11),(22,13),(22,15),(24,18),(22,17),(20,16),(19,14),(18,12)],7,0)
        p([(11,5),(15,5),(16,6),(15,8),(13,9),(11,10),(9,11),(7,12),(5,13),(7,10),(9,7)],9,0)
        p([(16,10),(18,10),(19,12),(18,14),(16,14),(15,13),(15,11)],6,0)
        p([(20,13),(21,14),(21,17),(23,18),(21,17),(20,16)],6,0)
        l([(12,6),(13,8),(15,9),(17,9),(17,11),(19,12),(20,14),(22,15)],5)
        p([(9,8),(10,8),(10,10),(11,11),(8,11),(7,10)],3,0)
        p([(7,12),(9,12),(9,13),(8,13)],3,0)
        l([(5,14),(6,15),(6,16)],5)
    region('bell_helmet',shell)
    region('side_bell_tab',lambda p,l,b:(p([(25,11),(26,12),(26,13),(27,14),(27,15),(26,16),(26,17),(24,17),(24,15),(23,15),(23,13),(24,13)],7),p([(24,13),(25,12),(25,14),(26,14),(26,15),(24,15)],8,0),b((24,15,25,16),5)))
    def eyes(p,l,b):
        if state=='idle' and f==3:
            l([(9,17),(10,17)],18); b((14,18,15,18),9)
        else:
            p([(9,15),(10,15),(10,16),(11,16),(11,18),(10,19),(9,19),(9,18),(8,18),(8,16),(9,16)],17,0)
            b((9,16,10,18),18); b((10,16,10,17),19)
            p([(14,17),(15,17),(16,18),(15,19),(14,19),(13,18)],9,0)
            b((14,17,15,17),7)
    region('glowing_eyes',eyes)
    region('scarf_collar',lambda p,l,b:(p([(8,20),(12,20),(13,21),(17,20),(18,19),(21,19),(22,20),(22,21),(20,22),(18,23),(16,24),(12,24),(10,23),(9,22)],14),l([(9,20),(11,20),(12,21)],15),l([(10,22),(12,23),(15,23),(18,22),(20,21)],14),b((10,22,10,22),13),b((13,21,14,21),13),l([(19,19),(20,20)],15)))
    if state=='cast' and f in (1,2):
        region('cast_sparks',lambda p,l,b:(l([(3,18),(2,20),(3,22)],18),b((1,16,1,16),19),b((5,23,5,23),17)))
    if state=='hurt' and f==1:
        region('impact',lambda p,l,b:(l([(4,17),(2,15)],14),l([(3,19),(1,19)],15)),False)
    return parts

def main():
    ROOT.mkdir(exist_ok=True)
    gallery=Image.new('RGB',(960,490),'#161927'); gd=ImageDraw.Draw(gallery)
    animframes=[Image.new('RGB',(768,340),'#161927') for _ in range(4)]
    for role,(slug,title) in enumerate(ROLES):
        out=ROOT/slug; out.mkdir(exist_ok=True)
        doc=['@pix2 1.1','@meta',f'  title: "Ember - {title}"','  author: "Ember art study"','  version: "1.1"','  color_mode: "RGBA"','  coordinate_origin: "top-left"','@grid','  columns: 4','  rows: 8','  default_cell: 32x32','  background: none','@palette']
        doc += [f'  {k}: {v}' for k,v in COL.items()]
        atlas=Image.new('RGBA',(128,256)); frames=[]; clips=[]
        for side in range(2):
            direction='right' if side==0 else 'left'
            for s,(state,fps,loop) in enumerate(STATES):
                ids=[]; gif=[]
                for f in range(4):
                    row=side*4+s; cid=f'cell_{row}_{f}'; ids.append(cid)
                    doc += [f'@cell {cid}',f'  name: "{state}_{direction}_{f}"',f'  direction: {direction}',f'  frame: {f}',f'  grid_pos: {f},{row}']
                    im=Image.new('RGBA',(32,32))
                    for name,part in sprite(role,state,f):
                        if side: part=part.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
                        im=Image.alpha_composite(im,part)
                        lid=f'{cid}_{name}_layer'; rid=f'{cid}_{name}'
                        doc += [f'@layer {lid}',f'  cell: {cid}',f'  name: "{name}"','  visible: true','  opacity: 1','  blend: normal',f'@region {rid}',f'  cell: {cid}',f'  parent: {lid}',f'  tags: [{name}]']
                        for y in range(32):
                            pixels=[]
                            for x in range(32):
                                c=part.getpixel((x,y))
                                if c[3]: pixels.append(f'p {x},{y} {PAL.index("%02x%02x%02x"%c[:3])+1}')
                            if pixels: doc.append('  '+' '.join(pixels))
                    atlas.paste(im,(f*32,row*32))
                    frames.append(dict(name=f'{state}_{direction}_{f}',x=f*32,y=row*32,w=32,h=32,pivotX=.5,pivotY=1/32))
                    bg=Image.new('RGB',(192,192),'#24283b'); bg.paste(im.resize((192,192),Image.Resampling.NEAREST),(0,0),im.resize((192,192),Image.Resampling.NEAREST)); gif.append(bg)
                    if s==1:
                        big=im.resize((96,96),Image.Resampling.NEAREST)
                        animframes[f].paste(big,(role*256+side*112+14,100),big)
                    if s==0 and f==0:
                        big=im.resize((128,128),Image.Resampling.NEAREST)
                        gallery.paste(big,(role*320+side*140+10,65),big)
                    if side==0:
                        big=im.resize((64,64),Image.Resampling.NEAREST)
                        gallery.paste(big,(role*320+f*70+8,215+s*65),big)
                gif[0].save(out/f'{state}_{direction}.gif',save_all=True,append_images=gif[1:],duration=round(1000/fps),loop=0 if loop else 1,disposal=2)
                clips.append(dict(clipName=f'{state}_{direction}',fps=fps,loop=loop,frameNames=[f'{state}_{direction}_{f}' for f in range(4)]))
                doc += [f'@animation {state}_{direction}',f'  name: "{state}_{direction}"',f'  direction: {direction}',f'  frames: [{", ".join(ids)}]',f'  fps: {fps}',f'  loop: {str(loop).lower()}',f'  mode: {"loop" if loop else "once"}']
        # Canonical section ordering: all cells precede all animation blocks.
        cells=[]; animations=[]; active=cells
        for line in doc:
            if line.startswith('@animation'): active=animations
            elif line.startswith('@cell'): active=cells
            active.append(line)
        (out/f'{slug}.pix2').write_text('\n'.join(cells+animations+['@end'])+'\n',encoding='utf-8')
        atlas.save(out/f'{slug}.png')
        if role==0:
            pair=Image.new('RGBA',(64,32))
            pair.paste(atlas.crop((0,128,32,160)),(0,0))
            pair.paste(atlas.crop((0,0,32,32)),(32,0))
            pair.save(out/'reference_pair_32px.png')
            pair.resize((768,384),Image.Resampling.NEAREST).save(ROOT/'bell_reference_preview.png')
        meta=dict(meta=dict(image=f'{slug}.png',cellWidth=32,cellHeight=32,size=dict(w=128,h=256)),frames=frames,animations=clips)
        (out/f'{slug}.pix2.json').write_text(json.dumps(meta,indent=2),encoding='utf-8')
        gd.text((role*320+12,20),title,fill='#e9ddc9')
        gd.text((role*320+12,43),'3/4 RIGHT       3/4 LEFT',fill='#85edee')
        for f in range(4): ImageDraw.Draw(animframes[f]).text((role*256+15,55),title,fill='#e9ddc9')
        target=ROOT.parents[1]/'My project'/'Assets'/'Art'/'BellVessels'/slug
        target.mkdir(parents=True,exist_ok=True)
        for suffix in ('.png','.pix2.json'): shutil.copy2(out/f'{slug}{suffix}',target/f'{slug}{suffix}')
    gallery.save(ROOT/'contact_sheet.png')
    animframes[0].save(ROOT/'run_preview.gif',save_all=True,append_images=animframes[1:],duration=100,loop=0)
    print('Created 3 characters, 96 frames, 24 animation clips.')

if __name__=='__main__': main()
