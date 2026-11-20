import os
from PIL import Image

def create_wildflowers():
    # 16x16 RGBA
    im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    
    PAL = {
        "OUT":   (15, 23, 42, 255),    # #0F172A Deep shadow
        "LEAF_D":(20, 83, 45, 255),    # #14532D Leaf shadow
        "LEAF_M":(22, 163, 74, 255),   # #16A34A Leaf midtone
        "LEAF_L":(74, 222, 128, 255),  # #4ADE80 Leaf highlight
        
        "Y_CORE":(245, 158, 11, 255),  # #F59E0B Yellow flower core
        "Y_PET": (253, 224, 71, 255),  # #FDE047 Yellow petals
        "Y_HI":  (254, 240, 138, 255), # #FEF08A Yellow highlight
        
        "P_CORE":(190, 24, 93, 255),   # #BE185D Pink flower core
        "P_PET": (244, 63, 94, 255),   # #F43F5E Pink petals
        "P_HI":  (253, 164, 175, 255), # #FDA4AF Pink highlight
        
        "C_CORE":(2, 132, 199, 255),   # #0284C7 Cyan flower core
        "C_PET": (56, 189, 248, 255),  # #38BDF8 Cyan petals
        "C_HI":  (186, 230, 253, 255), # #BAE6FD Cyan highlight
        
        "W_PET": (241, 245, 249, 255), # #F1F5F9 White flower petals
        "W_HI":  (255, 255, 255, 255), # #FFFFFF Pure white glint
    }
    
    # Ground shadow / leaf bed on rows 12..14
    for x in range(2, 14):
        im.putpixel((x, 14), PAL["OUT"])
    for x in range(3, 13):
        im.putpixel((x, 13), PAL["LEAF_D"])
    for x in [3, 4, 7, 8, 11, 12]:
        im.putpixel((x, 12), PAL["LEAF_M"])
    for x in [4, 8, 12]:
        im.putpixel((x, 11), PAL["LEAF_L"])
        
    # Yellow Flower (Top Center, centered around x=7..9, y=3..6)
    # Petals
    y_coords = [
        (8, 2, "OUT"),
        (7, 3, "Y_PET"), (8, 3, "Y_HI"), (9, 3, "Y_PET"),
        (6, 4, "Y_PET"), (7, 4, "Y_CORE"), (8, 4, "Y_HI"), (9, 4, "Y_PET"), (10, 4, "OUT"),
        (6, 5, "OUT"), (7, 5, "Y_PET"), (8, 5, "Y_PET"), (9, 5, "Y_CORE"), (10, 5, "OUT"),
        (7, 6, "OUT"), (8, 6, "LEAF_D"), (9, 6, "OUT")
    ]
    for x, y, c in y_coords:
        im.putpixel((x, y), PAL[c])
        
    # Pink Flower (Left, centered around x=3..5, y=7..10)
    p_coords = [
        (3, 6, "OUT"), (4, 6, "P_HI"), (5, 6, "OUT"),
        (2, 7, "OUT"), (3, 7, "P_PET"), (4, 7, "P_CORE"), (5, 7, "P_PET"), (6, 7, "OUT"),
        (2, 8, "P_PET"), (3, 8, "P_CORE"), (4, 8, "P_HI"), (5, 8, "P_PET"),
        (3, 9, "OUT"), (4, 9, "P_PET"), (5, 9, "OUT")
    ]
    for x, y, c in p_coords:
        im.putpixel((x, y), PAL[c])
        
    # Cyan Flower (Right, centered around x=11..13, y=7..10)
    c_coords = [
        (11, 6, "OUT"), (12, 6, "C_HI"), (13, 6, "OUT"),
        (10, 7, "OUT"), (11, 7, "C_PET"), (12, 7, "C_CORE"), (13, 7, "C_PET"), (14, 7, "OUT"),
        (11, 8, "C_PET"), (12, 8, "C_HI"), (13, 8, "C_PET"), (14, 8, "OUT"),
        (11, 9, "OUT"), (12, 9, "C_PET"), (13, 9, "OUT")
    ]
    for x, y, c in c_coords:
        im.putpixel((x, y), PAL[c])
        
    # White Blossom / Starlight glints (Center foreground, x=7..9, y=8..10)
    w_coords = [
        (7, 8, "W_PET"), (8, 8, "W_HI"), (9, 8, "W_PET"),
        (7, 9, "W_PET"), (8, 9, "W_HI"), (9, 9, "W_PET"),
        (8, 10, "LEAF_M")
    ]
    for x, y, c in w_coords:
        im.putpixel((x, y), PAL[c])
        
    return im

def create_stone_pebble():
    # 16x16 RGBA
    im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    
    PAL = {
        "SHADOW": (15, 23, 42, 255),    # #0F172A Contact shadow
        "BASE":   (51, 65, 85, 255),    # #334155 Rock shadow
        "MID":    (100, 116, 139, 255), # #64748B Rock midtone
        "LIGHT":  (148, 163, 184, 255), # #94A3B8 Rock light
        "SPEC":   (226, 232, 240, 255), # #E2E8F0 Specular highlight
        "GRASS":  (22, 101, 52, 255),   # #166534 Moss speck
    }
    
    # Ground contact shadows (y=13..14)
    for x in range(2, 15):
        im.putpixel((x, 14), PAL["SHADOW"])
    for x in range(3, 10):
        im.putpixel((x, 13), PAL["SHADOW"])
        
    # Pebble 1: Large smooth pebble on left (x=2..9, y=6..13)
    p1 = [
        # y=6
        (4, 6, "SHADOW"), (5, 6, "LIGHT"), (6, 6, "SPEC"), (7, 6, "LIGHT"), (8, 6, "SHADOW"),
        # y=7
        (3, 7, "SHADOW"), (4, 7, "LIGHT"), (5, 7, "SPEC"), (6, 7, "SPEC"), (7, 7, "LIGHT"), (8, 7, "MID"), (9, 7, "SHADOW"),
        # y=8
        (2, 8, "SHADOW"), (3, 8, "LIGHT"), (4, 8, "SPEC"), (5, 8, "LIGHT"), (6, 8, "LIGHT"), (7, 8, "MID"), (8, 8, "MID"), (9, 8, "SHADOW"),
        # y=9
        (2, 9, "SHADOW"), (3, 9, "MID"), (4, 9, "LIGHT"), (5, 9, "MID"), (6, 9, "MID"), (7, 9, "BASE"), (8, 9, "BASE"), (9, 9, "SHADOW"),
        # y=10
        (2, 10, "SHADOW"), (3, 10, "MID"), (4, 10, "MID"), (5, 10, "BASE"), (6, 10, "BASE"), (7, 10, "BASE"), (8, 10, "SHADOW"),
        # y=11
        (3, 11, "SHADOW"), (4, 11, "BASE"), (5, 11, "BASE"), (6, 11, "BASE"), (7, 11, "SHADOW"),
        # y=12
        (4, 12, "SHADOW"), (5, 12, "SHADOW"), (6, 12, "SHADOW")
    ]
    for x, y, c in p1:
        im.putpixel((x, y), PAL[c])
        
    # Pebble 2: Medium pebble on right (x=9..14, y=8..13)
    p2 = [
        # y=8
        (11, 8, "SHADOW"), (12, 8, "LIGHT"), (13, 8, "SPEC"), (14, 8, "SHADOW"),
        # y=9
        (10, 9, "SHADOW"), (11, 9, "LIGHT"), (12, 9, "SPEC"), (13, 9, "LIGHT"), (14, 9, "MID"), (15, 9, "SHADOW"),
        # y=10
        (9, 10, "SHADOW"), (10, 10, "LIGHT"), (11, 10, "MID"), (12, 10, "MID"), (13, 10, "BASE"), (14, 10, "SHADOW"),
        # y=11
        (9, 11, "SHADOW"), (10, 11, "MID"), (11, 11, "BASE"), (12, 11, "BASE"), (13, 11, "SHADOW"),
        # y=12
        (10, 12, "SHADOW"), (11, 12, "SHADOW"), (12, 12, "SHADOW"),
    ]
    for x, y, c in p2:
        im.putpixel((x, y), PAL[c])
        
    # Tiny pebble 3 in front (x=7..9, y=12..13)
    p3 = [
        (8, 12, "SPEC"), (9, 12, "LIGHT"),
        (7, 13, "SHADOW"), (8, 13, "MID"), (9, 13, "BASE"), (10, 13, "SHADOW")
    ]
    for x, y, c in p3:
        im.putpixel((x, y), PAL[c])
        
    return im

def create_mushroom():
    # 16x16 RGBA - Soul Knight style Mystic Bioluminescent Purple Mushroom
    im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    
    PAL = {
        "OUT":    (18, 10, 30, 255),    # #120A1E Deep void outline
        "CAP_D":  (54, 28, 77, 255),    # #361C4D Cap shadow
        "CAP_M":  (107, 45, 138, 255),  # #6B2D8A Cap midtone
        "CAP_L":  (168, 85, 247, 255),  # #A855F7 Cap vibrant purple
        "CAP_HI": (216, 180, 254, 255), # #D8B4FE Cap top highlight
        
        "CYAN_GLOW":(0, 229, 255, 255), # #00E5FF Bioluminescent cyan
        "CYAN_AURA":(0, 180, 216, 255), # #00B4D8 Cyan aura
        "WHITE":    (255, 255, 255, 255),# Blinding white glow spot
        
        "STEM_D": (155, 142, 181, 255),# #9B8EB5 Stem shadow
        "STEM_L": (232, 226, 242, 255),# #E8E2F2 Stem light
        
        "MOSS_D": (45, 106, 79, 255),   # #2D6A4F Moss dark
        "MOSS_L": (82, 183, 136, 255),  # #52B788 Moss light
    }
    
    # Ground shadow at y=15
    for x in range(5, 12):
        im.putpixel((x, 15), PAL["OUT"])
        
    # Stem (x=7..9, y=9..14)
    for y in range(9, 14):
        im.putpixel((6, y), PAL["OUT"])
        im.putpixel((7, y), PAL["STEM_L"])
        im.putpixel((8, y), PAL["STEM_L"])
        im.putpixel((9, y), PAL["STEM_D"])
        im.putpixel((10, y), PAL["OUT"])
        
    # Mossy base at y=13..14
    im.putpixel((5, 14), PAL["OUT"])
    im.putpixel((6, 14), PAL["MOSS_D"])
    im.putpixel((7, 14), PAL["MOSS_L"])
    im.putpixel((8, 14), PAL["MOSS_D"])
    im.putpixel((9, 14), PAL["MOSS_L"])
    im.putpixel((10, 14), PAL["OUT"])
    im.putpixel((11, 14), PAL["OUT"])

    # Wide Umbrella Cap (y=2..9, x=2..13)
    cap = [
        # y=2 (cap top)
        (6, 2, "OUT"), (7, 2, "CAP_HI"), (8, 2, "CAP_HI"), (9, 2, "OUT"),
        
        # y=3
        (4, 3, "OUT"), (5, 3, "CAP_L"), (6, 3, "CAP_HI"), (7, 3, "WHITE"), (8, 3, "CYAN_GLOW"), (9, 3, "CAP_L"), (10, 3, "CAP_M"), (11, 3, "OUT"),
        
        # y=4
        (3, 4, "OUT"), (4, 4, "CAP_L"), (5, 4, "CYAN_AURA"), (6, 4, "CYAN_GLOW"), (7, 4, "WHITE"), (8, 4, "CYAN_GLOW"), (9, 4, "CAP_L"), (10, 4, "CAP_M"), (11, 4, "CAP_D"), (12, 4, "OUT"),
        
        # y=5
        (2, 5, "OUT"), (3, 5, "CAP_L"), (4, 5, "CAP_L"), (5, 5, "CAP_L"), (6, 5, "CYAN_AURA"), (7, 5, "CAP_L"), (8, 5, "CAP_M"), (9, 5, "WHITE"), (10, 5, "CYAN_GLOW"), (11, 5, "CAP_D"), (12, 5, "CAP_D"), (13, 5, "OUT"),
        
        # y=6
        (2, 6, "OUT"), (3, 6, "CAP_L"), (4, 6, "WHITE"), (5, 6, "CYAN_GLOW"), (6, 6, "CAP_L"), (7, 6, "CAP_M"), (8, 6, "CAP_M"), (9, 6, "CYAN_GLOW"), (10, 6, "CAP_M"), (11, 6, "CAP_D"), (12, 6, "CAP_D"), (13, 6, "OUT"),
        
        # y=7 (gills & undercap)
        (2, 7, "OUT"), (3, 7, "CAP_M"), (4, 7, "CYAN_AURA"), (5, 7, "CAP_M"), (6, 7, "CAP_M"), (7, 7, "CAP_D"), (8, 7, "CAP_D"), (9, 7, "CAP_D"), (10, 7, "CAP_D"), (11, 7, "CAP_D"), (12, 7, "OUT"),
        
        # y=8 (cap rim lower edge)
        (3, 8, "OUT"), (4, 8, "OUT"), (5, 8, "CAP_D"), (6, 8, "OUT"), (10, 8, "OUT"), (11, 8, "OUT")
    ]
    for x, y, c in cap:
        im.putpixel((x, y), PAL[c])
        
    return im

def sync_prop(im, name):
    # 1. Props in My project
    p1 = f"My project/Assets/Resources/Ember/Props/{name}.png"
    os.makedirs(os.path.dirname(p1), exist_ok=True)
    im.save(p1)
    
    # 2. ArtShowcase raw
    p2 = f"ArtShowcase/raw/{name}.png"
    os.makedirs(os.path.dirname(p2), exist_ok=True)
    im.save(p2)
    
    # 3. ArtShowcase zoomed_8x
    im_8x = im.resize((im.width * 8, im.height * 8), Image.Resampling.NEAREST)
    p3 = f"ArtShowcase/zoomed_8x/{name}.png"
    p3b = f"ArtShowcase/zoomed_8x/{name}_8x.png"
    os.makedirs("ArtShowcase/zoomed_8x", exist_ok=True)
    im_8x.save(p3)
    im_8x.save(p3b)
    print(f"Prop {name} synchronized (1x: {im.size}, 8x: {im_8x.size})")

if __name__ == "__main__":
    wf = create_wildflowers()
    sync_prop(wf, "wildflowers")
    
    sp = create_stone_pebble()
    sync_prop(sp, "stone_pebble")
    
    mush = create_mushroom()
    sync_prop(mush, "mushroom")
    print("All 3 ecology props generated and synced!")
