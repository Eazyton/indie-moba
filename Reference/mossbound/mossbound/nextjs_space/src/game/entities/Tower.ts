import Phaser from 'phaser';
import { TOWER, Team, TEAM_COLOR } from '../config/balance';
import { Sound } from '../audio/SoundManager';
import { Unit } from './Unit';
import type { GameScene } from '../scenes/GameScene';

/** Santuário vivo: torre defensiva */
export class Tower extends Unit {
  img: Phaser.GameObjects.Image;
  crystal: Phaser.GameObjects.Image;
  glow: Phaser.GameObjects.Image;
  rangeGfx: Phaser.GameObjects.Graphics;
  smoke: Phaser.GameObjects.Particles.ParticleEmitter;
  target: Unit | null = null;
  atkReady = 0;
  rangeState = '';

  constructor(scene: GameScene, team: Team, x: number, y: number) {
    super(scene, team, 'tower', x, y, TOWER.radius, TOWER.hp, 64, 96);
    scene.add.image(x, y + 2, 'shadow').setDepth(-10).setScale(2.2, 2);
    this.img = scene.add.image(x, y, `tower-${team}`).setOrigin(0.5, 80 / 86).setDepth(y);
    this.glow = scene.add.image(x, y - 42, 'glow').setBlendMode(Phaser.BlendModes.ADD).setTint(TEAM_COLOR[team]).setScale(1.3).setDepth(y + 1);
    this.crystal = scene.add.image(x, y - 42, `tower-crystal-${team}`).setDepth(y + 2);
    this.rangeGfx = scene.add.graphics().setDepth(-400);
    this.smoke = scene.add.particles(x, y - 70, 'px-smoke', {
      speedY: { min: -45, max: -20 }, speedX: { min: -12, max: 12 }, lifespan: 1800,
      scale: { start: 1.2, end: 3 }, alpha: { start: 0.55, end: 0 }, frequency: 140, tint: 0x6a6a60, emitting: false,
    }).setDepth(y + 5);
  }

  private drawRange(state: string): void {
    if (state === this.rangeState) return;
    this.rangeState = state;
    const g = this.rangeGfx;
    g.clear();
    if (!state) return;
    const col = state === 'target' ? 0xff4a3a : 0xffb347;
    g.fillStyle(col, state === 'target' ? 0.1 : 0.05).fillCircle(this.x, this.y, TOWER.range);
    g.fillStyle(col, state === 'target' ? 0.9 : 0.6);
    const seg = 64;
    for (let i = 0; i < seg; i++) {
      if (i % 2 === 1) continue;
      const a0 = (i / seg) * Math.PI * 2;
      for (let k = 0; k < 6; k++) {
        const a = a0 + (k / 6) * (Math.PI * 2 / seg);
        g.fillRect(Math.round(this.x + Math.cos(a) * TOWER.range) - 1, Math.round(this.y + Math.sin(a) * TOWER.range) - 1, 3, 3);
      }
    }
  }

  private rank(u: Unit): number {
    if (u.kind === 'minion') return 0;
    if (u.kind === 'hero') return this.scene.now - u.lastHeroAttackAt < 2500 ? 1 : 2;
    return 9;
  }

  update(_dt: number): void {
    if (!this.alive) return;
    const now = this.scene.now;
    const bob = Math.sin(now * 0.003) * 3;
    this.crystal.setY(this.y - 44 + bob);
    this.glow.setY(this.y - 44 + bob).setAlpha(0.55 + Math.sin(now * 0.005) * 0.2);
    // seleção de alvo
    let best: Unit | null = null;
    let bestR = 99;
    let bestD = Infinity;
    for (const u of this.scene.units) {
      if (!u.alive || u.team === this.team || (u.kind !== 'minion' && u.kind !== 'hero')) continue;
      const d = this.distTo(u);
      if (d > TOWER.range + u.radius) continue;
      const r = this.rank(u);
      const sticky = u === this.target ? -60 : 0;
      if (r < bestR || (r === bestR && d + sticky < bestD)) { best = u; bestR = r; bestD = d + sticky; }
    }
    this.target = best;
    if (best && now >= this.atkReady) {
      this.atkReady = now + TOWER.cd;
      this.scene.combat?.homing({
        x: this.x, y: this.y - 44 + bob, target: best, tex: 'proj-tower', speed: TOWER.projSpeed,
        damage: best.kind === 'hero' ? TOWER.heroDamage : TOWER.minionDamage, source: this, spin: 12, glow: 0xffb347,
      });
      if (this.scene.isNearCamera(this.x, this.y)) Sound.play('tower', 80);
    }
    // indicador de alcance para o jogador
    const h = this.scene.hero;
    let state = '';
    if (h && h.alive && h.team !== this.team) {
      const d = this.distTo(h);
      if (d < TOWER.warnRange) state = this.target === h ? 'target' : 'warn';
    }
    this.drawRange(state);
    if (this.hpRatio < 0.5 && !this.smoke.emitting) this.smoke.start();
    this.updateBar();
  }

  die(_killer: Unit | null): void {
    this.alive = false;
    this.bar.clear();
    this.drawRange('');
    this.crystal.setVisible(false);
    this.glow.setVisible(false);
    this.img.setTexture('rubble').setOrigin(0.5, 34 / 40);
    this.smoke.setFrequency(260);
    this.scene.combat?.burst(this.x, this.y - 30, 'px-smoke', 0x8a8a80, 30, 120, 1400);
    this.scene.combat?.burst(this.x, this.y - 40, 'px-spark', TEAM_COLOR[this.team], 40, 220);
    this.scene.combat?.burst(this.x, this.y - 20, 'px-bark', 0xffffff, 24, 180);
    this.scene.shake(400, 0.012);
    Sound.play('structure');
  }
}
