# Silver Wanderer: 32-pixel reconstruction

Source of visual identity: the user-approved `../silver_wanderer_concept/silver_wanderer_v2_no_arms.png`.

`silver_wanderer_master.pix2` and matching PNG are the canonical right-facing 32 x 32 still. `silver_wanderer_views.pix2` has one column and two rows: right, then horizontally mirrored left. These are static poses, not animation clips. The source code contains an editable character map and face cleanup coordinates. No runtime resampling is needed.

The 17-color palette and integer geometry are a reconstruction, not a lossless conversion of the large generated concept. Hair, face, crescent and outline necessarily use fewer pixels. Hair and coat hide both arms; short legs and separate boots remain visible. The draft is isolated from active game art pending review of the reconstructed proportions.

Five editable material layers: outline, hat/coat, silver hair/metal, skin, wine accents. They composite without overlap. Metadata and PNG have matching cell positions. Official Noiradix strict parsing, pixel-preserving serialize/reparse and every-pixel PNG/compositor comparison passed for both documents; `validation.json` records the raster evidence. The approved source image was read only.

Rebuild from the project root:

```powershell
python art/silver_wanderer_32/source/build.py
node tools/verify_pix2.cjs art/silver_wanderer_32/silver_wanderer_master.pix2 art/silver_wanderer_32/silver_wanderer_views.pix2
python tools/check_pix2_png.py
```

`comparison.png` compares the approved large concept with the native master enlarged using nearest-neighbor. `preview.png` shows both directions enlarged and at native size.
