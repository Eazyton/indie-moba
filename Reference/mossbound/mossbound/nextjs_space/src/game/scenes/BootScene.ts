import Phaser from 'phaser';
import { VIEW } from '../config/balance';
import { buildCharacters } from '../graphics/SpriteFactory';
import { buildStructures } from '../graphics/StructureFactory';
import { buildDecor } from '../graphics/DecorFactory';
import { buildUI } from '../graphics/UIFactory';
import { renderGround } from '../graphics/TileRenderer';

/** Gera toda a pixel art proceduralmente antes do menu */
export class BootScene extends Phaser.Scene {
  constructor() {
    super('Boot');
  }

  create(): void {
    const cx = VIEW.w / 2;
    const cy = VIEW.h / 2;
    const title = this.add.text(cx, cy - 40, 'MOSSBOUND', { fontFamily: 'monospace', fontSize: '42px', color: '#ffb347', stroke: '#1b1410', strokeThickness: 6 }).setOrigin(0.5);
    const label = this.add.text(cx, cy + 40, 'Cultivando a floresta...', { fontFamily: 'monospace', fontSize: '16px', color: '#f5e6c8' }).setOrigin(0.5);
    const bar = this.add.graphics();
    const draw = (r: number): void => {
      bar.clear();
      bar.fillStyle(0x1b1410, 1).fillRect(cx - 162, cy + 6, 324, 16);
      bar.fillStyle(0x2d5a3d, 1).fillRect(cx - 160, cy + 8, 320, 12);
      bar.fillStyle(0xffb347, 1).fillRect(cx - 160, cy + 8, 320 * r, 12);
    };
    draw(0);
    const fontReady: Promise<unknown> = (typeof document !== 'undefined' && document.fonts?.load)
      ? Promise.all([document.fonts.load('400 16px "Pixelify Sans"'), document.fonts.load('700 16px "Pixelify Sans"')]).catch(() => null)
      : Promise.resolve(null);
    const steps: [string, () => void][] = [
      ['Esculpindo heróis...', () => buildCharacters(this)],
      ['Erguendo torres de cristal...', () => buildStructures(this)],
      ['Plantando árvores e cogumelos...', () => buildDecor(this)],
      ['Entalhando a interface...', () => buildUI(this)],
      ['Pintando o solo da floresta...', () => this.registry.set('decor', renderGround(this))],
    ];
    let i = 0;
    const next = (): void => {
      const s = steps[i];
      if (!s) {
        label.setText('Pronto!');
        const timeout = new Promise((res: (v: unknown) => void) => setTimeout(() => res(null), 2500));
        void Promise.race([fontReady, timeout]).then(() => {
          title.destroy();
          this.scene.start('Menu');
        });
        return;
      }
      label.setText(s[0]);
      this.time.delayedCall(30, () => {
        try {
          s[1]();
        } catch (e) {
          console.error('Erro ao gerar gráficos:', e);
        }
        i++;
        draw(i / steps.length);
        next();
      });
    };
    next();
  }
}
