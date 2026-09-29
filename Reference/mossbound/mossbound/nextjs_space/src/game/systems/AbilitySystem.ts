import { ABILITY, HERO } from '../config/balance';
import { Sound } from '../audio/SoundManager';
import type { Hero } from '../entities/Hero';
import type { Nexus } from '../entities/Nexus';
import type { Unit } from '../entities/Unit';
import type { GameScene } from '../scenes/GameScene';

export type Slot = 'AA' | 'Q' | 'W' | 'E' | 'R';
export const SLOTS: Slot[] = ['AA', 'Q', 'W', 'E', 'R'];

/** Habilidades de Nilo */
export class AbilitySystem {
  scene: GameScene;
  hero: Hero;
  readyAt: Record<Slot, number> = { AA: 0, Q: 0, W: 0, E: 0, R: 0 };
  totals: Record<Slot, number> = { AA: HERO.attackCd, Q: ABILITY.Q.cd, W: ABILITY.W.cd, E: ABILITY.E.cd, R: ABILITY.R.cd };

  constructor(scene: GameScene, hero: Hero) {
    this.scene = scene;
    this.hero = hero;
  }

  remaining(s: Slot): number {
    return Math.max(0, (this.readyAt[s] ?? 0) - this.scene.now);
  }

  locked(s: Slot): boolean {
    return s === 'R' && this.hero.level < ABILITY.R.unlockLevel;
  }

  private baseCd(s: Slot): number {
    const c = this.hero.cdrMul;
    switch (s) {
      case 'AA': return HERO.attackCd;
      case 'Q': return ABILITY.Q.cd * c;
      case 'W': return ABILITY.W.cd * c;
      case 'E': return ABILITY.E.cd * c;
      case 'R': return ABILITY.R.cd * c;
      default: return 1000;
    }
  }

  cast(s: Slot, ax: number, ay: number): boolean {
    const h = this.hero;
    if (!h?.alive || this.scene.over) return false;
    if (this.locked(s)) { Sound.play('deny', 300); return false; }
    if (this.remaining(s) > 0) { if (s !== 'AA') Sound.play('deny', 300); return false; }
    let ok = true;
    if (s === 'AA') this.basic(ax, ay);
    else if (s === 'Q') this.q(ax, ay);
    else if (s === 'W') this.w();
    else if (s === 'E') ok = this.e(ax, ay);
    else if (s === 'R') this.r(ax, ay);
    if (!ok) return false;
    const cd = this.baseCd(s);
    this.totals[s] = cd;
    this.readyAt[s] = this.scene.now + cd;
    if (s !== 'AA') h.cancelRecall();
    return true;
  }

  private basic(ax: number, ay: number): void {
    const h = this.hero;
    const ang = Math.atan2(ay - (h.y - 16), ax - h.x);
    h.face(Math.cos(ang));
    h.animLockUntil = this.scene.now + 250;
    h.playAnim('attack', true);
    h.cancelRecall();
    let best: Unit | null = null;
    let bestScore = Infinity;
    for (const u of this.scene.units) {
      if (!u.alive || u.team === h.team) continue;
      const d = Math.hypot(u.x - h.x, u.y - h.y) - u.radius;
      if (d > HERO.attackRange) continue;
      const ua = Math.atan2(u.y - (u.kind === 'minion' ? 10 : 20) - (h.y - 16), u.x - h.x);
      let diff = Math.abs(ua - ang);
      if (diff > Math.PI) diff = Math.PI * 2 - diff;
      if (diff > HERO.attackArc && d > 28) continue;
      const inv = u.kind === 'nexus' && (u as Nexus).invulnerable() ? 400 : 0;
      const score = d + diff * 80 + inv;
      if (score < bestScore) { best = u; bestScore = score; }
    }
    // rastro do golpe
    for (let i = 1; i <= 5; i++) {
      const r = (HERO.attackRange * 0.75 * i) / 5;
      this.scene.combat.burst(h.x + Math.cos(ang) * r, h.y - 18 + Math.sin(ang) * r, 'px-dot', 0xffd27a, 1, 20, 220);
    }
    Sound.play('attack');
    if (best) {
      this.scene.combat.damage(best, h.damage, h);
      Sound.play('hit');
    }
  }

  private q(ax: number, ay: number): void {
    const h = this.hero;
    const sy = h.y - 22;
    const ang = Math.atan2(ay - sy, ax - h.x);
    h.face(Math.cos(ang));
    h.animLockUntil = this.scene.now + 250;
    h.playAnim('attack', true);
    this.scene.combat.linear({ x: h.x + Math.cos(ang) * 14, y: sy, angle: ang, speed: ABILITY.Q.speed, range: ABILITY.Q.range, tex: 'proj-q', damage: ABILITY.Q.damage, source: h, width: ABILITY.Q.width, trail: 0xffb347 });
    Sound.play('q');
  }

  private w(): void {
    const h = this.hero;
    h.shield = ABILITY.W.shield;
    h.shieldUntil = this.scene.now + ABILITY.W.duration;
    h.barDirty = true;
    this.scene.combat.burst(h.x, h.y - 20, 'px-bark', 0xffffff, 18, 120, 700);
    this.scene.combat.burst(h.x, h.y - 20, 'px-leaf', 0xffffff, 10, 90, 800);
    Sound.play('shield');
  }

  private e(ax: number, ay: number): boolean {
    const h = this.hero;
    const dx = ax - h.x;
    const dy = ay - h.y;
    if (Math.hypot(dx, dy) < 4) return false;
    this.scene.combat.burst(h.x, h.y - 14, 'px-ember', 0xffffff, 20, 120, 600);
    h.startDash(dx, dy, ABILITY.E.distance, ABILITY.E.duration);
    Sound.play('dash');
    return true;
  }

  private r(ax: number, ay: number): void {
    const h = this.hero;
    let dx = ax - h.x;
    let dy = ay - h.y;
    const d = Math.hypot(dx, dy);
    if (d > ABILITY.R.castRange) { dx = (dx / d) * ABILITY.R.castRange; dy = (dy / d) * ABILITY.R.castRange; }
    h.face(dx);
    h.animLockUntil = this.scene.now + 250;
    h.playAnim('attack', true);
    this.scene.combat.area({ x: h.x + dx, y: h.y + dy, radius: ABILITY.R.radius, delay: ABILITY.R.delay, damage: ABILITY.R.damage, source: h, kind: 'fire', slow: ABILITY.R.slow, slowDuration: ABILITY.R.slowDuration });
    Sound.play('rwarn');
  }
}
