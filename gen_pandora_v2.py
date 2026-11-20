"""
PANDORA/ABANDONED MYSTERY CHEST SPRITE GENERATOR v2
Soul Knight / ChillyRoom style - 32x32 - Zero AA - Dark Fantasy

Improvements over v1:
- Cleaner silhouette with more defined lid arc
- Stronger skull lock with better proportioned skull
- Richer wood grain texture using systematic color variation
- More prominent brass trims and rivets
- Open chest: spectacular treasure with crystals and magical aura
- Zero orphan pixels guaranteed by design
"""
from PIL import Image

# === LOCKED-IN PALETTE ===
T  = (0,   0,   0,   0)    # Transparent
BK = (14,  10,  18, 255)    # Black outline / deepest shadow

# Wood — 5-step warm earthy mahogany
W0 = (36,  18,  20, 255)    # Plank groove (darkest)
W1 = (60,  30,  30, 255)    # Shadow
W2 = (100, 50,  44, 255)    # Midtone
W3 = (148, 76,  60, 255)    # Highlight
W4 = (192, 112, 84, 255)    # Specular

# Iron band — 4-step cold forge steel
I0 = (34,  40,  52, 255)    # Iron shadow
I1 = (58,  70,  90, 255)    # Iron mid
I2 = (96, 112, 142, 255)    # Iron highlight
I3 = (150, 170, 200, 255)   # Iron specular

# Brass/Bronze rivet — 4-step warm antique
B0 = (96,  60,  18, 255)    # Brass deep shadow
B1 = (162, 112, 36, 255)    # Brass mid
B2 = (220, 170, 56, 255)    # Brass highlight
B3 = (255, 230, 140, 255)   # Brass super specular / sparkle

# Skull bone — 4-step bleached bone
S0 = (90,  94, 108, 255)    # Bone shadow
S1 = (148, 156, 172, 255)   # Bone mid
S2 = (210, 215, 228, 255)   # Bone highlight
S3 = (248, 250, 255, 255)   # Bone specular

# Neon Cyan eye glow
E0 = (0,  110, 140, 255)    # Cyan deep
E1 = (0,  200, 230, 255)    # Cyan bright
E2 = (160, 250, 255, 255)   # Cyan hot core

# Ground shadow
GS = (20,  16,  26, 180)    # Ground contact shadow OPAQUE version
GS2= (12,   8,  18, 255)    # Cast shadow (use 255 alpha)

# === OPEN CHEST EXCLUSIVE ===
G0 = (130,  80, 20, 255)    # Gold deep
G1 = (210, 148, 36, 255)    # Gold mid
G2 = (248, 210, 56, 255)    # Gold light
G3 = (255, 248, 164, 255)   # Gold glint

P0 = (88,  28, 120, 255)    # Void purple deep
P1 = (158, 60, 210, 255)    # Void purple mid
P2 = (218, 120, 255, 255)   # Void purple bright

WH = (255, 255, 255, 255)   # Pure white sparkle


def put(pixels, x, y, c, w=32, h=32):
    if 0 <= x < w and 0 <= y < h:
        pixels[x, y] = c


def draw_rect(pixels, x1, y1, x2, y2, fill, border=None, border_right=None, border_top=None):
    for y in range(y1, y2 + 1):
        for x in range(x1, x2 + 1):
            pixels[x, y] = fill
    if border is not None:
        # Left and right border
        for y in range(y1, y2 + 1):
            pixels[x1, y] = border
            pixels[x2, y] = border_right if border_right else border
    if border_top is not None:
        for x in range(x1, x2 + 1):
            pixels[x, y1] = border_top


def add_outline(img):
    """Add crisp 1px black outline around all opaque non-ground pixels"""
    temp = img.copy()
    tp = temp.load()
    pixels = img.load()
    for y in range(32):
        for x in range(32):
            if tp[x, y][3] > 0:
                continue
            has_opaque = False
            for dx, dy in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
                nx, ny = x + dx, y + dy
                if 0 <= nx < 32 and 0 <= ny < 32 and tp[nx, ny][3] == 255:
                    has_opaque = True
                    break
            if has_opaque:
                pixels[x, y] = BK


def create_closed_chest():
    img = Image.new("RGBA", (32, 32), T)
    p = img.load()

    # === STRUCTURE LAYOUT ===
    # Lid:   y=8..15  (8px tall, arched top)
    # Hinge band: y=15..16
    # Body:  y=16..26 (11px tall)
    # Foot:  y=27     (1px footer rim)
    # Ground: y=28..29

    # Ground shadow - two rows of opaque pixels (no orphans)
    for x in range(5, 27):
        p[x, 28] = GS2
    for x in range(7, 25):
        p[x, 29] = GS2

    # === CHEST BODY (y=16..27) ===
    # Base wood fill
    for y in range(16, 27):
        for x in range(5, 27):
            p[x, y] = W2

    # Light source: top-right → bottom-left shadow
    for y in range(16, 27):
        # Left shadow zone (x=5..9)
        for x in range(5, 8):
            p[x, y] = W1
        p[5, y] = W0
        # Right highlight zone (x=20..26)
        for x in range(21, 26):
            p[x, y] = W3
        p[25, y] = W4

    # === BODY METAL BANDING ===
    # Three vertical iron bands: left (x=5..7), center (x=14..17), right (x=24..26)

    def iron_band_v(x1, x2, y1, y2):
        for y in range(y1, y2 + 1):
            for x in range(x1, x2 + 1):
                p[x, y] = I1
            p[x1, y] = I0
            p[x2, y] = I2

    iron_band_v(5, 7, 16, 26)
    iron_band_v(14, 17, 16, 26)
    iron_band_v(24, 26, 16, 26)
    # Iron band right edge specular
    for y in range(17, 25):
        p[26, y] = I3

    # Bottom foot rim (y=26..27)
    for x in range(5, 27):
        p[x, 26] = I1
    for x in range(5, 27):
        p[x, 27] = I0
    for x in range(17, 26):
        p[x, 26] = I2
    p[25, 26] = I3

    # Brass rivets on body bands
    rivets_body = [
        (6, 18), (6, 23),   # left band
        (25, 18), (25, 23), # right band
        (15, 23), (16, 23), # center band
        (10, 26), (21, 26), # bottom rim
    ]
    for rx, ry in rivets_body:
        p[rx, ry] = B2
        p[rx, ry + 1] = B0 if ry + 1 <= 27 else B0

    # === CHEST LID (y=8..15) ===
    # Lid arch: wider in middle, narrows at top
    lid_shape = {
        8:  (12, 19),  # very top of arc (narrower, 8px wide)
        9:  (10, 21),  # arc step
        10: (8, 23),   # widening
        11: (6, 25),   # near-full width
        12: (5, 26),   # full width
        13: (5, 26),
        14: (5, 26),
        15: (5, 26),   # lid bottom rim
    }

    for y, (x1, x2) in lid_shape.items():
        for x in range(x1, x2 + 1):
            p[x, y] = W2

    # Lid lighting (top-right brightest)
    for y, (x1, x2) in lid_shape.items():
        mid = (x1 + x2) // 2
        # Left shadow
        for x in range(x1, min(x1 + 2, x2)):
            p[x, y] = W1
        p[x1, y] = W0
        # Right highlight
        for x in range(max(x2 - 2, mid + 1), x2 + 1):
            p[x, y] = W3
        p[x2, y] = W4

    # Lid metal bands (same x as body bands, top to bottom of lid)
    for y, (lx1, lx2) in lid_shape.items():
        # Left band
        if lx1 <= 7:
            for bx in range(lx1, min(8, lx2 + 1)):
                p[bx, y] = I1
            p[lx1, y] = I0
            if 7 <= lx2:
                p[7, y] = I2
        # Center band
        if lx1 <= 14 and lx2 >= 17:
            for bx in range(14, 18):
                p[bx, y] = I1
            p[14, y] = I0
            p[17, y] = I2
        # Right band
        if lx2 >= 24:
            for bx in range(max(24, lx1), lx2 + 1):
                p[bx, y] = I1
            p[lx2, y] = I3
            if lx2 - 1 >= 24:
                p[lx2 - 1, y] = I2
            p[24, y] = I0

    # Lid top crown row specular
    for x in range(13, 20):
        p[x, 8] = W3 if x < 17 else W4

    # Hinge join line at y=15 (bottom of lid)
    for x in range(5, 27):
        p[x, 15] = I1
    for x in range(17, 25):
        p[x, 15] = I2
    p[25, 15] = I3

    # === SKULL PADLOCK (centered on lid front, y=10..14, x=12..19) ===
    # Skull shape: a stylized chibi skull with big glowing eye sockets
    skull_coords = {
        # (x, y): color
        # Cranium
        (13, 10): S2, (14, 10): S3, (15, 10): S3, (16, 10): S3, (17, 10): S2, (18, 10): S1,
        (12, 11): S1, (13, 11): S2, (14, 11): S3, (15, 11): S3, (16, 11): S3, (17, 11): S2, (18, 11): S1, (19, 11): S0,
        (12, 12): S1, (13, 12): S2,                                             (17, 12): S2, (18, 12): S2, (19, 12): S0,
        # Eye sockets
        (14, 12): E2, (15, 12): E1,   (16, 12): E1, (17, 12): S2,
        (13, 12): S2, (14, 12): E2,
        # Cheekbones
        (12, 13): S0, (13, 13): S1, (14, 13): S1, (15, 13): S1, (16, 13): S1, (17, 13): S1, (18, 13): S1, (19, 13): S0,
        # Jaw with teeth gap
        (13, 14): S2, (14, 14): S1, (15, 14): BK, (16, 14): BK, (17, 14): S1, (18, 14): S2,
    }

    # First draw skull background rows
    for y in range(10, 15):
        pass

    # Then draw eye sockets as 2x2 blocks (zero orphan risk)
    # Left eye: x=14..15, y=12
    p[14, 12] = E2
    p[15, 12] = E1
    p[14, 11] = E0
    p[15, 11] = E1

    # Right eye: x=16..17, y=12
    p[16, 12] = E1
    p[17, 12] = E0
    p[16, 11] = E1
    p[17, 11] = E2

    # Skull outer shape fill
    skull_fill = [
        # y=10 cranium top
        (13,10,S2),(14,10,S3),(15,10,S3),(16,10,S3),(17,10,S2),(18,10,S1),
        # y=11 cranium wide
        (12,11,S1),(13,11,S2),(18,11,S1),(19,11,S0),
        # y=12 cheek body
        (12,12,S0),(13,12,S1),(18,12,S2),(19,12,S0),
        # y=13 jaw/tooth row
        (12,13,S0),(13,13,S1),(14,13,S1),(15,13,S1),(16,13,S1),(17,13,S2),(18,13,S1),(19,13,S0),
        # y=14 chin/teeth with 2 dark gaps
        (13,14,S2),(14,14,S1),(15,14,BK),(16,14,BK),(17,14,S1),(18,14,S2),
    ]
    for sx, sy, sc in skull_fill:
        p[sx, sy] = sc

    # Shackle/keyhole loop below skull (2x2 so no orphan)
    p[15, 15] = B2
    p[16, 15] = B2  # merged with hinge row (overrides white band color)
    p[15, 14] = B1  # just above chin

    # === BRASS CENTER PLATE behind skull ===
    # 2px brass backdrop for skull (connects to bands)
    for y in range(10, 15):
        p[12, y] = B0  # left connection  
        p[19, y] = B0  # right connection

    # Lid brass rivets
    rivets_lid = [
        (6, 12), (6, 10),   # left band
        (25, 12), (25, 10), # right band
    ]
    for rx, ry in rivets_lid:
        p[rx, ry] = B2
        p[rx, ry + 1] = B1

    # === OUTER CONTOUR ===
    add_outline(img)
    return img


def create_open_chest():
    img = Image.new("RGBA", (32, 32), T)
    p = img.load()

    # Ground shadow
    for x in range(5, 27):
        p[x, 28] = GS2
    for x in range(7, 25):
        p[x, 29] = GS2

    # === CHEST BODY BASE (y=16..27) ===
    for y in range(16, 27):
        for x in range(5, 27):
            p[x, y] = W2

    # Body shading
    for y in range(16, 27):
        for x in range(5, 8):
            p[x, y] = W1
        p[5, y] = W0
        for x in range(21, 27):
            p[x, y] = W3
        p[26, y] = W4

    # Body iron bands
    def iron_band_v(x1, x2, y1, y2):
        for y in range(y1, y2 + 1):
            for x in range(x1, x2 + 1):
                p[x, y] = I1
            p[x1, y] = I0
            p[x2, y] = I2

    iron_band_v(5, 7, 16, 26)
    iron_band_v(14, 17, 16, 26)
    iron_band_v(24, 26, 16, 26)
    for y in range(17, 25):
        p[26, y] = I3

    # Bottom rim
    for x in range(5, 27):
        p[x, 26] = I1
        p[x, 27] = I0
    for x in range(17, 26):
        p[x, 26] = I2
    p[25, 26] = I3

    # Body rivets
    rivets_body = [(6, 18), (6, 23), (25, 18), (25, 23), (15, 23), (16, 23), (10, 26), (21, 26)]
    for rx, ry in rivets_body:
        p[rx, ry] = B2
        p[rx, ry + 1] = B0

    # === LID THROWN OPEN (y=3..13 = 11px, tilted backward showing underside) ===
    # Lid is thrown open roughly 50-60°. It arcs outward, upper part tips backward.
    # Lid arc: visible as a trapezoidal slab leaning back
    # Front bottom edge of open lid is around y=13 (above chest opening rim at y=16)
    # Top edge is around y=3..5

    # Open lid exterior top (the dark underside visible from below):
    # Row y=3: tiny top cap (x=10..21)
    for x in range(10, 22):
        p[x, 3] = I1
    for x in range(13, 19):
        p[x, 3] = I2
    p[18, 3] = I3

    # y=4..5: lid top plane with iron
    for x in range(8, 24):
        p[x, 4] = I1
    for x in range(14, 22):
        p[x, 4] = I2
    p[22, 4] = I3

    for x in range(7, 25):
        p[x, 5] = I1
    for x in range(15, 23):
        p[x, 5] = I2
    p[23, 5] = I3

    # y=6..12: Underside of lid (dark wood interior showing through)
    for y in range(6, 13):
        for x in range(6, 26):
            p[x, y] = W0
        # Wood plank grooves on underside
        for lx in [9, 13, 17, 21]:
            if 6 <= lx <= 25:
                p[lx, y] = BK

    # Lip shadow on underside edges
    for y in range(6, 13):
        p[6, y] = BK
        p[25, y] = W1

    # === OPENED CHEST LID RIM / FRONT EDGE (y=13..15) ===
    # The hinge connection at y=14..15 transitioning lid to body
    for x in range(5, 27):
        p[x, 13] = I0  # dark inner edge
        p[x, 14] = I1
        p[x, 15] = I1
    for x in range(16, 25):
        p[x, 14] = I2
    p[25, 14] = I3
    p[25, 15] = I3

    # Hinge detail (left and right)
    for y in range(13, 16):
        p[7, y] = B1
        p[8, y] = B2
        p[23, y] = B1
        p[24, y] = B2
    p[8, 14] = B3
    p[24, 14] = B3

    # === INNER CAVITY - MAGICAL TREASURE (y=13..17) ===
    # Dark interior base
    for y in range(16, 18):
        for x in range(8, 24):
            p[x, y] = BK

    # Gold coins / pile
    coin_layer = [
        (9, 17, G1), (10, 17, G2), (11, 17, G1), (12, 17, G2), (13, 17, G3),
        (20, 17, G1), (21, 17, G2), (22, 17, G1), (23, 17, G1),
        (9, 16, G0), (10, 16, G1), (11, 16, G2), (12, 16, G1),
        (20, 16, G0), (21, 16, G1), (22, 16, G2), (23, 16, G1),
    ]
    for cx, cy, cc in coin_layer:
        p[cx, cy] = cc

    # Gold ingot bar (x=19..23, y=15..16)
    for x in range(19, 24):
        p[x, 15] = G2
    for x in range(20, 24):
        p[x, 14] = G1
    p[22, 14] = G3
    p[23, 15] = G3

    # === PANDORA CRYSTAL CENTERPIECE (x=13..18, y=11..16) ===
    # Cosmic cyan gemstone cluster rising from treasure
    crystal_map = [
        # Base 2x2 block
        (15, 16, E0), (16, 16, E0),
        # Stem
        (15, 15, E1), (16, 15, E0),
        (15, 14, E1), (16, 14, E1),
        # Crystal shard body
        (14, 13, E0), (15, 13, E2), (16, 13, E1), (17, 13, E0),
        (14, 12, E0), (15, 12, E2), (16, 12, E2), (17, 12, E1),
        # Crystal tip pair
        (15, 11, WH), (16, 11, E2),
        (15, 10, E2), (16, 10, E1),
        # Flare sparkle
        (14, 11, E1), (17, 11, E1),
        (13, 12, E0), (18, 12, E0),
    ]
    for cx, cy, cc in crystal_map:
        p[cx, cy] = cc

    # Side void crystal (small, x=11..12, y=14..15)
    p[11, 14] = P2
    p[12, 14] = P1
    p[11, 15] = P0
    p[12, 15] = P1

    # Right gold gem (x=19..20, y=14..15)
    p[19, 14] = G3
    p[20, 14] = WH
    p[19, 15] = G2
    p[20, 15] = G3

    # === MAGICAL STARLIGHT WISPS RISING FROM CRYSTAL ===
    # Groups of 2x1 and 2x2 to avoid orphans
    wisps = [
        # Center wisps (cyan) rising tall
        (15, 9, E1), (16, 9, E2),
        (15, 8, E0), (16, 8, E1),
        (16, 7, E2), (17, 7, E1),
        (16, 6, E1), (17, 6, E0),
        # Violet soul wisp left
        (12, 10, P1), (13, 10, P2),
        (12, 9, P0), (13, 9, P1),
        # Cyan sparkle right
        (19, 8, E1), (20, 8, E2),
        (19, 7, E0), (20, 7, E1),
        # Tiny top sparkle
        (17, 5, WH), (18, 5, E2),
        (17, 4, E1), (18, 4, WH),
    ]
    # Only draw wisps that don't overlap existing structure pixels
    for wx, wy, wc in wisps:
        if p[wx, wy] == T:
            p[wx, wy] = wc

    # === LID FRONT FACE (visible strip, y=12..15 on outer edge) ===
    # Already handled by lid arc

    # === OUTER CONTOUR ===
    add_outline(img)
    return img


if __name__ == "__main__":
    closed = create_closed_chest()
    closed.save("test_chest_closed_v2.png")

    opened = create_open_chest()
    opened.save("test_chest_open_v2.png")

    print("Generated test_chest_closed_v2.png and test_chest_open_v2.png")
