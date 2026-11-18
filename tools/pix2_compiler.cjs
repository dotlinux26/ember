const fs = require('fs');
const path = require('path');
const zlib = require('zlib');
const PIX2 = require('./noiradix-studio/js/pix2-format.js');

// CRC32 table for pure PNG chunk building
const CRC_TABLE = new Uint32Array(256);
for (let i = 0; i < 256; i++) {
  let c = i;
  for (let k = 0; k < 8; k++) c = (c & 1) ? (0xedb88320 ^ (c >>> 1)) : (c >>> 1);
  CRC_TABLE[i] = c >>> 0;
}

function crc32(buf) {
  let c = 0xffffffff;
  for (let i = 0; i < buf.length; i++) {
    c = CRC_TABLE[(c ^ buf[i]) & 0xff] ^ (c >>> 8);
  }
  return (c ^ 0xffffffff) >>> 0;
}

function makeChunk(type, data) {
  const len = Buffer.alloc(4);
  len.writeUInt32BE(data.length, 0);
  const typeBuf = Buffer.from(type, 'ascii');
  const crcBuf = Buffer.alloc(4);
  crcBuf.writeUInt32BE(crc32(Buffer.concat([typeBuf, data])), 0);
  return Buffer.concat([len, typeBuf, data, crcBuf]);
}

function encodeRgbaToPng(w, h, rgbaBuffer) {
  const sig = Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]);
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(w, 0);
  ihdr.writeUInt32BE(h, 4);
  ihdr[8] = 8;  // 8 bits per channel
  ihdr[9] = 6;  // RGBA color type
  ihdr[10] = 0; // Deflate compression
  ihdr[11] = 0; // Filter method
  ihdr[12] = 0; // Interlace none

  const rawScanlines = Buffer.alloc(h * (1 + w * 4));
  for (let y = 0; y < h; y++) {
    rawScanlines[y * (1 + w * 4)] = 0; // Filter type 0 (None)
    for (let x = 0; x < w; x++) {
      const src = (y * w + x) * 4;
      const dst = y * (1 + w * 4) + 1 + x * 4;
      rawScanlines[dst]     = rgbaBuffer[src];
      rawScanlines[dst + 1] = rgbaBuffer[src + 1];
      rawScanlines[dst + 2] = rgbaBuffer[src + 2];
      rawScanlines[dst + 3] = rgbaBuffer[src + 3];
    }
  }

  const idat = zlib.deflateSync(rawScanlines, { level: 9 });
  return Buffer.concat([
    sig,
    makeChunk('IHDR', ihdr),
    makeChunk('IDAT', idat),
    makeChunk('IEND', Buffer.alloc(0))
  ]);
}

function compilePix2ToPng(pix2Path, outputPngPath = null) {
  const text = fs.readFileSync(pix2Path, 'utf8');
  const doc = PIX2.parseValidated(text, { strict: true, profile: 'studio' });
  const cellSize = doc.meta.cell_size || { w: 48, h: 48 };
  const w = cellSize.w;
  const h = cellSize.h;

  console.log(`[PIX2-COMPILER] Parsing "${doc.meta.title}" (Version: ${doc.meta.version})`);
  console.log(`[PIX2-COMPILER] Canvas: ${w}x${h}, Cells: ${Object.keys(doc.cells).length}, Clips: ${Object.keys(doc.animations).length}`);

  const cellKeys = Object.keys(doc.cells);
  if (cellKeys.length === 0) {
    throw new Error('Document has no cells to render.');
  }

  const cell = doc.cells[cellKeys[0]];
  const pixels = PIX2.compositeCellLayers(cell, cellSize);

  const rgba = Buffer.alloc(w * h * 4);
  for (const [coord, hex] of Object.entries(pixels)) {
    const [x, y] = coord.split(',').map(Number);
    if (x >= 0 && x < w && y >= 0 && y < h) {
      const parsed = PIX2.hexToRgba(hex);
      const idx = (y * w + x) * 4;
      rgba[idx]     = parsed.r;
      rgba[idx + 1] = parsed.g;
      rgba[idx + 2] = parsed.b;
      rgba[idx + 3] = parsed.a;
    }
  }

  const pngBytes = encodeRgbaToPng(w, h, rgba);
  const outPath = outputPngPath || pix2Path.replace(/\.pix2$/i, '.png');
  fs.mkdirSync(path.dirname(outPath), { recursive: true });
  fs.writeFileSync(outPath, pngBytes);
  console.log(`[PIX2-COMPILER] Successfully compiled -> ${outPath} (${pngBytes.length} bytes)`);
  return { doc, outPath, bytes: pngBytes.length };
}

if (require.main === module) {
  const args = process.argv.slice(2);
  if (args.length === 0) {
    console.log('Usage: node tools/pix2_compiler.cjs <file.pix2> [output.png]');
    process.exit(1);
  }
  try {
    compilePix2ToPng(args[0], args[1]);
  } catch (err) {
    console.error('[PIX2-COMPILER-ERROR]', err.message);
    process.exit(1);
  }
}

module.exports = { compilePix2ToPng, encodeRgbaToPng };
