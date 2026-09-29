import { POS, RED, VEX, SHOP, ItemId } from '../config/balance';
import { inBase, isWalkable } from '../world/MapLayout';
import { Sound } from '../audio/SoundManager';
import { HeroBase } from './Hero';
import { Unit } from './Unit';
import type { GameScene } from '../scenes/GameScene';

/** Vex, a Tecelã de Sombras — herói controlado pela IA */
export class EnemyHero extends HeroBase {
  cd = { aa: 0, bolt: 0, pool: 0, blink: 0 };

  constructor(scene: GameScene) {
    super(scene, RED, {
      key: 'vex', name: 'Vex', baseHp: VEX.baseHp, hpPerLevel: VEX.hpPerLevel, baseDamage: VEX.baseDamage,
      damagePerLevel: VEX.damagePerLevel, speed: VEX.speed, radius: VEX.radius, spawn: POS.redSpawn,
    });
    this.facing = -1;
  }

  update(dt: number): void {
    if (!this.alive) {
      if (this.scene.now >= this.deadUntil) {
        this.respawn();
        this.facing = -1;
      }
      this.updateVisual();
      return;
    }
    this.baseTick(dt);
    if (inBase(this.team, this.x, this.y)) this.autoBuy();
    this.updateVisual();
  }

  moveToward(tx: number, ty: number, dt: number): boolean {
    const dx = tx - this.x;
    const dy = ty - this.y;
    const d = Math.hypot(dx, dy);
    if (d < 6) { this.moving = false; return true; }
    const sp = Math.min(d, this.cfg.speed * this.speedMul() * dt);
    this.scene.moveUnit(this, (dx / d) * sp, (dy / d) * sp);
    this.moving = true;
    if (this.scene.now >= this.animLockUntil) this.face(dx);
    return false;
  }

  ready(k: keyof EnemyHero['cd']): boolean {
    return this.scene.now >= this.cd[k];
  }

  basicAttack(target: Unit): void {
    if (!this.ready('aa') || !target.alive) return;
    this.cd.aa = this.scene.now + VEX.attackCd;
    this.face(target.x - this.x);
    this.animLockUntil = this.scene.now + 380;
    this.playAnim('attack', true);
    this.moving = false;
    this.scene.combat?.homing({
      x: this.x + this.facing * 16, y: this.y - 30, target, tex: 'proj-orb', speed: VEX.projSpeed,
      damage: this.damage, source: this, glow: 0x9b59ff,
    });
    Sound.play('orb');
  }

  castBolt(tx: number, ty: number): void {
    if (!this.ready('bolt')) return;
    this.cd.bolt = this.scene.now + VEX.bolt.cd * this.cdrMul;
    this.face(tx - this.x);
    this.animLockUntil = this.scene.now + 380;
    this.playAnim('attack', true);
    this.scene.combat?.linear({
      x: this.x, y: this.y - 24, angle: Math.atan2(ty - (this.y - 24), tx - this.x), speed: VEX.bolt.speed,
      range: VEX.bolt.range, tex: 'proj-bolt', damage: VEX.bolt.damage + (this.level - 1) * 10, source: this,
      width: VEX.bolt.width, trail: 0x9b59ff,
    });
    Sound.play('bolt');
  }

  castPool(x: number, y: number): void {
    if (!this.ready('pool')) return;
    this.cd.pool = this.scene.now + VEX.pool.cd * this.cdrMul;
    this.face(x - this.x);
    this.animLockUntil = this.scene.now + 380;
    this.playAnim('attack', true);
    this.scene.combat?.area({
      x, y, radius: VEX.pool.radius, delay: VEX.pool.delay, damage: VEX.pool.damage + (this.level - 1) * 12,
      source: this, kind: 'shadow', slow: 0.6, slowDuration: 1200,
    });
    Sound.play('pool');
  }

  blink(dx: number, dy: number): void {
    if (!this.ready('blink')) return;
    const len = Math.hypot(dx, dy) || 1;
    let best = 0;
    for (let d = 20; d <= VEX.blink.distance; d += 10) {
      if (isWalkable(this.x + (dx / len) * d, this.y + (dy / len) * d)) best = d;
      else break;
    }
    if (best < 40) return;
    this.cd.blink = this.scene.now + VEX.blink.cd * this.cdrMul;
    this.scene.combat?.burst(this.x, this.y - 20, 'px-spark', 0x9b59ff, 20, 90);
    this.x += (dx / len) * best;
    this.y += (dy / len) * best;
    this.scene.combat?.burst(this.x, this.y - 20, 'px-spark', 0x9b59ff, 20, 90);
    Sound.play('blink');
  }

  autoBuy(): void {
    const order: ItemId[] = ['amber', 'bark', 'tide', 'amber', 'bark'];
    for (const id of order) {
      const it = SHOP.find((s: { id: ItemId }) => s.id === id);
      if (it && this.gold >= it.cost && (this.items[id] ?? 0) < it.max) {
        this.buy(id);
        return;
      }
    }
  }
}
