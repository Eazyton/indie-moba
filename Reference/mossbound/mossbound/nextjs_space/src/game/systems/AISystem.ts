import { BLUE, POS, RED, TOWER, VEX } from '../config/balance';
import { inBase } from '../world/MapLayout';
import type { Unit } from '../entities/Unit';
import type { Minion } from '../entities/Minion';
import type { Nexus } from '../entities/Nexus';
import type { GameScene } from '../scenes/GameScene';

/** Cérebro de Vex: avança com as tropas, luta, usa habilidades e recua */
export class AISystem {
  scene: GameScene;
  state: 'push' | 'retreat' = 'push';
  nextThink = 0;
  focus: Unit | null = null;
  desired: { x: number; y: number } | null = null;

  constructor(scene: GameScene) {
    this.scene = scene;
  }

  update(dt: number): void {
    const b = this.scene.bot;
    if (!b) return;
    if (!b.alive) { this.state = 'push'; this.focus = null; return; }
    const now = this.scene.now;
    if (now >= this.nextThink) {
      this.nextThink = now + 160;
      this.think();
    }
    const f = this.focus;
    if (f && f.alive && b.distTo(f) - f.radius <= VEX.attackRange) {
      if (b.ready('aa')) b.basicAttack(f);
    }
    if (this.desired && now >= b.animLockUntil) {
      b.moveToward(this.desired.x, this.desired.y, dt);
    } else if (now >= b.animLockUntil) b.moving = false;
  }

  private think(): void {
    const s = this.scene;
    const b = s.bot;
    const hero = s.hero;
    if (!b || !hero) return;
    const hp = b.hpRatio;
    const enemyTower = s.towers[BLUE];
    const allies = s.units.filter((u: Unit) => u.alive && u.kind === 'minion' && u.team === RED) as Minion[];
    const enemies = s.units.filter((u: Unit) => u.alive && u.kind === 'minion' && u.team === BLUE) as Minion[];
    const heroD = hero.alive ? b.distTo(hero) : Infinity;
    const heroInLane = Math.abs(hero.y - POS.laneY) < 120 || inBase(RED, hero.x, hero.y);

    // recuar
    if (this.state === 'push' && hp < VEX.retreatAt) this.state = 'retreat';
    if (this.state === 'retreat') {
      if (inBase(RED, b.x, b.y) && hp >= VEX.returnAt) this.state = 'push';
      else {
        this.focus = null;
        this.desired = { x: POS.redSpawn.x, y: POS.laneY };
        if (heroD < 260 && b.ready('blink')) b.blink(1, 0);
        if (heroD < 300 && b.ready('pool')) b.castPool(hero.x, hero.y);
        return;
      }
    }

    const towerAlive = !!enemyTower?.alive;
    const alliesTanking = towerAlive && enemyTower ? allies.filter((m: Minion) => m.distTo(enemyTower) < TOWER.range + 10).length : 0;
    const safeX = towerAlive && enemyTower ? enemyTower.x + TOWER.range + 50 : 0;
    const heroUnderTower = towerAlive && enemyTower ? hero.distTo(enemyTower) < TOWER.range + 20 : false;

    let focus: Unit | null = null;
    const wantsHero = hero.alive && heroInLane && heroD < 460 && (hp >= hero.hpRatio - 0.1 || hero.hpRatio < 0.35) && !(heroUnderTower && alliesTanking === 0);
    if (wantsHero) focus = hero;
    if (!focus) {
      let best: Minion | null = null;
      let bd = 420;
      for (const m of enemies) {
        const d = b.distTo(m);
        if (d < bd && (!towerAlive || !enemyTower || m.distTo(enemyTower) > TOWER.range || alliesTanking > 0)) { best = m; bd = d; }
      }
      focus = best;
    }
    if (!focus && towerAlive && alliesTanking > 0 && enemyTower) focus = enemyTower;
    const enemyNexus = s.nexuses[BLUE] as Nexus | undefined;
    if (!focus && !towerAlive && enemyNexus?.alive && !enemyNexus.invulnerable() && (allies.length > 0 || hp > 0.6)) focus = enemyNexus;
    this.focus = focus;

    // posicionamento
    let dx: number;
    let dy: number;
    if (focus) {
      const range = VEX.attackRange * 0.82 + (focus.kind === 'hero' || focus.kind === 'minion' ? 0 : focus.radius);
      const vx = b.x - focus.x;
      const vy = b.y - focus.y;
      const len = Math.hypot(vx, vy) || 1;
      dx = focus.x + (vx / len) * range;
      dy = focus.y + (vy / len) * range;
    } else {
      let front: Minion | null = null;
      for (const m of allies) if (!front || m.x < front.x) front = m;
      dx = front ? front.x + 80 : POS.redTower.x - 140;
      dy = POS.laneY + 18;
    }
    if (towerAlive && alliesTanking === 0) dx = Math.max(dx, safeX);
    dy = Math.max(POS.laneY - 70, Math.min(POS.laneY + 70, dy));
    this.desired = { x: dx, y: dy };

    // habilidades
    if (hero.alive && heroInLane && !(heroUnderTower && alliesTanking === 0)) {
      if (heroD < VEX.bolt.range && b.ready('bolt') && Math.random() < 0.7) {
        const lead = hero.moving ? 40 * hero.facing : 0;
        b.castBolt(hero.x + lead, hero.y - 20);
      } else if (heroD < VEX.pool.range && b.ready('pool') && Math.random() < 0.6) {
        b.castPool(hero.x, hero.y);
      }
      if (heroD < 140 && hp < 0.5 && b.ready('blink')) b.blink(1, (Math.random() - 0.5) * 0.6);
    } else if (b.ready('pool')) {
      for (const m of enemies) {
        if (b.distTo(m) > VEX.pool.range) continue;
        const cluster = enemies.filter((o: Minion) => o.distTo(m) < VEX.pool.radius).length;
        if (cluster >= 3) { b.castPool(m.x, m.y); break; }
      }
    }
  }
}
