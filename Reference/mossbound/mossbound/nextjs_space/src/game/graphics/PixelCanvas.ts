/** Pequena tela de pixel art: desenha pixel a pixel em um canvas 2D */
export class PixelCanvas {
  canvas: HTMLCanvasElement;
  ctx: CanvasRenderingContext2D;
  w: number;
  h: number;
  ox = 0;
  oy = 0;

  constructor(w: number, h: number) {
    this.w = w;
    this.h = h;
    this.canvas = document.createElement('canvas');
    this.canvas.width = w;
    this.canvas.height = h;
    const ctx = this.canvas.getContext('2d', { willReadFrequently: true });
    if (!ctx) throw new Error('Canvas 2D indisponível');
    this.ctx = ctx;
    this.ctx.imageSmoothingEnabled = false;
  }

  at(ox: number, oy: number): this {
    this.ox = ox;
    this.oy = oy;
    return this;
  }

  px(x: number, y: number, c: string): void {
    this.ctx.fillStyle = c;
    this.ctx.fillRect(Math.round(x + this.ox), Math.round(y + this.oy), 1, 1);
  }

  rect(x: number, y: number, w: number, h: number, c: string): void {
    if (w <= 0 || h <= 0) return;
    this.ctx.fillStyle = c;
    this.ctx.fillRect(Math.round(x + this.ox), Math.round(y + this.oy), Math.round(w), Math.round(h));
  }

  line(x0: number, y0: number, x1: number, y1: number, c: string): void {
    x0 = Math.round(x0); y0 = Math.round(y0); x1 = Math.round(x1); y1 = Math.round(y1);
    const dx = Math.abs(x1 - x0);
    const dy = -Math.abs(y1 - y0);
    const sx = x0 < x1 ? 1 : -1;
    const sy = y0 < y1 ? 1 : -1;
    let err = dx + dy;
    let guard = 0;
    this.ctx.fillStyle = c;
    while (guard++ < 500) {
      this.ctx.fillRect(x0 + this.ox, y0 + this.oy, 1, 1);
      if (x0 === x1 && y0 === y1) break;
      const e2 = 2 * err;
      if (e2 >= dy) { err += dy; x0 += sx; }
      if (e2 <= dx) { err += dx; y0 += sy; }
    }
  }

  disc(cx: number, cy: number, r: number, c: string): void {
    this.ellipse(cx, cy, r, r, c);
  }

  ellipse(cx: number, cy: number, rx: number, ry: number, c: string): void {
    this.ctx.fillStyle = c;
    const rxx = rx + 0.4;
    const ryy = ry + 0.4;
    for (let y = -Math.ceil(ry); y <= Math.ceil(ry); y++) {
      const t = 1 - (y * y) / (ryy * ryy);
      if (t < 0) continue;
      const half = Math.floor(rxx * Math.sqrt(t));
      this.ctx.fillRect(Math.round(cx - half + this.ox), Math.round(cy + y + this.oy), half * 2 + 1, 1);
    }
  }

  /** blob sombreado: escuro, médio, claro e brilho (luz vinda de cima-esquerda) */
  blob(cx: number, cy: number, r: number, cols: string[], rnd: () => number): void {
    this.disc(cx, cy, r, cols[0] ?? '#000');
    this.disc(cx - 1, cy - 1, Math.max(1, r - 1), cols[1] ?? '#333');
    this.disc(cx - Math.ceil(r * 0.3), cy - Math.ceil(r * 0.35), Math.max(1, Math.floor(r * 0.55)), cols[2] ?? '#666');
    const n = Math.floor(r * 0.9);
    for (let i = 0; i < n; i++) {
      const a = rnd() * Math.PI * 2;
      const d = rnd() * r * 0.7;
      const x = cx - r * 0.25 + Math.cos(a) * d * 0.7;
      const y = cy - r * 0.3 + Math.sin(a) * d * 0.6;
      this.px(x, y, cols[3] ?? '#fff');
    }
    for (let i = 0; i < n; i++) {
      const a = rnd() * Math.PI * 2;
      const x = cx + Math.cos(a) * (r - 1);
      const y = cy + Math.sin(a) * (r - 1);
      if (y > cy) this.px(x, y, cols[0] ?? '#000');
    }
  }

  /** contorno de 1px ao redor dos pixels opacos da região */
  outline(x: number, y: number, w: number, h: number, color: string): void {
    const img = this.ctx.getImageData(x, y, w, h);
    const d = img.data;
    const mask = new Uint8Array(w * h);
    for (let i = 0; i < w * h; i++) mask[i] = (d[i * 4 + 3] ?? 0) > 110 ? 1 : 0;
    const r = parseInt(color.slice(1, 3), 16);
    const g = parseInt(color.slice(3, 5), 16);
    const b = parseInt(color.slice(5, 7), 16);
    for (let yy = 0; yy < h; yy++) {
      for (let xx = 0; xx < w; xx++) {
        const i = yy * w + xx;
        if (mask[i]) continue;
        const n = (xx > 0 && mask[i - 1]) || (xx < w - 1 && mask[i + 1]) || (yy > 0 && mask[i - w]) || (yy < h - 1 && mask[i + w]);
        if (n) {
          d[i * 4] = r; d[i * 4 + 1] = g; d[i * 4 + 2] = b; d[i * 4 + 3] = 255;
        }
      }
    }
    this.ctx.putImageData(img, x, y);
  }

  /** copia uma região girada em múltiplos de 90° (sem suavização) */
  drawRotated(src: HTMLCanvasElement, sx: number, sy: number, sw: number, sh: number, dx: number, dy: number, pivotX: number, pivotY: number, angle: number, alpha = 1): void {
    this.ctx.save();
    this.ctx.globalAlpha = alpha;
    this.ctx.translate(dx, dy);
    this.ctx.rotate(angle);
    this.ctx.drawImage(src, sx, sy, sw, sh, -pivotX, -pivotY, sw, sh);
    this.ctx.restore();
  }

  alpha(a: number): void {
    this.ctx.globalAlpha = a;
  }
}

export function shade(hex: string, amt: number): string {
  const n = parseInt(hex.slice(1), 16);
  const f = (v: number): number => Math.max(0, Math.min(255, Math.round(amt >= 0 ? v + (255 - v) * amt : v * (1 + amt))));
  const r = f((n >> 16) & 255);
  const g = f((n >> 8) & 255);
  const b = f(n & 255);
  return '#' + ((1 << 24) | (r << 16) | (g << 8) | b).toString(16).slice(1);
}
