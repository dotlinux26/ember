"""Native 64px sprite geometry traced by hand from the reference silhouette.
Coordinates below describe landmarks in the supplied artwork, rasterized onto
the new integer canvas. This is not an upscale of the previous 32px design.
"""
from pathlib import Path
from PIL import Image, ImageDraw
import json, shutil
ROOT=Path(__file__).resolve().parents[1]
PALETTE=['100e20','24203e','38325c','4b447d','5b528c','81758d','b6a6b6','d1bfc4','eadace','fff1da','ba8b72','ebc49a','b94463','e65b76','ff7f8d','ff9a9d','2eb9d9','69e6f4','efffff','eff4dc','386b70','65b59e','d3edce','d7b66b','8f704a','a2c6dd']
C=['#'+v for v in PALETTE]
ROLES=['bell_wanderer','moon_mender','rune_warden']
STATES=[('idle',6,True),('run',10,True),('cast',10,False),('hurt',10,False)]

def draw(role,state,f):
    parts=[]
    bob=([0,0,-1,-1,0,0][f] if state=='idle' else [0,-1,-2,0,-1,-2][f] if state=='run' else [0,1,0,-1,-1,0][f] if state=='cast' else [0,1,1,0,0,0][f])
    lean=[0,-1,-1,0,1,0][f] if state=='run' else [0,1,2,1,0,0][f] if state=='hurt' else 0
    # Source landmarks map to a 42px-wide, 59px-tall body with animation margin.
    def xy(v):return (round((v[0]-223)*.094)+10,round((v[1]-199)*.094)+3)
    def region(name,paint,offset=(0,0),body=True):
        im=Image.new('RGBA',(64,64));d=ImageDraw.Draw(im)
        def p(points,c,o=None):
            pts=[xy(v) for v in points];d.polygon(pts,fill=C[c-1],outline=C[o-1] if o else None)
        def l(points,c,w=1):d.line([xy(v) for v in points],fill=C[c-1],width=w)
        def b(bounds,c):d.rectangle([xy(bounds[:2]),xy(bounds[2:])],fill=C[c-1])
        paint(p,l,b)
        dx,dy=offset
        if body:dx+=lean;dy+=bob
        moved=Image.new('RGBA',(64,64));moved.paste(im,(dx,dy));parts.append((name,moved))
    robe=4 if role!=1 else 21;fold=5 if role!=1 else 22
    scarf=15 if role==0 else 23 if role==1 else 24
    tail=[0,1,2,1,0,-1][(f-1)%6]
    region('scarf_tail',lambda p,l,b:(p([(494,611),(525,617),(543,641),(563,650),(580,666),(600,677),(620,680),(620,714),(596,721),(581,742),(553,743),(549,720),(527,705),(513,687),(497,674),(486,643)],1),p([(503,625),(520,629),(535,648),(556,657),(568,674),(596,686),(613,688),(613,705),(588,711),(576,733),(558,731),(556,709),(534,699),(522,680),(506,672),(496,644)],scarf),l([(504,630),(520,640),(535,659)],14 if role==0 else 22)),offset=(0,tail))
    def feet(p,l,b):
        a=[0,-1,-2,0,1,0][f] if state=='run' else 0
        p([(358,767),(395,765),(405,812),(371,812),(359,794)],1)
        p([(373,780),(389,784),(395,805),(374,805)],2)
        p([(467,771),(499,770),(508,815),(470,815)],1)
        p([(477,786),(493,786),(499,806),(475,806)],2)
        if a: l([(373,802-a*12),(393,802-a*12)],3)
    region('boots',feet,body=False)
    region('robe',lambda p,l,b:(p([(347,657),(385,661),(414,648),(450,639),(479,657),(494,683),(510,716),(536,746),(565,780),(565,797),(535,785),(511,782),(495,802),(467,790),(459,803),(426,809),(407,794),(391,784),(372,802),(343,800),(327,785),(314,789),(314,757),(325,727),(322,693)],1),p([(352,669),(387,680),(416,666),(451,651),(470,665),(483,694),(501,725),(521,752),(550,782),(523,771),(500,770),(478,790),(449,780),(430,795),(403,782),(385,770),(362,790),(329,782),(336,749),(341,719)],robe),p([(445,674),(466,688),(478,719),(494,755),(480,775),(444,788),(406,784),(400,764),(415,738),(420,711)],fold),p([(353,683),(382,704),(412,691),(429,678),(420,705),(397,730),(382,731),(368,711)],fold),l([(382,727),(378,749),(365,768)],1,2),l([(437,694),(454,720),(460,746)],robe,2),l([(488,728),(513,758),(534,773)],fold)))
    # Attached sleeve silhouettes remain in the faithful art; runtime hands are separate.
    region('sleeve',lambda p,l,b:(p([(326,675),(348,686),(350,711),(329,725),(315,747),(301,768),(276,765),(275,741),(289,721),(302,705),(311,684)],1),p([(327,688),(340,697),(338,712),(317,729),(302,751),(287,756),(289,740),(305,719),(316,707)],fold),l([(320,715),(302,734)],robe)))
    region('face',lambda p,l,b:(p([(254,479),(290,460),(331,453),(382,470),(424,479),(466,506),(494,529),(519,551),(527,574),(507,599),(471,611),(410,619),(351,609),(300,604),(274,583),(264,550),(254,522)],1),p([(266,492),(284,490),(280,529),(289,568),(299,580),(292,589),(277,578),(270,550)],3),p([(480,546),(498,553),(513,574),(495,588),(476,580)],3)))
    def ring(p,l,b):
        p([(451,199),(505,199),(505,212),(520,212),(520,228),(535,244),(535,278),(549,292),(493,301),(421,285),(406,278),(406,237),(421,221),(435,221),(435,212),(451,212)],1)
        p([(451,216),(494,216),(494,227),(507,227),(519,245),(519,272),(499,272),(497,247),(480,238),(452,242),(440,255),(440,271),(422,271),(422,242),(437,230),(451,230)],12)
        p([(499,244),(518,249),(518,274),(499,274)],11)
        b((452,249,486,286),1);b((465,255,479,272),2)
    region('hanging_ring',ring)
    def shell(p,l,b):
        p([(360,277),(409,277),(423,269),(482,277),(535,289),(550,303),(566,314),(582,337),(598,358),(608,383),(615,413),(601,445),(591,459),(585,485),(586,511),(601,551),(615,573),(607,595),(577,599),(556,585),(542,572),(520,568),(505,549),(478,542),(456,525),(438,513),(405,502),(377,495),(343,481),(323,472),(303,472),(286,485),(270,500),(246,512),(223,499),(223,468),(240,451),(254,432),(269,414),(282,394),(296,371),(308,351),(309,336),(326,320),(326,305),(345,289),(360,289)],1)
        p([(363,294),(409,288),(423,284),(480,291),(525,300),(547,315),(568,339),(584,361),(598,391),(598,418),(583,445),(573,477),(573,513),(584,549),(597,576),(582,582),(560,566),(538,557),(517,537),(480,527),(456,510),(431,499),(406,487),(374,483),(346,470),(323,458),(302,458),(282,473),(262,488),(242,495),(240,479),(257,459),(275,437),(291,409),(308,383),(323,354),(338,336),(347,317)],9 if role!=2 else 24)
        p([(480,294),(524,304),(546,320),(568,343),(583,368),(593,399),(591,422),(575,450),(566,480),(566,519),(582,565),(562,554),(548,535),(535,506),(522,473),(518,438),(510,406),(497,375),(479,349)],8 if role!=2 else 25)
        p([(539,321),(559,337),(576,363),(587,389),(590,414),(578,441),(566,453),(565,424),(552,390)],7 if role!=2 else 11)
        p([(359,299),(410,291),(423,288),(464,294),(474,317),(458,337),(452,366),(437,391),(408,408),(387,433),(372,447),(346,447),(322,439),(301,445),(273,463),(256,476),(263,457),(286,426),(303,396),(326,358),(341,338)],10 if role!=2 else 12)
        p([(458,383),(479,385),(492,399),(499,425),(494,453),(478,476),(459,473),(438,457),(434,435),(442,409)],8 if role!=2 else 11)
        p([(513,453),(529,463),(535,493),(548,530),(568,563),(550,551),(533,535),(524,506)],7 if role!=2 else 25)
        l([(413,314),(418,328),(429,338),(433,351),(446,362),(456,373),(474,376),(489,389),(500,408),(501,425),(517,437),(529,450),(550,457),(568,472)],6)
        l([(474,376),(465,391),(457,402)],6)
        p([(360,338),(382,338),(382,363),(397,384),(397,403),(373,401),(350,390),(328,397),(326,385),(338,370),(346,352)],3)
        p([(318,407),(345,407),(360,414),(357,428),(343,439),(330,436),(330,424),(318,423)],3)
        l([(243,491),(260,494),(268,510)],7)
    region('bell_shell',shell)
    region('side_tab',lambda p,l,b:(p([(606,419),(628,419),(628,435),(642,449),(642,465),(659,477),(659,498),(644,511),(643,526),(614,539),(599,523),(583,522),(580,496),(581,462),(596,447)],1),p([(609,435),(622,436),(625,452),(636,464),(640,481),(627,494),(626,513),(610,522),(602,510),(594,510),(594,486),(598,462)],26),p([(601,480),(615,480),(615,499),(602,508),(596,498)],5)))
    def eyes(p,l,b):
        if state=='idle' and f==5:
            l([(315,535),(342,535)],18); l([(405,547),(429,547)],20)
        else:
            p([(323,496),(337,496),(337,507),(350,508),(350,550),(337,550),(337,562),(322,562),(322,550),(310,548),(310,510),(322,510)],17)
            b((321,509,340,550),18);b((326,518,336,545),19)
            p([(409,520),(421,520),(421,529),(432,535),(436,548),(429,560),(418,568),(405,562),(398,550),(398,536),(409,532)],20)
            p([(410,520),(420,520),(428,533),(408,533),(402,539),(401,533)],26)
    region('eyes',eyes)
    region('scarf_collar',lambda p,l,b:(p([(300,579),(351,579),(351,590),(407,593),(439,582),(465,574),(465,563),(497,564),(528,574),(529,599),(513,615),(491,629),(471,641),(445,651),(426,665),(391,674),(358,663),(340,650),(319,646),(319,625),(302,609)],1),p([(304,585),(348,585),(349,598),(404,601),(440,589),(466,580),(470,572),(495,574),(519,581),(519,596),(500,608),(480,621),(455,634),(429,645),(406,659),(364,655),(346,643),(326,639),(327,621),(310,605)],scarf),l([(308,587),(336,587),(347,599)],16 if role==0 else 23,2),l([(472,579),(494,580),(508,588)],16 if role==0 else 23),l([(330,615),(346,620)],14 if role==0 else 22),l([(376,611),(391,605)],14 if role==0 else 22),l([(447,624),(475,609)],14 if role==0 else 22)))
    if role==1:
        region('moon_crown',lambda p,l,b:(p([(366,275),(359,239),(370,216),(388,208),(380,230),(381,248),(402,263),(419,265),(402,279)],23,1),l([(370,233),(371,253),(389,267)],19)))
    if role==2:
        region('rune_crest',lambda p,l,b:(l([(367,370),(378,351),(389,370),(378,392),(367,370)],18),l([(377,355),(377,387)],19)))
    if state=='cast' and 1<=f<=4:
        amount=[0,1,2,3,2,0][f]
        region('cast_effect',lambda p,l,b:(l([(254-amount*14,556),(240-amount*14,582),(252-amount*14,609)],18),l([(280,665),(263,682)],17),b((249,533,254,539),19)))
    return parts

def main():
    ROOT.mkdir(exist_ok=True); preview=Image.new('RGB',(800,864),'#171725'); pd=ImageDraw.Draw(preview)
    for role,slug in enumerate(ROLES):
        folder=ROOT/slug;folder.mkdir(exist_ok=True)
        doc=['@pix2 1.1','@meta',f'  title: "Ember {slug} 64"','  author: "Ember"','  version: "1.1"','  color_mode: "RGBA"','  coordinate_origin: "top-left"','@grid','  columns: 6','  rows: 8','  default_cell: 64x64','  background: none','@palette']+[f'  {i+1}: {c}' for i,c in enumerate(C)]
        anim=[];frames=[];atlas=Image.new('RGBA',(384,512)); clips=[]
        for side in range(2):
            direction='left' if side==0 else 'right'
            for s,(state,fps,loop) in enumerate(STATES):
                row=side*4+s;ids=[];gif=[]
                for f in range(6):
                    cid=f'cell_{row}_{f}';ids.append(cid);im=Image.new('RGBA',(64,64))
                    doc += [f'@cell {cid}',f'  name: "{state}_{direction}_{f}"',f'  direction: {direction}',f'  frame: {f}',f'  grid_pos: {f},{row}']
                    for name,part in draw(role,state,f):
                        if side:part=part.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
                        im=Image.alpha_composite(im,part);lid=cid+'_'+name+'_layer';rid=cid+'_'+name
                        doc += [f'@layer {lid}',f'  cell: {cid}',f'  name: "{name}"','  opacity: 1','  blend: normal',f'@region {rid}',f'  cell: {cid}',f'  parent: {lid}',f'  tags: [{name}]']
                        for y in range(64):
                            run=[]
                            for x in range(64):
                                c=part.getpixel((x,y))
                                if c[3]:run.append(f'p {x},{y} {PALETTE.index("%02x%02x%02x"%c[:3])+1}')
                            if run:doc.append('  '+' '.join(run))
                    atlas.paste(im,(f*64,row*64));frames.append(dict(name=f'{state}_{direction}_{f}',x=f*64,y=row*64,w=64,h=64,pivotX=.5,pivotY=1/64))
                    bg=Image.new('RGB',(256,256),'#171725');big=im.resize((256,256),Image.Resampling.NEAREST);bg.paste(big,(0,0),big);gif.append(bg)
                    if s==0 and f==0:
                        preview.paste(big,(side*260,role*288+16),big)
                        if role==0:im.save(folder/f'base_{direction}.png')
                gif[0].save(folder/f'{state}_{direction}.gif',save_all=True,append_images=gif[1:],duration=round(1000/fps),loop=0 if loop else 1,disposal=2)
                anim += [f'@animation {state}_{direction}',f'  direction: {direction}',f'  frames: [{", ".join(ids)}]',f'  fps: {fps}',f'  loop: {str(loop).lower()}',f'  mode: {"loop" if loop else "once"}']
                clips.append(dict(clipName=f'{state}_{direction}',fps=fps,loop=loop,frameNames=[f'{state}_{direction}_{f}' for f in range(6)]))
        (folder/(slug+'.pix2')).write_text('\n'.join(doc+anim+['@end'])+'\n',encoding='utf8');atlas.save(folder/(slug+'.png'))
        (folder/(slug+'.pix2.json')).write_text(json.dumps(dict(meta=dict(image=slug+'.png',cellWidth=64,cellHeight=64,size=dict(w=384,h=512)),frames=frames,animations=clips),indent=2))
        target=ROOT.parents[1]/'My project/Assets/Resources/Ember/Characters'/slug;target.mkdir(parents=True,exist_ok=True)
        for ext in ('.png','.pix2.json'):shutil.copy2(folder/(slug+ext),target/(slug+ext))
        pd.text((550,role*288+95),slug,fill='#eadace')
    preview.save(ROOT/'contact_sheet.png')
    print('64px: 3 characters, 144 frames, 24 clips')
if __name__=='__main__':main()
