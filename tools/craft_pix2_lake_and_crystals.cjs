const fs = require('fs');
const path = require('path');
const PIX2 = require('./noiradix-studio/js/pix2-format.js');
const { compilePix2ToPng } = require('./pix2_compiler.cjs');

function makeRegion(id, parent, name, tags, pLines) {
  let out = `      @region ${id}\n        cell: cell_0_0\n        parent: ${parent}\n        name: "${name}"\n`;
  if (tags && tags.length) out += `        tags: [${tags.join(', ')}]\n`;
  for (const line of pLines) {
    if (line.trim()) out += `        ${line.trim()}\n`;
  }
  return out;
}

// ============================================================================
// 1. SACRED LAKE SANCTUARY (64x64) - PIX2 1.1 Conforming Document
// ============================================================================
function generateSacredLakePix2(lit = false) {
  const palette = `  1: #0c0a12  ; 1: Dark Slate Silhouette / Mortar
  2: #181422  ; 2: Stone Shadow
  3: #2a2438  ; 3: Stone Body Dark
  4: #443c58  ; 4: Stone Midtone
  5: #665c80  ; 5: Stone Edge Highlight
  6: #305834  ; 6: Moss / Overgrowth
  7: #22442a  ; 7: Dark Grass Border
  8: #0c2455  ; 8: Abyss Water Depth
  9: #123673  ; 9: Deep Sapphire Water
  10: #1e5891 ; 10: Mid Water Layer
  11: #2d7da5 ; 11: Shallow Turquoise Shore
  12: ${lit ? '#2de6ff' : '#4b96c3'} ; 12: Wave Ripple Highlight
  13: ${lit ? '#d7faff' : '#6eb9dc'} ; 13: Caustic Sunlight Glint`;

  const rimPixels = [];
  const waterPixels = [];
  const cx = 31.5, cy = 31.5;

  for (let y = 0; y < 64; y++) {
    const rowRim = [];
    const rowWater = [];
    for (let x = 0; x < 64; x++) {
      const dx = x - cx;
      const dy = (y - cy) * 1.15;
      const d = Math.sqrt(dx * dx + dy * dy);
      if (d > 29.5) continue;

      if (d >= 22.0) {
        if (d >= 28.5) {
          rowRim.push(`p ${x},${y} 1`);
        } else {
          const angle = Math.atan2(dy, dx);
          const isJoint = Math.abs((angle + Math.PI) % (Math.PI / 7.0)) < 0.08;
          if (isJoint) {
            rowRim.push(`p ${x},${y} 2`);
          } else if (d > 27.0) {
            rowRim.push(`p ${x},${y} 3`);
          } else if (d > 24.5) {
            const light = -dx * 0.6 - dy * 0.7;
            rowRim.push(`p ${x},${y} ${light > 4 ? '5' : light > 0 ? '4' : '3'}`);
          } else {
            rowRim.push(`p ${x},${y} ${(x + y) % 3 === 0 ? '6' : '2'}`);
          }
        }
      } else {
        if (d >= 18.5) rowWater.push(`p ${x},${y} 11`);
        else if (d >= 13.5) rowWater.push(`p ${x},${y} 10`);
        else if (d >= 7.5) rowWater.push(`p ${x},${y} 9`);
        else rowWater.push(`p ${x},${y} 8`);
      }
    }
    if (rowRim.length) rimPixels.push(rowRim.join(' '));
    if (rowWater.length) waterPixels.push(rowWater.join(' '));
  }

  // Ripples
  const ripples = [5.5, 11.0, 16.5];
  const ripplePixels = [];
  for (const r0 of ripples) {
    for (let deg = 0; deg < 360; deg += 6) {
      const rad = deg * (Math.PI / 180);
      const wobble = Math.sin(deg * 4) * 0.4;
      const rx = Math.floor(cx + (r0 + wobble) * Math.cos(rad));
      const ry = Math.floor(cy + (r0 + wobble) * Math.sin(rad) * 0.85);
      if (rx >= 0 && rx < 64 && ry >= 0 && ry < 64) {
        if (deg >= 120 && deg <= 300) {
          const col = (deg === 180 || deg === 210 || deg === 240) ? '13' : '12';
          ripplePixels.push(`p ${rx},${ry} ${col}`);
        }
      }
    }
  }

  return `@pix2 1.1

@meta
  title: "${lit ? 'Sacred Lake Sanctuary Lit' : 'Sacred Lake Sanctuary Dormant'}"
  author: "NOIRADIX Studio v2.1.0"
  version: "1.1"
  color_mode: "RGBA"
  coordinate_origin: "top-left"

@grid
  columns: 1
  rows: 1
  default_cell: 64x64
  background: none

@palette
${palette}

@cell cell_0_0
  name: "Lake Basin"
  direction: down
  frame: 0
  grid_pos: 0,0
  @layer terrain_lake
    cell: cell_0_0
    name: "Lake Terrain"
    opacity: 1.0
    blend: normal
    @group water_basin
      cell: cell_0_0
      parent: terrain_lake
      name: "Water Depth Basin"
      tags: [water, pond]
${makeRegion('reg_water_depth', 'water_basin', 'Water Depth', ['water'], waterPixels)}
${makeRegion('reg_water_ripples', 'water_basin', 'Water Surface Ripples', ['ripples'], ripplePixels)}
    @group stone_curb
      cell: cell_0_0
      parent: terrain_lake
      name: "Stone Flagstone Curb"
      tags: [stone, rim]
${makeRegion('reg_stone_rim', 'stone_curb', 'Flagstone Rim', ['stone'], rimPixels)}

@end`;
}

// ============================================================================
// 2. THE GREAT FLOATING CRYSTAL (32x48) - PIX2 1.1 Conforming Document
// ============================================================================
function generateFloatingCrystalPix2(lit = false) {
  const palette = lit
    ? `  1: #062636  ; 1: Deep Cyan Shadow Outline
  2: #0c5576  ; 2: Dark Cyan Facet
  3: #168eb9  ; 3: Mid Cyan Shading
  4: #2dd2f5  ; 4: Radiant Cyan Body
  5: #a5f5ff  ; 5: Core Light
  6: #ffffff  ; 6: Pure White Light Glint`
    : `  1: #08060c  ; 1: Obsidian Silhouette
  2: #12101a  ; 2: Deep Dark Shadow
  3: #201e2e  ; 3: Dark Slate Facet
  4: #302d44  ; 4: Slate Midtone
  5: #484462  ; 5: Subdued Facet Highlight
  6: #66628a  ; 6: Subtle Edge Rim`;

  const crystalPixels = [];
  const cx = 15.5;

  for (let y = 4; y <= 44; y++) {
    const t = (y - 4) / 40.0;
    const w = t < 0.5
      ? 1.0 + (t / 0.5) ** 0.85 * 9.5
      : 1.0 + ((1.0 - t) / 0.5) ** 1.1 * 9.5;
    const minX = Math.floor(cx - w);
    const maxX = Math.floor(cx + w);
    const row = [];

    for (let x = minX; x <= maxX; x++) {
      if (x === minX || x === maxX || y === 4 || y === 44) {
        row.push(`p ${x},${y} 1`);
      } else if (x < 15) {
        if (x === minX + 1) row.push(`p ${x},${y} 6`);
        else if (x < 12) row.push(`p ${x},${y} 5`);
        else row.push(`p ${x},${y} 4`);
      } else if (x === 15) {
        row.push(`p ${x},${y} ${lit ? '6' : '5'}`);
      } else if (x < 22) {
        row.push(`p ${x},${y} ${lit ? '4' : '3'}`);
      } else {
        row.push(`p ${x},${y} ${lit ? '3' : '2'}`);
      }
    }
    if (row.length) crystalPixels.push(row.join(' '));
  }

  // Core flare if lit
  const corePixels = [];
  if (lit) {
    for (let y = 18; y <= 30; y++) {
      const row = [];
      for (let x = 11; x <= 20; x++) {
        const d = Math.sqrt((x - 15.5) ** 2 + ((y - 24) * 1.2) ** 2);
        if (d < 3.0) row.push(`p ${x},${y} 6`);
        else if (d < 5.0) row.push(`p ${x},${y} 5`);
      }
      if (row.length) corePixels.push(row.join(' '));
    }
  }

  return `@pix2 1.1

@meta
  title: "${lit ? 'Great Floating Crystal Lit' : 'Great Floating Crystal Dormant'}"
  author: "NOIRADIX Studio v2.1.0"
  version: "1.1"
  color_mode: "RGBA"
  coordinate_origin: "top-left"

@grid
  columns: 1
  rows: 1
  default_cell: 32x48
  background: none

@palette
${palette}

@cell cell_0_0
  name: "Floating Monolith"
  direction: down
  frame: 0
  grid_pos: 0,0
  @layer crystal_body
    cell: cell_0_0
    name: "Crystal Body Layer"
    opacity: 1.0
    blend: normal
    @group monolith_facets
      cell: cell_0_0
      parent: crystal_body
      name: "Monolith Facets"
      tags: [crystal, monolith]
${makeRegion('reg_facets', 'monolith_facets', 'Crystal Facets', ['crystal'], crystalPixels)}
${corePixels.length ? makeRegion('reg_core_light', 'monolith_facets', 'Core Light Flare', ['fire', 'glow'], corePixels) : ''}

@end`;
}

function run() {
  const pix2Dir = path.join('My project', 'Assets', 'Resources', 'Ember', 'PIX2');
  const propsDir = path.join('My project', 'Assets', 'Resources', 'Ember', 'Props');
  fs.mkdirSync(pix2Dir, { recursive: true });
  fs.mkdirSync(propsDir, { recursive: true });

  const docs = [
    { name: 'sacred_lake_dark.pix2', gen: () => generateSacredLakePix2(false), png: 'beacon_lake.png' },
    { name: 'sacred_lake_lit.pix2', gen: () => generateSacredLakePix2(true), png: 'beacon_lit.png' },
    { name: 'lake_crystal_dark.pix2', gen: () => generateFloatingCrystalPix2(false), png: 'beacon_crystal_dark.png' },
    { name: 'lake_crystal_lit.pix2', gen: () => generateFloatingCrystalPix2(true), png: 'beacon_crystal_lit.png' }
  ];

  for (const item of docs) {
    const text = item.gen();
    const pix2Path = path.join(pix2Dir, item.name);
    fs.writeFileSync(pix2Path, text, 'utf8');

    // Strict validation
    const parsed = PIX2.parseValidated(text, { strict: true, profile: 'studio' });
    console.log(`[PASS PIX2 1.1] ${item.name}: ${parsed.meta.title}`);

    // Compile to PNG using pure headless compiler
    const outPngPath = path.join(propsDir, item.png);
    compilePix2ToPng(pix2Path, outPngPath);
  }

  // Also copy lit lake to default beacon.png
  fs.copyFileSync(path.join(propsDir, 'beacon_lit.png'), path.join(propsDir, 'beacon.png'));
  fs.copyFileSync(path.join(propsDir, 'beacon_lake.png'), path.join(propsDir, 'beacon_dark.png'));
  console.log('Successfully generated and compiled all Sacred Lake & Floating Crystal PIX2 documents!');
}

run();
