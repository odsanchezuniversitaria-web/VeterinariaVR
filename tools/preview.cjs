// node tools/preview.cjs <distribution> [port]
const http=require('node:http');
const fs=require('node:fs');
const path=require('node:path');
const root=fs.realpathSync(process.argv[2]);
const prefix='/VeterinariaVR/';
const types={'.html':'text/html; charset=utf-8','.js':'application/javascript','.json':'application/json','.wasm':'application/wasm','.mp4':'video/mp4','.css':'text/css'};
http.createServer((req,res)=>{
  try{
    const url=new URL(req.url,'http://localhost');
    if(!url.pathname.startsWith(prefix)||!['GET','HEAD'].includes(req.method)){res.writeHead(404).end();return;}
    const file=fs.realpathSync(path.resolve(root,decodeURIComponent(url.pathname.slice(prefix.length))||'index.html'));
    const rel=path.relative(root,file);
    if(!rel||rel.startsWith('..')||path.isAbsolute(rel)){res.writeHead(404).end();return;}
    const stat=fs.statSync(file);
    if(!stat.isFile()){res.writeHead(404).end();return;}
    let start=0,end=stat.size-1,status=200;
    if(req.headers.range){
      const m=/^bytes=(\d*)-(\d*)$/.exec(req.headers.range);
      if(!m || (!m[1]&&!m[2])){res.writeHead(416,{'Content-Range':'bytes */'+stat.size}).end();return;}
      if(m[1]){start=Number(m[1]);if(m[2])end=Math.min(end,Number(m[2]));}
      else start=Math.max(0,stat.size-Number(m[2]));
      if(start>end){res.writeHead(416,{'Content-Range':'bytes */'+stat.size}).end();return;}
      res.setHeader('Content-Range',`bytes ${start}-${end}/${stat.size}`);status=206;
    }
    res.writeHead(status,{'Content-Type':types[path.extname(file)]||'application/octet-stream','Accept-Ranges':'bytes','Content-Length':end-start+1});
    if(req.method==='HEAD'||!stat.size){res.end();return;}
    const stream=fs.createReadStream(file,{start,end});
    stream.on('error',()=>res.destroy());res.on('close',()=>stream.destroy());stream.pipe(res);
  }catch{res.writeHead(404).end();}
}).listen(Number(process.argv[3]||8765),'127.0.0.1',()=>console.log('Vista local en /VeterinariaVR/'));

