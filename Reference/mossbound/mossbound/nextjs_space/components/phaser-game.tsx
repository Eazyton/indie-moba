'use client';

import { useEffect, useRef } from 'react';

interface GameHandle { destroy: (removeCanvas: boolean) => void }

export default function PhaserGame() {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    let game: GameHandle | null = null;
    let cancelled = false;
    import('@/src/game/main')
      .then((m: { createGame: (el: HTMLElement) => GameHandle }) => {
        if (cancelled || !ref?.current) return;
        game = m?.createGame?.(ref.current) ?? null;
        (window as unknown as { __MOSSBOUND__?: GameHandle | null }).__MOSSBOUND__ = game;
      })
      .catch((e: unknown) => console.error('Falha ao carregar o jogo', e));
    return () => {
      cancelled = true;
      game?.destroy?.(true);
      game = null;
    };
  }, []);

  return <div ref={ref} className="h-full w-full" onContextMenu={(e) => e.preventDefault()} />;
}
