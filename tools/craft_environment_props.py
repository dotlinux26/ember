import math
import os
from PIL import Image, ImageDraw, ImageFont

def make_rgba(w, h):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))

# ============================================================================
# 1. APPLE TREE WITH RIPE GLOWING APPLES (48x56) - EXACT FROM REFERENCE 2
# ============================================================================
def draw_apple_tree():
    img = make_rgba(48, 56)
    pix = img.load()

    c_out = (12, 10, 8, 255)
    c_bark_dark = (42, 26, 16, 255)
    c_bark_mid = (76, 46, 28, 255)
    c_bark_hi = (112, 70, 40, 255)
    c_bark_rim = (148, 98, 60, 255)

    c_fol_out = (10, 36, 18, 255)
    c_fol_shadow = (18, 62, 30, 255)
    c_fol_mid = (34, 108, 46, 255)
    c_fol_hi = (58, 155, 62, 255)
    c_fol_top = (102, 205, 82, 255)
    c_fol_sun = (165, 245, 115, 255)

    c_moss = (54, 98, 48, 255)
    c_apple = (225, 35, 48, 255)
    c_apple_shadow = (145, 18, 28, 255)
    c_apple_shine = (255, 180, 180, 255)

    # Trunk & Roots: y=28..54
    for y in range(28, 55):
        t = (y - 28) / 26.0
        half_w = 3.6 + (t ** 2.2) * 5.6
        min_x = int(24 - half_w)
        max_x = int(24 + half_w)
        for x in range(min_x, max_x + 1):
            if x == min_x or x == max_x or y == 54:
                pix[x, y] = c_out
            elif x == min_x + 1:
                pix[x, y] = c_bark_rim
            elif x < 23:
                pix[x, y] = c_bark_hi if (x % 2 == 0) else c_bark_mid
            elif x < 27:
                pix[x, y] = c_bark_mid
            else:
                pix[x, y] = c_bark_dark

    for x, y in [(15, 53), (14, 54), (13, 54), (12, 54)]: pix[x, y] = c_bark_hi; pix[x, y + 1] = c_out
    for x, y in [(32, 53), (33, 54), (34, 54), (35, 54)]: pix[x, y] = c_bark_mid; pix[x, y + 1] = c_out
    for x, y in [(23, 53), (24, 54), (25, 54)]: pix[x, y] = c_moss

    # 5 Volumetric Spherical Foliage Lobes
    lobes = [
        {"cx": 24, "cy": 13, "r": 12.0},
        {"cx": 13, "cy": 20, "r": 11.0},
        {"cx": 34, "cy": 20, "r": 11.0},
        {"cx": 18, "cy": 28, "r": 10.5},
        {"cx": 29, "cy": 28, "r": 10.5},
    ]

    for y in range(2, 40):
        for x in range(2, 46):
            best_val = -1
            best_col = None
            for lobe in lobes:
                dx = x - lobe["cx"]
                dy = y - lobe["cy"]
                dist = math.sqrt(dx * dx + (dy * 1.05) ** 2)
                r = lobe["r"]
                if dist <= r:
                    norm = dist / r
                    light_d = math.sqrt((dx + 0.35 * r) ** 2 + (dy + 0.4 * r) ** 2) / (r * 1.5)
                    if norm > 0.88: cur_col = c_fol_out
                    elif light_d < 0.28: cur_col = c_fol_sun
                    elif light_d < 0.50: cur_col = c_fol_top
                    elif light_d < 0.72: cur_col = c_fol_hi
                    elif light_d < 0.95: cur_col = c_fol_mid
                    else: cur_col = c_fol_shadow

                    priority = (1.0 - norm) * 10.0 + (lobe["cy"])
                    if priority > best_val:
                        best_val = priority
                        best_col = cur_col
            if best_col is not None:
                pix[x, y] = best_col

    # Ripe red apples with specular glint
    apples = [(16, 17), (29, 15), (22, 23), (34, 25), (14, 27), (27, 31), (21, 33)]
    for ax, ay in apples:
        for ox in range(-1, 2):
            for oy in range(-1, 2):
                if ox * ox + oy * oy <= 1:
                    px, py = ax + ox, ay + oy
                    if 0 <= px < 48 and 0 <= py < 56:
                        if ox == -1 and oy == -1: pix[px, py] = c_apple_shine
                        elif ox == 1 or oy == 1: pix[px, py] = c_apple_shadow
                        else: pix[px, py] = c_apple
        # Dark bottom outline under apple
        if ay + 2 < 56 and pix[ax, ay + 2][3] != 0: pix[ax, ay + 2] = c_fol_out

    return img

# ============================================================================
# 2. TROPICAL PALM TREE (40x48) - FROM DESERT/BEACH BIOME IN REFERENCE 2
# ============================================================================
def draw_palm_tree():
    img = make_rgba(40, 48)
    pix = img.load()

    c_out = (18, 12, 10, 255)
    c_trunk_dark = (72, 44, 24, 255)
    c_trunk_mid = (120, 76, 42, 255)
    c_trunk_hi = (175, 118, 68, 255)
    c_trunk_ring = (52, 30, 16, 255)

    c_frond_out = (12, 42, 18, 255)
    c_frond_shadow = (22, 74, 32, 255)
    c_frond_mid = (45, 132, 54, 255)
    c_frond_hi = (82, 188, 76, 255)
    c_frond_sun = (148, 235, 115, 255)
    c_nut = (85, 52, 26, 255)

    # 1. Slender Curved Trunk (y=16..46, curves slightly from x=20 at base to x=19 at crown)
    for y in range(16, 47):
        t = (y - 16) / 30.0
        curve = math.sin(t * math.pi * 0.8) * 2.2
        cx = 19.5 + curve
        half_w = 2.0 + t * 1.5
        min_x = int(cx - half_w)
        max_x = int(cx + half_w)
        is_ring = (y % 3 == 0)
        for x in range(min_x, max_x + 1):
            if x == min_x or x == max_x or y == 46:
                pix[x, y] = c_out
            elif is_ring:
                pix[x, y] = c_trunk_ring
            elif x < cx:
                pix[x, y] = c_trunk_hi if x == min_x + 1 else c_trunk_mid
            else:
                pix[x, y] = c_trunk_dark

    # 2. Palm Fronds (Starburst arching leaves radiating from (19, 15))
    frond_arms = [
        {"angle": -165, "len": 15, "sag": 4},
        {"angle": -130, "len": 17, "sag": 6},
        {"angle": -85,  "len": 18, "sag": 3},
        {"angle": -45,  "len": 17, "sag": 5},
        {"angle": -15,  "len": 15, "sag": 4},
        {"angle": -105, "len": 14, "sag": 2},
    ]

    for fa in frond_arms:
        rad = math.radians(fa["angle"])
        for step in range(fa["len"]):
            st = step / float(fa["len"])
            fx = int(19.5 + step * math.cos(rad))
            fy = int(15.5 + step * math.sin(rad) + (st ** 1.8) * fa["sag"])
            leaf_w = int(math.sin(st * math.pi) * 2.2)
            for ow in range(-leaf_w, leaf_w + 1):
                px = fx + ow
                py = fy
                if 0 <= px < 40 and 0 <= py < 48:
                    if abs(ow) == leaf_w:
                        pix[px, py] = c_frond_out
                    elif ow < 0:
                        pix[px, py] = c_frond_sun if st < 0.5 else c_frond_hi
                    elif ow == 0:
                        pix[px, py] = c_frond_hi
                    else:
                        pix[px, py] = c_frond_mid if st < 0.5 else c_frond_shadow

    # Coconuts at crown
    for cx, cy in [(18, 16), (21, 16), (20, 18)]:
        pix[cx, cy] = c_nut
        if cx + 1 < 40: pix[cx + 1, cy] = c_out

    return img

# ============================================================================
# 3. FROST SNOW PINE (36x48) - FROM SNOW/ICE BIOME IN REFERENCE 2
# ============================================================================
def draw_snow_pine():
    img = make_rgba(36, 48)
    pix = img.load()

    c_out = (10, 16, 22, 255)
    c_bark = (42, 28, 22, 255)
    c_needle_dark = (18, 48, 52, 255)
    c_needle_mid = (32, 78, 80, 255)
    c_needle_hi = (54, 115, 110, 255)

    # Thick fluffy snow palette
    c_snow_shadow = (145, 175, 205, 255)
    c_snow_mid = (195, 220, 242, 255)
    c_snow_hi = (235, 246, 255, 255)
    c_snow_white = (255, 255, 255, 255)

    # Trunk base: y=38..46
    for y in range(38, 47):
        for x in range(16, 21):
            if x == 16 or x == 20 or y == 46: pix[x, y] = c_out
            else: pix[x, y] = c_bark

    # 4 Tiered Conical Pine Tiers with thick snow blankets
    tiers = [
        {"startY": 4,  "endY": 15, "maxW": 7.0,  "cx": 18.0},
        {"startY": 12, "endY": 24, "maxW": 10.5, "cx": 18.0},
        {"startY": 20, "endY": 33, "maxW": 13.5, "cx": 18.0},
        {"startY": 28, "endY": 40, "maxW": 16.0, "cx": 18.0},
    ]

    for tier in tiers:
        cx = tier["cx"]
        for y in range(tier["startY"], tier["endY"] + 1):
            t = (y - tier["startY"]) / float(tier["endY"] - tier["startY"])
            w = t * tier["maxW"] + 1.2
            min_x = int(cx - w)
            max_x = int(cx + w)
            # Top half of each tier is thick snow, bottom half has needles peeking out
            is_snow = (t <= 0.60)
            for x in range(min_x, max_x + 1):
                if x < 0 or x >= 36: continue
                if x == min_x or x == max_x or y == tier["endY"]:
                    pix[x, y] = c_out
                elif is_snow:
                    light = -(x - cx) * 0.7 - (y - tier["startY"]) * 0.5
                    if light > 2: pix[x, y] = c_snow_white
                    elif light > -1: pix[x, y] = c_snow_hi
                    elif light > -4: pix[x, y] = c_snow_mid
                    else: pix[x, y] = c_snow_shadow
                else:
                    if x < cx: pix[x, y] = c_needle_hi if (x % 2 == 0) else c_needle_mid
                    else: pix[x, y] = c_needle_dark

    return img

# ============================================================================
# 4. FALLEN MOSSY LOG (36x18) - FROM REFERENCE 2 WITH EMBER MUSHROOMS
# ============================================================================
def draw_fallen_log():
    img = make_rgba(36, 18)
    pix = img.load()

    c_out = (12, 10, 8, 255)
    c_bark_dark = (38, 24, 16, 255)
    c_bark_mid = (68, 42, 26, 255)
    c_bark_hi = (102, 65, 38, 255)
    c_bark_rim = (135, 90, 52, 255)

    c_cut_dark = (120, 82, 48, 255)
    c_cut_mid = (165, 118, 72, 255)
    c_cut_hi = (205, 155, 100, 255)
    c_cut_ring = (95, 62, 34, 255)

    c_moss_dark = (32, 68, 36, 255)
    c_moss_hi = (68, 145, 62, 255)

    # Eldritch touch: Glowing cyan night mushrooms sprouting on the log!
    c_shroom_stem = (210, 245, 255, 255)
    c_shroom_cap = (45, 225, 255, 255)
    c_shroom_glow = (180, 250, 255, 255)

    # 1. Main Horizontal Log Cylinder: x=4..32, y=6..15
    for x in range(4, 33):
        for y in range(6, 16):
            if y == 6 or y == 15:
                pix[x, y] = c_out
            elif y == 7:
                # Moss cover on top of log
                pix[x, y] = c_moss_hi if (x % 3 != 0) else c_moss_dark
            elif y <= 10:
                pix[x, y] = c_bark_rim if (x % 4 == 0) else c_bark_hi
            elif y <= 12:
                pix[x, y] = c_bark_mid
            else:
                pix[x, y] = c_bark_dark

    # 2. Cut Cross-Section Ends (Left end at x=4..7, y=6..15)
    for y in range(6, 16):
        dy = (y - 10.5) / 4.5
        for x in range(1, 6):
            dx = (x - 3.5) / 2.2
            d = math.sqrt(dx * dx + dy * dy)
            if d <= 1.0:
                if d >= 0.88 or x == 1: pix[x, y] = c_out
                elif 0.45 <= d <= 0.65: pix[x, y] = c_cut_ring
                elif dx < 0 and dy < 0: pix[x, y] = c_cut_hi
                else: pix[x, y] = c_cut_mid

    # Left & Right end outlines
    for y in range(6, 16):
        if pix[32, y][3] != 0: pix[33, y] = c_out

    # 3. Glowing cyan night mushrooms on top of log
    shrooms = [(14, 4), (18, 3), (22, 5)]
    for sx, sy in shrooms:
        # Stem
        pix[sx, sy + 1] = c_shroom_stem
        pix[sx, sy + 2] = c_shroom_stem
        # Glowing cap
        pix[sx - 1, sy] = c_shroom_cap
        pix[sx, sy] = c_shroom_glow
        pix[sx + 1, sy] = c_shroom_cap
        pix[sx, sy - 1] = c_shroom_cap

    return img

# ============================================================================
# 5. MEADOW WILDFLOWER CLUSTER (20x16) - YELLOW DAISIES & CYAN STARFLOWERS
# ============================================================================
def draw_wildflowers():
    img = make_rgba(20, 16)
    pix = img.load()

    c_stem = (40, 110, 48, 255)
    c_leaf = (75, 175, 65, 255)
    c_out = (12, 38, 16, 255)

    # Flower 1: Yellow Daisy at (5, 6)
    pix[5, 8] = c_stem; pix[5, 9] = c_stem; pix[5, 10] = c_stem; pix[4, 9] = c_leaf
    pix[5, 6] = (255, 150, 20, 255) # Center
    for ox, oy in [(-1, 0), (1, 0), (0, -1), (0, 1)]: pix[5 + ox, 6 + oy] = (255, 235, 60, 255)

    # Flower 2: White Blossom at (14, 5)
    pix[14, 7] = c_stem; pix[14, 8] = c_stem; pix[14, 9] = c_stem; pix[15, 8] = c_leaf
    pix[14, 5] = (245, 220, 80, 255) # Center
    for ox, oy in [(-1, 0), (1, 0), (0, -1), (0, 1)]: pix[14 + ox, 5 + oy] = (255, 255, 255, 255)

    # Flower 3: Glowing Cyan Ember Starflower at (10, 9)
    pix[10, 11] = c_stem; pix[10, 12] = c_stem
    pix[10, 9] = (220, 255, 255, 255)
    for ox, oy in [(-1, 0), (1, 0), (0, -1), (0, 1)]: pix[10 + ox, 9 + oy] = (45, 220, 255, 255)

    return img

def build_all_props():
    props_dir = "My project/Assets/Resources/Ember/Props"
    docs_dir = "docs"
    os.makedirs(props_dir, exist_ok=True)
    os.makedirs(docs_dir, exist_ok=True)

    apple_tree = draw_apple_tree()
    palm_tree = draw_palm_tree()
    snow_pine = draw_snow_pine()
    fallen_log = draw_fallen_log()
    wildflowers = draw_wildflowers()

    # Save to game props
    apple_tree.save(os.path.join(props_dir, "tree_apple.png"))
    palm_tree.save(os.path.join(props_dir, "tree_palm.png"))
    snow_pine.save(os.path.join(props_dir, "tree_snow.png"))
    fallen_log.save(os.path.join(props_dir, "log_fallen.png"))
    wildflowers.save(os.path.join(props_dir, "wildflowers.png"))

    print("All extended environment props exported to Props directory!")

    # ------------------------------------------------------------------------
    # Generate Environment Showcase Sheet
    # ------------------------------------------------------------------------
    pw, ph = 960, 480
    sheet = Image.new("RGBA", (pw, ph), (15, 12, 22, 255))
    draw = ImageDraw.Draw(sheet)

    draw.rectangle([(0, 0), (pw, 44)], fill=(24, 18, 36, 255))
    draw.line([(0, 44), (pw, 44)], fill=(80, 60, 120, 255), width=2)
    draw.text((20, 12), "EMBER · BỘ TÀI NGUYÊN MÔ PHỎNG 1:1 NGUYÊN MẪU & NÉT CHẤM PHÁ PHÁT QUANG", fill=(255, 230, 180, 255))

    props = [
        ("Cây Táo Quả Đỏ (tree_apple)", apple_tree, "48×56px", "Tán mây tròn 5 khối, sai trĩu quả táo đỏ mọng, gốc bạnh rêu phong."),
        ("Cây Cọ / Dừa (tree_palm)", palm_tree, "40×48px", "Thân đốt cong mềm mại, tán lá xòe cánh sao nhiệt đới bờ cát."),
        ("Cây Thông Tuyết (tree_snow)", snow_pine, "36×48px", "4 tầng tán lá phủ dày lớp tuyết xốp mùa đông trắng xóa."),
        ("Khúc Gỗ Mục (log_fallen)", fallen_log, "36×18px", "Thân gỗ mục đổ rêu phong, nấm phát quang cyan ban đêm."),
        ("Khóm Hoa Dại (wildflowers)", wildflowers, "20×16px", "Cúc vàng dại, hoa trắng và dạ thảo phát sáng lung linh.")
    ]

    col_w = (pw - 40) // len(props)
    for i, (name, img_asset, size_str, desc) in enumerate(props):
        bx = 20 + i * col_w
        by = 60
        bw = col_w - 12
        bh = ph - 80
        draw.rounded_rectangle([(bx, by), (bx + bw, by + bh)], radius=6, fill=(20, 16, 28, 255), outline=(52, 42, 70, 255))
        draw.text((bx + 10, by + 12), name.split(" ")[0], fill=(240, 215, 160, 255))
        draw.text((bx + 10, by + 30), size_str, fill=(120, 220, 200, 255))

        # Scaled sprite preview
        scale = 3
        sw, sh = img_asset.width * scale, img_asset.height * scale
        scaled = img_asset.resize((sw, sh), Image.NEAREST)
        px = bx + (bw - sw) // 2
        py = by + 65 + (220 - sh) // 2
        sheet.paste(scaled, (px, py), scaled)

        # Multi-line description
        words = desc.split()
        lines = []
        cur = ""
        for w in words:
            if len(cur + " " + w) > 20:
                lines.append(cur)
                cur = w
            else:
                cur = cur + " " + w if cur else w
        if cur: lines.append(cur)

        for li, line in enumerate(lines):
            draw.text((bx + 10, by + 305 + li * 16), line, fill=(160, 150, 175, 255))

    sheet.save("docs/environment_props_showcase.png")
    artifact_dir = "C:/Users/OS/.gemini/antigravity/brain/dc6023b7-ad69-4d5b-a5d0-20427d5efb77"
    if os.path.exists(artifact_dir):
        sheet.save(os.path.join(artifact_dir, "environment_props_showcase.png"))
    print("Saved docs/environment_props_showcase.png successfully!")

if __name__ == "__main__":
    build_all_props()
