import os
import json
from PIL import Image, ImageDraw, ImageFont

def render_assets():
    with open("Builds/map-raster-evidence.json", "r", encoding="utf-8") as f:
        data = json.load(f)

    generated_images = {}

    for name, item in data.items():
        w = item["w"]
        h = item["h"]
        img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        pix = img.load()

        for coord_str, hex_col in item["pixels"].items():
            x, y = map(int, coord_str.split(","))
            if 0 <= x < w and 0 <= y < h:
                # Parse hex
                hc = hex_col.lstrip("#")
                if len(hc) == 6:
                    r, g, b = int(hc[0:2], 16), int(hc[2:4], 16), int(hc[4:6], 16)
                    a = 255
                elif len(hc) == 8:
                    r, g, b, a = int(hc[0:2], 16), int(hc[2:4], 16), int(hc[4:6], 16), int(hc[6:8], 16)
                else:
                    continue
                pix[x, y] = (r, g, b, a)

        target_png = item["targetPng"]
        os.makedirs(os.path.dirname(target_png), exist_ok=True)
        img.save(target_png)
        print(f"Exported PNG: {target_png} ({w}x{h})")
        generated_images[name] = img

    # -------------------------------------------------------------
    # Generate High-Resolution Showcase Preview
    # -------------------------------------------------------------
    pw, ph = 880, 520
    preview = Image.new("RGBA", (pw, ph), (14, 11, 22, 255))
    draw = ImageDraw.Draw(preview)

    # Header
    draw.rectangle([(0, 0), (pw, 44)], fill=(22, 17, 34, 255))
    draw.line([(0, 44), (pw, 44)], fill=(75, 55, 110, 255), width=2)

    try:
        font_title = ImageFont.truetype("arial.ttf", 20)
        font_name = ImageFont.truetype("arialbd.ttf", 15)
        font_sub = ImageFont.truetype("arial.ttf", 12)
    except:
        font_title = font_name = font_sub = ImageFont.load_default()

    draw.text((20, 12), "EMBER · BỘ TÀI NGUYÊN BẢN ĐỒ & CÔNG TRÌNH PIX2 1.1 MỚI", fill=(255, 230, 180, 255), font=font_title)

    items_showcase = [
        ("Cây Cổ Thụ (Ancient Tree)", "tree", 48, 64, "48×64px", "Cổ thụ rễ cuồn cuộn bám đất, tán lá thông/sồi rậm rạp 3 tầng rủ rêu ma quái."),
        ("Trụ Hải Đăng (Runic Beacon)", "beacon", 48, 64, "48×64px", "Bệ đá đa tầng chạm khắc cổ ngữ, lồng đá giam ngọn Lửa Vĩnh Cửu xanh băng."),
        ("Tường Barricade (Defense Wall)", "wall", 32, 32, "32×32px", "Hàng rào cọc gỗ nhọn kiên cố bọc đai hắc thiết có đinh tán tán nhọn."),
        ("Rương Cổ (Relic Chest)", "chest", 32, 32, "32×32px", "Rương hắc thiết đúc đồng cổ chạm khắc rune, ổ khóa phát sáng dịu."),
        ("Bẫy Điện Gai (Runic Trap)", "trap", 32, 32, "32×32px", "Bẫy sàn bát giác đúc thép, chông nhọn 4 góc và rãnh điện tích lam ngọc."),
        ("Đuốc Cắm Đất (Ember Torch)", "torch", 32, 32, "32×32px", "Trụ gỗ cổ quấn xích sắt, vạc than rực ngọn lửa cam vàng bập bùng.")
    ]

    # Two rows of 3 items
    for idx, (title, key, fw, fh, sz_text, desc) in enumerate(items_showcase):
        row = idx // 3
        col = idx % 3
        cx = 20 + col * 280
        cy = 58 + row * 220
        cw = 265
        ch = 206

        # Card
        draw.rounded_rectangle([(cx, cy), (cx + cw, cy + ch)], radius=8, fill=(22, 18, 32, 255), outline=(50, 40, 70, 255))

        # Title
        draw.text((cx + 12, cy + 10), title, fill=(245, 215, 160, 255), font=font_name)
        draw.text((cx + 12, cy + 28), f"Chuẩn PIX2 1.1 · {sz_text}", fill=(115, 165, 215, 255), font=font_sub)

        # Draw Sprite Centered in a preview box
        box_w = 120
        box_h = 110
        bx = cx + (cw - box_w) // 2
        by = cy + 46
        draw.rectangle([(bx, by), (bx + box_w, by + box_h)], fill=(10, 8, 16, 255), outline=(42, 34, 58, 255))

        # Scale sprite 1.5x
        scale = 1.5 if fh == 64 else 2.5
        s_img = generated_images[key]
        sw = int(fw * scale)
        sh = int(fh * scale)
        s_scaled = s_img.resize((sw, sh), Image.NEAREST)

        sx = bx + (box_w - sw) // 2
        sy = by + (box_h - sh) // 2
        preview.paste(s_scaled, (sx, sy), s_scaled)

        # Description text
        draw.text((cx + 10, cy + 164), desc[:36], fill=(160, 150, 180, 255), font=font_sub)
        draw.text((cx + 10, cy + 182), desc[36:72], fill=(160, 150, 180, 255), font=font_sub)

    os.makedirs("docs", exist_ok=True)
    preview.save("docs/map_props_preview.png")

    artifact_dir = "C:/Users/OS/.gemini/antigravity/brain/dc6023b7-ad69-4d5b-a5d0-20427d5efb77"
    if os.path.exists(artifact_dir):
        preview.save(os.path.join(artifact_dir, "map_props_preview.png"))

    print("Successfully rendered map props preview image!")

if __name__ == "__main__":
    render_assets()
