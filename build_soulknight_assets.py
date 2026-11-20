import os
import subprocess
from PIL import Image

def hex_rgba(h, a=255):
    h = h.lstrip('#')
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)

OUTLINE = hex_rgba('#14101c')
WHITE = hex_rgba('#ffffff')

# Wood ramp
WOOD_LIGHT = hex_rgba('#e4a672')
WOOD_MID = hex_rgba('#b86f50')
WOOD_DARK = hex_rgba('#733e39')
WOOD_DEEP = hex_rgba('#3e2731')

# Steel / Iron ramp
STEEL_WHITE = hex_rgba('#ffffff')
STEEL_LIGHT = hex_rgba('#a0b4cd')
STEEL_MID = hex_rgba('#627597')
STEEL_DARK = hex_rgba('#3a4466')
STEEL_DEEP = hex_rgba('#262b44')

# Fire ramp
FIRE_WHITE = hex_rgba('#ffffff')
FIRE_YELLOW = hex_rgba('#fee761')
FIRE_ORANGE = hex_rgba('#f77622')
FIRE_RED = hex_rgba('#ff0044')
FIRE_DARK = hex_rgba('#8c1428')

# Gold / Brass ramp
GOLD_LIGHT = hex_rgba('#fee761')
GOLD_MID = hex_rgba('#d9a066')
GOLD_DARK = hex_rgba('#8f563b')
GOLD_DEEP = hex_rgba('#532b1d')

# Cyan / Crystal ramp
CYAN_WHITE = hex_rgba('#ffffff')
CYAN_BRIGHT = hex_rgba('#00f0ff')
CYAN_MID = hex_rgba('#38bdf8')
CYAN_DEEP = hex_rgba('#0077b6')
CYAN_DARK = hex_rgba('#023e8a')

# Ruby / Red ramp
RED_HI = hex_rgba('#ff5566')
RED_MID = hex_rgba('#e62030')
RED_DARK = hex_rgba('#991122')
RED_DEEP = hex_rgba('#550515')

# Green / Plant ramp
LEAF_LIGHT = hex_rgba('#73ce60')
LEAF_MID = hex_rgba('#44a650')
LEAF_DARK = hex_rgba('#226830')

# Stone / Earth ramp
STONE_LIGHT = hex_rgba('#9c95b3')
STONE_MID = hex_rgba('#605878')
STONE_DARK = hex_rgba('#3d3752')

# Crystal Magenta ramp
MAGENTA_LIGHT = hex_rgba('#f6757a')
MAGENTA_MID = hex_rgba('#b55088')
MAGENTA_DARK = hex_rgba('#682852')

# Ivory / Stalk ramp
IVORY_LIGHT = hex_rgba('#faf6ee')
IVORY_MID = hex_rgba('#e4ddcc')
IVORY_DARK = hex_rgba('#b0a894')

COMMON_MAP = {
    '.': (0, 0, 0, 0),
    '#': OUTLINE,
    'W': WHITE,
    'w': WOOD_LIGHT,
    'm': WOOD_MID,
    'd': WOOD_DARK,
    'D': WOOD_DEEP,
    'S': STEEL_LIGHT,
    's': STEEL_MID,
    'i': STEEL_DARK,
    'I': STEEL_DEEP,
    'Y': FIRE_YELLOW,
    'O': FIRE_ORANGE,
    'F': FIRE_RED,
    'f': FIRE_DARK,
    'G': GOLD_LIGHT,
    'g': GOLD_MID,
    'b': GOLD_DARK,
    'B': GOLD_DEEP,
    'C': CYAN_BRIGHT,
    'c': CYAN_MID,
    'k': CYAN_DEEP,
    'R': RED_HI,
    'r': RED_MID,
    'p': RED_DARK,
    'P': RED_DEEP,
    'L': LEAF_LIGHT,
    'l': LEAF_MID,
    'e': LEAF_DARK,
    't': STONE_LIGHT,
    'T': STONE_MID,
    'u': STONE_DARK,
    'M': MAGENTA_LIGHT,
    'N': MAGENTA_MID,
    'V': IVORY_LIGHT,
    'v': IVORY_MID,
    'x': IVORY_DARK,
}

def parse_ascii(lines, cmap=COMMON_MAP):
    lines = [line.strip() for line in lines if line.strip()]
    h = len(lines)
    w = len(lines[0])
    img = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    for y, line in enumerate(lines):
        assert len(line) == w, fRow {y} len {len(line)} != {w}
        for x, ch in enumerate(line):
            col = cmap.get(ch, (0, 0, 0, 0))
            if col[3] > 0:
                img.putpixel((x, y), col)
    return img

print(Parser ready.)
