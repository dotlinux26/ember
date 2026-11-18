import math
import os
from PIL import Image, ImageDraw, ImageFont

def make_rgba(w, h):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))

# ============================================================================
# 1. BEACON: DORMANT (DARK) & AWAKENED (LIT) (48x64)
# ============================================================================
def draw_master_beacon(lit=True):
    img = make_rgba(48, 64)
    pix = img.load()

    c_out = (8, 6, 12, 255)
    c_stone_dark = (16, 14, 22, 255)
    c_stone_mid = (30, 26, 42, 255)
    c_stone_hi = (50, 44, 68, 255)
    c_stone_rim = (76, 70, 102, 255)

    c_gold_dark = (50, 32, 16, 255) if not lit else (90, 52, 22, 255)
    c_gold_mid = (100, 68, 30, 255) if not lit else (165, 108, 42, 255)
    c_gold_hi = (150, 105, 45, 255) if not lit else (235, 172, 68, 255)
    c_gold_shine = (190, 140, 70, 255) if not lit else (255, 235, 140, 255)

    if lit:
        c_cyan_dark = (8, 52, 70, 255)
        c_cyan_mid = (18, 138, 175, 255)
        c_cyan_bright = (45, 230, 255, 255)
        c_cyan_glow = (185, 252, 255, 255)
        c_white = (255, 255, 255, 255)
    else:
        # Dark dormant rune & dormant unlit core
        c_cyan_dark = (14, 16, 24, 255)
        c_cyan_mid = (22, 26, 38, 255)
        c_cyan_bright = (36, 42, 58, 255)
        c_cyan_glow = (52, 60, 80, 255)
        c_white = (70, 80, 105, 255)

    # 1. Base Stepped Slabs: y=53..63
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

    # Gothic Flying Buttresses (y=38..53)
    for y in range(38, 54):
        h = 53 - y
        w_span = int(h * 0.58)
        for x in range(15 - w_span, 15):
            if x == 15 - w_span or y == 38: pix[x, y] = c_out
            elif x == 16 - w_span: pix[x, y] = c_stone_rim
            else: pix[x, y] = c_stone_mid
        for x in range(33, 33 + w_span):
            if x == 32 + w_span or y == 38: pix[x, y] = c_out
            else: pix[x, y] = c_stone_dark

    # 2. Octagonal Obelisk Pillar: y=20..52
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

    # Carved runic ribbon winding around pillar
    for y in range(24, 49):
        wave = math.sin((y - 24) * 0.42) * 4.8
        rx = int(23.5 + wave)
        if 16 <= rx <= 31:
            pix[rx, y] = c_cyan_glow
            if rx > 0: pix[rx - 1, y] = c_cyan_bright
            if rx < 47: pix[rx + 1, y] = c_cyan_mid

    # 3. Gold Filigree Astragal Collar: y=18..20
    for y in range(18, 21):
        for x in range(13, 35):
            if x == 13 or x == 34 or y == 18 or y == 20:
                pix[x, y] = c_out
            else:
                pix[x, y] = c_gold_shine if (x % 2 == 0) else c_gold_hi

    # 4. Floating Astrolabe Armillary Cage (y=6..18)
    for y in range(6, 18):
        pix[13, y] = c_out; pix[14, y] = c_gold_hi
        pix[33, y] = c_gold_mid; pix[34, y] = c_out
        if y in (9, 13, 17):
            for x in range(14, 34, 3):
                pix[x, y] = c_gold_mid

    # 5. Core
    cx, cy = 23.5, 11.0
    for y in range(4, 17):
        for x in range(16, 32):
            d = math.sqrt((x - cx) ** 2 + ((y - cy) * 1.1) ** 2)
            if d <= 5.8:
                if d <= 1.8: pix[x, y] = c_white
                elif d <= 3.2: pix[x, y] = c_cyan_glow
                elif d <= 4.6: pix[x, y] = c_cyan_bright
                else: pix[x, y] = c_cyan_mid

    if lit:
        # Ascending flame tongues & sparks
        pix[23, 3] = c_cyan_glow; pix[24, 3] = c_white
        pix[22, 4] = c_cyan_bright; pix[25, 4] = c_cyan_glow
        pix[23, 1] = c_cyan_bright; pix[25, 0] = c_cyan_glow

    # Golden spire finial crown at top
    pix[23, 2] = c_gold_shine; pix[24, 2] = c_gold_hi

    return img

# ============================================================================
# 2. FLOATING LEVITATING CRYSTAL SHARDS (14x24)
# ============================================================================
def draw_floating_crystal(lit=False):
    img = make_rgba(14, 24)
    pix = img.load()
    if not lit:
        c_out = (10, 12, 18, 255)
        c_dark = (24, 28, 40, 255)
        c_mid = (44, 50, 70, 255)
        c_hi = (70, 80, 108, 255)
        c_rim = (100, 112, 148, 255)
    else:
        c_out = (6, 45, 62, 255)
        c_dark = (14, 115, 150, 255)
        c_mid = (35, 195, 235, 255)
        c_hi = (165, 245, 255, 255)
        c_rim = (255, 255, 255, 255)

    for y in range(1, 23):
        t = (y - 1) / 21.0
        w = (1.0 - abs(t - 0.5) * 2.0) ** 0.65 * 5.2
        min_x = int(6.5 - w)
        max_x = int(6.5 + w)
        for x in range(min_x, max_x + 1):
            if x == min_x or x == max_x or y == 1 or y == 22:
                pix[x, y] = c_out
            elif x < 6:
                pix[x, y] = c_rim if x == min_x + 1 else c_hi
            elif x == 6:
                pix[x, y] = c_hi if lit else c_mid
            else:
                pix[x, y] = c_mid if x < max_x - 1 else c_dark

    return img

# ============================================================================
# 3. LUSH DECIDUOUS TREE WITH APPLE / BERRY DOTS (48x56) - EXACT REF 1 & 2
# ============================================================================
def draw_ref_oak(with_apples=True):
    img = make_rgba(48, 56)
    pix = img.load()

    c_out = (10, 8, 6, 255)
    c_bark_dark = (36, 20, 12, 255)
    c_bark_mid = (68, 40, 22, 255)
    c_bark_hi = (105, 65, 34, 255)
    c_bark_rim = (145, 92, 50, 255)
    c_moss = (54, 98, 38, 255)

    c_fol_out = (8, 38, 18, 255)
    c_fol_shadow = (18, 64, 30, 255)
    c_fol_mid = (36, 110, 48, 255)
    c_fol_hi = (68, 155, 62, 255)
    c_fol_top = (105, 198, 78, 255)
    c_fol_sun = (155, 230, 108, 255)

    c_apple = (215, 38, 30, 255)
    c_apple_shine = (255, 160, 140, 255)

    # 1. TRUNK & ROOTS: y=28..54, cx=24
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

    # 3 Flared Root Claws (Left, Middle, Right) touching the ground
    for x, y in [(15, 53), (14, 54), (13, 54), (12, 54)]:
        pix[x, y] = c_bark_hi; pix[x, y + 1] = c_out
    for x, y in [(32, 53), (33, 54), (34, 54), (35, 54)]:
        pix[x, y] = c_bark_mid; pix[x, y + 1] = c_out
    for x, y in [(23, 53), (24, 54), (25, 54)]:
        pix[x, y] = c_moss # Moss tufts around base of trunk!

    # 2. VOLUMETRIC SPHERICAL CLOUD LOBES:
    lobes = [
        {"cx": 24, "cy": 13, "r": 12.0}, # Top main cloud
        {"cx": 13, "cy": 20, "r": 11.0}, # Left shoulder cloud
        {"cx": 34, "cy": 20, "r": 11.0}, # Right shoulder cloud
        {"cx": 18, "cy": 28, "r": 10.5}, # Bottom-left droop
        {"cx": 29, "cy": 28, "r": 10.5}, # Bottom-right droop
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
                    # Light angle (-45 degrees top-left)
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

    # Red apples / berries scattered in canopy (from reference image 2)
    if with_apples:
        apple_spots = [(17, 18), (30, 16), (22, 24), (35, 26), (14, 28), (28, 32)]
        for ax, ay in apple_spots:
            pix[ax, ay] = c_apple
            pix[ax - 1, ay] = c_apple
            pix[ax, ay - 1] = c_apple_shine
            if pix[ax + 1, ay][3] != 0: pix[ax + 1, ay] = c_fol_out

    return img

# ============================================================================
# 4. TREE STUMP (24x22) - EXACT FROM REFERENCE 2
# ============================================================================
def draw_ref_stump():
    img = make_rgba(24, 22)
    pix = img.load()

    c_out = (12, 10, 8, 255)
    c_bark_dark = (42, 26, 16, 255)
    c_bark_mid = (76, 46, 28, 255)
    c_bark_hi = (112, 70, 40, 255)
    c_wood_dark = (135, 92, 54, 255)
    c_wood_mid = (175, 125, 78, 255)
    c_wood_hi = (212, 162, 108, 255)
    c_ring = (105, 68, 38, 255)

    # 1. Base trunk: y=7..20
    for y in range(7, 21):
        t = (y - 7) / 13.0
        half_w = 5.5 + t * 4.2
        min_x = int(12 - half_w)
        max_x = int(12 + half_w)
        for x in range(min_x, max_x + 1):
            if x == min_x or x == max_x or y == 20: pix[x, y] = c_out
            elif x < 11: pix[x, y] = c_bark_hi if x == min_x + 1 else c_bark_mid
            elif x < 15: pix[x, y] = c_bark_mid
            else: pix[x, y] = c_bark_dark

    # Root flares
    for x, y in [(3, 19), (2, 20), (1, 20)]: pix[x, y] = c_bark_hi; pix[x, y + 1] = c_out
    for x, y in [(20, 19), (21, 20), (22, 20)]: pix[x, y] = c_bark_mid; pix[x, y + 1] = c_out

    # 2. Cut Top Flat Surface (Ellipse): y=3..7, x=6..18
    for y in range(3, 8):
        dy = (y - 5) / 2.2
        for x in range(6, 19):
            dx = (x - 12) / 5.8
            d = math.sqrt(dx * dx + dy * dy)
            if d <= 1.05:
                if d >= 0.90 or y == 3: pix[x, y] = c_out
                elif 0.50 <= d <= 0.65: pix[x, y] = c_ring # Growth ring
                elif d < 0.25: pix[x, y] = c_ring # Core ring
                elif dx < 0 and dy < 0: pix[x, y] = c_wood_hi # Highlight
                else: pix[x, y] = c_wood_mid

    return img

# ============================================================================
# 5. LARGE CRACKED BOULDER / ROCK (32x28) - EXACT FROM REFERENCE 2
# ============================================================================
def draw_ref_rock():
    img = make_rgba(32, 28)
    pix = img.load()

    c_out = (14, 16, 20, 255)
    c_shadow = (34, 38, 48, 255)
    c_dark = (58, 64, 78, 255)
    c_mid = (95, 102, 118, 255)
    c_hi = (142, 150, 168, 255)
    c_sun = (195, 204, 222, 255)
    c_crack = (20, 22, 28, 255)

    cx, cy = 16.0, 14.0
    for y in range(2, 26):
        dy = (y - cy) / 10.5
        for x in range(3, 29):
            dx = (x - cx) / 12.0
            d = math.sqrt(dx * dx + dy * dy)
            if d <= 1.0:
                # Faceted bevel lighting (light from top-left)
                light_val = -dx * 0.65 - dy * 0.75
                if d >= 0.92: pix[x, y] = c_out
                elif light_val > 0.45: pix[x, y] = c_sun
                elif light_val > 0.15: pix[x, y] = c_hi
                elif light_val > -0.25: pix[x, y] = c_mid
                elif light_val > -0.60: pix[x, y] = c_dark
                else: pix[x, y] = c_shadow

    # Deep carved surface fissures & cracks (from reference image 2)
    cracks = [
        (14, 7), (15, 8), (15, 9), (16, 10), (17, 11), (17, 12), (18, 13),
        (16, 10), (15, 11), (14, 12), (13, 14), (12, 15),
        (20, 14), (21, 15), (22, 16), (23, 17)
    ]
    for kx, ky in cracks:
        if 0 <= kx < 32 and 0 <= ky < 28:
            pix[kx, ky] = c_crack
            if kx > 0 and pix[kx - 1, ky][3] != 0 and pix[kx - 1, ky] != c_out:
                pix[kx - 1, ky] = c_hi # Highlight rim along crack

    return img

# ============================================================================
# 6. ROUND & WIDE BUSHES (24x20 & 36x22) - EXACT FROM REFERENCE 1 & 2
# ============================================================================
def draw_ref_bush_round():
    img = make_rgba(24, 20)
    pix = img.load()

    c_out = (8, 38, 18, 255)
    c_shadow = (18, 64, 30, 255)
    c_mid = (36, 110, 48, 255)
    c_hi = (68, 155, 62, 255)
    c_sun = (135, 218, 92, 255)

    cx, cy = 12.0, 10.0
    for y in range(2, 19):
        dy = (y - cy) / 7.5
        for x in range(2, 22):
            dx = (x - cx) / 9.0
            d = math.sqrt(dx * dx + dy * dy)
            if d <= 1.0:
                light_val = -dx * 0.65 - dy * 0.75
                if d >= 0.88 or y == 18: pix[x, y] = c_out
                elif light_val > 0.35: pix[x, y] = c_sun
                elif light_val > 0.0: pix[x, y] = c_hi
                elif light_val > -0.45: pix[x, y] = c_mid
                else: pix[x, y] = c_shadow

    return img

def draw_ref_bush_wide():
    img = make_rgba(36, 22)
    pix = img.load()

    c_out = (8, 38, 18, 255)
    c_shadow = (18, 64, 30, 255)
    c_mid = (36, 110, 48, 255)
    c_hi = (68, 155, 62, 255)
    c_sun = (135, 218, 92, 255)

    # 3 overlapping lobes
    lobes = [
        {"cx": 11, "cy": 12, "r": 8.0},
        {"cx": 25, "cy": 11, "r": 8.5},
        {"cx": 18, "cy": 8, "r": 7.0}
    ]
    for y in range(1, 21):
        for x in range(1, 35):
            best_val = -1
            best_col = None
            for lobe in lobes:
                dx = x - lobe["cx"]
                dy = y - lobe["cy"]
                dist = math.sqrt(dx * dx + (dy * 1.1) ** 2)
                r = lobe["r"]
                if dist <= r:
                    norm = dist / r
                    light_d = math.sqrt((dx + 0.35 * r) ** 2 + (dy + 0.4 * r) ** 2) / (r * 1.5)
                    if norm > 0.88 or y >= 19: cur_col = c_out
                    elif light_d < 0.32: cur_col = c_sun
                    elif light_d < 0.65: cur_col = c_hi
                    elif light_d < 0.95: cur_col = c_mid
                    else: cur_col = c_shadow

                    priority = (1.0 - norm) * 10.0 + (lobe["cy"])
                    if priority > best_val:
                        best_val = priority
                        best_col = cur_col
            if best_col is not None:
                pix[x, y] = best_col

    return img

# ============================================================================
# EXPORT ALL ASSETS AND BUILD SHOWCASE PREVIEW
# ============================================================================
def build_all():
    props_dir = "My project/Assets/Resources/Ember/Props"
    docs_dir = "docs"
    os.makedirs(props_dir, exist_ok=True)
    os.makedirs(docs_dir, exist_ok=True)

    beacon_lit = draw_master_beacon(True)
    beacon_dark = draw_master_beacon(False)
    crystal_lit = draw_floating_crystal(True)
    crystal_dark = draw_floating_crystal(False)

    tree_oak = draw_ref_oak(True) # With red apples
    stump = draw_ref_stump()
    rock = draw_ref_rock()
    bush_round = draw_ref_bush_round()
    bush_wide = draw_ref_bush_wide()

    # Save to game props
    beacon_lit.save(os.path.join(props_dir, "beacon.png"))
    beacon_lit.save(os.path.join(props_dir, "beacon_lit.png"))
    beacon_dark.save(os.path.join(props_dir, "beacon_dark.png"))
    crystal_lit.save(os.path.join(props_dir, "beacon_crystal_lit.png"))
    crystal_dark.save(os.path.join(props_dir, "beacon_crystal_dark.png"))

    tree_oak.save(os.path.join(props_dir, "tree.png"))
    tree_oak.save(os.path.join(props_dir, "tree_oak.png"))
    stump.save(os.path.join(props_dir, "tree_stump.png"))
    rock.save(os.path.join(props_dir, "rock_large.png"))
    bush_round.save(os.path.join(props_dir, "bush_round.png"))
    bush_wide.save(os.path.join(props_dir, "bush_wide.png"))

    print("All exact reference assets exported to Props directory!")

    # ------------------------------------------------------------------------
    # Create Side-by-Side Comparison Showcase Artifact
    # ------------------------------------------------------------------------
    pw, ph = 1040, 680
    preview = Image.new("RGBA", (pw, ph), (15, 12, 22, 255))
    draw = ImageDraw.Draw(preview)

    draw.rectangle([(0, 0), (pw, 48)], fill=(24, 18, 36, 255))
    draw.line([(0, 48), (pw, 48)], fill=(80, 60, 115, 255), width=2)

    try:
        font_title = ImageFont.truetype("arial.ttf", 20)
        font_name = ImageFont.truetype("arialbd.ttf", 13)
        font_sub = ImageFont.truetype("arial.ttf", 11)
    except:
        font_title = font_name = font_sub = ImageFont.load_default()

    draw.text((20, 14), "EMBER · TÁI HIỆN CHÍNH XÁC TỪNG MÓN ĐỒ TỪ ẢNH MẪU & TRỤ TINH THỂ LƠ LỬNG", fill=(255, 230, 180, 255), font=font_title)

    cards = [
        ("Trụ Hải Đăng (Khi Thắp Sáng)", beacon_lit, "48×64px", "Lõi lửa xanh bừng sáng, rune lượn sóng phát quang huỳnh quang."),
        ("Trụ Hải Đăng (Khi Bị Tối)", beacon_dark, "48×64px", "Trạng thái bình thường chưa thắp sáng, đá cổ kính u tối, lõi lạnh ngắt."),
        ("Tinh Thể Lơ Lửng (Tối & Sáng)", crystal_lit, "14×24px", "Lơ lửng hình sin 3D quanh trụ: bình thường bị tối, khi nạp đầy thì bừng sáng."),
        ("Cây Tán Mây Tròn (Ref 1 & 2)", tree_oak, "48×56px", "Tán mây 5 khối cầu, quả táo đỏ mọng, gốc bạnh rễ bám rêu phong."),
        ("Gốc Cây Cắt (Ref 2)", stump, "24×22px", "Mặt cắt elip lộ vân vòng năm tuổi của gỗ, thân xòe rễ bám đất."),
        ("Tảng Đá Nứt (Ref 2)", rock, "32×28px", "Đá hoa cương vát cạnh góc cạnh, có vết nứt sâu gãy khúc sắc nét."),
        ("Bụi Cây & Khóm Cỏ (Ref 1 & 2)", bush_wide, "36×22px", "Bụi cây bồng bềnh 3 khối và bụi cây tròn đổ bóng sáng tối chuẩn JRPG.")
    ]

    card_w = 138
    card_h = 580
    start_x = 18
    spacing = 10

    for idx, (title, s_img, sz_text, desc) in enumerate(cards):
        cx = start_x + idx * (card_w + spacing)
        cy = 64

        draw.rounded_rectangle([(cx, cy), (cx + card_w, cy + card_h)], radius=8, fill=(22, 17, 30, 255), outline=(52, 40, 70, 255))
        draw.text((cx + 8, cy + 10), title[:20], fill=(245, 215, 160, 255), font=font_name)
        draw.text((cx + 8, cy + 28), sz_text, fill=(115, 180, 235, 255), font=font_sub)

        # Sprite Box
        box_w = 122
        box_h = 240
        bx = cx + 8
        by = cy + 46
        draw.rectangle([(bx, by), (bx + box_w, by + box_h)], fill=(12, 10, 16, 255), outline=(40, 32, 52, 255))

        scale = 3.6 if s_img.size[0] <= 32 else 3.0
        fw, fh = s_img.size
        sw = int(fw * scale)
        sh = int(fh * scale)
        s_scaled = s_img.resize((sw, sh), Image.NEAREST)

        sx = bx + (box_w - sw) // 2
        sy = by + (box_h - sh) // 2
        preview.paste(s_scaled, (sx, sy), s_scaled)

        # Extra display for Crystal: show both dark & lit side by side!
        if idx == 2:
            s_dark_scaled = crystal_dark.resize((int(14 * 3.6), int(24 * 3.6)), Image.NEAREST)
            preview.paste(s_dark_scaled, (bx + 14, by + 70), s_dark_scaled)
            preview.paste(s_scaled, (bx + box_w - sw - 14, by + 70), s_scaled)
            draw.text((bx + 16, by + 165), "TỐI", fill=(120, 130, 160, 255), font=font_sub)
            draw.text((bx + box_w - 40, by + 165), "SÁNG", fill=(45, 230, 255, 255), font=font_sub)

        # Wrapped description
        lines = []
        cur = ""
        for w in desc.split():
            if len(cur + " " + w) <= 20:
                cur = (cur + " " + w).strip()
            else:
                if cur: lines.append(cur)
                cur = w
        if cur: lines.append(cur)

        for li, line in enumerate(lines[:10]):
            draw.text((cx + 8, cy + 300 + li * 18), line, fill=(170, 160, 185, 255), font=font_sub)

    preview.save("docs/exact_reference_showcase.png")
    artifact_dir = "C:/Users/OS/.gemini/antigravity/brain/dc6023b7-ad69-4d5b-a5d0-20427d5efb77"
    if os.path.exists(artifact_dir):
        preview.save(os.path.join(artifact_dir, "exact_reference_showcase.png"))
    print("Saved docs/exact_reference_showcase.png successfully!")

if __name__ == "__main__":
    build_all()
