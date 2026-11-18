const fs = require('fs');
const PIX2 = require('./tools/noiradix-studio/js/pix2-format.js');

const sample = `@pix2 1.1

@meta
  title: "Gothic Runic Pillar 64x64"
  author: "NOIRADIX Studio v2.1.0"
  version: "1.1"
  color_mode: "RGBA"
  coordinate_origin: "top-left"

@grid
  columns: 4
  rows: 4
  default_cell: 64x64
  background: none

@palette
  1: #0e0b16  ; 1: Deep Obsidian Outline
  2: #1e192c  ; 2: Dark Slate Shadow
  3: #342c48  ; 3: Carved Stone Body
  4: #544970  ; 4: Stone Midtone
  5: #7e709e  ; 5: Stone Edge Highlight
  6: #b8a9d6  ; 6: Rim Light
  7: #0e6e8a  ; 7: Dark Cyan Accent
  8: #2de6ff  ; 8: Glowing Cyan Rune
  9: #d8fbff  ; 9: Rune Starlight Highlight
  10: #a57832 ; 10: Ancient Gold Trim
  11: #f2cb68 ; 11: Gold Highlight

@cell cell_0_0
  name: "Pillar Frame 0"
  direction: down
  frame: 0
  grid_pos: 0,0
  @layer foundation_base
    cell: cell_0_0
    name: "Foundation Base"
    opacity: 1.0
    blend: normal
    @group stepped_plinth
      cell: cell_0_0
      parent: foundation_base
      name: "Stepped Plinth Group"
      tags: [architecture, base]
      @region bottom_slab
        cell: cell_0_0
        parent: stepped_plinth
        name: "Bottom Slab"
        tags: [stone, slab]
        rect 8,54 48,9 1 fill
        rect 10,55 44,7 3 fill
        line 10,55 53,55 5
        line 10,56 10,61 6
        line 53,56 53,61 2
      @region middle_slab
        cell: cell_0_0
        parent: stepped_plinth
        name: "Middle Slab"
        tags: [stone, slab]
        rect 14,48 36,7 1 fill
        rect 16,49 32,5 4 fill
        line 16,49 47,49 6
        line 16,50 16,53 5
        line 47,50 47,53 2
  @layer obelisk_shaft
    cell: cell_0_0
    name: "Obelisk Shaft"
    opacity: 1.0
    blend: normal
    @group pillar_body
      cell: cell_0_0
      parent: obelisk_shaft
      name: "Pillar Body"
      tags: [pillar, column]
      @region stone_shaft
        cell: cell_0_0
        parent: pillar_body
        name: "Stone Shaft"
        tags: [stone, masonry]
        rect 20,16 24,33 1 fill
        rect 22,18 20,29 3 fill
        line 22,18 41,18 5
        line 22,19 22,46 5
        line 23,19 23,46 4
        line 41,19 41,46 2
        line 32,18 32,46 2
      @region gold_astragal
        cell: cell_0_0
        parent: pillar_body
        name: "Gold Astragal"
        tags: [gold, trim]
        rect 18,14 28,4 1 fill
        rect 19,15 26,2 10 fill
        line 19,15 44,15 11
      @region runic_carvings
        cell: cell_0_0
        parent: pillar_body
        name: "Runic Carvings"
        tags: [runes, glow]
        p 27,24 8 p 28,24 9 p 29,24 8
        p 28,25 8 p 28,26 8
        p 26,27 8 p 27,27 8 p 28,27 9 p 29,27 8 p 30,27 8
        p 34,32 8 p 35,32 9 p 36,32 8
        p 35,33 8 p 35,34 8
        p 33,35 8 p 34,35 8 p 35,35 9 p 36,35 8 p 37,35 8
        p 27,40 8 p 28,40 9 p 29,40 8
        p 28,41 8 p 28,42 8
  @layer floating_crystals
    cell: cell_0_0
    name: "Floating Crystals"
    opacity: 1.0
    blend: normal
    @region west_shard
      cell: cell_0_0
      parent: floating_crystals
      name: "West Shard"
      tags: [crystal, floating]
      rect 10,22 6,10 1 fill
      rect 11,23 4,8 3 fill
      line 11,23 11,30 5
      line 14,24 14,30 2
      p 12,25 7 p 13,25 7
    @region east_shard
      cell: cell_0_0
      parent: floating_crystals
      name: "East Shard"
      tags: [crystal, floating]
      rect 48,22 6,10 1 fill
      rect 49,23 4,8 3 fill
      line 49,23 49,30 5
      line 52,24 52,30 2
      p 50,25 7 p 51,25 7
    @region apex_finial
      cell: cell_0_0
      parent: floating_crystals
      name: "Apex Finial"
      tags: [crystal, crown]
      rect 28,4 8,11 1 fill
      rect 29,5 6,9 10 fill
      line 29,5 34,5 11
      p 31,8 8 p 32,8 9
      p 31,9 8 p 32,9 8

@cell cell_0_1
  name: "Pillar Frame 1"
  direction: down
  frame: 1
  grid_pos: 0,1
  @layer foundation_base_1
    cell: cell_0_1
    name: "Foundation Base"
    opacity: 1.0
    blend: normal
    @group stepped_plinth_1
      cell: cell_0_1
      parent: foundation_base_1
      name: "Stepped Plinth Group"
      tags: [architecture, base]
      @region bottom_slab_1
        cell: cell_0_1
        parent: stepped_plinth_1
        name: "Bottom Slab"
        tags: [stone, slab]
        rect 8,54 48,9 1 fill
        rect 10,55 44,7 3 fill
        line 10,55 53,55 5
        line 10,56 10,61 6
        line 53,56 53,61 2
      @region middle_slab_1
        cell: cell_0_1
        parent: stepped_plinth_1
        name: "Middle Slab"
        tags: [stone, slab]
        rect 14,48 36,7 1 fill
        rect 16,49 32,5 4 fill
        line 16,49 47,49 6
        line 16,50 16,53 5
        line 47,50 47,53 2
  @layer obelisk_shaft_1
    cell: cell_0_1
    name: "Obelisk Shaft"
    opacity: 1.0
    blend: normal
    @group pillar_body_1
      cell: cell_0_1
      parent: obelisk_shaft_1
      name: "Pillar Body"
      tags: [pillar, column]
      @region stone_shaft_1
        cell: cell_0_1
        parent: pillar_body_1
        name: "Stone Shaft"
        tags: [stone, masonry]
        rect 20,16 24,33 1 fill
        rect 22,18 20,29 3 fill
        line 22,18 41,18 5
        line 22,19 22,46 5
        line 23,19 23,46 4
        line 41,19 41,46 2
        line 32,18 32,46 2
      @region gold_astragal_1
        cell: cell_0_1
        parent: pillar_body_1
        name: "Gold Astragal"
        tags: [gold, trim]
        rect 18,14 28,4 1 fill
        rect 19,15 26,2 10 fill
        line 19,15 44,15 11
      @region runic_carvings_1
        cell: cell_0_1
        parent: pillar_body_1
        name: "Runic Carvings"
        tags: [runes, glow]
        p 27,24 8 p 28,24 9 p 29,24 8
        p 28,25 8 p 28,26 8
        p 26,27 8 p 27,27 8 p 28,27 9 p 29,27 8 p 30,27 8
        p 34,32 8 p 35,32 9 p 36,32 8
        p 35,33 8 p 35,34 8
        p 33,35 8 p 34,35 8 p 35,35 9 p 36,35 8 p 37,35 8
        p 27,40 8 p 28,40 9 p 29,40 8
        p 28,41 8 p 28,42 8
  @layer floating_crystals_1
    cell: cell_0_1
    name: "Floating Crystals"
    opacity: 1.0
    blend: normal
    @region west_shard_1
      cell: cell_0_1
      parent: floating_crystals_1
      name: "West Shard"
      tags: [crystal, floating]
      rect 10,21 6,10 1 fill
      rect 11,22 4,8 3 fill
      line 11,22 11,29 5
      line 14,23 14,29 2
      p 12,24 7 p 13,24 7
    @region east_shard_1
      cell: cell_0_1
      parent: floating_crystals_1
      name: "East Shard"
      tags: [crystal, floating]
      rect 48,23 6,10 1 fill
      rect 49,24 4,8 3 fill
      line 49,24 49,31 5
      line 52,25 52,31 2
      p 50,26 7 p 51,26 7
    @region apex_finial_1
      cell: cell_0_1
      parent: floating_crystals_1
      name: "Apex Finial"
      tags: [crystal, crown]
      rect 28,3 8,11 1 fill
      rect 29,4 6,9 10 fill
      line 29,4 34,4 11
      p 31,7 8 p 32,7 9
      p 31,8 8 p 32,8 8

@cell cell_0_2
  name: "Pillar Frame 2"
  direction: down
  frame: 2
  grid_pos: 0,2
  @layer foundation_base_2
    cell: cell_0_2
    name: "Foundation Base"
    opacity: 1.0
    blend: normal
    @group stepped_plinth_2
      cell: cell_0_2
      parent: foundation_base_2
      name: "Stepped Plinth Group"
      tags: [architecture, base]
      @region bottom_slab_2
        cell: cell_0_2
        parent: stepped_plinth_2
        name: "Bottom Slab"
        tags: [stone, slab]
        rect 8,54 48,9 1 fill
        rect 10,55 44,7 3 fill
        line 10,55 53,55 5
        line 10,56 10,61 6
        line 53,56 53,61 2
      @region middle_slab_2
        cell: cell_0_2
        parent: stepped_plinth_2
        name: "Middle Slab"
        tags: [stone, slab]
        rect 14,48 36,7 1 fill
        rect 16,49 32,5 4 fill
        line 16,49 47,49 6
        line 16,50 16,53 5
        line 47,50 47,53 2
  @layer obelisk_shaft_2
    cell: cell_0_2
    name: "Obelisk Shaft"
    opacity: 1.0
    blend: normal
    @group pillar_body_2
      cell: cell_0_2
      parent: obelisk_shaft_2
      name: "Pillar Body"
      tags: [pillar, column]
      @region stone_shaft_2
        cell: cell_0_2
        parent: pillar_body_2
        name: "Stone Shaft"
        tags: [stone, masonry]
        rect 20,16 24,33 1 fill
        rect 22,18 20,29 3 fill
        line 22,18 41,18 5
        line 22,19 22,46 5
        line 23,19 23,46 4
        line 41,19 41,46 2
        line 32,18 32,46 2
      @region gold_astragal_2
        cell: cell_0_2
        parent: pillar_body_2
        name: "Gold Astragal"
        tags: [gold, trim]
        rect 18,14 28,4 1 fill
        rect 19,15 26,2 10 fill
        line 19,15 44,15 11
      @region runic_carvings_2
        cell: cell_0_2
        parent: pillar_body_2
        name: "Runic Carvings"
        tags: [runes, glow]
        p 27,24 8 p 28,24 9 p 29,24 8
        p 28,25 8 p 28,26 8
        p 26,27 8 p 27,27 8 p 28,27 9 p 29,27 8 p 30,27 8
        p 34,32 8 p 35,32 9 p 36,32 8
        p 35,33 8 p 35,34 8
        p 33,35 8 p 34,35 8 p 35,35 9 p 36,35 8 p 37,35 8
        p 27,40 8 p 28,40 9 p 29,40 8
        p 28,41 8 p 28,42 8
  @layer floating_crystals_2
    cell: cell_0_2
    name: "Floating Crystals"
    opacity: 1.0
    blend: normal
    @region west_shard_2
      cell: cell_0_2
      parent: floating_crystals_2
      name: "West Shard"
      tags: [crystal, floating]
      rect 10,20 6,10 1 fill
      rect 11,21 4,8 3 fill
      line 11,21 11,28 5
      line 14,22 14,28 2
      p 12,23 7 p 13,23 7
    @region east_shard_2
      cell: cell_0_2
      parent: floating_crystals_2
      name: "East Shard"
      tags: [crystal, floating]
      rect 48,24 6,10 1 fill
      rect 49,25 4,8 3 fill
      line 49,25 49,32 5
      line 52,26 52,32 2
      p 50,27 7 p 51,27 7
    @region apex_finial_2
      cell: cell_0_2
      parent: floating_crystals_2
      name: "Apex Finial"
      tags: [crystal, crown]
      rect 28,2 8,11 1 fill
      rect 29,3 6,9 10 fill
      line 29,3 34,3 11
      p 31,6 8 p 32,6 9
      p 31,7 8 p 32,7 8

@cell cell_0_3
  name: "Pillar Frame 3"
  direction: down
  frame: 3
  grid_pos: 0,3
  @layer foundation_base_3
    cell: cell_0_3
    name: "Foundation Base"
    opacity: 1.0
    blend: normal
    @group stepped_plinth_3
      cell: cell_0_3
      parent: foundation_base_3
      name: "Stepped Plinth Group"
      tags: [architecture, base]
      @region bottom_slab_3
        cell: cell_0_3
        parent: stepped_plinth_3
        name: "Bottom Slab"
        tags: [stone, slab]
        rect 8,54 48,9 1 fill
        rect 10,55 44,7 3 fill
        line 10,55 53,55 5
        line 10,56 10,61 6
        line 53,56 53,61 2
      @region middle_slab_3
        cell: cell_0_3
        parent: stepped_plinth_3
        name: "Middle Slab"
        tags: [stone, slab]
        rect 14,48 36,7 1 fill
        rect 16,49 32,5 4 fill
        line 16,49 47,49 6
        line 16,50 16,53 5
        line 47,50 47,53 2
  @layer obelisk_shaft_3
    cell: cell_0_3
    name: "Obelisk Shaft"
    opacity: 1.0
    blend: normal
    @group pillar_body_3
      cell: cell_0_3
      parent: obelisk_shaft_3
      name: "Pillar Body"
      tags: [pillar, column]
      @region stone_shaft_3
        cell: cell_0_3
        parent: pillar_body_3
        name: "Stone Shaft"
        tags: [stone, masonry]
        rect 20,16 24,33 1 fill
        rect 22,18 20,29 3 fill
        line 22,18 41,18 5
        line 22,19 22,46 5
        line 23,19 23,46 4
        line 41,19 41,46 2
        line 32,18 32,46 2
      @region gold_astragal_3
        cell: cell_0_3
        parent: pillar_body_3
        name: "Gold Astragal"
        tags: [gold, trim]
        rect 18,14 28,4 1 fill
        rect 19,15 26,2 10 fill
        line 19,15 44,15 11
      @region runic_carvings_3
        cell: cell_0_3
        parent: pillar_body_3
        name: "Runic Carvings"
        tags: [runes, glow]
        p 27,24 8 p 28,24 9 p 29,24 8
        p 28,25 8 p 28,26 8
        p 26,27 8 p 27,27 8 p 28,27 9 p 29,27 8 p 30,27 8
        p 34,32 8 p 35,32 9 p 36,32 8
        p 35,33 8 p 35,34 8
        p 33,35 8 p 34,35 8 p 35,35 9 p 36,35 8 p 37,35 8
        p 27,40 8 p 28,40 9 p 29,40 8
        p 28,41 8 p 28,42 8
  @layer floating_crystals_3
    cell: cell_0_3
    name: "Floating Crystals"
    opacity: 1.0
    blend: normal
    @region west_shard_3
      cell: cell_0_3
      parent: floating_crystals_3
      name: "West Shard"
      tags: [crystal, floating]
      rect 10,21 6,10 1 fill
      rect 11,22 4,8 3 fill
      line 11,22 11,29 5
      line 14,23 14,29 2
      p 12,24 7 p 13,24 7
    @region east_shard_3
      cell: cell_0_3
      parent: floating_crystals_3
      name: "East Shard"
      tags: [crystal, floating]
      rect 48,23 6,10 1 fill
      rect 49,24 4,8 3 fill
      line 49,24 49,31 5
      line 52,25 52,31 2
      p 50,26 7 p 51,26 7
    @region apex_finial_3
      cell: cell_0_3
      parent: floating_crystals_3
      name: "Apex Finial"
      tags: [crystal, crown]
      rect 28,3 8,11 1 fill
      rect 29,4 6,9 10 fill
      line 29,4 34,4 11
      p 31,7 8 p 32,7 9
      p 31,8 8 p 32,8 8

@animation pillar_idle
  name: "Pillar Levitation Idle"
  direction: down
  frames: [cell_0_0, cell_0_1, cell_0_2, cell_0_3]
  fps: 6
  loop: true
  mode: loop

@end`;

try {
  const doc = PIX2.parseValidated(sample);
  console.log('SUCCESS! PIX2.parseValidated passed. Cells:', Object.keys(doc.cells).length, 'Animations:', Object.keys(doc.animations).length);
} catch (err) {
  console.error('PARSE ERROR:', err.message);
}
