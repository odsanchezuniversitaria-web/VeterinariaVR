/* Exercise the public UI with Chromium, mouse events and native multi-touch contacts.
   node tools/verify-gestures.cjs <URL> <evidence directory> */
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'playwright');
const assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
const [url,out]=process.argv.slice(2);
fs.mkdirSync(out,{recursive:true});

// Read the transforms actually submitted to WebGL on demand. Nothing is exposed by the player.
function installFrameReader(){
  const proto=WebGL2RenderingContext.prototype;
  let pending=null;
  window.readGestureFrame=async()=>{
    const capture={objects:{},instances:{},projection:0};pending=capture;pending.projections=[];
    for(let i=0;i<15 && !Object.keys(capture.objects).length;i++)
      await new Promise(r=>setTimeout(r,100));
    pending=null;const samples=capture.projections.sort((a,b)=>a-b);
    capture.projection=samples[Math.floor(samples.length/2)]||0;delete capture.projections;return capture;
  };
  for(const name of ['drawElements','drawElementsInstanced','drawRangeElements']){
    const native=proto[name];
    proto[name]=function(...args){
      const count=args[name==='drawRangeElements'?3:1];
      if(pending && count>=48){
        const gl=this,p=gl.getParameter(gl.CURRENT_PROGRAM),block=gl.getUniformBlockIndex(p,'UnityPerDraw');
        if(block!==gl.INVALID_INDEX){
          const slot=gl.getActiveUniformBlockParameter(p,block,gl.UNIFORM_BLOCK_BINDING);
          const buf=gl.getIndexedParameter(gl.UNIFORM_BUFFER_BINDING,slot);
          if(buf){
            const saved=gl.getParameter(gl.COPY_READ_BUFFER_BINDING),m=new Float32Array(16);
            gl.bindBuffer(gl.COPY_READ_BUFFER,buf);
            gl.getBufferSubData(gl.COPY_READ_BUFFER,gl.getIndexedParameter(gl.UNIFORM_BUFFER_START,slot),m);
            gl.bindBuffer(gl.COPY_READ_BUFFER,saved);
            {
              const cols=[0,1,2].map(i=>gl.getUniformLocation(p,'hlslcc_mtx4x4unity_MatrixVP['+i+']'));
              if(cols.every(Boolean))pending.projections.push(Math.hypot(...cols.map(c=>gl.getUniform(p,c)[1])));
            }
            const offset=args[name==='drawRangeElements'?5:3];
            const key=count+'-'+offset;
            // A given mesh can be instanced several times. Compare unambiguous meshes only.
            const row={pos:Array.from(m.slice(12,15)),scale:Math.hypot(m[0],m[1],m[2])};
            const poseKey=key+':'+row.pos.map(v=>v.toFixed(3)).join(',');
            if(row.scale>1e-8)pending.instances[poseKey]=row.scale;
            if(!Object.hasOwn(pending.objects,key))pending.objects[key]=row;
            else if(pending.objects[key] && Math.hypot(...row.pos.map((v,i)=>v-pending.objects[key].pos[i]))>.001)
              pending.objects[key]=null;
          }
        }
      }
      return native.apply(this,args);
    };
  }
}
function compare(a,b){
  const pairs=[];
  for(const [key,x] of Object.entries(a.objects)){
    const y=b.objects[key];if(!x || !y || x.scale<1e-8)continue;
    pairs.push({key,delta:y.pos.map((v,i)=>v-x.pos[i]),ratio:y.scale/x.scale});
  }
  return pairs;
}
async function check(browser,touch){
  const context=await browser.newContext({viewport:{width:920,height:560},hasTouch:touch,isMobile:touch,deviceScaleFactor:1});
  await context.addInitScript(installFrameReader);
  if(process.env.XR_TEST_BUNDLE)await context.addInitScript({content:fs.readFileSync(process.env.XR_TEST_BUNDLE,'utf8')+
    '\nwindow.questTest=new IWER.XRDevice(IWER.metaQuest3);questTest.installRuntime({forceInstall:true});'});
  try{
    const page=await context.newPage(),cdp=await context.newCDPSession(page),errors=[];
    page.on('pageerror',e=>errors.push(e.message));
    page.on('console',m=>{if(/\w+Exception:|out of memory|memory access out of bounds/i.test(m.text()))errors.push(m.text().slice(0,260));});
    const toolbar=page.locator('#zoomBar'),percent=async()=>Number(await toolbar.getAttribute('data-percent'));
    async function snap(){
      for(let attempt=0;attempt<12;attempt++){
        const frame=await page.evaluate(()=>window.readGestureFrame());
        if(frame.projection>0 && Object.keys(frame.objects).length)return frame;
      }
      throw new Error('El navegador no dibujó una escena 3D medible');
    }
    async function awaitProjection(expected){
      let frame;
      for(let attempt=0;attempt<20;attempt++){
        frame=await snap();
        if(Math.abs(frame.projection/expected-1)<.035)return frame;
        await page.waitForTimeout(125);
      }
      throw new Error('Proyección esperada '+expected+', observada '+frame.projection);
    }
    const send=(type,touchPoints)=>cdp.send('Input.dispatchTouchEvent',{type,touchPoints});
    const at=(id,x,y)=>({id,x,y,radiusX:4,radiusY:4});
    const pause=(ms=150)=>page.waitForTimeout(ms);
    async function open(scene){
      await page.selectOption('#levels',scene);
      await page.waitForFunction(n=>document.querySelector('#view').dataset.scene===n,scene,{timeout:150000});
      await pause(950);
    }
    async function wheel(delta){
      await page.keyboard.down('Shift');
      try{await page.mouse.wheel(0,delta);await pause();}finally{await page.keyboard.up('Shift');}
    }
    async function press(p){
      if(touch)await send('touchStart',[p]);
      else{await page.mouse.move(p.x,p.y);await page.mouse.down();}
      await pause();
    }
    async function release(){
      if(touch)await send('touchEnd',[]);else await page.mouse.up();
      await pause();
    }
    await page.goto(url,{waitUntil:'domcontentloaded'});
    await page.waitForFunction(()=>document.querySelector('#view')?.dataset.scene==='0ScenaMenu',null,{timeout:180000});
    assert.equal(await toolbar.isVisible(),false);
    await open('1ScenaOseo');
    let point=null;
    const candidates=[[.37,.55],[.38,.56],[.35,.56],[.40,.53],[.36,.59],[.46,.59],[.52,.62],[.57,.66],[.60,.67],[.50,.70]];
    for(const [x,y] of candidates){
      const p=at(1,x*920,y*560);await press(p);
      if(await toolbar.isVisible()){point=p;break;}
      await release();
    }
    assert(point,'Seleccionar un hueso visible');const target=await toolbar.getAttribute('data-target');
    const start=await snap();const moved={...point,x:point.x+50,y:point.y-25};
    if(touch)await send('touchMove',[moved]);else await page.mouse.move(moved.x,moved.y,{steps:5});
    await pause(220);const dragged=await snap();
    const translation=compare(start,dragged).filter(p=>p.delta[0]>.012 && Math.abs(p.ratio-1)<.03);
    assert(translation.length,'El hueso seleccionado debe desplazarse con el puntero');
    const mesh=translation.sort((a,b)=>b.delta[0]-a.delta[0])[0].key;
    let remaining=moved;
    if(touch){
      const other=at(2,moved.x+90,moved.y);await send('touchStart',[moved,other]);
      remaining={...other,x:other.x+45};
      await send('touchMove',[moved,remaining]);await pause();
    }else await wheel(-225);
    const zoomed=await snap(),enlarged=await percent();
    assert(enlarged>120 && enlarged<=200,'Agrandar la pieza');
    const actualRatio=zoomed.objects[mesh].scale/dragged.objects[mesh].scale;
    assert(Math.abs(actualRatio-enlarged/100)<.04,'Debe ampliarse la malla visible');
    if(touch){
      await send('touchEnd',[moved]);await pause();const one=await snap();
      const jump=compare(zoomed,one).find(p=>p.key===mesh);
      assert(jump && Math.hypot(...jump.delta)<.025,'Al levantar el primer dedo la pieza conserva su posición');
      await send('touchMove',[{...remaining,x:remaining.x+38}]);await pause();const continued=await snap();
      assert(compare(one,continued).find(p=>p.key===mesh)?.delta[0]>.012,'El dedo restante continúa arrastrando');
    }
    await release();
    if(touch){
      const a=at(1,458,166),b=at(2,620,166);
      await send('touchStart',[a]);await send('touchStart',[a,b]);await send('touchMove',[a,{...b,x:520}]);await release();
    }else await wheel(420);
    assert(await percent()<enlarged,'Encoger la pieza después de soltarla');
    await page.locator('#zoomReset').click();await pause();assert.equal(await percent(),100);
    const reset=await snap();if(reset.objects[mesh])assert(Math.abs(reset.objects[mesh].scale/dragged.objects[mesh].scale-1)<.035);
    for(let i=0;i<7;i++)await page.locator('#zoomMore').click();assert.equal(await percent(),200);
    for(let i=0;i<12;i++)await page.locator('#zoomLess').click();assert.equal(await percent(),50);
    await page.locator('#zoomReset').click();
    const report={device:touch?'celular':'PC',target,mesh,drag:translation[0].delta,piecePercent:enlarged,actualRatio,surgery:[]};
    for(const scene of ['8ScenaCirugia','Scena6.1 Quirofano']){
      await open(scene);assert.equal(await page.locator('#zoomType').innerText(),'Vista');
      assert.equal(await percent(),100);const before=await snap();
      const a=at(1,438,245),b=at(2,560,245);
      if(touch){await send('touchStart',[a,b]);await send('touchMove',[a,{...b,x:615}]);await release();}
      else{await page.mouse.move(480,260);await wheel(-220);}
      const level=await percent(),after=await awaitProjection(before.projection*level/100),ratio=after.projection/before.projection;
      assert(level>125 && Math.abs(ratio-level/100)<.04,'En cirugía debe cambiar el encuadre de la cámara');
      const stable=Object.keys(before.instances).filter(k=>Object.hasOwn(after.instances,k));
      assert(stable.length>=2 && stable.every(k=>Math.abs(after.instances[k]/before.instances[k]-1)<.005),
        'El instrumental debe conservar su escala en '+scene+' ('+stable.length+' mallas comparables)');
      report.surgery.push({scene,level,projectionRatio:ratio,stableMeshes:stable.length});
      if(scene==='8ScenaCirugia' && process.env.XR_TEST_BUNDLE){
        await page.locator('#xr').click();await page.waitForFunction(()=>document.documentElement.dataset.mode==='xr',null,{timeout:30000});
        assert.equal(await toolbar.isVisible(),false);await page.evaluate(()=>questTest.activeSession.end());
        await page.waitForFunction(()=>document.documentElement.dataset.mode==='screen');await pause();assert.equal(await percent(),100);
        await awaitProjection(before.projection);report.xrRestored=true;
      }else{await page.locator('#zoomReset').click();await pause();assert.equal(await percent(),100);}
    }
    assert.deepEqual(errors,[]);console.log(JSON.stringify(report));
    fs.writeFileSync(path.join(out,touch?'gestos-celular.json':'gestos-pc.json'),JSON.stringify(report,null,2));
  }finally{await context.close();}
}
(async()=>{
  const browser=await chromium.launch({executablePath:process.env.CHROME,headless:true,
    args:['--enable-webgl','--enable-unsafe-swiftshader','--use-gl=angle','--use-angle=swiftshader']});
  try{await check(browser,false);await check(browser,true);}finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
