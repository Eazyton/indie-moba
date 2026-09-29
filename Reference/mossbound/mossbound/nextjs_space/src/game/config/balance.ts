// Todos os números de balanceamento do Mossbound
export type Team = 0 | 1;
export const BLUE: Team = 0;
export const RED: Team = 1;

export const VIEW = { w: 1280, h: 720 };
export const FONT = '"Pixelify Sans", "Courier New", monospace';
export const MAP = { size: 2400, tile: 32 };

export const PAL = {
  moss: '#4a7c59',
  mossDark: '#2d5a3d',
  petrol: '#1a4a5c',
  cream: '#f5e6c8',
  terracotta: '#c17a4a',
  amber: '#ffb347',
  blue: '#00c8b4',
  red: '#ff6b6b',
  ink: '#1b1410',
};

export const TEAM_COLOR: Record<Team, number> = { 0: 0x00c8b4, 1: 0xff6b6b };
export const TEAM_HEX: Record<Team, string> = { 0: '#00c8b4', 1: '#ff6b6b' };

export const POS = {
  laneY: 1200,
  blueBase: { x: 250, y: 1200 },
  redBase: { x: 2150, y: 1200 },
  blueSpawn: { x: 170, y: 1200 },
  redSpawn: { x: 2230, y: 1200 },
  blueNexus: { x: 330, y: 1200 },
  redNexus: { x: 2070, y: 1200 },
  blueTower: { x: 760, y: 1200 },
  redTower: { x: 1640, y: 1200 },
  baseRadius: 250,
};

export const HERO = {
  baseHp: 640,
  hpPerLevel: 95,
  baseDamage: 58,
  damagePerLevel: 10,
  speed: 185,
  attackRange: 150,
  attackCd: 800,
  attackArc: 0.75, // radianos (meio ângulo)
  respawn: 8000,
  baseRegen: 50,
  radius: 12,
  recall: 3000,
};

export const ABILITY = {
  Q: { cd: 8000, range: 400, damage: 80, speed: 640, width: 22 },
  W: { cd: 15000, shield: 200, duration: 4000 },
  E: { cd: 10000, distance: 200, duration: 170 },
  R: { cd: 60000, radius: 200, delay: 1000, damage: 150, slow: 0.5, slowDuration: 2200, unlockLevel: 3, castRange: 650 },
};

export const VEX = {
  baseHp: 600,
  hpPerLevel: 90,
  baseDamage: 48,
  damagePerLevel: 9,
  speed: 165,
  attackRange: 300,
  attackCd: 1150,
  projSpeed: 470,
  radius: 12,
  bolt: { cd: 7500, damage: 85, range: 450, speed: 560, width: 20 },
  pool: { cd: 14000, radius: 110, delay: 750, damage: 100, range: 480 },
  blink: { cd: 12000, distance: 210 },
  retreatAt: 0.3,
  returnAt: 0.92,
};

export const MINION = {
  melee: { hp: 300, damage: 40, range: 42, cd: 1000, speed: 82, radius: 9 },
  ranged: { hp: 200, damage: 30, range: 200, cd: 1300, speed: 82, radius: 8, projSpeed: 380 },
  structureMult: 0.6,
  aggroRange: 250,
  waveInterval: 30000,
  firstWave: 4000,
  pattern: ['melee', 'ranged', 'melee', 'ranged', 'melee'] as const,
  spawnGap: 700,
};

export const TOWER = {
  hp: 3000,
  range: 250,
  heroDamage: 145,
  minionDamage: 115,
  cd: 1100,
  projSpeed: 430,
  radius: 26,
  warnRange: 420,
};

export const NEXUS = { hp: 5000, radius: 42 };

export const REWARDS = {
  minion: { xp: 50, gold: 25 },
  hero: { xp: 200, gold: 100 },
  structure: { xp: 500, gold: 200 },
  shareRange: 700,
};

export const LEVEL_XP = [0, 200, 500, 900, 1400, 2000];
export const MAX_LEVEL = 6;

export type ItemId = 'amber' | 'bark' | 'tide';
export interface ShopItem { id: ItemId; name: string; desc: string; cost: number; max: number; icon: string }
export const SHOP: ShopItem[] = [
  { id: 'amber', name: 'Aço Âmbar', desc: '+30 de dano', cost: 300, max: 3, icon: 'item-amber' },
  { id: 'bark', name: 'Casca Reforçada', desc: '+200 de vida máxima', cost: 250, max: 3, icon: 'item-bark' },
  { id: 'tide', name: 'Pedra da Maré', desc: '-20% de recarga', cost: 400, max: 2, icon: 'item-tide' },
];

export const START_GOLD = 150;
