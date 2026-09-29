import Phaser from 'phaser';
import { SHOP, ShopItem, VIEW } from '../config/balance';
import { Sound } from '../audio/SoundManager';
import type { GameScene } from '../scenes/GameScene';
import type { UIScene } from '../scenes/UIScene';
import { Button, TXT, button, panel } from './Widgets';

interface Card { it: ShopItem; btn: Button; cost: Phaser.GameObjects.Text; pips: Phaser.GameObjects.Graphics; x: number; y: number }

const W = 600;
const H = 360;

/** Loja do Bosque */
export class Shop {
  ui: UIScene;
  gs: GameScene;
  root: Phaser.GameObjects.Container;
  rect: Phaser.Geom.Rectangle;
  gold: Phaser.GameObjects.Text;
  cards: Card[] = [];
  isOpen = false;
  sparks: Phaser.GameObjects.Particles.ParticleEmitter;

  constructor(ui: UIScene, gs: GameScene) {
    this.ui = ui;
    this.gs = gs;
    const x0 = VIEW.w / 2 - W / 2;
    const y0 = VIEW.h / 2 - H / 2 - 10;
    this.rect = new Phaser.Geom.Rectangle(x0, y0, W, H);
    this.root = ui.add.container(x0, y0).setVisible(false).setDepth(40);
    const c = this.root;
    c.add(panel(ui, 0, 0, W, H, 'ui-parch', 6).setInteractive());
    c.add(panel(ui, 0, 0, W, 54, 'ui-wood'));
    c.add(ui.add.text(24, 27, 'LOJA DO BOSQUE', TXT(22, '#ffe07a', { fontStyle: 'bold' })).setOrigin(0, 0.5));
    c.add(ui.add.image(W - 170, 27, 'ic-seed').setScale(1.4));
    this.gold = ui.add.text(W - 154, 27, '0', TXT(20, '#ffd24a', { fontStyle: 'bold' })).setOrigin(0, 0.5);
    c.add(this.gold);
    c.add(button(ui, W - 32, 27, 36, 36, 'X', () => this.close(), { size: 16 }).root);
    SHOP.forEach((it: ShopItem, i: number) => {
      const cx = 30 + i * 186;
      const cy = 72;
      c.add(panel(ui, cx, cy, 168, 226, 'ui-wood'));
      c.add(ui.add.image(cx + 84, cy + 50, 'glow').setBlendMode(Phaser.BlendModes.ADD).setTint(it.id === 'amber' ? 0xffb347 : it.id === 'bark' ? 0x9bd07a : 0x00c8b4).setScale(1.4).setAlpha(0.5));
      c.add(ui.add.image(cx + 84, cy + 50, 'ui-slot'));
      c.add(ui.add.image(cx + 84, cy + 50, it.icon).setScale(1.4));
      c.add(ui.add.text(cx + 84, cy + 98, it.name, TXT(15, '#f5e6c8', { fontStyle: 'bold' })).setOrigin(0.5));
      c.add(ui.add.text(cx + 84, cy + 120, it.desc, TXT(12, '#ffe07a')).setOrigin(0.5));
      const pips = ui.add.graphics();
      c.add(pips);
      c.add(ui.add.image(cx + 64, cy + 164, 'ic-seed'));
      const cost = ui.add.text(cx + 76, cy + 164, String(it.cost), TXT(16, '#ffd24a', { fontStyle: 'bold' })).setOrigin(0, 0.5);
      c.add(cost);
      const btn = button(ui, cx + 84, cy + 198, 132, 34, 'Comprar', () => this.buy(it), { size: 15 });
      c.add(btn.root);
      this.cards.push({ it, btn, cost, pips, x: cx, y: cy });
    });
    c.add(ui.add.text(W / 2, H - 28, 'Teclas 1, 2, 3 compram • P abre/fecha • abre sozinha ao entrar na base', { fontFamily: TXT(12).fontFamily, fontSize: '12px', color: '#7a4f2a' }).setOrigin(0.5));
    this.sparks = ui.add.particles(0, 0, 'px-spark', {
      speed: { min: 60, max: 200 }, lifespan: 700, alpha: { start: 1, end: 0 }, gravityY: 200, tint: [0xffe07a, 0xffb347, 0xffffff], emitting: false,
    }).setDepth(45);
  }

  contains(x: number, y: number): boolean {
    return this.isOpen && this.rect.contains(x, y);
  }

  open(): void {
    if (this.isOpen) return;
    this.isOpen = true;
    this.root.setVisible(true).setAlpha(0).setScale(0.92);
    this.root.setPosition(this.rect.x + W * 0.04, this.rect.y + H * 0.04);
    this.ui.tweens.add({ targets: this.root, alpha: 1, scale: 1, x: this.rect.x, y: this.rect.y, duration: 180, ease: 'Back.Out' });
    Sound.play('click');
  }

  close(): void {
    if (!this.isOpen) return;
    this.isOpen = false;
    this.ui.tweens.add({ targets: this.root, alpha: 0, duration: 120, onComplete: () => { if (!this.isOpen) this.root.setVisible(false); } });
  }

  toggle(): void {
    if (this.isOpen) this.close();
    else this.open();
  }

  buyIndex(i: number): void {
    const it = SHOP[i];
    if (it && this.isOpen) this.buy(it);
  }

  private buy(it: ShopItem): void {
    const h = this.gs?.hero;
    if (!h) return;
    if (!h.alive) { this.gs.notify('Espere renascer para comprar', '#ff6b6b', 1400); Sound.play('deny'); return; }
    if (h.buy(it.id)) {
      Sound.play('buy');
      const card = this.cards.find((c: Card) => c.it.id === it.id);
      if (card) this.sparks.explode(28, this.rect.x + card.x + 84, this.rect.y + card.y + 50);
      this.gs.combat?.burst(h.x, h.y - 20, 'px-spark', 0xffe07a, 18, 110);
      this.gs.notify(`${it.name} adquirido! ${it.desc}`, '#ffe07a', 1600);
    } else {
      Sound.play('deny');
      const n = h.items?.[it.id] ?? 0;
      this.gs.notify(n >= it.max ? 'Limite deste item atingido' : 'Ouro insuficiente', '#ff6b6b', 1300);
    }
  }

  update(): void {
    if (!this.isOpen) return;
    const h = this.gs?.hero;
    if (!h) return;
    this.gold.setText(String(h.gold));
    for (const c of this.cards) {
      const n = h.items?.[c.it.id] ?? 0;
      const maxed = n >= c.it.max;
      const afford = h.gold >= c.it.cost;
      c.btn.setEnabled(!maxed && afford && h.alive);
      c.btn.setLabel(maxed ? 'Máximo' : 'Comprar');
      c.cost.setColor(afford || maxed ? '#ffd24a' : '#ff6b6b');
      const g = c.pips;
      g.clear();
      const pw = 16;
      const sx = c.x + 84 - (c.it.max * (pw + 4)) / 2;
      for (let k = 0; k < c.it.max; k++) {
        g.fillStyle(0x1b1410, 1).fillRect(sx + k * (pw + 4), c.y + 136, pw, 8);
        g.fillStyle(k < n ? 0xffb347 : 0x5a3a1e, 1).fillRect(sx + k * (pw + 4) + 1, c.y + 137, pw - 2, 6);
      }
    }
  }
}
