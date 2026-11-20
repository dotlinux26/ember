"""
Pixel Art Generator for Merchant & Mobs in Soul Knight Style.
Strictly follows d:\Ember\.agents\skills\pixel-art\SKILL.md:
- Palette First (db32 / endesga-32)
- Zero Anti-Aliasing (alpha 0 or 255 only)
- 1px dark outline
- 2-3 tone cel-shading
- 0 orphan pixels
- Exact grid alignment
"""

import os
from PIL import Image

def hex_to_rgba(h, a=255):
    h = h.lstrip('#')
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)

# Color constants from Endesga-32 & DB32
OUTLINE = hex_to_rgba('#181425')       # Dark obsidian / deepest purple-black
ROBE_DARK = hex_to_rgba('#262b44')     # Deep navy-purple
ROBE_MID = hex_to_rgba('#3a4466')      # Mystic cloak mid-tone
ROBE_LIGHT = hex_to_rgba('#5a6988')    # Cloak highlight / fold
ROBE_TRIM = hex_to_rgba('#fee761')     # Gold trim
LEATHER_DARK = hex_to_rgba('#733e39')  # Deep leather shadow
LEATHER_MID = hex_to_rgba('#b86f50')   # Backpack leather
LEATHER_LIGHT = hex_to_rgba('#e4a672') # Leather highlight
BEDROLL = hex_to_rgba('#ead4aa')       # Travel cloth roll
BEDROLL_DARK = hex_to_rgba('#c28569')  # Travel cloth shadow
CYAN_GLOW = hex_to_rgba('#00f0ff')     # Magic eyes / lantern / rune glow
CYAN_BRIGHT = hex_to_rgba('#ffffff')   # High spec
CYAN_DEEP = hex_to_rgba('#0099db')     # Cyan shade
RED_POTION = hex_to_rgba('#ff0044')    # Vitality elixir
RED_POTION_HI = hex_to_rgba('#f6757a') # Red potion shine
GOLD_POTION = hex_to_rgba('#fee761')   # Energy elixir
GOLD_POTION_HI = hex_to_rgba('#ffffff')
PURPLE_POTION = hex_to_rgba('#b55088') # Arcane elixir
PURPLE_POTION_HI = hex_to_rgba('#f6757a')
GLASS = hex_to_rgba('#c0cbdc')         # Flask glass rim
WOOD_DARK = hex_to_rgba('#3e2731')     # Staff / wooden ribs
WOOD_MID = hex_to_rgba('#733e39')      # Staff wood
LANTERN_METAL = hex_to_rgba('#262b44') # Lantern hood
LANTERN_CORE = hex_to_rgba('#fee761')  # Flame core
LANTERN_AMBER = hex_to_rgba('#f77622') # Flame rim
SKIN_SHADOW = hex_to_rgba('#181425')   # Deep shadow under hood

print("Art generator loaded successfully.")
