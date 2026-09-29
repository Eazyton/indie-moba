import Phaser from 'phaser';
import { MAP, POS, VIEW, BLUE, RED } from '../config/balance';
import type { Decor } from '../world/MapLayout';
import { Ambience } from '../world/Ambience';
import { Sound } from '../audio/SoundManager';
import { TXT, button, panel } from '../ui/Widgets';

export const CONTROLS: [string, string][] = [
  ['Setas', 'Mover o Nilo'],
  ['Botão direito', 'Mover até o cursor (segure)'],
  ['Botão esquerdo', 'Ataque básico (segure)'],
  ['Q', 'Estilhaço Solar (projétil)'],
  ['W', 'Casca Viva (escudo)'],
  ['E', 'Passo de Brasa (dash)'],
  ['R', 'Jardim de Fogo (nível 3)'],
  ['B', 'Teleporte para a base (3s)'],
  ['P', 'Abrir / fechar a loja'],
  ['Esc', 'Pausar'],
];

/** Menu principal com a floresta viva ao fundo */
export class MenuScene extends Phaser.Scene {
  ambience: Ambience | null = null;
  help: Phaser.GameObjects.Container | null = null;

  constructor() {
    super('Menu');
  }

  create(): void {
    const cam = this.cameras.main;
    cam.setBounds(0, 0, MAP.size, MAP.size);
    this.add.image(0, 0, 'ground').setOrigin(0).setDepth(-1000);
    this.ambience = new Ambience(this, (this.registry.get('decor') as Decor[] | undefined) ?? []);
    // estruturas decorativas ao fundo
    for (const [t, tp, np] of [[BLUE, POS.blueTower, POS.blueNexus], [RED, POS.redTower, POS.redNexus]] as const) {
      this.add.image(tp.x, tp.y, `tower-${t}`).setOrigin(0.5, 80 / 86).setDepth(tp.y);
      this.add.image(tp.x, tp.y - 42, `tower-crystal-${t}`).setDepth(tp.y + 2);
      this.add.image(np.x, np.y, `nexus-${t}`).setOrigin(0.5, 114 / 118).setDepth(np.y);
      this.add.image(np.x, np.y - 27, `nexus-crystal-${t}`).setDepth(np.y + 2);
    }
    cam.centerOn(420, POS.laneY - 60);
    const pan = { x: 420 };
    this.tweens.add({ targets: pan, x: 1980, duration: 60000, yoyo: true, repeat: -1, ease: 'Sine.easeInOut', onUpdate: () => cam.centerOn(pan.x, POS.laneY - 60 + Math.sin(pan.x / 300) * 80) });
    cam.fadeIn(700, 15, 31, 24);

    const ui: Phaser.GameObjects.GameObject[] = [];
    const fix = <T extends Phaser.GameObjects.GameObject>(o: T): T => { ui.push(o); return o; };
    fix(this.add.rectangle(0, 0, VIEW.w, VIEW.h, 0x0f1f18, 0.35).setOrigin(0));
    fix(this.add.image(VIEW.w / 2, VIEW.h / 2, 'vignette').setDisplaySize(VIEW.w, VIEW.h).setAlpha(0.9));
    const halo = fix(this.add.image(VIEW.w / 2, 190, 'glow').setBlendMode(Phaser.BlendModes.ADD).setTint(0xffb347).setScale(11, 4).setAlpha(0.28));
    this.tweens.add({ targets: halo, alpha: 0.42, duration: 1800, yoyo: true, repeat: -1, ease: 'Sine.easeInOut' });

    // logo com letras flutuando
    const word = 'MOSSBOUND';
    const spacing = 70;
    const startX = VIEW.w / 2 - ((word.length - 1) * spacing) / 2;
    [...word].forEach((ch: string, i: number) => {
      const x = startX + i * spacing;
      const back = fix(this.add.text(x, 186, ch, TXT(96, '#7a3b1e', { strokeThickness: 14, fontStyle: 'bold' })).setOrigin(0.5));
      const front = fix(this.add.text(x, 178, ch, TXT(96, '#ffb347', { strokeThickness: 12, fontStyle: 'bold' })).setOrigin(0.5));
      front.setShadow(0, 0, '#ffd27a', 18, false, true);
      front.setAlpha(0).setY(140);
      back.setAlpha(0).setY(148);
      this.tweens.add({ targets: [front, back], alpha: 1, duration: 500, delay: 200 + i * 70 });
      this.tweens.add({ targets: front, y: 178, duration: 700, delay: 200 + i * 70, ease: 'Back.Out' });
      this.tweens.add({ targets: back, y: 186, duration: 700, delay: 200 + i * 70, ease: 'Back.Out' });
      this.tweens.add({ targets: [front, back], y: '-=6', duration: 1400, delay: 1100 + i * 110, yoyo: true, repeat: -1, ease: 'Sine.easeInOut' });
    });
    // musgo sob o logo
    const moss = fix(this.add.graphics());
    for (let i = 0; i < 90; i++) {
      const x = startX - 40 + Math.random() * (word.length * spacing + 10);
      const h = 2 + Math.random() * 10;
      moss.fillStyle([0x2d5a3d, 0x4a7c59, 0x6fa66a, 0x9bd07a][i % 4] ?? 0x4a7c59, 1).fillRect(Math.round(x), 226, 4, Math.round(h));
    }
    const sub = fix(this.add.text(VIEW.w / 2, 262, 'UM MOBA 2D DA FLORESTA ANCESTRAL', TXT(20, '#f5e6c8', { letterSpacing: 4 } as Phaser.Types.GameObjects.Text.TextStyle)).setOrigin(0.5).setAlpha(0));
    this.tweens.add({ targets: sub, alpha: 1, delay: 1000, duration: 800 });

    // heróis
    const nilo = fix(this.add.sprite(250, 590, 'nilo', 0).setOrigin(0.5, 53 / 56).setScale(4).play('nilo-idle'));
    const vex = fix(this.add.sprite(1030, 590, 'vex', 0).setOrigin(0.5, 53 / 56).setScale(4).setFlipX(true).play('vex-idle'));
    fix(this.add.image(250, 594, 'shadow').setScale(4.2, 4));
    fix(this.add.image(1030, 594, 'shadow').setScale(4.2, 4));
    fix(this.add.text(250, 620, 'NILO', TXT(26, '#00c8b4', { fontStyle: 'bold' })).setOrigin(0.5));
    fix(this.add.text(250, 648, 'Guardião da Brasa — você', TXT(14, '#f5e6c8')).setOrigin(0.5));
    fix(this.add.text(1030, 620, 'VEX', TXT(26, '#ff6b6b', { fontStyle: 'bold' })).setOrigin(0.5));
    fix(this.add.text(1030, 648, 'Tecelã de Sombras — IA', TXT(14, '#f5e6c8')).setOrigin(0.5));
    nilo.setX(120).setAlpha(0);
    vex.setX(1160).setAlpha(0);
    this.tweens.add({ targets: nilo, x: 250, alpha: 1, duration: 900, delay: 500, ease: 'Cubic.Out' });
    this.tweens.add({ targets: vex, x: 1030, alpha: 1, duration: 900, delay: 500, ease: 'Cubic.Out' });
    this.time.addEvent({ delay: 3200, loop: true, callback: () => { nilo.play('nilo-attack'); nilo.once('animationcomplete', () => nilo.play('nilo-idle')); } });
    this.time.addEvent({ delay: 4100, loop: true, callback: () => { vex.play('vex-attack'); vex.once('animationcomplete', () => vex.play('vex-idle')); } });

    // botões
    const play = button(this, VIEW.w / 2, 380, 280, 70, 'JOGAR', () => this.startGame(), { size: 32, color: '#ffe07a' });
    const how = button(this, VIEW.w / 2, 470, 280, 52, 'COMO JOGAR', () => this.toggleHelp(), { size: 20 });
    fix(play.root);
    fix(how.root);
    [play.root, how.root].forEach((b: Phaser.GameObjects.Container, i: number) => {
      b.setAlpha(0).setY(b.y + 30);
      this.tweens.add({ targets: b, alpha: 1, y: b.y - 30, delay: 1200 + i * 150, duration: 600, ease: 'Back.Out' });
    });
    this.tweens.add({ targets: play.root, scale: 1.04, duration: 900, yoyo: true, repeat: -1, delay: 2000, ease: 'Sine.easeInOut' });
    fix(this.add.text(VIEW.w / 2, 540, 'Destrua a torre e o núcleo de Vex antes que ela destrua os seus.', TXT(15, '#f5e6c8')).setOrigin(0.5));
    fix(this.add.text(VIEW.w / 2, 700, 'Enter ou Espaço também iniciam • Som sintético ativado ao clicar', TXT(12, '#bfae8a')).setOrigin(0.5));

    this.help = this.buildHelp();
    fix(this.help);
    ui.forEach((o: Phaser.GameObjects.GameObject) => (o as unknown as Phaser.GameObjects.Components.ScrollFactor).setScrollFactor?.(0));
    ui.forEach((o: Phaser.GameObjects.GameObject, i: number) => (o as unknown as Phaser.GameObjects.Components.Depth).setDepth?.(20000 + i));

    this.input.keyboard?.once('keydown-ENTER', () => this.startGame());
    this.input.keyboard?.once('keydown-SPACE', () => this.startGame());
    this.input.keyboard?.on('keydown-ESC', () => this.help?.setVisible(false));
  }

  private buildHelp(): Phaser.GameObjects.Container {
    const w = 560;
    const h = 470;
    const c = this.add.container(VIEW.w / 2 - w / 2, VIEW.h / 2 - h / 2 + 10).setVisible(false);
    const blocker = this.add.rectangle(-c.x, -c.y, VIEW.w, VIEW.h, 0x0b140f, 0.6).setOrigin(0).setInteractive();
    blocker.on('pointerdown', () => c.setVisible(false));
    c.add(blocker);
    c.add(panel(this, 0, 0, w, h, 'ui-parch', 6).setInteractive());
    c.add(this.add.text(w / 2, 34, 'COMO JOGAR', TXT(28, '#c17a4a', { stroke: '#3b2414', strokeThickness: 4 })).setOrigin(0.5));
    c.add(this.add.text(w / 2, 66, 'Avance com suas tropas, destrua a torre inimiga e depois o núcleo.', { fontFamily: TXT(12).fontFamily, fontSize: '13px', color: '#5a3a1e' }).setOrigin(0.5));
    CONTROLS.forEach(([k, d]: [string, string], i: number) => {
      const y = 100 + i * 30;
      c.add(panel(this, 40, y - 12, 150, 26, 'ui-stone', 6));
      c.add(this.add.text(115, y, k, TXT(14, '#ffe07a')).setOrigin(0.5));
      c.add(this.add.text(210, y, d, { fontFamily: TXT(12).fontFamily, fontSize: '16px', color: '#3b2414' }).setOrigin(0, 0.5));
    });
    c.add(this.add.text(w / 2, h - 38, 'Ouro: tropas 25 • herói 100 • estruturas 200. Gaste na loja!', { fontFamily: TXT(12).fontFamily, fontSize: '14px', color: '#7a4f2a' }).setOrigin(0.5));
    const close = button(this, w - 26, 26, 36, 36, 'X', () => c.setVisible(false), { size: 16 });
    c.add(close.root);
    return c;
  }

  private toggleHelp(): void {
    const h = this.help;
    if (!h) return;
    h.setVisible(!h.visible);
    if (h.visible) {
      h.setAlpha(0);
      this.tweens.add({ targets: h, alpha: 1, duration: 200 });
    }
  }

  private started = false;

  private startGame(): void {
    if (this.started) return;
    this.started = true;
    Sound.init();
    Sound.startMusic();
    Sound.play('respawn');
    this.cameras.main.fadeOut(450, 15, 31, 24);
    this.cameras.main.once('camerafadeoutcomplete', () => {
      this.started = false;
      this.scene.start('Game');
    });
  }

  update(time: number): void {
    this.ambience?.update(time, this.cameras.main);
  }
}
