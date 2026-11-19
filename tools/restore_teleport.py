"""Restore the exact missing VFX prefab and its dependency GUIDs from the locally owned Unity package."""
from pathlib import Path, PurePosixPath
import gzip
import re
import sys
import tarfile

assets, archive = map(Path, sys.argv[1:])
assets=assets.resolve(strict=True)
if assets.name!='Assets' or not (assets/'VeterinariaWeb/WebRig.cs').is_file():
    raise ValueError('Se requiere la copia de Unity con el adaptador instalado')
present=set()
for meta in assets.rglob('*.meta'):
    with meta.open('rb') as stream:
        m=re.search(rb'(?m)^guid: ([0-9a-f]{32})',stream.read(256))
        if m:present.add(m[1].decode())
records={}
with gzip.open(archive,'rb') as stream,tarfile.open(fileobj=stream,mode='r|') as tar:
    for item in tar:
        parts=PurePosixPath(item.name).parts
        if len(parts)!=2 or not re.fullmatch(r'[0-9a-f]{32}',parts[0]) or not item.isfile():continue
        if parts[1] in {'asset','asset.meta','pathname'}:
            records.setdefault(parts[0],{})[parts[1]]=tar.extractfile(item).read()
todo=['1eb833340b41fb444b576c4dced8d4dc']
seen=set()
while todo:
    guid=todo.pop()
    if guid in seen:continue
    seen.add(guid)
    record=records[guid]
    rel=PurePosixPath(record['pathname'].decode('utf-8-sig').splitlines()[0].strip('\0 '))
    if rel.is_absolute() or rel.parts[0]!='Assets' or '..' in rel.parts:raise ValueError(rel)
    target=assets.joinpath(*rel.parts[1:]).resolve()
    if not target.is_relative_to(assets):raise ValueError(target)
    body=record['asset']
    if guid not in present:
        if target.exists() or Path(str(target)+'.meta').exists():raise ValueError('Colisión: '+str(target))
        target.parent.mkdir(parents=True,exist_ok=True)
        target.write_bytes(body)
        Path(str(target)+'.meta').write_bytes(record['asset.meta'])
        print('Restaurado',rel.as_posix(),flush=True)
    for reference in set(re.findall(rb'guid: ([0-9a-f]{32})',body+record['asset.meta'])):
        ref=reference.decode()
        if ref in records and ref not in seen:todo.append(ref)

