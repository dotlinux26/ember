import os
from PIL import Image, ImageDraw, ImageFont

def make_rgba(w, h):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))

# -------------------------------------------------------------
# SHADE (Bóng Ma Rừng Rậm) - 32x32, 4 frames
# -------------------------------------------------------------
def draw_shade(frame):
    img = make_rgba(32, 32)
    pix = img.load()
    
    # Palette
    c_out = (10, 8, 18, 255)
    c_base = (22, 16, 38, 255)
    c_mid = (38, 28, 62, 255)
    c_hi = (58, 44, 92, 255)
    c_rim = (86, 68, 130, 255)
    c_eye_rim = (180, 20, 50, 255)
    c_eye = (255, 50, 90, 255)
    c_eye_glow = (255, 170, 190, 255)
    c_claw = (130, 110, 165, 255)
    c_mist = (45, 32, 70, 140)

    # Motion offsets
    bob = [0, -1, -2, -1][frame]
    reach = [0, 1, 2, 1][frame]
    wisp_shift = [0, 1, 2, 1][frame]
    
    # Mist particles trailing behind
    for mx, my in [(8 - wisp_shift, 27 + bob), (23 + wisp_shift, 28 + bob), (16, 29 + bob)]:
        if 0 <= mx < 32 and 0 <= my < 32:
            pix[mx, my] = c_mist

    # Main Hood & Cloak Body
    # Center x around 16
    for y in range(6 + bob, 27 + bob):
        t = (y - (6 + bob)) / 20.0
        # Width expands as it goes down
        half_w = 4.0 + t * 6.5
        if y < 10 + bob:
            half_w = 3.5 + (y - (6 + bob)) * 0.8
        
        x_min = int(16 - half_w)
        x_max = int(16 + half_w)
        for x in range(x_min, x_max + 1):
            if x == x_min or x == x_max or y == 6 + bob or y == 26 + bob:
                pix[x, y] = c_out
            else:
                # Interior shading
                dist_edge = min(x - x_min, x_max - x)
                if dist_edge == 1:
                    pix[x, y] = c_base
                elif x < 15:
                    pix[x, y] = c_hi if y < 16 + bob else c_mid
                else:
                    pix[x, y] = c_mid if y < 16 + bob else c_base

    # Hood Crown / Horned cowl tips
    pix[12, 5 + bob] = c_out; pix[13, 5 + bob] = c_mid; pix[14, 5 + bob] = c_hi
    pix[15, 5 + bob] = c_hi; pix[16, 5 + bob] = c_hi; pix[17, 5 + bob] = c_mid; pix[18, 5 + bob] = c_out
    pix[11, 6 + bob] = c_out; pix[19, 6 + bob] = c_out
    pix[10, 7 + bob] = c_out; pix[20, 7 + bob] = c_out

    # Ragged Tattered Cloak Base (animated hem)
    tatter_pattern = [
        [0, 2, 1, 3, 1, 2, 0],
        [1, 3, 2, 2, 0, 3, 1],
        [2, 1, 3, 0, 2, 1, 2],
        [1, 2, 0, 2, 3, 2, 1]
    ][frame]
    
    col_idx = 0
    for x in range(10, 23, 2):
        drop = tatter_pattern[col_idx % len(tatter_pattern)]
        by = 26 + bob + drop
        if by < 31:
            pix[x, by] = c_out
            pix[x, by - 1] = c_base
        col_idx += 1

    # Shadow void face cavity
    for fy in range(10 + bob, 16 + bob):
        for fx in range(13, 19):
            pix[fx, fy] = (6, 4, 12, 255)

    # Piercing glowing eyes (slit-like with glow aura)
    eye_y = 12 + bob
    # Left eye
    pix[13, eye_y] = c_eye_glow
    pix[14, eye_y] = c_eye
    pix[14, eye_y - 1] = c_eye_rim
    # Right eye
    pix[17, eye_y] = c_eye
    pix[18, eye_y] = c_eye_glow
    pix[17, eye_y - 1] = c_eye_rim

    # Shadowy clawed hands (reaching forward with animation)
    # Left hand
    pix[9 - reach, 17 + bob] = c_out
    pix[9 - reach, 18 + bob] = c_claw
    pix[8 - reach, 19 + bob] = c_claw
    # Right hand
    pix[21 + reach, 16 + bob] = c_out
    pix[22 + reach, 17 + bob] = c_claw
    pix[23 + reach, 18 + bob] = c_claw

    return img

# -------------------------------------------------------------
# SWAMP LEECH (Đỉa Đầm Lầy Biến Dị) - 32x32, 4 frames
# -------------------------------------------------------------
def draw_swamp(frame):
    img = make_rgba(32, 32)
    pix = img.load()
    
    # Palette
    c_out = (8, 16, 14, 255)
    c_base = (14, 34, 30, 255)
    c_mid = (24, 56, 48, 255)
    c_hi = (40, 88, 74, 255)
    c_belly = (18, 42, 36, 255)
    c_bio_rim = (30, 140, 95, 255)
    c_bio = (52, 225, 155, 255)
    c_bio_core = (175, 255, 215, 255)
    c_amber = (255, 145, 30, 255)
    c_amber_glow = (255, 210, 80, 255)
    c_tendril = (20, 50, 42, 230)

    # Animation contraction / lunging
    # Frame 0: coiled, 1: arch up, 2: lunge forward, 3: recoil
    arch = [0, -2, 1, 0][frame]
    lunge = [-1, 0, 2, 0][frame]
    pulse = [0, 1, 2, 1][frame]

    # Trailing tendrils
    for tx, ty in [(7 + lunge, 28), (9 + lunge, 29), (13 + lunge, 30), (18 + lunge, 29), (22 + lunge, 28)]:
        if 0 <= tx < 32 and 0 <= ty < 32:
            pix[tx, ty] = c_tendril

    # Slithering segmented body
    # Center x around 16 + lunge, y around 13..27
    for y in range(12 + arch, 28):
        t = (y - (12 + arch)) / (28 - (12 + arch))
        # Bulbous head at top, tapered tail at bottom
        half_w = 6.0 - abs(t - 0.4) * 4.5
        cx = 16 + lunge + int(1.5 * (1.0 - t))
        x_min = int(cx - half_w)
        x_max = int(cx + half_w)
        for x in range(x_min, x_max + 1):
            if x == x_min or x == x_max or y == 12 + arch or y == 27:
                pix[x, y] = c_out
            else:
                dist = x - x_min
                if dist < 2:
                    pix[x, y] = c_belly
                elif dist < half_w:
                    pix[x, y] = c_mid if y < 20 else c_base
                else:
                    pix[x, y] = c_hi if y < 20 else c_mid

    # Dorsal Spikes & Bioluminescent Pustules
    pustules = [
        (13 + lunge, 15 + arch),
        (18 + lunge, 14 + arch),
        (15 + lunge, 18 + arch),
        (17 + lunge, 21 + arch)
    ]
    for px, py in pustules:
        if 0 <= px < 32 and 0 <= py < 32:
            pix[px, py] = c_bio_core
            if px > 0: pix[px - 1, py] = c_bio
            if px < 31: pix[px + 1, py] = c_bio
            if py > 0: pix[px, py - 1] = c_bio_rim

    # Leech mouth aperture / ring of needle teeth with amber acid saliva
    mouth_y = 15 + arch
    mouth_x = 16 + lunge
    pix[mouth_x, mouth_y] = (10, 20, 18, 255)
    pix[mouth_x + 1, mouth_y] = (10, 20, 18, 255)
    pix[mouth_x, mouth_y + 1] = c_amber
    pix[mouth_x + 1, mouth_y + 1] = c_amber_glow
    # Needle teeth
    pix[mouth_x - 1, mouth_y] = (220, 240, 230, 255)
    pix[mouth_x + 2, mouth_y] = (220, 240, 230, 255)

    # Multiple clustered predatory eyes (amber glowing dots)
    eyes = [
        (mouth_x - 3, mouth_y - 2),
        (mouth_x + 4, mouth_y - 2),
        (mouth_x - 2, mouth_y - 4),
        (mouth_x + 3, mouth_y - 4),
    ]
    for ex, ey in eyes:
        if 0 <= ex < 32 and 0 <= ey < 32:
            pix[ex, ey] = c_amber_glow
            if ey > 0: pix[ex, ey - 1] = c_amber

    # Acid droplet trailing on lunge frame
    if frame == 2:
        pix[mouth_x + 3, mouth_y + 4] = c_amber_glow
        pix[mouth_x + 4, mouth_y + 6] = c_amber

    return img

# -------------------------------------------------------------
# VOID STALKER (Thú Săn Hư Không) - 32x32, 4 frames
# -------------------------------------------------------------
def draw_void_stalker(frame):
    img = make_rgba(32, 32)
    pix = img.load()
    
    # Palette
    c_out = (10, 6, 20, 255)
    c_base = (20, 14, 38, 255)
    c_mid = (36, 24, 62, 255)
    c_hi = (58, 38, 96, 255)
    c_rim = (92, 60, 148, 255)
    c_horn = (120, 78, 185, 255)
    c_eye = (195, 80, 255, 255)
    c_eye_core = (245, 190, 255, 255)
    c_claw = (165, 120, 220, 255)

    # Quadruped walk cycle
    step = frame # 0, 1, 2, 3
    head_bob = [0, 1, 0, -1][step]
    tail_wag = [0, 2, 1, -1][step]
    
    # Body torso (sleek predator hound/panther silhouette)
    # Chest at 13..21, Y around 14..22
    for y in range(14, 23):
        for x in range(9, 23):
            # Form an athletic torso shape
            dx = (x - 16) / 7.0
            dy = (y - 18) / 4.0
            if dx*dx + dy*dy <= 1.05:
                if x in (9, 22) or y in (14, 22):
                    pix[x, y] = c_out
                else:
                    pix[x, y] = c_hi if y < 17 and x < 16 else c_mid if y < 19 else c_base

    # Muscular predatory head with snout
    hx = 20
    hy = 12 + head_bob
    for y in range(hy - 3, hy + 4):
        for x in range(hx - 3, hx + 5):
            if x >= 31 or y >= 31: continue
            dist = (x - hx)**2 + (y - hy)**2
            if dist <= 12:
                if dist >= 9:
                    pix[x, y] = c_out
                else:
                    pix[x, y] = c_hi if y < hy else c_mid

    # Sleek curved demonic horns
    horn_pts = [
        (hx - 2, hy - 4), (hx - 3, hy - 6), (hx - 4, hy - 8),
        (hx + 1, hy - 4), (hx + 2, hy - 6), (hx + 3, hy - 8)
    ]
    for px, py in horn_pts:
        if 0 <= px < 32 and 0 <= py < 32:
            pix[px, py] = c_horn
            pix[px - 1, py] = c_out

    # Glowing slit eye (piercing violet)
    pix[hx + 1, hy] = c_eye_core
    pix[hx + 2, hy] = c_eye
    pix[hx + 1, hy - 1] = c_rim

    # Barbed whip tail (arched over back, animated)
    tail_pts = [
        (9, 16), (7, 14), (5, 12 - tail_wag), (4, 9 - tail_wag),
        (5, 7 - tail_wag), (7, 6 - tail_wag)
    ]
    for tx, ty in tail_pts:
        if 0 <= tx < 32 and 0 <= ty < 32:
            pix[tx, ty] = c_mid
            if tx > 0: pix[tx - 1, ty] = c_out
    # Tail barb tip
    tip_x, tip_y = 8, 5 - tail_wag
    if 0 <= tip_x < 32 and 0 <= tip_y < 32:
        pix[tip_x, tip_y] = c_claw

    # 4 Legs (Quadruped walk cycle offsets)
    # Front Right & Left legs
    fr_y = 28 if step in (0, 2) else 26
    fl_y = 28 if step in (1, 3) else 26
    # Back Right & Left legs
    br_y = 28 if step in (1, 3) else 26
    bl_y = 28 if step in (0, 2) else 26

    # Front right leg
    for ly in range(21, fr_y):
        pix[19, ly] = c_out; pix[20, ly] = c_mid
    pix[20, fr_y] = c_claw; pix[21, fr_y] = c_out

    # Front left leg
    for ly in range(21, fl_y):
        pix[16, ly] = c_out; pix[17, ly] = c_base
    pix[17, fl_y] = c_claw

    # Back right leg
    for ly in range(20, br_y):
        pix[11, ly] = c_out; pix[12, ly] = c_mid
    pix[12, br_y] = c_claw; pix[13, br_y] = c_out

    # Back left leg
    for ly in range(20, bl_y):
        pix[8, ly] = c_out; pix[9, ly] = c_base
    pix[9, bl_y] = c_claw

    return img

# -------------------------------------------------------------
# SENTINEL ELITE (Hộ Vệ Cổ Ngữ Sa Đọa) - 32x32, 4 frames
# -------------------------------------------------------------
def draw_sentinel(frame):
    img = make_rgba(32, 32)
    pix = img.load()
    
    # Palette
    c_out = (12, 14, 20, 255)
    c_iron_dark = (22, 26, 38, 255)
    c_iron_mid = (36, 44, 62, 255)
    c_iron_hi = (56, 68, 94, 255)
    c_gold_dark = (85, 60, 22, 255)
    c_gold_mid = (150, 110, 42, 255)
    c_gold_hi = (225, 175, 70, 255)
    c_crimson = (240, 30, 65, 255)
    c_crimson_glow = (255, 120, 145, 255)
    c_core_white = (255, 235, 240, 255)

    # Frame animation: heavy stomp, runic core pulse
    stomp = [0, 1, 0, -1][frame]
    paul_float = [-1, 0, 1, 0][frame]
    pulse = [0, 1, 2, 1][frame]

    # Torso Monolith (Heavy angular armored chestplate)
    for y in range(8 + stomp, 24 + stomp):
        half_w = 6.0 if y < 16 + stomp else 5.0
        x_min = int(16 - half_w)
        x_max = int(16 + half_w)
        for x in range(x_min, x_max + 1):
            if x == x_min or x == x_max or y == 8 + stomp or y == 23 + stomp:
                pix[x, y] = c_out
            else:
                if x < 15:
                    pix[x, y] = c_iron_hi if y < 16 + stomp else c_iron_mid
                else:
                    pix[x, y] = c_iron_mid if y < 16 + stomp else c_iron_dark

    # Ancient Corroded Gold Crown & Crest
    crown_y = 6 + stomp
    for x in range(13, 20):
        pix[x, crown_y] = c_gold_hi if x in (14, 16, 18) else c_gold_mid
        pix[x, crown_y - 1] = c_out if x in (13, 16, 19) else (0,0,0,0)
    pix[16, crown_y - 2] = c_gold_hi
    pix[16, crown_y - 3] = c_out

    # Floating Runic Shoulder Pauldrons (levitating relics)
    # Left pauldron
    lpx = 7; lpy = 11 + paul_float
    for y in range(lpy, lpy + 6):
        for x in range(lpx, lpx + 3):
            if x == lpx or x == lpx + 2 or y == lpy or y == lpy + 5:
                pix[x, y] = c_out
            else:
                pix[x, y] = c_gold_hi if y == lpy + 2 else c_iron_mid
    # Right pauldron
    rpx = 22; rpy = 11 + paul_float
    for y in range(rpy, rpy + 6):
        for x in range(rpx, rpx + 3):
            if x == rpx or x == rpx + 2 or y == rpy or y == rpy + 5:
                pix[x, y] = c_out
            else:
                pix[x, y] = c_gold_mid if y == rpy + 2 else c_iron_dark

    # Horizontal Visor Slit (Luminous Crimson Cyclopean Gaze)
    visor_y = 11 + stomp
    pix[14, visor_y] = c_crimson
    pix[15, visor_y] = c_crimson_glow
    pix[16, visor_y] = c_core_white
    pix[17, visor_y] = c_crimson_glow
    pix[18, visor_y] = c_crimson

    # Cracked Runic Chest Core (glowing fracture)
    core_y = 16 + stomp
    pix[16, core_y] = c_core_white
    pix[16, core_y + 1] = c_crimson_glow
    pix[15, core_y + 1] = c_crimson
    pix[17, core_y + 2] = c_crimson if pulse > 0 else c_gold_dark

    # Heavy Iron Golem Legs (walking stomp)
    # Left leg
    ll_y = 28 + (1 if frame == 1 else 0)
    for ly in range(24 + stomp, ll_y):
        pix[12, ly] = c_out; pix[13, ly] = c_iron_mid; pix[14, ly] = c_out
    pix[12, ll_y] = c_out; pix[13, ll_y] = c_gold_mid; pix[14, ll_y] = c_out

    # Right leg
    rl_y = 28 + (1 if frame == 3 else 0)
    for ly in range(24 + stomp, rl_y):
        pix[18, ly] = c_out; pix[19, ly] = c_iron_dark; pix[20, ly] = c_out
    pix[18, rl_y] = c_out; pix[19, rl_y] = c_gold_dark; pix[20, rl_y] = c_out

    return img

# -------------------------------------------------------------
# ECLIPSE BOSS (Đại Thực Thể Nhật Thực) - 64x64, 4 frames
# -------------------------------------------------------------
def draw_boss(frame):
    img = make_rgba(64, 64)
    pix = img.load()
    
    # Palette
    c_out = (12, 10, 18, 255)
    c_dark = (22, 18, 32, 255)
    c_mid = (38, 32, 54, 255)
    c_hi = (58, 48, 80, 255)
    c_rim = (88, 74, 118, 255)
    c_bone = (145, 130, 170, 255)
    c_bone_hi = (195, 180, 220, 255)
    c_eclipse_out = (220, 30, 50, 255)
    c_eclipse_mid = (255, 95, 35, 255)
    c_eclipse_core = (255, 220, 110, 255)
    c_eclipse_white = (255, 255, 220, 255)
    c_mist = (40, 25, 60, 160)

    # Majestic hovering animation
    hover = [0, -1, -2, -1][frame]
    arm_spread = [0, -1, 1, 0][frame]
    flare = [0, 1, 2, 1][frame]

    # Lower Abyssal Mist Tendrils
    for ty in range(48 + hover, 62):
        spread = (ty - 48) * 1.3
        for tx in range(int(32 - spread), int(32 + spread)):
            if (tx + ty + frame * 3) % 4 == 0 and 0 <= tx < 64 and 0 <= ty < 64:
                pix[tx, ty] = c_mist

    # Towering Horned Bone Crown
    # Center around x=32
    horn_pairs = [
        # Main outer sweeping horns
        [(22, 18 + hover), (18, 14 + hover), (15, 9 + hover), (13, 4 + hover), (12, 1 + hover)],
        [(41, 18 + hover), (45, 14 + hover), (48, 9 + hover), (50, 4 + hover), (51, 1 + hover)],
        # Inner horns
        [(25, 16 + hover), (23, 11 + hover), (22, 6 + hover)],
        [(38, 16 + hover), (40, 11 + hover), (41, 6 + hover)],
    ]
    for hp in horn_pairs:
        for hx, hy in hp:
            if 0 <= hx < 64 and 0 <= hy < 64:
                pix[hx, hy] = c_bone_hi
                if hx > 0: pix[hx - 1, hy] = c_bone
                if hx < 63: pix[hx + 1, hy] = c_out
                if hy > 0: pix[hx, hy - 1] = c_out

    # Massive Helm / Mask Face
    for y in range(16 + hover, 30 + hover):
        half_w = 7.0 - abs(y - (23 + hover)) * 0.4
        x_min = int(32 - half_w)
        x_max = int(32 + half_w)
        for x in range(x_min, x_max + 1):
            if x == x_min or x == x_max or y == 16 + hover:
                pix[x, y] = c_out
            else:
                pix[x, y] = c_bone if x < 32 else c_bone_hi

    # Eyes: Luminous twin fiery slits
    eye_y = 22 + hover
    pix[28, eye_y] = c_eclipse_core; pix[29, eye_y] = c_eclipse_mid; pix[28, eye_y - 1] = c_eclipse_out
    pix[34, eye_y] = c_eclipse_mid; pix[35, eye_y] = c_eclipse_core; pix[35, eye_y - 1] = c_eclipse_out

    # Colossal Stone Carapace / Torso Mantle
    for y in range(28 + hover, 50 + hover):
        t = (y - (28 + hover)) / 22.0
        half_w = 11.0 + t * 4.0 if y < 40 + hover else 15.0 - (y - (40 + hover)) * 0.8
        x_min = int(32 - half_w)
        x_max = int(32 + half_w)
        for x in range(x_min, x_max + 1):
            if x == x_min or x == x_max or y == 49 + hover:
                pix[x, y] = c_out
            else:
                dist = x - x_min
                if dist < 4:
                    pix[x, y] = c_hi if y < 38 + hover else c_mid
                elif dist < half_w:
                    pix[x, y] = c_mid if y < 38 + hover else c_dark
                else:
                    pix[x, y] = c_dark

    # The Eclipse Sun Core in Chest Cavity (Pulsing glowing orb)
    core_cx = 32
    core_cy = 38 + hover
    core_radius = 4.2 + flare * 0.6
    for y in range(int(core_cy - core_radius - 1), int(core_cy + core_radius + 2)):
        for x in range(int(core_cx - core_radius - 1), int(core_cx + core_radius + 2)):
            d2 = (x - core_cx)**2 + (y - core_cy)**2
            if d2 <= core_radius**2:
                if d2 <= 2.2:
                    pix[x, y] = c_eclipse_white
                elif d2 <= 7.0:
                    pix[x, y] = c_eclipse_core
                elif d2 <= 14.0:
                    pix[x, y] = c_eclipse_mid
                else:
                    pix[x, y] = c_eclipse_out

    # Massive Shadowy Arms & Razor Claws
    # Left Arm
    lax = 13 + arm_spread
    for y in range(30 + hover, 48 + hover):
        for x in range(lax, lax + 6):
            if x == lax or x == lax + 5 or y == 30 + hover:
                pix[x, y] = c_out
            else:
                pix[x, y] = c_hi if x == lax + 1 else c_mid
    # Left Claws
    for cy in range(48 + hover, 53 + hover):
        pix[lax + 1, cy] = c_bone; pix[lax + 3, cy] = c_bone_hi; pix[lax + 5, cy] = c_bone

    # Right Arm
    rax = 45 - arm_spread
    for y in range(30 + hover, 48 + hover):
        for x in range(rax, rax + 6):
            if x == rax or x == rax + 5 or y == 30 + hover:
                pix[x, y] = c_out
            else:
                pix[x, y] = c_mid if x == rax + 1 else c_dark
    # Right Claws
    for cy in range(48 + hover, 53 + hover):
        pix[rax, cy] = c_bone; pix[rax + 2, cy] = c_bone; pix[rax + 4, cy] = c_bone_hi

    return img

# -------------------------------------------------------------
# Main Assembly & File Exporter
# -------------------------------------------------------------
def build_sheets():
    os.makedirs("My project/Assets/Resources/Ember/Props", exist_ok=True)
    os.makedirs("docs", exist_ok=True)
    
    enemies = [
        ("enemy_shade.png", draw_shade, 32, 32),
        ("enemy_swamp.png", draw_swamp, 32, 32),
        ("enemy_void.png", draw_void_stalker, 32, 32),
        ("enemy_sentinel.png", draw_sentinel, 32, 32),
        ("boss.png", draw_boss, 64, 64)
    ]
    
    generated_frames = {}

    for fname, draw_fn, fw, fh in enemies:
        sheet = make_rgba(fw * 4, fh)
        frames_list = []
        for f in range(4):
            frame_img = draw_fn(f)
            frames_list.append(frame_img)
            sheet.paste(frame_img, (f * fw, 0))
        target_path = os.path.join("My project/Assets/Resources/Ember/Props", fname)
        sheet.save(target_path)
        print(f"Saved sprite sheet {target_path} ({sheet.size[0]}x{sheet.size[1]})")
        generated_frames[fname] = frames_list

    # ---------------------------------------------------------
    # Generate Showcase Preview Image (High-res 4x pixel zoom)
    # ---------------------------------------------------------
    pw, ph = 860, 520
    preview = Image.new("RGBA", (pw, ph), (15, 12, 24, 255))
    draw = ImageDraw.Draw(preview)

    # Stylized Header
    draw.rectangle([(0, 0), (pw, 44)], fill=(24, 18, 38, 255))
    draw.line([(0, 44), (pw, 44)], fill=(75, 55, 110, 255), width=2)
    
    # Try default font
    try:
        font_title = ImageFont.truetype("arial.ttf", 20)
        font_name = ImageFont.truetype("arialbd.ttf", 15)
        font_sub = ImageFont.truetype("arial.ttf", 12)
    except:
        font_title = font_name = font_sub = ImageFont.load_default()

    draw.text((20, 12), "EMBER · BỘ NGOẠI HÌNH QUÁI VẬT MỚI (4-FRAME ANIMATION)", fill=(255, 230, 180, 255), font=font_title)

    rows = [
        ("Shade (Bóng Ma Rừng Rậm)", "enemy_shade.png", 32, 32, "Bóng ma áo choàng rách, móng vuốt bóng đêm, mắt đỏ rực."),
        ("Swamp Leech (Đỉa Đầm Lầy)", "enemy_swamp.png", 32, 32, "Thực thể đầm lầy phát quang sinh học, miệng gai tiết nọc độc."),
        ("Void Stalker (Thú Săn Hư Không)", "enemy_void.png", 32, 32, "Dã thú 4 chân sừng cong vực thẳm, đuôi roi sắc, mắt tím ma mị."),
        ("Sentinel (Hộ Vệ Cổ Ngữ - Elite)", "enemy_sentinel.png", 32, 32, "Cự thạch giáp hắc thiết cổ, vai lơ lửng, mắt đỏ và lõi nứt rực lửa."),
        ("Eclipse Boss (Đại Thực Thể Nhật Thực)", "boss.png", 64, 64, "Boss khổng lồ 64px, vương miện sừng, lõi nhật thực bốc lửa.")
    ]

    y_cursor = 58
    zoom = 3

    for title, fname, fw, fh, desc in rows:
        # Card background
        card_h = 80 if fh == 32 else 96
        draw.rounded_rectangle([(16, y_cursor), (pw - 16, y_cursor + card_h)], radius=6, fill=(24, 20, 36, 255), outline=(52, 42, 74, 255))
        
        # Labels
        draw.text((28, y_cursor + 8), title, fill=(245, 215, 160, 255), font=font_name)
        draw.text((28, y_cursor + 28), desc, fill=(170, 160, 195, 255), font=font_sub)
        draw.text((28, y_cursor + 48), f"Sprite: {fw}x{fh}px · 4 Frame Animation Strip", fill=(115, 105, 140, 255), font=font_sub)

        # Draw the 4 frames zoomed
        frames = generated_frames[fname]
        x_frame = 430
        for fi, fimg in enumerate(frames):
            frame_zoom = 2 if fh == 64 else zoom
            scaled = fimg.resize((fw * frame_zoom, fh * frame_zoom), Image.NEAREST)
            fy = y_cursor + (card_h - fh * frame_zoom) // 2
            # Subtle frame slot box
            draw.rectangle([(x_frame - 2, fy - 2), (x_frame + fw * frame_zoom + 1, fy + fh * frame_zoom + 1)], outline=(65, 50, 90, 255), fill=(12, 10, 18, 255))
            preview.paste(scaled, (x_frame, fy), scaled)
            x_frame += fw * frame_zoom + 14

        y_cursor += card_h + 10

    # Save preview image to multiple accessible paths
    preview_path_docs = "docs/enemies_preview.png"
    preview.save(preview_path_docs)
    
    artifact_dir = "C:/Users/OS/.gemini/antigravity/brain/dc6023b7-ad69-4d5b-a5d0-20427d5efb77"
    if os.path.exists(artifact_dir):
        artifact_preview = os.path.join(artifact_dir, "enemies_preview.png")
        preview.save(artifact_preview)
        print(f"Saved artifact preview to {artifact_preview}")

    # Also build an animated GIF preview
    gif_frames = []
    for fi in range(4):
        g_img = Image.new("RGBA", (340, 96), (18, 14, 28, 255))
        gx = 12
        for _, fname, fw, fh, _ in rows:
            fimg = generated_frames[fname]
            frame_cur = fimg[fi]
            # scale all to height 64
            scale = 1 if fh == 64 else 2
            scaled = frame_cur.resize((fw * scale, fh * scale), Image.NEAREST)
            gy = (96 - fh * scale) // 2
            g_img.paste(scaled, (gx, gy), scaled)
            gx += fw * scale + 10
        gif_frames.append(g_img)

    gif_path = "docs/enemies_animated.gif"
    gif_frames[0].save(gif_path, save_all=True, append_images=gif_frames[1:], duration=220, loop=0)
    if os.path.exists(artifact_dir):
        gif_frames[0].save(os.path.join(artifact_dir, "enemies_animated.gif"), save_all=True, append_images=gif_frames[1:], duration=220, loop=0)
    print("Done building all sprites and previews!")

if __name__ == "__main__":
    build_sheets()
