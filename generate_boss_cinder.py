import os
from PIL import Image

def build_boss_cinder():
    W, H = 256, 64
    FRAME_W = 64
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    px = img.load()

    # Master Color Palette (100% Opaque, No Anti-Aliasing)
    C_OUT = (18, 9, 13, 255)         # #12090D Deep Obsidian Outline
    C_BASALT_0 = (30, 18, 24, 255)   # #1E1218 Deep Shadow
    C_BASALT_1 = (46, 27, 36, 255)   # #2E1B24 Shadow
    C_BASALT_2 = (68, 41, 54, 255)   # #442936 Base Basalt
    C_BASALT_3 = (98, 61, 78, 255)   # #623D4E Highlight Basalt
    C_BASALT_4 = (134, 87, 108, 255) # #86576C Edge Highlight

    C_LAVA_DEEP = (127, 29, 29, 255) # #7F1D1D Crust Magma
    C_LAVA_DARK = (185, 28, 28, 255) # #B91C1C Dark Lava
    C_LAVA_MID = (234, 88, 12, 255)  # #EA580C Molten Orange
    C_LAVA_BRIGHT = (250, 140, 20, 255) # #FA8C14 Glowing Flame
    C_LAVA_HOT = (255, 215, 40, 255) # #FFD728 Radiant Gold Fissure
    C_LAVA_CORE = (255, 255, 220, 255) # #FFFFDC White-Hot Hearth Core

    def set_px(fx, x, y, col):
        if 0 <= x < FRAME_W and 0 <= y < H:
            px[fx * FRAME_W + x, y] = col

    def set_sym(fx, cx, cy, col):
        set_px(fx, cx, cy, col)
        set_px(fx, 63 - cx, cy, col)

    for f in range(4):
        # Frame 0: Idle/Resting (lift=0)
        # Frame 1: Left Step Lift (lift=-1, left arm back, right arm forward, hearth pulse)
        # Frame 2: Landing (lift=0)
        # Frame 3: Right Step Lift (lift=-1, right arm back, left arm forward, hearth pulse)
        lift_y = -1 if f in (1, 3) else 0
        is_pulse = f in (1, 3)

        # --- 1. HORNS (y: 4..16 + lift_y) ---
        horn_curve = [
            (11, 4, 3), (12, 5, 4), (13, 6, 5), (14, 7, 5),
            (15, 8, 6), (16, 9, 6), (17, 10, 7), (18, 11, 7),
            (19, 12, 8), (20, 13, 8), (21, 14, 8), (22, 15, 8), (23, 16, 8)
        ]
        for hx, hy, hw in horn_curve:
            actual_y = hy + lift_y
            for w_off in range(hw):
                cur_x = hx + w_off
                if w_off == 0 or w_off == hw - 1:
                    set_sym(f, cur_x, actual_y, C_OUT)
                elif w_off == 1:
                    set_sym(f, cur_x, actual_y, C_BASALT_3)
                elif w_off == 2:
                    set_sym(f, cur_x, actual_y, C_BASALT_2)
                else:
                    set_sym(f, cur_x, actual_y, C_BASALT_1)

        # Magma Veins through horns (glowing pulse on step frames)
        for hx, hy, _ in horn_curve[2:10]:
            actual_y = hy + lift_y
            set_sym(f, hx + 2, actual_y, C_LAVA_HOT if is_pulse else C_LAVA_BRIGHT)
            set_sym(f, hx + 3, actual_y, C_LAVA_BRIGHT if is_pulse else C_LAVA_MID)

        # --- 2. DEMON VISAGE & BROW (y: 16..24 + lift_y) ---
        for vy in range(16, 25):
            actual_y = vy + lift_y
            half_w = 6 + (vy - 16)
            for x in range(32 - half_w, 32):
                if x == 32 - half_w:
                    set_sym(f, x, actual_y, C_OUT)
                elif x == 32 - half_w + 1:
                    set_sym(f, x, actual_y, C_BASALT_3)
                elif x < 28:
                    set_sym(f, x, actual_y, C_BASALT_2)
                else:
                    set_sym(f, x, actual_y, C_BASALT_1)

        # Eyes at y = 20..21 + lift_y, x = 25..28
        ey1 = 20 + lift_y
        ey2 = 21 + lift_y
        set_sym(f, 25, ey1, C_LAVA_MID)
        set_sym(f, 26, ey1, C_LAVA_HOT)
        set_sym(f, 27, ey1, C_LAVA_CORE)
        set_sym(f, 28, ey1, C_LAVA_BRIGHT)

        set_sym(f, 26, ey2, C_LAVA_BRIGHT)
        set_sym(f, 27, ey2, C_LAVA_HOT)

        # Brow crack
        set_sym(f, 30, 18 + lift_y, C_LAVA_HOT)
        set_sym(f, 31, 18 + lift_y, C_LAVA_CORE)
        set_sym(f, 29, 19 + lift_y, C_LAVA_BRIGHT)

        # --- 3. MASSIVE SHOULDERS & PAULDRONS (y: 20..36 + lift_y) ---
        for py in range(20, 37):
            actual_y = py + lift_y
            p_w = int(12 + (py - 20) * 0.95) if py <= 28 else int(20 - (py - 28) * 0.6)
            x_min = max(6, 31 - p_w)
            for x in range(x_min, 32):
                if x == x_min or py in (20, 36):
                    set_sym(f, x, actual_y, C_OUT)
                elif x <= x_min + 2:
                    set_sym(f, x, actual_y, C_BASALT_3)
                elif x <= x_min + 5:
                    set_sym(f, x, actual_y, C_BASALT_2)
                else:
                    set_sym(f, x, actual_y, C_BASALT_1)

        # Spikes on pauldrons
        for spike_i in range(3):
            sx = 7 + spike_i * 4
            sy = 22 + spike_i * 3 + lift_y
            set_sym(f, sx, sy, C_BASALT_4)
            set_sym(f, sx + 1, sy, C_BASALT_3)
            set_sym(f, sx + 1, sy + 1, C_LAVA_BRIGHT)

        # Shoulder magma fissures
        for fy_off in range(5):
            set_sym(f, 15 + fy_off, 24 + fy_off + lift_y, C_LAVA_HOT if is_pulse else C_LAVA_BRIGHT)
            set_sym(f, 16 + fy_off, 24 + fy_off + lift_y, C_LAVA_MID)

        # --- 4. CHEST CARAPACE & CORE FURNACE (y: 25..46 + lift_y) ---
        for cy in range(25, 47):
            actual_y = cy + lift_y
            taper = 0 if cy < 36 else (cy - 36) * 0.8
            half_w = int(14 - taper)
            for x in range(32 - half_w, 32):
                if x == 32 - half_w or cy == 46:
                    set_sym(f, x, actual_y, C_OUT)
                elif x == 32 - half_w + 1:
                    set_sym(f, x, actual_y, C_BASALT_3)
                elif x < 26:
                    set_sym(f, x, actual_y, C_BASALT_2)
                else:
                    set_sym(f, x, actual_y, C_BASALT_1)

        # HEARTH FURNACE (Center at x=31..32, y=32..38 + lift_y)
        # Pulses expanding with brilliant yellow-white core on step frames (f=1, 3)
        pulse_r = 4 if is_pulse else 3
        core_cy = 35 + lift_y
        for dy in range(-pulse_r, pulse_r + 1):
            for dx in range(-pulse_r, pulse_r + 1):
                dist = (dx * dx + dy * dy) ** 0.5
                tx = 31 + dx
                ty = core_cy + dy
                if dist <= (1.6 if is_pulse else 1.1):
                    set_px(f, tx, ty, C_LAVA_CORE)
                elif dist <= (2.6 if is_pulse else 2.1):
                    set_px(f, tx, ty, C_LAVA_HOT)
                elif dist <= (3.5 if is_pulse else 3.0):
                    set_px(f, tx, ty, C_LAVA_BRIGHT)
                elif dist <= pulse_r:
                    set_px(f, tx, ty, C_LAVA_MID)
                elif dist <= pulse_r + 0.8:
                    set_px(f, tx, ty, C_LAVA_DARK)

        # Radiating magma cracks
        set_sym(f, 25, 33 + lift_y, C_LAVA_BRIGHT)
        set_sym(f, 24, 34 + lift_y, C_LAVA_HOT)
        set_sym(f, 23, 35 + lift_y, C_LAVA_MID)
        set_sym(f, 26, 38 + lift_y, C_LAVA_HOT)
        set_sym(f, 25, 39 + lift_y, C_LAVA_MID)
        set_sym(f, 28, 41 + lift_y, C_LAVA_BRIGHT)

        # --- 5. ARMS & GAUNTLETS WITH SOLE SWING ANIMATION ---
        # Frame 0: both neutral (arm_y_off = 0)
        # Frame 1: left arm back (y-1), right arm forward (y+1)
        # Frame 2: both neutral (arm_y_off = 0)
        # Frame 3: left arm forward (y+1), right arm back (y-1)
        left_arm_off = -1 if f == 1 else (1 if f == 3 else 0)
        right_arm_off = 1 if f == 1 else (-1 if f == 3 else 0)

        # Draw Left Arm (x: 6..14)
        for ay in range(27, 51):
            cur_y = ay + lift_y + left_arm_off
            arm_w = 7 if ay < 44 else 9
            for x in range(6, 6 + arm_w):
                if x == 6 or x == 5 + arm_w or ay == 50:
                    set_px(f, x, cur_y, C_OUT)
                elif x == 7:
                    set_px(f, x, cur_y, C_BASALT_3)
                elif x <= 9:
                    set_px(f, x, cur_y, C_BASALT_2)
                else:
                    set_px(f, x, cur_y, C_BASALT_1)
        # Left claws
        claw_y_l = 51 + lift_y + left_arm_off
        for cx in (8, 10, 12):
            set_px(f, cx, claw_y_l, C_BASALT_4)
            set_px(f, cx, claw_y_l + 1, C_OUT)

        # Draw Right Arm (x: 49..57 symmetrically)
        for ay in range(27, 51):
            cur_y = ay + lift_y + right_arm_off
            arm_w = 7 if ay < 44 else 9
            for rx in range(6, 6 + arm_w):
                mirr_x = 63 - rx
                if rx == 6 or rx == 5 + arm_w or ay == 50:
                    set_px(f, mirr_x, cur_y, C_OUT)
                elif rx == 7:
                    set_px(f, mirr_x, cur_y, C_BASALT_3)
                elif rx <= 9:
                    set_px(f, mirr_x, cur_y, C_BASALT_2)
                else:
                    set_px(f, mirr_x, cur_y, C_BASALT_1)
        # Right claws
        claw_y_r = 51 + lift_y + right_arm_off
        for cx in (8, 10, 12):
            mirr_cx = 63 - cx
            set_px(f, mirr_cx, claw_y_r, C_BASALT_4)
            set_px(f, mirr_cx, claw_y_r + 1, C_OUT)

        # --- 6. PELVIS & GROIN GAP (y: 46..49) ---
        for py in range(46, 50):
            for x in range(20, 28):
                if x == 20 or x == 27 or py == 49:
                    set_sym(f, x, py, C_OUT)
                elif x < 24:
                    set_sym(f, x, py, C_BASALT_2)
                else:
                    set_sym(f, x, py, C_BASALT_1)

        # --- 7. TWO SEPARATED BASALT LEGS (y: 50..56) ---
        # Central gap x=28..36 strictly alpha=0! Rows 57..63 strictly alpha=0!
        if f == 0:
            # Frame 0: Resting stance, both legs planted firmly
            for ly in range(50, 57):
                for lx in range(18, 27):
                    if lx == 18 or lx == 26 or ly == 56:
                        set_px(f, lx, ly, C_OUT)
                    elif lx <= 20:
                        set_px(f, lx, ly, C_BASALT_3)
                    elif lx <= 23:
                        set_px(f, lx, ly, C_BASALT_2)
                    else:
                        set_px(f, lx, ly, C_BASALT_1)
            set_px(f, 21, 52, C_LAVA_BRIGHT)
            set_px(f, 22, 52, C_LAVA_HOT)

            for ry in range(50, 57):
                for rx in range(37, 46):
                    if rx == 37 or rx == 45 or ry == 56:
                        set_px(f, rx, ry, C_OUT)
                    elif rx <= 39:
                        set_px(f, rx, ry, C_BASALT_3)
                    elif rx <= 42:
                        set_px(f, rx, ry, C_BASALT_2)
                    else:
                        set_px(f, rx, ry, C_BASALT_1)
            set_px(f, 41, 52, C_LAVA_BRIGHT)
            set_px(f, 42, 52, C_LAVA_HOT)

        elif f == 1:
            # Frame 1: Left Step Lift (Left leg advancing/lifted, right leg supporting)
            # Left leg lifted (knee at y=51, boot at y=53..54; y=55..56 empty air)
            for ly in range(50, 55):
                for lx in range(18, 27):
                    if lx == 18 or lx == 26 or ly == 54:
                        set_px(f, lx, ly, C_OUT)
                    elif lx <= 20:
                        set_px(f, lx, ly, C_BASALT_3)
                    else:
                        set_px(f, lx, ly, C_BASALT_2)
            set_px(f, 21, 52, C_LAVA_BRIGHT)
            set_px(f, 22, 52, C_LAVA_HOT)

            # Right leg planted supporting pillar
            for ry in range(50, 57):
                for rx in range(37, 46):
                    if rx == 37 or rx == 45 or ry == 56:
                        set_px(f, rx, ry, C_OUT)
                    elif rx <= 39:
                        set_px(f, rx, ry, C_BASALT_3)
                    elif rx <= 42:
                        set_px(f, rx, ry, C_BASALT_2)
                    else:
                        set_px(f, rx, ry, C_BASALT_1)
            set_px(f, 41, 53, C_LAVA_HOT)

        elif f == 2:
            # Frame 2: Landing stance (legs firmly on ground)
            for ly in range(50, 57):
                for lx in range(18, 27):
                    if lx == 18 or lx == 26 or ly == 56:
                        set_px(f, lx, ly, C_OUT)
                    elif lx <= 20:
                        set_px(f, lx, ly, C_BASALT_3)
                    elif lx <= 23:
                        set_px(f, lx, ly, C_BASALT_2)
                    else:
                        set_px(f, lx, ly, C_BASALT_1)
            set_px(f, 21, 53, C_LAVA_HOT)

            for ry in range(50, 57):
                for rx in range(37, 46):
                    if rx == 37 or rx == 45 or ry == 56:
                        set_px(f, rx, ry, C_OUT)
                    elif rx <= 39:
                        set_px(f, rx, ry, C_BASALT_3)
                    elif rx <= 42:
                        set_px(f, rx, ry, C_BASALT_2)
                    else:
                        set_px(f, rx, ry, C_BASALT_1)
            set_px(f, 41, 53, C_LAVA_HOT)

        elif f == 3:
            # Frame 3: Right Step Lift (Right leg advancing/lifted, left leg supporting)
            # Left leg planted supporting pillar
            for ly in range(50, 57):
                for lx in range(18, 27):
                    if lx == 18 or lx == 26 or ly == 56:
                        set_px(f, lx, ly, C_OUT)
                    elif lx <= 20:
                        set_px(f, lx, ly, C_BASALT_3)
                    elif lx <= 23:
                        set_px(f, lx, ly, C_BASALT_2)
                    else:
                        set_px(f, lx, ly, C_BASALT_1)
            set_px(f, 21, 53, C_LAVA_HOT)

            # Right leg lifted (knee at y=51, boot at y=53..54; y=55..56 empty air)
            for ry in range(50, 55):
                for rx in range(37, 46):
                    if rx == 37 or rx == 45 or ry == 54:
                        set_px(f, rx, ry, C_OUT)
                    elif rx <= 39:
                        set_px(f, rx, ry, C_BASALT_3)
                    else:
                        set_px(f, rx, ry, C_BASALT_2)
            set_px(f, 41, 52, C_LAVA_BRIGHT)
            set_px(f, 42, 52, C_LAVA_HOT)

        # STRICT ROW TRANSPARENCY: rows 57..63 are (0, 0, 0, 0)
        for sy in range(57, 64):
            for sx in range(FRAME_W):
                px[f * FRAME_W + sx, sy] = (0, 0, 0, 0)

    return img

if __name__ == "__main__":
    cinder = build_boss_cinder()
    targets = [
        "ArtShowcase/raw/boss_cinder.png",
        "My project/Assets/Resources/Ember/Props/boss_cinder.png",
        "Builds/UiVerificationProject/Assets/Resources/Ember/Props/boss_cinder.png",
        "ArtShowcase/mobs_merchant/boss_cinder.png",
    ]
    for t in targets:
        os.makedirs(os.path.dirname(t), exist_ok=True)
        cinder.save(t)
        print(f"Saved: {t}")

    # Zoomed 8x for showcase
    zoom_path = "ArtShowcase/zoomed_8x/boss_cinder.png"
    if os.path.exists(os.path.dirname(zoom_path)):
        zoomed = cinder.resize((256 * 8, 64 * 8), Image.NEAREST)
        zoomed.save(zoom_path)
        print(f"Saved: {zoom_path}")
