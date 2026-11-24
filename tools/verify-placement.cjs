// Exercise assisted placement with ordinary pointer events in the published scene.
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'playwright');
const assert=require('node:assert/strict'),fs=require('node:fs');
const [url,output]=process.argv.slice(2);
(async()=>{
 const browser=await chromium.launch({executablePath:process.env.CHROME,args:['--enable-webgl','--enable-unsafe-swiftshader']});
 const report=[];
 try{
  for(const touch of [false,true]){
   const ctx=await browser.newContext({viewport:{width:920,height:560},deviceScaleFactor:1,isMobile:touch,hasTouch:touch});
   try{
    const p=await ctx.newPage(),cdp=await ctx.newCDPSession(p),errors=[];
    p.on('pageerror',e=>errors.push(e.message));
    const finger=(type,x,y)=>cdp.send('Input.dispatchTouchEvent',{type,touchPoints:x==null?[]:[{id:1,x,y,radiusX:3,radiusY:3}]});
    const down=async(x,y)=>{if(touch)await finger('touchStart',x,y);else{await p.mouse.move(x,y);await p.mouse.down();}};
    const move=(x,y)=>touch?finger('touchMove',x,y):p.mouse.move(x,y,{steps:4});
    const up=()=>touch?finger('touchEnd'):p.mouse.up();
    await p.goto(url,{waitUntil:'domcontentloaded'});
    const wait=id=>p.waitForFunction(id=>document.querySelector('#view')?.dataset.scene===id&&!document.querySelector('#levels').disabled,id,{timeout:360000});
    await wait('0ScenaMenu');await p.selectOption('#levels','1ScenaOseo');await wait('1ScenaOseo');await p.waitForTimeout(800);
    const marker=p.locator('#placementMarker'),toolbar=p.locator('#zoomBar');let hit;
    const spots=[[.37,.55],[.38,.56],[.35,.56],[.4,.53],[.36,.59],[.46,.59],[.52,.62],[.57,.66],[.60,.67],[.50,.70]];
    for(const [x,y] of spots){
     await down(920*x,560*y);await p.waitForTimeout(180);
     if(await marker.isVisible()){hit=[920*x,560*y];break;}await up();
    }
    assert(hit,'El hueso agarrado debe indicar su zona de devolución');
    const name=await toolbar.getAttribute('data-target');
    const dest=await marker.evaluate(m=>({x:parseFloat(m.style.left)*innerWidth/100,y:parseFloat(m.style.top)*innerHeight/100}));
    await move(hit[0]+55,hit[1]-40);await p.waitForTimeout(150);
    await move(dest.x+4,dest.y+3);
    await p.waitForFunction(()=>document.querySelector('#placementMarker').dataset.near==='true',null,{timeout:7000});
    assert.equal(await marker.textContent(),'Soltar aquí');
    await up();await marker.waitFor({state:'hidden'});await p.waitForTimeout(900);
    await down(...hit);await p.waitForTimeout(200);
    assert(await marker.isVisible(),'El hueso debe poder agarrarse otra vez en su posición inicial');
    assert.equal(await toolbar.getAttribute('data-target'),name);await up();
    assert.deepEqual(errors,[]);report.push({device:touch?'Celular':'PC',name,dest,returned:true});
    console.log(JSON.stringify(report.at(-1)));
   }finally{await ctx.close();}
  }
 }finally{await browser.close();fs.writeFileSync(output,JSON.stringify(report,null,2));}
})().catch(e=>{console.error(e);process.exitCode=1});
