import Phaser from 'phaser';
import { ABILITY, BLUE, RED, MAP, POS, REWARDS, Team } from '../config/balance';
import { isWalkable, inBase, Decor } from '../world/MapLayout';
import { Ambience } from '../world/Ambience';
import { Sound } from '../audio/SoundManager';
import { Unit } from '../entities/Unit';
import { Hero, HeroBase, HeroStats } from '../entities/Hero';
import { EnemyHero } from '../entities/EnemyHero';
import { Minion } from '../entities/Minion';
import { Tower } from '../entities/Tower';
import { Nexus } from '../entities/Nexus';
import { CombatSystem } from '../systems/CombatSystem';
import { AbilitySystem, Slot } from '../systems/AbilitySystem';
import { AISystem } from '../systems/AISystem';
import { WaveSystem } from '../systems/WaveSystem';
import type { UIScene } from './UIScene';

export interface MatchResult { victory: boolean; stats: HeroStats; level: number; duration: number }

export class GameScene extends Phaser.Scene {
  now = 0;
  units: Unit[] = [];
  hero!: Hero;
  bot!: EnemyHero;
  towers: Tower[] = [];
  nexuses: Nexus[] = [];
  combat!: CombatSystem;
  abilities!: AbilitySystem;
  ai!: AISystem;
  waves!: WaveSystem;
  ambience!: Ambience;
  over = false;
  shakeOn = true;
  armed: Slot | null = null;
  moveTarget: { x: number; y: number } | null = null;
  cursors: Phaser.Types.Input.Keyboard.CursorKeys | null = null;
  aim!: Phaser.GameObjects.Graphics;
  wasInBase = true;
  wasAlive = true;
  ui: UIScene | null = null;

  constructor() {
    super('Game');
  }

  create(): void {
    this.now = 0;
    this.units = [];
    this.towers = [];
    this.nexuses = [];
    this.over = false;
    this.armed = null;
    this.moveTarget = null;
    this.wasInBase = true;
    this.wasAlive = true;
    this.shakeOn = this.registry.get('shake') !== false;

    this.add.image(0, 0, 'ground').setOrigin(0).setDepth(-1000);
    this.ambience = new Ambience(this, (this.registry.get('decor') as Decor[] | undefined) ?? []);
    this.combat = new CombatSystem(this);
    for (const t of [BLUE, RED] as Team[]) {
      const tp = t === BLUE ? POS.blueTower : POS.redTower;
      const np = t === BLUE ? POS.blueNexus : POS.redNexus;
      const tw = new Tower(this, t, tp.x, tp.y);
      const nx = new Nexus(this, t, np.x, np.y);
      this.towers[t] = tw;
      this.nexuses[t] = nx;
      this.units.push(tw, nx);
    }
    this.hero = new Hero(this);
    this.bot = new EnemyHero(this);
    this.units.push(this.hero, this.bot);
    this.abilities = new AbilitySystem(this, this.hero);
    this.ai = new AISystem(this);
    this.waves = new WaveSystem(this);
    this.aim = this.add.graphics().setDepth(-440);

    const cam = this.cameras.main;
    cam.setBounds(0, 0, MAP.size, MAP.size);
    cam.setRoundPixels(true);
    cam.centerOn(this.hero.x, this.hero.y);
    cam.startFollow(this.hero.sprite, true, 0.1, 0.1);
    cam.fadeIn(700, 15, 31, 24);

    const kb = this.input.keyboard;
    this.cursors = kb?.createCursorKeys() ?? null;
    kb?.on('keydown-Q', () => this.castSlot('Q'));
    kb?.on('keydown-W', () => this.castSlot('W'));
    kb?.on('keydown-E', () => this.castSlot('E'));
    kb?.on('keydown-R', () => this.castSlot('R'));
    kb?.on('keydown-B', () => { if (!this.over) this.hero?.startRecall(); });
    this.input.mouse?.disableContextMenu();
    this.input.on('pointerdown', (p: Phaser.Input.Pointer) => this.onPointerDown(p));
    this.events.once('shutdown', () => {
      this.input.removeAllListeners();
      this.input.keyboard?.removeAllListeners();
      this.cameras.main.postFX?.clear();
    });

    this.scene.launch('UI');
    this.ui = this.scene.get('UI') as UIScene;
    Sound.startMusic();
  }

  // ---------- entrada ----------
  private worldPointer(): { x: number; y: number } {
    const p = this.input.activePointer;
    const w = this.cameras.main.getWorldPoint(p.x, p.y);
    return { x: w.x, y: w.y };
  }

  private onPointerDown(p: Phaser.Input.Pointer): void {
    if (this.over || this.ui?.blocks(p.x, p.y)) return;
    const w = this.cameras.main.getWorldPoint(p.x, p.y);
    if (p.rightButtonDown()) {
      if (this.armed) { this.armed = null; return; }
      this.setMoveTarget(w.x, w.y, true);
      return;
    }
    if (this.armed) {
      const s = this.armed;
      this.armed = null;
      this.abilities.cast(s, w.x, w.y);
    }
  }

  setMoveTarget(x: number, y: number, marker: boolean): void {
    if (!this.hero?.alive || this.over) return;
    this.moveTarget = { x, y };
    if (!marker) return;
    const m = this.add.graphics().setDepth(-430);
    m.lineStyle(2, 0x9bd07a, 1).strokeEllipse(0, 0, 22, 11);
    m.fillStyle(0x9bd07a, 0.35).fillEllipse(0, 0, 10, 5);
    m.setPosition(x, y);
    this.tweens.add({ targets: m, scale: 0.3, alpha: 0, duration: 450, onComplete: () => m.destroy() });
  }

  castSlot(s: Slot): void {
    if (this.over || !this.hero?.alive) return;
    const w = this.worldPointer();
    this.armed = null;
    this.abilities.cast(s, w.x, w.y);
  }

  /** Clique no slot da HUD: W lança na hora; Q/E/R ficam armadas até o próximo clique no mapa */
  arm(s: Slot): void {
    if (this.over || !this.hero?.alive) return;
    if (s === 'W') { this.castSlot('W'); return; }
    if (s === 'AA') { this.notify('Clique com o botão esquerdo no mapa para atacar', '#f5e6c8', 1400); return; }
    if (this.abilities.locked(s) || this.abilities.remaining(s) > 0) { Sound.play('deny', 200); return; }
    this.armed = this.armed === s ? null : s;
  }

  // ---------- utilidades usadas pelas entidades ----------
  private canStand(x: number, y: number): boolean {
    return x > 10 && y > 10 && x < MAP.size - 10 && y < MAP.size - 10 && isWalkable(x, y);
  }

  moveUnit(u: Unit, dx: number, dy: number): void {
    const free = !this.canStand(u.x, u.y);
    const nx = u.x + dx;
    if (free || this.canStand(nx, u.y)) u.x = nx;
    const ny = u.y + dy;
    if (free || this.canStand(u.x, ny)) u.y = ny;
    for (const s of [...this.towers, ...this.nexuses] as Unit[]) {
      if (!s?.alive || s === u) continue;
      const ddx = u.x - s.x;
      const ddy = u.y - s.y;
      const d = Math.hypot(ddx, ddy);
      const min = s.radius + u.radius * 0.8;
      if (d < min && d > 0.001) {
        u.x = s.x + (ddx / d) * min;
        u.y = s.y + (ddy / d) * min;
      }
    }
    u.x = Phaser.Math.Clamp(u.x, 10, MAP.size - 10);
    u.y = Phaser.Math.Clamp(u.y, 10, MAP.size - 10);
  }

  isNearCamera(x: number, y: number, margin = 120): boolean {
    const v = this.cameras.main.worldView;
    return x > v.x - margin && x < v.x + v.width + margin && y > v.y - margin && y < v.y + v.height + margin;
  }

  shake(duration: number, intensity: number): void {
    if (this.shakeOn) this.cameras.main.shake(duration, intensity);
  }

  setShake(on: boolean): void {
    this.shakeOn = on;
    this.registry.set('shake', on);
  }

  notify(text: string, color: string, duration = 2200): void {
    this.ui?.notify(text, color, duration);
  }

  private reward(h: HeroBase, xp: number, gold: number, at: Unit): void {
    h.addXp(xp);
    if (gold <= 0) return;
    h.addGold(gold);
    if (h.isPlayer) {
      this.combat.float(at.x, at.y - 44, `+${gold}`, '#ffd24a', 13);
      Sound.play('gold', 80);
    }
  }

  onKill(target: Unit, source: Unit | null): void {
    if (!target?.alive) return;
    const lvl = this.hero.level;
    target.die(source);
    const enemy: Team = target.team === BLUE ? RED : BLUE;
    const heroes: HeroBase[] = [this.hero, this.bot];
    const killer = source?.kind === 'hero' ? (source as HeroBase) : null;
    const enemyHero = heroes.find((h: HeroBase) => h.team === enemy) ?? null;
    if (target.kind === 'minion') {
      if (killer) {
        this.reward(killer, REWARDS.minion.xp, REWARDS.minion.gold, target);
        killer.stats.cs++;
      }
      for (const h of heroes) {
        if (h !== killer && h.alive && h.team === enemy && h.distTo(target) <= REWARDS.shareRange) {
          this.reward(h, REWARDS.minion.xp, Math.round(REWARDS.minion.gold * 0.4), target);
        }
      }
    } else if (target.kind === 'hero') {
      if (enemyHero) {
        this.reward(enemyHero, REWARDS.hero.xp, REWARDS.hero.gold, target);
        enemyHero.stats.kills++;
      }
      if (target.isPlayer) {
        this.notify('Você foi derrotado! Renascendo na base...', '#ff6b6b', 2600);
        this.armed = null;
        this.moveTarget = null;
      } else this.notify('Vex foi derrotada! +100 de ouro', '#00c8b4', 2400);
      this.shake(260, 0.008);
    } else {
      if (enemyHero) {
        this.reward(enemyHero, REWARDS.structure.xp, REWARDS.structure.gold, target);
        enemyHero.stats.structures++;
      }
      Sound.play('structure');
      if (target.kind === 'tower') {
        if (target.team === RED) this.notify('Torre inimiga destruída! O núcleo de Vex está exposto!', '#00c8b4', 3200);
        else this.notify('Sua torre caiu! Defenda o núcleo!', '#ff6b6b', 3200);
      } else this.endMatch(enemy);
    }
    if (this.hero.level > lvl) {
      Sound.play('levelup');
      this.notify(this.hero.level === ABILITY.R.unlockLevel ? `Nível ${this.hero.level}! Jardim de Fogo (R) desbloqueado!` : `Nível ${this.hero.level}! Mais vida e dano.`, '#ffe07a', 2600);
    }
  }

  private endMatch(win: Team): void {
    if (this.over) return;
    this.over = true;
    this.armed = null;
    this.moveTarget = null;
    this.hero.cancelRecall();
    const victory = win === BLUE;
    const nx = this.nexuses[victory ? RED : BLUE];
    const cam = this.cameras.main;
    cam.postFX?.clear();
    cam.stopFollow();
    if (nx) cam.pan(nx.x, nx.y - 40, 1200, 'Sine.easeInOut');
    Sound.stopMusic();
    this.time.delayedCall(700, () => Sound.play(victory ? 'victory' : 'defeat'));
    const result: MatchResult = { victory, stats: { ...this.hero.stats }, level: this.hero.level, duration: this.now };
    this.time.delayedCall(2400, () => this.ui?.showEnd(result));
  }

  private setGray(on: boolean): void {
    try {
      const fx = this.cameras.main.postFX;
      if (!fx) return;
      fx.clear();
      if (on) fx.addColorMatrix().grayscale(0.8);
    } catch (e) {
      console.error(e);
    }
  }

  private drawAim(): void {
    const g = this.aim;
    g.clear();
    const s = this.armed;
    const h = this.hero;
    if (!s || !h?.alive) return;
    const w = this.worldPointer();
    const range = s === 'Q' ? ABILITY.Q.range : s === 'E' ? ABILITY.E.distance : ABILITY.R.castRange;
    g.lineStyle(2, 0xffb347, 0.55).strokeCircle(h.x, h.y, range);
    g.fillStyle(0xffb347, 0.05).fillCircle(h.x, h.y, range);
    const ang = Math.atan2(w.y - h.y, w.x - h.x);
    if (s === 'Q' || s === 'E') {
      const len = range;
      g.lineStyle(s === 'Q' ? ABILITY.Q.width : 14, 0xffd27a, 0.28).lineBetween(h.x, h.y - 10, h.x + Math.cos(ang) * len, h.y - 10 + Math.sin(ang) * len);
    } else {
      const d = Math.min(ABILITY.R.castRange, Math.hypot(w.x - h.x, w.y - h.y));
      const cx = h.x + Math.cos(ang) * d;
      const cy = h.y + Math.sin(ang) * d;
      g.lineStyle(3, 0xff6b3a, 0.8).strokeCircle(cx, cy, ABILITY.R.radius);
      g.fillStyle(0xff6b3a, 0.12).fillCircle(cx, cy, ABILITY.R.radius);
    }
  }

  update(_time: number, delta: number): void {
    const dms = Math.min(delta, 50);
    this.now += dms;
    const dt = dms / 1000;
    const h = this.hero;
    const p = this.input.activePointer;
    const blocked = this.ui?.blocks(p.x, p.y) ?? false;
    let ax = 0;
    let ay = 0;
    if (!this.over && h.alive) {
      const c = this.cursors;
      if (c?.left?.isDown) ax -= 1;
      if (c?.right?.isDown) ax += 1;
      if (c?.up?.isDown) ay -= 1;
      if (c?.down?.isDown) ay += 1;
      if (ax || ay) this.moveTarget = null;
      else {
        if (p.rightButtonDown() && !blocked && !this.armed) {
          const w = this.worldPointer();
          this.moveTarget = { x: w.x, y: w.y };
        }
        const mt = this.moveTarget;
        if (mt) {
          const dx = mt.x - h.x;
          const dy = mt.y - h.y;
          if (Math.hypot(dx, dy) < 6 || h.dash) this.moveTarget = null;
          else { ax = dx; ay = dy; }
        }
      }
      if (p.leftButtonDown() && !blocked && !this.armed && this.abilities.remaining('AA') <= 0) {
        const w = this.worldPointer();
        this.abilities.cast('AA', w.x, w.y);
      }
    }
    h.update(dt, ax, ay);
    if (!this.over) {
      this.bot.update(dt);
      this.ai.update(dt);
      this.waves.update();
    } else this.bot.updateVisual();
    for (const u of this.units) {
      if (u.kind === 'minion') (u as Minion).update(dt);
      else if (u.kind === 'tower') (u as Tower).update(dt);
      else if (u.kind === 'nexus') (u as Nexus).update(dt);
    }
    this.units = this.units.filter((u: Unit) => u.alive || u.kind !== 'minion');
    this.combat.update(dt);
    this.ambience.update(this.now, this.cameras.main);
    this.drawAim();

    const ib = h.alive && inBase(BLUE, h.x, h.y);
    if (ib && !this.wasInBase && !this.over) this.ui?.openShop();
    this.wasInBase = ib;
    if (h.alive !== this.wasAlive) {
      this.wasAlive = h.alive;
      this.setGray(!h.alive && !this.over);
    }
  }
}
