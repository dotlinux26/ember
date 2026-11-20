import os, json, subprocess
from PIL import Image

sprites = [
    ('merchant.png', 192, 48, 4, 48, 48),
    ('enemy_shade.png', 128, 32, 4, 32, 32),
    ('enemy_sentinel.png', 128, 32, 4, 32, 32),
    ('enemy_void.png', 128, 32, 4, 32, 32),
    ('enemy_swamp.png', 128, 32, 4, 32, 32),
    ('boss.png', 256, 64, 4, 64, 64),
    ('nest.png', 32, 32, 1, 32, 32),
]

base_props = r'd:\Ember\My project\Assets\Resources\Ember\Props'
audit_script = r'd:\Ember\.agents\skills\pixel-art\scripts\quality_audit.py'

print('=== COMPREHENSIVE SPRITE AUDIT ===')
all_pass = True
for name, exp_w, exp_h, exp_frames, fw, fh in sprites:
    path = os.path.join(base_props, name)
    if not os.path.exists(path):
        print(f'[FAIL] {name} does not exist!')
        all_pass = False
        continue
    
    im = Image.open(path)
    w, h = im.size
    dim_ok = (w == exp_w and h == exp_h)
    
    # Audit via quality_audit.py
    proc = subprocess.run(['python', audit_script, '--image', path, '--json'], capture_output=True, text=True)
    try:
        report = json.loads(proc.stdout)
    except Exception as e:
        report = {'status': 'error', 'message': str(e)}
    
    # Frame animation check
    frame_diffs = []
    if exp_frames > 1:
        frames = [im.crop((i*fw, 0, (i+1)*fw, fh)) for i in range(exp_frames)]
        for i in range(exp_frames - 1):
            d = sum(1 for p1, p2 in zip(frames[i].getdata(), frames[i+1].getdata()) if p1 != p2)
            frame_diffs.append(d)
        anim_ok = all(d > 0 for d in frame_diffs)
    else:
        anim_ok = True
    
    passed = dim_ok and (report.get('status') == 'pass') and anim_ok
    all_pass = all_pass and passed
    
    status_str = '[PASS]' if passed else '[FAIL]'
    qa_status = report.get('status')
    aa_clean = report.get('antialiasing', {}).get('clean')
    orp_clean = report.get('orphan_pixels', {}).get('clean')
    print(f'{status_str} {name}: dim=({w}x{h}, exp {exp_w}x{exp_h}), QA={qa_status}, AA_clean={aa_clean}, orphan_clean={orp_clean}, anim_diffs={frame_diffs}')

print('Overall status:', 'ALL PASS' if all_pass else 'FAILURES FOUND')
