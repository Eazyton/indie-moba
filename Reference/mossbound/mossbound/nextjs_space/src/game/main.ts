import Phaser from 'phaser';
import { VIEW } from './config/balance';
import { BootScene } from './scenes/BootScene';
import { MenuScene } from './scenes/MenuScene';
import { GameScene } from './scenes/GameScene';
import { UIScene } from './scenes/UIScene';

export function createGame(parent: HTMLElement): Phaser.Game {
  return new Phaser.Game({
    type: Phaser.AUTO,
    parent,
    width: VIEW.w,
    height: VIEW.h,
    backgroundColor: '#0f1f18',
    pixelArt: true,
    roundPixels: true,
    disableContextMenu: true,
    banner: false,
    fps: { target: 60 },
    scale: { mode: Phaser.Scale.FIT, autoCenter: Phaser.Scale.CENTER_BOTH },
    scene: [BootScene, MenuScene, GameScene, UIScene],
  });
}
