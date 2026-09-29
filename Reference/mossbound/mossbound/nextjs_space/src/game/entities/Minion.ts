import Phaser from 'phaser';
import { MINION, POS, Team, BLUE } from '../config/balance';
import { Sound } from '../audio/SoundManager';
import { Unit } from './Unit';
import type { GameScene } from '../scenes/GameScene';
import type { Nexus } from './Nexus';

export type MinionType = 'melee' | 'ranged';

const PRIORITY: Record<string, number> = { minion: 0, tower: 1, hero: 2, nexus: 3 };

export class Minion extends Unit {
  type: MinionType;
  cfg: typeof MINION.melee | typeof MINION.ranged;
  sprite: Phaser.GameObjects.Sprite;
  shadow: Phaser.GameObjects.Image;
  target: Unit | null = null;
  atkReady = 0;
  retargetAt = 0;
  animLockUntil = 0;
  laneOffset: number;
  removed = false;
  facing: 1 | -1;
  tinted = false;

  constructor(scene: GameScene, team: Team, type: MinionType, x: number, y: number, laneOffset: number) {
    const cfg = type === 'melee' ? MINION.melee : MINION.ranged;
    super(scene, team, 'minion', x, y, cfg.radius, cfg.hp, 22, 30);
    this.type = type;
    this.cfg = cfg;
    this.laneOffset = laneOffset;
    this.facing = team === BLUE ? 1 : -1;
    this.shadow = scene.add.image(x, y, 'shadow').setDepth(-10).setScale(0.6, 0.7);
    this.sprite = scene.add.sprite(x, y, `minion-${type}-${team}`, 0).setOrigin(0.5, 27 / 30);
    this.sprite.play(`minion-${type}-${team}-walk`);
  }

  private pickTarget(): Unit | null {
    let best: Unit | null = null;
    let bestP = 99;
    let bestD = Infinity;
    for (const u of this.scene.units) {
      if (!u.alive || u.team === this.team) continue;
      if (u.kind === 'nexus' && (u as Nexus).invulnerable()) continue;
      const d = this.distTo(u) - u.radius;
      if (d > MINION.aggroRange) continue;
      const p = PRIORITY[u.kind] ?? 9;
      const sticky = u === this.target ? -40 : 0;
      if (p < bestP || (p === bestP && d + sticky < bestD)) {
        best = u;
        bestP = p;
        bestD = d + sticky;
      }
    }
    return best;
  }

  update(dt: number): void {
    if (!this.alive) return;
    const now = this.scene.now;
    if (now >= this.retargetAt || !this.target?.alive) {
      this.target = this.pickTarget();
      this.retargetAt = now + 250;
    }
    const t = this.target;
    let moving = false;
    if (t && t.alive) {
      const gap = this.distTo(t) - t.radius - this.radius;
      if (gap > this.cfg.range) {
        this.step(t.x, t.y + (t.kind === 'minion' ? 0 : this.laneOffset * 0.3), dt);
        moving = true;
      } else if (now >= this.atkReady) {
        this.attack(t);
      }
    } else {
      const tx = this.team === BLUE ? POS.redNexus.x : POS.blueNexus.x;
      this.step(tx, POS.laneY + this.laneOffset, dt);
      moving = true;
    }
    if (now >= this.animLockUntil && moving) this.sprite.play(`minion-${this.type}-${this.team}-walk`, true);
    this.sync();
  }

  private step(tx: number, ty: number, dt: number): void {
    const dx = tx - this.x;
    const dy = ty - this.y;
    const d = Math.hypot(dx, dy) || 1;
    const sp = this.cfg.speed * this.speedMul() * dt;
    this.scene.moveUnit(this, (dx / d) * sp, (dy / d) * sp);
    if (Math.abs(dx) > 2) this.facing = dx < 0 ? -1 : 1;
  }

  private attack(t: Unit): void {
    const now = this.scene.now;
    this.atkReady = now + this.cfg.cd;
    this.animLockUntil = now + 330;
    this.facing = t.x < this.x ? -1 : 1;
    this.sprite.play(`minion-${this.type}-${this.team}-attack`, true);
    const mult = t.kind === 'tower' || t.kind === 'nexus' ? MINION.structureMult : 1;
    const dmg = this.cfg.damage * mult;
    const near = this.scene.isNearCamera(this.x, this.y);
    if (this.type === 'melee') {
      this.scene.combat?.damage(t, dmg, this);
      if (near) Sound.play('sword', 90);
    } else {
      this.scene.combat?.homing({ x: this.x + this.facing * 8, y: this.y - 12, target: t, tex: 'proj-arrow', speed: MINION.ranged.projSpeed, damage: dmg, source: this });
      if (near) Sound.play('arrow', 90);
    }
  }

  sync(): void {
    const rx = Math.round(this.x);
    const ry = Math.round(this.y);
    this.sprite.setPosition(rx, ry).setDepth(ry).setFlipX(this.facing < 0);
    this.shadow.setPosition(rx, ry);
    const flash = this.flashUntil > this.scene.now;
    if (flash !== this.tinted) {
      this.tinted = flash;
      if (flash) this.sprite.setTintFill(0xffffff);
      else this.sprite.clearTint();
    }
    this.updateBar();
  }

  die(_killer: Unit | null): void {
    this.alive = false;
    this.bar.clear();
    this.sprite.clearTint();
    this.sprite.play(`minion-${this.type}-${this.team}-death`, true);
    this.scene.combat?.burst(this.x, this.y - 10, 'px-leaf', 0xffffff, 8, 70);
    if (this.scene.isNearCamera(this.x, this.y)) Sound.play('mdeath', 60);
    this.scene.tweens.add({
      targets: [this.sprite, this.shadow], alpha: 0, delay: 700, duration: 500,
      onComplete: () => this.destroy(),
    });
  }

  destroy(): void {
    if (this.removed) return;
    this.removed = true;
    this.sprite.destroy();
    this.shadow.destroy();
    this.bar.destroy();
  }
}
