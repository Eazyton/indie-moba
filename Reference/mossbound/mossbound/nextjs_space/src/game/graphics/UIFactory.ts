import Phaser from 'phaser';
import { PixelCanvas } from './PixelCanvas';
import { makeTex } from './StructureFactory';

const MOSS = ['#2d5a3d', '#4a7c59', '#6fa66a', '#9bd07a'];

function woodPanel(p: PixelCanvas, r: () => number, w: number, h: number, light: boolean): void {
  p.rect(0, 0, w, h, '#2a1a10');
  p.rect(2, 2, w - 4, h - 4, light ? '#9a6536' : '#7a4f2a');
  for (let y = 2; y < h - 2; y += 7) {
    p.rect(2, y, w - 4, 1, light ? '#b8804a' : '#94623a');
    p.rect(2, y + 6, w - 4, 1, '#5a3a1e');
  }
  for (let i = 0; i < w * h * 0.05; i++) p.px(3 + r() * (w - 6), 3 + r() * (h - 6), r() < 0.5 ? '#5e3a1c' : '#a8743e');
  p.rect(3, 3, w - 6, 1, light ? '#d9a068' : '#b07a42');
  p.rect(3, h - 4, w - 6, 1, '#3b2414');
  for (const [cx, cy] of [[5, 5], [w - 6, 5], [5, h - 6], [w - 6, h - 6]]) {
    p.rect((cx ?? 0) - 1, (cy ?? 0) - 1, 3, 3, '#6b5a3a');
    p.px(cx ?? 0, cy ?? 0, '#ffb347');
  }
}

function stoneIcon(p: PixelCanvas, bg: string): void {
  p.rect(0, 0, 32, 32, bg);
}

export function buildUI(scene: Phaser.Scene): void {
  makeTex(scene, 'ui-wood', 48, 48, (p: PixelCanvas, r: () => number) => woodPanel(p, r, 48, 48, false), false);
  makeTex(scene, 'ui-btn', 48, 48, (p: PixelCanvas, r: () => number) => woodPanel(p, r, 48, 48, false), false);
  makeTex(scene, 'ui-btn-hi', 48, 48, (p: PixelCanvas, r: () => number) => woodPanel(p, r, 48, 48, true), false);
  makeTex(scene, 'ui-parch', 48, 48, (p: PixelCanvas, r: () => number) => {
    p.rect(0, 0, 48, 48, '#5a3a1e');
    p.rect(2, 2, 44, 44, '#d8c39a');
    p.rect(4, 4, 40, 40, '#f5e6c8');
    for (let i = 0; i < 160; i++) p.px(4 + r() * 40, 4 + r() * 40, r() < 0.5 ? '#ead7b0' : '#fff4dc');
    for (let i = 0; i < 40; i++) { const e = Math.floor(r() * 4); const t = 2 + r() * 44; p.px(e === 0 ? 3 : e === 1 ? 44 : t, e === 2 ? 3 : e === 3 ? 44 : t, '#b89868'); }
  }, false);
  makeTex(scene, 'ui-stone', 48, 48, (p: PixelCanvas, r: () => number) => {
    p.rect(0, 0, 48, 48, '#23261f');
    p.rect(2, 2, 44, 44, '#5d635a');
    for (let y = 2; y < 46; y += 8) {
      p.rect(2, y, 44, 1, '#7d837a');
      for (let x = 2 + ((y / 8) % 2) * 6; x < 46; x += 12) p.rect(x, y, 1, 8, '#3b3f38');
    }
    for (let i = 0; i < 50; i++) p.px(3 + r() * 42, 3 + r() * 42, MOSS[Math.floor(r() * 3)] ?? '');
  }, false);
  makeTex(scene, 'ui-slot', 58, 58, (p: PixelCanvas, r: () => number) => {
    p.rect(0, 0, 58, 58, '#1d1f1a');
    p.rect(2, 2, 54, 54, '#6b7068');
    p.rect(2, 2, 54, 2, '#a3a996');
    p.rect(2, 54, 54, 2, '#3b3f38');
    p.rect(6, 6, 46, 46, '#15130f');
    for (let i = 0; i < 20; i++) p.px(2 + r() * 54, r() < 0.5 ? 2 + r() * 3 : 52 + r() * 3, MOSS[Math.floor(r() * 3)] ?? '');
  }, false);
  makeTex(scene, 'ui-frame', 64, 64, (p: PixelCanvas) => {
    p.disc(32, 32, 31, '#2a1a10');
    p.disc(32, 32, 29, '#c89650');
    p.disc(32, 32, 27, '#7a4f2a');
    p.disc(32, 32, 24, '#1a4a5c');
    p.disc(30, 29, 20, '#23596b');
    for (let a = 0; a < 12; a++) { const ang = (a / 12) * Math.PI * 2; p.px(32 + Math.cos(ang) * 28, 32 + Math.sin(ang) * 28, '#ffb347'); }
  }, false);
  // ícones de habilidade
  makeTex(scene, 'ic-aa', 32, 32, (p: PixelCanvas) => {
    stoneIcon(p, '#3a2a1a');
    p.line(5, 27, 22, 10, '#6b4424'); p.line(6, 27, 23, 10, '#b07a42');
    for (let k = 0; k < 7; k++) { const w = [1, 2, 2, 2, 1, 1, 0][k] ?? 0; for (let s = -w; s <= w; s++) p.px(22 + k - s * 0.7, 10 - k - s * 0.7, s === 0 ? '#fff0b0' : '#ffb347'); }
    p.rect(19, 12, 3, 3, '#3b2718');
  });
  makeTex(scene, 'ic-q', 32, 32, (p: PixelCanvas) => {
    stoneIcon(p, '#4a2a10');
    p.disc(12, 20, 7, '#d9822b'); p.disc(11, 19, 5, '#ffb347'); p.disc(10, 18, 2, '#fff0b0');
    for (let a = 0; a < 8; a++) { const ang = (a / 8) * Math.PI * 2; p.px(12 + Math.cos(ang) * 10, 20 + Math.sin(ang) * 10, '#ffb347'); }
    for (let x = 0; x < 12; x++) { const hw = x < 7 ? Math.round(x / 3) : Math.round((12 - x) / 2); p.rect(16 + x, 11 - hw - x * 0.5, 1, hw * 2 + 1, '#ffd27a'); }
  });
  makeTex(scene, 'ic-w', 32, 32, (p: PixelCanvas) => {
    stoneIcon(p, '#243a20');
    p.ellipse(16, 16, 11, 13, '#4a2e1a'); p.ellipse(16, 16, 9, 11, '#8b5a2b');
    for (let y = 6; y < 27; y += 3) p.rect(10, y, 12, 1, '#6b4428');
    p.ellipse(16, 16, 4, 5, '#b07a42'); p.ellipse(16, 16, 2, 3, '#6b4428');
    p.rect(14, 3, 4, 3, '#6fa66a'); p.px(18, 2, '#9bd07a');
  });
  makeTex(scene, 'ic-e', 32, 32, (p: PixelCanvas) => {
    stoneIcon(p, '#3a1a10');
    for (let k = 0; k < 4; k++) { p.rect(4 + k * 2, 8 + k * 5, 14 - k * 2, 2, k % 2 ? '#ff8a3a' : '#ffb347'); }
    p.disc(23, 16, 5, '#e0452a'); p.disc(23, 15, 3, '#ffb347'); p.px(23, 14, '#fff0b0');
    p.px(8, 26, '#ffb347'); p.px(12, 28, '#ff8a3a'); p.px(5, 20, '#ffe07a');
  });
  makeTex(scene, 'ic-r', 32, 32, (p: PixelCanvas) => {
    stoneIcon(p, '#3a0f10');
    p.ellipse(16, 20, 12, 7, '#7a1e14'); p.ellipse(16, 20, 10, 5, '#b8341e');
    for (let a = 0; a < 6; a++) { const ang = (a / 6) * Math.PI * 2; const x = 16 + Math.cos(ang) * 9; const y = 19 + Math.sin(ang) * 5; p.disc(x, y - 3, 2, '#ff8a3a'); p.px(x, y - 4, '#ffe07a'); p.px(x, y - 6, '#ffb347'); }
    p.disc(16, 12, 4, '#ff8a3a'); p.disc(16, 11, 2, '#ffe07a'); p.px(16, 5, '#ffb347'); p.px(16, 6, '#ff8a3a');
  });
  makeTex(scene, 'ic-lock', 20, 22, (p: PixelCanvas) => {
    p.rect(5, 1, 10, 2, '#9aa08c'); p.rect(4, 2, 2, 8, '#9aa08c'); p.rect(14, 2, 2, 8, '#9aa08c');
    p.rect(2, 9, 16, 12, '#c89650'); p.rect(2, 9, 16, 2, '#e8c060'); p.rect(9, 13, 2, 5, '#3b2718');
  });
  makeTex(scene, 'ic-seed', 16, 16, (p: PixelCanvas) => {
    p.ellipse(8, 9, 5, 6, '#b8801e'); p.ellipse(7, 8, 4, 5, '#ffc84a'); p.px(6, 6, '#fff0b0'); p.px(5, 7, '#fff0b0');
    p.line(8, 3, 10, 1, '#4a7c59'); p.px(11, 1, '#6fa66a');
  });
  makeTex(scene, 'ic-skull', 12, 12, (p: PixelCanvas) => { p.disc(6, 5, 4, '#f5e6c8'); p.rect(4, 8, 5, 3, '#f5e6c8'); p.px(4, 5, '#1b1410'); p.px(7, 5, '#1b1410'); });
  makeTex(scene, 'ic-sound', 16, 16, (p: PixelCanvas) => { p.rect(2, 6, 3, 4, '#f5e6c8'); p.rect(5, 4, 2, 8, '#f5e6c8'); p.rect(7, 2, 1, 12, '#f5e6c8'); p.line(10, 5, 11, 7, '#ffb347'); p.line(11, 8, 10, 10, '#ffb347'); p.line(12, 3, 14, 7, '#ffb347'); p.line(14, 8, 12, 12, '#ffb347'); });
  makeTex(scene, 'ic-mute', 16, 16, (p: PixelCanvas) => { p.rect(2, 6, 3, 4, '#f5e6c8'); p.rect(5, 4, 2, 8, '#f5e6c8'); p.rect(7, 2, 1, 12, '#f5e6c8'); p.line(10, 5, 14, 10, '#ff6b6b'); p.line(14, 5, 10, 10, '#ff6b6b'); });
  makeTex(scene, 'ic-pause', 16, 16, (p: PixelCanvas) => { p.rect(3, 2, 4, 12, '#f5e6c8'); p.rect(9, 2, 4, 12, '#f5e6c8'); });
  makeTex(scene, 'ic-shop', 16, 16, (p: PixelCanvas) => { p.rect(3, 6, 10, 9, '#c17a4a'); p.rect(3, 6, 10, 2, '#e09a66'); p.rect(5, 2, 1, 5, '#f5e6c8'); p.rect(10, 2, 1, 5, '#f5e6c8'); p.rect(5, 2, 6, 1, '#f5e6c8'); p.px(8, 10, '#ffb347'); });
  // itens da loja
  makeTex(scene, 'item-amber', 32, 32, (p: PixelCanvas) => {
    stoneIcon(p, '#3a2a1a');
    p.line(8, 24, 24, 8, '#8fa0a8'); p.line(9, 24, 25, 8, '#e8f0f2'); p.line(7, 23, 23, 7, '#cfd8dc');
    p.rect(6, 22, 6, 2, '#c89650'); p.rect(8, 20, 2, 6, '#c89650'); p.rect(4, 26, 3, 3, '#6b4428');
    p.disc(9, 23, 1, '#ffb347'); p.px(24, 8, '#ffffff');
  });
  makeTex(scene, 'item-bark', 32, 32, (p: PixelCanvas) => {
    stoneIcon(p, '#243a20');
    p.rect(8, 7, 16, 18, '#6b4428'); p.rect(6, 7, 4, 7, '#8b5a2b'); p.rect(22, 7, 4, 7, '#8b5a2b');
    for (let y = 9; y < 25; y += 3) p.rect(9, y, 14, 1, '#4a2e1a');
    p.rect(15, 7, 2, 18, '#b07a42'); p.rect(12, 4, 3, 3, '#6fa66a'); p.rect(17, 3, 3, 3, '#9bd07a');
  });
  makeTex(scene, 'item-tide', 32, 32, (p: PixelCanvas) => {
    stoneIcon(p, '#10283a');
    p.ellipse(16, 17, 10, 11, '#1a4a5c'); p.ellipse(15, 16, 8, 9, '#2e7a94'); p.ellipse(13, 13, 3, 3, '#8fe0f0');
    for (let x = 9; x < 24; x++) p.px(x, 18 + Math.round(Math.sin(x * 0.8) * 1.5), '#bff5ff');
    p.px(12, 12, '#ffffff');
  });
  // vinheta
  if (!scene.textures.exists('vignette')) {
    const c = document.createElement('canvas');
    c.width = 640; c.height = 360;
    const ctx = c.getContext('2d');
    if (ctx) {
      const g = ctx.createRadialGradient(320, 180, 120, 320, 180, 400);
      g.addColorStop(0, 'rgba(8,20,16,0)');
      g.addColorStop(1, 'rgba(8,20,16,0.6)');
      ctx.fillStyle = g;
      ctx.fillRect(0, 0, 640, 360);
      scene.textures.addCanvas('vignette', c);
    }
  }
}
