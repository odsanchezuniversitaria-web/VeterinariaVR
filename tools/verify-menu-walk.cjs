// node tools/verify-menu-walk.cjs <url> <evidence-directory>
const {chromium} = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const [url, directory] = process.argv.slice(2);
const horizontalDistance = (a,b) => Math.hypot(a[0]-b[0],a[2]-b[2]);

async function checkMenu(browser,touch) {
  const context = await browser.newContext({
    viewport:touch ? {width:850,height:393} : {width:920,height:560}, isMobile:touch,hasTouch:touch
  });
  try {
    await context.addInitScript({content:fs.readFileSync(process.env.XR_TEST_BUNDLE,'utf8')+
      '\nwindow.qaQuest=new IWER.XRDevice(IWER.metaQuest3);qaQuest.installRuntime({forceInstall:true})'});
    const page=await context.newPage(), poses=[], errors=[];
    page.on('console',message=>{
      const line=message.text();
      const pose=/Vista de pantalla 0ScenaMenu · pies \(([^)]+)\)/.exec(line);
      if(pose)poses.push(pose[1].split(', ').map(Number));
      if(/Exception:|WebGL context lost|out of memory/i.test(line))errors.push(line.slice(0,200));
    });
    page.on('pageerror',e=>errors.push(e.message));
    await page.goto(url,{waitUntil:'domcontentloaded'});
    await page.waitForFunction(()=>document.querySelector('#view')?.dataset.scene==='0ScenaMenu' &&
      document.documentElement.dataset.mode==='screen',null,{timeout:150000});
    const cdp=touch ? await context.newCDPSession(page) : null;
    async function walk(key,ms) {
      if(touch){
        const pad=await page.locator('#stick').boundingBox();
        assert(pad,'La palanca debe estar visible en horizontal');
        const x=pad.x+pad.width/2,y=pad.y+pad.height/2;
        const [dx,dy]={w:[0,-1],s:[0,1],a:[-1,0],d:[1,0]}[key];
        const send=(type,touchPoints)=>cdp.send('Input.dispatchTouchEvent',{type,touchPoints});
        await send('touchStart',[{x,y}]);
        await send('touchMove',[{x:x+dx*pad.width*.38,y:y+dy*pad.height*.38}]);
        await page.waitForTimeout(ms);
        await send('touchEnd',[]);
      }else{
        await page.locator('#view').focus();
        await page.keyboard.down(key);
        await page.waitForTimeout(ms);
        await page.keyboard.up(key);
      }
    }
    // Returning from XR reports the saved screen position through the normal application path.
    async function sample(){
      const count=poses.length;
      await page.locator('#xr').click();
      await page.waitForFunction(()=>document.documentElement.dataset.mode==='xr');
      await page.evaluate(()=>qaQuest.activeSession.end());
      await page.waitForFunction(()=>document.documentElement.dataset.mode==='screen');
      for(let i=0;i<20 && poses.length===count;i++)await page.waitForTimeout(80);
      assert(poses.length>count,'Falta la posición al volver de VR');
      return poses.at(-1);
    }
    const result={start:poses.at(-1)};
    await walk('s',850);result.near=await sample();
    assert(horizontalDistance(result.near,result.start)>.14,'Debe poder caminar con normalidad');
    await walk('s',18000);result.edge=await sample();
    await walk('s',5000);result.further=await sample();
    assert(horizontalDistance(result.edge,result.further)<.18,'Debe detenerse en el borde');
    await walk('w',850);result.return=await sample();
    assert(horizontalDistance(result.return,result.edge)>.1,'Debe poder regresar desde el borde');
    await walk('d',5000);result.right=await sample();
    await walk('a',8000);result.left=await sample();
    assert(result.right[0]-result.left[0]>1,'Debe poder caminar hacia ambos lados');
    await walk('w',5000);result.forward=await sample();
    assert(result.forward[2]-result.start[2]<=.75,'Debe detenerse delante del panel');
    for(const point of Object.values(result)){
      assert(Math.abs(point[1]-result.start[1])<.12,'Debe conservar la altura del piso');
      assert(horizontalDistance(point,result.start)<=2.53,'Debe permanecer cerca del menú');
    }
    assert.equal(await page.locator('#view').getAttribute('data-scene'),'0ScenaMenu');
    assert.deepEqual(errors,[]);
    const device=touch?'movil':'pc';
    fs.writeFileSync(path.join(directory,'despues-'+device+'.json'),JSON.stringify(result,null,2));
    console.log(device,JSON.stringify(result));
  }finally{await context.close();}
}
(async()=>{
  fs.mkdirSync(directory,{recursive:true});
  const browser=await chromium.launch({executablePath:process.env.CHROME,headless:true,
    args:['--enable-webgl','--enable-unsafe-swiftshader','--use-gl=angle','--use-angle=swiftshader']});
  try{await checkMenu(browser,false);await checkMenu(browser,true);}
  finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1});
