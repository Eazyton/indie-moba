import Phaser from 'phaser';
import { PixelCanvas } from './PixelCanvas';
import { makeTex } from './StructureFactory';

const MOSS = ['#2d5a3d', '#4a7c59', '#6fa66a', '#9bd07a'];
const ST = { d: '#4b4f49', m: '#7d837a', l: '#a3a996', h: '#c4c8b4' };

function tree(p: PixelCanvas, r: () => number, v: number): void {
  const cx = 30;
  const palettes = [
    ['#1f4a30', '#2f6b42', '#4a8f55', '#8cc46a'],
    ['#173d2c', '#245a3e', '#3a7a4e', '#6aa86a'],
    ['#1d4a3e', '#2b6a55', '#459172', '#9ad6a0'],
  ];
  const leaves = palettes[v] ?? palettes[0] ?? MOSS;
  // raízes e tronco
  p.line(cx - 3, 70, cx - 10, 74, '#3f2616');
  p.line(cx + 3, 70, cx + 9, 74, '#3f2616');
  p.rect(cx - 4, 44, 8, 28, '#5e3a22');
  p.rect(cx - 4, 44, 2, 28, '#3f2616');
  p.rect(cx + 2, 46, 1, 24, '#8a5a34');
  for (let i = 0; i < 8; i++) p.px(cx - 3 + r() * 6, 48 + r() * 22, r() < 0.5 ? '#3f2616' : MOSS[1] ?? '#4a7c59');
  if (v === 1) {
    p.blob(cx, 40, 15, leaves, r);
    p.blob(cx - 1, 26, 13, leaves, r);
    p.blob(cx, 13, 10, leaves, r);
  } else {
    p.blob(cx - 12, 36, 12, leaves, r);
    p.blob(cx + 12, 35, 12, leaves, r);
    p.blob(cx, 22, 16, leaves, r);
    p.blob(cx - 2, 38, 12, leaves, r);
  }
  if (v === 2) for (let i = 0; i < 10; i++) { const x = cx - 18 + r() * 36; const y = 12 + r() * 30; p.px(x, y, '#ffd0e0'); p.px(x + 1, y, '#ff9fbf'); }
  if (v === 0) for (let i = 0; i < 5; i++) p.px(cx - 14 + r() * 28, 16 + r() * 26, '#ffb347');
}

function bush(p: PixelCanvas, r: () => number, v: number): void {
  const cols = v === 0 ? MOSS : ['#24503a', '#3a7350', '#5d9a60', '#c6e38a'];
  p.blob(9, 13, 7, cols, r);
  p.blob(19, 13, 7, cols, r);
  p.blob(14, 9, 7, cols, r);
  if (v === 1) for (let i = 0; i < 4; i++) p.px(6 + r() * 16, 5 + r() * 10, '#e05a5a');
}

function rock(p: PixelCanvas, r: () => number, v: number): void {
  const w = [11, 9, 7][v] ?? 9;
  p.ellipse(13, 13, w, w * 0.65, ST.d);
  p.ellipse(12, 12, w - 1, w * 0.6 - 1, ST.m);
  p.ellipse(10, 10, w * 0.5, w * 0.3, ST.l);
  p.px(8, 9, ST.h);
  p.line(12, 9, 15, 14, ST.d);
  for (let i = 0; i < w * 2; i++) p.px(13 - w + r() * w * 2, 13 - w * 0.6 + r() * w * 0.5, MOSS[Math.floor(r() * 4)] ?? MOSS[1]);
}

function mushroom(p: PixelCanvas, v: number): void {
  const cap = v === 0 ? ['#0f6b6b', '#1fb8b0', '#9ff5ea'] : ['#8a4b12', '#ffb347', '#fff0b0'];
  const one = (x: number, y: number, s: number): void => {
    p.rect(x - 1, y, 3, s + 2, '#e8dcc0');
    p.px(x + 1, y + 1, '#b8a888');
    p.ellipse(x, y, s + 1, Math.max(1, s * 0.6), cap[0] ?? '#000');
    p.ellipse(x - 1, y - 1, s, Math.max(1, s * 0.5), cap[1] ?? '#fff');
    p.px(x - 2, y - 1, cap[2] ?? '#fff');
    p.px(x + 1, y - 2, cap[2] ?? '#fff');
  };
  one(10, 9, 4);
  one(5, 13, 2);
}

function flower(p: PixelCanvas, v: number, frame: number): void {
  const petals = ['#ff8fb0', '#ffe07a', '#b9a5ff', '#f5e6c8'][v] ?? '#fff';
  const sway = frame === 0 ? 0 : 1;
  p.line(5, 13, 5 + sway, 6, '#3f7a3a');
  p.px(4, 10, '#6fa66a');
  p.px(6, 11, '#6fa66a');
  const fx = 5 + sway;
  p.px(fx, 3, petals); p.px(fx - 1, 4, petals); p.px(fx + 1, 4, petals); p.px(fx, 5, petals);
  p.px(fx, 4, '#ffb347');
  p.px(2, 12, '#3f7a3a'); p.px(8, 12, '#3f7a3a');
}

export function buildDecor(scene: Phaser.Scene): void {
  for (let v = 0; v < 3; v++) makeTex(scene, `tree-${v}`, 60, 76, (p: PixelCanvas, r: () => number) => tree(p, r, v));
  for (let v = 0; v < 2; v++) makeTex(scene, `bush-${v}`, 28, 22, (p: PixelCanvas, r: () => number) => bush(p, r, v));
  for (let v = 0; v < 3; v++) makeTex(scene, `rock-${v}`, 26, 22, (p: PixelCanvas, r: () => number) => rock(p, r, v));
  for (let v = 0; v < 2; v++) makeTex(scene, `mush-${v}`, 18, 18, (p: PixelCanvas) => mushroom(p, v));
  for (let v = 0; v < 4; v++) {
    makeTex(scene, `flower-${v}`, 12, 15, (p: PixelCanvas) => flower(p, v, 0));
    makeTex(scene, `flower-${v}b`, 12, 15, (p: PixelCanvas) => flower(p, v, 1));
  }
  makeTex(scene, 'pillar-0', 20, 46, (p: PixelCanvas, r: () => number) => {
    p.rect(2, 40, 16, 5, ST.d);
    p.rect(4, 6, 12, 35, ST.m);
    p.rect(4, 6, 3, 35, ST.d);
    p.rect(13, 6, 2, 35, ST.l);
    for (let y = 10; y < 40; y += 7) p.rect(4, y, 12, 1, ST.d);
    p.rect(2, 3, 16, 4, ST.l);
    p.rect(2, 3, 16, 1, ST.h);
    for (let y = 8; y < 42; y++) { const x = 6 + Math.round(Math.sin(y * 0.5) * 4); p.px(x, y, MOSS[0] ?? ''); if (r() < 0.5) p.px(x + 1, y, MOSS[2] ?? ''); }
    p.px(9, 20, '#ffb347'); p.px(10, 21, '#ffb347');
  });
  makeTex(scene, 'pillar-1', 20, 30, (p: PixelCanvas, r: () => number) => {
    p.rect(2, 24, 16, 5, ST.d);
    p.rect(4, 8, 12, 17, ST.m);
    p.rect(4, 8, 3, 17, ST.d);
    p.rect(13, 8, 2, 17, ST.l);
    for (let x = 4; x < 16; x++) p.px(x, 8 - ((x * 7) % 4), ST.m);
    for (let i = 0; i < 18; i++) p.px(3 + r() * 14, 6 + r() * 20, MOSS[Math.floor(r() * 4)] ?? '');
  });
  makeTex(scene, 'ruin-wall', 46, 30, (p: PixelCanvas, r: () => number) => {
    for (let row = 0; row < 4; row++) {
      const w = 42 - row * 8 - Math.floor(r() * 6);
      for (let x = 2; x < 2 + w; x += 9) {
        p.rect(x, 24 - row * 6, 8, 5, ST.m);
        p.rect(x, 24 - row * 6, 8, 1, ST.l);
        p.rect(x, 28 - row * 6, 8, 1, ST.d);
      }
    }
    for (let i = 0; i < 30; i++) p.px(2 + r() * 40, 6 + r() * 24, MOSS[Math.floor(r() * 4)] ?? '');
  });
  makeTex(scene, 'lantern', 14, 34, (p: PixelCanvas) => {
    p.rect(6, 12, 2, 21, '#5e3a1c');
    p.rect(6, 12, 1, 21, '#8b5a2b');
    p.rect(3, 31, 8, 2, '#3f2616');
    p.rect(3, 4, 8, 9, '#5e3a1c');
    p.rect(4, 5, 6, 7, '#ffb347');
    p.rect(5, 6, 3, 4, '#fff0b0');
    p.rect(2, 2, 10, 2, '#3f2616');
    p.px(7, 0, '#3f2616'); p.px(7, 1, '#3f2616');
  });
  makeTex(scene, 'crystal-deco', 24, 30, (p: PixelCanvas) => {
    p.ellipse(12, 26, 10, 3, ST.d);
    p.ellipse(12, 25, 9, 2, ST.m);
    const c = (x: number, top: number, h: number): void => {
      for (let y = 0; y < h; y++) { const hw = y < 3 ? y : Math.max(1, 2); p.rect(x - hw, top + y, hw * 2 + 1, 1, '#ffb347'); p.px(x - hw, top + y, '#fff0b0'); p.px(x + hw, top + y, '#c46a1c'); }
    };
    c(12, 4, 20); c(6, 12, 12); c(18, 10, 14);
  });
  // efeitos e projéteis
  makeTex(scene, 'proj-q', 22, 10, (p: PixelCanvas) => {
    for (let x = 0; x < 20; x++) { const hw = x < 12 ? Math.round(x / 4) : Math.round((20 - x) / 2.5); p.rect(x, 5 - hw, 1, hw * 2 + 1, x > 12 ? '#ffd27a' : '#ffb347'); }
    p.rect(10, 4, 9, 2, '#fff6d0');
  });
  makeTex(scene, 'proj-bolt', 20, 10, (p: PixelCanvas) => {
    for (let x = 0; x < 18; x++) { const hw = x < 10 ? Math.round(x / 4) : Math.round((18 - x) / 2.5); p.rect(x, 5 - hw, 1, hw * 2 + 1, x > 10 ? '#c9a0ff' : '#7a3ae0'); }
    p.rect(9, 4, 8, 2, '#f0e0ff');
  });
  makeTex(scene, 'proj-orb', 12, 12, (p: PixelCanvas) => { p.disc(6, 6, 4, '#7a3ae0'); p.disc(5, 5, 3, '#b27dff'); p.px(4, 4, '#ffffff'); p.px(5, 4, '#f0e0ff'); });
  makeTex(scene, 'proj-tower', 12, 12, (p: PixelCanvas) => {
    for (let y = 0; y < 10; y++) { const hw = y < 5 ? y : 9 - y; p.rect(6 - hw, 1 + y, hw * 2 + 1, 1, '#ffb347'); p.px(6 - hw, 1 + y, '#fff0b0'); }
    p.px(6, 3, '#ffffff');
  });
  makeTex(scene, 'proj-arrow', 14, 5, (p: PixelCanvas) => { p.rect(1, 2, 10, 1, '#b07a42'); p.rect(11, 1, 2, 3, '#cfd8dc'); p.px(13, 2, '#ffffff'); p.px(0, 1, '#f5e6c8'); p.px(0, 3, '#f5e6c8'); }, false);
  makeTex(scene, 'px-spark', 3, 3, (p: PixelCanvas) => { p.px(1, 0, '#ffffff'); p.rect(0, 1, 3, 1, '#ffffff'); p.px(1, 2, '#ffffff'); }, false);
  makeTex(scene, 'px-dot', 2, 2, (p: PixelCanvas) => p.rect(0, 0, 2, 2, '#ffffff'), false);
  makeTex(scene, 'px-ember', 4, 4, (p: PixelCanvas) => { p.rect(1, 0, 2, 4, '#ffb347'); p.rect(0, 1, 4, 2, '#ffb347'); p.rect(1, 1, 2, 2, '#fff0b0'); }, false);
  makeTex(scene, 'px-leaf', 5, 4, (p: PixelCanvas) => { p.rect(1, 0, 3, 1, '#6fa66a'); p.rect(0, 1, 5, 2, '#4a7c59'); p.px(4, 3, '#2d5a3d'); }, false);
  makeTex(scene, 'px-smoke', 10, 10, (p: PixelCanvas) => { p.alpha(0.5); p.disc(5, 5, 4, '#8a8a80'); p.alpha(0.8); p.disc(4, 4, 2, '#b0b0a4'); p.alpha(1); }, false);
  makeTex(scene, 'px-bark', 5, 5, (p: PixelCanvas) => { p.rect(0, 1, 5, 3, '#6b4428'); p.rect(1, 0, 3, 5, '#8b5a2b'); p.px(2, 2, '#b07a42'); }, false);
  makeTex(scene, 'shadow', 32, 12, (p: PixelCanvas) => { p.alpha(0.35); p.ellipse(16, 6, 15, 5, '#000000'); p.alpha(0.2); p.ellipse(16, 6, 11, 3, '#000000'); p.alpha(1); }, false);
  makeTex(scene, 'fire-flower', 14, 16, (p: PixelCanvas) => {
    p.line(7, 15, 7, 8, '#3f7a3a');
    p.px(5, 12, '#6fa66a'); p.px(9, 11, '#6fa66a');
    p.disc(7, 5, 4, '#e0452a'); p.disc(7, 5, 3, '#ff8a3a'); p.disc(7, 5, 1, '#ffe07a');
    p.px(7, 0, '#ffb347'); p.px(3, 3, '#ffb347'); p.px(11, 3, '#ffb347');
  });
  makeTex(scene, 'bark-ring', 56, 56, (p: PixelCanvas, r: () => number) => {
    for (let a = 0; a < 40; a++) {
      const ang = (a / 40) * Math.PI * 2;
      const x = 28 + Math.cos(ang) * 24;
      const y = 28 + Math.sin(ang) * 24;
      p.rect(x - 2, y - 2, 4, 4, a % 2 === 0 ? '#6b4428' : '#8b5a2b');
      p.px(x - 1, y - 1, '#b07a42');
      if (a % 5 === 0) { p.px(x + (r() - 0.5) * 6, y - 3, '#6fa66a'); p.px(x, y - 4, '#9bd07a'); }
    }
  });
  // brilho radial suave (iluminação)
  if (!scene.textures.exists('glow')) {
    const c = document.createElement('canvas');
    c.width = 64; c.height = 64;
    const ctx = c.getContext('2d');
    if (ctx) {
      const g = ctx.createRadialGradient(32, 32, 0, 32, 32, 32);
      g.addColorStop(0, 'rgba(255,255,255,1)');
      g.addColorStop(0.35, 'rgba(255,255,255,0.45)');
      g.addColorStop(1, 'rgba(255,255,255,0)');
      ctx.fillStyle = g;
      ctx.fillRect(0, 0, 64, 64);
      scene.textures.addCanvas('glow', c);
    }
  }
}
