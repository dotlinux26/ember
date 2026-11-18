import math
import numpy as np
from PIL import Image

def hash32(x, y):
    n = (x * 374761393 + y * 668265263) ^ 0x5bf03635
    n = ((n ^ (n >> 13)) * 1274126177) & 0xFFFFFFFF
    return ((n ^ (n >> 16)) & 0x7FFFFFFF)

def generate_sample_terrain_image():
    size = 120
    ppTile = 8
    px = size * ppTile
    img = Image.new("RGBA", (px, px), (0, 0, 0, 255))
    pix = img.load()

    # 0 = Forest, 1 = Swamp, 2 = Void, 3 = Tree, 4 = Abyss
    tiles = bytearray(size * size)
    for y in range(size):
        for x in range(size):
            dx = x - size // 2
            dy = y - size // 2
            r = math.sqrt(dx * dx + dy * dy)
            if x < 2 or y < 2 or x >= size - 2 or y >= size - 2:
                tiles[y * size + x] = 4
            elif r < 6:
                tiles[y * size + x] = 0
            elif (x * 17 + y * 31) % 19 == 0 and r > 7:
                tiles[y * size + x] = 3
            elif r < size * 0.22:
                tiles[y * size + x] = 0
            elif r < size * 0.38:
                tiles[y * size + x] = 1
            else:
                tiles[y * size + x] = 2

    for ty in range(size):
        for tx in range(size):
            t_idx = ty * size + tx
            tile = tiles[t_idx]
            dx = tx - size // 2
            dy = ty - size // 2
            dist2 = dx * dx + dy * dy
            is_sanctuary = dist2 <= 38

            for ly in range(ppTile):
                for lx in range(ppTile):
                    gx = tx * ppTile + lx
                    gy = ty * ppTile + ly
                    h = hash32(gx, gy)

                    if is_sanctuary:
                        # Flagstone pavement
                        if lx == 0 or ly == 0:
                            pix[gx, gy] = (22, 19, 30, 255) # grout
                        elif lx == 1 or ly == 1:
                            pix[gx, gy] = (58, 52, 74, 255) # stone highlight
                        elif dist2 > 8 and (h % 31 == 0):
                            pix[gx, gy] = (45, 215, 245, 255) # glowing runic dust
                        else:
                            pix[gx, gy] = (40, 36, 52, 255)
                    elif tile == 4:
                        pix[gx, gy] = (4, 3, 7, 255)
                    elif tile == 3: # tree base
                        if (h % 5) == 0:
                            pix[gx, gy] = (34, 24, 40, 255)
                        else:
                            pix[gx, gy] = (22, 16, 28, 255)
                    elif tile == 0: # Forest
                        m = h % 64
                        if m < 3:
                            pix[gx, gy] = (120, 235, 215, 255) # bioluminescent flower
                        elif m < 14:
                            pix[gx, gy] = (56, 92, 76, 255) # lush moss
                        elif m < 30:
                            pix[gx, gy] = (40, 64, 52, 255) # dark moss
                        elif m < 36:
                            pix[gx, gy] = (50, 46, 60, 255) # pebble
                        else:
                            pix[gx, gy] = (26, 38, 32, 255) # soil
                    elif tile == 1: # Swamp
                        m = h % 64
                        if m < 2:
                            pix[gx, gy] = (65, 235, 160, 255) # swamp spore
                        elif m < 12:
                            pix[gx, gy] = (48, 88, 72, 255) # algae swirl
                        elif m < 26:
                            pix[gx, gy] = (34, 60, 50, 255) # murky ripple
                        elif m < 38:
                            pix[gx, gy] = (15, 25, 25, 255) # dark pool
                        else:
                            pix[gx, gy] = (22, 34, 32, 255) # peat
                    else: # Void
                        m = h % 64
                        if m < 2:
                            pix[gx, gy] = (205, 170, 255, 255) # starlight crystal
                        elif m < 8:
                            pix[gx, gy] = (145, 80, 230, 255) # glowing rift
                        elif m < 20:
                            pix[gx, gy] = (95, 50, 155, 255) # void vein
                        elif m < 34:
                            pix[gx, gy] = (14, 10, 22, 255) # rift shadow
                        else:
                            pix[gx, gy] = (26, 20, 36, 255) # obsidian

    img.save("docs/terrain_preview.png")
    artifact_dir = "C:/Users/OS/.gemini/antigravity/brain/dc6023b7-ad69-4d5b-a5d0-20427d5efb77"
    img.save(f"{artifact_dir}/terrain_preview.png")
    print("Regenerated docs/terrain_preview.png with Murmur-style hash!")

if __name__ == "__main__":
    generate_sample_terrain_image()
