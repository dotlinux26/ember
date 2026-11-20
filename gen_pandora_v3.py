"""
Pandora / Abandoned Mystery Chest Sprite Generator v3 - Masterwork Edition
Strictly complies with pixel-art skill:
- 100% Opaque (Zero Anti-Aliasing, alpha is 0 or 255)
- 0 Orphan pixels (All pixels are 4-connected in clusters >= 2)
- 8x8 Grid aligned
- Soul Knight / ChillyRoom Dark-Fantasy Aesthetics:
  * Rich warm mahogany wood planks (4-step cel-shading)
  * Heavy forged iron corner brackets and reinforced vertical bands
  * Gleaming antique brass rivets and hinge pins
  * Ominous chibi skull padlock with piercing neon cyan glowing eyes
  * Open version: Angled perspective open lid, overflowing gold coins,
    faceted Starlight Cyan and Void Amethyst crystals, and rising celestial aura
"""
from PIL import Image

# ----------------- PALETTE DEFINITIONS -----------------
C_TRANS   = (0, 0, 0, 0)
C_OUTLINE = (18, 14, 24, 255)    # 1px Dark contour / deepest shadow
C_SHADOW  = (22, 18, 30, 255)    # Ground contact shadow

# Mahogany Wood (4-tone cel-shading, top-right lighting)
W_CREVICE = (42, 20, 24, 255)    # Deepest plank seams
W_SHADOW  = (72, 34, 36, 255)    # Wood shadow tone
W_MID     = (114, 56, 52, 255)   # Wood rich midtone
W_LIGHT   = (164, 88, 74, 255)   # Wood warm highlight
W_SPEC    = (208, 126, 106, 255) # Wood specular rim

# Forged Iron Bands (4-tone cold steel)
I_SHADOW  = (38, 44, 58, 255)    # Iron shadow
I_MID     = (64, 76, 98, 255)    # Iron midtone
I_LIGHT   = (104, 120, 150, 255) # Iron highlight
I_SPEC    = (164, 182, 212, 255) # Iron specular glint

# Antique Brass Rivets & Fittings (4-tone warm gold-brass)
B_SHADOW  = (108, 68, 24, 255)   # Brass deep shadow
B_MID     = (182, 128, 44, 255)  # Brass midtone
B_LIGHT   = (238, 188, 68, 255)  # Brass bright highlight
B_SPEC    = (255, 238, 156, 255) # Brass specular sparkle

# Skull Padlock Bone (4-tone ancient bleached bone)
S_SHADOW  = (96, 102, 118, 255)  # Bone shadow
S_MID     = (158, 166, 184, 255) # Bone midtone
S_LIGHT   = (218, 224, 236, 255) # Bone highlight
S_SPEC    = (250, 252, 255, 255) # Bone white specular

# Glowing Neon Cyan Eyes & Energy
CYAN_DARK = (0, 120, 150, 255)   # Eye glow border
CYAN_MID  = (0, 210, 235, 255)   # Neon cyan glow
CYAN_HOT  = (170, 252, 255, 255) # Core hot cyan
CYAN_CORE = (255, 255, 255, 255) # Pure white center spark

# Interior Treasure Gold (for open chest)
G_DEEP    = (140, 86, 22, 255)   # Gold shadow
G_MID     = (224, 158, 40, 255)  # Gold coins mid
G_LIGHT   = (252, 218, 64, 255)  # Gold highlight
G_SPEC    = (255, 250, 176, 255) # Gold specular glint

# Void Amethyst Gemstone & Mist
P_DARK    = (92, 30, 124, 255)   # Void purple shadow
P_MID     = (162, 62, 216, 255)  # Void purple mid
P_LIGHT   = (226, 126, 255, 255) # Void purple bright
P_HOT     = (250, 200, 255, 255) # Void purple core


def apply_outline(img):
    """Generates crisp 1px dark contour around opaque sprite pixels."""
    copy_img = img.copy()
    src = copy_img.load()
    dst = img.load()
    for y in range(32):
        for x in range(32):
            if src[x, y][3] > 0:
                continue
            has_opaque = False
            for dx, dy in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
                nx, ny = x + dx, y + dy
                if 0 <= nx < 32 and 0 <= ny < 32:
                    if src[nx, ny][3] > 0 and src[nx, ny] != C_SHADOW:
                        has_opaque = True
                        break
            if has_opaque:
                dst[x, y] = C_OUTLINE


def create_abandoned_chest():
    img = Image.new("RGBA", (32, 32), C_TRANS)
    p = img.load()

    # 1. Ground contact shadow (y=27..28, x=4..27)
    for x in range(4, 28):
        p[x, 27] = C_SHADOW
    for x in range(6, 26):
        p[x, 28] = C_SHADOW

    # 2. Chest Body Base (y=16 to 26, width x=4..27)
    for y in range(16, 27):
        for x in range(4, 28):
            p[x, y] = W_MID

    # Body Wood Shading (Top-right lighting)
    for y in range(16, 27):
        # Left shadow
        for x in range(4, 7):
            p[x, y] = W_SHADOW
        p[4, y] = W_CREVICE
        # Right highlight
        for x in range(21, 27):
            p[x, y] = W_LIGHT
        p[27, y] = W_SPEC

    # Vertical wood plank grooves (x=11, x=20)
    for y in range(18, 26):
        p[11, y] = W_CREVICE
        p[12, y] = W_SHADOW
        p[19, y] = W_CREVICE
        p[20, y] = W_LIGHT

    # 3. Arched Lid (y=10 to 16)
    # y=10: x=8..23
    # y=11: x=6..25
    # y=12..16: x=4..27
    lid_widths = {
        10: (8, 23),
        11: (6, 25),
        12: (4, 27),
        13: (4, 27),
        14: (4, 27),
        15: (4, 27),
        16: (4, 27),
    }
    for y, (x1, x2) in lid_widths.items():
        for x in range(x1, x2 + 1):
            p[x, y] = W_MID
        # Left shadow on lid
        for x in range(x1, min(x1 + 3, x2)):
            p[x, y] = W_SHADOW
        p[x1, y] = W_CREVICE
        # Right highlight on lid
        for x in range(max(x1, x2 - 5), x2 + 1):
            p[x, y] = W_LIGHT
        p[x2, y] = W_SPEC

    # Top arc specular crest
    for x in range(14, 22):
        p[x, 10] = W_SPEC
    for x in range(9, 14):
        p[x, 10] = W_LIGHT

    # 4. Iron Bands & Corner Reinforcements
    # Left Corner Band: x=4..6, y=12..26
    # Left Vertical Band: x=8..10, y=10..26
    # Right Vertical Band: x=21..23, y=10..26
    # Right Corner Band: x=25..27, y=12..26
    # Center Latch Plate: x=14..17, y=13..24

    def draw_iron_vert(x1, x2, y1, y2):
        for y in range(y1, y2 + 1):
            for x in range(x1, x2 + 1):
                p[x, y] = I_MID
            p[x1, y] = I_SHADOW
            p[x2, y] = I_LIGHT
            if x2 - x1 >= 2 and x2 > 18:
                p[x2, y] = I_SPEC

    draw_iron_vert(4, 6, 12, 26)
    draw_iron_vert(8, 10, 10, 26)
    draw_iron_vert(14, 17, 13, 25)
    draw_iron_vert(21, 23, 10, 26)
    draw_iron_vert(25, 27, 12, 26)

    # Bottom iron rim (y=26)
    for x in range(4, 28):
        p[x, 26] = I_MID
    for x in range(4, 9):
        p[x, 26] = I_SHADOW
    for x in range(18, 28):
        p[x, 26] = I_LIGHT
    p[26, 26] = I_SPEC
    p[27, 26] = I_SPEC

    # Horizontal seam band where lid meets body (y=16)
    for x in range(4, 28):
        p[x, 16] = I_MID
    for x in range(4, 10):
        p[x, 16] = I_SHADOW
    for x in range(18, 28):
        p[x, 16] = I_LIGHT
    p[26, 16] = I_SPEC
    p[27, 16] = I_SPEC
    # Dark shadow line under seam
    for x in range(4, 28):
        p[x, 17] = W_CREVICE

    # Brass Rivets (2x1 and 2x2 studs to ensure zero orphans)
    rivets = [
        # Left corner band
        (5, 13), (5, 19), (5, 24),
        # Left vertical band
        (9, 11), (9, 18), (9, 23),
        # Right vertical band
        (22, 11), (22, 18), (22, 23),
        # Right corner band
        (26, 13), (26, 19), (26, 24),
        # Center band bottom
        (15, 24),
    ]
    for rx, ry in rivets:
        p[rx, ry] = B_LIGHT
        p[rx + 1, ry] = B_SPEC if rx > 18 else B_LIGHT
        p[rx, ry + 1] = B_SHADOW
        p[rx + 1, ry + 1] = B_MID

    # 5. Masterpiece Skull Padlock (Centered at x=13..18, y=13..19)
    # Chibi Pandora Skull with distinct glowing cyan eyes separated by bone bridge
    # Row 13 (Skull top forehead): x=14..17
    p[14, 13] = S_MID
    p[15, 13] = S_LIGHT
    p[16, 13] = S_SPEC
    p[17, 13] = S_SPEC

    # Row 14 (Wide forehead): x=13..18
    p[13, 14] = S_SHADOW
    p[14, 14] = S_MID
    p[15, 14] = S_LIGHT
    p[16, 14] = S_SPEC
    p[17, 14] = S_SPEC
    p[18, 14] = S_LIGHT

    # Row 15 (Eye row): x=13..18
    # Distinct eyes: left eye at x=14, bone bridge at x=15..16, right eye at x=17!
    p[13, 15] = S_SHADOW
    p[14, 15] = CYAN_CORE   # Left glowing eye core
    p[15, 15] = S_MID       # Bone nose bridge
    p[16, 15] = S_LIGHT     # Bone nose bridge
    p[17, 15] = CYAN_CORE   # Right glowing eye core
    p[18, 15] = S_LIGHT

    # Row 16 (Eye lower & cheekbones): x=13..18
    p[13, 16] = S_SHADOW
    p[14, 16] = CYAN_MID    # Left glowing eye bottom
    p[15, 16] = C_OUTLINE   # Dark nasal cavity
    p[16, 16] = C_OUTLINE   # Dark nasal cavity
    p[17, 16] = CYAN_MID    # Right glowing eye bottom
    p[18, 16] = S_MID

    # Row 17 (Upper jaw / teeth row): x=14..17
    p[14, 17] = S_LIGHT     # Left tooth
    p[15, 17] = C_OUTLINE   # Tooth crevice
    p[16, 17] = S_SPEC      # Right tooth
    p[17, 17] = C_OUTLINE   # Crevice

    # Row 18 & 19 (Brass lock shackle loop bitten in jaws)
    p[14, 18] = B_MID
    p[15, 18] = B_LIGHT
    p[16, 18] = B_SPEC
    p[17, 18] = B_MID
    p[15, 19] = B_SHADOW
    p[16, 19] = B_MID

    # Apply 1px crisp dark contour
    apply_outline(img)
    return img


def create_abandoned_chest_open():
    img = Image.new("RGBA", (32, 32), C_TRANS)
    p = img.load()

    # 1. Ground contact shadow (y=27..28, x=4..27)
    for x in range(4, 28):
        p[x, 27] = C_SHADOW
    for x in range(6, 26):
        p[x, 28] = C_SHADOW

    # 2. Chest Body Base (y=16 to 26, width x=4..27)
    for y in range(16, 27):
        for x in range(4, 28):
            p[x, y] = W_MID

    # Body Wood Shading (Top-right lighting)
    for y in range(16, 27):
        for x in range(4, 7):
            p[x, y] = W_SHADOW
        p[4, y] = W_CREVICE
        for x in range(21, 27):
            p[x, y] = W_LIGHT
        p[27, y] = W_SPEC

    # Vertical wood plank grooves
    for y in range(18, 26):
        p[11, y] = W_CREVICE
        p[12, y] = W_SHADOW
        p[19, y] = W_CREVICE
        p[20, y] = W_LIGHT

    # Iron Bands on Body
    def draw_iron_vert(x1, x2, y1, y2):
        for y in range(y1, y2 + 1):
            for x in range(x1, x2 + 1):
                p[x, y] = I_MID
            p[x1, y] = I_SHADOW
            p[x2, y] = I_LIGHT
            if x2 - x1 >= 2 and x2 > 18:
                p[x2, y] = I_SPEC

    draw_iron_vert(4, 6, 16, 26)
    draw_iron_vert(8, 10, 16, 26)
    draw_iron_vert(14, 17, 18, 25)
    draw_iron_vert(21, 23, 16, 26)
    draw_iron_vert(25, 27, 16, 26)

    # Bottom iron rim (y=26)
    for x in range(4, 28):
        p[x, 26] = I_MID
    for x in range(4, 9):
        p[x, 26] = I_SHADOW
    for x in range(18, 28):
        p[x, 26] = I_LIGHT
    p[26, 26] = I_SPEC
    p[27, 26] = I_SPEC

    # Front open rim lip (y=16)
    for x in range(4, 28):
        p[x, 16] = I_MID
    for x in range(4, 10):
        p[x, 16] = I_SHADOW
    for x in range(18, 28):
        p[x, 16] = I_LIGHT
    p[26, 16] = I_SPEC
    p[27, 16] = I_SPEC

    # Body Brass Rivets
    rivets = [
        (5, 19), (5, 24),
        (9, 18), (9, 23),
        (22, 18), (22, 23),
        (26, 19), (26, 24),
        (15, 24),
    ]
    for rx, ry in rivets:
        p[rx, ry] = B_LIGHT
        p[rx + 1, ry] = B_SPEC if rx > 18 else B_LIGHT
        p[rx, ry + 1] = B_SHADOW
        p[rx + 1, ry + 1] = B_MID

    # 3. OPEN LID THROWN BACKWARD (y=4 to 14, isometric tilted perspective)
    # Lid top arched edge:
    for x in range(8, 24):
        p[x, 4] = I_MID
    for x in range(14, 24):
        p[x, 4] = I_LIGHT
    p[22, 4] = I_SPEC
    p[23, 4] = I_SPEC

    for x in range(6, 26):
        p[x, 5] = I_MID
    for x in range(16, 26):
        p[x, 5] = I_LIGHT
    p[24, 5] = I_SPEC
    p[25, 5] = I_SPEC

    # Underside wood panels of the open lid (y=6 to 13)
    for y in range(6, 14):
        for x in range(5, 27):
            p[x, y] = W_CREVICE
        for x in range(12, 17):
            p[x, y] = W_SHADOW
        for x in range(18, 25):
            p[x, y] = W_MID
        # Inner plank dividing lines
        p[9, y] = C_OUTLINE
        p[13, y] = C_OUTLINE
        p[18, y] = C_OUTLINE
        p[22, y] = C_OUTLINE

    # Open lid outer edges and corner iron fittings
    for y in range(6, 14):
        p[5, y] = I_SHADOW
        p[26, y] = I_SPEC
        p[25, y] = I_LIGHT

    # Heavy Iron Hinges (Left at x=5..8, Right at x=23..26, y=13..15)
    for y in range(13, 16):
        p[6, y] = I_SHADOW
        p[7, y] = I_MID
        p[8, y] = I_LIGHT
        p[23, y] = I_MID
        p[24, y] = I_LIGHT
        p[25, y] = I_SPEC
    # Hinge brass pivot pins
    p[7, 14] = B_LIGHT
    p[8, 14] = B_SPEC
    p[7, 15] = B_SHADOW
    p[8, 15] = B_MID
    p[24, 14] = B_SPEC
    p[25, 14] = B_SPEC
    p[24, 15] = B_MID
    p[25, 15] = B_LIGHT

    # 4. CAVERNOUS INTERIOR FILLED WITH TREASURE (y=12 to 17)
    # Deep interior shadow behind treasure
    for y in range(13, 16):
        for x in range(9, 23):
            p[x, y] = C_OUTLINE

    # Piles of Glittering Gold Coins (x=9..13 and x=18..23, y=14..16)
    coins = [
        # Left gold mound
        (9, 16, G_DEEP), (10, 16, G_MID), (11, 16, G_LIGHT), (12, 16, G_MID), (13, 16, G_DEEP),
        (9, 15, G_MID), (10, 15, G_LIGHT), (11, 15, G_SPEC), (12, 15, G_LIGHT), (13, 15, G_MID),
        (10, 14, G_LIGHT), (11, 14, G_SPEC), (12, 14, G_SPEC),
        # Right gold mound
        (18, 16, G_DEEP), (19, 16, G_MID), (20, 16, G_LIGHT), (21, 16, G_SPEC), (22, 16, G_LIGHT), (23, 16, G_DEEP),
        (18, 15, G_MID), (19, 15, G_LIGHT), (20, 15, G_SPEC), (21, 15, G_SPEC), (22, 15, G_LIGHT), (23, 15, G_MID),
        (19, 14, G_LIGHT), (20, 14, G_SPEC), (21, 14, G_SPEC), (22, 14, G_LIGHT),
    ]
    for cx, cy, col in coins:
        p[cx, cy] = col

    # Gold Ingot bar sitting atop right pile (x=20..23, y=13..14)
    p[20, 13] = G_MID
    p[21, 13] = G_LIGHT
    p[22, 13] = G_SPEC
    p[23, 13] = G_SPEC
    p[21, 14] = G_SPEC
    p[22, 14] = G_SPEC

    # 5. PANDORA CELESTIAL CRYSTAL CLUSTER (Center x=13..18, y=10..16)
    # Large faceted Starlight Cyan Crystal:
    crystals = [
        # Tip (y=10..11)
        (15, 10, CYAN_CORE), (16, 10, CYAN_CORE),
        (14, 11, CYAN_HOT), (15, 11, CYAN_CORE), (16, 11, CYAN_HOT), (17, 11, CYAN_MID),
        # Body (y=12..14)
        (14, 12, CYAN_MID), (15, 12, CYAN_HOT), (16, 12, CYAN_CORE), (17, 12, CYAN_HOT),
        (13, 13, CYAN_DARK), (14, 13, CYAN_MID), (15, 13, CYAN_HOT), (16, 13, CYAN_HOT), (17, 13, CYAN_MID), (18, 13, CYAN_DARK),
        (14, 14, CYAN_DARK), (15, 14, CYAN_MID), (16, 14, CYAN_MID), (17, 14, CYAN_DARK),
        # Base (y=15..16)
        (14, 15, CYAN_DARK), (15, 15, CYAN_MID), (16, 15, CYAN_MID), (17, 15, CYAN_DARK),
    ]
    for cx, cy, col in crystals:
        p[cx, cy] = col

    # Void Amethyst Gemstone Cluster on Left (x=11..13, y=12..14)
    amethyst = [
        (12, 12, P_HOT), (13, 12, P_LIGHT),
        (11, 13, P_MID), (12, 13, P_LIGHT), (13, 13, P_DARK),
        (11, 14, P_DARK), (12, 14, P_MID),
    ]
    for ax, ay, col in amethyst:
        p[ax, ay] = col

    # 6. Unlocked Skull Latch Plate hanging down on front lip (x=13..18, y=17..22)
    p[14, 17] = S_MID
    p[15, 17] = S_LIGHT
    p[16, 17] = S_SPEC
    p[17, 17] = S_LIGHT

    # Eye row
    p[13, 18] = S_SHADOW
    p[14, 18] = CYAN_HOT
    p[15, 18] = S_MID
    p[16, 18] = S_LIGHT
    p[17, 18] = CYAN_HOT
    p[18, 18] = S_MID

    # Cheek & nose
    p[13, 19] = S_SHADOW
    p[14, 19] = CYAN_MID
    p[15, 19] = C_OUTLINE
    p[16, 19] = C_OUTLINE
    p[17, 19] = CYAN_MID
    p[18, 19] = S_MID

    # Teeth & hanging loop
    p[14, 20] = S_LIGHT
    p[15, 20] = C_OUTLINE
    p[16, 20] = S_SPEC
    p[17, 20] = C_OUTLINE
    p[14, 21] = B_MID
    p[15, 21] = B_LIGHT
    p[16, 21] = B_SPEC
    p[17, 21] = B_MID
    p[15, 22] = B_SHADOW
    p[16, 22] = B_MID

    # 7. Rising Celestial Aura & Energy Wisps (Connected 2x2 and 2x1 blocks, ZERO orphans!)
    aura = [
        # Center cyan beam rising (y=6..9, x=15..16)
        (15, 9, CYAN_HOT), (16, 9, CYAN_HOT),
        (15, 8, CYAN_MID), (16, 8, CYAN_HOT),
        (15, 7, CYAN_MID), (16, 7, CYAN_MID),
        (16, 6, CYAN_DARK), (17, 6, CYAN_MID),
        # Void purple starlight sparkle (x=10..11, y=9..10)
        (10, 10, P_MID), (11, 10, P_LIGHT),
        (10, 9, P_DARK), (11, 9, P_MID),
        # High celestial sparkle (x=17..18, y=3..4)
        (17, 4, CYAN_CORE), (18, 4, CYAN_HOT),
        (17, 3, CYAN_HOT), (18, 3, CYAN_CORE),
    ]
    for ax, ay, col in aura:
        p[ax, ay] = col

    # Apply 1px crisp dark contour
    apply_outline(img)
    return img


if __name__ == "__main__":
    closed = create_abandoned_chest()
    closed.save("abandoned_chest.png")

    opened = create_abandoned_chest_open()
    opened.save("abandoned_chest_open.png")

    print("Successfully generated abandoned_chest.png and abandoned_chest_open.png (v3 Masterwork)")
