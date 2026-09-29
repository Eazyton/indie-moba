// Decodes the headless-browser dump into PNG files inside the Unity project
// and writes the prototype layout JSON consumed by the one-shot scene builder.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '../..');
const art = path.join(root, 'IndieMoba/Assets/_Game/Art');

const TARGETS = {
  'nilo': 'Characters/_Placeholder/Nilo/Nilo_Sheet.png',
  'shadow': 'Characters/_Placeholder/Shared/CharacterShadow.png',
  'tree-0': 'Environment/_Placeholder/Trees/Tree_0.png',
  'tree-1': 'Environment/_Placeholder/Trees/Tree_1.png',
  'tree-2': 'Environment/_Placeholder/Trees/Tree_2.png',
  'bush-0': 'Environment/_Placeholder/Vegetation/Bush_0.png',
  'bush-1': 'Environment/_Placeholder/Vegetation/Bush_1.png',
  'mush-0': 'Environment/_Placeholder/Vegetation/Mushroom_0.png',
  'mush-1': 'Environment/_Placeholder/Vegetation/Mushroom_1.png',
  'flower-0': 'Environment/_Placeholder/Vegetation/Flower_0.png',
  'flower-1': 'Environment/_Placeholder/Vegetation/Flower_1.png',
  'flower-2': 'Environment/_Placeholder/Vegetation/Flower_2.png',
  'flower-3': 'Environment/_Placeholder/Vegetation/Flower_3.png',
  'rock-0': 'Environment/_Placeholder/Rocks/Rock_0.png',
  'rock-1': 'Environment/_Placeholder/Rocks/Rock_1.png',
  'rock-2': 'Environment/_Placeholder/Rocks/Rock_2.png',
  'pillar-0': 'Environment/_Placeholder/Ruins/Pillar_0.png',
  'pillar-1': 'Environment/_Placeholder/Ruins/Pillar_1.png',
  'ruin-wall': 'Environment/_Placeholder/Ruins/RuinWall.png',
  'crystal-deco': 'Environment/_Placeholder/Ruins/Crystal.png',
  'terrain': 'Environment/_Placeholder/Terrain/Terrain_PrototypeArea.png',
  'collision-cell': 'Environment/_Placeholder/Debug/CollisionCell.png',
};

const dump = fs.readFileSync(path.join(here, 'dist/dump.html'), 'utf8');
const m = dump.match(/<pre id="out">([\s\S]*?)<\/pre>/);
if (!m) throw new Error('export output not found in dump');
const json = m[1].replace(/&quot;/g, '"').replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&amp;/g, '&');
const data = JSON.parse(json);
if (data.error) throw new Error(data.error);

for (const [key, rel] of Object.entries(TARGETS)) {
  const t = data.textures[key];
  if (!t) throw new Error(`missing texture ${key}`);
  const file = path.join(art, rel);
  fs.mkdirSync(path.dirname(file), { recursive: true });
  fs.writeFileSync(file, Buffer.from(t.png.split(',')[1], 'base64'));
  console.log(`${rel}  ${t.w}x${t.h}`);
}

const outDir = path.join(root, 'IndieMoba/Assets/_Game/Data/Prototype');
fs.mkdirSync(outDir, { recursive: true });
const layout = { crop: data.crop, cellSize: data.cellSize, anims: data.anims, ...data.layout };
fs.writeFileSync(path.join(outDir, 'PrototypeLayout.json'), JSON.stringify(layout, null, 1));
console.log(`props: ${layout.props.length}, grid: ${layout.blocked[0].length}x${layout.blocked.length}`);
console.log('anims:', layout.anims.map((a) => `${a.key}[${a.frames.join(',')}]@${a.frameRate}`).join(' '));
