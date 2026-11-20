"""
High-Fidelity Soul Knight Style Merchant Generator
Creates 192x48 spritesheet (4 frames of 48x48)
"""
from PIL import Image

def hex_to_rgba(h, a=255):
    h = h.lstrip('#')
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)

# Palette (Endesga-32 / DB32 / Soul Knight)
OUTLINE     = hex_to_rgba('#120e24')  # 1px Crisp dark outline
HOOD_DARK   = hex_to_rgba('#1f1933')  # Deep cloak shadow
HOOD_MID    = hex_to_rgba('#32274e')  # Cloak body
HOOD_LIGHT  = hex_to_rgba('#4e3d75')  # Cloak fold highlight
HOOD_RIM    = hex_to_rgba('#6d569e')  # Edge rim highlight
HOOD_INNER  = hex_to_rgba('#0d0a17')  # Impenetrable dark shadow under hood

GOLD_TRIM   = hex_to_rgba('#e6a827')  # Gold trim / embroidery
GOLD_LIGHT  = hex_to_rgba('#ffe153')  # Bright gold accent
BELT_BRASS  = hex_to_rgba('#f7b031')  # Belt buckle

EYE_CYAN    = hex_to_rgba('#00f0ff')  # Piercing Cyan magic eye
EYE_WHITE   = hex_to_rgba('#ffffff')  # Eye pupil specular core
EYE_DEEP    = hex_to_rgba('#00a8cc')  # Cyan halo

PACK_FRAME  = hex_to_rgba('#2b191c')  # Wooden pack frame
PACK_DARK   = hex_to_rgba('#572c29')  # Leather pack shadow
PACK_MID    = hex_to_rgba('#8c4839')  # Stitched leather pack
PACK_LIGHT  = hex_to_rgba('#b8654b')  # Leather highlight
PACK_STRAP  = hex_to_rgba('#3d211d')  # Leather straps

BEDROLL_D   = hex_to_rgba('#a87556')  # Top travel roll dark
BEDROLL_M   = hex_to_rgba('#d6a27e')  # Travel roll mid
BEDROLL_L   = hex_to_rgba('#fae0c5')  # Travel roll light

# Potions nestled in pack
GLASS_RIM   = hex_to_rgba('#b0c2de')  # Vial glass rim
GLASS_SPEC  = hex_to_rgba('#ffffff')  # Glass specular shine
POT_RED_D   = hex_to_rgba('#a81334')  # Vitality potion dark
POT_RED_M   = hex_to_rgba('#ff194d')  # Vitality potion mid
POT_RED_L   = hex_to_rgba('#ff6b8b')  # Vitality potion glow

POT_GOLD_D  = hex_to_rgba('#d97c18')  # Sun potion dark
POT_GOLD_M  = hex_to_rgba('#fcc028')  # Sun potion mid
POT_GOLD_L  = hex_to_rgba('#fff08a')  # Sun potion glow

POT_PURP_D  = hex_to_rgba('#5a1a6b')  # Void potion dark
POT_PURP_M  = hex_to_rgba('#a83bbd')  # Void potion mid
POT_PURP_L  = hex_to_rgba('#e87dff')  # Void potion glow

# Staff & Swaying Lantern (Left flank, matching (-0.55f, 1.15f))
STAFF_DARK  = hex_to_rgba('#291917')  # Dark ironwood staff
STAFF_MID   = hex_to_rgba('#4f302b')  # Staff wood mid
STAFF_HOOK  = hex_to_rgba('#1f2333')  # Iron hanger hook

LANTERN_IRON= hex_to_rgba('#1e2233')  # Lantern iron hood/base
LANTERN_GLAS= hex_to_rgba('#ffd575')  # Lantern glass
FIRE_CORE   = hex_to_rgba('#ffffff')  # Flame core
FIRE_MID    = hex_to_rgba('#fff275')  # Bright yellow flame
FIRE_AMBER  = hex_to_rgba('#ff7e29')  # Amber flame rim

BOOT_DARK   = hex_to_rgba('#181425')  # Traveler boots
BOOT_MID    = hex_to_rgba('#3b2d29')


def create_merchant_frame(frame_idx):
    canvas = Image.new('RGBA', (48, 48), (0, 0, 0, 0))
    px = canvas.load()

    # Breathing offsets:
    # Frame 0: Neutral (dy = 0)
    # Frame 1: Inhale start (dy = -1 for head & chest)
    # Frame 2: Peak inhale (dy = -1, slight outward chest expansion)
    # Frame 3: Exhale settling (dy = 0)
    dy = -1 if frame_idx in (1, 2) else 0

    # Lantern pendulum sway:
    # Lantern core reference: X=10, Y=17 (offset (-0.55f, 1.15f))
    # Frame 0: X=10, Y=17
    # Frame 1: X=9,  Y=16 (swings out-left)
    # Frame 2: X=10, Y=17 (passes center)
    # Frame 3: X=11, Y=18 (swings in-right)
    lan_offsets = [
        (0, 0),
        (-1, -1),
        (0, 0),
        (1, 1)
    ]
    lx_off, ly_off = lan_offsets[frame_idx]

    # --- 1. MASSIVE BACKPACK & BEDROLL (Drawn first, behind merchant) ---
    # Top Bedroll (Y=6..10, X=28..40)
    for y in range(6 + dy, 11 + dy):
        for x in range(28, 41):
            px[x, y] = OUTLINE
    for y in range(7 + dy, 10 + dy):
        for x in range(29, 40):
            if y == 7 + dy:
                px[x, y] = BEDROLL_L if x < 38 else BEDROLL_M
            elif y == 8 + dy:
                px[x, y] = BEDROLL_M
            else:
                px[x, y] = BEDROLL_D
    # Straps on bedroll
    for y in range(6 + dy, 11 + dy):
        px[31, y] = PACK_STRAP
        px[37, y] = PACK_STRAP

    # Main Leather Pack Body (Y=11..33, X=26..42)
    for y in range(11 + dy, 34):
        for x in range(26, 43):
            # Taper pack shape
            if y < 14 + dy and x > 40:
                continue
            if y > 30 and x > 40:
                continue
            px[x, y] = OUTLINE

    for y in range(12 + dy, 33):
        for x in range(27, 42):
            if y < 14 + dy and x > 39:
                continue
            if y > 30 and x > 39:
                continue
            # Leather shading (top-right highlight)
            if x >= 36 and y <= 20:
                px[x, y] = PACK_LIGHT
            elif x >= 31:
                px[x, y] = PACK_MID
            else:
                px[x, y] = PACK_DARK

    # Pack frame wooden rib
    for y in range(12 + dy, 32):
        px[27, y] = PACK_FRAME

    # --- 2. GLOWING POTION VIALS IN BACKPACK ---
    # Potion A: Yellow Sun Elixir (Top inner: X=30..33, Y=11..16 + dy)
    px[31, 11 + dy] = GLASS_RIM
    px[32, 11 + dy] = GLASS_RIM
    for y in range(12 + dy, 17 + dy):
        for x in range(30, 34):
            px[x, y] = OUTLINE
    for y in range(13 + dy, 16 + dy):
        for x in range(31, 33):
            px[x, y] = POT_GOLD_M
    px[31, 13 + dy] = POT_GOLD_L if frame_idx in (1, 2) else POT_GOLD_M
    px[32, 14 + dy] = POT_GOLD_D

    # Potion B: Red Vitality Elixir (Top outer: X=36..39, Y=14..19 + dy)
    px[37, 14 + dy] = GLASS_RIM
    px[38, 14 + dy] = GLASS_RIM
    for y in range(15 + dy, 20 + dy):
        for x in range(36, 40):
            px[x, y] = OUTLINE
    for y in range(16 + dy, 19 + dy):
        for x in range(37, 39):
            px[x, y] = POT_RED_M
    px[37, 16 + dy] = POT_RED_L if frame_idx in (0, 2) else POT_RED_M
    px[38, 17 + dy] = POT_RED_D

    # Potion C: Purple Void Elixir (Bottom side: X=36..40, Y=22..27)
    px[38, 22] = GLASS_RIM
    px[39, 22] = GLASS_RIM
    for y in range(23, 28):
        for x in range(36, 41):
            px[x, y] = OUTLINE
    for y in range(24, 27):
        for x in range(37, 40):
            px[x, y] = POT_PURP_M
    px[38, 24] = POT_PURP_L if frame_idx in (2, 3) else POT_PURP_M
    px[39, 25] = POT_PURP_D

    # --- 3. TRAVELER STAFF (X=11..13, Y=10..44) ---
    for y in range(11, 45):
        px[12, y] = OUTLINE
        px[13, y] = STAFF_MID
        px[14, y] = STAFF_DARK

    # Top staff gnarled head & hook
    for x in range(10, 14):
        px[x, 10] = OUTLINE
        px[x, 11] = STAFF_DARK
    px[11, 12] = STAFF_HOOK
    px[10, 13] = STAFF_HOOK

    # --- 4. SWAYING LANTERN (Left flank matching (-0.55f, 1.15f)) ---
    # Base lantern center: X=10, Y=17 (with sway offset)
    cx = 10 + lx_off
    cy = 17 + ly_off

    # Suspension chain/ring
    px[cx, cy - 4] = OUTLINE
    px[cx, cy - 3] = OUTLINE
    # Lantern roof cap
    for x in range(cx - 2, cx + 3):
        px[x, cy - 2] = OUTLINE
        px[x, cy - 1] = LANTERN_IRON
    # Glass chamber & glow core
    for y in range(cy, cy + 3):
        px[cx - 2, y] = OUTLINE
        px[cx + 2, y] = OUTLINE
        for x in range(cx - 1, cx + 2):
            px[x, y] = FIRE_MID
    # Intense flame core
    px[cx, cy + 1] = FIRE_CORE
    px[cx, cy] = FIRE_CORE if frame_idx in (1, 2) else FIRE_MID
    px[cx - 1, cy + 1] = FIRE_AMBER
    px[cx + 1, cy + 1] = FIRE_AMBER
    # Lantern iron base
    for x in range(cx - 2, cx + 3):
        px[x, cy + 3] = LANTERN_IRON
        px[x, cy + 4] = OUTLINE

    # --- 5. MERCHANT BODY, ROBES & HOOD ---
    # Head & Hood (Y=13..27 + dy, X=16..33)
    # Outer outline of hood
    for y in range(13 + dy, 28 + dy):
        for x in range(16, 34):
            # Shape the pointed hood
            dist_top = (y - (13 + dy))
            width_allowed = min(16, dist_top * 2 + 4)
            if abs(x - 24) > width_allowed // 2:
                continue
            px[x, y] = OUTLINE

    # Hood fill
    for y in range(14 + dy, 27 + dy):
        for x in range(17, 33):
            dist_top = (y - (13 + dy))
            width_allowed = min(14, dist_top * 2 + 2)
            if abs(x - 24) > width_allowed // 2:
                continue
            # Cel-shading
            if x >= 27:
                px[x, y] = HOOD_LIGHT
            elif x >= 21:
                px[x, y] = HOOD_MID
            else:
                px[x, y] = HOOD_DARK

    # Gold embroidered trim along hood opening
    for y in range(17 + dy, 26 + dy):
        px[19, y] = GOLD_TRIM
        px[29, y] = GOLD_LIGHT if x > 28 else GOLD_TRIM

    # Deep Shadow cavern under hood (Y=18..24 + dy, X=20..28)
    for y in range(18 + dy, 25 + dy):
        for x in range(20, 29):
            px[x, y] = HOOD_INNER

    # GLOWING CYAN MAGIC EYES (#00f0ff)
    # Left eye: X=21..22, Y=20..21 + dy
    # Right eye: X=26..27, Y=20..21 + dy
    eye_c = EYE_WHITE if frame_idx == 2 else EYE_CYAN
    px[21, 20 + dy] = eye_c
    px[22, 20 + dy] = EYE_CYAN
    px[21, 21 + dy] = EYE_DEEP
    px[22, 21 + dy] = eye_c

    px[26, 20 + dy] = EYE_CYAN
    px[27, 20 + dy] = eye_c
    px[26, 21 + dy] = eye_c
    px[27, 21 + dy] = EYE_DEEP

    # Main Robe Torso & Flowing Cloak (Y=27 + dy to Y=43)
    # Outline of robes
    for y in range(27 + dy, 44):
        w_robe = 16 + (y - 27) // 2
        for x in range(24 - w_robe // 2 - 1, 24 + w_robe // 2 + 2):
            if x < 13 or x > 36:
                continue
            px[x, y] = OUTLINE

    for y in range(27 + dy, 43):
        w_robe = 16 + (y - 27) // 2
        for x in range(24 - w_robe // 2, 24 + w_robe // 2 + 1):
            if x < 14 or x > 35:
                continue
            # Cel-shading on robe
            if x >= 28:
                px[x, y] = HOOD_LIGHT
            elif x >= 21:
                px[x, y] = HOOD_MID
            else:
                px[x, y] = HOOD_DARK

    # Leather Belt & Ornate Brass Buckle (Y=29..31 + dy, X=18..31)
    for y in range(29 + dy, 32 + dy):
        for x in range(18, 31):
            px[x, y] = PACK_STRAP
    # Buckle
    for y in range(29 + dy, 32 + dy):
        for x in range(23, 26):
            px[x, y] = BELT_BRASS
    px[24, 30 + dy] = GOLD_LIGHT

    # Gold hem embroidery at bottom of cloak (Y=41..42)
    for x in range(15, 34):
        if px[x, 41] != (0, 0, 0, 0) and px[x, 41] != OUTLINE:
            px[x, 41] = GOLD_TRIM
        if px[x, 42] != (0, 0, 0, 0) and px[x, 42] != OUTLINE:
            px[x, 42] = GOLD_TRIM

    # Sturdy Traveler Boots (Y=43..45, X=19..23 and X=26..30)
    for y in range(43, 46):
        for x in range(18, 23):
            px[x, y] = OUTLINE
        for x in range(26, 31):
            px[x, y] = OUTLINE
    for y in range(43, 45):
        for x in range(19, 22):
            px[x, y] = BOOT_MID
        for x in range(27, 30):
            px[x, y] = BOOT_MID

    # Left hand gripping the staff near lantern (Y=22..25, X=13..16)
    for y in range(22, 26):
        for x in range(13, 17):
            px[x, y] = OUTLINE
    for y in range(23, 25):
        for x in range(14, 16):
            px[x, y] = HOOD_MID

    # Clean up orphan pixels if any
    return canvas

def assemble_merchant():
    sheet = Image.new('RGBA', (192, 48), (0, 0, 0, 0))
    for f in range(4):
        frame = create_merchant_frame(f)
        sheet.paste(frame, (f * 48, 0))
    return sheet

if __name__ == '__main__':
    sheet = assemble_merchant()
    sheet.save('merchant_test.png')
    print('Generated merchant_test.png (192x48)')
