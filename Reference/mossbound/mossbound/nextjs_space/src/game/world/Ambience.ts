import Phaser from 'phaser';
import { POS } from '../config/balance';
import type { Decor } from './MapLayout';

interface Flower { img: Phaser.GameObjects.Image; a: string; b: string; phase: number; state: number }
interface Glow { img: Phaser.GameObjects.Image; base: number; amp: number; speed: number; phase: number; flicker: boolean }

/** Decoração viva: flores balançando, cogumelos e cristais brilhando, vaga-lumes e folhas */
export class Ambience {
  flowers: Flower[] = [];
  glows: Glow[] = [];
  fireflies: Phaser.GameObjects.Particles.ParticleEmitter;
  leaves: Phaser.GameObjects.Particles.ParticleEmitter;

  constructor(scene: Phaser.Scene, decor: Decor[]) {
    const addGlow = (x: number, y: number, tint: number, scale: number, base: number, amp: number, speed: number, flicker = false, depth = y + 1): void => {
      const img = scene.add.image(x, y, 'glow').setBlendMode(Phaser.BlendModes.ADD).setTint(tint).setScale(scale).setAlpha(base).setDepth(depth);
      this.glows.push({ img, base, amp, speed, phase: Math.random() * 1000, flicker });
    };
    for (const d of decor ?? []) {
      const key = d?.key ?? '';
      if (!key || !scene.textures.exists(key)) continue;
      const img = scene.add.image(Math.round(d.x), Math.round(d.y), key).setFlipX(!!d.flip).setDepth(d.y);
      if (key.startsWith('tree')) img.setOrigin(0.5, 72 / 76);
      else img.setOrigin(0.5, 0.94);
      if (key.startsWith('flower')) {
        this.flowers.push({ img, a: key, b: `${key}b`, phase: Math.random() * 1200, state: 0 });
      } else if (key.startsWith('mush')) {
        addGlow(d.x, d.y - 6, key === 'mush-0' ? 0x3fe0d0 : 0xffb347, 0.55, 0.35, 0.3, 0.0022);
      } else if (key === 'crystal-deco') {
        addGlow(d.x, d.y - 14, 0x7fe9ff, 1.1, 0.35, 0.25, 0.0016);
      } else if (key === 'lantern') {
        addGlow(d.x, d.y - 26, 0xffb347, 0.9, 0.55, 0.2, 0.01, true);
      }
    }
    // luzes âmbar nas bases
    for (const b of [POS.blueBase, POS.redBase]) {
      addGlow(b.x, b.y, 0xffb347, 7.5, 0.16, 0.06, 0.0012, false, -500);
      addGlow(b.x, b.y, 0xffd27a, 3.5, 0.14, 0.05, 0.002, false, -499);
    }
    this.fireflies = scene.add.particles(0, 0, 'px-dot', {
      x: { min: 0, max: 1280 }, y: { min: 0, max: 720 }, lifespan: { min: 2600, max: 5000 },
      speedX: { min: -14, max: 14 }, speedY: { min: -16, max: 6 }, scale: { min: 1, max: 1.8 },
      alpha: { onEmit: () => 0, onUpdate: (_p: Phaser.GameObjects.Particles.Particle, _k: string, t: number) => Math.sin(t * Math.PI) * (0.6 + 0.4 * Math.sin(t * 40)) },
      tint: [0xffe07a, 0xc8ff8a, 0x9ff5ea], blendMode: 'ADD', frequency: 90,
    }).setDepth(8200);
    this.leaves = scene.add.particles(0, 0, 'px-leaf', {
      x: { min: -100, max: 1280 }, y: { min: -40, max: 300 }, lifespan: 6000,
      speedX: { min: 10, max: 40 }, speedY: { min: 14, max: 34 }, rotate: { min: 0, max: 360 },
      alpha: { start: 0.9, end: 0 }, scale: { min: 1, max: 1.6 }, frequency: 650, tint: [0xffffff, 0xd9e8a0, 0xffc88a],
    }).setDepth(8190);
  }

  update(t: number, cam: Phaser.Cameras.Scene2D.Camera): void {
    const v = cam.worldView;
    const x0 = v.x - 40, x1 = v.x + v.width + 40, y0 = v.y - 40, y1 = v.y + v.height + 80;
    for (const f of this.flowers) {
      const im = f.img;
      if (im.x < x0 || im.x > x1 || im.y < y0 || im.y > y1) continue;
      const s = Math.floor((t + f.phase) / 560) % 2;
      if (s !== f.state) { f.state = s; im.setTexture(s ? f.b : f.a); }
    }
    for (const g of this.glows) {
      const im = g.img;
      if (im.depth > -400 && (im.x < x0 - 200 || im.x > x1 + 200 || im.y < y0 - 200 || im.y > y1 + 200)) continue;
      let a = g.base + Math.sin((t + g.phase) * g.speed) * g.amp;
      if (g.flicker) a += (Math.random() - 0.5) * 0.12;
      im.setAlpha(Math.max(0, a));
    }
    this.fireflies.setPosition(v.x, v.y);
    this.leaves.setPosition(v.x, v.y);
  }
}
