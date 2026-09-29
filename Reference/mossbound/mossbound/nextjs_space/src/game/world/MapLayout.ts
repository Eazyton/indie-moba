import { MAP, POS } from '../config/balance';

export interface Seg { ax: number; ay: number; bx: number; by: number; hw: number }
export interface Circle { x: number; y: number; r: number }

const S = MAP.size;
const mirror = (y: number): number => S - y;

export const LANES: Seg[] = [
  // rota principal
  { ax: 200, ay: POS.laneY, bx: 2200, by: POS.laneY, hw: 92 },
  // flanco superior
  { ax: 300, ay: 1060, bx: 440, by: 500, hw: 72 },
  { ax: 440, ay: 500, bx: 1960, by: 500, hw: 72 },
  { ax: 1960, ay: 500, bx: 2100, by: 1060, hw: 72 },
  // flanco inferior
  { ax: 300, ay: mirror(1060), bx: 440, by: mirror(500), hw: 72 },
  { ax: 440, ay: mirror(500), bx: 1960, by: mirror(500), hw: 72 },
  { ax: 1960, ay: mirror(500), bx: 2100, by: mirror(1060), hw: 72 },
  // trilha da selva
  { ax: 1200, ay: 500, bx: 1200, by: mirror(500), hw: 48 },
];

export const BASES: Circle[] = [
  { x: POS.blueBase.x, y: POS.blueBase.y, r: POS.baseRadius },
  { x: POS.redBase.x, y: POS.redBase.y, r: POS.baseRadius },
];

export const CLEARINGS: Circle[] = [
  { x: 1200, y: 850, r: 150 },
  { x: 1200, y: mirror(850), r: 150 },
];

export const PONDS: Circle[] = [
  { x: 1305, y: 870, r: 52 },
  { x: 1095, y: mirror(870), r: 52 },
];

export function segDist(x: number, y: number, s: Seg): number {
  const dx = s.bx - s.ax;
  const dy = s.by - s.ay;
  const l2 = dx * dx + dy * dy;
  let t = l2 > 0 ? ((x - s.ax) * dx + (y - s.ay) * dy) / l2 : 0;
  t = Math.max(0, Math.min(1, t));
  const px = s.ax + dx * t;
  const py = s.ay + dy * t;
  return Math.hypot(x - px, y - py);
}

/** distância com sinal até a borda da área caminhável (negativo = dentro) */
export function walkDist(x: number, y: number): number {
  let d = Infinity;
  for (const s of LANES) d = Math.min(d, segDist(x, y, s) - s.hw);
  for (const c of BASES) d = Math.min(d, Math.hypot(x - c.x, y - c.y) - c.r);
  for (const c of CLEARINGS) d = Math.min(d, Math.hypot(x - c.x, y - c.y) - c.r);
  return d;
}

/** distância até o centro de caminho de terra (negativo = dentro do caminho) */
export function pathDist(x: number, y: number): number {
  let d = Infinity;
  for (const s of LANES) d = Math.min(d, segDist(x, y, s) - s.hw * 0.5);
  return d;
}

export function inWater(x: number, y: number): boolean {
  for (const p of PONDS) if (Math.hypot(x - p.x, y - p.y) < p.r) return true;
  return false;
}

export function isWalkable(x: number, y: number): boolean {
  if (x < 24 || y < 24 || x > S - 24 || y > S - 24) return false;
  if (inWater(x, y)) return false;
  return walkDist(x, y) <= 0;
}

export function inBase(team: 0 | 1, x: number, y: number): boolean {
  const b = BASES[team];
  return !!b && Math.hypot(x - b.x, y - b.y) < b.r - 20;
}

/** RNG determinístico */
export function mulberry(seed: number): () => number {
  let a = seed >>> 0;
  return () => {
    a = (a + 0x6d2b79f5) >>> 0;
    let t = a;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

function hash2(x: number, y: number): number {
  let h = (x * 374761393 + y * 668265263) | 0;
  h = Math.imul(h ^ (h >>> 13), 1274126177);
  return ((h ^ (h >>> 16)) >>> 0) / 4294967296;
}

/** value noise suave */
export function noise(x: number, y: number): number {
  const xi = Math.floor(x);
  const yi = Math.floor(y);
  const xf = x - xi;
  const yf = y - yi;
  const u = xf * xf * (3 - 2 * xf);
  const v = yf * yf * (3 - 2 * yf);
  const a = hash2(xi, yi);
  const b = hash2(xi + 1, yi);
  const c = hash2(xi, yi + 1);
  const d = hash2(xi + 1, yi + 1);
  return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
}

export function fbm(x: number, y: number): number {
  return noise(x, y) * 0.55 + noise(x * 2.1, y * 2.1) * 0.3 + noise(x * 4.3, y * 4.3) * 0.15;
}

export interface Decor { key: string; x: number; y: number; flip: boolean }

/** posições de decoração geradas de forma determinística */
export function buildDecor(): { sprites: Decor[]; baked: Decor[] } {
  const rnd = mulberry(1337);
  const sprites: Decor[] = [];
  const baked: Decor[] = [];
  const step = 46;
  for (let gy = 0; gy < S + step; gy += step) {
    for (let gx = 0; gx < S + step; gx += step) {
      const x = gx + (rnd() - 0.5) * step * 0.9;
      const y = gy + (rnd() - 0.5) * step * 0.9;
      const wd = walkDist(x, y);
      const r = rnd();
      const flip = rnd() < 0.5;
      if (wd > 18) {
        const key = `tree-${Math.floor(rnd() * 3)}`;
        if (wd < 170) sprites.push({ key, x, y, flip });
        else baked.push({ key, x, y, flip });
      } else if (wd > -14) {
        if (inWater(x, y)) continue;
        if (r < 0.34) sprites.push({ key: `bush-${Math.floor(rnd() * 2)}`, x, y, flip });
        else if (r < 0.5) sprites.push({ key: `rock-${Math.floor(rnd() * 3)}`, x, y, flip });
        else if (r < 0.66) sprites.push({ key: `mush-${Math.floor(rnd() * 2)}`, x, y, flip });
        else if (r < 0.9) sprites.push({ key: `flower-${Math.floor(rnd() * 4)}`, x, y, flip });
      } else if (pathDist(x, y) > 10 && !inWater(x, y) && r < 0.1) {
        const inB = BASES.some((b: Circle) => Math.hypot(x - b.x, y - b.y) < b.r);
        if (!inB && Math.abs(y - POS.laneY) > 70) sprites.push({ key: `flower-${Math.floor(rnd() * 4)}`, x, y, flip });
      }
    }
  }
  // ruínas nas clareiras
  for (const c of CLEARINGS) {
    for (let i = 0; i < 6; i++) {
      const a = (i / 6) * Math.PI * 2 + 0.3;
      const x = c.x + Math.cos(a) * (c.r - 34);
      const y = c.y + Math.sin(a) * (c.r - 40);
      if (inWater(x, y) || Math.abs(x - 1200) < 60) continue;
      sprites.push({ key: i % 2 === 0 ? 'pillar-0' : 'pillar-1', x, y, flip: i % 3 === 0 });
    }
    sprites.push({ key: 'crystal-deco', x: c.x - 70, y: c.y + 10, flip: false });
  }
  // ruínas espalhadas pelos flancos
  const ruinSpots = [
    [700, 440], [1000, 560], [1500, 440], [1760, 570], [700, 1960], [1000, 1840], [1500, 1960], [1760, 1830],
    [980, 1110], [1420, 1290], [520, 1300], [1880, 1100],
  ];
  for (const [x, y] of ruinSpots) sprites.push({ key: rnd() < 0.5 ? 'pillar-1' : 'ruin-wall', x, y, flip: rnd() < 0.5 });
  // lanternas nas bases
  for (const b of BASES) {
    for (let i = 0; i < 6; i++) {
      const a = (i / 6) * Math.PI * 2 + Math.PI / 6;
      sprites.push({ key: 'lantern', x: b.x + Math.cos(a) * 185, y: b.y + Math.sin(a) * 185, flip: false });
    }
  }
  return { sprites, baked };
}
