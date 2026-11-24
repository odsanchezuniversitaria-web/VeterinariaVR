// Keep a clip unless its only known references belong to explicitly disabled Unity scenes.
const fs = require('node:fs');
const path = require('node:path');
const {execFileSync} = require('node:child_process');
const root = path.resolve(process.argv[2], 'Unity');
const norm = p => p.replace(/\\/g, '/').toLowerCase();
const scan = args => {
  try {
    return execFileSync('rg', args, {cwd: root, encoding: 'utf8', maxBuffer: 32*1024*1024})
      .split(/\r?\n/).filter(Boolean);
  } catch (error) {
    if (error.status === 1) return [];
    throw error;
  }
};
const config = fs.readFileSync(path.join(root, 'ProjectSettings/EditorBuildSettings.asset'), 'utf8');
const disabled = new Set(Array.from(config.matchAll(/^\s*- enabled: 0\r?\n\s*path: (.+)$/gm),
  m => norm(m[1].trim())));
const aliases = new Map(), clips = [];
for (const meta of scan(['--files', '--no-ignore', '-g', '*.mp4.meta', 'Assets'])) {
  const guid = /^guid: ([a-f\d]{32})/m.exec(fs.readFileSync(path.join(root, meta), 'utf8'))?.[1];
  if (!guid) throw Error('GUID de vídeo ausente en '+meta);
  const name = path.basename(meta, '.meta').toLowerCase();
  const clip = {guid, name, disabled: false, used: false};
  clips.push(clip);
  for (const alias of [guid, name]) {
    if (!aliases.has(alias)) aliases.set(alias, []);
    aliases.get(alias).push(clip);
  }
}
if (!clips.length) throw Error('No se encontraron vídeos');
const args = ['-a', '-i', '-F', '-o', '--no-ignore', '--with-filename', '--no-heading', '--no-line-number', '--color', 'never'];
for (const ext of ['unity','prefab','asset','cs','js','jslib','json','playable','controller','overrideController','anim','txt','xml','shader','mat']) args.push('-g', '*.'+ext);
for (const key of aliases.keys()) args.push('-e', key);
for (const line of scan([...args, 'Assets'])) {
  const separator=line.lastIndexOf(':');
  const file=norm(line.slice(0,separator)), key=line.slice(separator+1).toLowerCase();
  for (const clip of aliases.get(key) || []) {
    if (disabled.has(file)) clip.disabled=true;
    else clip.used=true;
  }
}
process.stdout.write(JSON.stringify(clips.filter(c=>c.used || !c.disabled).map(c=>c.guid+'.mp4')));
