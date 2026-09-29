// Throwaway exporter: runs the demo's procedural texture generators against a
// stub Phaser scene and dumps the requested placeholder textures as PNG data URLs.
// It never modifies the demo sources.
import { buildCharacters } from '../../Reference/mossbound/mossbound/nextjs_space/src/game/graphics/SpriteFactory';
import { buildStructures } from '../../Reference/mossbound/mossbound/nextjs_space/src/game/graphics/StructureFactory';
import { buildDecor } from '../../Reference/mossbound/mossbound/nextjs_space/src/game/graphics/DecorFactory';
import { renderGround } from '../../Reference/mossbound/mossbound/nextjs_space/src/game/graphics/TileRenderer';
import { walkDist, PONDS } from '../../Reference/mossbound/mossbound/nextjs_space/src/game/world/MapLayout';

const textures = new Map<string, HTMLCanvasElement>();
const anims: { key: string; frames: number[]; frameRate: number; repeat: number }[] = [];

const scene = {
  textures: {
    exists: (k: string) => textures.has(k),
    addCanvas: (k: string, c: HTMLCanvasElement) => {
      textures.set(k, c);
      return { add: () => undefined };
    },
    get: (k: string) => ({ getSourceImage: () => textures.get(k) }),
  },
  anims: {
    create: (cfg: { key: string; frames: { frame: number }[]; frameRate: number; repeat: number }) => {
      anims.push({ key: cfg.key, frames: cfg.frames.map((f) => f.frame), frameRate: cfg.frameRate, repeat: cfg.repeat });
    },
  },
};

// Prototype area crop in demo world pixels (blue side of the mid lane + jungle clearing).
const CROP = { x: 400, y: 696, w: 1280, h: 864 };
const CELL = 16;
const HERO_RADIUS = 12;

const TEXTURES = [
  'nilo', 'shadow',
  'tree-0', 'tree-1', 'tree-2', 'bush-0', 'bush-1', 'rock-0', 'rock-1', 'rock-2',
  'mush-0', 'mush-1', 'flower-0', 'flower-1', 'flower-2', 'flower-3',
  'pillar-0', 'pillar-1', 'ruin-wall', 'crystal-deco',
];

function run(): unknown {
  const s = scene as never;
  buildCharacters(s);
  buildStructures(s);
  buildDecor(s);
  const decor = renderGround(s);

  const ground = textures.get('ground');
  if (!ground) throw new Error('ground texture missing');
  const crop = document.createElement('canvas');
  crop.width = CROP.w;
  crop.height = CROP.h;
  const ctx = crop.getContext('2d');
  if (!ctx) throw new Error('no 2d context');
  ctx.imageSmoothingEnabled = false;
  ctx.drawImage(ground, CROP.x, CROP.y, CROP.w, CROP.h, 0, 0, CROP.w, CROP.h);

  const out: Record<string, { w: number; h: number; png: string }> = {};
  for (const k of TEXTURES) {
    const c = textures.get(k);
    if (!c) throw new Error(`texture missing: ${k}`);
    out[k] = { w: c.width, h: c.height, png: c.toDataURL('image/png') };
  }
  out['terrain'] = { w: crop.width, h: crop.height, png: crop.toDataURL('image/png') };

  const cell = document.createElement('canvas');
  cell.width = CELL;
  cell.height = CELL;
  const cctx = cell.getContext('2d');
  if (!cctx) throw new Error('no 2d context');
  cctx.fillStyle = 'rgba(255,60,60,0.35)';
  cctx.fillRect(0, 0, CELL, CELL);
  cctx.fillStyle = 'rgba(255,60,60,0.9)';
  cctx.fillRect(0, 0, CELL, 1);
  cctx.fillRect(0, 0, 1, CELL);
  out['collision-cell'] = { w: CELL, h: CELL, png: cell.toDataURL('image/png') };

  const wanted = new Set(TEXTURES);
  const props = decor
    .filter((d) => wanted.has(d.key))
    .filter((d) => d.x >= CROP.x - 30 && d.x <= CROP.x + CROP.w + 30 && d.y >= CROP.y && d.y <= CROP.y + CROP.h + 60)
    .map((d) => ({ key: d.key, x: Math.round(d.x), y: Math.round(d.y), flip: d.flip }));

  const cols = CROP.w / CELL;
  const rows = CROP.h / CELL;
  const blocked: string[] = [];
  for (let r = 0; r < rows; r++) {
    let line = '';
    for (let c = 0; c < cols; c++) {
      const x = CROP.x + c * CELL + CELL / 2;
      const y = CROP.y + r * CELL + CELL / 2;
      const border = r === 0 || c === 0 || r === rows - 1 || c === cols - 1;
      let water = false;
      for (const p of PONDS) if (Math.hypot(x - p.x, y - p.y) < p.r - 6) water = true;
      const block = border || water || walkDist(x, y) > HERO_RADIUS - 2;
      line += block ? '#' : '.';
    }
    blocked.push(line);
  }

  return {
    crop: CROP,
    cellSize: CELL,
    textures: out,
    anims: anims.filter((a) => a.key.startsWith('nilo-')),
    layout: { props, blocked, heroSpawn: { x: 600, y: 1200 } },
  };
}

const pre = document.getElementById('out');
try {
  const result = run();
  if (pre) pre.textContent = JSON.stringify(result);
} catch (e) {
  if (pre) pre.textContent = JSON.stringify({ error: String(e) });
}
