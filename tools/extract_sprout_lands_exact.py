import os
from PIL import Image, ImageDraw

def extract_and_enhance():
    src_path = "temp_sprout/Basic Grass Biom things 1.png"
    if not os.path.exists(src_path):
        raise FileNotFoundError(f"Source file {src_path} not found.")

    src = Image.open(src_path).convert("RGBA")
    props_dir = "My project/Assets/Resources/Ember/Props"
    os.makedirs(props_dir, exist_ok=True)

    # ------------------------------------------------------------------------
    # 1. EXACT 1:1 SLICES DIRECTLY FROM SPROUT LANDS
    # ------------------------------------------------------------------------
    # Tree Oak: (16, 0, 48, 32)
    raw_oak = src.crop((16, 0, 48, 32))
    
    # Tree Apple: (48, 0, 80, 32)
    raw_apple = src.crop((48, 0, 80, 32))
    
    # Tree Stump: (80, 32, 96, 48)
    raw_stump = src.crop((80, 32, 96, 48))
    
    # Large Rock: (128, 16, 144, 32)
    raw_rock = src.crop((128, 16, 144, 32))
    
    # Bushes:
    raw_bush_round = src.crop((112, 64, 128, 80))
    raw_bush_wide = src.crop((128, 64, 144, 80))
    
    # Wildflowers / Mushrooms: (112, 0, 128, 16) & (128, 0, 144, 16)
    raw_flowers = src.crop((112, 0, 128, 16))

    # ------------------------------------------------------------------------
    # 2. CHẤM PHÁ (ADD EMBER ARTISTIC TOUCHES ON TOP OF 1:1 SPROUT LANDS)
    # ------------------------------------------------------------------------
    
    # A. Tree Apple (Ember Red Apples with Glowing Highlights)
    ember_apple = raw_apple.copy()
    pix_a = ember_apple.load()
    w, h = ember_apple.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = pix_a[x, y]
            if a > 0:
                # If red/brown apple pixel in Sprout Lands
                if (r > 140 and g < 100 and b < 100) or (r > 160 and g < 130 and b < 110 and r > g + 40):
                    # Make into ripe glowing ember apple
                    pix_a[x, y] = (235, 38, 52, 255)
                    # Add highlight on top-left of apple
                    if x > 0 and y > 0 and pix_a[x - 1, y - 1][3] > 0:
                        pix_a[x - 1, y - 1] = (255, 185, 195, 255)

    # B. Tree Oak (Subtle Twilight Foliage Highlights)
    ember_oak = raw_oak.copy()

    # C. Tree Autumn (Warm Amber/Crimson Autumn Palette Shift from 1:1 Tree)
    ember_autumn = raw_oak.copy()
    pix_aut = ember_autumn.load()
    for y in range(h):
        for x in range(w):
            r, g, b, a = pix_aut[x, y]
            if a > 0 and g > r and g > b:
                # Foliage pixel: shift to rich autumn maple (amber / crimson / golden orange)
                lum = (r * 0.299 + g * 0.587 + b * 0.114) / 255.0
                if lum > 0.65:
                    pix_aut[x, y] = (245, 180, 50, a) # Golden sunlit leaves
                elif lum > 0.50:
                    pix_aut[x, y] = (225, 110, 40, a) # Vibrant orange
                elif lum > 0.35:
                    pix_aut[x, y] = (185, 55, 35, a)  # Crimson red
                else:
                    pix_aut[x, y] = (115, 30, 30, a)  # Deep shadow wine

    # D. Tree Void (Eldritch Violet Palette Shift from 1:1 Tree)
    ember_void = raw_oak.copy()
    pix_v = ember_void.load()
    for y in range(h):
        for x in range(w):
            r, g, b, a = pix_v[x, y]
            if a > 0 and g > r and g > b:
                lum = (r * 0.299 + g * 0.587 + b * 0.114) / 255.0
                if lum > 0.65:
                    pix_v[x, y] = (195, 145, 255, a) # Luminescent lilac
                elif lum > 0.50:
                    pix_v[x, y] = (145, 75, 220, a)  # Deep amethyst
                elif lum > 0.35:
                    pix_v[x, y] = (85, 38, 140, a)   # Dark void purple
                else:
                    pix_v[x, y] = (38, 18, 68, a)    # Abyss shadow

    # E. Rock Large (Sprout Lands Rock with Ancient Glowing Cyan Runic Crack)
    ember_rock = raw_rock.copy()
    pix_r = ember_rock.load()
    # Add cyan glowing rune fissure down the rock face
    rune_pixels = [(8, 6), (8, 7), (9, 8), (9, 9), (10, 10), (10, 11)]
    for rx, ry in rune_pixels:
        if 0 <= rx < 16 and 0 <= ry < 16 and pix_r[rx, ry][3] > 0:
            pix_r[rx, ry] = (45, 230, 255, 255) # Cyan rune core
            if rx > 0 and pix_r[rx - 1, ry][3] > 0 and pix_r[rx - 1, ry] != (45, 230, 255, 255):
                pix_r[rx - 1, ry] = (18, 95, 135, 255) # Rune shadow

    # F. Tree Stump (Sprout Lands Stump with Glowing Spore)
    ember_stump = raw_stump.copy()
    pix_s = ember_stump.load()
    # Add tiny cyan spore on root flare
    if pix_s[3, 10][3] > 0:
        pix_s[3, 10] = (45, 230, 255, 255)
        pix_s[4, 10] = (180, 250, 255, 255)

    # G. Wildflowers (Yellow Daisy & Cyan Ember Blossom)
    ember_flowers = raw_flowers.copy()
    pix_f = ember_flowers.load()
    for y in range(16):
        for x in range(16):
            r, g, b, a = pix_f[x, y]
            if a > 0:
                if b > 140 and r < 120 and g < 120:
                    pix_f[x, y] = (45, 230, 255, 255) # Glowing cyan petal

    # ------------------------------------------------------------------------
    # 3. SAVE DIRECTLY TO GAME PROPS
    # ------------------------------------------------------------------------
    ember_oak.save(os.path.join(props_dir, "tree.png"))
    ember_oak.save(os.path.join(props_dir, "tree_oak.png"))
    ember_apple.save(os.path.join(props_dir, "tree_apple.png"))
    ember_autumn.save(os.path.join(props_dir, "tree_autumn.png"))
    ember_void.save(os.path.join(props_dir, "tree_void.png"))
    raw_bush_round.save(os.path.join(props_dir, "bush_round.png"))
    raw_bush_wide.save(os.path.join(props_dir, "bush_wide.png"))
    ember_stump.save(os.path.join(props_dir, "tree_stump.png"))
    ember_rock.save(os.path.join(props_dir, "rock_large.png"))
    ember_flowers.save(os.path.join(props_dir, "wildflowers.png"))

    print("Successfully extracted 1:1 Sprout Lands assets and applied Ember enhancements!")

    # ------------------------------------------------------------------------
    # 4. SHOWCASE PREVIEW ARTIFACT
    # ------------------------------------------------------------------------
    pw, ph = 960, 520
    preview = Image.new("RGBA", (pw, ph), (14, 11, 20, 255))
    draw = ImageDraw.Draw(preview)

    draw.rectangle([(0, 0), (pw, 44)], fill=(22, 16, 32, 255))
    draw.line([(0, 44), (pw, 44)], fill=(80, 60, 115, 255), width=2)
    draw.text((20, 12), "EMBER · TRÍCH XUẤT 1:1 SPROUT LANDS & NÉT CHẤM PHÁ PHÁT QUANG HUYỀN BÍ", fill=(255, 230, 180, 255))

    items = [
        ("Cây Sồi (tree_oak)", raw_oak, ember_oak, "Trích xuất 1:1 tán lá tròn, thân gốc bạnh rêu."),
        ("Cây Táo (tree_apple)", raw_apple, ember_apple, "Quả táo Sprout Lands chấm phá ánh sáng rực rỡ."),
        ("Cây Thu (tree_autumn)", raw_oak, ember_autumn, "Chuyển hệ màu lá phong đỏ cam mùa thu."),
        ("Cây Hư Không (tree_void)", raw_oak, ember_void, "Chuyển hệ màu thạch anh tím bóng đêm."),
        ("Đá Tảng (rock_large)", raw_rock, ember_rock, "Đá vát cạnh 1:1 chạm khắc thêm khe nứt rune cyan."),
        ("Gốc Cây (tree_stump)", raw_stump, ember_stump, "Gốc cây đốn 1:1 đính kèm bào tử nấm dạ quang."),
        ("Bụi Cây (bush_round)", raw_bush_round, raw_bush_round, "Bụi lá tròn 1:1 xanh mướt Sprout Lands.")
    ]

    col_w = (pw - 40) // len(items)
    for i, (name, raw_img, enh_img, desc) in enumerate(items):
        bx = 20 + i * col_w
        by = 55
        bw = col_w - 8
        bh = ph - 70
        draw.rounded_rectangle([(bx, by), (bx + bw, by + bh)], radius=6, fill=(20, 16, 26, 255), outline=(48, 38, 62, 255))
        
        # Label
        draw.text((bx + 6, by + 10), name.split(" ")[0], fill=(240, 210, 160, 255))
        
        # 1. Raw 1:1 Image
        draw.text((bx + 6, by + 30), "1:1 Gốc", fill=(140, 140, 160, 255))
        scale = 3 if raw_img.width <= 16 else 2
        rw, rh = raw_img.width * scale, raw_img.height * scale
        r_scaled = raw_img.resize((rw, rh), Image.NEAREST)
        preview.paste(r_scaled, (bx + (bw - rw) // 2, by + 50), r_scaled)

        # 2. Enhanced Image
        draw.text((bx + 6, by + 130), "+ Chấm Phá", fill=(45, 230, 255, 255))
        ew, eh = enh_img.width * scale, enh_img.height * scale
        e_scaled = enh_img.resize((ew, eh), Image.NEAREST)
        preview.paste(e_scaled, (bx + (bw - ew) // 2, by + 150), e_scaled)

        # Description
        words = desc.split()
        lines = []
        cur = ""
        for w in words:
            if len(cur + " " + w) > 16:
                lines.append(cur)
                cur = w
            else:
                cur = cur + " " + w if cur else w
        if cur: lines.append(cur)

        for li, line in enumerate(lines):
            draw.text((bx + 6, by + 235 + li * 14), line, fill=(155, 145, 170, 255))

    preview.save("docs/sprout_lands_1to1_extracted.png")
    artifact_dir = "C:/Users/OS/.gemini/antigravity/brain/dc6023b7-ad69-4d5b-a5d0-20427d5efb77"
    if os.path.exists(artifact_dir):
        preview.save(os.path.join(artifact_dir, "sprout_lands_1to1_extracted.png"))
    print("Saved docs/sprout_lands_1to1_extracted.png successfully!")

if __name__ == "__main__":
    extract_and_enhance()
