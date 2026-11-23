/* Verify that each browser receives the other's avatar in the same practice.
   node tools/verify-multiplayer.cjs <url> <report directory> */
const { chromium }=require(process.env.PLAYWRIGHT_MODULE||'playwright');
const assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
const [url,out]=process.argv.slice(2);
const region='sa',practice='1ScenaOseo',menu='0ScenaMenu';
fs.mkdirSync(out,{recursive:true});

async function client(browser,label,touch){
  const ctx=await browser.newContext({viewport:touch?{width:390,height:844}:{width:1060,height:700},
    deviceScaleFactor:1,hasTouch:touch,isMobile:touch});
  if(process.env.XR_TEST_BUNDLE && touch)await ctx.addInitScript({content:fs.readFileSync(process.env.XR_TEST_BUNDLE,'utf8')+
    '\nwindow.questTest=new IWER.XRDevice(IWER.metaQuest3);questTest.installRuntime({forceInstall:true});'});
  const page=await ctx.newPage(),rooms=new Map(),visible=new Map(),errors=[],evidence=[];
  page.on('pageerror',e=>errors.push(e.message));
  page.on('console',message=>{
    const line=message.text();
    if(/no se pudo conectar|Failed to resolve|UnityException|memory access out of bounds/i.test(line))errors.push(line.slice(0,240));
    const room=/\[VeterinariaVR\] Sala (.+?) · región (\S+) · alumno (\d+)/.exec(line);
    if(room){
      const [,scene,actual,id]=room;
      rooms.set(scene,{region:actual,id:Number(id)});evidence.push(line.trim());
      console.log(label,line.trim());
    }
    const list=/\[VeterinariaVR\] Visibles (.+?) · (\S+) · (\d+): (.*)/.exec(line);
    if(list){
      const [,scene,actual,me,names]=list;
      const ids=Array.from(names.matchAll(/Alumno (\d+)/g),x=>Number(x[1])).sort((a,b)=>a-b);
      visible.set(scene,{region:actual,me:Number(me),ids});evidence.push(line.trim());
    }
  });
  const state=async()=>page.evaluate(()=>({scene:document.querySelector('#view').dataset.scene,mode:document.documentElement.dataset.mode}));
  async function open(scene){
    if(await page.locator('#mobileLevels').isVisible()){
      const label=await page.locator('#levels option').evaluateAll((opts,id)=>opts.find(o=>o.value===id).text,scene);
      await page.locator('#mobileLevels').getByRole('button',{name:label,exact:true}).tap();
    }else await page.selectOption('#levels',scene);
    await page.waitForFunction(id=>document.querySelector('#view')?.dataset.scene===id && !document.querySelector('#levels').disabled,
      scene,{timeout:600000});
  }
  return {ctx,page,rooms,visible,label,state,open,errors,evidence};
}
async function until(test,label,clients,timeout=90000){
  const limit=Date.now()+timeout;
  while(Date.now()<limit){
    for(const c of clients)if(c.errors.length)throw new Error(c.label+': '+c.errors.join(' | '));
    const result=await test();
    if(result)return result;
    await new Promise(r=>setTimeout(r,400));
  }
  throw new Error(label+' · '+JSON.stringify(clients.map(c=>({label:c.label,rooms:[...c.rooms],visible:[...c.visible]}))));
}
async function bothSeeEachOther(clients,scene){
  return until(()=>{
    const [a,b]=clients.map(c=>c.rooms.get(scene));
    if(!a || !b)return;
    assert.equal(a.region,region);assert.equal(b.region,region);
    assert.notEqual(a.id,b.id,'Los dos clientes deben ocupar la misma sala con identificadores distintos');
    const list=clients.map(c=>c.visible.get(scene));
    if(list.every(v=>v?.region===region && v.ids.includes(a.id) && v.ids.includes(b.id)))
      return list;
  },'Ambas listas deben reconocer a los mismos alumnos en '+scene,clients);
}

(async()=>{
  const browser=await chromium.launch({executablePath:process.env.CHROME,
    args:['--enable-webgl','--enable-unsafe-swiftshader','--use-gl=angle','--use-angle=swiftshader',
      '--disable-background-timer-throttling','--disable-renderer-backgrounding','--disable-backgrounding-occluded-windows']});
  const clients=[];
  try{
    clients.push(await client(browser,'PC',false),await client(browser,'móvil',true));
    // Let the first peer finish joining before initializing the second client.
    for(const c of clients){
      await c.page.goto(url,{waitUntil:'domcontentloaded'});
      await c.page.waitForFunction(()=>document.querySelector('#view')?.dataset.scene==='0ScenaMenu'
        && document.querySelector('#levels').options.length===18,null,{timeout:720000});
      await until(()=>c.rooms.get(menu),c.label+' debe entrar al menú',[c]);
    }
    const lobby=await bothSeeEachOther(clients,menu);
    // Each practice has its own room. Move both clients to Sistema óseo.
    for(const c of clients){
      await c.open(practice);
      await until(()=>c.rooms.get(practice),c.label+' debe entrar a Sistema óseo',[c]);
    }
    const practiceScreen=await bothSeeEachOther(clients,practice);
    for(const c of clients)await c.page.screenshot({path:path.join(out,c.label+'-sistema-oseo.png')});
    let xr;
    if(process.env.XR_TEST_BUNDLE){
      const mobile=clients[1];
      let restored=false;
      mobile.page.on('console',m=>{if(/Cámara de visor restaurada · 1ScenaOseo · observatorio=False/.test(m.text()))restored=true;});
      await mobile.page.locator('#xr').tap();
      await mobile.page.waitForFunction(()=>document.documentElement.dataset.mode==='xr',null,{timeout:40000});
      const views=await mobile.page.evaluate(async()=>{
        const session=questTest.activeSession;
        const space=await session.requestReferenceSpace('local-floor');
        return await new Promise(r=>session.requestAnimationFrame((_,frame)=>r(frame.getViewerPose(space)?.views.length)));
      });
      assert.equal(views,2);assert(restored,'La cámara inmersiva conserva sus ajustes de visor');
      const desktop=clients[0],seat=mobile.rooms.get(practice),departing=desktop.rooms.get(practice);
      desktop.rooms.delete(menu);
      await desktop.open(menu);
      await until(()=>desktop.rooms.get(menu), 'El PC debe entrar a la sala del menú', clients);
      await until(()=>{
        const list=mobile.visible.get(practice);
        return list && list.ids.includes(seat.id) && !list.ids.includes(departing.id);
      },'El visor debe ver salir al otro alumno de la práctica',clients);
      desktop.rooms.delete(practice);desktop.visible.delete(practice);
      await desktop.open(practice);
      const reunited=await bothSeeEachOther(clients,practice);
      assert.equal((await mobile.state()).mode,'xr');
      assert.equal(mobile.rooms.get(practice).id,seat.id,'WebVR debe conservar la misma conexión');
      await mobile.page.evaluate(()=>questTest.activeSession.end());
      await mobile.page.waitForFunction(()=>document.documentElement.dataset.mode==='screen');
      xr={views,restored,player:seat.id,avatars:reunited};
    }
    const report={region,menu:lobby,practice:practiceScreen,xr};
    console.log(JSON.stringify(report));fs.writeFileSync(path.join(out,'multijugador.json'),JSON.stringify(report,null,2));
  }finally{
    for(const c of clients)fs.writeFileSync(path.join(out,c.label+'-photon.log'),c.evidence.join('\n'));
    await browser.close();
  }
})().catch(e=>{console.error(e);process.exitCode=1});