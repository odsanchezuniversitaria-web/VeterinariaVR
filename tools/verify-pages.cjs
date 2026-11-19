/* Run against a local preview under /VeterinariaVR/ or the deployed URL.
   PLAYWRIGHT_MODULE and CHROME may be supplied on managed workstations. */
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
(async()=>{
  const base = new URL(process.argv[2]);
  const evidence = [];
  const browser = await chromium.launch({executablePath:process.env.CHROME, headless:true,
    args:['--enable-webgl','--enable-unsafe-swiftshader','--use-gl=angle','--use-angle=swiftshader']});
  try {
    for(const touch of [false,true]){
      const context=await browser.newContext({viewport:touch?{width:390,height:844}:{width:1280,height:800},hasTouch:touch,isMobile:touch});
      const page=await context.newPage();
      const errors=[];
      page.on('pageerror',e=>errors.push(e.message));
      page.on('response',r=>{if(r.url().startsWith(base.href)&&r.status()>=400)errors.push(r.status()+' '+r.url());});
      await page.goto(base.href,{waitUntil:'domcontentloaded'});
      await page.waitForFunction(()=>!!document.querySelector('#view')?.dataset.scene,null,{timeout:180000});
      const names=await page.locator('#levels option').evaluateAll(opts=>opts.map(o=>o.value));
      assert.equal(names.length,18);
      for(const scene of names){
        await page.selectOption('#levels',scene);
        await page.waitForFunction(name=>document.querySelector('#view').dataset.scene===name && !document.querySelector('#levels').disabled,scene,{timeout:150000});
        const canvas=await page.locator('#view').boundingBox();assert.ok(canvas.width>0&&canvas.height>0);
        if(touch)await page.touchscreen.tap(canvas.width*.60,canvas.height*.46);
        else{await page.keyboard.down('w');await page.waitForTimeout(150);await page.keyboard.up('w');}
        evidence.push({touch,scene});
        console.log(touch?'Táctil':'Escritorio',scene);
      }
      assert.deepEqual(errors,[]);
      await context.close();
    }
  } finally {
    if(process.argv[3])fs.writeFileSync(process.argv[3],JSON.stringify(evidence,null,2));
    await browser.close();
  }
})().catch(e=>{console.error(e);process.exitCode=1});

