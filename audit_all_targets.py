import subprocess
import sys

targets = [
    ('My project/Assets/Resources/Ember/Props/boss.png', 64),
    ('ArtShowcase/raw/boss.png', 64),
    ('My project/Assets/Resources/Ember/Props/torch_fire.png', 32),
    ('ArtShowcase/raw/torch_fire.png', 32),
    ('My project/Assets/Resources/Ember/Props/wildflowers.png', 8),
    ('ArtShowcase/raw/wildflowers.png', 8),
    ('My project/Assets/Resources/Ember/Props/stone_pebble.png', 8),
    ('ArtShowcase/raw/stone_pebble.png', 8),
    ('My project/Assets/Resources/Ember/Props/mushroom.png', 8),
    ('ArtShowcase/raw/mushroom.png', 8),
]

all_pass = True
for path, grid in targets:
    cmd = ['python', '.agents/skills/pixel-art/scripts/quality_audit.py', '--image', path, '--grid', str(grid)]
    res = subprocess.run(cmd, capture_output=True, text=True)
    out = res.stdout.strip()
    status = 'PASS' if 'PASS' in out else 'FAIL'
    if status == 'FAIL':
        all_pass = False
        print(f'FAIL {path}: {out}')
    else:
        print(f'PASS {path} (grid={grid})')

if all_pass:
    print('\nOVERALL QUALITY AUDIT: 100% PASSED (0 AA, 0 orphan pixels)')
    sys.exit(0)
else:
    print('\nOVERALL QUALITY AUDIT: SOME FAILED')
    sys.exit(1)
