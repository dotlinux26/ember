import os
import shutil
import uuid
from PIL import Image, ImageDraw, ImageFont

def generate_unity_meta(file_path):
    meta_path = file_path + ".meta"
    if not os.path.exists(meta_path):
        guid = uuid.uuid4().hex
        meta_content = f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 12
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
  bumpmap:
    convertToNormalMap: 0
  isReadable: 1
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vramSurplusFlag: 0
  alphaIsTransparency: 1
  spriteImportMode: 1
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 16
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  textureType: 8
  textureShape: 1
  filterMode: 0
  aniso: 1
  wrapU: 1
  wrapV: 1
  wrapW: 1
"""
        with open(meta_path, "w", encoding="utf-8") as f:
            f.write(meta_content)

def master_extract():
    art_dir = "art_src"
    
    target_dirs = [
        ("My project/Assets/Resources/Ember/Props", "My project/Assets/Resources/Ember/Tilesets", "My project/Assets/Resources/Ember/Characters"),
        ("Builds/UiVerificationProject/Assets/Resources/Ember/Props", "Builds/UiVerificationProject/Assets/Resources/Ember/Tilesets", "Builds/UiVerificationProject/Assets/Resources/Ember/Characters")
    ]

    for p_dir, t_dir, c_dir in target_dirs:
        os.makedirs(p_dir, exist_ok=True)
        os.makedirs(t_dir, exist_ok=True)
        os.makedirs(c_dir, exist_ok=True)

    # 1. LOAD SOURCE SHEETS
    tools_src = Image.open(os.path.join(art_dir, "Basic tools and meterials.png")).convert("RGBA")
    biom_src = Image.open(os.path.join(art_dir, "Basic Grass Biom things 1.png")).convert("RGBA")
    doors_src = Image.open(os.path.join(art_dir, "Doors.png")).convert("RGBA")
    chest_src = Image.open(os.path.join(art_dir, "Chest.png")).convert("RGBA")
    fences_src = Image.open(os.path.join(art_dir, "Fences.png")).convert("RGBA")
    grass_src = Image.open(os.path.join(art_dir, "Grass.png")).convert("RGBA")
    hills_src = Image.open(os.path.join(art_dir, "Hills.png")).convert("RGBA")
    paths_src = Image.open(os.path.join(art_dir, "Paths.png")).convert("RGBA")
    water_src = Image.open(os.path.join(art_dir, "Water.png")).convert("RGBA")
    bridge_src = Image.open(os.path.join(art_dir, "Wood Bridge.png")).convert("RGBA")
    char_actions_src = Image.open(os.path.join(art_dir, "Basic Charakter Actions.png")).convert("RGBA")
    char_walk_src = Image.open(os.path.join(art_dir, "Basic Charakter Spritesheet.png")).convert("RGBA")
    tools_overlay_src = Image.open(os.path.join(art_dir, "Tools.png")).convert("RGBA")

    # 2. EXTRACT EXACT 1:1 SPRITES
    sprites = {}

    # --- TOOLS & MATERIALS ---
    sprites["watering_can"] = tools_src.crop((0, 0, 16, 16))
    sprites["axe"] = tools_src.crop((16, 0, 32, 16))
    sprites["hoe"] = tools_src.crop((32, 0, 48, 16))
    sprites["pickaxe"] = tools_src.crop((16, 16, 32, 32))
    raw_wood_mat = tools_src.crop((32, 16, 48, 32))
    sprites["wood"] = raw_wood_mat.copy()
    sprites["wood_material"] = raw_wood_mat.copy()

    raw_stone_mat = tools_src.crop((0, 16, 16, 32))
    sprites["stone_material"] = raw_stone_mat.copy()
    
    ember_ore = raw_stone_mat.copy()
    pix_ore = ember_ore.load()
    for ox, oy in [(7, 7), (8, 7), (8, 8), (9, 8), (7, 9)]:
        if pix_ore[ox, oy][3] > 0:
            pix_ore[ox, oy] = (255, 140, 35, 255)
    sprites["ore"] = ember_ore

    # --- ENVIRONMENT PROPS ---
    raw_oak = biom_src.crop((16, 0, 48, 32)) # 32x32
    raw_apple = biom_src.crop((48, 0, 80, 32)) # 32x32
    raw_pine = biom_src.crop((0, 0, 16, 32)) # 16x32
    
    sprites["tree"] = raw_oak.copy()
    sprites["tree_oak"] = raw_oak.copy()
    sprites["tree_pine"] = raw_pine.copy()

    ember_apple = raw_apple.copy()
    pix_a = ember_apple.load()
    for y in range(32):
        for x in range(32):
            r, g, b, a = pix_a[x, y]
            if a > 0 and ((r > 140 and g < 100 and b < 100) or (r > 160 and g < 130 and r > g + 40)):
                pix_a[x, y] = (235, 38, 52, 255)
                if x > 0 and y > 0 and pix_a[x - 1, y - 1][3] > 0:
                    pix_a[x - 1, y - 1] = (255, 185, 195, 255)
    sprites["tree_apple"] = ember_apple

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
    sprites["tree_autumn"] = ember_autumn

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
    sprites["tree_void"] = ember_void

    raw_stump = biom_src.crop((80, 32, 96, 48))
    ember_stump = raw_stump.copy()
    pix_s = ember_stump.load()
    if pix_s[3, 10][3] > 0:
        pix_s[3, 10] = (45, 230, 255, 255)
        pix_s[4, 10] = (180, 250, 255, 255)
    sprites["tree_stump"] = ember_stump

    sprites["wood_branch"] = biom_src.crop((48, 32, 64, 48))
    sprites["wood_log"] = biom_src.crop((64, 32, 80, 48))
    sprites["tall_stump"] = biom_src.crop((128, 32, 144, 64))

    raw_rock_boulder = biom_src.crop((128, 16, 144, 32))
    ember_rock = raw_rock_boulder.copy()
    pix_r = ember_rock.load()
    for rx, ry in [(8, 6), (8, 7), (9, 8), (9, 9), (10, 10), (11, 11)]:
        if 0 <= rx < 16 and 0 <= ry < 16 and pix_r[rx, ry][3] > 0:
            pix_r[rx, ry] = (45, 230, 255, 255)
            if rx > 0 and pix_r[rx - 1, ry][3] > 0 and pix_r[rx - 1, ry] != (45, 230, 255, 255):
                pix_r[rx - 1, ry] = (18, 95, 135, 255)
    sprites["rock_large"] = ember_rock
    sprites["rock_boulder"] = raw_rock_boulder
    sprites["rock_small"] = biom_src.crop((112, 16, 128, 32))
    sprites["stone_pebble"] = biom_src.crop((96, 0, 112, 16))
    sprites["ore_node"] = biom_src.crop((80, 48, 96, 64))

    sprites["bush_round"] = biom_src.crop((112, 64, 128, 80))
    sprites["bush_small"] = biom_src.crop((80, 64, 96, 80))
    sprites["bush_wide"] = biom_src.crop((0, 64, 32, 80))
    sprites["hedge_large"] = biom_src.crop((32, 64, 80, 80))
    
    raw_flowers = biom_src.crop((112, 0, 128, 16))
    ember_flowers = raw_flowers.copy()
    pix_f = ember_flowers.load()
    for y in range(16):
        for x in range(16):
            r, g, b, a = pix_f[x, y]
            if a > 0 and b > 140 and r < 120 and g < 120:
                pix_f[x, y] = (45, 230, 255, 255)
    sprites["wildflowers"] = ember_flowers
    sprites["mushrooms"] = biom_src.crop((128, 0, 144, 16))

    # --- CHESTS ---
    raw_chest = chest_src.crop((16, 18, 32, 34))
    ember_chest = raw_chest.copy()
    pix_ch = ember_chest.load()
    for cx, cy in [(7, 7), (8, 7), (7, 8), (8, 8)]:
        if 0 <= cx < 16 and 0 <= cy < 16 and pix_ch[cx, cy][3] > 0:
            pix_ch[cx, cy] = (45, 230, 255, 255)
    sprites["chest"] = ember_chest
    sprites["chest_wood_closed"] = raw_chest
    sprites["chest_wood_open"] = chest_src.crop((16 + 4*48, 18, 32 + 4*48, 34))
    sprites["chest_side_closed"] = chest_src.crop((16, 18 + 48, 32, 34 + 48))
    sprites["chest_side_open"] = chest_src.crop((16 + 4*48, 18 + 48, 32 + 4*48, 34 + 48))

    # --- FENCES & DOORS ---
    sprites["wall"] = fences_src.crop((16, 0, 32, 16))
    sprites["fence_gate"] = fences_src.crop((16, 32, 32, 48))
    sprites["door"] = doors_src.crop((0, 0, 16, 16))
    sprites["door_open"] = doors_src.crop((0, 48, 16, 64))

    print(f"Master extraction created {len(sprites)} essential Ember sprites.")

    for p_dir, t_dir, c_dir in target_dirs:
        for name, img in sprites.items():
            out_file = os.path.join(p_dir, f"{name}.png")
            img.save(out_file)
            generate_unity_meta(out_file)

        tiles = {
            "grass_tileset.png": grass_src,
            "hills_tileset.png": hills_src,
            "paths_tileset.png": paths_src,
            "water_tileset.png": water_src,
            "bridge_tileset.png": bridge_src,
            "fences_tileset.png": fences_src
        }
        for tname, timg in tiles.items():
            tout = os.path.join(t_dir, tname)
            timg.save(tout)
            generate_unity_meta(tout)

        chars = {
            "character_spritesheet.png": char_walk_src,
            "character_actions.png": char_actions_src,
            "tools_overlay.png": tools_overlay_src
        }
        for cname, cimg in chars.items():
            cout = os.path.join(c_dir, cname)
            cimg.save(cout)
            generate_unity_meta(cout)

    create_verified_showcase(sprites, "docs/sprout_lands_verified_catalog.png")
    artifact_dir = r"C:\Users\OS\.gemini\antigravity\brain\dc6023b7-ad69-4d5b-a5d0-20427d5efb77"
    if os.path.exists(artifact_dir):
        create_verified_showcase(sprites, os.path.join(artifact_dir, "sprout_lands_verified_catalog.png"))

def create_verified_showcase(sprites, output_path):
    cell_w, cell_h = 100, 110
    cols = 6
    
    showcase_items = [
        ("wood", "Wood (Item)"),
        ("ore", "Ember Ore"),
        ("watering_can", "Watering Can"),
        ("axe", "Axe"),
        ("hoe", "Hoe"),
        ("pickaxe", "Pickaxe"),
        
        ("chest", "Ember Chest"),
        ("chest_wood_open", "Open Chest"),
        ("wall", "Wood Fence"),
        ("fence_gate", "Fence Gate"),
        ("door", "Wood Door"),
        ("door_open", "Door Open"),
        
        ("tree_oak", "Oak Tree"),
        ("tree_apple", "Ember Apple"),
        ("tree_autumn", "Autumn Tree"),
        ("tree_void", "Void Tree"),
        ("tree_pine", "Snow Pine"),
        ("tree_stump", "Glow Stump"),
        
        ("wood_log", "Firewood Log"),
        ("wood_branch", "Branch"),
        ("rock_large", "Rune Boulder"),
        ("rock_small", "Rock Small"),
        ("ore_node", "Ore Node"),
        ("stone_pebble", "Stone Pebble"),
        
        ("bush_round", "Round Bush"),
        ("bush_small", "Small Bush"),
        ("bush_wide", "Wide Bush"),
        ("hedge_large", "Large Hedge"),
        ("wildflowers", "Wildflowers"),
        ("mushrooms", "Mushrooms")
    ]

    rows = (len(showcase_items) + cols - 1) // cols
    header_h = 60
    img_w = cols * cell_w + 40
    img_h = rows * cell_h + header_h + 30
    
    canvas = Image.new("RGBA", (img_w, img_h), (18, 22, 28, 255))
    draw = ImageDraw.Draw(canvas)

    draw.rectangle([(0, 0), (img_w, header_h)], fill=(28, 34, 44, 255))
    draw.text((20, 12), "EMBER x SPROUT LANDS - VERIFIED 1:1 CLEAN CATALOG", fill=(245, 245, 250, 255))
    draw.text((20, 36), "100% Focused on Survival / Exploration - Zero Redundant Assets", fill=(120, 220, 255, 255))

    for idx, (key, label) in enumerate(showcase_items):
        r = idx // cols
        c = idx % cols
        x0 = 20 + c * cell_w
        y0 = header_h + 15 + r * cell_h
        
        draw.rectangle([(x0, y0), (x0 + cell_w - 6, y0 + cell_h - 8)], fill=(26, 32, 42, 255), outline=(48, 58, 76, 255))
        
        if key in sprites:
            sp = sprites[key]
            scale = 3 if sp.width <= 16 and sp.height <= 16 else (2 if sp.width <= 32 and sp.height <= 32 else 1)
            scaled = sp.resize((sp.width * scale, sp.height * scale), Image.NEAREST)
            cx = x0 + (cell_w - 6 - scaled.width) // 2
            cy = y0 + 10 + (55 - scaled.height) // 2
            canvas.paste(scaled, (cx, cy), scaled)
        
        draw.text((x0 + 4, y0 + 72), label[:14], fill=(220, 225, 235, 255))
        if key in sprites:
            sp = sprites[key]
            draw.text((x0 + 4, y0 + 86), f"{sp.width}x{sp.height}px", fill=(130, 150, 175, 255))

    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    canvas.save(output_path)
    print(f"Showcase catalog saved to {output_path}")

if __name__ == "__main__":
    master_extract()
