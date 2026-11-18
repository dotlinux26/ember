import math
import os
from PIL import Image, ImageDraw

def make_rgba(w, h):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))

# ============================================================================
# 1. SACRED LAKE SANCTUARY (64x64) - REPLACES PILLAR WITH SACRED POND
# ============================================================================
def draw_lake_sanctuary(lit=False):
    # Circular sacred pond surrounded by mossy flagstone rim
    img = make_rgba(64, 64)
    pix = img.load()

    # Stone rim palette
    c_out = (12, 10, 18, 255)
    c_stone_shadow = (24, 20, 34, 255)
    c_stone_dark = (42, 36, 56, 255)
    c_stone_mid = (68, 60, 88, 255)
    c_stone_hi = (102, 92, 128, 255)
    c_moss = (48, 88, 52, 255)
    c_grass = (34, 68, 42, 255)

    # Water palette (Deep sapphire to turquoise rim)
    c_sand_rim = (120, 108, 92, 255)
    c_water_shallow = (45, 125, 165, 255)
    c_water_mid = (30, 88, 145, 255)
    c_water_deep = (18, 54, 115, 255)
    c_water_abyss = (12, 36, 85, 255)
    c_ripple = (145, 225, 255, 255) if lit else (75, 150, 195, 255)
    c_caustic = (215, 250, 255, 255) if lit else (110, 185, 220, 255)

    # Submerged ancient runic circle in the pool
    cx, cy = 31.5, 31.5

    # 1. Outer Flagstone Rim: R from 24 to 30
    for y in range(64):
        for x in range(64):
            dx = (x - cx)
            dy = (y - cy) * 1.15 # Isometric top-down squash
            d = math.sqrt(dx * dx + dy * dy)

            # Outside pond
            if d > 29.5:
                continue

            # Flagstone curb / stone rim (d: 22..29)
            if d >= 22.0:
                if d >= 28.5:
                    pix[x, y] = c_out
                else:
                    # Segment stones with mortar joints
                    angle = math.atan2(dy, dx)
                    stone_id = int((angle + math.pi) / (math.pi / 7.0))
                    is_joint = abs((angle + math.pi) % (math.pi / 7.0)) < 0.08
                    if is_joint:
                        pix[x, y] = c_stone_shadow
                    elif d > 27.0:
                        pix[x, y] = c_stone_dark
                    elif d > 24.5:
                        # Top-left gets highlight, bottom-right gets shadow
                        light = -dx * 0.6 - dy * 0.7
                        if light > 4: pix[x, y] = c_stone_hi
                        elif light > 0: pix[x, y] = c_stone_mid
                        else: pix[x, y] = c_stone_dark
                    else:
                        # Inner shore edge
                        pix[x, y] = c_moss if (x + y) % 3 == 0 else c_stone_shadow
            # Inside the Lake Basin (d < 22.0)
            else:
                # Water depth zones
                if d >= 18.5:
                    pix[x, y] = c_water_shallow
                elif d >= 13.5:
                    pix[x, y] = c_water_mid
                elif d >= 7.5:
                    pix[x, y] = c_water_deep
                else:
                    pix[x, y] = c_water_abyss

                # Submerged ancient stepping stone / pedestal in center
                if 2.5 <= d <= 6.5:
                    if (x * 7 + y * 13) % 5 == 0:
                        pix[x, y] = (25, 45, 75, 255)

    # 2. Concentric Wave Ripples radiating from center
    ripples = [
        {"r": 5.5, "w": 0.8},
        {"r": 11.0, "w": 0.8},
        {"r": 16.5, "w": 0.8}
    ]
    for rip in ripples:
        r0 = rip["r"]
        for deg in range(0, 360, 6):
            rad = math.radians(deg)
            # Wobble wave
            wobble = math.sin(deg * 4) * 0.4
            rx = int(cx + (r0 + wobble) * math.cos(rad))
            ry = int(cy + (r0 + wobble) * math.sin(rad) * 0.85)
            if 0 <= rx < 64 and 0 <= ry < 64 and pix[rx, ry][3] != 0:
                # Draw subtle crescent highlight on top-left of each ripple
                if 120 <= deg <= 300:
                    pix[rx, ry] = c_caustic if deg in (180, 210, 240) else c_ripple

    # 3. 4 Stepping stones at edge of the lake (North, South, East, West)
    steps = [(32, 7), (32, 54), (7, 32), (54, 32)]
    for sx, sy in steps:
        for ox in range(-2, 3):
            for oy in range(-2, 3):
                if ox * ox + oy * oy <= 4:
                    px, py = sx + ox, sy + oy
                    if 0 <= px < 64 and 0 <= py < 64:
                        if ox * ox + oy * oy == 4: pix[px, py] = c_out
                        elif ox < 0 and oy < 0: pix[px, py] = c_stone_hi
                        else: pix[px, py] = c_stone_mid

    return img

# ============================================================================
# 2. THE GREAT FLOATING CRYSTAL (18x30) - MONOLITH IN CENTER OF THE LAKE
# ============================================================================
def draw_lake_crystal(lit=False):
    # Majestic octahedral floating crystal with sharp facets
    img = make_rgba(18, 30)
    pix = img.load()
    cx = 8.5

    if not lit:
        # TẮT NGỦM (Unlit / Dormant) - Dark obsidian stone, subtle slate blue rim
        c_out = (8, 6, 12, 255)
        c_shadow = (18, 16, 26, 255)
        c_dark = (32, 30, 46, 255)
        c_mid = (48, 45, 68, 255)
        c_hi = (72, 68, 98, 255)
        c_rim = (102, 98, 138, 255)
    else:
        # BỪNG SÁNG (Awakened / Lit) - Blazing Cyan Ember Core & Aura
        c_out = (6, 38, 54, 255)
        c_shadow = (12, 85, 118, 255)
        c_dark = (22, 142, 185, 255)
        c_mid = (45, 210, 245, 255)
        c_hi = (165, 245, 255, 255)
        c_rim = (255, 255, 255, 255)

    for y in range(1, 29):
        t = (y - 1) / 27.0
        # Octahedron profile (widest at y=13..15, narrows to points at top/bottom)
        if t < 0.5:
            w = 1.0 + (t / 0.5) ** 0.85 * 6.8
        else:
            w = 1.0 + ((1.0 - t) / 0.5) ** 1.1 * 6.8

        min_x = int(cx - w)
        max_x = int(cx + w)
        for x in range(min_x, max_x + 1):
            if x == min_x or x == max_x or y == 1 or y == 28:
                pix[x, y] = c_out
            elif x < 8:
                # West facet: receives prime illumination
                if x == min_x + 1:
                    pix[x, y] = c_rim
                elif x < 6:
                    pix[x, y] = c_hi
                else:
                    pix[x, y] = c_mid
            elif x == 8:
                # Central ridge line
                pix[x, y] = c_rim if lit else c_hi
            elif x < 13:
                # East-inner facet
                pix[x, y] = c_mid if lit else c_dark
            else:
                # East-outer facet (shadow)
                pix[x, y] = c_dark if lit else c_shadow

    # If lit: radiating core ember light
    if lit:
        for y in range(10, 18):
            for x in range(6, 11):
                d = math.sqrt((x - 8.5) ** 2 + ((y - 13.5) * 1.2) ** 2)
                if d < 2.0: pix[x, y] = c_rim
                elif d < 3.2: pix[x, y] = c_hi

    return img

def build_assets():
    props_dir = "My project/Assets/Resources/Ember/Props"
    docs_dir = "docs"
    os.makedirs(props_dir, exist_ok=True)
    os.makedirs(docs_dir, exist_ok=True)

    lake_dark = draw_lake_sanctuary(False)
    lake_lit = draw_lake_sanctuary(True)
    crystal_dark = draw_lake_crystal(False)
    crystal_lit = draw_lake_crystal(True)

    # Save to game props
    lake_dark.save(os.path.join(props_dir, "beacon_lake.png"))
    lake_dark.save(os.path.join(props_dir, "beacon_dark.png")) # Dark lake for unlit beacon
    lake_lit.save(os.path.join(props_dir, "beacon_lit.png"))   # Lit lake for active beacon
    lake_lit.save(os.path.join(props_dir, "beacon.png"))       # Standard fallback
    crystal_dark.save(os.path.join(props_dir, "beacon_crystal_dark.png"))
    crystal_lit.save(os.path.join(props_dir, "beacon_crystal_lit.png"))

    print("Exported Lake Sanctuary & Great Monolith Crystals to Props directory!")

    # ------------------------------------------------------------------------
    # Generate Preview Showcase (with Elevated Floating Crystal + Water Reflection)
    # ------------------------------------------------------------------------
    pw, ph = 880, 500
    preview = Image.new("RGBA", (pw, ph), (14, 11, 22, 255))
    draw = ImageDraw.Draw(preview)

    draw.rectangle([(0, 0), (pw, 44)], fill=(24, 18, 36, 255))
    draw.line([(0, 44), (pw, 44)], fill=(80, 60, 120, 255), width=2)
    draw.text((20, 12), "EMBER · HỒ NƯỚC THÁNH ĐỊA VÀ TINH THỂ CÁCH KHÔNG LƠ LỬNG TRÊN MẶT NƯỚC", fill=(255, 230, 180, 255))

    # Left: Dark State (Tắt ngủm)
    draw.rounded_rectangle([(30, 65), (420, 465)], radius=8, fill=(20, 16, 28, 255), outline=(50, 40, 68, 255))
    draw.text((45, 80), "TRẠNG THÁI BÌNH THƯỜNG (TẮT NGỦM - CÁCH KHÔNG LƠ LỬNG)", fill=(240, 210, 160, 255))
    draw.text((45, 100), "Hồ nước xanh thẳm, tinh thể hắc diện tối lơ lửng cách không phía trên mặt hồ.", fill=(160, 150, 175, 255))

    # Composite lake + floating crystal dark
    lake_dark_scaled = lake_dark.resize((240, 240), Image.NEAREST)
    c_dark_scaled = crystal_dark.resize((56, 94), Image.NEAREST)
    shard_dark_scaled = crystal_dark.resize((22, 36), Image.NEAREST)

    lake_comp_dark = lake_dark_scaled.copy()
    ldraw = ImageDraw.Draw(lake_comp_dark)
    # Water shadow directly under the floating crystal
    ldraw.ellipse([(95, 125), (145, 145)], fill=(10, 18, 40, 180), outline=(15, 30, 60, 220))

    # Paste elevated crystal (cách không: floating higher above water)
    cx = (240 - 56) // 2
    cy = (240 - 94) // 2 - 32
    lake_comp_dark.paste(c_dark_scaled, (cx, cy), c_dark_scaled)

    # Paste 3 orbiting satellite shards
    lake_comp_dark.paste(shard_dark_scaled, (cx - 40, cy + 18), shard_dark_scaled)
    lake_comp_dark.paste(shard_dark_scaled, (cx + 62, cy + 12), shard_dark_scaled)
    lake_comp_dark.paste(shard_dark_scaled, (cx + 16, cy + 72), shard_dark_scaled)

    preview.paste(lake_comp_dark, (105, 150), lake_comp_dark)

    # Right: Lit State (Bừng sáng)
    draw.rounded_rectangle([(460, 65), (850, 465)], radius=8, fill=(20, 16, 28, 255), outline=(50, 40, 68, 255))
    draw.text((475, 80), "TRẠNG THÁI KÍCH HOẠT (BỪNG SÁNG HÀO QUANG CYAN)", fill=(45, 230, 255, 255))
    draw.text((475, 100), "Lõi lửa cyan bừng cháy, phản chiếu vệt sáng lung linh trên gợn sóng.", fill=(160, 150, 175, 255))

    lake_lit_scaled = lake_lit.resize((240, 240), Image.NEAREST)
    c_lit_scaled = crystal_lit.resize((56, 94), Image.NEAREST)
    shard_lit_scaled = crystal_lit.resize((22, 36), Image.NEAREST)

    lake_comp_lit = lake_lit_scaled.copy()
    lldraw = ImageDraw.Draw(lake_comp_lit)
    # Glowing caustic light reflection on water
    lldraw.ellipse([(85, 122), (155, 148)], fill=(30, 180, 230, 140), outline=(120, 235, 255, 200))

    lake_comp_lit.paste(c_lit_scaled, (cx, cy), c_lit_scaled)
    lake_comp_lit.paste(shard_lit_scaled, (cx - 40, cy + 18), shard_lit_scaled)
    lake_comp_lit.paste(shard_lit_scaled, (cx + 62, cy + 12), shard_lit_scaled)
    lake_comp_lit.paste(shard_lit_scaled, (cx + 16, cy + 72), shard_lit_scaled)

    preview.paste(lake_comp_lit, (535, 150), lake_comp_lit)

    preview.save("docs/lake_sanctuary_showcase.png")
    artifact_dir = "C:/Users/OS/.gemini/antigravity/brain/dc6023b7-ad69-4d5b-a5d0-20427d5efb77"
    if os.path.exists(artifact_dir):
        preview.save(os.path.join(artifact_dir, "lake_sanctuary_showcase.png"))
    print("Saved docs/lake_sanctuary_showcase.png successfully!")

if __name__ == "__main__":
    build_assets()
