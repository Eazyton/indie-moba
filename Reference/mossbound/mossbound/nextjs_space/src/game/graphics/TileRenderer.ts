import Phaser from 'phaser';
import { MAP, POS } from '../config/balance';
import { BASES, CLEARINGS, PONDS, buildDecor, fbm, mulberry, noise, pathDist, walkDist, inWater, Decor } from '../world/MapLayout';

type RGB = [number, number, number];
const GRASS: RGB[] = [[63, 111, 78], [70, 122, 82], [74, 124, 89], [86, 137, 95], [97, 150, 100]];
const FOREST: RGB[] = [[30, 62, 44], [36, 72, 50], [43, 84, 57]];
const DIRT: RGB[] = [[122, 86, 55], [146, 104, 67], [165, 121, 80], [184, 140, 96]];

function h2(x: number, y: number): number {
  let h = (x * 73856093) ^ (y * 19349663);
  h = Math.imul(h ^ (h >>> 13), 1274126177);
  return ((h ^ (h >>> 16)) >>> 0) / 4294967296;
}

const mix = (a: RGB, b: RGB, t: number): RGB => [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t];

function stoneDist(x: number, y: number): number {
  let d = Infinity;
  for (const b of BASES) d = Math.min(d, Math.hypot(x - b.x, y - b.y) - 175);
  for (const c of CLEARINGS) d = Math.min(d, Math.hypot(x - c.x, y - c.y) - 78);
  for (const t of [POS.blueTower, POS.redTower]) d = Math.min(d, Math.hypot(x - t.x, (y - t.y) * 1.3) - 56);
  return d;
}

function colorAt(x: number, y: number): RGB {
  const n = fbm(x * 0.011, y * 0.011);
  const dn = noise(x * 0.17, y * 0.17);
  // água
  for (const p of PONDS) {
    const dp = Math.hypot(x - p.x, y - p.y) - p.r;
    if (dp < 0) {
      if (dp > -3) return [16, 44, 54];
      const depth = Math.min(1, -dp / p.r);
      let c = mix([40, 100, 116], [18, 58, 74], depth);
      const rip = Math.sin((x * 0.9 + y * 0.35) * 0.18 + noise(x * 0.05, y * 0.05) * 6);
      if (rip > 0.93) c = [88, 150, 160];
      if (h2(x >> 1, y >> 1) < 0.006) c = [255, 179, 71];
      return c;
    }
    if (dp < 7) return mix([176, 152, 108], [120, 128, 84], dn);
  }
  // pedra (praças das bases, ruínas)
  const sd = stoneDist(x, y) + (dn - 0.5) * 10;
  if (sd < 0) {
    const tx = Math.floor(x / 16);
    const off = tx % 2 === 0 ? 0 : 8;
    const ty = Math.floor((y + off) / 16);
    const lx = x % 16;
    const ly = (y + off) % 16;
    const tv = h2(tx, ty);
    if (lx < 2 || ly < 2) return [70, 74, 68];
    let c: RGB = mix([128, 132, 120], [158, 163, 148], tv);
    if (lx < 4 && ly < 4) c = [178, 182, 166];
    if (lx > 13 || ly > 13) c = mix(c, [90, 94, 86], 0.5);
    const moss = n + dn * 0.3 + (sd > -24 ? 0.25 : 0);
    if (moss > 0.82) c = dn > 0.5 ? [74, 124, 89] : [96, 150, 100];
    return c;
  }
  const pd = pathDist(x, y) + (noise(x * 0.045, y * 0.045) - 0.5) * 16;
  if (pd < 0) {
    let idx = Math.floor(n * 3.2 + dn * 0.9);
    idx = Math.max(0, Math.min(3, idx));
    let c = DIRT[idx] ?? DIRT[1] ?? [140, 100, 60];
    if (pd > -5) c = mix(c, [96, 78, 50], 0.45);
    const hh = h2(x >> 1, y >> 1);
    if (hh < 0.012) c = [200, 176, 136];
    else if (hh < 0.02) c = [98, 70, 44];
    return c;
  }
  const wd = walkDist(x, y);
  if (wd > 0) {
    const t = Math.min(1, wd / 70);
    const idx = Math.max(0, Math.min(2, Math.floor(n * 2.5 + dn * 0.8)));
    const base = FOREST[idx] ?? FOREST[1] ?? [36, 72, 50];
    return mix(GRASS[1] ?? base, base, t);
  }
  let gi = Math.floor(n * 4.4 + (dn - 0.5) * 1.3);
  gi = Math.max(0, Math.min(4, gi));
  let c = GRASS[gi] ?? GRASS[2] ?? [74, 124, 89];
  if (pd < 5) c = mix(c, [110, 120, 70], 0.35);
  if (h2(x >> 1, y >> 1) < 0.02) c = mix(c, [150, 196, 120], 0.6);
  return c;
}

export function renderGround(scene: Phaser.Scene): Decor[] {
  const S = MAP.size;
  const { sprites, baked } = buildDecor();
  if (scene.textures.exists('ground')) return sprites;
  const canvas = document.createElement('canvas');
  canvas.width = S;
  canvas.height = S;
  const ctx = canvas.getContext('2d');
  if (!ctx) return sprites;
  ctx.imageSmoothingEnabled = false;
  const img = ctx.createImageData(S, S);
  const d = img.data;
  for (let by = 0; by < S; by += 2) {
    for (let bx = 0; bx < S; bx += 2) {
      const c = colorAt(bx, by);
      const r = c[0] | 0, g = c[1] | 0, b = c[2] | 0;
      const i0 = (by * S + bx) * 4;
      const i1 = i0 + S * 4;
      d[i0] = r; d[i0 + 1] = g; d[i0 + 2] = b; d[i0 + 3] = 255;
      d[i0 + 4] = r; d[i0 + 5] = g; d[i0 + 6] = b; d[i0 + 7] = 255;
      d[i1] = r; d[i1 + 1] = g; d[i1 + 2] = b; d[i1 + 3] = 255;
      d[i1 + 4] = r; d[i1 + 5] = g; d[i1 + 6] = b; d[i1 + 7] = 255;
    }
  }
  ctx.putImageData(img, 0, 0);
  const rnd = mulberry(99);
  const px = (x: number, y: number, c: string, w = 2, h = 2): void => { ctx.fillStyle = c; ctx.fillRect(Math.round(x), Math.round(y), w, h); };
  // tufos de grama, flores minúsculas e pedrinhas
  for (let i = 0; i < 16000; i++) {
    const x = rnd() * S;
    const y = rnd() * S;
    const wd = walkDist(x, y);
    if (inWater(x, y)) continue;
    const pd = pathDist(x, y);
    if (wd < 0 && pd > 4 && stoneDist(x, y) > 4) {
      const r = rnd();
      if (r < 0.7) {
        const c = rnd() < 0.5 ? '#2f5f3e' : '#86bf6e';
        px(x, y, c, 1, 3); px(x + 2, y - 1, c, 1, 4); px(x + 4, y + 1, c, 1, 2);
      } else if (r < 0.9) {
        const fc = ['#ff8fb0', '#ffe07a', '#f5e6c8', '#b9a5ff'][Math.floor(rnd() * 4)] ?? '#fff';
        px(x, y, fc); px(x + 1, y + 2, '#2f5f3e', 1, 2);
      } else {
        px(x, y, '#9aa08c', 3, 2); px(x, y, '#c4c8b4', 1, 1);
      }
    } else if (pd < -4 && stoneDist(x, y) > 0) {
      if (rnd() < 0.5) { px(x, y, '#c9ad80', 2, 2); px(x + 1, y + 2, '#6e4c2c', 2, 1); }
      else px(x, y, '#7e5a38', 3, 1);
    } else if (wd > 0 && wd < 400) {
      if (rnd() < 0.25) { ctx.fillStyle = '#1d3d2a'; ctx.fillRect(x, y, 6, 3); ctx.fillStyle = '#3b6f47'; ctx.fillRect(x + 1, y, 3, 1); }
    }
  }
  // raízes nas bordas da floresta
  ctx.strokeStyle = '#4a3020';
  for (let i = 0; i < 500; i++) {
    const x = rnd() * S;
    const y = rnd() * S;
    const wd = walkDist(x, y);
    if (wd < -6 || wd > 12 || inWater(x, y) || pathDist(x, y) < 0) continue;
    let cx = x, cy = y;
    const a = rnd() * Math.PI * 2;
    for (let k = 0; k < 8; k++) {
      cx += Math.cos(a + Math.sin(k) * 0.6) * 2;
      cy += Math.sin(a + Math.sin(k) * 0.6) * 2;
      px(cx, cy, k % 3 === 0 ? '#6b4428' : '#4a3020');
    }
  }
  // rachaduras nas praças
  for (let i = 0; i < 600; i++) {
    const x = rnd() * S;
    const y = rnd() * S;
    if (stoneDist(x, y) > -6) continue;
    let cx = x, cy = y;
    for (let k = 0; k < 5; k++) { cx += (rnd() - 0.5) * 4; cy += rnd() * 2; px(cx, cy, '#555a50', 1, 1); }
  }
  // círculo rúnico nas bases
  BASES.forEach((b: { x: number; y: number }, team: number) => {
    const col = team === 0 ? '#00c8b4' : '#ff6b6b';
    for (let a = 0; a < 96; a++) {
      const ang = (a / 96) * Math.PI * 2;
      px(b.x + Math.cos(ang) * 120, b.y + Math.sin(ang) * 120, '#5a5e54', 3, 3);
      if (a % 8 === 0) { px(b.x + Math.cos(ang) * 132, b.y + Math.sin(ang) * 132, col, 3, 3); px(b.x + Math.cos(ang) * 132 + 1, b.y + Math.sin(ang) * 132 - 3, col, 1, 2); }
    }
  });
  // vitórias-régias
  for (const p of PONDS) {
    for (let i = 0; i < 6; i++) {
      const a = rnd() * Math.PI * 2;
      const r = rnd() * (p.r - 14);
      const x = p.x + Math.cos(a) * r;
      const y = p.y + Math.sin(a) * r;
      ctx.fillStyle = '#2d6a3d'; ctx.fillRect(x - 4, y - 2, 8, 5);
      ctx.fillStyle = '#4f9a5a'; ctx.fillRect(x - 3, y - 2, 6, 3);
      if (i % 2 === 0) px(x, y - 3, '#ff9fbf');
    }
  }
  // árvores densas assadas na textura (floresta profunda)
  baked.sort((a: Decor, b: Decor) => a.y - b.y);
  for (const t of baked) {
    const src = scene.textures.get(t.key)?.getSourceImage() as HTMLCanvasElement | undefined;
    if (!src) continue;
    ctx.save();
    ctx.globalAlpha = 0.35;
    ctx.fillStyle = '#000';
    ctx.beginPath();
    ctx.ellipse(t.x, t.y - 2, 16, 5, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.restore();
    if (t.flip) {
      ctx.save();
      ctx.translate(Math.round(t.x), 0);
      ctx.scale(-1, 1);
      ctx.drawImage(src, -30, Math.round(t.y - 72));
      ctx.restore();
    } else ctx.drawImage(src, Math.round(t.x - 30), Math.round(t.y - 72));
  }
  scene.textures.addCanvas('ground', canvas);
  // minimapa
  const mm = document.createElement('canvas');
  mm.width = 150; mm.height = 150;
  const mctx = mm.getContext('2d');
  if (mctx) {
    mctx.imageSmoothingEnabled = true;
    mctx.drawImage(canvas, 0, 0, 150, 150);
    mctx.fillStyle = 'rgba(10,20,15,0.18)';
    mctx.fillRect(0, 0, 150, 150);
    scene.textures.addCanvas('minimap-bg', mm);
  }
  return sprites;
}
