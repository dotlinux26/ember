# Silver Wanderer: ten native 48 x 48 design studies

These are editable native-resolution drawings, not enlarged copies of the rejected 32px asset. The approved silver-haired, black-clothed no-arms concept is the identity reference. The user subsequently requested 48px, short cute feet and ten different silhouettes.

Every numbered folder contains a right-facing and horizontally mirrored left-facing still in PNG, PIX2 and metadata; a separate master PNG for each direction; and a preview showing both enlarged and at 1x. The PIX2 grid has one column and two rows, not an animation. The root `all_10.pix2` and `all_10.png` are a five-column, two-row contact sheet of the ten right-facing designs, also not an animation.

| Number | Headgear | Hair | Clothing |
| --- | --- | --- | --- |
| 01 | Original crescent witch hat | Long flowing silver hair | Split coat |
| 02 | Curled-tip witch hat | Short bob | Poncho |
| 03 | Crescent beret | Side braid | Cropped traveler coat |
| 04 | Rounded lunar hood | Side locks | Poncho |
| 05 | Drooping mage cap | Side ponytail | Split coat |
| 06 | Compact witch hat | Twin tails | Cropped coat |
| 07 | Crescent tiara | Short bob | Bell-shaped dress coat |
| 08 | Wide traveler brim | Side braid | Long coat |
| 09 | Ribbon headpiece | Side ponytail | Cropped coat |
| 10 | Pointed fox hood | Side locks | Bell-shaped dress coat |

All designs share warm rounded cheeks, red eyes, white highlights, a smile or wink, silver hair, dark clothing, hidden arms, and short boots. Their outlines and color regions differ; these are not merely palette swaps. The left views are mirrors, not independently drawn asymmetric costume turnarounds. No gameplay asset has been replaced.

The native pixel maps and integer geometry live in `source/build.py`. Five disjoint editable material layers preserve exact PNG/PIX2 composition. The palette is a controlled reconstruction of the concept, not a claim of lossless recovery of its original 47,938 colors. `geometry_evidence.json` records differences between all pairs of designs. `validation.json` records official-parser raster data for all eleven PIX2 documents.

Verification completed: official Noiradix strict parse, bounds and unique-cell checks, serialize/reparse pixel equality, and PNG/compositor equality for every pixel in all documents. These tests verify files and layout, not artistic approval or in-game animation.

Rebuild from D:/Ember:

```powershell
python art/silver_wanderer_48/source/build.py
$studies = @(Get-ChildItem art/silver_wanderer_48 -Filter *.pix2 -Recurse | ForEach-Object { $_.FullName })
node tools/verify_pix2.cjs @studies
python tools/check_pix2_png.py
```
