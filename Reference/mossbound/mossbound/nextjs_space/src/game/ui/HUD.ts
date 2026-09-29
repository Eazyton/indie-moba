import Phaser from 'phaser';
import { ABILITY, HERO, SHOP, VIEW } from '../config/balance';
import { Sound } from '../audio/SoundManager';
import { SLOTS, Slot } from '../systems/AbilitySystem';
import type { GameScene } from '../scenes/GameScene';
import type { UIScene } from '../scenes/UIScene';
import { TXT, button, drawVolume, fmtTime, panel } from './Widgets';

interface SlotUI {
  s: Slot; x: number; y: number; bg: Phaser.GameObjects.Image; icon: Phaser.GameObjects.Image; cd: Phaser.GameObjects.Graphics;
  num: Phaser.GameObjects.Text; lock: Phaser.GameObjects.Image; lockTxt: Phaser.GameObjects.Text; ring: Phaser.GameObjects.Graphics; wasCd: boolean;
}

const SLOT_INFO: Record<Slot, { name: string; key: string; desc: string }> = {
  AA: { name: 'Golpe de Lança', key: 'ESQ', desc: `Ataque corpo a corpo • alcance ${HERO.attackRange}px • recarga 0,8s` },
  Q: { name: 'Estilhaço Solar', key: 'Q', desc: `Projétil de ${ABILITY.Q.range}px • ${ABILITY.Q.damage} de dano • recarga 8s` },
  W: { name: 'Casca Viva', key: 'W', desc: `Escudo de ${ABILITY.W.shield} por 4s • recarga 15s` },
  E: { name: 'Passo de Brasa', key: 'E', desc: `Dash de ${ABILITY.E.distance}px até o cursor • recarga 10s` },
  R: { name: 'Jardim de Fogo', key: 'R', desc: `Área de ${ABILITY.R.radius}px • ${ABILITY.R.damage} de dano + lentidão 50% • recarga 60s` },
};

/** Interface principal: painel do herói, slots de habilidade, placar e botões */
export class HUD {
  ui: UIScene;
  gs: GameScene;
  rects: Phaser.Geom.Rectangle[] = [];
  hpG: Phaser.GameObjects.Graphics;
  hpTxt: Phaser.GameObjects.Text;
  lvlTxt: Phaser.GameObjects.Text;
  goldTxt: Phaser.GameObjects.Text;
  goldShown = 0;
  itemTxt: Phaser.GameObjects.Text[] = [];
  itemIcons: Phaser.GameObjects.Image[] = [];
  slots: SlotUI[] = [];
  timeTxt: Phaser.GameObjects.Text;
  waveTxt: Phaser.GameObjects.Text;
  scoreTxt: Phaser.GameObjects.Text;
  volG: Phaser.GameObjects.Graphics;
  muteIcon: Phaser.GameObjects.Image;
  recallG: Phaser.GameObjects.Graphics;
  recallTxt: Phaser.GameObjects.Text;
  deadTxt: Phaser.GameObjects.Text;
  deadSub: Phaser.GameObjects.Text;
  tip: Phaser.GameObjects.Container;
  tipName: Phaser.GameObjects.Text;
  tipDesc: Phaser.GameObjects.Text;
  lastLevel = 1;

  constructor(ui: UIScene, gs: GameScene) {
    this.ui = ui;
    this.gs = gs;
    const add = ui.add;
    // ---- painel do herói ----
    this.rects.push(new Phaser.Geom.Rectangle(12, 12, 340, 112));
    panel(ui, 12, 12, 340, 112, 'ui-wood').setInteractive();
    add.image(56, 60, 'ui-frame').setScale(1.15);
    const port = add.sprite(56, 88, 'nilo', 0).setOrigin(0.5, 1).setScale(1.6).play('nilo-idle');
    const mask = add.graphics().setVisible(false);
    mask.fillStyle(0xffffff).fillCircle(56, 60, 27);
    port.setMask(mask.createGeometryMask());
    add.circle(84, 90, 13, 0x1b1410).setStrokeStyle(2, 0xffb347);
    this.lvlTxt = add.text(84, 89, '1', TXT(15, '#ffe07a', { fontStyle: 'bold' })).setOrigin(0.5);
    add.text(110, 22, 'NILO', TXT(18, '#f5e6c8', { fontStyle: 'bold' }));
    add.text(166, 26, 'Guardião da Brasa', TXT(11, '#e0c9a0'));
    this.hpG = add.graphics();
    this.hpTxt = add.text(225, 57, '', TXT(12, '#ffffff')).setOrigin(0.5);
    add.image(118, 102, 'ic-seed').setScale(1.1);
    this.goldTxt = add.text(130, 102, '0', TXT(16, '#ffd24a', { fontStyle: 'bold' })).setOrigin(0, 0.5);
    SHOP.forEach((it: { icon: string }, i: number) => {
      const x = 232 + i * 38;
      this.itemIcons.push(add.image(x, 102, it.icon).setScale(0.6));
      this.itemTxt.push(add.text(x + 10, 108, '', TXT(10, '#ffffff')).setOrigin(0.5));
    });

    // ---- placar central ----
    this.rects.push(new Phaser.Geom.Rectangle(VIEW.w / 2 - 120, 10, 240, 64));
    panel(ui, VIEW.w / 2 - 120, 10, 240, 64, 'ui-parch', 6).setInteractive();
    this.timeTxt = add.text(VIEW.w / 2, 30, '0:00', TXT(22, '#3b2414', { stroke: '#f5e6c8', strokeThickness: 2 })).setOrigin(0.5);
    this.waveTxt = add.text(VIEW.w / 2, 55, '', { fontFamily: TXT(12).fontFamily, fontSize: '12px', color: '#7a4f2a' }).setOrigin(0.5);
    add.circle(VIEW.w / 2 - 92, 32, 7, 0x00c8b4).setStrokeStyle(2, 0x1b1410);
    add.circle(VIEW.w / 2 + 92, 32, 7, 0xff6b6b).setStrokeStyle(2, 0x1b1410);
    this.scoreTxt = add.text(VIEW.w / 2, 86, '', TXT(14, '#f5e6c8')).setOrigin(0.5);

    // ---- botões superiores ----
    const rx = VIEW.w - 12;
    this.rects.push(new Phaser.Geom.Rectangle(rx - 290, 12, 290, 56));
    panel(ui, rx - 290, 12, 290, 56, 'ui-wood').setInteractive();
    button(ui, rx - 30, 40, 40, 40, '', () => ui.togglePause(), { icon: 'ic-pause', iconScale: 1.4 });
    button(ui, rx - 76, 40, 40, 40, '', () => ui.toggleShop(), { icon: 'ic-shop', iconScale: 1.4 });
    const mute = button(ui, rx - 122, 40, 40, 40, '', () => { Sound.init(); Sound.toggleMute(); }, { icon: 'ic-sound', iconScale: 1.4 });
    this.muteIcon = mute.root.list[2] as Phaser.GameObjects.Image;
    button(ui, rx - 262, 40, 30, 30, '-', () => { Sound.init(); Sound.setVolume(Sound.volume - 0.2); }, { size: 16 });
    button(ui, rx - 170, 40, 30, 30, '+', () => { Sound.init(); Sound.setVolume(Sound.volume + 0.2); }, { size: 16 });
    this.volG = add.graphics();

    // ---- slots de habilidade ----
    const sw = 64;
    const total = SLOTS.length * sw + 20;
    const px = VIEW.w / 2 - total / 2;
    const py = VIEW.h - 92;
    this.rects.push(new Phaser.Geom.Rectangle(px, py, total, 84));
    panel(ui, px, py, total, 84, 'ui-stone').setInteractive();
    SLOTS.forEach((s: Slot, i: number) => {
      const x = px + 10 + sw / 2 + i * sw;
      const y = py + 42;
      const bg = add.image(x, y, 'ui-slot').setInteractive({ useHandCursor: true });
      const icon = add.image(x, y, `ic-${s.toLowerCase()}`).setScale(1.35);
      const cd = add.graphics();
      const num = add.text(x, y, '', TXT(20, '#ffffff', { fontStyle: 'bold' })).setOrigin(0.5);
      const ring = add.graphics();
      const lock = add.image(x, y - 4, 'ic-lock').setScale(1.2).setVisible(false);
      const lockTxt = add.text(x, y + 16, 'Nv 3', TXT(10, '#ffe07a')).setOrigin(0.5).setVisible(false);
      const k = SLOT_INFO[s]?.key ?? s;
      add.rectangle(x - 22, y + 22, k.length > 1 ? 26 : 14, 13, 0x1b1410, 0.9).setOrigin(0, 1).setStrokeStyle(1, 0xc89650);
      add.text(x - 22 + (k.length > 1 ? 13 : 7), y + 16, k, TXT(9, '#ffe07a')).setOrigin(0.5);
      bg.on('pointerdown', () => { gs.arm(s); });
      bg.on('pointerover', () => this.showTip(s, x, py - 8));
      bg.on('pointerout', () => this.tip.setVisible(false));
      this.slots.push({ s, x, y, bg, icon, cd, num, lock, lockTxt, ring, wasCd: false });
    });
    this.recallG = add.graphics();
    this.recallTxt = add.text(VIEW.w / 2, py - 30, 'Teleportando para a base...', TXT(13, '#9ff5ea')).setOrigin(0.5).setVisible(false);

    // ---- tooltip ----
    this.tip = add.container(0, 0).setVisible(false).setDepth(50);
    const tb = panel(ui, -150, -70, 300, 64, 'ui-parch', 6);
    this.tipName = add.text(0, -54, '', TXT(15, '#c17a4a', { stroke: '#3b2414', strokeThickness: 3 })).setOrigin(0.5);
    this.tipDesc = add.text(0, -28, '', { fontFamily: TXT(12).fontFamily, fontSize: '12px', color: '#3b2414', align: 'center', wordWrap: { width: 280 } }).setOrigin(0.5);
    this.tip.add([tb, this.tipName, this.tipDesc]);

    // ---- tela de morte ----
    this.deadTxt = add.text(VIEW.w / 2, VIEW.h / 2 - 60, '', TXT(40, '#ff6b6b', { fontStyle: 'bold', strokeThickness: 8 })).setOrigin(0.5).setVisible(false);
    this.deadSub = add.text(VIEW.w / 2, VIEW.h / 2 - 18, 'Nilo renascerá na base azul', TXT(16, '#f5e6c8')).setOrigin(0.5).setVisible(false);
    this.goldShown = gs.hero?.gold ?? 0;
  }

  private showTip(s: Slot, x: number, y: number): void {
    const info = SLOT_INFO[s];
    this.tipName.setText(`${info?.name ?? s}  [${info?.key ?? s}]`);
    this.tipDesc.setText(info?.desc ?? '');
    this.tip.setPosition(Phaser.Math.Clamp(x, 160, VIEW.w - 160), y).setVisible(true);
  }

  update(): void {
    const h = this.gs?.hero;
    const ab = this.gs?.abilities;
    if (!h || !ab) return;
    const now = this.gs.now;
    // vida e XP
    const g = this.hpG;
    g.clear();
    const bx = 110, by = 48, bw = 230, bh = 18;
    const total = h.maxHp + h.shield;
    g.fillStyle(0x1b1410, 1).fillRect(bx - 2, by - 2, bw + 4, bh + 4);
    g.fillStyle(0x3a2620, 1).fillRect(bx, by, bw, bh);
    const hw = total > 0 ? (Math.max(0, h.hp) / total) * bw : 0;
    const low = h.hpRatio < 0.3;
    g.fillStyle(low ? 0xe0503a : 0x5fb84a, 1).fillRect(bx, by, hw, bh);
    g.fillStyle(0xffffff, 0.3).fillRect(bx, by, hw, 3);
    g.fillStyle(0x000000, 0.18).fillRect(bx, by + bh - 4, hw, 4);
    if (h.shield > 0) g.fillStyle(0xf5e6c8, 1).fillRect(bx + hw, by, (h.shield / total) * bw, bh);
    if (low && h.alive) g.fillStyle(0xff0000, 0.18 + Math.sin(now * 0.012) * 0.12).fillRect(bx, by, bw, bh);
    this.hpTxt.setText(`${Math.ceil(Math.max(0, h.hp))} / ${h.maxHp}${h.shield > 0 ? `  (+${Math.ceil(h.shield)})` : ''}`).setPosition(bx + bw / 2, by + bh / 2);
    const xp = h.xpProgress;
    g.fillStyle(0x1b1410, 1).fillRect(bx - 2, 72, bw + 4, 10);
    g.fillStyle(0x3a2a14, 1).fillRect(bx, 74, bw, 6);
    g.fillStyle(0xffb347, 1).fillRect(bx, 74, bw * xp.ratio, 6);
    g.fillStyle(0xffe07a, 1).fillRect(bx, 74, bw * xp.ratio, 2);
    if (h.level !== this.lastLevel) {
      this.lastLevel = h.level;
      this.lvlTxt.setText(String(h.level)).setScale(2);
      this.ui.tweens.add({ targets: this.lvlTxt, scale: 1, duration: 500, ease: 'Back.Out' });
    }
    // ouro com contador animado
    if (this.goldShown !== h.gold) {
      const diff = h.gold - this.goldShown;
      this.goldShown += Math.sign(diff) * Math.max(1, Math.ceil(Math.abs(diff) * 0.15));
      this.goldTxt.setText(String(this.goldShown));
    }
    SHOP.forEach((it: { id: 'amber' | 'bark' | 'tide' }, i: number) => {
      const n = h.items?.[it.id] ?? 0;
      this.itemIcons[i]?.setAlpha(n > 0 ? 1 : 0.25);
      this.itemTxt[i]?.setText(n > 0 ? `x${n}` : '');
    });
    // placar
    this.timeTxt.setText(fmtTime(now));
    this.waveTxt.setText(`Próxima onda em ${fmtTime(this.gs.waves?.timeToNext() ?? 0)}`);
    this.scoreTxt.setText(`Abates ${h.stats.kills}  •  Mortes ${h.stats.deaths}  •  Tropas ${h.stats.cs}`);
    // volume
    drawVolume(this.volG, VIEW.w - 12 - 238, 50, Sound.volume, Sound.muted);
    this.muteIcon?.setTexture(Sound.muted ? 'ic-mute' : 'ic-sound');
    // slots
    for (const sl of this.slots) {
      const locked = ab.locked(sl.s);
      const rem = ab.remaining(sl.s);
      const tot = ab.totals[sl.s] ?? 1;
      sl.cd.clear();
      sl.ring.clear();
      sl.lock.setVisible(locked);
      sl.lockTxt.setVisible(locked);
      sl.icon.setAlpha(locked || !h.alive ? 0.35 : 1);
      if (!locked && rem > 0) {
        const r = Math.min(1, rem / tot);
        sl.cd.fillStyle(0x0b0906, 0.72).fillRect(sl.x - 23, sl.y - 23 + 46 * (1 - r), 46, 46 * r);
        sl.cd.fillStyle(0xffb347, 0.9).fillRect(sl.x - 23, sl.y - 23 + 46 * (1 - r), 46, 1);
        sl.num.setText(sl.s === 'AA' ? '' : rem >= 1000 ? String(Math.ceil(rem / 1000)) : (rem / 1000).toFixed(1));
      } else sl.num.setText('');
      const onCd = rem > 0;
      if (sl.wasCd && !onCd && !locked && sl.s !== 'AA') {
        const f = this.ui.add.image(sl.x, sl.y, 'glow').setBlendMode(Phaser.BlendModes.ADD).setTint(0xffe07a).setScale(0.6);
        this.ui.tweens.add({ targets: f, scale: 1.3, alpha: 0, duration: 450, onComplete: () => f.destroy() });
      }
      sl.wasCd = onCd;
      if (this.gs.armed === sl.s) {
        const a = 0.6 + Math.sin(now * 0.015) * 0.4;
        sl.ring.lineStyle(3, 0xffb347, a).strokeRect(sl.x - 28, sl.y - 28, 56, 56);
      }
    }
    // teleporte
    this.recallG.clear();
    const rs = h.recallStart;
    this.recallTxt.setVisible(rs !== null);
    if (rs !== null) {
      const t = Math.min(1, (now - rs) / HERO.recall);
      const x = VIEW.w / 2 - 110;
      const y = VIEW.h - 112;
      this.recallG.fillStyle(0x1b1410, 1).fillRect(x - 2, y - 2, 224, 12);
      this.recallG.fillStyle(0x1a4a5c, 1).fillRect(x, y, 220, 8);
      this.recallG.fillStyle(0x00c8b4, 1).fillRect(x, y, 220 * t, 8);
    }
    // morte
    const dead = !h.alive && !this.gs.over;
    this.deadTxt.setVisible(dead);
    this.deadSub.setVisible(dead);
    if (dead) this.deadTxt.setText(`Renascendo em ${Math.max(0, Math.ceil((h.deadUntil - now) / 1000))}`);
  }
}
