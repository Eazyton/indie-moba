// Sons sintéticos via Web Audio API (nenhum arquivo externo)
type Wave = OscillatorType;

class SoundManagerImpl {
  ctx: AudioContext | null = null;
  master: GainNode | null = null;
  sfx: GainNode | null = null;
  music: GainNode | null = null;
  volume = 0.6;
  muted = false;
  private noiseBuf: AudioBuffer | null = null;
  private last: Record<string, number> = {};
  private musicTimer: number | null = null;
  private nextNote = 0;
  private step = 0;

  init(): void {
    try {
      if (!this.ctx) {
        const AC = (window.AudioContext ?? (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext);
        if (!AC) return;
        this.ctx = new AC();
        this.master = this.ctx.createGain();
        this.master.gain.value = this.muted ? 0 : this.volume;
        this.master.connect(this.ctx.destination);
        this.sfx = this.ctx.createGain();
        this.sfx.gain.value = 0.8;
        this.sfx.connect(this.master);
        this.music = this.ctx.createGain();
        this.music.gain.value = 0.35;
        this.music.connect(this.master);
        const len = this.ctx.sampleRate;
        this.noiseBuf = this.ctx.createBuffer(1, len, this.ctx.sampleRate);
        const d = this.noiseBuf.getChannelData(0);
        for (let i = 0; i < len; i++) d[i] = Math.random() * 2 - 1;
      }
      if (this.ctx.state === 'suspended') void this.ctx.resume();
    } catch (e) {
      console.error('Falha ao iniciar áudio', e);
    }
  }

  setVolume(v: number): void {
    this.volume = Math.max(0, Math.min(1, v));
    if (this.master && this.ctx) this.master.gain.setTargetAtTime(this.muted ? 0 : this.volume, this.ctx.currentTime, 0.05);
  }

  toggleMute(): boolean {
    this.muted = !this.muted;
    this.setVolume(this.volume);
    return this.muted;
  }

  private tone(freq: number, dur: number, type: Wave, vol: number, slideTo?: number, delay = 0, dest?: AudioNode | null): void {
    const ctx = this.ctx;
    const out = dest ?? this.sfx;
    if (!ctx || !out) return;
    const t = ctx.currentTime + delay;
    const o = ctx.createOscillator();
    const g = ctx.createGain();
    o.type = type;
    o.frequency.setValueAtTime(freq, t);
    if (slideTo) o.frequency.exponentialRampToValueAtTime(Math.max(20, slideTo), t + dur);
    g.gain.setValueAtTime(0.0001, t);
    g.gain.exponentialRampToValueAtTime(vol, t + Math.min(0.02, dur * 0.2));
    g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
    o.connect(g);
    g.connect(out);
    o.start(t);
    o.stop(t + dur + 0.05);
  }

  private noise(dur: number, vol: number, freq: number, type: BiquadFilterType, slideTo?: number, delay = 0, q = 1): void {
    const ctx = this.ctx;
    if (!ctx || !this.sfx || !this.noiseBuf) return;
    const t = ctx.currentTime + delay;
    const src = ctx.createBufferSource();
    src.buffer = this.noiseBuf;
    src.loop = true;
    const f = ctx.createBiquadFilter();
    f.type = type;
    f.Q.value = q;
    f.frequency.setValueAtTime(freq, t);
    if (slideTo) f.frequency.exponentialRampToValueAtTime(slideTo, t + dur);
    const g = ctx.createGain();
    g.gain.setValueAtTime(vol, t);
    g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
    src.connect(f);
    f.connect(g);
    g.connect(this.sfx);
    src.start(t, Math.random() * 0.5);
    src.stop(t + dur + 0.05);
  }

  play(name: string, gap = 45): void {
    if (!this.ctx || this.muted) return;
    const now = performance.now();
    if ((this.last[name] ?? 0) + gap > now) return;
    this.last[name] = now;
    switch (name) {
      case 'attack': this.noise(0.14, 0.3, 2400, 'bandpass', 500, 0, 2); this.tone(260, 0.08, 'triangle', 0.08, 140); break;
      case 'hit': this.tone(170, 0.1, 'sine', 0.3, 60); this.noise(0.06, 0.18, 1400, 'lowpass'); break;
      case 'q': this.tone(380, 0.2, 'sawtooth', 0.1, 1300); this.tone(1200, 0.3, 'sine', 0.08, 1900, 0.05); this.noise(0.2, 0.08, 3000, 'highpass'); break;
      case 'shield': this.tone(110, 0.3, 'triangle', 0.3, 90); this.tone(660, 0.5, 'sine', 0.07); this.tone(990, 0.6, 'sine', 0.05, undefined, 0.08); break;
      case 'dash': this.noise(0.28, 0.3, 400, 'bandpass', 3200, 0, 1.5); for (let i = 0; i < 4; i++) this.noise(0.03, 0.12, 5000, 'highpass', undefined, 0.05 + i * 0.05); break;
      case 'rwarn': this.tone(70, 1.0, 'sawtooth', 0.09, 110); this.noise(1.0, 0.1, 300, 'lowpass', 900); break;
      case 'rboom': this.noise(0.9, 0.55, 900, 'lowpass', 90); this.tone(95, 0.7, 'sine', 0.5, 30); this.tone(520, 0.3, 'triangle', 0.06, 200); break;
      case 'tower': this.tone(880, 0.14, 'square', 0.05, 440); this.tone(1400, 0.25, 'sine', 0.05, 1100); break;
      case 'orb': this.tone(480, 0.2, 'sine', 0.1, 900); this.tone(240, 0.2, 'triangle', 0.05, 300); break;
      case 'bolt': this.tone(300, 0.25, 'sawtooth', 0.08, 1000); this.tone(150, 0.3, 'sine', 0.1, 500); break;
      case 'pool': this.tone(200, 0.8, 'sine', 0.12, 120); this.noise(0.8, 0.06, 600, 'bandpass', 200, 0, 3); break;
      case 'blink': this.tone(900, 0.2, 'sine', 0.1, 300); this.noise(0.15, 0.1, 4000, 'highpass'); break;
      case 'arrow': this.noise(0.07, 0.1, 3500, 'highpass'); break;
      case 'sword': this.noise(0.07, 0.1, 1600, 'bandpass', 800, 0, 3); break;
      case 'death': this.tone(420, 0.7, 'triangle', 0.22, 70); this.tone(210, 0.8, 'sine', 0.15, 50, 0.1); break;
      case 'mdeath': this.tone(520, 0.14, 'sine', 0.06, 200); break;
      case 'levelup': [587, 740, 880, 1175].forEach((f: number, i: number) => this.tone(f, 0.25, 'sine', 0.12, undefined, i * 0.08)); break;
      case 'gold': this.tone(1320, 0.08, 'sine', 0.07); this.tone(1760, 0.12, 'sine', 0.07, undefined, 0.06); break;
      case 'structure': this.noise(1.6, 0.6, 1200, 'lowpass', 60); this.tone(80, 1.4, 'sine', 0.5, 25); this.tone(160, 0.6, 'square', 0.05, 40); break;
      case 'victory': [523, 659, 784, 1047, 784, 1047].forEach((f: number, i: number) => { this.tone(f, 0.4, 'triangle', 0.14, undefined, i * 0.16); this.tone(f / 2, 0.4, 'sine', 0.08, undefined, i * 0.16); }); break;
      case 'defeat': [392, 349, 311, 262].forEach((f: number, i: number) => this.tone(f, 0.6, 'triangle', 0.14, undefined, i * 0.3)); break;
      case 'click': this.tone(720, 0.05, 'square', 0.05); break;
      case 'deny': this.tone(150, 0.14, 'square', 0.06, 110); break;
      case 'recall': [660, 880, 990, 1320].forEach((f: number, i: number) => this.tone(f, 0.5, 'sine', 0.05, undefined, i * 0.18)); break;
      case 'respawn': [440, 554, 659, 880].forEach((f: number, i: number) => this.tone(f, 0.3, 'triangle', 0.09, undefined, i * 0.07)); break;
      case 'buy': this.tone(990, 0.08, 'square', 0.04); this.tone(1480, 0.18, 'sine', 0.08, undefined, 0.06); break;
      case 'wave': this.tone(196, 0.5, 'triangle', 0.1); this.tone(294, 0.6, 'triangle', 0.08, undefined, 0.15); break;
      default: break;
    }
  }

  startMusic(): void {
    if (!this.ctx || this.musicTimer !== null) return;
    this.nextNote = this.ctx.currentTime + 0.1;
    this.step = 0;
    this.musicTimer = window.setInterval(() => this.schedule(), 120);
  }

  stopMusic(): void {
    if (this.musicTimer !== null) window.clearInterval(this.musicTimer);
    this.musicTimer = null;
  }

  private schedule(): void {
    const ctx = this.ctx;
    if (!ctx || !this.music) return;
    const eighth = 60 / 80 / 2;
    const chords = [[146.8, 174.6, 220], [116.5, 146.8, 174.6], [174.6, 220, 261.6], [130.8, 164.8, 196]];
    const scale = [293.7, 349.2, 392, 440, 523.3, 587.3, 698.5];
    while (this.nextNote < ctx.currentTime + 0.4) {
      const bar = Math.floor(this.step / 8) % 4;
      const chord = chords[bar] ?? chords[0] ?? [];
      const delay = this.nextNote - ctx.currentTime;
      if (this.step % 8 === 0) {
        chord.forEach((f: number) => this.tone(f, eighth * 8.5, 'triangle', 0.035, undefined, delay, this.music));
        this.tone((chord[0] ?? 146.8) / 2, eighth * 4, 'sine', 0.08, undefined, delay, this.music);
      }
      if (this.step % 8 === 4) this.tone((chord[0] ?? 146.8) / 2, eighth * 4, 'sine', 0.06, undefined, delay, this.music);
      const seed = Math.sin(this.step * 12.9898) * 43758.5453;
      const rnd = seed - Math.floor(seed);
      if (rnd < 0.5) {
        const f = scale[Math.floor(rnd * 2 * scale.length) % scale.length] ?? 440;
        this.tone(f, 0.45, 'sine', 0.045, undefined, delay, this.music);
        this.tone(f * 2, 0.3, 'sine', 0.012, undefined, delay + eighth * 3, this.music);
      }
      this.nextNote += eighth;
      this.step++;
    }
  }
}

export const Sound = new SoundManagerImpl();
