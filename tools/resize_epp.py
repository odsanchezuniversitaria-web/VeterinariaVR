"""Resize embedded PPE textures while retaining glTF object IDs, geometry and rigging."""
from pathlib import Path
from io import BytesIO
import json
import struct
import sys
from PIL import Image

project=Path(sys.argv[1]).resolve()
if not (project/'Assets/VeterinariaWeb/WebRig.cs').is_file():
    raise ValueError('Se requiere la copia web independiente')
for path in (project/'Assets/ModelosEPP').glob('*.glb'):
    raw=path.read_bytes()
    magic,version,length,n,kind=struct.unpack_from('<4s4I',raw)
    if (magic,version,length,kind)!=(b'glTF',2,len(raw),0x4E4F534A):raise ValueError(path)
    doc=json.loads(raw[20:20+n]);pos=20+n
    count,kind=struct.unpack_from('<II',raw,pos)
    if kind!=0x004E4942 or pos+8+count!=length or len(doc['buffers'])!=1 or doc.get('extensionsUsed'):raise ValueError(path)
    body=raw[pos+8:];views=doc['bufferViews'];images={}
    for img in doc.get('images',[]):
        i=img['bufferView'];v=views[i];begin=v.get('byteOffset',0)
        with Image.open(BytesIO(body[begin:begin+v['byteLength']])) as image:
            if max(image.size)<=512:continue
            fmt=image.format;image.thumbnail((512,512),Image.Resampling.LANCZOS)
            out=BytesIO();image.save(out,format=fmt);images[i]=out.getvalue()
    if not images:continue
    new=bytearray();cursor=0
    for i,v in sorted(enumerate(views),key=lambda item:item[1].get('byteOffset',0)):
        offset=v.get('byteOffset',0);end=offset+v['byteLength']
        if offset<cursor or end>len(body):raise ValueError('Vistas solapadas en '+path.name)
        new.extend(body[cursor:offset]);new.extend(b'\0'*(-len(new)%4))
        data=images[i] if i in images else body[offset:end]
        v['byteOffset']=len(new);v['byteLength']=len(data);new.extend(data);cursor=end
    new.extend(body[cursor:]);doc['buffers'][0]['byteLength']=len(new)
    new.extend(b'\0'*(-len(new)%4))
    header=json.dumps(doc,separators=(',',':'),ensure_ascii=False).encode('utf8');header+=b' '*(-len(header)%4)
    result=struct.pack('<4s4I',magic,2,28+len(header)+len(new),len(header),0x4E4F534A)+header+struct.pack('<II',len(new),0x004E4942)+new
    temp=path.with_suffix('.web-tmp');temp.write_bytes(result);temp.replace(path)
    print(path.name,len(raw),'->',len(result))

