import Phaser from 'phaser';
import { FONT, HERO, ItemId, LEVEL_XP, MAX_LEVEL, SHOP, START_GOLD, Team, BLUE, POS } from '../config/balance';
import { inBase } from '../world/MapLayout';
import { Sound } from '../audio/SoundManager';
import { Unit } from './Unit';
import type { GameScene } from '../scenes/GameScene';

export interface HeroCfg {
  key: string; name: string; baseHp: number; hpPerLevel: number; baseDamage: number; damagePerLevel: number;
  speed: number; radius: number; spawn: { x: number; y: number };
}
export interface HeroStats { kills: number; deaths: number; cs: number; damage: number; gold: number; structures: number }

export abstract class HeroBase extends Unit {
  cfg: HeroCfg;
  sprite: Phaser.GameObjects.Sprite;
  shadow: Phaser.GameObjects.Image;
  label: Phaser.GameObjects.Text;
  level = 1;
  xp = 0;
  gold = START_GOLD;
  items: Record<ItemId, number> = { amber: 0, bark: 0, tide: 0 };
  facing: 1 | -1 = 1;
  animLockUntil = 0;
  deadUntil = 0;
  moving = false;
  currentAnim = '';
  tintState = '';
  stats: HeroStats = { kills: 0, deaths: 0, cs: 0, damage: 0, gold: 0, structures: 0 };

  constructor(scene: GameScene, team: Team, cfg: HeroCfg) {
    super(scene, team, 'hero', cfg.spawn.x, cfg.spawn.y, cfg.radius, cfg.baseHp, 40, 58);
    this.cfg = cfg;
    this.shadow = scene.add.image(this.x, this.y, 'shadow').setDepth(-10).setScale(1.05, 1.1);
    this.sprite = scene.add.sprite(this.x, this.y, cfg.key, 0).setOrigin(0.5, 53 / 56);
    this.label = scene.add.text(0, 0, '1', { fontFamily: FONT, fontSize: '11px', color: '#ffe7a8', stroke: '#1b1410', strokeThickness: 3 }).setOrigin(0.5).setDepth(9001);
    this.playAnim('idle');
  }

  get damage(): number {
    return this.cfg.baseDamage + (this.level - 1) * this.cfg.damagePerLevel + this.items.amber * 30;
  }

  get cdrMul(): number {
    return Math.max(0.6, 1 - this.items.tide * 0.2);
  }

  get xpProgress(): { cur: number; need: number; ratio: number } {
    if (this.level >= MAX_LEVEL) return { cur: 1, need: 1, ratio: 1 };
    const lo = LEVEL_XP[this.level - 1] ?? 0;
    const hi = LEVEL_XP[this.level] ?? lo + 1;
    const cur = this.xp - lo;
    const need = hi - lo;
    return { cur, need, ratio: need > 0 ? Math.min(1, cur / need) : 1 };
  }

  recalcMaxHp(): void {
    const next = this.cfg.baseHp + (this.level - 1) * this.cfg.hpPerLevel + this.items.bark * 200;
    const diff = next - this.maxHp;
    this.maxHp = next;
    if (this.alive) this.hp = Math.min(this.maxHp, this.hp + Math.max(0, diff));
    this.barDirty = true;
  }

  addXp(n: number): void {
    this.xp += n;
    while (this.level < MAX_LEVEL && this.xp >= (LEVEL_XP[this.level] ?? Infinity)) {
      this.level++;
      this.recalcMaxHp();
      this.label.setText(String(this.level));
      this.onLevelUp();
    }
  }

  onLevelUp(): void {
    this.scene.combat?.burst(this.x, this.y - 20, 'px-spark', 0xffe07a, 26, 140);
    this.scene.combat?.float(this.x, this.y - 70, `NÍVEL ${this.level}`, '#ffe07a', 16);
  }

  addGold(n: number): void {
    this.gold += n;
    this.stats.gold += n;
  }

  buy(id: ItemId): boolean {
    const it = SHOP.find((s: { id: ItemId }) => s.id === id);
    if (!it || !this.alive) return false;
    if ((this.items[id] ?? 0) >= it.max || this.gold < it.cost) return false;
    this.gold -= it.cost;
    this.items[id] = (this.items[id] ?? 0) + 1;
    if (id === 'bark') this.recalcMaxHp();
    return true;
  }

  playAnim(name: string, force = false): void {
    const k = `${this.cfg.key}-${name}`;
    if (this.currentAnim === k && !force) return;
    this.sprite.play(k);
    this.currentAnim = k;
  }

  face(dx: number): void {
    if (Math.abs(dx) > 0.01) this.facing = dx < 0 ? -1 : 1;
  }

  baseTick(dt: number): void {
    if (this.shield > 0 && this.scene.now > this.shieldUntil) {
      this.shield = 0;
      this.barDirty = true;
    }
    if (inBase(this.team, this.x, this.y)) this.heal(HERO.baseRegen * dt);
  }

  updateVisual(): void {
    const rx = Math.round(this.x);
    const ry = Math.round(this.y);
    this.sprite.setPosition(rx, ry).setDepth(ry).setFlipX(this.facing < 0);
    this.shadow.setPosition(rx, ry);
    this.label.setPosition(rx - this.barW / 2 - 8, ry - this.barYOff + 2).setVisible(this.alive);
    this.updateBar();
    if (!this.alive) return;
    if (this.scene.now >= this.animLockUntil) this.playAnim(this.moving ? 'walk' : 'idle');
    const want = this.flashUntil > this.scene.now ? 'flash' : this.slowUntil > this.scene.now ? 'slow' : '';
    if (want !== this.tintState) {
      this.tintState = want;
      if (want === 'flash') this.sprite.setTintFill(0xffffff);
      else if (want === 'slow') this.sprite.setTint(0x9fc8ff);
      else this.sprite.clearTint();
    }
  }

  die(_killer: Unit | null): void {
    this.alive = false;
    this.hp = 0;
    this.shield = 0;
    this.moving = false;
    this.stats.deaths++;
    this.deadUntil = this.scene.now + HERO.respawn;
    this.sprite.clearTint();
    this.tintState = '';
    this.playAnim('death', true);
    this.bar.clear();
    this.scene.combat?.burst(this.x, this.y - 20, 'px-leaf', 0xffffff, 18, 120);
    Sound.play('death');
  }

  respawn(): void {
    this.alive = true;
    this.hp = this.maxHp;
    this.x = this.cfg.spawn.x;
    this.y = this.cfg.spawn.y;
    this.slowUntil = 0;
    this.barDirty = true;
    this.playAnim('idle', true);
    this.scene.combat?.burst(this.x, this.y - 20, 'px-spark', this.team === BLUE ? 0x00c8b4 : 0xff6b6b, 30, 120);
  }
}

/** Nilo, o Guardião da Brasa — herói do jogador */
export class Hero extends HeroBase {
  dash: { vx: number; vy: number; until: number } | null = null;
  recallStart: number | null = null;
  shieldFx: Phaser.GameObjects.Image;
  shieldGlow: Phaser.GameObjects.Image;
  recallFx: Phaser.GameObjects.Image;
  emberTimer = 0;

  constructor(scene: GameScene) {
    super(scene, BLUE, {
      key: 'nilo', name: 'Nilo', baseHp: HERO.baseHp, hpPerLevel: HERO.hpPerLevel, baseDamage: HERO.baseDamage,
      damagePerLevel: HERO.damagePerLevel, speed: HERO.speed, radius: HERO.radius, spawn: POS.blueSpawn,
    });
    this.isPlayer = true;
    this.shieldGlow = scene.add.image(0, 0, 'glow').setBlendMode(Phaser.BlendModes.ADD).setTint(0xc89650).setScale(1.6).setVisible(false);
    this.shieldFx = scene.add.image(0, 0, 'bark-ring').setVisible(false);
    this.recallFx = scene.add.image(0, 0, 'glow').setBlendMode(Phaser.BlendModes.ADD).setTint(0x00c8b4).setScale(1, 2.4).setVisible(false);
  }

  onDamaged(amount: number, source: Unit | null): void {
    super.onDamaged(amount, source);
    if (this.recallStart !== null && amount > 0) this.cancelRecall();
  }

  startRecall(): void {
    if (!this.alive || this.recallStart !== null || this.dash) return;
    this.recallStart = this.scene.now;
    Sound.play('recall');
  }

  cancelRecall(): void {
    if (this.recallStart === null) return;
    this.recallStart = null;
    this.recallFx.setVisible(false);
  }

  startDash(dx: number, dy: number, dist: number, dur: number): void {
    const len = Math.hypot(dx, dy) || 1;
    const sp = dist / (dur / 1000);
    this.dash = { vx: (dx / len) * sp, vy: (dy / len) * sp, until: this.scene.now + dur };
    this.face(dx);
    this.cancelRecall();
  }

  update(dt: number, ax: number, ay: number): void {
    const now = this.scene.now;
    if (!this.alive) {
      this.shieldFx.setVisible(false);
      this.shieldGlow.setVisible(false);
      this.recallFx.setVisible(false);
      if (now >= this.deadUntil) {
        this.respawn();
        Sound.play('respawn');
      }
      this.updateVisual();
      return;
    }
    this.baseTick(dt);
    if (this.dash) {
      this.scene.moveUnit(this, this.dash.vx * dt, this.dash.vy * dt);
      this.moving = true;
      this.emberTimer -= dt;
      if (this.emberTimer <= 0) {
        this.emberTimer = 0.012;
        this.scene.combat?.burst(this.x, this.y - 14, 'px-ember', 0xffffff, 3, 40, 500);
      }
      if (now >= this.dash.until) this.dash = null;
    } else if (ax !== 0 || ay !== 0) {
      const len = Math.hypot(ax, ay) || 1;
      const sp = this.cfg.speed * this.speedMul() * dt;
      this.scene.moveUnit(this, (ax / len) * sp, (ay / len) * sp);
      this.moving = true;
      if (now >= this.animLockUntil) this.face(ax);
      this.cancelRecall();
    } else this.moving = false;
    // canalização do teleporte
    if (this.recallStart !== null) {
      const t = (now - this.recallStart) / HERO.recall;
      this.recallFx.setVisible(true).setPosition(this.x, this.y - 30).setAlpha(0.3 + t * 0.6).setScale(0.8 + t * 0.6, 2.4);
      if (Math.random() < 0.5) this.scene.combat?.burst(this.x + (Math.random() - 0.5) * 30, this.y, 'px-spark', 0x00c8b4, 1, 30, 800);
      if (t >= 1) {
        this.cancelRecall();
        this.x = this.cfg.spawn.x;
        this.y = this.cfg.spawn.y;
        this.scene.combat?.burst(this.x, this.y - 20, 'px-spark', 0x00c8b4, 30, 140);
        this.scene.cameras.main.centerOn(this.x, this.y);
        Sound.play('respawn');
      }
    }
    // escudo Casca Viva
    const hasShield = this.shield > 0;
    this.shieldFx.setVisible(hasShield);
    this.shieldGlow.setVisible(hasShield);
    if (hasShield) {
      const pulse = 0.5 + Math.sin(now * 0.012) * 0.25;
      this.shieldFx.setPosition(this.x, this.y - 20).setDepth(this.y + 1).setRotation(now * 0.0015).setAlpha(0.65 + pulse * 0.3).setScale(0.95 + pulse * 0.1);
      this.shieldGlow.setPosition(this.x, this.y - 20).setDepth(this.y - 1).setAlpha(pulse * 0.6);
    }
    this.updateVisual();
  }

  respawn(): void {
    super.respawn();
    this.scene.cameras.main.centerOn(this.x, this.y);
  }
}
