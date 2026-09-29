import Phaser from 'phaser';
import { NEXUS, Team, TEAM_COLOR } from '../config/balance';
import { Sound } from '../audio/SoundManager';
import { Unit } from './Unit';
import type { GameScene } from '../scenes/GameScene';

/** Árvore anciã com cristais pulsando: o núcleo de cada equipe */
export class Nexus extends Unit {
  img: Phaser.GameObjects.Image;
  crystal: Phaser.GameObjects.Image;
  glow: Phaser.GameObjects.Image;
  aura: Phaser.GameObjects.Image;
  bubble: Phaser.GameObjects.Graphics;
  smoke: Phaser.GameObjects.Particles.ParticleEmitter;
  lastInvulMsg = 0;

  constructor(scene: GameScene, team: Team, x: number, y: number) {
    super(scene, team, 'nexus', x, y, NEXUS.radius, NEXUS.hp, 84, 130);
    scene.add.image(x, y, 'shadow').setDepth(-10).setScale(3.2, 2.6);
    this.aura = scene.add.image(x, y - 50, 'glow').setBlendMode(Phaser.BlendModes.ADD).setTint(TEAM_COLOR[team]).setScale(4.5).setAlpha(0.25).setDepth(-300);
    this.img = scene.add.image(x, y, `nexus-${team}`).setOrigin(0.5, 114 / 118).setDepth(y);
    this.glow = scene.add.image(x, y - 27, 'glow').setBlendMode(Phaser.BlendModes.ADD).setTint(TEAM_COLOR[team]).setScale(1.2).setDepth(y + 1);
    this.crystal = scene.add.image(x, y - 27, `nexus-crystal-${team}`).setDepth(y + 2);
    this.bubble = scene.add.graphics().setDepth(y + 3);
    this.smoke = scene.add.particles(x, y - 70, 'px-smoke', {
      speedY: { min: -40, max: -18 }, speedX: { min: -15, max: 15 }, lifespan: 2000,
      scale: { start: 1.4, end: 3.4 }, alpha: { start: 0.5, end: 0 }, frequency: 120, tint: 0x5a5a50, emitting: false,
      x: { min: -30, max: 30 },
    }).setDepth(y + 5);
  }

  invulnerable(): boolean {
    return this.scene.towers?.[this.team]?.alive ?? false;
  }

  update(_dt: number): void {
    if (!this.alive) return;
    const now = this.scene.now;
    const p = Math.sin(now * 0.004);
    this.crystal.setScale(1 + p * 0.06);
    this.glow.setScale(1.1 + p * 0.25).setAlpha(0.6 + p * 0.3);
    this.aura.setAlpha(0.18 + p * 0.07);
    const g = this.bubble;
    g.clear();
    if (this.invulnerable()) {
      const a = 0.1 + (p + 1) * 0.05;
      g.lineStyle(2, TEAM_COLOR[this.team], a * 3).strokeEllipse(this.x, this.y - 50, 124, 136);
      g.fillStyle(TEAM_COLOR[this.team], a * 0.6).fillEllipse(this.x, this.y - 50, 124, 136);
    }
    if (this.hpRatio < 0.5 && !this.smoke.emitting) this.smoke.start();
    this.updateBar();
  }

  die(_killer: Unit | null): void {
    this.alive = false;
    this.bar.clear();
    this.bubble.clear();
    this.crystal.setVisible(false);
    this.glow.setVisible(false);
    this.img.setTint(0x6a5a50);
    this.scene.combat?.burst(this.x, this.y - 50, 'px-spark', TEAM_COLOR[this.team], 80, 300, 1400);
    this.scene.combat?.burst(this.x, this.y - 50, 'px-leaf', 0xffffff, 50, 220, 1600);
    this.scene.combat?.burst(this.x, this.y - 40, 'px-smoke', 0x8a8a80, 40, 140, 1800);
    this.scene.shake(900, 0.02);
    Sound.play('structure');
  }
}
