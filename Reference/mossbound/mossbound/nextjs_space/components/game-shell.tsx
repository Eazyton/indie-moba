'use client';

import dynamic from 'next/dynamic';
import { useRef } from 'react';
import { motion } from 'framer-motion';
import { Leaf, Maximize2, Gamepad2 } from 'lucide-react';

const PhaserGame = dynamic(() => import('@/components/phaser-game'), {
  ssr: false,
  loading: () => (
    <div className="flex h-full w-full items-center justify-center text-primary" style={{ fontFamily: '"Pixelify Sans", monospace' }}>
      Carregando a floresta...
    </div>
  ),
});

export default function GameShell() {
  const wrap = useRef<HTMLDivElement>(null);

  const fullscreen = () => {
    try {
      const el = wrap?.current;
      if (!el) return;
      if (document.fullscreenElement) void document.exitFullscreen?.();
      else void el.requestFullscreen?.();
    } catch (e) {
      console.error('Tela cheia indisponível', e);
    }
  };

  return (
    <div className="flex h-[100dvh] w-full flex-col bg-background text-foreground">
      <header className="sticky top-0 z-10 w-full border-b border-primary/20 bg-background/70 backdrop-blur">
        <motion.div
          initial={{ opacity: 0, y: -8 }}
          animate={{ opacity: 1, y: 0 }}
          className="mx-auto flex h-11 max-w-[1200px] items-center justify-between px-4"
          style={{ fontFamily: '"Pixelify Sans", monospace' }}
        >
          <div className="flex items-center gap-2 text-lg font-bold tracking-wider text-primary">
            <Leaf className="h-5 w-5" aria-hidden />
            MOSSBOUND
            <span className="ml-2 hidden rounded bg-card px-2 py-0.5 text-xs font-normal text-foreground/80 sm:inline">
              <Gamepad2 className="mr-1 inline h-3 w-3" aria-hidden />
              Demo jogável
            </span>
          </div>
          <button
            type="button"
            onClick={fullscreen}
            className="flex items-center gap-1.5 rounded-md bg-primary px-3 py-1 text-sm font-semibold text-primary-foreground shadow transition hover:brightness-110 active:scale-95"
          >
            <Maximize2 className="h-4 w-4" aria-hidden />
            Tela cheia
          </button>
        </motion.div>
      </header>
      <main ref={wrap} className="relative min-h-0 flex-1 bg-[#0b1510]">
        <PhaserGame />
      </main>
    </div>
  );
}
