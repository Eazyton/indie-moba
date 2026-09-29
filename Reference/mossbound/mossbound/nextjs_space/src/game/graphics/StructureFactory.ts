import Phaser from 'phaser';
import { PixelCanvas, shade } from './PixelCanvas';
import { mulberry } from '../world/MapLayout';
import { TEAM_PAL } from './SpriteFactory';

const INK = '#1b1410';
const ST = { d: '#4b4f49', m: '#7d837a', l: '#a3a996', h: '#c4c8b4' };
const MOSS = ['#2d5a3d', '#4a7c59', '#6fa66a', '#9bd07a'];

export function makeTex(scene: Phaser.Scene, key: string, w: number, h: number, draw: (p: PixelCanvas, r: () => number) => void, outline = true, glow?: (p: PixelCanvas, r: () => number) => void, seed = 7): void {
  if (scene.textures.exists(key)) return;
  const p = new PixelCanvas(w, h);
  let s = seed;
  for (let i = 0; i < key.length; i++) s = s * 31 + key.charCodeAt(i);
  const r = mulberry(s);
  draw(p, r);
  if (outline) p.outline(0, 0, w, h, INK);
  glow?.(p, r);
  p.alpha(1);
  scene.textures.addCanvas(key, p.canvas);
}

function stoneBlocks(p: PixelCanvas, x: number, y: number, w: number, h: number, r: () => number, bh = 6): void {
  p.rect(x, y, w, h, ST.m);
  let row = 0;
  for (let yy = y; yy < y + h; yy += bh) {
    p.rect(x, yy, w, 1, ST.l);
    if (yy > y) p.rect(x, yy - 1, w, 1, ST.d);
    const off = row % 2 === 0 ? 0 : 5;
    for (let xx = x + off; xx < x + w; xx += 10) {
      p.rect(xx, yy, 1, Math.min(bh - 1, y + h - yy), ST.d);
      p.px(xx + 1, yy + 1, ST.h);
    }
    row++;
  }
  p.rect(x, y, 2, h, ST.d);
  p.rect(x + w - 1, y, 1, h, ST.l);
  for (let i = 0; i < w * h * 0.06; i++) p.px(x + r() * w, y + r() * h, r() < 0.5 ? ST.d : ST.h);
  // musgo
  for (let i = 0; i < w * 0.9; i++) {
    const mx = x + r() * w;
    const my = y + (r() < 0.7 ? r() * 3 : r() * h);
    p.px(mx, my, MOSS[Math.floor(r() * 4)] ?? MOSS[1]);
    if (r() < 0.4) p.px(mx + 1, my, MOSS[1]);
  }
}

function wood(p: PixelCanvas, x: number, y: number, w: number, h: number, r: () => number): void {
  p.rect(x, y, w, h, '#8b5a2b');
  p.rect(x, y, 1, h, '#5e3a1c');
  p.rect(x + w - 1, y, 1, h, '#b07a42');
  for (let i = 0; i < h * 0.5; i++) p.px(x + 1 + Math.floor(r() * (w - 2)), y + r() * h, r() < 0.5 ? '#6e4a22' : '#a06a36');
}

function vine(p: PixelCanvas, x: number, y0: number, y1: number, r: () => number): void {
  for (let y = y0; y < y1; y++) {
    const xx = x + Math.round(Math.sin(y * 0.6) * 2);
    p.px(xx, y, MOSS[0]);
    if (r() < 0.35) p.px(xx + (r() < 0.5 ? -1 : 1), y, MOSS[2]);
  }
}

function tower(p: PixelCanvas, r: () => number, team: number): void {
  const t = TEAM_PAL[team] ?? TEAM_PAL[0];
  // raízes
  for (const s of [-1, 1]) {
    p.line(32 + s * 10, 76, 32 + s * 27, 82, '#4a3020');
    p.line(32 + s * 11, 77, 32 + s * 24, 83, '#6b4428');
    p.line(32 + s * 16, 78, 32 + s * 20, 84, '#4a3020');
  }
  stoneBlocks(p, 9, 64, 46, 16, r, 7);
  stoneBlocks(p, 14, 56, 36, 9, r, 5);
  // pilares
  wood(p, 16, 24, 6, 33, r);
  wood(p, 42, 24, 6, 33, r);
  vine(p, 18, 30, 56, r);
  vine(p, 45, 26, 50, r);
  // altar
  stoneBlocks(p, 26, 47, 12, 10, r, 5);
  p.rect(25, 46, 14, 2, ST.h);
  // viga e telhado de folhas
  p.rect(8, 21, 48, 4, '#5e3a1c');
  p.rect(8, 21, 48, 1, '#a06a36');
  for (let row = 0; row < 15; row++) {
    const w = 12 + row * 2.6;
    const x = 32 - w / 2;
    const y = 6 + row;
    const col = MOSS[row % 3 === 0 ? 0 : row % 3 === 1 ? 1 : 2] ?? MOSS[1];
    p.rect(x, y, w, 1, col);
    p.px(x, y, MOSS[0]);
    if (row > 4) for (let k = 0; k < 3; k++) p.px(x + 2 + r() * (w - 4), y, MOSS[3]);
  }
  for (let x = 12; x < 53; x += 4) p.rect(x, 20, 3, 2, MOSS[0]);
  p.rect(30, 1, 4, 6, t.dark);
  p.rect(31, 1, 2, 5, t.main);
  p.px(31, 2, t.light);
  // estandartes
  for (const bx of [10, 50]) {
    p.rect(bx, 25, 5, 13, t.main);
    p.rect(bx, 25, 1, 13, t.dark);
    p.rect(bx + 4, 25, 1, 13, t.light);
    p.px(bx + 2, 38, t.main);
    p.px(bx + 2, 30, '#f5e6c8');
    p.px(bx + 2, 31, '#f5e6c8');
    p.px(bx + 1, 32, '#f5e6c8');
    p.px(bx + 3, 32, '#f5e6c8');
  }
  // runas
  for (const [rx, ry] of [[14, 70], [22, 72], [42, 72], [50, 70], [31, 51]]) {
    p.px(rx ?? 0, ry ?? 0, t.main);
    p.px((rx ?? 0) + 1, (ry ?? 0) + 1, t.light);
    p.px(rx ?? 0, (ry ?? 0) + 2, t.main);
  }
  // cogumelos na base
  p.rect(11, 60, 3, 2, '#e2a24a');
  p.px(12, 62, '#f5e6c8');
  p.rect(50, 61, 3, 2, '#e2a24a');
  p.px(51, 63, '#f5e6c8');
}

function crystal(p: PixelCanvas, cx: number, top: number, w: number, h: number, main: string, light: string, dark: string): void {
  for (let y = 0; y < h; y++) {
    const t = y < h * 0.35 ? y / (h * 0.35) : 1 - (y - h * 0.35) / (h * 0.65);
    const half = Math.max(0, Math.round((w / 2) * t));
    p.rect(cx - half, top + y, half * 2 + 1, 1, main);
    p.rect(cx - half, top + y, Math.max(1, Math.floor(half * 0.6)), 1, light);
    p.px(cx + half, top + y, dark);
  }
  p.line(cx, top + 1, cx, top + h - 2, shade(light, 0.4));
}

function nexus(p: PixelCanvas, r: () => number, team: number): void {
  const t = TEAM_PAL[team] ?? TEAM_PAL[0];
  const leaves = team === 0
    ? ['#1f4f45', '#2f7a64', '#4aa88a', '#8ff0cf']
    : ['#5a2a24', '#9c4432', '#d9704e', '#ffc09a'];
  const cx = 56;
  // anel de pedra
  p.ellipse(cx, 111, 46, 8, ST.d);
  p.ellipse(cx, 109, 45, 7, ST.m);
  p.ellipse(cx - 2, 108, 40, 5, ST.l);
  for (let a = 0; a < 16; a++) {
    const x = cx + Math.cos((a / 16) * Math.PI * 2) * 42;
    const y = 109 + Math.sin((a / 16) * Math.PI * 2) * 6;
    p.px(x, y, ST.d);
    if (a % 3 === 0) p.px(x + 1, y - 1, t.main);
  }
  for (let i = 0; i < 40; i++) p.px(cx - 44 + r() * 88, 104 + r() * 8, MOSS[Math.floor(r() * 3)] ?? MOSS[1]);
  // raízes
  for (let k = 0; k < 7; k++) {
    const s = k - 3;
    p.line(cx + s * 4, 100, cx + s * 11 + (r() - 0.5) * 6, 112, '#3f2616');
    p.line(cx + s * 4 + 1, 100, cx + s * 11 + 1, 111, '#6b4428');
  }
  // tronco
  for (let y = 56; y <= 104; y++) {
    const tt = (y - 56) / 48;
    const hw = Math.round(10 + tt * tt * 9);
    p.rect(cx - hw, y, hw * 2, 1, '#5e3a22');
    p.rect(cx - hw, y, 3, 1, '#3f2616');
    p.rect(cx + hw - 4, y, 3, 1, '#8a5a34');
    for (let g = -hw + 5; g < hw - 5; g += 5) p.px(cx + g + Math.round(Math.sin(y * 0.3 + g) * 1), y, '#3f2616');
  }
  for (let i = 0; i < 30; i++) p.px(cx - 16 + r() * 32, 64 + r() * 40, r() < 0.6 ? MOSS[1] : MOSS[2]);
  // cavidade
  p.ellipse(cx, 86, 9, 13, '#2a160c');
  p.ellipse(cx, 87, 7, 11, '#120905');
  p.alpha(0.5);
  p.ellipse(cx, 92, 6, 5, t.dark);
  p.alpha(1);
  // galhos
  p.line(cx - 6, 60, cx - 30, 40, '#3f2616');
  p.line(cx - 5, 60, cx - 29, 41, '#6b4428');
  p.line(cx + 6, 60, cx + 30, 38, '#3f2616');
  p.line(cx + 5, 60, cx + 29, 39, '#6b4428');
  // copa
  const blobs: [number, number, number][] = [
    [cx - 32, 44, 15], [cx + 32, 42, 15], [cx, 24, 20], [cx - 22, 26, 16], [cx + 22, 24, 16],
    [cx - 38, 34, 11], [cx + 40, 32, 11], [cx - 12, 46, 16], [cx + 14, 46, 16], [cx, 14, 13],
  ];
  for (const [bx, byy, br] of blobs) p.blob(bx, byy, br, leaves, r);
  // vinhas e frutos luminosos
  for (let k = 0; k < 9; k++) {
    const vx = cx - 40 + k * 10 + Math.round(r() * 4);
    const len = 6 + Math.floor(r() * 10);
    p.line(vx, 52, vx, 52 + len, leaves[0] ?? '#000');
    p.px(vx, 53 + len, t.light);
  }
  for (let k = 0; k < 14; k++) p.px(cx - 40 + r() * 80, 8 + r() * 44, t.light);
}

export function buildStructures(scene: Phaser.Scene): void {
  for (const team of [0, 1]) {
    const t = TEAM_PAL[team] ?? TEAM_PAL[0];
    makeTex(scene, `tower-${team}`, 64, 86, (p: PixelCanvas, r: () => number) => tower(p, r, team));
    makeTex(scene, `nexus-${team}`, 112, 118, (p: PixelCanvas, r: () => number) => nexus(p, r, team));
    makeTex(scene, `tower-crystal-${team}`, 14, 22, (p: PixelCanvas) => crystal(p, 7, 1, 10, 20, t.main, t.light, t.dark));
    makeTex(scene, `nexus-crystal-${team}`, 24, 28, (p: PixelCanvas) => {
      crystal(p, 12, 2, 8, 24, t.main, t.light, t.dark);
      crystal(p, 6, 10, 6, 16, t.main, t.light, t.dark);
      crystal(p, 18, 8, 6, 18, t.main, t.light, t.dark);
    });
  }
  makeTex(scene, 'rubble', 64, 40, (p: PixelCanvas, r: () => number) => {
    p.ellipse(32, 32, 28, 7, ST.d);
    for (let i = 0; i < 14; i++) {
      const x = 8 + r() * 48;
      const y = 22 + r() * 12;
      const s = 3 + r() * 5;
      p.ellipse(x, y, s, s * 0.7, ST.m);
      p.px(x - 1, y - 1, ST.h);
    }
    wood(p, 12, 14, 5, 18, r);
    p.line(38, 30, 54, 16, '#5e3a1c');
    p.line(39, 30, 55, 16, '#8b5a2b');
    for (let i = 0; i < 20; i++) p.px(6 + r() * 52, 20 + r() * 16, MOSS[Math.floor(r() * 3)] ?? MOSS[1]);
  });
}
