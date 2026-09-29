import Phaser from 'phaser';
import { FONT } from '../config/balance';
import { Sound } from '../audio/SoundManager';
import { Unit } from '../entities/Unit';
import type { Nexus } from '../entities/Nexus';
import type { HeroBase } from '../entities/Hero';
import type { GameScene } from '../scenes/GameScene';

export interface HomingOpts { x: number; y: number; target: Unit; tex: string; speed: number; damage: number; source: Unit | null; spin?: number; glow?: number }
export interface LinearOpts { x: number; y: number; angle: number; speed: number; range: number; tex: string; damage: number; source: Unit | null; width: number; trail?: number }
export interface AreaOpts { x: number; y: number; radius: number; delay: number; damage: number; source: Unit | null; kind: 'fire' | 'shadow'; slow?: number; slowDuration?: number }

interface Proj {
  img: Phaser.GameObjects.Image; glow: Phaser.GameObjects.Image | null; trail: Phaser.GameObjects.Particles.ParticleEmitter | null;
  x: number; y: number; vx: number; vy: number; speed: number; target: Unit | null; dist: number; range: number;
  damage: number; source: Unit | null; team: number; width: number; spin: number; dead: boolean;
}
interface Area { o: AreaOpts; team: number; start: number; gfx: Phaser.GameObjects.Graphics; done: boolean }

const hitH = (u: Unit): number => (u.kind === 'hero' ? 24 : u.kind === 'minion' ? 11 : 42);

export class CombatSystem {
  scene: GameScene;
  projs: Proj[] = [];
  areas: Area[] = [];
  emitters: Record<string, Phaser.GameObjects.Particles.ParticleEmitter> = {};
  lastInvul = 0;

  constructor(scene: GameScene) {
    this.scene = scene;
    for (const tex of ['px-spark', 'px-ember', 'px-leaf', 'px-smoke', 'px-bark', 'px-dot']) {
      this.emitters[tex] = scene.add.particles(0, 0, tex, {
        lifespan: 600, speed: { min: 20, max: 100 }, alpha: { start: 1, end: 0 }, emitting: false,
        rotate: tex === 'px-leaf' || tex === 'px-bark' ? { min: 0, max: 360 } : 0,
        scale: tex === 'px-smoke' ? { start: 1, end: 2.5 } : 1, gravityY: tex === 'px-leaf' || tex === 'px-bark' ? 120 : 0,
      }).setDepth(8000);
    }
  }

  burst(x: number, y: number, tex: string, tint: number, count: number, speed: number, life = 600): void {
    const e = this.emitters[tex];
    if (!e || !this.scene.isNearCamera(x, y, 200)) return;
    e.setParticleTint(tint);
    e.setParticleSpeed(speed * 0.3, speed);
    e.setParticleLifespan(life);
    e.explode(count, x, y);
  }

  float(x: number, y: number, text: string, color: string, size = 13): void {
    if (!this.scene.isNearCamera(x, y)) return;
    const t = this.scene.add.text(x + (Math.random() - 0.5) * 16, y, text, {
      fontFamily: FONT, fontSize: `${size}px`, color, stroke: '#1b1410', strokeThickness: 4, fontStyle: 'bold',
    }).setOrigin(0.5).setDepth(10000);
    t.setScale(0.6);
    this.scene.tweens.add({ targets: t, scale: 1, duration: 120, ease: 'Back.Out' });
    this.scene.tweens.add({ targets: t, y: y - 34, alpha: 0, delay: 250, duration: 750, ease: 'Cubic.Out', onComplete: () => t.destroy() });
  }

  damage(target: Unit, amount: number, source: Unit | null): number {
    if (!target?.alive || amount <= 0 || this.scene.over) return 0;
    const now = this.scene.now;
    if (target.kind === 'nexus' && (target as Nexus).invulnerable()) {
      if (source?.isPlayer && now - this.lastInvul > 1200) {
        this.lastInvul = now;
        this.float(target.x, target.y - 90, 'Invulnerável — destrua a torre!', '#f5e6c8', 12);
        Sound.play('deny');
      }
      return 0;
    }
    let dmg = Math.round(amount);
    let absorbed = 0;
    if (target.shield > 0) {
      absorbed = Math.min(target.shield, dmg);
      target.shield -= absorbed;
      dmg -= absorbed;
    }
    target.hp -= dmg;
    target.barDirty = true;
    target.onDamaged(dmg + absorbed, source);
    if (source?.kind === 'hero' && target.kind === 'hero') source.lastHeroAttackAt = now;
    if (source?.kind === 'hero') (source as HeroBase).stats.damage += dmg + absorbed;
    const heroInvolved = source?.kind === 'hero' || target.kind === 'hero';
    if (heroInvolved) {
      const col = target.isPlayer ? '#ff5a4a' : target.kind === 'hero' ? '#ffd27a' : '#ffb347';
      this.float(target.x, target.y - hitH(target) - 22, absorbed > 0 && dmg === 0 ? `(${absorbed})` : String(dmg), col, target.kind === 'hero' ? 15 : 12);
    }
    this.burst(target.x, target.y - hitH(target), 'px-spark', target.isPlayer ? 0xff6b6b : 0xffd27a, heroInvolved ? 7 : 3, 110, 350);
    if (target.isPlayer && dmg >= 90) this.scene.shake(120, 0.004);
    if (target.hp <= 0) this.scene.onKill(target, source);
    return dmg;
  }

  homing(o: HomingOpts): void {
    const img = this.scene.add.image(o.x, o.y, o.tex).setDepth(8500);
    const glow = o.glow !== undefined ? this.scene.add.image(o.x, o.y, 'glow').setBlendMode(Phaser.BlendModes.ADD).setTint(o.glow).setScale(0.45).setDepth(8499) : null;
    this.projs.push({ img, glow, trail: null, x: o.x, y: o.y, vx: 0, vy: 0, speed: o.speed, target: o.target, dist: 0, range: 0, damage: o.damage, source: o.source, team: o.source?.team ?? 0, width: 0, spin: o.spin ?? 0, dead: false });
  }

  linear(o: LinearOpts): void {
    const img = this.scene.add.image(o.x, o.y, o.tex).setDepth(8500).setRotation(o.angle);
    const glow = o.trail !== undefined ? this.scene.add.image(o.x, o.y, 'glow').setBlendMode(Phaser.BlendModes.ADD).setTint(o.trail).setScale(0.8).setDepth(8499) : null;
    let trail: Phaser.GameObjects.Particles.ParticleEmitter | null = null;
    if (o.trail !== undefined) {
      trail = this.scene.add.particles(0, 0, 'px-ember', {
        lifespan: 380, speed: { min: 5, max: 30 }, alpha: { start: 1, end: 0 }, frequency: 12, tint: o.trail === 0xffb347 ? 0xffffff : o.trail,
      }).setDepth(8498);
      trail.startFollow(img);
    }
    this.projs.push({ img, glow, trail, x: o.x, y: o.y, vx: Math.cos(o.angle) * o.speed, vy: Math.sin(o.angle) * o.speed, speed: o.speed, target: null, dist: 0, range: o.range, damage: o.damage, source: o.source, team: o.source?.team ?? 0, width: o.width, spin: 0, dead: false });
  }

  area(o: AreaOpts): void {
    const gfx = this.scene.add.graphics().setDepth(-450);
    this.areas.push({ o, team: o.source?.team ?? 0, start: this.scene.now, gfx, done: false });
  }

  private killProj(p: Proj, burst = true): void {
    p.dead = true;
    if (burst) this.burst(p.x, p.y, 'px-spark', p.glow ? (p.glow.tintTopLeft ?? 0xffffff) : 0xffffff, 5, 80, 300);
    p.img.destroy();
    p.glow?.destroy();
    if (p.trail) {
      const tr = p.trail;
      tr.stopFollow();
      tr.stop();
      this.scene.time.delayedCall(450, () => tr.destroy());
    }
  }

  update(dt: number): void {
    const now = this.scene.now;
    for (const p of this.projs) {
      if (p.dead) continue;
      if (p.target) {
        const t = p.target;
        if (!t.alive) { this.killProj(p); continue; }
        const dx = t.x - p.x;
        const dy = t.y - hitH(t) - p.y;
        const d = Math.hypot(dx, dy);
        const step = p.speed * dt;
        if (d <= step + 4) {
          this.damage(t, p.damage, p.source);
          this.killProj(p);
          continue;
        }
        p.x += (dx / d) * step;
        p.y += (dy / d) * step;
        if (p.spin) p.img.rotation += p.spin * dt;
        else p.img.setRotation(Math.atan2(dy, dx));
      } else {
        p.x += p.vx * dt;
        p.y += p.vy * dt;
        p.dist += p.speed * dt;
        let hit: Unit | null = null;
        for (const u of this.scene.units) {
          if (!u.alive || u.team === p.team || (u.kind !== 'hero' && u.kind !== 'minion')) continue;
          if (Math.hypot(u.x - p.x, u.y - hitH(u) - p.y) < p.width / 2 + u.radius + 4) { hit = u; break; }
        }
        if (hit) {
          this.damage(hit, p.damage, p.source);
          this.burst(p.x, p.y, 'px-ember', 0xffffff, 16, 150, 500);
          if (p.source?.isPlayer) this.scene.shake(90, 0.003);
          Sound.play('hit');
          this.killProj(p);
          continue;
        }
        if (p.dist >= p.range) { this.killProj(p); continue; }
      }
      p.img.setPosition(p.x, p.y);
      p.glow?.setPosition(p.x, p.y);
    }
    this.projs = this.projs.filter((p: Proj) => !p.dead);
    for (const a of this.areas) this.updateArea(a, now);
    this.areas = this.areas.filter((a: Area) => !a.done);
  }

  private updateArea(a: Area, now: number): void {
    const o = a.o;
    const t = Math.min(1, (now - a.start) / o.delay);
    const col = o.kind === 'fire' ? 0xff4a2a : 0x8b4dff;
    const g = a.gfx;
    g.clear();
    g.fillStyle(col, 0.1 + t * 0.22).fillCircle(o.x, o.y, o.radius);
    g.lineStyle(3, col, 0.7 + Math.sin(now * 0.03) * 0.3).strokeCircle(o.x, o.y, o.radius);
    g.lineStyle(2, o.kind === 'fire' ? 0xffb347 : 0xd6b3ff, 0.9).strokeCircle(o.x, o.y, Math.max(2, o.radius * t));
    if (t < 1) return;
    a.done = true;
    g.destroy();
    for (const u of this.scene.units) {
      if (!u.alive || u.team === a.team || (u.kind !== 'hero' && u.kind !== 'minion')) continue;
      if (Math.hypot(u.x - o.x, u.y - o.y) <= o.radius + u.radius) {
        this.damage(u, o.damage, o.source);
        if (o.slow && u.alive) { u.slowFactor = o.slow; u.slowUntil = now + (o.slowDuration ?? 1500); }
      }
    }
    const flash = this.scene.add.image(o.x, o.y, 'glow').setBlendMode(Phaser.BlendModes.ADD).setTint(o.kind === 'fire' ? 0xff8a3a : 0x9b59ff).setScale(o.radius / 20).setDepth(8600);
    this.scene.tweens.add({ targets: flash, alpha: 0, scale: o.radius / 14, duration: 500, onComplete: () => flash.destroy() });
    if (o.kind === 'fire') {
      const n = 16;
      for (let i = 0; i < n; i++) {
        const ang = (i / n) * Math.PI * 2 + Math.random() * 0.4;
        const r = Math.sqrt(Math.random()) * o.radius * 0.9;
        const fx = o.x + Math.cos(ang) * r;
        const fy = o.y + Math.sin(ang) * r;
        const fl = this.scene.add.image(fx, fy, 'fire-flower').setOrigin(0.5, 1).setScale(0).setDepth(fy);
        this.scene.tweens.add({ targets: fl, scale: 1.4, duration: 180, delay: i * 18, ease: 'Back.Out' });
        this.scene.tweens.add({ targets: fl, alpha: 0, y: fy - 10, delay: 1300 + i * 20, duration: 600, onComplete: () => fl.destroy() });
      }
      this.burst(o.x, o.y - 10, 'px-ember', 0xffffff, 70, 260, 900);
      this.burst(o.x, o.y - 10, 'px-smoke', 0x5a4030, 20, 90, 1200);
      this.scene.shake(260, 0.01);
      Sound.play('rboom');
    } else {
      this.burst(o.x, o.y - 10, 'px-spark', 0x9b59ff, 45, 180, 700);
      Sound.play('hit');
    }
  }
}
