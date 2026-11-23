// Exercise the public controls and observe the state reported by the running Unity scene.
const { chromium }=require(process.env.PLAYWRIGHT_MODULE||'playwright');
const assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
const [url,out]=process.argv.slice(2);
fs.mkdirSync(out,{recursive:true});
async function check(browser,touch){
  const ctx=await browser.newContext({viewport:touch?{width:390,height:844}:{width:1100,height:760},isMobile:touch,hasTouch:touch,deviceScaleFactor:1});
  await ctx.addInitScript(()=>document.addEventListener('veterinaria-clinical',e=>window.clinicalWitness=e.detail));
  if(process.env.XR_TEST_BUNDLE)await ctx.addInitScript({content:fs.readFileSync(process.env.XR_TEST_BUNDLE,'utf8')+
    '\nwindow.questTest=new IWER.XRDevice(IWER.metaQuest3);questTest.installRuntime({forceInstall:true});'});
  const device=touch?'celular':'pc',report={device},errors=[];
  const page=await ctx.newPage(),cdp=await ctx.newCDPSession(page);
  page.on('pageerror',e=>errors.push(e.message));
  page.on('console',m=>{if(/Exception:|memory access out of bounds|WebGL context lost/.test(m.text()))errors.push(m.text().slice(0,220));});
  const wait=(fn,arg,timeout=60000)=>page.waitForFunction(fn,arg,{timeout});
  const read=()=>page.evaluate(()=>window.clinicalWitness);
  const click=async selector=>touch?page.locator(selector).tap():page.locator(selector).click();
  async function open(scene){
    if(await page.locator('#mobileLevels').isVisible()){
      const title=await page.locator('#levels option').evaluateAll((opts,id)=>opts.find(o=>o.value===id).text,scene);
      await page.locator('#mobileLevels').getByRole('button',{name:title,exact:true}).tap();
    }else await page.selectOption('#levels',scene);
    await wait(id=>document.querySelector('#view').dataset.scene===id,scene,600000);
  }
  async function xr(){
    if(!process.env.XR_TEST_BUNDLE)return;
    await click('#xr');await wait(()=>document.documentElement.dataset.mode==='xr');
    assert(!await page.locator('#clinical').isVisible());assert(!await page.locator('#guidePins').isVisible());
    await page.evaluate(()=>questTest.activeSession.end());await wait(()=>document.documentElement.dataset.mode==='screen');
    await page.locator('#clinicToggle').waitFor();
  }
  const finger=(type,x,y)=>cdp.send('Input.dispatchTouchEvent',{type,touchPoints:x==null?[]:[{id:1,x,y,radiusX:4,radiusY:4}]});
  async function drag(from,to,lose=false){
    const box=await page.locator('#slideRail').boundingBox();
    const x=p=>box.x+box.width*(.14+.72*p),y=box.y+box.height*.70;
    if(touch)await finger('touchStart',x(from),y);
    else{await page.mouse.move(x(from),y);await page.mouse.down();}
    await page.waitForTimeout(140);
    for(let n=1;n<=10;n++){const pos=x(from+(to-from)*n/10);if(touch)await finger('touchMove',pos,y);else await page.mouse.move(pos,y);await page.waitForTimeout(45);}
    if(lose){if(touch)await finger('touchMove',x(to),y-box.height*.45);else await page.mouse.move(x(to),y-box.height*.45);}
    if(touch)await finger('touchEnd');else await page.mouse.up();
    await page.waitForTimeout(250);
  }
  try{
    await page.goto(url,{waitUntil:'domcontentloaded'});
    await wait(()=>document.querySelector('#view')?.dataset.scene==='0ScenaMenu',null,600000);
    assert(!await page.locator('#clinical').isVisible());
    await open('7ScenaLaboratorioClinico');await click('#clinicToggle');await click('#dropButton');
    await wait(()=>window.clinicalWitness?.ready && window.clinicalWitness.next,null,90000);
    await drag(0,.42);assert.equal((await read()).progress,0,'El ángulo bajo debe impedir el extendido');
    const range=await page.locator('#slideAngle').boundingBox();
    if(touch)await page.touchscreen.tap(range.x+range.width*.47,range.y+range.height*.5);
    else await page.mouse.click(range.x+range.width*.47,range.y+range.height*.5);
    const angle=Number(await page.locator('#slideAngle').inputValue());assert(angle>=30 && angle<=45);
    await drag(0,.43,true);
    const half=await read();assert(half.progress>25 && half.progress<60 && !half.held);
    assert.match(await page.locator('#labCue').innerText(),/contacto/);
    await page.screenshot({path:path.join(out,'frotis-'+device+'.png')});
    if(!touch && process.env.XR_TEST_BUNDLE){
      await xr();await click('#clinicToggle');assert.equal((await read()).progress,half.progress);
      const r=await page.locator('#slideAngle').boundingBox();await page.mouse.click(r.x+r.width*.46,r.y+r.height*.5);
    }
    await drag(half.progress/100,1.035);
    await wait(()=>window.clinicalWitness?.complete && window.clinicalWitness.progress===100);
    await wait(()=>window.clinicalWitness?.step>=2,null,90000);
    report.frotis={angle,partial:half.progress,progress:(await read()).progress,nextMission:(await read()).title};
    console.log(device,JSON.stringify(report.frotis));
    await open('8ScenaCirugia');await wait(()=>window.clinicalWitness?.mode==='surgery');
    assert.match(await page.locator('#surgeryTitle').innerText(),/1 de 20/);
    await click('#protocolButton');await wait(()=>window.clinicalWitness?.step===1);
    assert.match(await page.locator('#toolName').innerText(),/acepromacina/i);
    assert.match(await page.locator('#zoneName').innerText(),/detector/i);
    assert.match(await page.locator('#gestureName').innerText(),/arrastra/i);
    await wait(()=>window.clinicalWitness.instrument.z && window.clinicalWitness.site.z);
    const s=await read();report.surgery={step:s.step+1,total:s.total,tool:s.instrument,zone:s.site};
    await page.screenshot({path:path.join(out,'cirugia-'+device+'.png')});
    await xr();assert.equal((await read()).step,1);
    await click('#clinicToggle');assert(!await page.locator('#guidePins').isVisible());
    await open('1ScenaOseo');assert(!await page.locator('#clinical').isVisible());
    assert.equal(errors.length,0,errors.join('\n'));
    console.log(device,JSON.stringify(report.surgery));
    fs.writeFileSync(path.join(out,'guia-'+device+'.json'),JSON.stringify(report,null,2));
  }catch(e){console.error(device,await read().catch(()=>null),await page.locator('#clinical').innerText().catch(()=>''));throw e;}
  finally{await ctx.close();}
}
(async()=>{
  const browser=await chromium.launch({executablePath:process.env.CHROME,args:['--enable-webgl','--enable-unsafe-swiftshader','--use-gl=angle','--use-angle=swiftshader']});
  try{await check(browser,false);await check(browser,true);}finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1});