import Phaser from 'phaser';
import { VIEW } from '../config/balance';
import { Sound } from '../audio/SoundManager';
import { HUD } from '../ui/HUD';
import { Shop } from '../ui/Shop';
import { Minimap } from '../ui/Minimap';
import { Button, TXT, button, drawVolume, fmtTime, panel } from '../ui/Widgets';
import { CONTROLS } from './MenuScene';
import type { GameScene, MatchResult } from './GameScene';

/** Cena de interface sobreposta ao jogo */
export class UIScene extends Phaser.Scene {
  gs!: GameScene;
  hud: HUD | null = null;
  shop: Shop | null = null;
  minimap: Minimap | null = null;
  banner: Phaser.GameObjects.Text | null = null;
  bannerTween: Phaser.Tweens.Tween | null = null;
  pauseLayer: Phaser.GameObjects.Container | null = null;
  shakeBtn: Button | null = null;
  pauseVol: Phaser.GameObjects.Graphics | null = null;
  paused = false;
  ended = false;
  ready = false;

  constructor() {
    super('UI');
  }

  create(): void {
    this.gs = this.scene.get('Game') as GameScene;
    this.paused = false;
    this.ended = false;
    this.add.image(VIEW.w / 2, VIEW.h / 2, 'vignette').setDisplaySize(VIEW.w, VIEW.h).setAlpha(0.55);
    this.hud = new HUD(this, this.gs);
    this.minimap = new Minimap(this, this.gs);
    this.shop = new Shop(this, this.gs);
    this.banner = this.add.text(VIEW.w / 2, 130, '', TXT(22, '#ffe07a', { fontStyle: 'bold', strokeThickness: 6 })).setOrigin(0.5).setAlpha(0).setDepth(30);
    this.pauseLayer = this.buildPause();
    const kb = this.input.keyboard;
    kb?.on('keydown-ESC', () => { if (this.shop?.isOpen && !this.paused) this.shop.close(); else this.togglePause(); });
    kb?.on('keydown-P', () => this.toggleShop());
    kb?.on('keydown-ONE', () => this.shop?.buyIndex(0));
    kb?.on('keydown-TWO', () => this.shop?.buyIndex(1));
    kb?.on('keydown-THREE', () => this.shop?.buyIndex(2));
    this.events.once('shutdown', () => {
      this.ready = false;
      this.input.keyboard?.removeAllListeners();
    });
    this.ready = true;
    this.notify('Destrua a torre e o núcleo de Vex! (Esc = pausa e controles)', '#f5e6c8', 3600);
  }

  blocks(x: number, y: number): boolean {
    if (!this.ready) return false;
    if (this.paused || this.ended) return true;
    if (this.shop?.contains(x, y)) return true;
    if (this.minimap?.contains(x, y)) return true;
    return this.hud?.rects?.some((r: Phaser.Geom.Rectangle) => r.contains(x, y)) ?? false;
  }

  notify(text: string, color: string, duration = 2200): void {
    const b = this.banner;
    if (!this.ready || !b) return;
    this.bannerTween?.stop();
    b.setText(text).setColor(color).setAlpha(0).setScale(0.8).setY(140);
    this.bannerTween = this.tweens.add({
      targets: b, alpha: 1, scale: 1, y: 130, duration: 220, ease: 'Back.Out',
      onComplete: () => { this.bannerTween = this.tweens.add({ targets: b, alpha: 0, y: 120, delay: duration, duration: 400 }); },
    });
  }

  openShop(): void {
    if (!this.ended && !this.paused) this.shop?.open();
  }

  toggleShop(): void {
    if (!this.ended && !this.paused) this.shop?.toggle();
  }

  // ---------- pausa ----------
  private buildPause(): Phaser.GameObjects.Container {
    const c = this.add.container(0, 0).setVisible(false).setDepth(100);
    c.add(this.add.rectangle(0, 0, VIEW.w, VIEW.h, 0x07100b, 0.72).setOrigin(0).setInteractive());
    const w = 640;
    const h = 560;
    const x0 = VIEW.w / 2 - w / 2;
    const y0 = VIEW.h / 2 - h / 2;
    c.add(panel(this, x0, y0, w, h, 'ui-wood'));
    c.add(panel(this, x0 + 20, y0 + 70, w - 40, 330, 'ui-parch', 6));
    c.add(this.add.text(VIEW.w / 2, y0 + 38, 'PAUSADO', TXT(34, '#ffe07a', { fontStyle: 'bold' })).setOrigin(0.5));
    CONTROLS.forEach(([k, d]: [string, string], i: number) => {
      const y = y0 + 98 + i * 30;
      c.add(panel(this, x0 + 44, y - 12, 150, 25, 'ui-stone', 6));
      c.add(this.add.text(x0 + 119, y, k, TXT(13, '#ffe07a')).setOrigin(0.5));
      c.add(this.add.text(x0 + 212, y, d, { fontFamily: TXT(12).fontFamily, fontSize: '15px', color: '#3b2414' }).setOrigin(0, 0.5));
    });
    const by = y0 + 440;
    c.add(button(this, VIEW.w / 2 - 150, by, 260, 46, 'Retomar', () => this.togglePause(), { size: 20, color: '#ffe07a' }).root);
    this.shakeBtn = button(this, VIEW.w / 2 + 150, by, 260, 46, '', () => {
      this.gs.setShake(!this.gs.shakeOn);
      this.refreshPause();
    }, { size: 16 });
    c.add(this.shakeBtn.root);
    c.add(this.add.text(x0 + 60, by + 62, 'Volume', TXT(16)).setOrigin(0, 0.5));
    c.add(button(this, x0 + 160, by + 62, 34, 34, '-', () => { Sound.setVolume(Sound.volume - 0.2); this.refreshPause(); }, { size: 16 }).root);
    c.add(button(this, x0 + 272, by + 62, 34, 34, '+', () => { Sound.setVolume(Sound.volume + 0.2); this.refreshPause(); }, { size: 16 }).root);
    this.pauseVol = this.add.graphics();
    c.add(this.pauseVol);
    c.add(button(this, VIEW.w / 2 + 150, by + 62, 260, 40, 'Menu principal', () => this.toMenu(), { size: 16 }).root);
    return c;
  }

  private refreshPause(): void {
    this.shakeBtn?.setLabel(`Tremor da câmera: ${this.gs.shakeOn ? 'Ligado' : 'Desligado'}`);
    if (this.pauseVol) drawVolume(this.pauseVol, VIEW.w / 2 - 320 + 195, VIEW.h / 2 - 280 + 440 + 72, Sound.volume, Sound.muted);
  }

  togglePause(): void {
    if (this.ended || !this.ready) return;
    this.paused = !this.paused;
    const layer = this.pauseLayer;
    if (this.paused) {
      this.shop?.close();
      this.scene.pause('Game');
      this.refreshPause();
      layer?.setVisible(true).setAlpha(0);
      this.tweens.add({ targets: layer, alpha: 1, duration: 160 });
    } else {
      this.scene.resume('Game');
      layer?.setVisible(false);
    }
  }

  private toMenu(): void {
    Sound.startMusic();
    this.scene.stop('Game');
    this.scene.start('Menu');
  }

  // ---------- fim de partida ----------
  showEnd(r: MatchResult): void {
    if (this.ended || !this.ready) return;
    this.ended = true;
    this.shop?.close();
    const c = this.add.container(0, 0).setDepth(200);
    const dim = this.add.rectangle(0, 0, VIEW.w, VIEW.h, r.victory ? 0x0a1a14 : 0x1a0808, 0.75).setOrigin(0).setInteractive().setAlpha(0);
    c.add(dim);
    this.tweens.add({ targets: dim, alpha: 1, duration: 500 });
    const col = r.victory ? '#ffb347' : '#ff6b6b';
    const glow = this.add.image(VIEW.w / 2, 130, 'glow').setBlendMode(Phaser.BlendModes.ADD).setTint(r.victory ? 0xffb347 : 0xff4a3a).setScale(10, 3).setAlpha(0.5);
    c.add(glow);
    this.tweens.add({ targets: glow, alpha: 0.25, duration: 1200, yoyo: true, repeat: -1 });
    const title = this.add.text(VIEW.w / 2, 125, r.victory ? 'VITÓRIA!' : 'DERROTA', TXT(80, col, { fontStyle: 'bold', strokeThickness: 12 })).setOrigin(0.5).setScale(0.2).setAlpha(0);
    c.add(title);
    this.tweens.add({ targets: title, scale: 1, alpha: 1, duration: 700, ease: 'Back.Out' });
    c.add(this.add.text(VIEW.w / 2, 192, r.victory ? 'O núcleo de Vex ruiu. A floresta volta a respirar!' : 'Seu núcleo foi destruído. As sombras tomam o bosque...', TXT(18)).setOrigin(0.5));
    const w = 520;
    const h = 330;
    const x0 = VIEW.w / 2 - w / 2;
    const y0 = 226;
    const pnl = this.add.container(0, 40).setAlpha(0);
    c.add(pnl);
    this.tweens.add({ targets: pnl, y: 0, alpha: 1, delay: 300, duration: 500, ease: 'Cubic.Out' });
    pnl.add(panel(this, x0, y0, w, h, 'ui-wood'));
    pnl.add(panel(this, x0 + 16, y0 + 16, w - 32, h - 32, 'ui-parch', 6));
    const rows: [string, number, string][] = [
      ['Abates', r.stats.kills, ''],
      ['Mortes', r.stats.deaths, ''],
      ['Tropas abatidas', r.stats.cs, ''],
      ['Dano causado', r.stats.damage, ''],
      ['Ouro obtido', r.stats.gold, ''],
      ['Estruturas destruídas', r.stats.structures, ''],
      ['Nível final', r.level, ''],
      ['Duração', r.duration, 'time'],
    ];
    rows.forEach(([label, val, kind]: [string, number, string], i: number) => {
      const y = y0 + 44 + i * 34;
      if (i % 2 === 0) pnl.add(this.add.rectangle(x0 + 30, y - 14, w - 60, 28, 0xc8a878, 0.35).setOrigin(0));
      pnl.add(this.add.text(x0 + 44, y, label, { fontFamily: TXT(12).fontFamily, fontSize: '18px', color: '#3b2414' }).setOrigin(0, 0.5));
      const t = this.add.text(x0 + w - 44, y, '0', TXT(20, '#c17a4a', { stroke: '#3b2414', strokeThickness: 3, fontStyle: 'bold' })).setOrigin(1, 0.5);
      pnl.add(t);
      const o = { v: 0 };
      this.tweens.add({
        targets: o, v: val, delay: 600 + i * 120, duration: 900, ease: 'Cubic.Out',
        onUpdate: () => t.setText(kind === 'time' ? fmtTime(o.v) : String(Math.round(o.v))),
        onComplete: () => t.setText(kind === 'time' ? fmtTime(val) : String(Math.round(val))),
      });
    });
    const again = button(this, VIEW.w / 2 - 140, 610, 250, 56, 'Jogar Novamente', () => this.scene.start('Game'), { size: 22, color: '#ffe07a' });
    const menu = button(this, VIEW.w / 2 + 140, 610, 250, 56, 'Menu', () => this.toMenu(), { size: 22 });
    c.add([again.root, menu.root]);
    [again.root, menu.root].forEach((b: Phaser.GameObjects.Container, i: number) => {
      b.setAlpha(0);
      this.tweens.add({ targets: b, alpha: 1, delay: 1500 + i * 150, duration: 400 });
    });
    const fx = this.add.particles(0, 0, r.victory ? 'px-leaf' : 'px-smoke', {
      x: { min: 0, max: VIEW.w }, y: -10, lifespan: 5000, speedY: { min: 40, max: 110 }, speedX: { min: -30, max: 30 },
      rotate: { min: 0, max: 360 }, alpha: { start: 1, end: 0.2 }, scale: { min: 1.5, max: 2.5 }, frequency: r.victory ? 40 : 90,
      tint: r.victory ? [0xffe07a, 0x9bd07a, 0xffb347, 0x00c8b4] : [0x6a6a60, 0x8a4040],
    });
    c.add(fx);
  }

  update(_t: number, delta: number): void {
    if (!this.ready) return;
    this.hud?.update();
    this.minimap?.update(delta);
    this.shop?.update();
  }
}
