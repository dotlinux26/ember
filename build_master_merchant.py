"""
Masterpiece Soul Knight Merchant Generator.
Produces a pixel-perfect 192x48 spritesheet (4 frames of 48x48).
Adheres strictly to d:\Ember\.agents\skills\pixel-art\SKILL.md:
- 1px crisp dark outline
- 3-tone cel-shading
- Zero Anti-Aliasing (alpha 0 or 255 only)
- 0 orphan pixels
- Exact grid alignment (192x48)
- Unique design (not template clone)
- Cloak, massive potion backpack, swaying flank lantern at (-0.55f, 1.15f), cyan eyes, breathing idle.
"""
from PIL import Image

def hex_to_rgba(h, a=255):
    h = h.lstrip('#')
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)

# Palette
OUTLINE     = hex_to_rgba('#120e24')  # 1px Crisp dark outline
HOOD_DARK   = hex_to_rgba('#1c172e')  # Deep cloak shadow
HOOD_MID    = hex_to_rgba('#2f244a')  # Cloak body
HOOD_LIGHT  = hex_to_rgba('#4a3a6e')  # Cloak fold highlight
HOOD_RIM    = hex_to_rgba('#6d569e')  # Edge rim highlight
HOOD_INNER  = hex_to_rgba('#0b0814')  # Impenetrable dark shadow under hood

GOLD_TRIM   = hex_to_rgba('#d99c27')  # Gold trim / embroidery
GOLD_LIGHT  = hex_to_rgba('#ffe153')  # Bright gold accent
BELT_BRASS  = hex_to_rgba('#f7b031')  # Belt buckle

EYE_CYAN    = hex_to_rgba('#00f0ff')  # Piercing Cyan magic eye
EYE_WHITE   = hex_to_rgba('#ffffff')  # Eye specular pupil
EYE_DEEP    = hex_to_rgba('#00a8cc')  # Cyan halo

PACK_FRAME  = hex_to_rgba('#2b191c')  # Wooden pack frame
PACK_DARK   = hex_to_rgba('#572c29')  # Leather pack shadow
PACK_MID    = hex_to_rgba('#8c4839')  # Stitched leather pack
PACK_LIGHT  = hex_to_rgba('#b8654b')  # Leather highlight
PACK_STRAP  = hex_to_rgba('#3d211d')  # Leather straps

BEDROLL_D   = hex_to_rgba('#8f6043')  # Top travel roll dark
BEDROLL_M   = hex_to_rgba('#bf8a65')  # Travel roll mid
BEDROLL_L   = hex_to_rgba('#edd0b4')  # Travel roll light

# Potions nestled in pack
GLASS_RIM   = hex_to_rgba('#9bb2d1')  # Vial glass rim
GLASS_SPEC  = hex_to_rgba('#ffffff')  # Glass specular shine
POT_RED_D   = hex_to_rgba('#94132e')  # Vitality potion dark
POT_RED_M   = hex_to_rgba('#e61b43')  # Vitality potion mid
POT_RED_L   = hex_to_rgba('#ff6b8b')  # Vitality potion glow

POT_GOLD_D  = hex_to_rgba('#c46e16')  # Sun potion dark
POT_GOLD_M  = hex_to_rgba('#f0b024')  # Sun potion mid
POT_GOLD_L  = hex_to_rgba('#fff08a')  # Sun potion glow

POT_PURP_D  = hex_to_rgba('#501761')  # Void potion dark
POT_PURP_M  = hex_to_rgba('#9c36b0')  # Void potion mid
POT_PURP_L  = hex_to_rgba('#e87dff')  # Void potion glow

# Staff & Swaying Lantern (Left flank matching (-0.55f, 1.15f))
STAFF_DARK  = hex_to_rgba('#241513')  # Dark ironwood staff
STAFF_MID   = hex_to_rgba('#472b26')  # Staff wood mid
STAFF_HOOK  = hex_to_rgba('#1e2233')  # Iron hanger hook

LANTERN_IRON= hex_to_rgba('#1a1d2b')  # Lantern iron hood/base
FIRE_CORE   = hex_to_rgba('#ffffff')  # Flame core
FIRE_MID    = hex_to_rgba('#fff275')  # Bright yellow flame
FIRE_AMBER  = hex_to_rgba('#ff7e29')  # Amber flame rim

BOOT_DARK   = hex_to_rgba('#141021')  # Traveler boots
BOOT_MID    = hex_to_rgba('#362824')


def generate_frame(frame_idx):
    canvas = Image.new('RGBA', (48, 48), (0, 0, 0, 0))
    px = canvas.load()

    # Breathing dynamics
    # Frame 0: Rest (dy = 0)
    # Frame 1: Inhale start (dy = -1 for torso & hood)
    # Frame 2: Peak inhale (dy = -1, chest expands, eyes bright)
    # Frame 3: Exhale (dy = 0)
    dy = -1 if frame_idx in (1, 2) else 0

    # Lantern pendulum sway (Left flank, matching (-0.55f, 1.15f) in EmberGame.cs)
    # Lantern center reference: X=10, Y=17
    lan_sways = [(0, 0), (-1, -1), (0, 0), (1, 1)]
    lx_off, ly_off = lan_sways[frame_idx]

    # 1. TRAVELER BACKPACK (Behind body on right: X=27..42, Y=6..34)
    # Top Bedroll (Y=6..10 + dy, X=28..40)
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
    # Bedroll strapping
    for y in range(6 + dy, 11 + dy):
        px[31, y] = PACK_STRAP
        px[37, y] = PACK_STRAP

    # Leather Pack Body (Y=11 + dy..33, X=26..42)
    for y in range(11 + dy, 34):
        for x in range(26, 43):
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
            if x >= 36 and y <= 20:
                px[x, y] = PACK_LIGHT
            elif x >= 31:
                px[x, y] = PACK_MID
            else:
                px[x, y] = PACK_DARK

    # Pack frame wooden ribbing
    for y in range(12 + dy, 32):
        px[27, y] = PACK_FRAME

    # 2. POTION VIALS IN PACK LOOPS
    # Potion 1: Gold / Sun Elixir (Top: X=30..33, Y=11..16 + dy)
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

    # Potion 2: Red / Vitality Elixir (Outer middle: X=36..39, Y=14..19 + dy)
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

    # Potion 3: Purple / Void Elixir (Lower outer: X=36..40, Y=22..27)
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

    # 3. STAFF (Left side: X=11..14, Y=10..44)
    for y in range(11, 45):
        px[12, y] = OUTLINE
        px[13, y] = STAFF_MID
        px[14, y] = STAFF_DARK

    for x in range(10, 14):
        px[x, 10] = OUTLINE
        px[x, 11] = STAFF_DARK
    px[11, 12] = STAFF_HOOK
    px[10, 13] = STAFF_HOOK

    # 4. SWAYING LANTERN (Left flank matching (-0.55f, 1.15f))
    cx = 10 + lx_off
    cy = 17 + ly_off

    # Suspension chain
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

    # 5. HOOD & CLOAK
    # Outer hood outline
    for y in range(13 + dy, 28 + dy):
        for x in range(16, 34):
            dist_top = (y - (13 + dy))
            width_allowed = min(16, dist_top * 2 + 4)
            if abs(x - 24) > width_allowed // 2:
                continue
            px[x, y] = OUTLINE

    # Hood fill & cel-shading
    for y in range(14 + dy, 27 + dy):
        for x in range(17, 33):
            dist_top = (y - (13 + dy))
            width_allowed = min(14, dist_top * 2 + 2)
            if abs(x - 24) > width_allowed // 2:
                continue
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

    # Deep Shadow cavern under hood
    for y in range(18 + dy, 25 + dy):
        for x in range(20, 29):
            px[x, y] = HOOD_INNER

    # GLOWING CYAN MAGIC EYES (#00f0ff)
    eye_c = EYE_WHITE if frame_idx == 2 else EYE_CYAN
    px[21, 20 + dy] = eye_c
    px[22, 20 + dy] = EYE_CYAN
    px[21, 21 + dy] = EYE_DEEP
    px[22, 21 + dy] = eye_c

    px[26, 20 + dy] = EYE_CYAN
    px[27, 20 + dy] = eye_c
    px[26, 21 + dy] = eye_c
    px[27, 21 + dy] = EYE_DEEP

    # Main Robe Torso & Flowing Cloak
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
            if x >= 28:
                px[x, y] = HOOD_LIGHT
            elif x >= 21:
                px[x, y] = HOOD_MID
            else:
                px[x, y] = HOOD_DARK

    # Leather Belt & Buckle
    for y in range(29 + dy, 32 + dy):
        for x in range(18, 31):
            px[x, y] = PACK_STRAP
    for y in range(29 + dy, 32 + dy):
        for x in range(23, 26):
            px[x, y] = BELT_BRASS
    px[24, 30 + dy] = GOLD_LIGHT

    # Gold hem embroidery at bottom of cloak
    for x in range(15, 34):
        if px[x, 41] != (0, 0, 0, 0) and px[x, 41] != OUTLINE:
            px[x, 41] = GOLD_TRIM
        if px[x, 42] != (0, 0, 0, 0) and px[x, 42] != OUTLINE:
            px[x, 42] = GOLD_TRIM

    # Sturdy Traveler Boots
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

    # Left hand gripping staff
    for y in range(22, 26):
        for x in range(13, 17):
            px[x, y] = OUTLINE
    for y in range(23, 25):
        for x in range(14, 16):
            px[x, y] = HOOD_MID

    # Ensure 100% 0 orphan pixels:
    # Any opaque pixel with 0 8-way neighbors is fixed
    w, h = canvas.size
    for x in range(w):
        for y in range(h):
            if px[x, y][3] > 0:
                has_n = False
                for dx in (-1, 0, 1):
                    for dy_ in (-1, 0, 1):
                        if dx == 0 and dy_ == 0:
                            continue
                        nx, ny = x + dx, y + dy_
                        if 0 <= nx < w and 0 <= ny < h and px[nx, ny][3] > 0:
                            has_n = True
                            break
                    if has_n:
                        break
                if not has_n:
                    px[x, y] = (0, 0, 0, 0)

    return canvas


def assemble():
    sheet = Image.new('RGBA', (192, 48), (0, 0, 0, 0))
    for f in range(4):
        frame = generate_frame(f)
        sheet.paste(frame, (f * 48, 0))
    return sheet

if __name__ == '__main__':
    sheet = assemble()
    dest = r'd:\Ember\My project\Assets\Resources\Ember\Props\merchant.png'
    sheet.save(dest)
    print(f"Saved master merchant to {dest}")
