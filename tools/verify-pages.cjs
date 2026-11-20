/* Run against a local preview under /VeterinariaVR/ or the deployed URL.
   PLAYWRIGHT_MODULE and CHROME may be supplied on managed workstations. */
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
(async()=>{
  const base = new URL(process.argv[2]);
  const evidence = [];
  const failures = [];
  const galaxy=new Set(['7ScenaMusculo','4ScenaBoxeo','2ScenaCraneos','1ScenaOseo','6ScenaTodoRuleta',
    '5ScenaCerebro','10ScenaReflejoPerro','3ScenaResortera']);
  const browser = await chromium.launch({executablePath:process.env.CHROME, headless:true,
    args:['--enable-webgl','--enable-unsafe-swiftshader','--use-gl=angle','--use-angle=swiftshader']});
  try {
    for(const touch of [false,true]){
      const context=await browser.newContext({viewport:touch?{width:390,height:844}:{width:1280,height:800},hasTouch:touch,isMobile:touch});
      const page=await context.newPage();
      const errors=[];
      const heights=new Map();
      let menuPost=false;
      const recordError=message=>{if(!errors.includes(message)){console.error(message);errors.push(message);}};
      page.on('pageerror',e=>recordError(e.message));
      page.on('console',msg=>{
        const line=msg.text();
        if(line.includes('Posprocesado del menú en pantalla activo'))menuPost=true;
        const pose=/Vista de pantalla (.+?) · pies (.+?) · ojos ([\d.,-]+) · fondo (.+)/.exec(line);
        if(pose)heights.set(pose[1],{feet:pose[2],eye:Number(pose[3].replace(',','.')),background:pose[4].trim()});
        if(process.env.WEB_TEST_LOG)console.log('Navegador',line.slice(0,750));
        if(/\b\w+Exception:|out of memory|memory access out of bounds|WebGL context lost|Unable to parse Build/i.test(line))
          recordError(line.slice(0,500));
      });
      page.on('response',r=>{if(r.url().startsWith(base.href)&&r.status()>=400)recordError(r.status()+' '+r.url());});
      await page.goto(base.href,{waitUntil:'domcontentloaded'});
      await page.waitForFunction(()=>!!document.querySelector('#view')?.dataset.scene,null,{timeout:180000});
      const names=await page.locator('#levels option').evaluateAll(opts=>opts.map(o=>o.value));
      assert.equal(names.length,18);
      assert.equal(await page.getByRole('button',{name:'Mano',exact:true}).count(),0);
      await page.locator('#help').click();
      assert.match(await page.locator('#status').innerText(),touch?/Toca un objeto/:/Haz clic/);
      await page.locator('#help').click();
      assert.equal(await page.locator('#mobileLevels').isVisible(),touch);
      for(const scene of names){
        if(await page.locator('#view').getAttribute('data-scene')!==scene){
          if(await page.locator('#mobileLevels').isVisible()){
            const label=await page.locator('#levels option').evaluateAll((opts,id)=>opts.find(o=>o.value===id).text,scene);
            await page.locator('#mobileLevels').getByRole('button',{name:label,exact:true}).click();
          }else await page.selectOption('#levels',scene);
        }
        await page.waitForFunction(name=>document.querySelector('#view').dataset.scene===name && !document.querySelector('#levels').disabled,scene,{timeout:150000});
        await page.waitForFunction(()=>document.documentElement.dataset.mode==='screen');
        const canvas=await page.locator('#view').boundingBox();assert.ok(canvas.width>0&&canvas.height>0);
        assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),'La página desborda el ancho de pantalla');
        if(touch)await page.touchscreen.tap(canvas.width*.60,canvas.height*.46);
        else{await page.keyboard.down('w');await page.waitForTimeout(150);await page.keyboard.up('w');}
        const pose=heights.get(scene);
        assert.ok(pose,'Falta la posición de pantalla en '+scene);
        if(scene==='8ScenaCirugia'||scene==='Scena6.1 Quirofano'||scene==='11ScenaVaca'){
          const floor={'8ScenaCirugia':.0293,'Scena6.1 Quirofano':.2093,'11ScenaVaca':-.008}[scene];
          assert.ok(Math.abs(pose.eye-floor-1.6)<.12,'Altura de entrada incorrecta en '+scene+' '+pose.eye);
        }
        assert.equal(pose.background==='ViaLactea',galaxy.has(scene),'Fondo incorrecto en '+scene);
        if(scene==='0ScenaMenu')assert.ok(menuPost,'El menú debe usar posprocesado en pantalla');
        if(scene!=='0ScenaMenu')assert.equal(await page.locator('#stick').isVisible(),touch);
        if(process.env.WEB_TEST_SHOTS && ['0ScenaMenu','8ScenaCirugia','1ScenaOseo'].includes(scene))
          await page.screenshot({path:require('node:path').join(process.env.WEB_TEST_SHOTS,(touch?'movil-':'pc-')+scene+'.png')});
        evidence.push({touch,scene,...pose,errors:[...new Set(errors)]});
        console.log(touch?'Táctil':'Escritorio',scene);
      }
      failures.push(...errors);
      await context.close();
    }
    if(process.env.XR_TEST_BUNDLE){
      const xr=await browser.newContext({viewport:{width:1100,height:720}});
      await xr.addInitScript({content:fs.readFileSync(process.env.XR_TEST_BUNDLE,'utf8')+
        '\nwindow.questTest=new IWER.XRDevice(IWER.metaQuest3);window.questTest.installRuntime({forceInstall:true});'});
      const page=await xr.newPage();
      await page.goto(base.href,{waitUntil:'domcontentloaded'});
      await page.waitForFunction(()=>!!document.querySelector('#view')?.dataset.scene,null,{timeout:180000});
      await page.locator('#xr').click();
      await page.waitForFunction(()=>!!window.questTest.activeSession,null,{timeout:30000});
      await page.waitForFunction(()=>document.documentElement.dataset.mode==='xr',null,{timeout:30000});
      const result=await page.evaluate(async()=>{
        const session=window.questTest.activeSession,space=await session.requestReferenceSpace('local-floor');
        const views=await new Promise(resolve=>session.requestAnimationFrame((_,frame)=>resolve(frame.getViewerPose(space)?.views.length)));
        const hands=Array.from(session.inputSources,s=>s.handedness);
        const right=window.questTest.controllers.right;
        right.updateButtonValue('trigger',1);
        await new Promise(resolve=>session.requestAnimationFrame(resolve));
        right.updateButtonValue('trigger',0);
        await session.end();
        return {views,hands};
      });
      assert.equal(result.views,2);assert.ok(result.hands.includes('right')&&result.hands.includes('left'));
      await page.waitForFunction(()=>document.documentElement.dataset.mode==='screen',null,{timeout:30000});
      evidence.push({device:'Quest 3 emulada',...result});
      await page.selectOption('#levels','1ScenaOseo');
      await page.waitForFunction(()=>document.querySelector('#view').dataset.scene==='1ScenaOseo');
      await page.locator('#xr').click();
      await page.waitForFunction(()=>document.documentElement.dataset.mode==='xr');
      await page.evaluate(()=>window.questTest.activeSession.end());
      await page.waitForFunction(()=>document.documentElement.dataset.mode==='screen');
      evidence.push({device:'Quest 3 emulada',scene:'1ScenaOseo',returnedToScreen:true});
      await xr.close();
    }
    assert.deepEqual([...new Set(failures)],[]);
  } finally {
    if(process.argv[3])fs.writeFileSync(process.argv[3],JSON.stringify(evidence,null,2));
    await browser.close();
  }
})().catch(e=>{console.error(e);process.exitCode=1});

