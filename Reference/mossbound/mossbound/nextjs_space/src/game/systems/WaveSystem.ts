import { BLUE, MINION, POS, RED, Team } from '../config/balance';
import { Sound } from '../audio/SoundManager';
import { Minion, MinionType } from '../entities/Minion';
import type { GameScene } from '../scenes/GameScene';

interface Pending { at: number; team: Team; type: MinionType; offset: number }

/** Ondas de tropas a cada 30 segundos */
export class WaveSystem {
  scene: GameScene;
  nextWave = MINION.firstWave;
  queue: Pending[] = [];
  count = 0;

  constructor(scene: GameScene) {
    this.scene = scene;
  }

  timeToNext(): number {
    return Math.max(0, this.nextWave - this.scene.now);
  }

  update(): void {
    const now = this.scene.now;
    if (now >= this.nextWave) {
      this.spawnWave();
      this.nextWave += MINION.waveInterval;
    }
    if (this.queue.length === 0) return;
    const rest: Pending[] = [];
    for (const p of this.queue) {
      if (now < p.at) { rest.push(p); continue; }
      const x = p.team === BLUE ? POS.blueNexus.x + 70 : POS.redNexus.x - 70;
      const m = new Minion(this.scene, p.team, p.type, x, POS.laneY + p.offset, p.offset);
      this.scene.units.push(m);
      this.scene.combat?.burst(x, POS.laneY + p.offset - 10, 'px-leaf', 0xffffff, 6, 60);
    }
    this.queue = rest;
  }

  private spawnWave(): void {
    this.count++;
    const now = this.scene.now;
    MINION.pattern.forEach((type: MinionType, i: number) => {
      const offset = ((i % 3) - 1) * 24;
      for (const team of [BLUE, RED] as Team[]) this.queue.push({ at: now + i * MINION.spawnGap, team, type, offset });
    });
    if (this.count === 1) this.scene.notify('As tropas da floresta avançam!', '#ffe07a');
    else this.scene.notify(`Onda ${this.count} de tropas`, '#f5e6c8', 1400);
    Sound.play('wave');
  }
}
