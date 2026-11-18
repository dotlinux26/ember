import math
import os
from PIL import Image, ImageDraw, ImageFont

def make_rgba(w, h):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))

# ============================================================================
# CLOUD-LOBE DECIDUOUS TREE GENERATOR (48x56)
# ============================================================================
def draw_cloud_tree(palette_type="green"):
    # Size: 48 wide x 56 tall
    img = make_rgba(48, 56)
    pix = img.load()

    # Bark palette
    c_out = (14, 10, 8, 255)
    c_bark_dark = (38, 22, 14, 255)
    c_bark_mid = (68, 42, 24, 255)
    c_bark_hi = (105, 68, 38, 255)
    c_bark_light = (142, 95, 54, 255)

    if palette_type == "green":
        c_fol_out = (12, 24, 14, 255)
        c_fol_shadow = (22, 54, 26, 255)
        c_fol_mid = (38, 92, 44, 255)
        c_fol_hi = (62, 138, 58, 255)
        c_fol_top = (98, 185, 74, 255)
        c_fol_sun = (142, 222, 98, 255)
    elif palette_type == "autumn": # Autumn Red/Amber
        c_fol_out = (24, 10, 8, 255)
        c_fol_shadow = (65, 20, 16, 255)
        c_fol_mid = (115, 38, 24, 255)
        c_fol_hi = (175, 65, 32, 255)
        c_fol_top = (225, 115, 42, 255)
        c_fol_sun = (255, 175, 75, 255)
    elif palette_type == "void": # Eclipse Violet/Magenta
        c_fol_out = (14, 8, 22, 255)
        c_fol_shadow = (34, 16, 52, 255)
        c_fol_mid = (64, 28, 94, 255)
        c_fol_hi = (105, 45, 145, 255)
        c_fol_top = (155, 72, 198, 255)
        c_fol_sun = (212, 135, 245, 255)

    # 1. TRUNK & ROOTS: y=28..54, cx=24
    for y in range(26, 55):
        t = (y - 26) / 28.0
        # Trunk width: narrow at top (w=7), flares wide at base (w=16)
        half_w = 3.5 + (t ** 2.2) * 5.2
        min_x = int(24 - half_w)
        max_x = int(24 + half_w)
        for x in range(min_x, max_x + 1):
            if x == min_x or x == max_x or y == 54:
                pix[x, y] = c_out
            elif x == min_x + 1:
                pix[x, y] = c_bark_hi
            elif x < 23:
                pix[x, y] = c_bark_mid if (x % 2 == 0) else c_bark_hi
            elif x < 27:
                pix[x, y] = c_bark_mid
            else:
                pix[x, y] = c_bark_dark

    # Root spurs clawing the ground at base
    # Left root spur
    for x, y in [(15, 53), (14, 54), (13, 54), (12, 54)]:
        pix[x, y] = c_bark_hi
        pix[x, y + 1] = c_out
    # Right root spur
    for x, y in [(32, 53), (33, 54), (34, 54), (35, 54)]:
        pix[x, y] = c_bark_mid
        pix[x, y + 1] = c_out

    # 2. VOLUMETRIC CLOUD-LOBE CANOPY:
    # 5 primary puffy spheres (cx, cy, radius):
    lobes = [
        {"cx": 24, "cy": 13, "r": 11.5}, # Top main lobe
        {"cx": 14, "cy": 19, "r": 11.0}, # Left shoulder lobe
        {"cx": 33, "cy": 19, "r": 11.0}, # Right shoulder lobe
        {"cx": 18, "cy": 27, "r": 10.5}, # Bottom-left lobe
        {"cx": 29, "cy": 27, "r": 10.5}, # Bottom-right lobe
    ]

    # Render each lobe with spherical pillow shading
    # Sun light comes from top-left (angle ~ -45 degrees)
    for y in range(2, 38):
        for x in range(2, 46):
            # Find best fitting lobe and its internal normalized distance
            best_val = -1
            best_col = None
            is_edge = False

            # Check lobes in back-to-front or composite order
            for lobe in lobes:
                dx = x - lobe["cx"]
                dy = y - lobe["cy"]
                dist = math.sqrt(dx * dx + (dy * 1.05) ** 2)
                r = lobe["r"]

                if dist <= r:
                    norm = dist / r
                    # Light direction vector (dx - 0.35*r, dy - 0.4*r)
                    light_d = math.sqrt((dx + 0.32 * r) ** 2 + (dy + 0.38 * r) ** 2) / (r * 1.5)

                    if norm > 0.88:
                        cur_col = c_fol_out
                    elif light_d < 0.28:
                        cur_col = c_fol_sun
                    elif light_d < 0.50:
                        cur_col = c_fol_top
                    elif light_d < 0.72:
                        cur_col = c_fol_hi
                    elif light_d < 0.95:
                        cur_col = c_fol_mid
                    else:
                        cur_col = c_fol_shadow

                    priority = (1.0 - norm) * 10.0 + (lobe["cy"]) # closer lobes paint in front
                    if priority > best_val:
                        best_val = priority
                        best_col = cur_col

            if best_col is not None:
                pix[x, y] = best_col

    return img

# ============================================================================
# DEAD ANTLER THORN TREE (48x56) - Bare Gnarled Twisted Wasteland Tree
# ============================================================================
def draw_dead_tree():
    img = make_rgba(48, 56)
    pix = img.load()

    c_out = (12, 8, 8, 255)
    c_bark_dark = (34, 20, 16, 255)
    c_bark_mid = (58, 36, 26, 255)
    c_bark_hi = (92, 58, 42, 255)
    c_bark_rim = (135, 88, 62, 255)

    # 1. Base trunk: y=32..54, cx=24
    for y in range(32, 55):
        t = (y - 32) / 22.0
        half_w = 3.0 + (t ** 2.0) * 4.5
        min_x = int(24 - half_w)
        max_x = int(24 + half_w)
        for x in range(min_x, max_x + 1):
            if x == min_x or x == max_x or y == 54:
                pix[x, y] = c_out
            elif x < 23:
                pix[x, y] = c_bark_rim if x == min_x + 1 else c_bark_hi
            elif x < 27:
                pix[x, y] = c_bark_mid
            else:
                pix[x, y] = c_bark_dark

    # Root claws at base
    for x, y in [(16, 53), (15, 54), (14, 54), (13, 54)]:
        pix[x, y] = c_bark_hi; pix[x, y + 1] = c_out
    for x, y in [(31, 53), (32, 54), (33, 54), (34, 54)]:
        pix[x, y] = c_bark_mid; pix[x, y + 1] = c_out

    # 2. Main splitting gnarled branches (sharp antler forks)
    branches = [
        # Center-left main bough
        [(23, 32), (21, 28), (18, 24), (15, 20), (12, 16), (9, 11), (7, 6)],
        # Far-left twig
        [(18, 24), (14, 23), (10, 20), (7, 18), (5, 14)],
        # Center-left fork
        [(15, 20), (16, 15), (15, 10), (14, 6)],
        # Center-right main bough
        [(25, 32), (27, 27), (30, 23), (34, 18), (38, 13), (40, 8), (41, 4)],
        # Far-right twig
        [(30, 23), (34, 22), (38, 19), (42, 16), (44, 12)],
        # Center-right fork
        [(34, 18), (32, 14), (31, 9), (30, 5)],
        # Crown vertical spikes
        [(24, 30), (24, 24), (23, 19), (24, 14), (23, 9), (24, 4)]
    ]

    for b in branches:
        for idx, (bx, by) in enumerate(b):
            # Branch thickness: 2px at base, 1px at tip
            thick = 2 if idx < len(b) - 2 else 1
            for tx in range(bx, bx + thick):
                if 0 <= tx < 48 and 0 <= by < 56:
                    pix[tx, by] = c_bark_hi if tx == bx else c_bark_mid
                    # Outline around branch
                    if tx > 0 and pix[tx - 1, by][3] == 0: pix[tx - 1, by] = c_out
                    if tx < 47 and pix[tx + 1, by][3] == 0: pix[tx + 1, by] = c_out
                    if by > 0 and pix[tx, by - 1][3] == 0: pix[tx, by - 1] = c_out
                    if by < 55 and pix[tx, by + 1][3] == 0: pix[tx, by + 1] = c_out

    return img

# ============================================================================
# LAYERED DARK CONIFER / PINE (36x56) - Sharp Needle Tiers with Snowy Frost
# ============================================================================
def draw_layered_pine():
    img = make_rgba(36, 56)
    pix = img.load()

    c_out = (10, 16, 18, 255)
    c_bark_dark = (24, 18, 14, 255)
    c_bark_mid = (48, 34, 26, 255)
    c_bark_hi = (78, 54, 40, 255)

    c_needle_deep = (12, 32, 36, 255)
    c_needle_mid = (22, 54, 58, 255)
    c_needle_hi = (36, 82, 86, 255)
    c_needle_rim = (58, 122, 125, 255)
    c_frost = (165, 220, 230, 255)
    c_snow = (235, 250, 255, 255)

    # 1. Pine Trunk: y=42..54, cx=18
    for y in range(42, 55):
        t = (y - 42) / 12.0
        half_w = 2.0 + t * 2.2
        min_x = int(18 - half_w)
        max_x = int(18 + half_w)
        for x in range(min_x, max_x + 1):
            if x == min_x or x == max_x or y == 54:
                pix[x, y] = c_out
            elif x < 18:
                pix[x, y] = c_bark_hi
            else:
                pix[x, y] = c_bark_dark

    # 2. 5 Distinct Downward-Scalloped Needle Tiers (y=3..44)
    tiers = [
        {"top_y": 3, "bot_y": 12, "w": 6},
        {"top_y": 9, "bot_y": 20, "w": 9},
        {"top_y": 16, "bot_y": 28, "w": 12},
        {"top_y": 24, "bot_y": 36, "w": 14},
        {"top_y": 32, "bot_y": 44, "w": 16}
    ]

    for tier in tiers:
        top_y = tier["top_y"]
        bot_y = tier["bot_y"]
        max_w = tier["w"]
        for y in range(top_y, bot_y + 1):
            t = (y - top_y) / float(bot_y - top_y)
            w = t * max_w
            # Scalloped flared corners
            if y >= bot_y - 2: w += 1.2
            min_x = int(18 - w)
            max_x = int(18 + w)
            for x in range(min_x, max_x + 1):
                if x < 0 or x >= 36: continue
                if x == min_x or x == max_x or y == top_y:
                    pix[x, y] = c_out
                else:
                    # Frost caps on the top-left shoulder of each tier
                    if y <= top_y + 2 and x < 18:
                        pix[x, y] = c_snow if y == top_y + 1 and x < 17 else c_frost
                    elif x < 17:
                        pix[x, y] = c_needle_rim if x == min_x + 1 else c_needle_hi
                    elif x < 24:
                        pix[x, y] = c_needle_mid
                    else:
                        pix[x, y] = c_needle_deep

    return img

# ============================================================================
# MASTER RUNIC BEACON (48x64) - Grand Cathedral Obelisk & Eternal Ember Core
# ============================================================================
def draw_master_beacon():
    img = make_rgba(48, 64)
    pix = img.load()

    c_out = (8, 6, 12, 255)
    c_stone_dark = (18, 15, 26, 255)
    c_stone_mid = (32, 28, 48, 255)
    c_stone_hi = (54, 48, 76, 255)
    c_stone_rim = (86, 78, 118, 255)

    c_gold_dark = (90, 52, 22, 255)
    c_gold_mid = (165, 108, 42, 255)
    c_gold_hi = (235, 172, 68, 255)
    c_gold_shine = (255, 235, 140, 255)

    c_cyan_dark = (8, 52, 70, 255)
    c_cyan_mid = (18, 138, 175, 255)
    c_cyan_bright = (45, 230, 255, 255)
    c_cyan_glow = (185, 252, 255, 255)
    c_white = (255, 255, 255, 255)

    # 1. Grand Cathedral Base Dais: y=50..63
    # Bottom stepped slab: y=58..63, x=4..43
    for y in range(58, 64):
        for x in range(4, 44):
            if x == 4 or x == 43 or y == 58 or y == 63:
                pix[x, y] = c_out
            elif y == 59:
                pix[x, y] = c_stone_rim if x < 24 else c_stone_hi
            elif x < 24:
                pix[x, y] = c_stone_hi if (x % 3 == 0) else c_stone_mid
            else:
                pix[x, y] = c_stone_dark

    # Mid stepped slab: y=53..57, x=8..39
    for y in range(53, 58):
        for x in range(8, 40):
            if x == 8 or x == 39 or y == 53:
                pix[x, y] = c_out
            elif y == 54:
                pix[x, y] = c_gold_hi if (x % 4 == 0) else c_stone_rim
            elif x < 24:
                pix[x, y] = c_stone_hi if x < 16 else c_stone_mid
            else:
                pix[x, y] = c_stone_dark

    # Arched Gothic Flying Buttresses flanking the column (y=38..53)
    # Left buttress: x=6..14, Right buttress: x=33..41
    for y in range(38, 54):
        h = 53 - y
        w_span = int(h * 0.58)
        for x in range(15 - w_span, 15):
            if x == 15 - w_span or y == 38:
                pix[x, y] = c_out
            elif x == 16 - w_span:
                pix[x, y] = c_stone_rim
            else:
                pix[x, y] = c_stone_mid
        for x in range(33, 33 + w_span):
            if x == 32 + w_span or y == 38:
                pix[x, y] = c_out
            else:
                pix[x, y] = c_stone_dark

    # 2. Tapered Octagonal Obelisk Pillar: y=20..52
    for y in range(20, 53):
        t = (y - 20) / 32.0
        half_w = 7.0 + t * 2.5
        min_x = int(24 - half_w)
        max_x = int(24 + half_w)
        for x in range(min_x, max_x + 1):
            if x == min_x or x == max_x or y == 52:
                pix[x, y] = c_out
            elif x == min_x + 1:
                pix[x, y] = c_stone_rim
            elif x < 23:
                pix[x, y] = c_stone_hi if (x % 3 == 0) else c_stone_mid
            elif x < 29:
                pix[x, y] = c_stone_mid if (x % 3 == 0) else c_stone_dark
            else:
                pix[x, y] = c_stone_dark

    # Deep carved glowing runic ribbon winding around the pillar
    for y in range(24, 49):
        wave = math.sin((y - 24) * 0.42) * 4.8
        rx = int(23.5 + wave)
        if 16 <= rx <= 31:
            pix[rx, y] = c_cyan_glow
            if rx > 0: pix[rx - 1, y] = c_cyan_bright
            if rx < 47: pix[rx + 1, y] = c_cyan_mid
            if y > 0: pix[rx, y - 1] = c_cyan_dark

    # 3. Gold Filigree Astragal Collar: y=18..20, x=13..34
    for y in range(18, 21):
        for x in range(13, 35):
            if x == 13 or x == 34 or y == 18 or y == 20:
                pix[x, y] = c_out
            else:
                pix[x, y] = c_gold_shine if (x % 2 == 0) else c_gold_hi

    # 4. Floating Astrolabe / Celestial Armillary Cage (y=6..18)
    for y in range(6, 18):
        # Left and right outer cage pillars
        pix[13, y] = c_out; pix[14, y] = c_gold_hi
        pix[33, y] = c_gold_mid; pix[34, y] = c_out
        # Transverse orbital rings
        if y in (9, 13, 17):
            for x in range(14, 34, 3):
                pix[x, y] = c_gold_mid

    # 5. THE ETERNAL EMBER FLAME CORE (y=4..16, cx=23.5, cy=11)
    cx, cy = 23.5, 11.0
    for y in range(4, 17):
        for x in range(16, 32):
            d = math.sqrt((x - cx) ** 2 + ((y - cy) * 1.1) ** 2)
            if d <= 5.8:
                if d <= 1.8:
                    pix[x, y] = c_white
                elif d <= 3.2:
                    pix[x, y] = c_cyan_glow
                elif d <= 4.6:
                    pix[x, y] = c_cyan_bright
                else:
                    pix[x, y] = c_cyan_mid

    # Ascending flame tongues & sparks
    pix[23, 3] = c_cyan_glow; pix[24, 3] = c_white
    pix[22, 4] = c_cyan_bright; pix[25, 4] = c_cyan_glow
    pix[23, 1] = c_cyan_bright; pix[25, 0] = c_cyan_glow

    # Levitating focus crystals
    pix[17, 8] = c_cyan_glow; pix[18, 8] = c_white
    pix[29, 8] = c_white; pix[30, 8] = c_cyan_glow

    # Golden spire finial crown at top
    pix[23, 2] = c_gold_shine; pix[24, 2] = c_gold_hi

    return img

def build_assets():
    os.makedirs("My project/Assets/Resources/Ember/Props", exist_ok=True)
    os.makedirs("docs", exist_ok=True)

    tree_oak = draw_cloud_tree("green")
    tree_autumn = draw_cloud_tree("autumn")
    tree_void = draw_cloud_tree("void")
    tree_dead = draw_dead_tree()
    tree_pine = draw_layered_pine()
    beacon = draw_master_beacon()

    # Save to game props
    beacon.save("My project/Assets/Resources/Ember/Props/beacon.png")
    tree_oak.save("My project/Assets/Resources/Ember/Props/tree.png") # Default fallback
    tree_oak.save("My project/Assets/Resources/Ember/Props/tree_oak.png")
    tree_autumn.save("My project/Assets/Resources/Ember/Props/tree_autumn.png")
    tree_void.save("My project/Assets/Resources/Ember/Props/tree_void.png")
    tree_dead.save("My project/Assets/Resources/Ember/Props/tree_dead.png")
    tree_pine.save("My project/Assets/Resources/Ember/Props/tree_pine.png")

    print("Exported all 5 tree variants + master beacon to Props directory!")

    # -------------------------------------------------------------
    # High-Resolution Showcase Comparison Preview
    # -------------------------------------------------------------
    pw, ph = 960, 560
    preview = Image.new("RGBA", (pw, ph), (14, 11, 20, 255))
    draw = ImageDraw.Draw(preview)

    draw.rectangle([(0, 0), (pw, 44)], fill=(22, 17, 32, 255))
    draw.line([(0, 44), (pw, 44)], fill=(75, 55, 110, 255), width=2)

    try:
        font_title = ImageFont.truetype("arial.ttf", 20)
        font_name = ImageFont.truetype("arialbd.ttf", 13)
        font_sub = ImageFont.truetype("arial.ttf", 11)
    except:
        font_title = font_name = font_sub = ImageFont.load_default()

    draw.text((20, 12), "EMBER · BỘ TÀI NGUYÊN 5 LOẠI CÂY CHUẨN PIXEL-ART & TRỤ HẢI ĐĂNG GOTHIC", fill=(255, 230, 180, 255), font=font_title)

    items = [
        ("Trụ Hải Đăng (Master Beacon)", beacon, "48×64px", "Bệ đá đa tầng, vòm bay gothic, obelisk chạm rune cuộn xoắn, đài lửa vĩnh cửu xanh băng."),
        ("Cây Tán Tròn (Lush Oak)", tree_oak, "48×56px", "Tán lá mây bồng bềnh 5 khối cầu, đổ bóng khối đa tầng, gốc cây rễ bám rêu cổ kính."),
        ("Phong Đỏ (Autumn Red)", tree_autumn, "48×56px", "Biến thể phong đỏ rực lá thu, tán lá sắc cam đỏ ấm áp xen kẽ giữa màn đêm."),
        ("Liễu Khô Gai (Dead Thorn Tree)", tree_dead, "48×56px", "Cây khô khẳng khiu vùng đầm lầy/vực thẳm, cành nhánh xương xẩu nhọn sắc như sừng hươu."),
        ("Thông Tầng Sương (Layered Pine)", tree_pine, "36×56px", "Cây thông 5 tầng tán nhọn phủ sương tuyết, dốc nhọn phong cách retro RPG kinh điển."),
        ("Cây Hư Không (Void Violet)", tree_void, "48×56px", "Cây tán tròn đột biến năng lượng Nhật Thực, tán lá tím hồng ma thuật huyền bí.")
    ]

    card_w = 145
    card_h = 470
    start_x = 18
    spacing = 10

    for idx, (title, s_img, sz_text, desc) in enumerate(items):
        cx = start_x + idx * (card_w + spacing)
        cy = 58

        draw.rounded_rectangle([(cx, cy), (cx + card_w, cy + card_h)], radius=8, fill=(20, 16, 28, 255), outline=(48, 38, 64, 255))
        draw.text((cx + 8, cy + 10), title[:22], fill=(245, 215, 160, 255), font=font_name)
        draw.text((cx + 8, cy + 28), sz_text, fill=(115, 175, 225, 255), font=font_sub)

        # Sprite Box
        box_w = 129
        box_h = 240
        bx = cx + 8
        by = cy + 46
        draw.rectangle([(bx, by), (bx + box_w, by + box_h)], fill=(10, 8, 14, 255), outline=(38, 30, 48, 255))

        scale = 3.6
        fw, fh = s_img.size
        sw = int(fw * scale)
        sh = int(fh * scale)
        s_scaled = s_img.resize((sw, sh), Image.NEAREST)

        sx = bx + (box_w - sw) // 2
        sy = by + (box_h - sh) // 2
        preview.paste(s_scaled, (sx, sy), s_scaled)

        # Wrapped description
        lines = []
        words = desc.split()
        cur_line = ""
        for w in words:
            if len(cur_line + " " + w) <= 22:
                cur_line += (" " if cur_line else "") + w
            else:
                lines.append(cur_line)
                cur_line = w
        if cur_line: lines.append(cur_line)

        for li, line in enumerate(lines[:8]):
            draw.text((cx + 8, cy + 296 + li * 18), line, fill=(165, 155, 180, 255), font=font_sub)

    preview.save("docs/reference_trees_preview.png")
    artifact_dir = "C:/Users/OS/.gemini/antigravity/brain/dc6023b7-ad69-4d5b-a5d0-20427d5efb77"
    if os.path.exists(artifact_dir):
        preview.save(os.path.join(artifact_dir, "reference_trees_preview.png"))

    print("Saved docs/reference_trees_preview.png successfully!")

if __name__ == "__main__":
    build_assets()
