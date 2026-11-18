# Ember — compact native 32px designs

This supersedes the 64px tracing experiment and the first 32px drafts. The current direction is original, compact character design informed by the silhouette, proportions and color clustering of the Soul Knight reference supplied by the user. No Soul Knight pixels are included in these sprites.

- 32×32 transparent cells, actual idle silhouette about 18–20 pixels wide and 23 high.
- Three distinct heads: ivory bell scout, teal cap and human face for the medic, broad gold-trimmed stone helm for the warden.
- Large head, short torso, four-pixel boots, consistent one-pixel outlines, no antialiasing.
- Independent trailing scarf and boots. No built-in weapons or floating hands: those are runtime overlays.
- Four frames per state, two directions, idle/run/cast/hurt. The left row mirrors the right exactly. Each character has its own palette-consistent PIX2 hierarchy.
- `design_plate.png` compares 6× nearest-neighbor and 1×. `preview.html` switches states and directions.
- The builder reuses the tested PIX2 export code in `../bell_vessels/source/build_sprites.py`; design geometry is entirely in `source/build.py`.

Rebuild from D:/Ember:

```powershell
python art/ember32/source/build.py
node art/ember32/source/validate.cjs
python art/ember32/source/check_png.py
```

`validation.json` records strict official parser acceptance, pixel-preserving round trip and mirroring; PNGs are compared against the official compositor at every coordinate. Technical validity does not imply that the visual design has been approved by the user.

Historical art folders are retained. The Unity runtime loads only `Assets/Resources/Ember/Active`, populated by this builder. The large images under `Resources/Ember/Characters` are an archived experiment and must not be used as the current art direction.
