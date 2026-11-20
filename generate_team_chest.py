import os
from PIL import Image

def create_team_chest():
    w, h = 32, 32
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = img.load()

    # Palette
    C_OUTLINE = (24, 20, 37, 255)       # #181425 Dark contour
    C_WOOD_DARK = (74, 40, 16, 255)     # #4A2810 Shadow wood
    C_WOOD_MID1 = (92, 50, 20, 255)     # #5C3214 Mid-dark wood
    C_WOOD_MID2 = (120, 68, 33, 255)    # #784421 Main wood
    C_WOOD_LIGHT = (143, 83, 43, 255)   # #8F532B Light wood
    C_WOOD_HI = (181, 107, 56, 255)     # #B56B38 Highlight wood
    C_WOOD_TOP = (210, 130, 71, 255)    # #D28247 Top edge highlight

    C_IRON_OUT = (34, 37, 42, 255)      # #22252A Dark iron
    C_IRON_MID = (62, 68, 79, 255)      # #3E444F Mid iron
    C_IRON_HI = (104, 113, 130, 255)    # #687182 Light iron
    C_IRON_SPEC = (160, 170, 185, 255)  # #A0AAB9 Specular iron rivet

    C_GOLD_DARK = (120, 80, 16, 255)    # #785010 Brass shadow
    C_GOLD_MID = (184, 134, 30, 255)    # #B8861E Brass mid
    C_GOLD_HI = (245, 200, 66, 255)     # #F5C842 Brass highlight
    C_GOLD_SPEC = (255, 240, 170, 255)  # #FFF0AA White-gold gleam

    # 1. Fill base silhouette with C_OUTLINE
    for x in range(8, 24):
        px[x, 9] = C_OUTLINE
    for x in range(6, 26):
        px[x, 10] = C_OUTLINE
    for y in range(11, 28):
        px[4, y] = C_OUTLINE
        px[27, y] = C_OUTLINE
    for x in range(5, 27):
        px[x, 28] = C_OUTLINE

    # 2. Interior wood fill
    for y in range(10, 17):
        x_start = 8 if y == 10 else 6 if y == 11 else 5
        x_end = 23 if y == 10 else 25 if y == 11 else 26
        for x in range(x_start, x_end + 1):
            if px[x, y][3] == 0:
                if y == 10:
                    px[x, y] = C_WOOD_TOP if x > 16 else C_WOOD_HI
                elif y == 11:
                    px[x, y] = C_WOOD_HI if x > 14 else C_WOOD_LIGHT
                elif y in (12, 13):
                    px[x, y] = C_WOOD_LIGHT if x > 15 else C_WOOD_MID2
                else:
                    px[x, y] = C_WOOD_MID2 if x > 12 else C_WOOD_MID1

    for y in range(18, 28):
        for x in range(5, 27):
            if y == 18:
                px[x, y] = C_WOOD_DARK
            elif y in (19, 20):
                px[x, y] = C_WOOD_LIGHT if x > 16 else C_WOOD_MID2
            elif y in (21, 22):
                px[x, y] = C_WOOD_MID2 if x > 15 else C_WOOD_MID1
            elif y in (23, 24, 25):
                px[x, y] = C_WOOD_MID1 if x > 12 else C_WOOD_DARK
            else:
                px[x, y] = C_WOOD_DARK

    for x in range(5, 27):
        px[x, 14] = C_WOOD_DARK
        px[x, 22] = C_WOOD_DARK

    # 3. Iron Straps & Reinforcements
    for x in range(4, 28):
        px[x, 17] = C_IRON_MID
    px[4, 17] = C_OUTLINE
    px[27, 17] = C_OUTLINE
    for x in range(6, 26):
        px[x, 16] = C_IRON_HI if x > 14 else C_IRON_MID
    px[14, 16] = C_IRON_SPEC
    px[15, 16] = C_IRON_SPEC

    for band_x in (8, 22):
        for y in range(10, 28):
            if px[band_x, y][3] > 0 and px[band_x, y] != C_OUTLINE:
                px[band_x, y] = C_IRON_MID
                px[band_x + 1, y] = C_IRON_HI if band_x == 22 else C_IRON_MID
        for rivet_y in (12, 19, 25):
            px[band_x, rivet_y] = C_IRON_SPEC
            px[band_x + 1, rivet_y] = C_IRON_OUT

    for y in (26, 27):
        px[5, y] = C_IRON_MID
        px[6, y] = C_IRON_MID
        px[25, y] = C_IRON_HI
        px[26, y] = C_IRON_MID
    px[6, 26] = C_IRON_SPEC
    px[25, 26] = C_IRON_SPEC

    for x in (5, 6, 7, 24, 25, 26):
        px[x, 28] = C_IRON_OUT

    # 4. Golden Co-op Clasp
    for y in range(17, 22):
        for x in range(14, 18):
            px[x, y] = C_GOLD_MID
    px[14, 17] = C_GOLD_HI
    px[15, 17] = C_GOLD_SPEC
    px[16, 17] = C_GOLD_SPEC
    px[17, 17] = C_GOLD_HI

    px[14, 18] = C_GOLD_HI
    px[15, 18] = C_GOLD_SPEC
    px[16, 18] = C_GOLD_MID
    px[17, 18] = C_GOLD_DARK

    px[14, 21] = C_GOLD_DARK
    px[15, 21] = C_GOLD_MID
    px[16, 21] = C_GOLD_MID
    px[17, 21] = C_GOLD_DARK

    px[15, 19] = C_OUTLINE
    px[16, 19] = C_OUTLINE
    px[15, 20] = C_OUTLINE

    for x in (11, 12, 19, 20):
        px[x, 18] = (220, 160, 60, 255)

    return img

if __name__ == "__main__":
    chest = create_team_chest()
    targets = [
        "ArtShowcase/raw/chest.png",
        "My project/Assets/Resources/Ember/Props/chest.png",
        "Builds/UiVerificationProject/Assets/Resources/Ember/Props/chest.png",
        "My project/Assets/Resources/Ember/Interface/chest.png",
        "Builds/UiVerificationProject/Assets/Resources/Ember/Interface/chest.png",
        "ArtShowcase/props_items/chest.png",
    ]
    for t in targets:
        os.makedirs(os.path.dirname(t), exist_ok=True)
        chest.save(t)
        print(f"Saved: {t}")

    # Zoomed 8x for showcase
    zoom_path = "ArtShowcase/zoomed_8x/chest.png"
    if os.path.exists(os.path.dirname(zoom_path)):
        zoomed = chest.resize((256, 256), Image.NEAREST)
        zoomed.save(zoom_path)
        print(f"Saved: {zoom_path}")

    if os.path.exists("chest_test.png"):
        os.remove("chest_test.png")
