"""
Script to generate Soul Knight style Abandoned Mystery Chest (Pandora Box) sprites:
1. abandoned_chest.png (32x32)
2. abandoned_chest_open.png (32x32)

Adhering strictly to pixel-art skill rules:
- Palette first (DB32 / Endesga 32 dark fantasy tones)
- Zero anti-aliasing (alpha 0 or 255 only)
- Zero orphan pixels (all pixels 4-connected in groups >= 2)
- 8x8 grid compliant
- Soul Knight thick 1px dark contour and vibrant cel-shading
"""
from PIL import Image

# Palette definition
C_TRANS = (0, 0, 0, 0)
C_DARK = (20, 16, 26, 255)         # 1px Dark contour / shadow
C_SHADOW_G = (24, 20, 32, 255)      # Ground contact shadow

# Wood tones (Ancient weathered mahogany)
C_W_DEEP = (44, 22, 28, 255)       # Deepest wood shadow / plank lines
C_W_DARK = (68, 34, 38, 255)       # Wood shadow
C_W_MID  = (108, 54, 52, 255)      # Wood midtone
C_W_LIGHT= (154, 82, 70, 255)      # Wood highlight
C_W_SPEC = (194, 118, 98, 255)     # Wood top specular

# Iron/Steel band tones (Cold dark forged iron)
C_I_DARK = (38, 43, 56, 255)       # Iron shadow
C_I_MID  = (62, 72, 94, 255)       # Iron midtone
C_I_LIGHT= (98, 114, 142, 255)     # Iron highlight
C_I_SPEC = (156, 172, 202, 255)    # Iron specular

# Brass / Bronze Rivets & Trims (Ancient brass)
C_B_DARK = (102, 64, 24, 255)      # Brass shadow
C_B_MID  = (174, 122, 44, 255)     # Brass mid
C_B_LIGHT= (230, 182, 68, 255)     # Brass highlight / rivet
C_B_SPEC = (255, 232, 148, 255)    # Brass specular sparkle

# Skull Lock tones (Pale bone with ominous glowing cyan eyes)
C_SK_DARK  = (92, 96, 112, 255)    # Bone shadow
C_SK_MID   = (152, 160, 178, 255)  # Bone midtone
C_SK_LIGHT = (212, 218, 230, 255)  # Bone highlight
C_SK_SPEC  = (248, 250, 255, 255)  # Bone specular
C_CYAN_DEEP= (0, 128, 160, 255)    # Eye socket glow
C_CYAN_MID = (0, 210, 230, 255)    # Neon cyan eye glow
C_CYAN_HOT = (168, 252, 255, 255)  # Glowing eye core

# Interior Treasure tones (for open chest)
C_GOLD_DEEP = (136, 84, 20, 255)   # Gold shadow
C_GOLD_MID  = (218, 154, 38, 255)  # Gold coins mid
C_GOLD_LIGHT= (250, 214, 60, 255)  # Gold coins highlight
C_GOLD_SPEC = (255, 248, 168, 255) # Gold specular glint

# Pandora Magic Crystal (Starlight Cyan & Violet)
C_PURP_DEEP = (96, 32, 128, 255)   # Void purple shadow
C_PURP_MID  = (164, 64, 212, 255)  # Void purple mid
C_PURP_LIGHT= (224, 128, 255, 255) # Void purple glow
C_WHITE     = (255, 255, 255, 255) # Glint white

def create_closed_chest():
    img = Image.new("RGBA", (32, 32), C_TRANS)
    pixels = img.load()

    # Ground contact shadow (y=27..28)
    for x in range(5, 27):
        pixels[x, 28] = C_SHADOW_G
    for x in range(7, 25):
        pixels[x, 29] = C_SHADOW_G

    # 1. Chest Body & Lid Outer Silhouette (Dark contour)
    # y=9 to y=27
    # Top lid curve: y=9 (x=9..22), y=10 (x=7..24), y=11 (x=5..26), y=12..26 (x=5..26), y=27 (x=5..26)
    
    # Fill main wood body
    for y in range(12, 28):
        for x in range(6, 26):
            pixels[x, y] = C_W_MID

    # Lid dome
    for x in range(10, 22):
        pixels[x, 10] = C_W_LIGHT
    for x in range(8, 24):
        pixels[x, 11] = C_W_LIGHT
        
    # Top-right lighting on wood
    for y in range(13, 27):
        for x in range(15, 24):
            pixels[x, y] = C_W_LIGHT
        for x in range(7, 12):
            pixels[x, y] = C_W_DARK
        for x in range(6, 8):
            pixels[x, y] = C_W_DEEP

    # Horizontal seam where lid meets base (y=16)
    for x in range(6, 26):
        pixels[x, 16] = C_W_DEEP
        pixels[x, 17] = C_DARK

    # Vertical Wood Plank groove lines (x=10, x=21)
    for y in range(18, 27):
        pixels[10, y] = C_W_DEEP
        pixels[21, y] = C_W_DEEP
    for y in range(12, 16):
        pixels[10, y] = C_W_DEEP
        pixels[21, y] = C_W_DEEP

    # Metal Forged Iron Corner Bands & Center Reinforcement
    # Left band: x=6..8
    # Center band: x=14..17 (under lock)
    # Right band: x=23..25
    def draw_vertical_band(x1, x2, y1, y2):
        for y in range(y1, y2 + 1):
            for x in range(x1, x2 + 1):
                pixels[x, y] = C_I_MID
            pixels[x1, y] = C_I_DARK
            pixels[x2, y] = C_I_LIGHT
            if x2 - x1 >= 2:
                pixels[x2 - 1, y] = C_I_SPEC if x2 > 20 else C_I_LIGHT

    # Draw bands on body (y=17..27)
    draw_vertical_band(6, 8, 17, 27)
    draw_vertical_band(14, 17, 17, 27)
    draw_vertical_band(23, 25, 17, 27)

    # Draw bands on lid (y=10..16)
    draw_vertical_band(6, 8, 12, 16)
    draw_vertical_band(14, 17, 10, 16)
    draw_vertical_band(23, 25, 12, 16)

    # Horizontal bottom rim band (y=26..27)
    for x in range(6, 26):
        pixels[x, 26] = C_I_MID
        pixels[x, 27] = C_I_DARK
    for x in range(16, 25):
        pixels[x, 26] = C_I_LIGHT

    # Horizontal lid rim band (y=15..16)
    for x in range(6, 26):
        pixels[x, 15] = C_I_LIGHT if x > 15 else C_I_MID
        pixels[x, 16] = C_I_DARK

    # Brass Rivets / Studs (tán đinh đồng) on metal bands
    # On lid bands:
    rivets = [
        (7, 13), (24, 13),
        (15, 11), (16, 11),
        # On body bands:
        (7, 19), (7, 24),
        (24, 19), (24, 24),
        (15, 24), (16, 24),
        # Along bottom rim:
        (11, 26), (20, 26)
    ]
    for rx, ry in rivets:
        pixels[rx, ry] = C_B_LIGHT
        if rx + 1 < 32 and pixels[rx + 1, ry] in (C_I_LIGHT, C_I_SPEC, C_I_MID):
            pixels[rx + 1, ry] = C_B_SPEC
        pixels[rx, ry + 1] = C_B_DARK

    # 2. Iconic Pandora Skull Lock (Centered x=13..18, y=14..20)
    # Skull shape:
    # y=14: x=14..17 (cranium top)
    # y=15: x=13..18 (cranium wide)
    # y=16: x=13..18 (eye row)
    # y=17: x=14..17 (cheekbones)
    # y=18: x=14..17 (jaw/teeth)
    # y=19: x=15..16 (lock shackle loop bottom)

    # Skull background shadow / outline
    for y in range(14, 19):
        for x in range(13, 19):
            pixels[x, y] = C_SK_MID

    # Cranium highlights (top-right light)
    for x in range(14, 18):
        pixels[x, 14] = C_SK_LIGHT
    pixels[16, 14] = C_SK_SPEC
    pixels[17, 14] = C_SK_SPEC
    pixels[17, 15] = C_SK_SPEC

    for x in range(13, 15):
        pixels[x, 15] = C_SK_DARK
        pixels[x, 16] = C_SK_DARK

    # Glowing Cyan Eye Sockets (x=14, x=17 at y=16)
    pixels[14, 16] = C_CYAN_HOT
    pixels[15, 16] = C_CYAN_MID
    pixels[17, 16] = C_CYAN_HOT
    pixels[16, 16] = C_CYAN_MID
    # Eye brows
    pixels[14, 15] = C_DARK
    pixels[17, 15] = C_DARK

    # Nose cavity & teeth
    pixels[15, 17] = C_DARK
    pixels[16, 17] = C_DARK
    pixels[14, 18] = C_SK_LIGHT
    pixels[15, 18] = C_DARK       # keyhole gap
    pixels[16, 18] = C_DARK       # keyhole gap
    pixels[17, 18] = C_SK_LIGHT

    # Lock Shackle loop (Vòng khóa ngậm dưới hàm y=19..20)
    pixels[14, 19] = C_B_MID
    pixels[15, 19] = C_B_LIGHT
    pixels[16, 19] = C_B_LIGHT
    pixels[17, 19] = C_B_MID
    pixels[15, 20] = C_B_DARK
    pixels[16, 20] = C_B_DARK

    # Outer 1px Crisp Black Contour around the whole chest
    # Trace exterior of opaque pixels
    temp = img.copy()
    tp = temp.load()
    for y in range(32):
        for x in range(32):
            if tp[x, y][3] > 0:
                continue
            # Check 4-neighbors
            has_opaque = False
            for dx, dy in [(-1,0), (1,0), (0,-1), (0,1)]:
                nx, ny = x + dx, y + dy
                if 0 <= nx < 32 and 0 <= ny < 32:
                    if tp[nx, ny][3] > 0 and tp[nx, ny] != C_SHADOW_G:
                        has_opaque = True
                        break
            if has_opaque:
                pixels[x, y] = C_DARK

    return img


def create_open_chest():
    img = Image.new("RGBA", (32, 32), C_TRANS)
    pixels = img.load()

    # Ground contact shadow (y=27..28)
    for x in range(5, 27):
        pixels[x, 28] = C_SHADOW_G
    for x in range(7, 25):
        pixels[x, 29] = C_SHADOW_G

    # 1. OPEN LID (Tilted back at ~50° angle, y=4 to y=14)
    # The lid is thrown back, showing:
    # - Rim arched top edge (y=4..7)
    # - Underside lid planks (y=7..13)
    # - Side panels angled back
    
    # Outer frame of the open lid
    # Top arched lid rim:
    for x in range(9, 23):
        pixels[x, 4] = C_I_MID
    for x in range(7, 25):
        pixels[x, 5] = C_I_LIGHT if x > 15 else C_I_MID
    for x in range(6, 26):
        pixels[x, 6] = C_I_LIGHT if x > 16 else C_I_MID

    # Underside interior of the lid (dark recessed wood in shadow)
    for y in range(7, 14):
        for x in range(7, 25):
            pixels[x, y] = C_W_DEEP
    # Planks in lid interior
    for y in range(7, 14):
        pixels[11, y] = C_DARK
        pixels[16, y] = C_DARK
        pixels[20, y] = C_DARK
        # Subtle wood texture
        for x in range(12, 16):
            pixels[x, y] = C_W_DARK
        for x in range(21, 24):
            pixels[x, y] = C_W_MID

    # Lid side metal brackets & hinge joints
    # Hinges at left (x=5..7, y=13..15) and right (x=24..26, y=13..15)
    for y in range(13, 16):
        pixels[5, y] = C_I_MID
        pixels[6, y] = C_I_LIGHT
        pixels[7, y] = C_I_DARK
        pixels[24, y] = C_I_DARK
        pixels[25, y] = C_I_LIGHT
        pixels[26, y] = C_I_SPEC

    # Brass hinge pins
    pixels[6, 14] = C_B_LIGHT
    pixels[25, 14] = C_B_SPEC

    # 2. CHEST BASE (y=15 to y=27)
    # Front and sides of the chest box
    for y in range(16, 28):
        for x in range(6, 26):
            pixels[x, y] = C_W_MID

    # Wood shading
    for y in range(17, 27):
        for x in range(16, 24):
            pixels[x, y] = C_W_LIGHT
        for x in range(7, 12):
            pixels[x, y] = C_W_DARK
        for x in range(6, 8):
            pixels[x, y] = C_W_DEEP

    # Front corner bands and center latch band
    def draw_body_band(x1, x2, y1, y2):
        for y in range(y1, y2 + 1):
            for x in range(x1, x2 + 1):
                pixels[x, y] = C_I_MID
            pixels[x1, y] = C_I_DARK
            pixels[x2, y] = C_I_LIGHT
            if x2 - x1 >= 2:
                pixels[x2 - 1, y] = C_I_SPEC if x2 > 20 else C_I_LIGHT

    draw_body_band(6, 8, 17, 27)
    draw_body_band(14, 17, 18, 27)
    draw_body_band(23, 25, 17, 27)

    # Bottom rim band (y=26..27)
    for x in range(6, 26):
        pixels[x, 26] = C_I_MID
        pixels[x, 27] = C_I_DARK
    for x in range(16, 25):
        pixels[x, 26] = C_I_LIGHT

    # Front opening lip rim (y=16)
    for x in range(6, 26):
        pixels[x, 16] = C_I_LIGHT if x > 15 else C_I_MID
    pixels[14, 16] = C_I_DARK
    pixels[17, 16] = C_I_DARK

    # Rivets on body
    rivets = [
        (7, 19), (7, 24),
        (24, 19), (24, 24),
        (15, 24), (16, 24),
        (11, 26), (20, 26)
    ]
    for rx, ry in rivets:
        pixels[rx, ry] = C_B_LIGHT
        if rx + 1 < 32 and pixels[rx + 1, ry] in (C_I_LIGHT, C_I_SPEC, C_I_MID):
            pixels[rx + 1, ry] = C_B_SPEC
        pixels[rx, ry + 1] = C_B_DARK

    # 3. INTERIOR CAVITY (y=13 to y=17) OVERFLOWING WITH TREASURE
    # Deep inner cavity shadow
    for y in range(13, 16):
        for x in range(8, 24):
            pixels[x, y] = C_DARK

    # Heaps of Gold Coins (y=14..16, x=8..23)
    coin_coords_mid = [
        (8, 15), (9, 15), (10, 15), (11, 15), (12, 15), (13, 15),
        (18, 15), (19, 15), (20, 15), (21, 15), (22, 15), (23, 15),
        (9, 16), (10, 16), (11, 16), (12, 16), (13, 16), (18, 16), (19, 16), (20, 16), (21, 16), (22, 16)
    ]
    for cx, cy in coin_coords_mid:
        pixels[cx, cy] = C_GOLD_MID

    coin_coords_light = [
        (9, 14), (10, 14), (11, 14), (12, 14), (13, 14),
        (18, 14), (19, 14), (20, 14), (21, 14), (22, 14),
        (10, 15), (12, 15), (19, 15), (21, 15)
    ]
    for cx, cy in coin_coords_light:
        pixels[cx, cy] = C_GOLD_LIGHT

    coin_specs = [(11, 14), (13, 14), (20, 14), (22, 14), (10, 16), (21, 16)]
    for cx, cy in coin_specs:
        pixels[cx, cy] = C_GOLD_SPEC

    # Gold Ingot Bars stacked on the right
    # Ingot 1 (x=19..22, y=13)
    for x in range(19, 23):
        pixels[x, 13] = C_GOLD_LIGHT
    pixels[21, 13] = C_GOLD_SPEC
    pixels[22, 13] = C_GOLD_SPEC

    # 4. PANDORA MAGIC CRYSTALS IN CENTER (Cyan & Void Purple Gemstones)
    # Cyan Crystal Cluster (x=14..17, y=12..16)
    # Hexagonal / shard shape:
    crystals = [
        (15, 12, C_WHITE), (16, 12, C_CYAN_HOT),
        (14, 13, C_CYAN_HOT), (15, 13, C_CYAN_HOT), (16, 13, C_CYAN_MID), (17, 13, C_CYAN_DEEP),
        (14, 14, C_CYAN_MID), (15, 14, C_CYAN_HOT), (16, 14, C_CYAN_MID), (17, 14, C_CYAN_DEEP),
        (14, 15, C_CYAN_DEEP), (15, 15, C_CYAN_MID), (16, 15, C_CYAN_DEEP), (17, 15, C_CYAN_DEEP)
    ]
    for cx, cy, col in crystals:
        pixels[cx, cy] = col

    # Violet Gemstone next to it (x=12..13, y=13..14)
    pixels[12, 13] = C_PURP_LIGHT
    pixels[13, 13] = C_WHITE
    pixels[12, 14] = C_PURP_DEEP
    pixels[13, 14] = C_PURP_MID

    # 5. Open Skull Latch Hanging on Front (y=17..21, x=13..18)
    # When open, the skull latch plate is resting on the front lip
    for y in range(17, 21):
        for x in range(14, 18):
            pixels[x, y] = C_SK_MID
    pixels[15, 17] = C_SK_LIGHT
    pixels[16, 17] = C_SK_SPEC
    pixels[17, 17] = C_SK_SPEC

    # Eyes glowing cyan
    pixels[14, 18] = C_CYAN_HOT
    pixels[15, 18] = C_CYAN_MID
    pixels[16, 18] = C_CYAN_MID
    pixels[17, 18] = C_CYAN_HOT

    pixels[14, 19] = C_SK_LIGHT
    pixels[15, 19] = C_DARK
    pixels[16, 19] = C_DARK
    pixels[17, 19] = C_SK_LIGHT

    # Hanging shackle loop
    pixels[15, 20] = C_B_LIGHT
    pixels[16, 20] = C_B_LIGHT
    pixels[15, 21] = C_B_DARK
    pixels[16, 21] = C_B_DARK

    # 6. Ethereal Rising Starlight Mist (Connected 2x2 and 2x1 clusters, ZERO orphans!)
    # Rising magical energy whisps from the crystals:
    wisp_pixels = [
        # Center cyan wisp rising (y=8..11, x=15..16)
        (15, 11, C_CYAN_HOT), (16, 11, C_CYAN_HOT),
        (15, 10, C_CYAN_MID), (16, 10, C_CYAN_HOT),
        (15, 9, C_CYAN_MID),  (16, 9, C_CYAN_MID),
        (16, 8, C_CYAN_HOT),  (17, 8, C_CYAN_HOT),
        (16, 7, C_CYAN_MID),  (17, 7, C_CYAN_MID),
        
        # Violet ethereal spark (x=12..13, y=10..11)
        (12, 11, C_PURP_LIGHT), (13, 11, C_PURP_LIGHT),
        (12, 10, C_PURP_MID),   (13, 10, C_PURP_LIGHT),
        
        # High starlight sparkle (x=18..19, y=5..6)
        (18, 6, C_CYAN_HOT), (19, 6, C_WHITE),
        (18, 5, C_CYAN_MID), (19, 5, C_CYAN_HOT),
    ]
    for wx, wy, wcol in wisp_pixels:
        pixels[wx, wy] = wcol

    # Outer 1px Crisp Black Contour
    temp = img.copy()
    tp = temp.load()
    for y in range(32):
        for x in range(32):
            if tp[x, y][3] > 0:
                continue
            has_opaque = False
            for dx, dy in [(-1,0), (1,0), (0,-1), (0,1)]:
                nx, ny = x + dx, y + dy
                if 0 <= nx < 32 and 0 <= ny < 32:
                    if tp[nx, ny][3] > 0 and tp[nx, ny] != C_SHADOW_G:
                        has_opaque = True
                        break
            if has_opaque:
                pixels[x, y] = C_DARK

    return img

if __name__ == "__main__":
    closed = create_closed_chest()
    closed.save("test_chest_closed.png")
    opened = create_open_chest()
    opened.save("test_chest_open.png")
    print("Generated test_chest_closed.png and test_chest_open.png")
