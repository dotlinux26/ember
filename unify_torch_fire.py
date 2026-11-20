import os
import shutil
from PIL import Image

def process_torch_fire():
    src_path = "My project/Assets/Resources/Ember/Props/torch_fire.png"
    im = Image.open(src_path).convert("RGBA")
    
    # In Frame 3 (x=3*32 to 4*32), shift pixels at x=10 in rows y=2,3,4 by +1 px to x=11
    f3_x0 = 3 * 32
    # Check pixels at f3_x0 + 10
    # For y in 2, 3, 4: if pixel at (f3_x0 + 10, y) is set, shift line right by 1 or clear x=10 and reinforce x=11
    for y in [2, 3, 4]:
        p = im.getpixel((f3_x0 + 10, y))
        if p[3] > 0:
            # Shift the slice x=10..14 to x=11..15
            row_pixels = [im.getpixel((f3_x0 + x, y)) for x in range(10, 16)]
            im.putpixel((f3_x0 + 10, y), (0, 0, 0, 0))
            for i, px in enumerate(row_pixels[:-1]):
                im.putpixel((f3_x0 + 11 + i, y), px)
                
    im.save(src_path)
    
    # Sync to ArtShowcase
    p_raw = "ArtShowcase/raw/torch_fire.png"
    im.save(p_raw)
    
    # 8x scaled
    im_8x = im.resize((im.width * 8, im.height * 8), Image.Resampling.NEAREST)
    im_8x.save("ArtShowcase/zoomed_8x/torch_fire.png")
    im_8x.save("ArtShowcase/zoomed_8x/torch_fire_8x.png")
    print("torch_fire.png processed and synced successfully!")

if __name__ == "__main__":
    process_torch_fire()
