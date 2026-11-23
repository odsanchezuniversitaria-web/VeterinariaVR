// Tests the normal start, pointer, score and pause controls of the real Unity scene.
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'playwright'),assert=require('node:assert/strict');
const fs=require('node:fs'),path=require('node:path');
const [url,out]=process.argv.slice(2);
fs.mkdirSync(out,{recursive:true});
async function check(browser,touch){
  const context=await browser.newContext({viewport:{width:1020,height:650},isMobile:touch,hasTouch:touch,deviceScaleFactor:1});
  await context.addInitScript(()=>document.addEventListener('veterinaria-boxeo',e=>window.boxingTestState=e.detail));
  try{
    const page=await context.newPage(),device=touch?'movil':'pc',errors=[];
    page.on('pageerror',e=>errors.push(e.message));
    const state=()=>page.evaluate(()=>window.boxingTestState);
    const tap=async (x,y)=>{
      if(touch)await page.touchscreen.tap(x,y);
      else{await page.mouse.move(x,y);await page.mouse.down();await page.waitForTimeout(46);await page.mouse.up();}
    };
    await page.goto(url,{waitUntil:'domcontentloaded'});
    await page.waitForFunction(()=>document.querySelector('#view')?.dataset.scene==='0ScenaMenu' && document.querySelector('#levels').options.length===18,null,{timeout:600000});
    await page.selectOption('#levels','4ScenaBoxeo');
    await page.waitForFunction(()=>window.boxingTestState?.estado==='inicio',null,{timeout:240000});
    const help=page.locator('#boxingHint');
    assert.match(await help.innerText(),touch?/Toca el hueso/:/Haz clic en el hueso/);
    const fullscreen=page.locator('#wideMode');
    if(await fullscreen.isVisible()){
      await fullscreen.click();await page.waitForFunction(()=>!!document.fullscreenElement);
      await fullscreen.click();await page.waitForFunction(()=>!document.fullscreenElement);
    }
    await page.locator('#boxingPlay').click();
    await page.waitForFunction(()=>window.boxingTestState?.estado==='jugando' && window.boxingTestState.objetivo,null,{timeout:12000});
    await page.screenshot({path:path.join(out,'boxeo-'+device+'.png')});
    await page.locator('#boxingPlay').click();await page.waitForFunction(()=>window.boxingTestState?.estado==='pausa');
    assert.match(await help.innerText(),/pausa/i);
    await page.locator('#boxingPlay').click();await page.waitForFunction(()=>window.boxingTestState?.estado==='jugando');
    const view=await page.locator('#view').boundingBox();
    const was=await state();
    // The active bones occupy the central shelves. Probe a small grid with ordinary clicks or taps.
    const cols=[.39,.45,.51,.57,.63,.69,.75,.33,.27,.81,.21];
    const rows=[.45,.39,.51,.33,.57,.27,.63,.21,.69,.75,.81,.15];
    search:for(const y of rows)for(const x of cols){
      await tap(view.x+x*view.width,view.y+y*view.height);
      await page.waitForTimeout(115);
      if((await state()).puntos>was.puntos)break search;
    }
    const hit=await state();
    assert(hit.puntos>was.puntos,'Un clic o toque en el hueso objetivo debe aumentar la puntuación');
    assert.deepEqual(errors,[]);
    const result={device,score:hit.puntos,round:hit.ronda,target:hit.objetivo};
    fs.writeFileSync(path.join(out,'boxeo-'+device+'.json'),JSON.stringify(result,null,2));console.log(JSON.stringify(result));
  }finally{await context.close();}
}
(async()=>{const browser=await chromium.launch({executablePath:process.env.CHROME,args:['--enable-webgl','--enable-unsafe-swiftshader','--use-gl=angle','--use-angle=swiftshader']});
try{await check(browser,false);await check(browser,true)}finally{await browser.close()}})().catch(e=>{console.error(e);process.exitCode=1});
