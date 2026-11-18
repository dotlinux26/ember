const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');
const PIX2 = require('./noiradix-studio/js/pix2-format.js');

// Helper to format a region with pixel plotting
function makeRegion(id, parent, name, tags, pLines) {
  let out = `    @region ${id}\n      cell: cell_0_0\n      parent: ${parent}\n      name: "${name}"\n`;
  if (tags && tags.length) out += `      tags: [${tags.join(', ')}]\n`;
  for (const line of pLines) {
    if (line.trim()) out += `      ${line.trim()}\n`;
  }
  return out;
}

// ============================================================================
// 1. ANCIENT ELDRITCH TREE (ancient_tree.pix2) - 48x64
// ============================================================================
function generateAncientTreePix2() {
  const palette = `  1: #0b0814  ; Dark silhouette outline
  2: #161224  ; Deep shadow bark
  3: #261f38  ; Weathered ancient bark
  4: #3c3252  ; Bark midtone
  5: #564872  ; Bark rim highlight
  6: #12211c  ; Foliage deep shadow
  7: #1c362d  ; Foliage shadow
  8: #294e41  ; Foliage midtone
  9: #3b6b59  ; Foliage highlight
  10: #54927a ; Foliage rim highlight
  11: #78c2a4 ; Bioluminescent moss / leaf tip
  12: #a8f0d4 ; Night spore glint
  13: #5c3a28 ; Root soil blend`;

  const trunkPixels = [];
  // Gnarled roots from y=46 to 62
  // Base roots spread wide: x=12..36
  for (let y = 44; y <= 62; y++) {
    const t = (y - 44) / 18.0;
    const halfW = 4.0 + t * 6.5;
    const minX = Math.floor(24 - halfW);
    const maxX = Math.floor(24 + halfW);
    const row = [];
    for (let x = minX; x <= maxX; x++) {
      if (x === minX || x === maxX || y === 62) {
        row.push(`p ${x},${y} 1`);
      } else {
        const edge = Math.min(x - minX, maxX - x);
        const col = edge === 1 ? '2' : (x < 24 ? (y % 3 === 0 ? '4' : '3') : '2');
        row.push(`p ${x},${y} ${col}`);
      }
    }
    // Gnarled root spurs at bottom
    if (y >= 54) {
      const leftSpur = minX - Math.floor((y - 54) * 0.9);
      const rightSpur = maxX + Math.floor((y - 54) * 0.8);
      if (leftSpur >= 6) row.push(`p ${leftSpur},${y} 1 p ${leftSpur + 1},${y} 3`);
      if (rightSpur <= 42) row.push(`p ${rightSpur},${y} 1 p ${rightSpur - 1},${y} 2`);
    }
    trunkPixels.push(row.join(' '));
  }

  // Foliage Canopy in 3 grand tiers: y=4..46
  const foliagePixels = [];
  // Tier 1 (top): y=4..18, Tier 2: y=16..32, Tier 3: y=28..46
  const tiers = [
    { startY: 4, endY: 18, maxR: 11, cx: 24 },
    { startY: 16, endY: 32, maxR: 16, cx: 24 },
    { startY: 28, endY: 46, maxR: 20, cx: 24 }
  ];

  for (const tier of tiers) {
    for (let y = tier.startY; y <= tier.endY; y++) {
      const progress = (y - tier.startY) / (tier.endY - tier.startY);
      const r = Math.sin(progress * Math.PI) * tier.maxR * 0.95 + progress * 2.5;
      const minX = Math.floor(tier.cx - r);
      const maxX = Math.floor(tier.cx + r);
      const row = [];
      for (let x = minX; x <= maxX; x++) {
        if (x < 0 || x >= 48) continue;
        if (x === minX || x === maxX || y === tier.startY) {
          row.push(`p ${x},${y} 1`);
        } else {
          // Foliage shading with dark fantasy tones
          const distFromCenter = Math.sqrt((x - tier.cx) ** 2 + ((y - tier.startY) * 0.7) ** 2);
          if (x < tier.cx - 2) {
            row.push(`p ${x},${y} ${distFromCenter < 6 ? '10' : distFromCenter < 12 ? '9' : '8'}`);
          } else {
            row.push(`p ${x},${y} ${distFromCenter < 6 ? '8' : distFromCenter < 12 ? '7' : '6'}`);
          }
        }
      }
      // Hanging moss strands / spores
      if (y === tier.endY && y < 45) {
        for (let sx = minX + 2; sx <= maxX - 2; sx += 4) {
          row.push(`p ${sx},${y + 1} 9 p ${sx},${y + 2} 11 p ${sx},${y + 3} 12`);
        }
      }
      foliagePixels.push(row.join(' '));
    }
  }

  return `@pix2 1.1

@meta
  title: "Ancient Eldritch Tree"
  author: "NOIRADIX Studio v2.1.0"
  version: "1.1"
  color_mode: "RGBA"
  coordinate_origin: "top-left"

@grid
  columns: 1
  rows: 1
  default_cell: 48x64
  background: none

@palette
${palette}

@cell cell_0_0
  name: "Ancient Tree"
  direction: down
  frame: 0
  grid_pos: 0,0
  @layer tree_trunk_layer
    cell: cell_0_0
    name: "Tree Trunk & Roots"
    opacity: 1.0
    blend: normal
    @group trunk_grp
      cell: cell_0_0
      parent: tree_trunk_layer
      name: "Trunk Group"
      tags: [trunk, roots]
${makeRegion('reg_trunk', 'trunk_grp', 'Gnarled Trunk', ['bark'], trunkPixels)}
  @layer tree_canopy_layer
    cell: cell_0_0
    name: "Tree Canopy Foliage"
    opacity: 1.0
    blend: normal
    @group canopy_grp
      cell: cell_0_0
      parent: tree_canopy_layer
      name: "Canopy Group"
      tags: [foliage, leaves]
${makeRegion('reg_canopy', 'canopy_grp', 'Eldritch Canopy', ['leaves'], foliagePixels)}

@end`;
}

// ============================================================================
// 2. RUNIC BEACON MONOLITH (runic_beacon.pix2) - 48x64
// ============================================================================
function generateRunicBeaconPix2() {
  const palette = `  1: #0a0814  ; Dark obsidian contour
  2: #161426  ; Deep shadow stone
  3: #28243e  ; Weathered carved stone
  4: #423c62  ; Stone midtone
  5: #635b8c  ; Stone highlight rim
  6: #7d4d2b  ; Ancient bronze fitting
  7: #c2883f  ; Polished runic bronze
  8: #f0c265  ; Golden runic engraving
  9: #083c50  ; Dark cyan energy shadow
  10: #12829c ; Cyan runic flame
  11: #2fe0f8 ; Brilliant cyan corona
  12: #a2f8ff ; Core pure starlight white
  13: #ffffff ; Pure flame highlight`;

  const basePixels = [];
  // 3-tiered pedestal base at y=46..62
  // Tier 1 (bottom slab): y=56..62, w=40 (x=4..43)
  for (let y = 56; y <= 62; y++) {
    const row = [];
    for (let x = 4; x <= 43; x++) {
      if (x === 4 || x === 43 || y === 56 || y === 62) row.push(`p ${x},${y} 1`);
      else if (x < 22) row.push(`p ${x},${y} ${y === 57 ? '5' : '4'}`);
      else row.push(`p ${x},${y} ${y === 57 ? '4' : '2'}`);
    }
    basePixels.push(row.join(' '));
  }
  // Tier 2 (middle slab): y=51..55, w=32 (x=8..39)
  for (let y = 51; y <= 55; y++) {
    const row = [];
    for (let x = 8; x <= 39; x++) {
      if (x === 8 || x === 39 || y === 51) row.push(`p ${x},${y} 1`);
      else if (x < 22) row.push(`p ${x},${y} ${y === 52 ? '5' : '4'}`);
      else row.push(`p ${x},${y} 3`);
    }
    basePixels.push(row.join(' '));
  }
  // Tier 3 (top pedestal): y=46..50, w=26 (x=11..36)
  for (let y = 46; y <= 50; y++) {
    const row = [];
    for (let x = 11; x <= 36; x++) {
      if (x === 11 || x === 36 || y === 46) row.push(`p ${x},${y} 1`);
      else row.push(`p ${x},${y} ${x < 22 ? '4' : '3'}`);
    }
    // Bronze inlay on pedestal
    row.push(`p 15,48 7 p 16,48 8 p 31,48 8 p 32,48 7`);
    basePixels.push(row.join(' '));
  }

  // Monolith Shaft & Runic Inscriptions: y=20..45, w=18 (x=15..32)
  const shaftPixels = [];
  for (let y = 20; y <= 45; y++) {
    const row = [];
    for (let x = 15; x <= 32; x++) {
      if (x === 15 || x === 32) row.push(`p ${x},${y} 1`);
      else if (x === 16) row.push(`p ${x},${y} 5`);
      else if (x < 23) row.push(`p ${x},${y} 4`);
      else if (x < 30) row.push(`p ${x},${y} 3`);
      else row.push(`p ${x},${y} 2`);
    }
    // Glowing Runic Glyphs down the center shaft (x=22..25)
    if (y >= 24 && y <= 42) {
      if ((y % 4 === 0) || (y % 4 === 1)) {
        row.push(`p 23,${y} 11 p 24,${y} 12 p 22,${y} 10 p 25,${y} 10`);
      }
    }
    shaftPixels.push(row.join(' '));
  }

  // Crystalline Cage & The Eternal Cyan Ember Flame: y=4..19
  const flamePixels = [];
  // Floating lantern crown pillars: x=14, 18, 29, 33
  for (let y = 8; y <= 19; y++) {
    const row = [];
    row.push(`p 14,${y} 1 p 15,${y} 7`);
    row.push(`p 32,${y} 7 p 33,${y} 1`);
    flamePixels.push(row.join(' '));
  }
  // The Ember Flame Crown (Pure Starlight Cyan Orb with dancing tongues)
  const cx = 24, cy = 12;
  for (let y = 4; y <= 18; y++) {
    const row = [];
    for (let x = 17; x <= 30; x++) {
      const d = Math.sqrt((x - cx) ** 2 + ((y - cy) * 1.1) ** 2);
      if (d <= 5.5) {
        if (d <= 1.8) row.push(`p ${x},${y} 13`);
        else if (d <= 3.2) row.push(`p ${x},${y} 12`);
        else if (d <= 4.8) row.push(`p ${x},${y} 11`);
        else row.push(`p ${x},${y} 10`);
      }
    }
    // Flame tips reaching up
    if (y === 4) row.push(`p 23,4 12 p 24,4 13 p 25,4 11`);
    if (y === 5) row.push(`p 21,5 11 p 26,5 11`);
    flamePixels.push(row.join(' '));
  }
  // Top spire finial at y=1..3
  flamePixels.push(`p 24,1 8 p 24,2 7 p 23,3 1 p 24,3 8 p 25,3 1`);

  return `@pix2 1.1

@meta
  title: "Runic Beacon Monolith"
  author: "NOIRADIX Studio v2.1.0"
  version: "1.1"
  color_mode: "RGBA"
  coordinate_origin: "top-left"

@grid
  columns: 1
  rows: 1
  default_cell: 48x64
  background: none

@palette
${palette}

@cell cell_0_0
  name: "Runic Beacon"
  direction: down
  frame: 0
  grid_pos: 0,0
  @layer pedestal_layer
    cell: cell_0_0
    name: "Base Pedestal"
    opacity: 1.0
    blend: normal
    @group base_grp
      cell: cell_0_0
      parent: pedestal_layer
      name: "Pedestal Group"
      tags: [stone, base]
${makeRegion('reg_pedestal', 'base_grp', 'Tiered Pedestal', ['stone'], basePixels)}
  @layer shaft_layer
    cell: cell_0_0
    name: "Monolith Shaft"
    opacity: 1.0
    blend: normal
    @group shaft_grp
      cell: cell_0_0
      parent: shaft_layer
      name: "Shaft Group"
      tags: [runes, pillar]
${makeRegion('reg_shaft', 'shaft_grp', 'Runic Pillar', ['pillar'], shaftPixels)}
  @layer flame_layer
    cell: cell_0_0
    name: "Eternal Ember Flame"
    opacity: 1.0
    blend: normal
    @group flame_grp
      cell: cell_0_0
      parent: flame_layer
      name: "Flame Group"
      tags: [fire, core]
${makeRegion('reg_flame', 'flame_grp', 'Eternal Flame', ['light'], flamePixels)}

@end`;
}

// ============================================================================
// 3. DEFENSE BARRICADE WALL (defense_wall.pix2) - 32x32
// ============================================================================
function generateDefenseWallPix2() {
  const palette = `  1: #0a0812  ; Dark contour
  2: #1e1610  ; Shadow timber
  3: #36281e  ; Weathered wood
  4: #543f2e  ; Wood midtone
  5: #755941  ; Wood grain highlight
  6: #1f2532  ; Iron bracket shadow
  7: #3b475e  ; Iron band midtone
  8: #617398  ; Iron rivet highlight
  9: #a2b2d6  ; Polished rivet glint`;

  const wallPixels = [];
  // 4 sharpened wooden stakes (x=2..29, y=4..31)
  const posts = [
    { minX: 2, maxX: 8, tipY: 4 },
    { minX: 9, maxX: 15, tipY: 2 },
    { minX: 16, maxX: 22, tipY: 3 },
    { minX: 23, maxX: 29, tipY: 5 }
  ];

  for (const post of posts) {
    const midX = Math.floor((post.minX + post.maxX) / 2);
    // Sharpened spike tip
    wallPixels.push(`p ${midX},${post.tipY} 1 p ${midX},${post.tipY + 1} 5`);
    wallPixels.push(`p ${midX - 1},${post.tipY + 1} 1 p ${midX + 1},${post.tipY + 1} 1`);
    for (let y = post.tipY + 2; y <= 30; y++) {
      const row = [];
      for (let x = post.minX; x <= post.maxX; x++) {
        if (x === post.minX || x === post.maxX || y === 30) row.push(`p ${x},${y} 1`);
        else if (x === post.minX + 1) row.push(`p ${x},${y} 5`);
        else if (x <= midX) row.push(`p ${x},${y} 4`);
        else row.push(`p ${x},${y} 3`);
      }
      wallPixels.push(row.join(' '));
    }
  }

  // Reinforced Heavy Iron Braces & Rivets across all posts (y=12..15 and y=23..26)
  const ironPixels = [];
  for (const bandY of [12, 23]) {
    for (let y = bandY; y <= bandY + 3; y++) {
      const row = [];
      for (let x = 1; x <= 30; x++) {
        if (x === 1 || x === 30 || y === bandY || y === bandY + 3) row.push(`p ${x},${y} 1`);
        else if (y === bandY + 1) row.push(`p ${x},${y} 8`);
        else row.push(`p ${x},${y} 7`);
      }
      // Silver Rivets
      row.push(`p 5,${bandY + 1} 9 p 12,${bandY + 1} 9 p 19,${bandY + 1} 9 p 26,${bandY + 1} 9`);
      ironPixels.push(row.join(' '));
    }
  }

  return `@pix2 1.1

@meta
  title: "Reinforced Defense Barricade"
  author: "NOIRADIX Studio v2.1.0"
  version: "1.1"
  color_mode: "RGBA"
  coordinate_origin: "top-left"

@grid
  columns: 1
  rows: 1
  default_cell: 32x32
  background: none

@palette
${palette}

@cell cell_0_0
  name: "Defense Wall"
  direction: down
  frame: 0
  grid_pos: 0,0
  @layer timber_layer
    cell: cell_0_0
    name: "Timber Palisade"
    opacity: 1.0
    blend: normal
    @group timber_grp
      cell: cell_0_0
      parent: timber_layer
      name: "Timber Posts"
      tags: [wood, spikes]
${makeRegion('reg_timber', 'timber_grp', 'Spiked Timber', ['wood'], wallPixels)}
  @layer iron_layer
    cell: cell_0_0
    name: "Iron Reinforcements"
    opacity: 1.0
    blend: normal
    @group iron_grp
      cell: cell_0_0
      parent: iron_layer
      name: "Iron Braces"
      tags: [iron, armor]
${makeRegion('reg_iron', 'iron_grp', 'Bolted Braces', ['iron'], ironPixels)}

@end`;
}

// ============================================================================
// 4. RELIC CHEST (relic_chest.pix2) - 32x32
// ============================================================================
function generateRelicChestPix2() {
  const palette = `  1: #0a0812  ; Dark outline
  2: #1c1824  ; Iron plate shadow
  3: #2d263a  ; Dark iron midtone
  4: #433954  ; Iron highlight
  5: #5c4e74  ; Iron rim
  6: #784818  ; Corroded bronze shadow
  7: #b8782a  ; Runic bronze trim
  8: #e6a244  ; Bronze highlight
  9: #fadc7a  ; Gold latch glint
  10: #2fe0f8 ; Runic keyhole cyan glow`;

  const bodyPixels = [];
  // Chest body from x=4..27, y=10..27
  // Lid: y=10..18, Base: y=18..27
  for (let y = 10; y <= 27; y++) {
    const row = [];
    for (let x = 4; x <= 27; x++) {
      if (x === 4 || x === 27 || y === 10 || y === 27 || y === 18) {
        row.push(`p ${x},${y} 1`);
      } else {
        // Bronze border banding
        if (x <= 6 || x >= 25 || y === 11 || y === 17 || y === 19 || y === 26) {
          row.push(`p ${x},${y} ${x < 16 ? '8' : '7'}`);
        } else {
          row.push(`p ${x},${y} ${x < 16 ? '4' : '3'}`);
        }
      }
    }
    bodyPixels.push(row.join(' '));
  }
  // Central Runic Bronze Plate & Lock Latch: x=14..17, y=16..21
  const lockPixels = [];
  lockPixels.push(`p 14,16 9 p 15,16 9 p 16,16 8 p 17,16 8`);
  lockPixels.push(`p 14,17 8 p 15,17 10 p 16,17 10 p 17,17 7`);
  lockPixels.push(`p 14,18 7 p 15,18 10 p 16,18 10 p 17,18 6`);
  lockPixels.push(`p 14,19 7 p 15,19 8 p 16,19 8 p 17,19 6`);
  lockPixels.push(`p 15,20 1 p 16,20 1`);

  return `@pix2 1.1

@meta
  title: "Ancient Relic Chest"
  author: "NOIRADIX Studio v2.1.0"
  version: "1.1"
  color_mode: "RGBA"
  coordinate_origin: "top-left"

@grid
  columns: 1
  rows: 1
  default_cell: 32x32
  background: none

@palette
${palette}

@cell cell_0_0
  name: "Relic Chest"
  direction: down
  frame: 0
  grid_pos: 0,0
  @layer chest_body_layer
    cell: cell_0_0
    name: "Chest Body & Iron"
    opacity: 1.0
    blend: normal
    @group body_grp
      cell: cell_0_0
      parent: chest_body_layer
      name: "Body Group"
      tags: [chest, container]
${makeRegion('reg_body', 'body_grp', 'Iron Container', ['iron'], bodyPixels)}
  @layer lock_layer
    cell: cell_0_0
    name: "Runic Latch"
    opacity: 1.0
    blend: normal
    @group lock_grp
      cell: cell_0_0
      parent: lock_layer
      name: "Lock Group"
      tags: [latch, lock]
${makeRegion('reg_lock', 'lock_grp', 'Runic Keyhole', ['lock'], lockPixels)}

@end`;
}

// ============================================================================
// 5. RUNIC ELECTRIC TRAP (runic_trap.pix2) - 32x32
// ============================================================================
function generateRunicTrapPix2() {
  const palette = `  1: #0a0812  ; Dark outline
  2: #161824  ; Iron base shadow
  3: #282c3e  ; Iron plate midtone
  4: #3f4660  ; Plate highlight
  5: #5e688c  ; Rim steel
  6: #6e1c28  ; Spikes shadow
  7: #b82a3c  ; Crimson warning rim
  8: #14728a  ; Discharge channel shadow
  9: #22c0e8  ; Electric cyan arc
  10: #a2f2ff ; Spark core white`;

  const platePixels = [];
  // Octagonal floor trap plate: x=3..28, y=5..26
  for (let y = 5; y <= 26; y++) {
    const row = [];
    const cornerCut = (y < 8 ? (8 - y) : y > 23 ? (y - 23) : 0);
    const minX = 3 + cornerCut;
    const maxX = 28 - cornerCut;
    for (let x = minX; x <= maxX; x++) {
      if (x === minX || x === maxX || y === 5 || y === 26) {
        row.push(`p ${x},${y} 1`);
      } else {
        row.push(`p ${x},${y} ${x < 16 ? (y < 16 ? '4' : '3') : '2'}`);
      }
    }
    platePixels.push(row.join(' '));
  }

  // Steel spikes protruding from floor slots: 4 cluster spikes
  const spikes = [
    [8, 10], [9, 9], [10, 10],
    [21, 10], [22, 9], [23, 10],
    [8, 21], [9, 22], [10, 21],
    [21, 21], [22, 22], [23, 21]
  ];
  const spikePixels = [];
  for (const [sx, sy] of spikes) {
    spikePixels.push(`p ${sx},${sy} 10 p ${sx},${sy + 1} 7 p ${sx},${sy + 2} 1`);
  }
  // Glowing cyan electric circuit runes radiating across plate
  for (let i = 11; i <= 20; i++) {
    spikePixels.push(`p ${i},15 9 p ${i},16 10 p 15,${i} 9 p 16,${i} 10`);
  }

  return `@pix2 1.1

@meta
  title: "Runic Electric Spike Trap"
  author: "NOIRADIX Studio v2.1.0"
  version: "1.1"
  color_mode: "RGBA"
  coordinate_origin: "top-left"

@grid
  columns: 1
  rows: 1
  default_cell: 32x32
  background: none

@palette
${palette}

@cell cell_0_0
  name: "Runic Trap"
  direction: down
  frame: 0
  grid_pos: 0,0
  @layer plate_layer
    cell: cell_0_0
    name: "Octagonal Plate"
    opacity: 1.0
    blend: normal
    @group plate_grp
      cell: cell_0_0
      parent: plate_layer
      name: "Plate Group"
      tags: [plate, steel]
${makeRegion('reg_plate', 'plate_grp', 'Iron Plate', ['plate'], platePixels)}
  @layer spikes_layer
    cell: cell_0_0
    name: "Spikes & Electric Sigils"
    opacity: 1.0
    blend: normal
    @group spikes_grp
      cell: cell_0_0
      parent: spikes_layer
      name: "Spikes Group"
      tags: [spikes, electric]
${makeRegion('reg_spikes', 'spikes_grp', 'Spikes & Sigils', ['spikes'], spikePixels)}

@end`;
}

// ============================================================================
// 6. ANCIENT EMBER TORCH (torch.pix2) - 32x32
// ============================================================================
function generateTorchPix2() {
  const palette = `  1: #0a0812  ; Dark contour
  2: #241812  ; Timber shadow
  3: #422d20  ; Wood midtone
  4: #664632  ; Wood highlight
  5: #282e3c  ; Iron band
  6: #4d576e  ; Iron highlight
  7: #7a2818  ; Ember deep flame
  8: #d4481e  ; Vibrant flame orange
  9: #f89624  ; Flame golden core
  10: #ffe87a ; White-hot spark core`;

  const postPixels = [];
  // Ground stake: x=14..17, y=14..30
  for (let y = 14; y <= 30; y++) {
    const row = [];
    for (let x = 14; x <= 17; x++) {
      if (x === 14 || x === 17 || y === 30) row.push(`p ${x},${y} 1`);
      else row.push(`p ${x},${y} ${x === 15 ? '4' : '3'}`);
    }
    // Iron brace at y=20 and y=26
    if (y === 20 || y === 26) {
      row.push(`p 13,${y} 1 p 18,${y} 1 p 14,${y} 6 p 15,${y} 6 p 16,${y} 5 p 17,${y} 5`);
    }
    postPixels.push(row.join(' '));
  }

  // Iron brazier cup at y=11..13: x=11..20
  const flamePixels = [];
  for (let y = 11; y <= 13; y++) {
    const row = [];
    for (let x = 11; x <= 20; x++) {
      if (x === 11 || x === 20 || y === 13) row.push(`p ${x},${y} 1`);
      else row.push(`p ${x},${y} ${x < 15 ? '6' : '5'}`);
    }
    flamePixels.push(row.join(' '));
  }
  // Crackling Ember Flame: y=2..10, x=12..19
  const cx = 15.5, cy = 7;
  for (let y = 2; y <= 10; y++) {
    const row = [];
    for (let x = 12; x <= 19; x++) {
      const d = Math.sqrt((x - cx) ** 2 + ((y - cy) * 0.9) ** 2);
      if (d <= 3.8) {
        if (d <= 1.4) row.push(`p ${x},${y} 10`);
        else if (d <= 2.4) row.push(`p ${x},${y} 9`);
        else if (d <= 3.2) row.push(`p ${x},${y} 8`);
        else row.push(`p ${x},${y} 7`);
      }
    }
    // Flickering flame tips
    if (y === 2) row.push(`p 15,2 9 p 16,2 10`);
    if (y === 3) row.push(`p 14,3 8 p 17,3 9`);
    flamePixels.push(row.join(' '));
  }

  return `@pix2 1.1

@meta
  title: "Ancient Ember Torch"
  author: "NOIRADIX Studio v2.1.0"
  version: "1.1"
  color_mode: "RGBA"
  coordinate_origin: "top-left"

@grid
  columns: 1
  rows: 1
  default_cell: 32x32
  background: none

@palette
${palette}

@cell cell_0_0
  name: "Ember Torch"
  direction: down
  frame: 0
  grid_pos: 0,0
  @layer post_layer
    cell: cell_0_0
    name: "Wooden Post & Iron"
    opacity: 1.0
    blend: normal
    @group post_grp
      cell: cell_0_0
      parent: post_layer
      name: "Post Group"
      tags: [post, wood]
${makeRegion('reg_post', 'post_grp', 'Carved Stake', ['wood'], postPixels)}
  @layer flame_layer
    cell: cell_0_0
    name: "Crackling Ember Flame"
    opacity: 1.0
    blend: normal
    @group flame_grp
      cell: cell_0_0
      parent: flame_layer
      name: "Flame Group"
      tags: [fire, flame]
${makeRegion('reg_flame', 'flame_grp', 'Ember Flame', ['fire'], flamePixels)}

@end`;
}

// ----------------------------------------------------------------------------
// Compiler & Rasterizer Execution
// ----------------------------------------------------------------------------
function run() {
  const assets = [
    { name: 'tree', pix2Name: 'ancient_tree.pix2', generator: generateAncientTreePix2, w: 48, h: 64 },
    { name: 'beacon', pix2Name: 'runic_beacon.pix2', generator: generateRunicBeaconPix2, w: 48, h: 64 },
    { name: 'wall', pix2Name: 'defense_wall.pix2', generator: generateDefenseWallPix2, w: 32, h: 32 },
    { name: 'chest', pix2Name: 'relic_chest.pix2', generator: generateRelicChestPix2, w: 32, h: 32 },
    { name: 'trap', pix2Name: 'runic_trap.pix2', generator: generateRunicTrapPix2, w: 32, h: 32 },
    { name: 'torch', pix2Name: 'torch.pix2', generator: generateTorchPix2, w: 32, h: 32 }
  ];

  const rasterReport = {};

  for (const asset of assets) {
    const text = asset.generator();
    
    // 1. Validate with official PIX2 1.1 parser
    const doc = PIX2.parseValidated(text, { strict: true, profile: 'studio' });
    console.log(`[PASS] PIX2 1.1 strict validation for ${asset.pix2Name} (${doc.meta.title})`);

    // Save .pix2 documents to both repository locations
    const p1 = path.join('tools', 'noiradix-studio', 'pix2', asset.pix2Name);
    const p2 = path.join('My project', 'Assets', 'Resources', 'Ember', 'PIX2', asset.pix2Name);
    fs.writeFileSync(p1, text, 'utf8');
    fs.writeFileSync(p2, text, 'utf8');

    // 2. Composite Cell Layers using official PIX2 compositing engine
    const cell = doc.cells['cell_0_0'];
    const pixels = PIX2.compositeCellLayers(cell, doc.meta.cell_size);
    rasterReport[asset.name] = {
      w: asset.w,
      h: asset.h,
      targetPng: path.join('My project', 'Assets', 'Resources', 'Ember', 'Props', `${asset.name}.png`),
      pixels
    };
  }

  // Dump raster report for python PIL writer
  fs.mkdirSync('Builds', { recursive: true });
  fs.writeFileSync('Builds/map-raster-evidence.json', JSON.stringify(rasterReport, null, 2), 'utf8');
  console.log('Saved Builds/map-raster-evidence.json. Handing over to Python rasterizer...');
}

run();
