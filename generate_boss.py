import os
import sys
import shutil
from PIL import Image

PALETTE = {
    "OUTLINE":      (13, 7, 20, 255),      # #0D0714 Void Abyss Black
    "OBS_DEEP":     (24, 20, 37, 255),     # #181425 Obsidian Deep Base
    "OBS_SLATE":    (38, 43, 68, 255),     # #262B44 Obsidian Slate
    "OBS_MID":      (62, 53, 70, 255),     # #3E3546 Obsidian Midtone
    "OBS_HI":       (90, 105, 136, 255),   # #5A6988 Obsidian Highlight
    "SPECULAR":     (139, 155, 180, 255),  # #8B9BB4 Specular Glint
    
    "RUNE_BED":     (69, 41, 63, 255),     # #45293F Rune Bed Cavity
    "RUNE_DIM":     (118, 66, 138, 255),   # #76428A Dim Inscription
    "RUNE_GLOW":    (168, 85, 247, 255),   # #A855F7 Active Rune Glow
    "RUNE_PEAK":    (216, 180, 254, 255),  # #D8B4FE Peak Rune Flare
    
    "HEART_SING":   (74, 14, 46, 255),     # #4A0E2E Void Singularity Core
    "HEART_MANTLE": (131, 28, 93, 255),    # #831C5D Dark Magenta Mantle
    "HEART_PLASMA": (192, 38, 211, 255),   # #C026D3 Radiant Magenta
    "HEART_FLAME":  (244, 63, 94, 255),    # #F43F5E Blazing Void Flame
    "HEART_WHITE":  (255, 255, 255, 255),  # #FFFFFF White-Hot Core
    
    "EYE_SHADOW":   (127, 29, 29, 255),    # #7F1D1D Eye Cavity Shadow
    "EYE_RUBY":     (220, 38, 38, 255),    # #DC2626 Ruby Outer Iris
    "EYE_BLAZE":    (255, 0, 68, 255),     # #FF0044 Blazing Ruby Center
    "EYE_SPARK":    (254, 202, 202, 255),  # #FECACA Ruby Specular Spark
}

def create_boss_frame(frame_idx):
    grid = [[(0, 0, 0, 0) for _ in range(64)] for _ in range(64)]
    
    def set_p(x, y, col):
        if 0 <= x < 64 and 0 <= y < 64:
            grid[y][x] = PALETTE[col]
            
    def set_sym(x, y, col):
        set_p(x, y, col)
        set_p(63 - x, y, col)

    def draw_h_sym(x1, x2, y, col):
        for x in range(x1, x2 + 1):
            set_sym(x, y, col)

    lift_y = -1 if frame_idx == 2 else 0
    chest_expand = 0 if frame_idx == 0 else (2 if frame_idx == 2 else 1)
    
    # 1. GROUND SHADOW (y = 61..62)
    for x in range(16, 32):
        set_sym(x, 62, "OUTLINE")
    for x in range(18, 32):
        set_sym(x, 61, "OBS_DEEP")
        
    # 2. GREAVES & CLAWED BOOTS (Y = 53..61)
    for y in range(58, 62):
        draw_h_sym(17, 27, y, "OBS_MID")
    for x in range(17, 28):
        set_sym(x, 61, "OUTLINE")
        set_sym(x, 58, "OUTLINE")
    for y in range(58, 62):
        set_sym(17, y, "OUTLINE")
        set_sym(27, y, "OUTLINE")
    for y in range(59, 61):
        set_sym(18, y, "OBS_SLATE")
        set_sym(19, y, "OBS_DEEP")
        set_sym(25, y, "OBS_HI")
        set_sym(26, y, "SPECULAR")
    for y in range(52, 58):
        draw_h_sym(19, 26, y, "OBS_MID")
        set_sym(19, y, "OUTLINE")
        set_sym(26, y, "OUTLINE")
        set_sym(20, y, "OBS_DEEP")
        set_sym(25, y, "OBS_HI")
    set_sym(21, 53, "SPECULAR")
    set_sym(22, 53, "OBS_HI")
    
    # 3. WAIST, BELT & TASSETS (Y = 46..52) - V-TAPER
    for y in range(46, 52):
        t_inset = (y - 46) // 2
        for x in range(21 + t_inset, 32):
            set_sym(x, y, "OBS_MID")
        set_sym(21 + t_inset, y, "OUTLINE")
        set_sym(22 + t_inset, y, "OBS_SLATE")
    for x in range(27, 32):
        set_sym(x, 47, "OBS_SLATE")
        set_sym(x, 48, "OBS_DEEP")
    set_sym(30, 47, "OBS_HI")
    set_sym(31, 47, "SPECULAR")
    set_sym(30, 48, "RUNE_BED")
    set_sym(31, 48, "RUNE_DIM" if frame_idx == 0 else ("RUNE_PEAK" if frame_idx == 2 else "RUNE_GLOW"))

    # 4. TORSO & CHEST CARAPACE (Y = 25..46 + lift_y)
    torso_top = 25 + lift_y
    torso_bot = 46 + lift_y
    
    for y in range(torso_top, torso_bot):
        prog = (y - torso_top) / max(1, (torso_bot - torso_top))
        if prog < 0.4:
            edge = 18 - chest_expand
        elif prog < 0.7:
            edge = 19 - chest_expand
        else:
            edge = 20 - (chest_expand // 2)
            
        for x in range(edge, 32):
            set_sym(x, y, "OBS_MID")
            
        set_sym(edge, y, "OUTLINE")
        set_sym(edge + 1, y, "OBS_DEEP")
        set_sym(edge + 2, y, "OBS_SLATE")
        set_p(63 - (edge + 1), y, "OBS_HI")
        
    for rib_y in [28 + lift_y, 32 + lift_y, 41 + lift_y]:
        if 0 <= rib_y < 64:
            for x in range(20 - chest_expand, 26):
                set_sym(x, rib_y, "OUTLINE")
                set_sym(x, rib_y + 1, "OBS_DEEP")

    # 5. CENTRAL VOID HEART
    hy = 35 + lift_y
    hx = 31
    
    if frame_idx == 0:
        for dy in range(-3, 4):
            for dx in range(-3, 1):
                if abs(dx) + abs(dy) <= 4:
                    set_sym(hx + dx, hy + dy, "HEART_MANTLE")
        for dy in range(-2, 3):
            for dx in range(-2, 1):
                if abs(dx) + abs(dy) <= 2:
                    set_sym(hx + dx, hy + dy, "HEART_PLASMA")
        set_sym(hx, hy, "HEART_WHITE")
        set_sym(hx, hy - 1, "HEART_WHITE")
    elif frame_idx in (1, 3):
        for dy in range(-4, 5):
            for dx in range(-4, 1):
                d = abs(dx) + abs(dy)
                if d <= 5:
                    set_sym(hx + dx, hy + dy, "HEART_SING")
                if d <= 4:
                    set_sym(hx + dx, hy + dy, "HEART_MANTLE")
                if d <= 3:
                    set_sym(hx + dx, hy + dy, "HEART_FLAME")
                if d <= 2:
                    set_sym(hx + dx, hy + dy, "HEART_PLASMA")
        for dy in range(-1, 2):
            for dx in range(-1, 1):
                set_sym(hx + dx, hy + dy, "HEART_WHITE")
    else: # frame_idx == 2
        for dy in range(-5, 6):
            for dx in range(-5, 1):
                d = abs(dx) + abs(dy)
                if d <= 6:
                    set_sym(hx + dx, hy + dy, "HEART_SING")
                if d <= 5:
                    set_sym(hx + dx, hy + dy, "HEART_MANTLE")
                if d <= 4:
                    set_sym(hx + dx, hy + dy, "HEART_FLAME")
                if d <= 3:
                    set_sym(hx + dx, hy + dy, "HEART_PLASMA")
        set_sym(hx - 5, hy, "HEART_FLAME")
        set_sym(hx, hy - 5, "HEART_FLAME")
        set_sym(hx, hy + 5, "HEART_FLAME")
        for dy in range(-2, 2):
            for dx in range(-2, 1):
                set_sym(hx + dx, hy + dy, "HEART_WHITE")

    # 6. ARMS & DEMONIC CLAW GAUNTLETS (Y = 27..52 + lift_y)
    for y in range(27 + lift_y, 47 + lift_y):
        for x in range(6, 17):
            set_sym(x, y, "OBS_MID")
        set_sym(6, y, "OUTLINE")
        set_sym(16, y, "OUTLINE")
        set_sym(7, y, "OBS_DEEP")
        set_sym(8, y, "OBS_SLATE")
        set_p(63 - 7, y, "OBS_HI")
        set_p(63 - 8, y, "SPECULAR")
        
    for y in range(47 + lift_y, 52 + lift_y):
        for x in range(8, 16):
            set_sym(x, y, "OBS_DEEP")
        set_sym(8, y, "OUTLINE")
        set_sym(15, y, "OUTLINE")
    claw_tip_y = 52 + lift_y
    for cx in [9, 12, 14]:
        set_sym(cx, claw_tip_y, "OUTLINE")
        set_sym(cx + 1, claw_tip_y - 1, "SPECULAR")
        set_sym(cx + 1, claw_tip_y, "OBS_HI")

    # 7. SPIKED PAULDRONS & RUNIC GLYPHS (Y = 20..36 + lift_y)
    py_start = 20 + lift_y
    py_end = 36 + lift_y
    for y in range(py_start, py_end):
        p_top = (y - py_start)
        if p_top < 4:
            x_left = 10 - p_top * 2
            x_right = 23
        else:
            x_left = 6
            x_right = 23
        for x in range(max(6, x_left), x_right):
            set_sym(x, y, "OBS_MID")
        set_sym(max(6, x_left), y, "OUTLINE")
        set_sym(x_right - 1, y, "OUTLINE")
        set_sym(max(7, x_left + 1), y, "OBS_DEEP")
        set_p(63 - (max(7, x_left + 1)), y, "OBS_HI")
        
    for s_off in range(4):
        set_sym(6 + s_off, py_start + s_off, "OUTLINE")
        set_sym(7 + s_off, py_start + s_off, "OBS_HI")
        set_sym(8 + s_off, py_start + s_off, "SPECULAR")
    set_sym(6, py_start, "SPECULAR")
    
    rune_col = "RUNE_DIM" if frame_idx == 0 else ("RUNE_PEAK" if frame_idx == 2 else "RUNE_GLOW")
    rune_core = "RUNE_PEAK" if frame_idx == 2 else "RUNE_GLOW"
    
    rune_coords = [
        (13, py_start + 4), (14, py_start + 4), (15, py_start + 4),
        (12, py_start + 5), (15, py_start + 5),
        (12, py_start + 6), (13, py_start + 6), (14, py_start + 6), (15, py_start + 6),
        (14, py_start + 7),
        (13, py_start + 8), (14, py_start + 8), (15, py_start + 8),
        (15, py_start + 9),
        (13, py_start + 10), (14, py_start + 10), (15, py_start + 10),
    ]
    for rx, ry in rune_coords:
        set_sym(rx, ry, rune_col)
    set_sym(14, py_start + 6, rune_core)
    set_sym(14, py_start + 8, rune_core)

    # 8. HEAD, DEMON VISAGE & SLIT RUBY EYES (Y = 16..25 + lift_y)
    head_y = 16 + lift_y
    for y in range(head_y, head_y + 10):
        w_in = 25
        w_out = 32
        for x in range(w_in, w_out):
            set_sym(x, y, "OBS_MID")
        set_sym(w_in, y, "OUTLINE")
        set_sym(w_in + 1, y, "OBS_DEEP")
        set_p(63 - (w_in + 1), y, "OBS_HI")
        
    for x in range(25, 32):
        set_sym(x, head_y + 1, "OUTLINE")
        set_sym(x, head_y + 2, "OBS_DEEP")
    for x in range(28, 32):
        set_sym(x, head_y + 9, "OUTLINE")
        set_sym(x, head_y + 8, "OBS_DEEP")
        
    ey = head_y + 3
    for x in range(25, 29):
        set_sym(x, ey - 1, "EYE_SHADOW")
        set_sym(x, ey + 1, "EYE_SHADOW")
    set_sym(25, ey, "EYE_SHADOW")
    set_sym(29, ey, "EYE_SHADOW")
    
    set_sym(26, ey, "EYE_RUBY")
    set_sym(27, ey, "EYE_BLAZE")
    set_sym(28, ey, "EYE_BLAZE")
    if frame_idx == 2:
        set_sym(27, ey, "EYE_SPARK")

    # 9. SWEEPING JAGGED OBSIDIAN HORNS (Y = 5..16 + lift_y)
    horn_base_y = 15 + lift_y
    horn_poly = [
        (horn_base_y, 23, 27),
        (horn_base_y - 1, 21, 26),
        (horn_base_y - 2, 19, 24),
        (horn_base_y - 3, 17, 23),
        (horn_base_y - 4, 15, 21),
        (horn_base_y - 5, 13, 19),
        (horn_base_y - 6, 11, 18),
        (horn_base_y - 7, 10, 16),
        (horn_base_y - 8, 9, 15),
        (horn_base_y - 9, 10, 14),
        (horn_base_y - 10, 11, 13),
    ]
    
    for hy_rel, x_min, x_max in horn_poly:
        if 0 <= hy_rel < 64:
            for x in range(x_min, x_max + 1):
                set_sym(x, hy_rel, "OBS_MID")
            set_sym(x_min, hy_rel, "OUTLINE")
            set_sym(x_max, hy_rel, "OUTLINE")
            set_sym(x_min + 1, hy_rel, "OBS_DEEP")
            set_sym(x_max - 1, hy_rel, "OBS_HI")
            
    tip_y = horn_base_y - 10
    if 0 <= tip_y < 64:
        set_sym(12, tip_y, "SPECULAR")
        set_sym(12, tip_y - 1, "SPECULAR")
        set_sym(12, tip_y - 2, "OUTLINE")
        set_sym(11, tip_y - 1, "OUTLINE")
        set_sym(13, tip_y - 1, "OUTLINE")

    return grid

def generate_boss_sheet():
    sheet = Image.new("RGBA", (256, 64), (0, 0, 0, 0))
    for f in range(4):
        grid = create_boss_frame(f)
        for y in range(64):
            for x in range(64):
                sheet.putpixel((f * 64 + x, y), grid[y][x])
    return sheet

def sync_boss(sheet):
    # 1. Primary Unity Asset
    p1 = "My project/Assets/Resources/Ember/Props/boss.png"
    os.makedirs(os.path.dirname(p1), exist_ok=True)
    sheet.save(p1)
    
    # 2. ArtShowcase raw
    p2 = "ArtShowcase/raw/boss.png"
    os.makedirs(os.path.dirname(p2), exist_ok=True)
    sheet.save(p2)
    
    # 3. 8x nearest neighbor scaled
    sheet_8x = sheet.resize((sheet.width * 8, sheet.height * 8), Image.Resampling.NEAREST)
    
    p3 = "ArtShowcase/zoomed_8x/boss.png"
    p3b = "ArtShowcase/zoomed_8x/boss_8x.png"
    os.makedirs("ArtShowcase/zoomed_8x", exist_ok=True)
    sheet_8x.save(p3)
    sheet_8x.save(p3b)
    
    # 4. ArtShowcase mobs_merchant
    p4 = "ArtShowcase/mobs_merchant/boss.png"
    p4b = "ArtShowcase/mobs_merchant/boss_8x.png"
    os.makedirs("ArtShowcase/mobs_merchant", exist_ok=True)
    sheet.save(p4)
    sheet_8x.save(p4b)
    
    # 5. Builds/UiVerificationProject
    p5 = "Builds/UiVerificationProject/Assets/Resources/Ember/Props/boss.png"
    if os.path.exists("Builds/UiVerificationProject"):
        os.makedirs(os.path.dirname(p5), exist_ok=True)
        sheet.save(p5)
        print(f"Synced to {p5}")
        
    print("Boss successfully synchronized to all locations!")

if __name__ == "__main__":
    im = generate_boss_sheet()
    sync_boss(im)
