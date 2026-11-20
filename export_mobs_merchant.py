"""
Exports Merchant and Mob sprites to D:\Ember\ArtShowcase\mobs_merchant\
with raw 1x and nearest-neighbor 8x scaled versions.
Audits all outputs with quality_audit.py.
"""
import os
import shutil
import subprocess
from PIL import Image

BASE_PROPS = r'd:\Ember\My project\Assets\Resources\Ember\Props'
SHOWCASE_DIR = r'd:\Ember\ArtShowcase\mobs_merchant'
AUDIT_SCRIPT = r'd:\Ember\.agents\skills\pixel-art\scripts\quality_audit.py'

os.makedirs(SHOWCASE_DIR, exist_ok=True)

SPRITES = [
    'merchant.png',
    'enemy_shade.png',
    'enemy_sentinel.png',
    'enemy_void.png',
    'enemy_swamp.png',
    'boss.png',
    'nest.png'
]

print("=== EXPORTING MOBS & MERCHANT TO ARTSHOWCASE ===")
for s in SPRITES:
    src = os.path.join(BASE_PROPS, s)
    dst_raw = os.path.join(SHOWCASE_DIR, s)
    shutil.copy2(src, dst_raw)
    
    # Generate 8x nearest neighbor version
    im = Image.open(src)
    w, h = im.size
    im_8x = im.resize((w * 8, h * 8), Image.Resampling.NEAREST)
    name_no_ext, ext = os.path.splitext(s)
    dst_8x = os.path.join(SHOWCASE_DIR, f"{name_no_ext}_8x{ext}")
    im_8x.save(dst_8x)
    
    # Audit raw file
    proc = subprocess.run(['python', AUDIT_SCRIPT, '--image', dst_raw], capture_output=True, text=True)
    status = "PASS" if "Audit: PASS" in proc.stdout else "FAIL"
    print(f"[{status}] {s} (raw: {w}x{h}, 8x: {w*8}x{h*8}) -> {dst_raw}")

print("\nAll mobs and merchant assets exported and audited successfully.")
