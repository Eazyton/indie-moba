import Phaser from 'phaser';
import { TEAM_COLOR, Team } from '../config/balance';
import type { GameScene } from '../scenes/GameScene';

export type UnitKind = 'hero' | 'minion' | 'tower' | 'nexus';
let nextId = 1;

export abstract class Unit {
  scene: GameScene;
  id: number;
  team: Team;
  kind: UnitKind;
  x: number;
  y: number;
  radius: number;
  hp: number;
  maxHp: number;
  alive = true;
  shield = 0;
  shieldUntil = 0;
  slowUntil = 0;
  slowFactor = 1;
  flashUntil = 0;
  lastHeroAttackAt = -99999;
  bar: Phaser.GameObjects.Graphics;
  barW: number;
  barH: number;
  barYOff: number;
  barDirty = true;
  isPlayer = false;

  constructor(scene: GameScene, team: Team, kind: UnitKind, x: number, y: number, radius: number, maxHp: number, barW: number, barYOff: number) {
    this.scene = scene;
    this.id = nextId++;
    this.team = team;
    this.kind = kind;
    this.x = x;
    this.y = y;
    this.radius = radius;
    this.hp = maxHp;
    this.maxHp = maxHp;
    this.barW = barW;
    this.barH = kind === 'minion' ? 3 : kind === 'hero' ? 5 : 6;
    this.barYOff = barYOff;
    this.bar = scene.add.graphics().setDepth(9000);
  }

  get hpRatio(): number {
    return this.maxHp > 0 ? Math.max(0, this.hp / this.maxHp) : 0;
  }

  speedMul(): number {
    return this.slowUntil > this.scene.now ? this.slowFactor : 1;
  }

  heal(n: number): void {
    if (!this.alive || this.hp >= this.maxHp) return;
    this.hp = Math.min(this.maxHp, this.hp + n);
    this.barDirty = true;
  }

  onDamaged(_amount: number, _source: Unit | null): void {
    this.flashUntil = this.scene.now + 90;
  }

  abstract die(killer: Unit | null): void;

  drawBar(): void {
    const g = this.bar;
    g.clear();
    if (!this.alive) return;
    const w = this.barW;
    const h = this.barH;
    const total = this.maxHp + this.shield;
    const fill = total > 0 ? (Math.max(0, this.hp) / total) * w : 0;
    const sh = total > 0 ? (this.shield / total) * w : 0;
    const col = this.isPlayer ? 0x8fdc5a : TEAM_COLOR[this.team];
    g.fillStyle(0x1b1410, 0.92).fillRect(-w / 2 - 1, -1, w + 2, h + 2);
    g.fillStyle(0x3a2620, 1).fillRect(-w / 2, 0, w, h);
    g.fillStyle(col, 1).fillRect(-w / 2, 0, fill, h);
    g.fillStyle(0xffffff, 0.35).fillRect(-w / 2, 0, fill, 1);
    if (sh > 0) g.fillStyle(0xf5e6c8, 1).fillRect(-w / 2 + fill, 0, sh, h);
    if (this.kind === 'hero') {
      g.fillStyle(0x1b1410, 0.7);
      for (let v = 100; v < total; v += 100) g.fillRect(-w / 2 + (v / total) * w, 0, 1, h - 2);
    }
  }

  updateBar(): void {
    if (this.barDirty) {
      this.drawBar();
      this.barDirty = false;
    }
    this.bar.setPosition(Math.round(this.x), Math.round(this.y - this.barYOff));
  }

  distTo(o: { x: number; y: number }): number {
    return Math.hypot(o.x - this.x, o.y - this.y);
  }

  /** distância até a borda deste alvo */
  edgeDist(o: { x: number; y: number }): number {
    return this.distTo(o) - this.radius;
  }
}
