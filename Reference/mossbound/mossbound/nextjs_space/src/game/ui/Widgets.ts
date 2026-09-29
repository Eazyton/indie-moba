import Phaser from 'phaser';
import { FONT } from '../config/balance';
import { Sound } from '../audio/SoundManager';

export const TXT = (size: number, color = '#f5e6c8', extra: Phaser.Types.GameObjects.Text.TextStyle = {}): Phaser.Types.GameObjects.Text.TextStyle => ({
  fontFamily: FONT, fontSize: `${size}px`, color, stroke: '#1b1410', strokeThickness: Math.max(2, Math.round(size / 5)), ...extra,
});

/** Gera (uma vez) uma textura 9-slice com bordas e centro LADRILHADOS, preservando os pixels */
function panelTexture(scene: Phaser.Scene, key: string, w: number, h: number, c: number): string {
  const W = Math.max(c * 2 + 1, Math.round(w));
  const H = Math.max(c * 2 + 1, Math.round(h));
  const k = `${key}-${W}x${H}-${c}`;
  if (scene.textures.exists(k)) return k;
  const src = scene.textures.get(key)?.getSourceImage() as HTMLCanvasElement | HTMLImageElement | undefined;
  const canvas = document.createElement('canvas');
  canvas.width = W;
  canvas.height = H;
  const ctx = canvas.getContext('2d');
  if (!ctx || !src) return key;
  ctx.imageSmoothingEnabled = false;
  const sw = src.width;
  const sh = src.height;
  const iw = sw - c * 2;
  const ih = sh - c * 2;
  const tile = (sx: number, sy: number, sW: number, sH: number, dx: number, dy: number, dW: number, dH: number): void => {
    for (let yy = 0; yy < dH; yy += sH) {
      for (let xx = 0; xx < dW; xx += sW) {
        const cw = Math.min(sW, dW - xx);
        const ch = Math.min(sH, dH - yy);
        ctx.drawImage(src, sx, sy, cw, ch, dx + xx, dy + yy, cw, ch);
      }
    }
  };
  tile(c, c, iw, ih, c, c, W - c * 2, H - c * 2);
  tile(c, 0, iw, c, c, 0, W - c * 2, c);
  tile(c, sh - c, iw, c, c, H - c, W - c * 2, c);
  tile(0, c, c, ih, 0, c, c, H - c * 2);
  tile(sw - c, c, c, ih, W - c, c, c, H - c * 2);
  ctx.drawImage(src, 0, 0, c, c, 0, 0, c, c);
  ctx.drawImage(src, sw - c, 0, c, c, W - c, 0, c, c);
  ctx.drawImage(src, 0, sh - c, c, c, 0, H - c, c, c);
  ctx.drawImage(src, sw - c, sh - c, c, c, W - c, H - c, c, c);
  scene.textures.addCanvas(k, canvas);
  return k;
}

/** Painel com as texturas de madeira / pergaminho / pedra */
export function panel(scene: Phaser.Scene, x: number, y: number, w: number, h: number, key = 'ui-wood', corner = 8): Phaser.GameObjects.Image {
  return scene.add.image(x, y, panelTexture(scene, key, w, h, corner)).setOrigin(0, 0);
}

export interface Button {
  root: Phaser.GameObjects.Container;
  label: Phaser.GameObjects.Text | null;
  hit: Phaser.GameObjects.Image;
  setEnabled: (on: boolean) => void;
  setLabel: (t: string) => void;
}

/** Botão de madeira com hover, clique e som */
export function button(
  scene: Phaser.Scene, x: number, y: number, w: number, h: number, text: string, onClick: () => void,
  opts: { size?: number; color?: string; icon?: string; iconScale?: number } = {},
): Button {
  const root = scene.add.container(x, y);
  const bg = panel(scene, 0, 0, w, h, 'ui-btn').setOrigin(0.5);
  const hi = panel(scene, 0, 0, w, h, 'ui-btn-hi').setOrigin(0.5).setVisible(false);
  root.add([bg, hi]);
  let label: Phaser.GameObjects.Text | null = null;
  if (opts.icon) {
    const ic = scene.add.image(text ? -w / 2 + 20 : 0, 0, opts.icon).setScale(opts.iconScale ?? 1.5);
    root.add(ic);
  }
  if (text || !opts.icon) {
    label = scene.add.text(opts.icon ? 10 : 0, -1, text, TXT(opts.size ?? 18, opts.color ?? '#f5e6c8')).setOrigin(0.5);
    root.add(label);
  }
  let enabled = true;
  bg.setInteractive({ useHandCursor: true });
  bg.on('pointerover', () => {
    if (!enabled) return;
    hi.setVisible(true);
    scene.tweens.add({ targets: root, scale: 1.05, duration: 90 });
  });
  bg.on('pointerout', () => {
    hi.setVisible(false);
    scene.tweens.add({ targets: root, scale: 1, duration: 90 });
  });
  bg.on('pointerdown', (p: Phaser.Input.Pointer) => {
    if (p.rightButtonDown()) return;
    if (!enabled) { Sound.play('deny', 200); return; }
    root.setScale(0.95);
    scene.tweens.add({ targets: root, scale: 1.05, duration: 120 });
    Sound.play('click');
    onClick();
  });
  return {
    root, label, hit: bg,
    setEnabled: (on: boolean) => { enabled = on; root.setAlpha(on ? 1 : 0.55); if (!on) hi.setVisible(false); },
    setLabel: (t: string) => { label?.setText(t); },
  };
}

/** Pip de volume: 5 barrinhas */
export function drawVolume(g: Phaser.GameObjects.Graphics, x: number, y: number, vol: number, muted: boolean): void {
  g.clear();
  const n = Math.round(vol * 5);
  for (let i = 0; i < 5; i++) {
    const h = 6 + i * 3;
    g.fillStyle(0x1b1410, 1).fillRect(x + i * 9 - 1, y - h - 1, 8, h + 2);
    g.fillStyle(!muted && i < n ? 0xffb347 : 0x4a3a2a, 1).fillRect(x + i * 9, y - h, 6, h);
  }
}

export function fmtTime(ms: number): string {
  const s = Math.max(0, Math.floor(ms / 1000));
  return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`;
}
