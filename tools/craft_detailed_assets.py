import os
import math
from PIL import Image, ImageDraw, ImageFont

def make_rgba(w, h):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))

# ============================================================================
# 1. UPGRADED BEACON MONOLITH (48x64) - Grand Eldritch Celestial Obelisk
# ============================================================================
def draw_grand_beacon():
    img = make_rgba(48, 64)
    pix = img.load()

    c_out = (8, 6, 14, 255)
    c_stone_dark = (18, 15, 26, 255)
    c_stone_mid = (32, 28, 48, 255)
    c_stone_hi = (54, 48, 76, 255)
    c_stone_rim = (82, 74, 112, 255)
    
    c_bronze_dark = (85, 48, 22, 255)
    c_bronze_mid = (145, 92, 38, 255)
    c_bronze_hi = (215, 152, 62, 255)
    c_gold = (255, 215, 105, 255)

    c_cyan_deep = (6, 45, 62, 255)
    c_cyan_mid = (18, 128, 160, 255)
    c_cyan_bright = (45, 225, 250, 255)
    c_cyan_glow = (175, 250, 255, 255)
    c_white = (255, 255, 255, 255)

    # Base steps & octagonal pedestal (y=50..63)
    # Tier 1 (lowest base): y=59..63, x=4..43
    for y in range(59, 64):
        for x in range(4, 44):
            if x == 4 or x == 43 or y == 59 or y == 63:
                pix[x, y] = c_out
            elif y == 60:
                pix[x, y] = c_stone_rim if x < 24 else c_stone_hi
            elif x < 24:
                pix[x, y] = c_stone_hi if (x + y) % 4 == 0 else c_stone_mid
            else:
                pix[x, y] = c_stone_mid if (x + y) % 4 == 0 else c_stone_dark

    # Tier 2 (middle base): y=54..58, x=8..39
    for y in range(54, 59):
        for x in range(8, 40):
            if x == 8 or x == 39 or y == 54:
                pix[x, y] = c_out
            elif y == 55:
                pix[x, y] = c_bronze_hi if (x % 3 == 0) else c_stone_rim
            elif x < 24:
                pix[x, y] = c_stone_hi if x < 16 else c_stone_mid
            else:
                pix[x, y] = c_stone_mid if x < 32 else c_stone_dark

    # Flanking stone buttress wings (y=42..54)
    # Left wing: x=6..13, Right wing: x=34..41
    for y in range(42, 54):
        h = 54 - y
        w_left = int(h * 0.6)
        for x in range(14 - w_left, 14):
            if x == 14 - w_left or y == 42:
                pix[x, y] = c_out
            else:
                pix[x, y] = c_stone_hi if x == 15 - w_left else c_stone_mid
        for x in range(34, 34 + w_left):
            if x == 33 + w_left or y == 42:
                pix[x, y] = c_out
            else:
                pix[x, y] = c_stone_mid if x == 34 else c_stone_dark

    # Obelisk Pillar Shaft (y=22..53, x=14..33, tapered to x=16..31 at top)
    for y in range(22, 54):
        progress = (y - 22) / 32.0 # 0 at top, 1 at bottom
        half_w = 7.5 + progress * 2.2
        min_x = int(23.5 - half_w)
        max_x = int(23.5 + half_w)
        for x in range(min_x, max_x + 1):
            if x == min_x or x == max_x or y == 53:
                pix[x, y] = c_out
            elif x == min_x + 1:
                pix[x, y] = c_stone_rim
            elif x < 23:
                # Vertical gothic flute grooving
                pix[x, y] = c_stone_hi if (x % 3 == 0) else c_stone_mid
            elif x < 30:
                pix[x, y] = c_stone_mid if (x % 3 == 0) else c_stone_dark
            else:
                pix[x, y] = c_stone_dark

    # Spiraling Runic Energy Ribbon carved along the obelisk shaft (y=24..50)
    for y in range(24, 51):
        # Sine wave spiral across pillar
        wave = math.sin((y - 24) * 0.45) * 4.5
        rx = int(23.5 + wave)
        if 16 <= rx <= 31:
            pix[rx, y] = c_cyan_glow
            if rx > 0: pix[rx - 1, y] = c_cyan_bright
            if rx < 47: pix[rx + 1, y] = c_cyan_mid
            if y > 0: pix[rx, y - 1] = c_cyan_deep

    # Ornate bronze astragal collar (y=20..22, x=13..34)
    for y in range(20, 23):
        for x in range(13, 35):
            if x == 13 or x == 34 or y == 20 or y == 22:
                pix[x, y] = c_out
            else:
                pix[x, y] = c_gold if (x % 2 == 0) else c_bronze_hi

    # Floating Celestial Armillary Lantern Cage (y=8..20)
    # Arched bronze cage ribs:
    cage_x_points = [13, 17, 23, 24, 30, 34]
    for y in range(8, 20):
        for cx in [13, 34]:
            pix[cx, y] = c_out
            pix[cx + (1 if cx == 13 else -1), y] = c_bronze_hi
        # Arched curves
        if y in (11, 15, 19):
            for x in range(14, 34):
                if (x % 3 == 0): pix[x, y] = c_bronze_mid

    # The Eternal Cyan Ember Flame (y=5..18, center cx=23.5, cy=13)
    cx, cy = 23.5, 13.0
    for y in range(5, 19):
        for x in range(16, 32):
            d = math.sqrt((x - cx) ** 2 + ((y - cy) * 1.15) ** 2)
            if d <= 5.8:
                if d <= 1.8:
                    pix[x, y] = c_white
                elif d <= 3.2:
                    pix[x, y] = c_cyan_glow
                elif d <= 4.6:
                    pix[x, y] = c_cyan_bright
                else:
                    pix[x, y] = c_cyan_mid

    # Flame tongues reaching up
    pix[23, 4] = c_cyan_glow; pix[24, 4] = c_white
    pix[22, 5] = c_cyan_bright; pix[25, 5] = c_cyan_glow
    pix[21, 6] = c_cyan_mid; pix[26, 6] = c_cyan_bright

    # Levitating crystalline focus shards around the flame
    pix[16, 10] = c_cyan_glow; pix[17, 10] = c_white
    pix[30, 10] = c_white; pix[31, 10] = c_cyan_glow
    pix[20, 7] = c_cyan_bright; pix[27, 7] = c_cyan_bright

    # Floating Embers ascending
    for ex, ey in [(23, 2), (25, 1), (21, 3), (26, 3)]:
        pix[ex, ey] = c_cyan_bright

    # Spire needle crown finial at top (y=0..5)
    pix[23, 1] = c_gold; pix[24, 1] = c_gold
    pix[23, 2] = c_bronze_hi; pix[24, 2] = c_gold
    pix[22, 3] = c_out; pix[23, 3] = c_bronze_mid; pix[24, 3] = c_bronze_hi; pix[25, 3] = c_out

    return img

# ============================================================================
# 2. TREE 1: ANCIENT ELDER OAK (tree_oak.png) - 48x64
# ============================================================================
def draw_elder_oak():
    img = make_rgba(48, 64)
    pix = img.load()

    c_out = (8, 6, 12, 255)
    c_bark_dark = (18, 14, 22, 255)
    c_bark_mid = (34, 26, 40, 255)
    c_bark_hi = (56, 44, 66, 255)
    c_moss_dark = (16, 28, 22, 255)
    c_moss_mid = (26, 48, 36, 255)
    c_moss_hi = (42, 78, 58, 255)
    
    c_leaf_dark = (14, 24, 20, 255)
    c_leaf_mid = (24, 44, 36, 255)
    c_leaf_hi = (38, 70, 56, 255)
    c_leaf_rim = (58, 104, 82, 255)
    c_leaf_spore = (120, 220, 185, 255)

    # Gnarled sprawling roots at base (y=44..63)
    # Trunk x=18..30, roots flare to x=8..40
    for y in range(42, 64):
        progress = (y - 42) / 21.0
        half_w = 4.5 + progress * 7.5
        min_x = int(24 - half_w)
        max_x = int(24 + half_w)
        for x in range(min_x, max_x + 1):
            if x == min_x or x == max_x or y == 63:
                pix[x, y] = c_out
            elif x < 23:
                pix[x, y] = c_moss_hi if (x % 3 == 0) else c_bark_hi
            elif x < 28:
                pix[x, y] = c_bark_mid
            else:
                pix[x, y] = c_bark_dark

    # Hollow in the ancient trunk (y=48..53, x=22..25)
    for y in range(48, 54):
        for x in range(22, 26):
            pix[x, y] = (8, 6, 12, 255)

    # Massive 3-Tier Layered Oak Canopy (y=4..44)
    # Tier 1 (top crown): cy=12, r=11
    # Tier 2 (mid canopy): cy=22, r=16
    # Tier 3 (lower canopy): cy=32, r=21
    canopy_centers = [(24, 12, 11), (24, 22, 16), (24, 32, 21), (14, 26, 10), (34, 26, 10)]
    
    for y in range(4, 44):
        for x in range(2, 46):
            # Check if inside any canopy lobe
            inside = False
            min_dist_norm = 999.0
            for cx, cy, cr in canopy_centers:
                d = math.sqrt((x - cx) ** 2 + ((y - cy) * 1.1) ** 2)
                if d <= cr:
                    inside = True
                    min_dist_norm = min(min_dist_norm, d / cr)

            if inside:
                if min_dist_norm > 0.92:
                    pix[x, y] = c_out
                else:
                    # Multi-tone foliage shading
                    if x < 21:
                        if min_dist_norm < 0.4:
                            pix[x, y] = c_leaf_rim
                        elif min_dist_norm < 0.7:
                            pix[x, y] = c_leaf_hi
                        else:
                            pix[x, y] = c_leaf_mid
                    else:
                        if min_dist_norm < 0.5:
                            pix[x, y] = c_leaf_mid
                        else:
                            pix[x, y] = c_leaf_dark

    # Dangling moss strands and glowing night spores
    moss_drops = [(10, 36, 5), (16, 40, 4), (22, 42, 6), (28, 41, 5), (34, 38, 6), (39, 34, 4)]
    for mx, my, length in moss_drops:
        for ly in range(length):
            if my + ly < 64:
                pix[mx, my + ly] = c_leaf_rim if ly % 2 == 0 else c_leaf_hi
        # Tip spore
        if my + length < 64:
            pix[mx, my + length] = c_leaf_spore

    return img

# ============================================================================
# 3. TREE 2: GNARLED SWAMP WILLOW (tree_willow.png) - 48x64
# ============================================================================
def draw_swamp_willow():
    img = make_rgba(48, 64)
    pix = img.load()

    c_out = (6, 10, 10, 255)
    c_bark_dark = (14, 22, 20, 255)
    c_bark_mid = (24, 38, 34, 255)
    c_bark_hi = (38, 58, 52, 255)
    c_tendril_dark = (18, 32, 28, 255)
    c_tendril_mid = (28, 54, 46, 255)
    c_tendril_hi = (44, 88, 74, 255)
    c_spore_core = (68, 230, 175, 255)
    c_spore_white = (200, 255, 235, 255)

    # Crooked, leaning serpentine trunk: y=26..63
    for y in range(26, 64):
        # Trunk curves towards the right then back
        t = (y - 26) / 37.0
        bend = math.sin(t * math.pi) * 4.0
        cx = 24 + bend
        half_w = 3.2 + t * 4.8
        min_x = int(cx - half_w)
        max_x = int(cx + half_w)
        for x in range(min_x, max_x + 1):
            if x == min_x or x == max_x or y == 63:
                pix[x, y] = c_out
            elif x < cx:
                pix[x, y] = c_bark_hi if x == min_x + 1 else c_bark_mid
            else:
                pix[x, y] = c_bark_mid if x < cx + 2 else c_bark_dark

    # Knee-roots arching out of swamp mud
    knee_roots = [(12, 58), (14, 55), (34, 56), (37, 59)]
    for rx, ry in knee_roots:
        pix[rx, ry] = c_bark_hi
        pix[rx + 1, ry] = c_bark_mid
        pix[rx - 1, ry] = c_out; pix[rx + 2, ry] = c_out

    # Twisted, gnarled primary branches reaching out (y=12..28)
    branches = [
        [(22, 26), (18, 22), (13, 18), (8, 14)],
        [(25, 24), (30, 20), (36, 17), (41, 15)],
        [(23, 20), (20, 15), (18, 10)],
        [(25, 19), (28, 14), (31, 9)]
    ]
    for b in branches:
        for bx, by in b:
            pix[bx, by] = c_bark_hi
            pix[bx + 1, by] = c_bark_mid
            pix[bx - 1, by] = c_out

    # Weeping Spanish Moss tendrils dripping down like ghost veils (y=14..52)
    streamers = [
        (8, 15, 24), (11, 17, 30), (14, 19, 26), (17, 21, 28),
        (20, 16, 22), (28, 15, 25), (31, 18, 29), (35, 18, 27),
        (38, 16, 28), (41, 16, 22)
    ]
    for sx, sy, length in streamers:
        for ly in range(length):
            cur_y = sy + ly
            if cur_y < 63:
                # Wave sway
                sway = int(math.sin(ly * 0.45) * 1.2)
                cur_x = sx + sway
                if 0 <= cur_x < 48:
                    pix[cur_x, cur_y] = c_tendril_hi if ly % 3 == 0 else c_tendril_mid

    # Bioluminescent Swamp Pustules / Spore clusters
    pustules = [(18, 38), (19, 39), (28, 42), (29, 43), (23, 30), (22, 48)]
    for px, py in pustules:
        if 0 <= px < 48 and 0 <= py < 64:
            pix[px, py] = c_spore_white
            if px > 0: pix[px - 1, py] = c_spore_core
            if px < 47: pix[px + 1, py] = c_spore_core

    return img

# ============================================================================
# 4. TREE 3: ABYSSAL SHADOW PINE (tree_pine.png) - 48x64
# ============================================================================
def draw_shadow_pine():
    img = make_rgba(48, 64)
    pix = img.load()

    c_out = (8, 6, 14, 255)
    c_bark_dark = (16, 12, 22, 255)
    c_bark_mid = (30, 22, 38, 255)
    c_bark_hi = (48, 36, 60, 255)
    
    c_needle_dark = (12, 18, 24, 255)
    c_needle_mid = (20, 32, 44, 255)
    c_needle_hi = (34, 54, 72, 255)
    c_needle_frost = (68, 102, 132, 255)
    c_needle_tip = (145, 195, 235, 255)

    # Straight tall rugged pine trunk (y=36..63, x=22..26)
    for y in range(36, 64):
        progress = (y - 36) / 27.0
        half_w = 2.0 + progress * 2.8
        min_x = int(24 - half_w)
        max_x = int(24 + half_w)
        for x in range(min_x, max_x + 1):
            if x == min_x or x == max_x or y == 63:
                pix[x, y] = c_out
            elif x < 24:
                pix[x, y] = c_bark_hi if x == min_x + 1 else c_bark_mid
            else:
                pix[x, y] = c_bark_dark

    # 5 Grand Pagoda-style Needle Tiers (y=4..44)
    tiers = [
        {"tip_y": 4, "base_y": 12, "w": 8},
        {"tip_y": 10, "base_y": 20, "w": 12},
        {"tip_y": 17, "base_y": 28, "w": 16},
        {"tip_y": 25, "base_y": 37, "w": 19},
        {"tip_y": 34, "base_y": 45, "w": 22}
    ]

    for tier in tiers:
        tip_y = tier["tip_y"]
        base_y = tier["base_y"]
        max_w = tier["w"]
        for y in range(tip_y, base_y + 1):
            progress = (y - tip_y) / float(base_y - tip_y)
            # Sweeping triangular tier with upward swept ends
            w = progress * max_w
            if y == base_y: w *= 1.1 # flare at corner
            min_x = int(24 - w)
            max_x = int(24 + w)
            for x in range(min_x, max_x + 1):
                if x < 0 or x >= 48: continue
                if x == min_x or x == max_x or y == tip_y:
                    pix[x, y] = c_out
                else:
                    if x < 23:
                        if y == tip_y + 1 or x == min_x + 1:
                            pix[x, y] = c_needle_frost
                        elif progress < 0.6:
                            pix[x, y] = c_needle_hi
                        else:
                            pix[x, y] = c_needle_mid
                    else:
                        if progress < 0.5:
                            pix[x, y] = c_needle_mid
                        else:
                            pix[x, y] = c_needle_dark

            # Upward pointed frosted needle tips at tier ends
            if y == base_y:
                if 0 <= min_x < 48: pix[min_x, y - 1] = c_needle_tip
                if 0 <= max_x < 48: pix[max_x, y - 1] = c_needle_tip

    return img

# ============================================================================
# 5. TREE 4: VOID CRYSTAL SPIRE (tree_crystal.png) - 48x64
# ============================================================================
def draw_void_crystal():
    img = make_rgba(48, 64)
    pix = img.load()

    c_out = (10, 6, 18, 255)
    c_obsidian_dark = (16, 10, 26, 255)
    c_obsidian_mid = (28, 18, 44, 255)
    c_obsidian_hi = (46, 32, 68, 255)
    c_obsidian_rim = (76, 54, 110, 255)

    c_crystal_dark = (65, 20, 110, 255)
    c_crystal_mid = (125, 45, 195, 255)
    c_crystal_bright = (195, 90, 255, 255)
    c_crystal_core = (245, 190, 255, 255)
    c_starlight = (255, 240, 255, 255)

    # Angular crystalline jagged tree trunk (y=30..63)
    for y in range(30, 64):
        progress = (y - 30) / 33.0
        half_w = 3.5 + progress * 5.5
        min_x = int(24 - half_w)
        max_x = int(24 + half_w)
        for x in range(min_x, max_x + 1):
            if x == min_x or x == max_x or y == 63:
                pix[x, y] = c_out
            elif x == min_x + 1:
                pix[x, y] = c_obsidian_rim
            elif x < 24:
                # Crystalline facet facets
                pix[x, y] = c_obsidian_hi if (x + y) % 5 == 0 else c_obsidian_mid
            else:
                pix[x, y] = c_obsidian_dark

    # Spire crystal branches shooting outwards (y=10..34)
    spires = [
        [(22, 30), (18, 25), (14, 20), (10, 15), (7, 10)],
        [(26, 28), (30, 23), (35, 19), (40, 14), (43, 9)],
        [(23, 24), (21, 18), (19, 12), (18, 6)],
        [(25, 22), (27, 16), (29, 11), (30, 5)]
    ]
    for sp in spires:
        for sx, sy in sp:
            pix[sx, sy] = c_crystal_core
            if sx > 0: pix[sx - 1, sy] = c_crystal_bright
            if sx < 47: pix[sx + 1, sy] = c_crystal_mid
            if sy > 0: pix[sx, sy - 1] = c_out

    # Huge Central Void Crystal Core at the crown (y=12..24, x=20..28)
    for y in range(12, 25):
        h_w = 4.2 - abs(y - 18) * 0.6
        min_x = int(24 - h_w)
        max_x = int(24 + h_w)
        for x in range(min_x, max_x + 1):
            if x == min_x or x == max_x or y == 12 or y == 24:
                pix[x, y] = c_out
            elif x == 24 or y == 18:
                pix[x, y] = c_starlight
            elif abs(x - 24) <= 1:
                pix[x, y] = c_crystal_core
            else:
                pix[x, y] = c_crystal_bright

    # Levitating Void Crystal Shards hovering in the air
    shards = [(8, 8), (38, 7), (14, 4), (32, 3), (5, 22), (42, 24)]
    for kx, ky in shards:
        pix[kx, ky] = c_starlight
        pix[kx + 1, ky] = c_crystal_core
        pix[kx, ky + 1] = c_crystal_bright
        pix[kx + 1, ky + 1] = c_crystal_dark

    return img

# ============================================================================
# Main Exporter & Showcase Preview Generator
# ============================================================================
def build_all():
    os.makedirs("My project/Assets/Resources/Ember/Props", exist_ok=True)
    os.makedirs("docs", exist_ok=True)

    beacon_img = draw_grand_beacon()
    tree_oak = draw_elder_oak()
    tree_willow = draw_swamp_willow()
    tree_pine = draw_shadow_pine()
    tree_crystal = draw_void_crystal()

    # Save Props
    beacon_img.save("My project/Assets/Resources/Ember/Props/beacon.png")
    tree_oak.save("My project/Assets/Resources/Ember/Props/tree.png") # Default fallback
    tree_oak.save("My project/Assets/Resources/Ember/Props/tree_oak.png")
    tree_willow.save("My project/Assets/Resources/Ember/Props/tree_willow.png")
    tree_pine.save("My project/Assets/Resources/Ember/Props/tree_pine.png")
    tree_crystal.save("My project/Assets/Resources/Ember/Props/tree_crystal.png")

    print("Saved all detailed PNGs to My project/Assets/Resources/Ember/Props!")

    # -------------------------------------------------------------
    # High-Resolution Showcase Preview Image
    # -------------------------------------------------------------
    pw, ph = 920, 560
    preview = Image.new("RGBA", (pw, ph), (14, 11, 22, 255))
    draw = ImageDraw.Draw(preview)

    # Header
    draw.rectangle([(0, 0), (pw, 44)], fill=(24, 18, 36, 255))
    draw.line([(0, 44), (pw, 44)], fill=(80, 58, 120, 255), width=2)

    try:
        font_title = ImageFont.truetype("arial.ttf", 20)
        font_name = ImageFont.truetype("arialbd.ttf", 14)
        font_sub = ImageFont.truetype("arial.ttf", 11)
    except:
        font_title = font_name = font_sub = ImageFont.load_default()

    draw.text((20, 12), "EMBER · ĐẠI TRÙNG TU HẢI ĐĂNG CỔ THẠCH & ĐA DẠNG HÓA 4 LOẠI CÂY (PIX2 1.1)", fill=(255, 230, 180, 255), font=font_title)

    cards = [
        ("Trụ Hải Đăng (Grand Beacon)", beacon_img, "48×64px", "Bệ đá đa giác chạm khắc rune cổ, dải năng lượng cuộn xoắn, đài lửa vĩnh cửu lơ lửng."),
        ("Cổ Thụ Rừng (Elder Oak)", tree_oak, "48×64px", "Thân rễ gồ ghề bám đất, hốc cây tối, tán lá 3 tầng rậm rạp có rêu rủ & bào tử dạ quang."),
        ("Liễu Ma Đầm Lầy (Swamp Willow)", tree_willow, "48×64px", "Thân cây uốn éo khẳng khiu, dải rêu ma quái buông rủ dài như tấm màn, bọc nấm phát sáng."),
        ("Tùng Cổ Đêm (Shadow Pine)", tree_pine, "48×64px", "Cây tùng vươn cao hùng vĩ, 5 tầng tán lá chóp nhọn phong cách gothic phủ sương tuyết đêm."),
        ("Cây Hư Không (Void Crystal)", tree_crystal, "48×64px", "Cây gai đá hắc diện thạch, mọc cụm tinh thể tím rực sáng và mảnh pha lê lơ lửng.")
    ]

    card_w = 168
    card_h = 470
    start_x = 24
    spacing = 10

    for idx, (title, s_img, sz_text, desc) in enumerate(cards):
        cx = start_x + idx * (card_w + spacing)
        cy = 60

        # Background card
        draw.rounded_rectangle([(cx, cy), (cx + card_w, cy + card_h)], radius=8, fill=(22, 17, 32, 255), outline=(52, 42, 72, 255))

        # Title & Subtitle
        draw.text((cx + 10, cy + 10), title, fill=(245, 215, 160, 255), font=font_name)
        draw.text((cx + 10, cy + 28), f"Chuẩn PIX2 · {sz_text}", fill=(115, 175, 225, 255), font=font_sub)

        # Sprite Box
        box_w = 148
        box_h = 240
        bx = cx + 10
        by = cy + 48
        draw.rectangle([(bx, by), (bx + box_w, by + box_h)], fill=(10, 8, 16, 255), outline=(42, 34, 56, 255))

        # Scale 3.4x
        scale = 3.4
        sw = int(48 * scale)
        sh = int(64 * scale)
        s_scaled = s_img.resize((sw, sh), Image.NEAREST)

        sx = bx + (box_w - sw) // 2
        sy = by + (box_h - sh) // 2
        preview.paste(s_scaled, (sx, sy), s_scaled)

        # Description wrapped
        lines = []
        words = desc.split()
        cur_line = ""
        for w in words:
            if len(cur_line + " " + w) <= 24:
                cur_line += (" " if cur_line else "") + w
            else:
                lines.append(cur_line)
                cur_line = w
        if cur_line: lines.append(cur_line)

        for li, line in enumerate(lines[:8]):
            draw.text((cx + 10, cy + 300 + li * 18), line, fill=(170, 160, 190, 255), font=font_sub)

    preview_path = "docs/diverse_trees_beacon_preview.png"
    preview.save(preview_path)
    
    artifact_dir = "C:/Users/OS/.gemini/antigravity/brain/dc6023b7-ad69-4d5b-a5d0-20427d5efb77"
    if os.path.exists(artifact_dir):
        preview.save(os.path.join(artifact_dir, "diverse_trees_beacon_preview.png"))

    print("Showcase preview generated successfully at docs/diverse_trees_beacon_preview.png!")

if __name__ == "__main__":
    build_all()
