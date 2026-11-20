import os
import math
from PIL import Image

def build_boss_eclipse():
    w, h = 256, 64
    FRAME_W = 64
    sheet = Image.new('RGBA', (w, h), (0, 0, 0, 0))

    # Palette: Deep Indigo Abyss Carapace + Golden-Purple Eclipse Halo + Black Sun Core
    C_OUTLINE  = (10, 6, 20, 255)       # #0A0614 Obsidian Outline
    C_INDIGO_D = (20, 16, 42, 255)      # #14102A Indigo Shadow
    C_INDIGO_M = (38, 30, 72, 255)      # #261E48 Indigo Base
    C_INDIGO_L = (68, 56, 118, 255)     # #443876 Indigo Mid
    C_INDIGO_H = (110, 96, 172, 255)    # #6E60AC Indigo Highlight

    # Golden-Purple Eclipse Halo
    C_HALO_PURPLE_D = (126, 34, 206, 255) # #7E22CE Deep Eclipse Purple
    C_HALO_PURPLE_L = (168, 85, 247, 255) # #A855F7 Neon Purple
    C_HALO_GOLD_D   = (217, 119, 6, 255)  # #D97706 Eclipse Gold Dark
    C_HALO_GOLD_L   = (245, 158, 11, 255) # #F59E0B Eclipse Gold Bright
    C_HALO_CORONA   = (254, 240, 138, 255)# #FEF08A White-Hot Corona

    # Black Sun Core
    C_BLACK_VOID    = (13, 7, 20, 255)    # #0D0714 Obsidian Black Sun
    C_SUN_CORONA    = (253, 224, 71, 255) # #FDE047 Corona Ring
    C_SUN_WHITE     = (255, 255, 240, 255)# #FFFFF0 White-Hot Rim

    # 6 Glowing Golden Eyes
    C_EYE_GOLD_D    = (217, 119, 6, 255)  # #D97706
    C_EYE_GOLD_L    = (245, 158, 11, 255) # #F59E0B
    C_EYE_GOLD_HOT  = (254, 240, 138, 255)# #FEF08A
    C_EYE_WHITE     = (255, 255, 255, 255)# #FFFFFF

    def build_frame(f):
        frame = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
        grid = {}
        y_bob = 1 if f in (1, 3) else 0

        # 1. Golden-Purple Eclipse Crown Halo (rows 4..24)
        halo_r = 13
        for dy in range(-halo_r, halo_r + 1):
            for dx in range(-halo_r, halo_r + 1):
                d = int(round(math.hypot(dx, dy)))
                if d in (10, 11, 12):
                    px_x, px_y = 32 + dx, 16 + dy - y_bob
                    if 0 <= px_x < 64 and 0 <= px_y < 64:
                        if d == 12:
                            grid[(px_x, px_y)] = C_HALO_GOLD_D if (dx + dy) % 2 == 0 else C_HALO_PURPLE_D
                        elif d == 11:
                            grid[(px_x, px_y)] = C_HALO_GOLD_L if (dx + dy) % 2 == 0 else C_HALO_PURPLE_L
                        else:
                            grid[(px_x, px_y)] = C_HALO_CORONA if abs(dx) <= 3 or abs(dy) <= 3 else C_HALO_GOLD_L

        # Radiant Sun Corona Spikes at 8 Cardinal/Ordinal points
        spikes = [(0, -14), (0, 14), (-14, 0), (14, 0), (-10, -10), (10, -10), (-10, 10), (10, 10)]
        for sx, sy in spikes:
            px_x, px_y = 32 + sx, 16 + sy - y_bob
            if 0 <= px_x < 64 and 0 <= px_y < 64:
                grid[(px_x, px_y)] = C_HALO_CORONA

        # 2. Spider Abdomen & Carapace (rows 30..48)
        for y in range(30 - y_bob, 48 - y_bob):
            w_b = 9 + int(math.sin((y - (30 - y_bob)) / 18.0 * math.pi) * 8)
            for x in range(32 - w_b, 32 + w_b):
                dist = abs(x - 31.5)
                grid[(x, y)] = C_INDIGO_M if dist < w_b - 2 else C_INDIGO_D

        # 3. Mysterious Black Sun Core on Abdomen (Centered at x=31.5, y=38 - y_bob)
        # Deep Obsidian Void Ring with White-Hot Corona
        core_cy = 38 - y_bob
        for cdy in range(-3, 4):
            for cdx in range(-3, 4):
                cd = int(round(math.hypot(cdx, cdy)))
                cx = 32 + cdx
                cy = core_cy + cdy
                if cd <= 1:
                    grid[(cx, cy)] = C_BLACK_VOID   # Black Sun Obsidian Singularity
                elif cd == 2:
                    grid[(cx, cy)] = C_SUN_WHITE if (cdx + cdy) % 2 == 0 else C_SUN_CORONA
                elif cd == 3:
                    grid[(cx, cy)] = C_HALO_GOLD_D if (cdx + cdy) % 2 == 0 else C_HALO_PURPLE_D

        # 4. Spider Torso & Indigo Carapace Mantle (rows 20..32)
        for y in range(20 - y_bob, 32 - y_bob):
            w_t = 6 + int((y - (20 - y_bob)) * 0.4)
            for x in range(32 - w_t, 32 + w_t):
                grid[(x, y)] = C_INDIGO_L

        # 5. Head with 6 Glowing Golden Spider Eyes (rows 14..22)
        for y in range(15 - y_bob, 22 - y_bob):
            for x in range(27, 37):
                grid[(x, y)] = C_INDIGO_D

        # 6 Golden Eyes with fiery pupils & specular glints
        grid[(30, 18 - y_bob)] = C_EYE_GOLD_HOT; grid[(33, 18 - y_bob)] = C_EYE_GOLD_HOT
        grid[(30, 19 - y_bob)] = C_EYE_GOLD_D;   grid[(33, 19 - y_bob)] = C_EYE_GOLD_D

        grid[(29, 17 - y_bob)] = C_EYE_WHITE;    grid[(34, 17 - y_bob)] = C_EYE_WHITE
        grid[(29, 18 - y_bob)] = C_EYE_GOLD_L;    grid[(34, 18 - y_bob)] = C_EYE_GOLD_L

        grid[(31, 16 - y_bob)] = C_EYE_GOLD_HOT; grid[(32, 16 - y_bob)] = C_EYE_GOLD_HOT
        grid[(31, 17 - y_bob)] = C_EYE_GOLD_D;   grid[(32, 17 - y_bob)] = C_EYE_GOLD_D

        # 6. ARTICULATED SCYTHE LEGS - 4-Phase Tripod Gait
        # Gait lift flag (L1, L2, L3, R1, R2, R3)
        gait = [
            (0, 1, 0, 1, 0, 1),
            (0, 0, 0, 0, 0, 0),
            (1, 0, 1, 0, 1, 0),
            (0, 0, 0, 0, 0, 0)
        ]
        lifts = gait[f]

        # Left Leg 1 (Front Scythe)
        l1_lift = 4 if lifts[0] == 1 else (1 if f == 1 else 0)
        l1_foot_y = 52 if lifts[0] == 1 else (56 if f == 1 else 57)
        l1_foot_x = 7 if f in (0, 3) else 9
        l1_pts = [(26, 24), (20, 20 - l1_lift), (15, 14 - l1_lift), (11, 11 - l1_lift), (8, 16), (6, 28), (6, 40), (l1_foot_x, l1_foot_y - 2)]
        for lx, ly in l1_pts:
            grid[(lx, ly)] = C_INDIGO_H
            grid[(lx+1, ly)] = C_INDIGO_L
        grid[(l1_foot_x, l1_foot_y - 1)] = C_INDIGO_M
        grid[(l1_foot_x, l1_foot_y)] = C_INDIGO_D

        # Left Leg 2 (Middle Scythe)
        l2_lift = 4 if lifts[1] == 1 else (1 if f == 3 else 0)
        l2_foot_y = 52 if lifts[1] == 1 else (56 if f == 3 else 57)
        l2_foot_x = 12 if f in (1, 2) else 10
        l2_pts = [(25, 28), (18, 26 - l2_lift), (12, 28 - l2_lift), (9, 36), (10, 46), (l2_foot_x, l2_foot_y - 2)]
        for lx, ly in l2_pts:
            grid[(lx, ly)] = C_INDIGO_H
            grid[(lx, ly+1)] = C_INDIGO_M
        grid[(l2_foot_x, l2_foot_y - 1)] = C_INDIGO_M
        grid[(l2_foot_x, l2_foot_y)] = C_INDIGO_D

        # Left Leg 3 (Rear Scythe)
        l3_lift = 4 if lifts[2] == 1 else (1 if f == 1 else 0)
        l3_foot_y = 52 if lifts[2] == 1 else (56 if f == 1 else 57)
        l3_foot_x = 16 if f in (0, 3) else 18
        l3_pts = [(26, 34), (19, 36 - l3_lift), (14, 42 - l3_lift), (13, 49), (l3_foot_x, l3_foot_y - 2)]
        for lx, ly in l3_pts:
            grid[(lx, ly)] = C_INDIGO_L
            grid[(lx+1, ly)] = C_INDIGO_D
        grid[(l3_foot_x, l3_foot_y - 1)] = C_INDIGO_M
        grid[(l3_foot_x, l3_foot_y)] = C_INDIGO_D

        # Right Leg 1 (Front Scythe)
        r1_lift = 4 if lifts[3] == 1 else (1 if f == 3 else 0)
        r1_foot_y = 52 if lifts[3] == 1 else (56 if f == 3 else 57)
        r1_foot_x = 56 if f in (1, 2) else 54
        r1_pts = [(37, 24), (43, 20 - r1_lift), (48, 14 - r1_lift), (52, 11 - r1_lift), (55, 16), (57, 28), (57, 40), (r1_foot_x, r1_foot_y - 2)]
        for rx, ry in r1_pts:
            grid[(rx, ry)] = C_INDIGO_H
            grid[(rx-1, ry)] = C_INDIGO_L
        grid[(r1_foot_x, r1_foot_y - 1)] = C_INDIGO_M
        grid[(r1_foot_x, r1_foot_y)] = C_INDIGO_D

        # Right Leg 2 (Middle Scythe)
        r2_lift = 4 if lifts[4] == 1 else (1 if f == 1 else 0)
        r2_foot_y = 52 if lifts[4] == 1 else (56 if f == 1 else 57)
        r2_foot_x = 51 if f in (0, 3) else 53
        r2_pts = [(38, 28), (45, 26 - r2_lift), (51, 28 - r2_lift), (54, 36), (53, 46), (r2_foot_x, r2_foot_y - 2)]
        for rx, ry in r2_pts:
            grid[(rx, ry)] = C_INDIGO_H
            grid[(rx, ry+1)] = C_INDIGO_M
        grid[(r2_foot_x, r2_foot_y - 1)] = C_INDIGO_M
        grid[(r2_foot_x, r2_foot_y)] = C_INDIGO_D

        # Right Leg 3 (Rear Scythe)
        r3_lift = 4 if lifts[5] == 1 else (1 if f == 3 else 0)
        r3_foot_y = 52 if lifts[5] == 1 else (56 if f == 3 else 57)
        r3_foot_x = 47 if f in (1, 2) else 45
        r3_pts = [(37, 34), (44, 36 - r3_lift), (49, 42 - r3_lift), (50, 49), (r3_foot_x, r3_foot_y - 2)]
        for rx, ry in r3_pts:
            grid[(rx, ry)] = C_INDIGO_L
            grid[(rx-1, ry)] = C_INDIGO_D
        grid[(r3_foot_x, r3_foot_y - 1)] = C_INDIGO_M
        grid[(r3_foot_x, r3_foot_y)] = C_INDIGO_D

        # Write grid to frame
        px_frame = frame.load()
        for (gx, gy), col in grid.items():
            if 0 <= gx < 64 and 0 <= gy < 64:
                px_frame[gx, gy] = col

        # Outer 1px dark contour around opaque pixels
        outline_pixels = []
        for y in range(64):
            for x in range(64):
                if px_frame[x, y][3] == 0:
                    for dy in (-1, 0, 1):
                        for dx in (-1, 0, 1):
                            if dx == 0 and dy == 0:
                                continue
                            nx, ny = x + dx, y + dy
                            if 0 <= nx < 64 and 0 <= ny < 64 and px_frame[nx, ny][3] > 0:
                                outline_pixels.append((x, y))
                                break
        for ox, oy in outline_pixels:
            if oy < 58:  # do not draw outline on rows >= 58
                px_frame[ox, oy] = C_OUTLINE

        # Clear any pixels on rows 58..63 to ensure 100% transparency
        for cy in range(58, 64):
            for cx in range(64):
                px_frame[cx, cy] = (0, 0, 0, 0)

        # Fix any orphan pixels
        for cy in range(58):
            for cx in range(64):
                if px_frame[cx, cy][3] > 0:
                    opaque_neighbors = 0
                    for dy in (-1, 0, 1):
                        for dx in (-1, 0, 1):
                            if dx == 0 and dy == 0:
                                continue
                            nx, ny = cx + dx, cy + dy
                            if 0 <= nx < 64 and 0 <= ny < 58 and px_frame[nx, ny][3] > 0:
                                opaque_neighbors += 1
                    if opaque_neighbors == 0:
                        px_frame[cx, cy] = (0, 0, 0, 0)

        return frame

    for f in range(4):
        frm = build_frame(f)
        sheet.paste(frm, (f * FRAME_W, 0))

    return sheet

if __name__ == "__main__":
    eclipse = build_boss_eclipse()
    targets = [
        "ArtShowcase/raw/boss_eclipse.png",
        "My project/Assets/Resources/Ember/Props/boss_eclipse.png",
        "Builds/UiVerificationProject/Assets/Resources/Ember/Props/boss_eclipse.png",
        "ArtShowcase/mobs_merchant/boss_eclipse.png",
    ]
    for t in targets:
        os.makedirs(os.path.dirname(t), exist_ok=True)
        eclipse.save(t)
        print(f"Saved: {t}")

    # Zoomed 8x for showcase
    zoom_path = "ArtShowcase/zoomed_8x/boss_eclipse.png"
    if os.path.exists(os.path.dirname(zoom_path)):
        zoomed = eclipse.resize((256 * 8, 64 * 8), Image.NEAREST)
        zoomed.save(zoom_path)
        print(f"Saved: {zoom_path}")
