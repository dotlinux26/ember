import os
import shutil
from PIL import Image, ImageDraw, ImageFont

def extract_all():
    temp_dir = "temp_sprout_all"
    props_dir = "My project/Assets/Resources/Ember/Props"
    tiles_dir = "My project/Assets/Resources/Ember/Tilesets"
    docs_dir = "docs"

    os.makedirs(props_dir, exist_ok=True)
    os.makedirs(tiles_dir, exist_ok=True)
    os.makedirs(docs_dir, exist_ok=True)

    # ------------------------------------------------------------------------
    # 1. LOAD RAW SPROUT LANDS SHEETS
    # ------------------------------------------------------------------------
    biom_src = Image.open(os.path.join(temp_dir, "Basic Grass Biom things 1.png")).convert("RGBA")
    tools_src = Image.open(os.path.join(temp_dir, "Basic tools and meterials.png")).convert("RGBA")
    chest_src = Image.open(os.path.join(temp_dir, "Chest.png")).convert("RGBA")
    fences_src = Image.open(os.path.join(temp_dir, "Fences.png")).convert("RGBA")
    grass_src = Image.open(os.path.join(temp_dir, "Grass.png")).convert("RGBA")
    hills_src = Image.open(os.path.join(temp_dir, "Hills.png")).convert("RGBA")
    paths_src = Image.open(os.path.join(temp_dir, "Paths.png")).convert("RGBA")
    water_src = Image.open(os.path.join(temp_dir, "Water.png")).convert("RGBA")
    bridge_src = Image.open(os.path.join(temp_dir, "Wood Bridge.png")).convert("RGBA")

    # ------------------------------------------------------------------------
    # 2. EXTRACT 1:1 PROPS & ITEMS
    # ------------------------------------------------------------------------
    # A. Trees (32x32)
    raw_oak = biom_src.crop((16, 0, 48, 32))
    raw_apple = biom_src.crop((48, 0, 80, 32))
    
    # Apple Tree with Ember glowing apples
    ember_apple = raw_apple.copy()
    pix_a = ember_apple.load()
    for y in range(32):
        for x in range(32):
            r, g, b, a = pix_a[x, y]
            if a > 0 and ((r > 140 and g < 100 and b < 100) or (r > 160 and g < 130 and r > g + 40)):
                pix_a[x, y] = (235, 38, 52, 255)
                if x > 0 and y > 0 and pix_a[x - 1, y - 1][3] > 0:
                    pix_a[x - 1, y - 1] = (255, 185, 195, 255)

    # Autumn Tree (Amber/Crimson foliage shift)
    ember_autumn = raw_oak.copy()
    pix_aut = ember_autumn.load()
    for y in range(32):
        for x in range(32):
            r, g, b, a = pix_aut[x, y]
            if a > 0 and g > r and g > b:
                lum = (r * 0.299 + g * 0.587 + b * 0.114) / 255.0
                if lum > 0.65: pix_aut[x, y] = (245, 180, 50, a)
                elif lum > 0.50: pix_aut[x, y] = (225, 110, 40, a)
                elif lum > 0.35: pix_aut[x, y] = (185, 55, 35, a)
                else: pix_aut[x, y] = (115, 30, 30, a)

    # Void Tree (Eldritch Amethyst foliage shift)
    ember_void = raw_oak.copy()
    pix_v = ember_void.load()
    for y in range(32):
        for x in range(32):
            r, g, b, a = pix_v[x, y]
            if a > 0 and g > r and g > b:
                lum = (r * 0.299 + g * 0.587 + b * 0.114) / 255.0
                if lum > 0.65: pix_v[x, y] = (195, 145, 255, a)
                elif lum > 0.50: pix_v[x, y] = (145, 75, 220, a)
                elif lum > 0.35: pix_v[x, y] = (85, 38, 140, a)
                else: pix_v[x, y] = (38, 18, 68, a)

    # B. Rocks & Stumps (16x16)
    raw_stump = biom_src.crop((80, 32, 96, 48))
    ember_stump = raw_stump.copy()
    pix_s = ember_stump.load()
    if pix_s[3, 10][3] > 0:
        pix_s[3, 10] = (45, 230, 255, 255)
        pix_s[4, 10] = (180, 250, 255, 255)

    raw_rock_large = biom_src.crop((128, 16, 144, 32))
    ember_rock = raw_rock_large.copy()
    pix_r = ember_rock.load()
    for rx, ry in [(8, 6), (8, 7), (9, 8), (9, 9), (10, 10), (10, 11)]:
        if 0 <= rx < 16 and 0 <= ry < 16 and pix_r[rx, ry][3] > 0:
            pix_r[rx, ry] = (45, 230, 255, 255)
            if rx > 0 and pix_r[rx - 1, ry][3] > 0 and pix_r[rx - 1, ry] != (45, 230, 255, 255):
                pix_r[rx - 1, ry] = (18, 95, 135, 255)

    raw_rock_small = biom_src.crop((80, 0, 96, 16))

    # C. Bushes & Flora (16x16)
    raw_bush_round = biom_src.crop((112, 64, 128, 80))
    raw_bush_wide = biom_src.crop((128, 64, 144, 80))
    raw_flowers = biom_src.crop((112, 0, 128, 16))
    ember_flowers = raw_flowers.copy()
    pix_f = ember_flowers.load()
    for y in range(16):
        for x in range(16):
            r, g, b, a = pix_f[x, y]
            if a > 0 and b > 140 and r < 120 and g < 120:
                pix_f[x, y] = (45, 230, 255, 255)

    # D. Tools & Items (from Basic tools and materials) (16x16)
    raw_pickaxe = tools_src.crop((16, 16, 32, 32))
    raw_axe = tools_src.crop((32, 16, 48, 32))
    raw_sword = tools_src.crop((16, 0, 32, 16))
    raw_wood = tools_src.crop((0, 0, 16, 16))
    raw_ore = tools_src.crop((0, 16, 16, 32))

    # E. Wooden Chest (from Chest.png closed frame at (16, 16, 32, 32))
    raw_chest = chest_src.crop((16, 16, 32, 32))
    ember_chest = raw_chest.copy()
    pix_ch = ember_chest.load()
    # Add cyan glowing keyhole rune
    for cx, cy in [(7, 7), (8, 7), (7, 8), (8, 8)]:
        if pix_ch[cx, cy][3] > 0: pix_ch[cx, cy] = (45, 230, 255, 255)

    # F. Wooden Fence / Wall (from Fences.png at (16, 0, 32, 16))
    raw_fence = fences_src.crop((16, 0, 32, 16))

    # ------------------------------------------------------------------------
    # 3. SAVE ALL PROPS TO RESOURCES/EMBER/PROPS/
    # ------------------------------------------------------------------------
    raw_oak.save(os.path.join(props_dir, "tree.png"))
    raw_oak.save(os.path.join(props_dir, "tree_oak.png"))
    ember_apple.save(os.path.join(props_dir, "tree_apple.png"))
    ember_autumn.save(os.path.join(props_dir, "tree_autumn.png"))
    ember_void.save(os.path.join(props_dir, "tree_void.png"))
    ember_stump.save(os.path.join(props_dir, "tree_stump.png"))
    ember_rock.save(os.path.join(props_dir, "rock_large.png"))
    raw_rock_small.save(os.path.join(props_dir, "rock_small.png"))
    raw_bush_round.save(os.path.join(props_dir, "bush_round.png"))
    raw_bush_wide.save(os.path.join(props_dir, "bush_wide.png"))
    ember_flowers.save(os.path.join(props_dir, "wildflowers.png"))

    raw_pickaxe.save(os.path.join(props_dir, "pickaxe.png"))
    raw_axe.save(os.path.join(props_dir, "axe.png"))
    ember_chest.save(os.path.join(props_dir, "chest.png"))
    raw_fence.save(os.path.join(props_dir, "wall.png"))
    raw_wood.save(os.path.join(props_dir, "wood.png"))
    raw_ore.save(os.path.join(props_dir, "ore.png"))

    # ------------------------------------------------------------------------
    # 4. SAVE COMPLETE TILESETS TO RESOURCES/EMBER/TILESETS/
    # ------------------------------------------------------------------------
    grass_src.save(os.path.join(tiles_dir, "grass_tileset.png"))
    hills_src.save(os.path.join(tiles_dir, "hills_tileset.png"))
    paths_src.save(os.path.join(tiles_dir, "paths_tileset.png"))
    water_src.save(os.path.join(tiles_dir, "water_tileset.png"))
    bridge_src.save(os.path.join(tiles_dir, "bridge_tileset.png"))
    fences_src.save(os.path.join(tiles_dir, "fences_tileset.png"))

    print("Successfully extracted ALL Sprout Lands props, items, and tilesets!")

    # ------------------------------------------------------------------------
    # 5. GENERATE MASTER SHOWCASE ARTIFACT
    # ------------------------------------------------------------------------
    pw, ph = 1000, 680
    sheet = Image.new("RGBA", (pw, ph), (15, 12, 22, 255))
    draw = ImageDraw.Draw(sheet)

    draw.rectangle([(0, 0), (pw, 44)], fill=(22, 17, 34, 255))
    draw.line([(0, 44), (pw, 44)], fill=(80, 60, 115, 255), width=2)
    draw.text((20, 12), "EMBER · TOÀN BỘ BỘ TÀI NGUYÊN TRÍCH XUẤT 1:1 TỪ SPROUT LANDS (PROPS, ITEMS & TILESETS)", fill=(255, 230, 180, 255))

    # Section A: Trees & Props
    draw.rounded_rectangle([(20, 56), (980, 220)], radius=8, fill=(20, 16, 28, 255), outline=(50, 40, 68, 255))
    draw.text((32, 66), "1. CÂY CỐI & ĐẠI BỒNG LAI (32×32 & 16×16)", fill=(240, 210, 160, 255))
    tree_items = [
        ("Cây Sồi (tree_oak)", raw_oak),
        ("Cây Táo (tree_apple)", ember_apple),
        ("Cây Thu (tree_autumn)", ember_autumn),
        ("Cây Hư Không (tree_void)", ember_void),
        ("Gốc Cây (tree_stump)", ember_stump),
        ("Đá Lớn (rock_large)", ember_rock),
        ("Bụi Tròn (bush_round)", raw_bush_round),
        ("Hoa Dại (wildflowers)", ember_flowers)
    ]
    for idx, (name, img) in enumerate(tree_items):
        cx = 35 + idx * 118
        cy = 92
        scale = 3 if img.width <= 16 else 2
        sw, sh = img.width * scale, img.height * scale
        scaled = img.resize((sw, sh), Image.NEAREST)
        sheet.paste(scaled, (cx + (90 - sw) // 2, cy + (64 - sh) // 2), scaled)
        draw.text((cx + 5, cy + 85), name.split(" ")[0], fill=(160, 150, 175, 255))

    # Section B: Tools, Items, Chest & Structures
    draw.rounded_rectangle([(20, 235), (980, 395)], radius=8, fill=(20, 16, 28, 255), outline=(50, 40, 68, 255))
    draw.text((32, 245), "2. VẬT PHẨM, RƯƠNG BÁU & CÔNG CỤ (ITEMS & CHEST)", fill=(240, 210, 160, 255))
    item_list = [
        ("Rương Gỗ (chest)", ember_chest),
        ("Cuốc Sắt (pickaxe)", raw_pickaxe),
        ("Rìu Đốn (axe)", raw_axe),
        ("Gỗ Mục (wood)", raw_wood),
        ("Quặng Đá (ore)", raw_ore),
        ("Hàng Rào (wall)", raw_fence),
        ("Đá Nhỏ (rock_small)", raw_rock_small),
        ("Bờ Dậu (bush_wide)", raw_bush_wide)
    ]
    for idx, (name, img) in enumerate(item_list):
        cx = 35 + idx * 118
        cy = 270
        scale = 3
        sw, sh = img.width * scale, img.height * scale
        scaled = img.resize((sw, sh), Image.NEAREST)
        sheet.paste(scaled, (cx + (90 - sw) // 2, cy + (64 - sh) // 2), scaled)
        draw.text((cx + 5, cy + 85), name.split(" ")[0], fill=(160, 150, 175, 255))

    # Section C: Full Tilesets Preview
    draw.rounded_rectangle([(20, 410), (980, 665)], radius=8, fill=(20, 16, 28, 255), outline=(50, 40, 68, 255))
    draw.text((32, 420), "3. BỘ TILESET NỀN TẢNG (GRASS, HILLS, PATHS, WATER, BRIDGES)", fill=(45, 230, 255, 255))
    tiles_show = [
        ("Grass Autotile (160×128)", grass_src, (160, 128)),
        ("Hills / Cliffs (96×96)", hills_src, (120, 120)),
        ("Paths (64×64)", paths_src, (100, 100)),
        ("Water (64×16)", water_src, (120, 30)),
        ("Wood Bridge (80×48)", bridge_src, (120, 72))
    ]
    cur_x = 35
    for title, img, (dw, dh) in tiles_show:
        scaled = img.resize((dw, dh), Image.NEAREST)
        sheet.paste(scaled, (cur_x, 445), scaled)
        draw.text((cur_x, 445 + dh + 6), title.split(" ")[0], fill=(180, 170, 200, 255))
        cur_x += dw + 25

    sheet.save("docs/sprout_lands_master_extraction.png")
    artifact_dir = "C:/Users/OS/.gemini/antigravity/brain/dc6023b7-ad69-4d5b-a5d0-20427d5efb77"
    if os.path.exists(artifact_dir):
        sheet.save(os.path.join(artifact_dir, "sprout_lands_master_extraction.png"))
    print("Saved docs/sprout_lands_master_extraction.png successfully!")

if __name__ == "__main__":
    extract_all()
