import Phaser from 'phaser';
import { MAP, TEAM_COLOR, VIEW } from '../config/balance';
import type { GameScene } from '../scenes/GameScene';
import type { UIScene } from '../scenes/UIScene';
import { panel } from './Widgets';

const SIZE = 150;

/** Minimapa 150x150 no canto inferior direito (clique para mover o Nilo) */
export class Minimap {
  gs: GameScene;
  x: number;
  y: number;
  g: Phaser.GameObjects.Graphics;
  rect: Phaser.Geom.Rectangle;
  acc = 0;

  constructor(ui: UIScene, gs: GameScene) {
    this.gs = gs;
    this.x = VIEW.w - SIZE - 20;
    this.y = VIEW.h - SIZE - 20;
    this.rect = new Phaser.Geom.Rectangle(this.x - 8, this.y - 8, SIZE + 16, SIZE + 16);
    panel(ui, this.x - 8, this.y - 8, SIZE + 16, SIZE + 16, 'ui-wood');
    const bg = ui.add.image(this.x, this.y, 'minimap-bg').setOrigin(0).setInteractive({ useHandCursor: true });
    ui.add.rectangle(this.x, this.y, SIZE, SIZE).setOrigin(0).setStrokeStyle(1, 0x1b1410);
    this.g = ui.add.graphics();
    bg.on('pointerdown', (p: Phaser.Input.Pointer) => {
      const s = MAP.size / SIZE;
      gs.setMoveTarget((p.x - this.x) * s, (p.y - this.y) * s, true);
    });
  }

  contains(x: number, y: number): boolean {
    return this.rect.contains(x, y);
  }

  update(delta: number): void {
    this.acc += delta;
    if (this.acc < 60) return;
    this.acc = 0;
    const g = this.g;
    const s = SIZE / MAP.size;
    const ox = this.x;
    const oy = this.y;
    g.clear();
    const t = this.gs.now;
    for (const u of this.gs.units ?? []) {
      if (!u) continue;
      const x = ox + u.x * s;
      const y = oy + u.y * s;
      const col = TEAM_COLOR[u.team];
      if (u.kind === 'minion') {
        if (!u.alive) continue;
        g.fillStyle(col, 1).fillRect(Math.round(x) - 1, Math.round(y) - 1, 2, 2);
      } else if (u.kind === 'tower') {
        g.fillStyle(0x1b1410, 1).fillRect(x - 5, y - 5, 10, 10);
        g.fillStyle(u.alive ? col : 0x55504a, 1).fillRect(x - 4, y - 4, 8, 8);
      } else if (u.kind === 'nexus') {
        g.fillStyle(0x1b1410, 1).fillCircle(x, y, 7);
        g.fillStyle(u.alive ? col : 0x55504a, 1).fillCircle(x, y, 6);
        g.fillStyle(0xffffff, 0.6).fillCircle(x - 1, y - 2, 2);
      }
    }
    for (const hr of [this.gs.bot, this.gs.hero]) {
      if (!hr?.alive) continue;
      const x = ox + hr.x * s;
      const y = oy + hr.y * s;
      if (hr.isPlayer) {
        g.fillStyle(0xffe07a, 0.35 + Math.sin(t * 0.008) * 0.2).fillCircle(x, y, 8);
      }
      g.fillStyle(0x1b1410, 1).fillCircle(x, y, 5);
      g.fillStyle(hr.isPlayer ? 0xf5e6c8 : TEAM_COLOR[hr.team], 1).fillCircle(x, y, 4);
      g.fillStyle(TEAM_COLOR[hr.team], 1).fillCircle(x, y, 2);
    }
    const v = this.gs.cameras?.main?.worldView;
    if (v) g.lineStyle(1, 0xf5e6c8, 0.9).strokeRect(ox + v.x * s, oy + v.y * s, v.width * s, v.height * s);
  }
}
