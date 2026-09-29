import Phaser from 'phaser';
import { PixelCanvas } from './PixelCanvas';

const INK = '#1b1410';

export interface FrameSpec { draw: (p: PixelCanvas) => void; glow?: (p: PixelCanvas) => void; lie?: boolean; alpha?: number }
export interface AnimSpec { name: string; frames: FrameSpec[]; rate: number; repeat: number }

/** Monta uma spritesheet a partir de funções que desenham pixel a pixel */
export function buildSheet(scene: Phaser.Scene, key: string, fw: number, fh: number, feetY: number, bodyH: number, anims: AnimSpec[]): void {
  if (scene.textures.exists(key)) return;
  const total = anims.reduce((s: number, a: AnimSpec) => s + a.frames.length, 0);
  const sheet = new PixelCanvas(fw * total, fh);
  const tmp = new PixelCanvas(fw, fh);
  let i = 0;
  const ranges: { name: string; start: number; count: number; rate: number; repeat: number }[] = [];
  for (const a of anims) {
    ranges.push({ name: a.name, start: i, count: a.frames.length, rate: a.rate, repeat: a.repeat });
    for (const f of a.frames) {
      tmp.ctx.clearRect(0, 0, fw, fh);
      tmp.at(0, 0);
      f.draw(tmp);
      tmp.outline(0, 0, fw, fh, INK);
      f.glow?.(tmp);
      tmp.alpha(1);
      if (f.lie) {
        sheet.drawRotated(tmp.canvas, 0, 0, fw, fh, i * fw + fw / 2 + bodyH / 2, feetY - 5, fw / 2, feetY, -Math.PI / 2, f.alpha ?? 1);
      } else {
        sheet.ctx.globalAlpha = f.alpha ?? 1;
        sheet.ctx.drawImage(tmp.canvas, i * fw, 0);
        sheet.ctx.globalAlpha = 1;
      }
      i++;
    }
  }
  const tex = scene.textures.addCanvas(key, sheet.canvas);
  if (!tex) return;
  for (let k = 0; k < total; k++) tex.add(k, 0, k * fw, 0, fw, fh);
  for (const r of ranges) {
    const frames: Phaser.Types.Animations.AnimationFrame[] = [];
    for (let k = 0; k < r.count; k++) frames.push({ key, frame: r.start + k });
    scene.anims.create({ key: `${key}-${r.name}`, frames, frameRate: r.rate, repeat: r.repeat });
  }
}

interface Pose { bob: number; step: number; atk: number; hurt: boolean; kneel: boolean; wave: number }
const P0: Pose = { bob: 0, step: 0, atk: 0, hurt: false, kneel: false, wave: 0 };
const pose = (o: Partial<Pose>): Pose => ({ ...P0, ...(o ?? {}) });

function crystalTip(p: PixelCanvas, x0: number, y0: number, x1: number, y1: number, cols: [string, string, string], len = 7): { tx: number; ty: number } {
  const L = Math.hypot(x1 - x0, y1 - y0) || 1;
  const ux = (x1 - x0) / L;
  const uy = (y1 - y0) / L;
  const widths = [1, 2, 2, 2, 1, 1, 0, 0].slice(0, len);
  widths.forEach((w: number, k: number) => {
    const cxp = x1 + ux * k;
    const cyp = y1 + uy * k;
    for (let s = -w; s <= w; s++) {
      const col = s === 0 ? cols[0] : Math.abs(s) === w ? cols[2] : cols[1];
      p.px(cxp - uy * s, cyp + ux * s, col);
    }
  });
  return { tx: x1 + ux * 3, ty: y1 + uy * 3 };
}

/* ------------------------------ NILO ------------------------------ */
const N = {
  bootD: '#24160d', boot: '#4a2e1a', bootL: '#6b4428', leg: '#4a7c59', legD: '#2d5a3d', legL: '#6aa377',
  arm: '#9c6b30', armD: '#6e4a22', armL: '#c89650', gold: '#e8c060', cape: '#c17a4a', capeD: '#8a4f2c', capeL: '#e09a66',
  capeDD: '#633519', hood: '#b3693d', hoodD: '#7d4526', mask: '#e6c28c', maskD: '#b88d56', carve: '#6b4424',
  eye: '#ffb347', eyeL: '#fff0b0', sprout: '#7fcf62', sproutD: '#3f8a3a', shaft: '#6b4424', shaftL: '#a8743e', belt: '#3b2718',
};

function nilo(p: PixelCanvas, o: Pose, glowPass: boolean): void {
  const cx = 28;
  const by = 53 + o.bob + (o.kneel ? 5 : 0);
  // posição da mão e da lança
  let hand = { x: cx + 6, y: by - 15 };
  let s0 = { x: hand.x - 1, y: hand.y + 9 };
  let s1 = { x: hand.x + 2, y: hand.y - 25 };
  if (o.atk === 1) { hand = { x: cx + 3, y: by - 16 }; s0 = { x: hand.x - 13, y: hand.y + 2 }; s1 = { x: hand.x + 10, y: hand.y - 2 }; }
  if (o.atk === 2) { hand = { x: cx + 9, y: by - 16 }; s0 = { x: hand.x - 7, y: hand.y }; s1 = { x: hand.x + 16, y: hand.y - 1 }; }
  if (o.atk === 3) { hand = { x: cx + 7, y: by - 16 }; s0 = { x: hand.x - 7, y: hand.y + 6 }; s1 = { x: hand.x + 11, y: hand.y - 12 }; }
  if (o.hurt) { hand = { x: cx + 3, y: by - 14 }; s0 = { x: hand.x + 3, y: hand.y + 8 }; s1 = { x: hand.x - 6, y: hand.y - 22 }; }
  const L = Math.hypot(s1.x - s0.x, s1.y - s0.y) || 1;
  const tip = { x: s1.x + ((s1.x - s0.x) / L) * 3, y: s1.y + ((s1.y - s0.y) / L) * 3 };
  if (glowPass) {
    p.alpha(o.atk === 2 ? 0.5 : 0.3);
    p.disc(tip.x, tip.y, o.atk === 2 ? 4 : 3, '#ffd27a');
    p.alpha(1);
    p.px(tip.x, tip.y, '#fffbe0');
    p.alpha(0.55);
    p.px(cx + 4, by - 30, N.eyeL);
    p.px(cx + 7, by - 30, N.eyeL);
    p.alpha(1);
    return;
  }
  const lean = o.hurt ? -1 : o.atk === 2 ? 1 : 0;
  // capa (atrás)
  const cTop = by - 24;
  const cBot = by - (o.kneel ? 5 : 6);
  for (let y = cTop; y <= cBot; y++) {
    const t = (y - cTop) / (cBot - cTop);
    const left = Math.round(cx - 6 - t * 5 - Math.sin(o.wave + t * 3) * 1.6 * t + lean);
    const right = cx - 1 + lean;
    p.rect(left, y, right - left, 1, N.cape);
    p.px(left, y, N.capeD);
    p.px(left + 1, y, t > 0.55 ? N.capeD : N.cape);
    if (t < 0.6) p.px(left + 3, y, N.capeL);
    if (y === cBot) {
      for (let x = left; x < right; x++) p.px(x, y + ((x + Math.round(o.wave)) % 3 === 0 ? 1 : 0), N.capeDD);
    }
  }
  // pernas
  const st = Math.round(o.step * 2);
  const lift = (v: number): number => (v > 0.4 ? 1 : 0);
  const leg = (x: number, l: number, back: boolean): void => {
    p.rect(x, by - 10, 3, 7 - l, back ? N.legD : N.leg);
    if (!back) p.rect(x, by - 10, 1, 6 - l, N.legL);
    p.rect(x, by - 3 - l, 4, 3, back ? N.bootD : N.boot);
    p.rect(x, by - 1 - l, 4, 1, N.bootD);
    if (!back) p.px(x + 3, by - 3 - l, N.bootL);
  };
  if (o.kneel) {
    p.rect(cx - 7, by - 4, 7, 3, N.legD);
    p.rect(cx - 8, by - 2, 3, 2, N.bootD);
    p.rect(cx + 1, by - 8, 3, 6, N.leg);
    p.rect(cx + 1, by - 3, 5, 3, N.boot);
  } else {
    leg(cx - 4 - st, lift(-o.step), true);
    leg(cx + 1 + st, lift(o.step), false);
  }
  // tronco / armadura
  const tx = cx - 5 + lean;
  p.rect(tx, by - 22, 10, 12, N.arm);
  p.rect(tx, by - 22, 2, 12, N.armD);
  p.rect(tx + 8, by - 21, 2, 9, N.armL);
  p.rect(tx + 1, by - 18, 8, 1, N.armD);
  p.rect(tx + 1, by - 15, 8, 1, N.armD);
  p.px(tx + 5, by - 20, N.gold);
  p.px(tx + 6, by - 19, N.gold);
  p.px(tx + 5, by - 19, N.eye);
  p.px(tx + 4, by - 17, N.armL);
  p.rect(tx, by - 12, 10, 2, N.belt);
  p.rect(tx + 5, by - 12, 2, 2, N.eye);
  p.px(tx + 5, by - 12, N.eyeL);
  p.rect(tx, by - 10, 3, 2, N.armD);
  p.rect(tx + 6, by - 10, 4, 2, N.arm);
  p.px(tx + 9, by - 10, N.armL);
  // cabeça: capuz + máscara de madeira
  const hx = cx + 1 + lean;
  const hy = by - 29;
  p.rect(hx - 9, hy + 1, 4, 5, N.hoodD);
  p.disc(hx, hy, 7, N.hoodD);
  p.disc(hx - 1, hy - 1, 6, N.hood);
  p.rect(hx - 4, hy - 6, 4, 1, N.capeL);
  p.px(hx - 5, hy - 5, N.capeL);
  p.ellipse(hx + 3, hy + 1, 4, 5, N.maskD);
  p.ellipse(hx + 3, hy, 3, 4, N.mask);
  p.px(hx + 2, hy - 3, '#f6dcae');
  p.line(hx + 3, hy - 4, hx + 3, hy - 2, N.carve);
  p.rect(hx + 1, hy - 1, 2, 1, INK);
  p.rect(hx + 5, hy - 1, 2, 1, INK);
  p.px(hx + 2, hy - 1, N.eye);
  p.px(hx + 6, hy - 1, N.eye);
  p.px(hx + 1, hy + 2, N.carve);
  p.px(hx + 2, hy + 3, N.carve);
  p.px(hx + 6, hy + 2, N.carve);
  p.rect(hx + 3, hy + 4, 2, 1, N.carve);
  // broto
  p.line(hx - 1, hy - 7, hx - 1, hy - 9, N.sproutD);
  p.rect(hx - 4, hy - 10, 3, 1, N.sprout);
  p.px(hx - 4, hy - 11, N.sprout);
  p.rect(hx, hy - 11, 3, 1, N.sprout);
  p.px(hx + 2, hy - 12, N.sprout);
  // lança
  p.line(s0.x, s0.y, s1.x, s1.y, N.shaft);
  p.line(s0.x + 1, s0.y, s1.x + 1, s1.y, N.shaftL);
  const bx = s1.x - ((s1.x - s0.x) / L) * 2;
  const byy = s1.y - ((s1.y - s0.y) / L) * 2;
  p.rect(bx - 1, byy - 1, 3, 2, N.belt);
  crystalTip(p, s0.x, s0.y, s1.x, s1.y, ['#fff0b0', '#ffb347', '#d9822b']);
  // braço e luva
  p.line(cx + 2 + lean, by - 20, hand.x, hand.y, N.armD);
  p.line(cx + 3 + lean, by - 20, hand.x, hand.y - 1, N.arm);
  p.ellipse(cx + 2 + lean, by - 21, 3, 2, N.armL);
  p.px(cx + 2 + lean, by - 21, N.gold);
  p.rect(hand.x - 1, hand.y - 1, 3, 3, N.boot);
  p.px(hand.x, hand.y - 1, N.bootL);
}

/* ------------------------------ VEX ------------------------------ */
const V = {
  robe: '#1a4a5c', robeD: '#0f2f3b', robeL: '#2e6e84', robeLL: '#4f94a8', trim: '#b9a5e8', trimD: '#7a62c0',
  mask: '#bfe8ff', maskL: '#effbff', maskD: '#7fb4e0', facet: '#5f7fd0', eye: '#d6b3ff', hand: '#9fb3c4',
  wand: '#3b2a4a', wandL: '#6a4f7e', orb: '#9b59ff', orbL: '#e0c8ff', orbD: '#5b2aa8',
};

function vex(p: PixelCanvas, o: Pose, glowPass: boolean): void {
  const cx = 28;
  const by = 53 + o.bob + (o.kneel ? 5 : 0);
  let hand = { x: cx + 6, y: by - 16 };
  let orb = { x: hand.x + 1, y: hand.y - 13 };
  if (o.atk === 1) { hand = { x: cx + 3, y: by - 24 }; orb = { x: hand.x - 3, y: hand.y - 11 }; }
  if (o.atk === 2) { hand = { x: cx + 9, y: by - 18 }; orb = { x: hand.x + 12, y: hand.y - 3 }; }
  if (o.atk === 3) { hand = { x: cx + 7, y: by - 17 }; orb = { x: hand.x + 7, y: hand.y - 10 }; }
  if (o.hurt) { hand = { x: cx + 3, y: by - 14 }; orb = { x: hand.x - 5, y: hand.y - 11 }; }
  if (glowPass) {
    const big = o.atk === 1 || o.atk === 2;
    p.alpha(big ? 0.45 : 0.28);
    p.disc(orb.x, orb.y, big ? 5 : 4, '#b98cff');
    p.alpha(1);
    p.px(orb.x - 1, orb.y - 1, '#ffffff');
    p.alpha(0.6);
    p.px(cx + 4, by - 30, V.eye);
    p.px(cx + 7, by - 30, V.eye);
    p.alpha(1);
    return;
  }
  const lean = o.hurt ? -1 : o.atk === 2 ? 1 : 0;
  // manto
  const top = by - 24;
  const bot = by - 2;
  const st = Math.round(o.step * 2);
  if (!o.kneel) {
    p.rect(cx - 4 - st, by - 3, 3, 3, INK);
    p.rect(cx + 1 + st, by - 3, 4, 3, '#2a2233');
  }
  for (let y = top; y <= bot; y++) {
    const t = (y - top) / (bot - top);
    const sway = Math.round(Math.sin(o.wave + t * 2.5) * t * 1.5);
    const left = Math.round(cx - 5 - t * 4 + sway + lean * (1 - t));
    const right = Math.round(cx + 5 + t * 3 + sway + lean * (1 - t));
    p.rect(left, y, right - left, 1, V.robe);
    p.rect(left, y, 2, 1, V.robeD);
    p.rect(right - 3, y, 2, 1, V.robeL);
    if (t > 0.15 && t < 0.8) p.px(left + 4, y, V.robeL);
    if (y === bot || y === bot - 1) p.rect(left, y, right - left, 1, y === bot ? V.trimD : V.trim);
  }
  // faixa e runas
  p.rect(cx - 5 + lean, by - 14, 11, 2, V.trimD);
  p.px(cx + lean, by - 14, V.orbL);
  p.px(cx - 2, by - 8, V.trim);
  p.px(cx + 2, by - 6, V.trim);
  p.px(cx - 1, by - 5, V.trimD);
  // capuz pontudo
  const hx = cx + 1 + lean;
  const hy = by - 29;
  p.disc(hx, hy, 7, V.robeD);
  p.disc(hx - 1, hy - 1, 6, V.robe);
  for (let k = 0; k < 7; k++) p.rect(hx - 4 - k, hy - 6 - Math.floor(k * 0.9), 4 - Math.floor(k / 2), 1, k < 3 ? V.robe : V.robeD);
  p.rect(hx - 3, hy - 6, 4, 1, V.robeLL);
  // máscara de cristal facetada
  p.ellipse(hx + 3, hy + 1, 4, 5, V.maskD);
  p.ellipse(hx + 3, hy, 3, 4, V.mask);
  p.line(hx + 1, hy - 3, hx + 5, hy + 3, V.maskL);
  p.px(hx + 5, hy - 3, V.facet);
  p.px(hx + 2, hy + 3, V.facet);
  p.rect(hx + 1, hy - 1, 2, 1, '#2a1b4a');
  p.rect(hx + 5, hy - 1, 2, 1, '#2a1b4a');
  p.px(hx + 2, hy - 1, V.eye);
  p.px(hx + 6, hy - 1, V.eye);
  p.px(hx + 4, hy + 4, V.facet);
  // varinha
  p.line(hand.x - 1, hand.y + 6, orb.x, orb.y + 2, V.wand);
  p.line(hand.x, hand.y + 6, orb.x + 1, orb.y + 2, V.wandL);
  p.disc(orb.x, orb.y, 2, V.orb);
  p.px(orb.x + 1, orb.y + 1, V.orbD);
  p.px(orb.x - 1, orb.y - 1, V.orbL);
  p.px(orb.x - 2, orb.y + 2, V.trim);
  p.px(orb.x + 2, orb.y + 2, V.trim);
  // manga
  p.line(cx + 2 + lean, by - 21, hand.x, hand.y, V.robeD);
  p.line(cx + 3 + lean, by - 21, hand.x + 1, hand.y, V.robeL);
  p.rect(hand.x - 1, hand.y - 1, 3, 2, V.hand);
}

function heroAnims(fn: (p: PixelCanvas, o: Pose, g: boolean) => void, atkRate: number): AnimSpec[] {
  const f = (o: Pose, extra: Partial<FrameSpec> = {}): FrameSpec => ({ draw: (p: PixelCanvas) => fn(p, o, false), glow: (p: PixelCanvas) => fn(p, o, true), ...extra });
  const walk: FrameSpec[] = [];
  for (let i = 0; i < 6; i++) {
    const s = Math.sin((i / 6) * Math.PI * 2);
    walk.push(f(pose({ step: s, bob: Math.abs(s) > 0.5 ? -1 : 0, wave: i * 1.1 })));
  }
  return [
    { name: 'idle', rate: 5, repeat: -1, frames: [0, -1, -1, 0].map((b: number, i: number) => f(pose({ bob: b, wave: i * 0.8 }))) },
    { name: 'walk', rate: 11, repeat: -1, frames: walk },
    { name: 'attack', rate: atkRate, repeat: 0, frames: [1, 2, 2, 3].map((a: number, i: number) => f(pose({ atk: a, wave: i }))) },
    { name: 'hurt', rate: 8, repeat: 0, frames: [f(pose({ hurt: true, wave: 2 }))] },
    {
      name: 'death', rate: 6, repeat: 0, frames: [
        f(pose({ hurt: true })), f(pose({ hurt: true, kneel: true })),
        { draw: (p: PixelCanvas) => fn(p, pose({ hurt: true }), false), lie: true },
        { draw: (p: PixelCanvas) => fn(p, pose({ hurt: true }), false), lie: true, alpha: 0.5 },
      ],
    },
  ];
}

/* ------------------------------ TROPAS ------------------------------ */
export const TEAM_PAL = [
  { main: '#00c8b4', dark: '#00786e', light: '#8ff5ea' },
  { main: '#ff6b6b', dark: '#b03c3c', light: '#ffc0b8' },
];

function minion(p: PixelCanvas, team: number, ranged: boolean, o: Pose): void {
  const t = TEAM_PAL[team] ?? TEAM_PAL[0];
  const cx = 14;
  const by = 27 + o.bob + (o.kneel ? 3 : 0);
  const st = Math.round(o.step * 1.5);
  const face = '#ecd6a8';
  // pernas
  if (!o.kneel) {
    p.rect(cx - 3 - st, by - 4, 2, 4, '#3b2718');
    p.rect(cx + 1 + st, by - 4, 2, 4, '#5a3b22');
  }
  // escudo / aljava nas costas
  if (ranged) {
    p.rect(cx - 6, by - 14, 3, 7, '#7a5230');
    p.px(cx - 6, by - 15, '#e8e0c8');
    p.px(cx - 4, by - 16, '#e8e0c8');
  } else {
    p.ellipse(cx - 5, by - 9, 2, 3, '#7a5230');
    p.px(cx - 5, by - 9, t.main);
  }
  // corpo de folha
  p.ellipse(cx, by - 8, 5, 4, '#2f6a3c');
  p.ellipse(cx - 1, by - 9, 3, 2, '#5fa066');
  p.rect(cx - 4, by - 7, 8, 1, '#3b2718');
  p.px(cx + 1, by - 7, t.light);
  // cabeça
  const hx = cx + 1;
  const hy = by - 15;
  if (ranged) {
    p.disc(hx, hy, 5, t.dark);
    p.disc(hx - 1, hy - 1, 4, t.main);
    p.px(hx - 3, hy - 3, t.light);
    p.px(hx - 2, hy - 4, t.light);
    p.rect(hx - 4, hy - 7, 2, 2, '#5fa066');
    p.ellipse(hx + 2, hy + 1, 2, 3, face);
    p.px(hx + 2, hy, INK);
    p.px(hx + 4, hy, INK);
  } else {
    p.disc(hx, hy, 4, face);
    p.px(hx - 2, hy + 2, '#c9ad7c');
    p.px(hx + 1, hy, INK);
    p.px(hx + 3, hy, INK);
    p.px(hx + 2, hy + 2, '#e89a7a');
    p.ellipse(hx, hy - 3, 5, 3, t.dark);
    p.ellipse(hx, hy - 4, 5, 2, t.main);
    p.px(hx - 2, hy - 5, t.light);
    p.px(hx + 1, hy - 5, t.light);
    p.rect(hx - 5, hy - 2, 11, 1, t.dark);
    p.rect(hx, hy - 8, 1, 2, '#5a3b22');
  }
  // arma
  if (ranged) {
    const pull = o.atk === 1 || o.atk === 2 ? 3 : 0;
    const bx = cx + 7;
    for (let y = -6; y <= 6; y++) p.px(bx + Math.round(2 * Math.sqrt(1 - (y / 6.5) ** 2)) - 2, by - 9 + y, y % 4 === 0 ? '#b07a42' : '#8b5a2b');
    p.line(bx - 2, by - 15, bx - 2 - pull, by - 9, '#efe6cc');
    p.line(bx - 2 - pull, by - 9, bx - 2, by - 3, '#efe6cc');
    if (pull) { p.line(bx - 3 - pull, by - 9, bx + 3, by - 9, '#d8c7a0'); p.px(bx + 3, by - 9, t.light); }
    p.rect(bx - 3 - pull, by - 10, 2, 2, face);
  } else {
    let h = { x: cx + 5, y: by - 9 };
    let tipP = { x: h.x + 2, y: h.y - 8 };
    if (o.atk === 1) { h = { x: cx + 2, y: by - 13 }; tipP = { x: h.x - 4, y: h.y - 7 }; }
    if (o.atk === 2) { h = { x: cx + 6, y: by - 9 }; tipP = { x: h.x + 8, y: h.y - 1 }; }
    p.line(h.x, h.y, tipP.x, tipP.y, '#cfd8dc');
    p.line(h.x + 1, h.y, tipP.x + 1, tipP.y, '#8fa0a8');
    p.px(tipP.x, tipP.y, '#ffffff');
    p.rect(h.x - 1, h.y, 3, 1, '#e8c060');
    p.rect(h.x, h.y + 1, 1, 1, face);
  }
}

function minionAnims(team: number, ranged: boolean): AnimSpec[] {
  const f = (o: Pose, extra: Partial<FrameSpec> = {}): FrameSpec => ({ draw: (p: PixelCanvas) => minion(p, team, ranged, o), ...extra });
  return [
    { name: 'walk', rate: 8, repeat: -1, frames: [0, 1, 0, -1].map((s: number) => f(pose({ step: s, bob: s === 0 ? 0 : -1 }))) },
    { name: 'attack', rate: 9, repeat: 0, frames: [1, 2, 0].map((a: number) => f(pose({ atk: a }))) },
    { name: 'death', rate: 7, repeat: 0, frames: [f(pose({ kneel: true })), f(pose({}), { lie: true }), f(pose({}), { lie: true, alpha: 0.5 })] },
  ];
}

export function buildCharacters(scene: Phaser.Scene): void {
  buildSheet(scene, 'nilo', 56, 56, 53, 44, heroAnims(nilo, 16));
  buildSheet(scene, 'vex', 56, 56, 53, 44, heroAnims(vex, 10));
  for (const team of [0, 1]) {
    buildSheet(scene, `minion-melee-${team}`, 30, 30, 27, 22, minionAnims(team, false));
    buildSheet(scene, `minion-ranged-${team}`, 30, 30, 27, 22, minionAnims(team, true));
  }
}
