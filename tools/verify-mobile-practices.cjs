// node tools/verify-mobile-practices.cjs <Pages URL> <results.json>
const fs=require('node:fs');
const assert=require('node:assert/strict');
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'playwright');
const [base,output]=process.argv.slice(2);
const anatomy='13ScenaAnatomia',surgery='8ScenaCirugia',theatre='Scena6.1 Quirofano',menu='0ScenaMenu';
const report={};
(async()=>{
    const browser=await chromium.launch({executablePath:process.env.CHROME,args:['--enable-webgl','--enable-unsafe-swiftshader']});
    try{
        for(const mode of ['celular','PC',...(process.env.XR_TEST_BUNDLE?['Quest']:[])]){
            const mobile=mode!=='PC';
            const ctx=await browser.newContext({viewport:{width:920,height:560},deviceScaleFactor:mobile?2:1,hasTouch:mobile,isMobile:mobile});
            if(mode==='Quest')await ctx.addInitScript({content:fs.readFileSync(process.env.XR_TEST_BUNDLE,'utf8')+
                '\nwindow.testHeadset=new IWER.XRDevice(IWER.metaQuest3);testHeadset.installRuntime({forceInstall:true});'});
            const page=await ctx.newPage(),requests=[],failures=[],states=[];
            const result=report[mode]={};
            page.on('request',r=>{const u=new URL(r.url());if(u.pathname.includes('/Content/') || u.pathname.includes('/Media/'))requests.push(u.pathname);});
            page.on('response',r=>{if(r.url().startsWith(base)&&r.status()>=400)failures.push(r.status()+' '+r.url());});
            page.on('pageerror',e=>failures.push(e.message));
            page.on('console',msg=>{const t=msg.text();if(t.includes('[Arranque]'))states.push(t);
              if(/\w+Exception:|out of memory|memory access out of bounds|WebGL context lost|Couldn't open archive|Unable to load asset/i.test(t))failures.push(t.slice(0,400));});
            const wait=id=>page.waitForFunction(id=>document.querySelector('#view')?.dataset.scene===id&&!document.querySelector('#levels').disabled,id,{timeout:360000});
            async function open(id){
                const list=page.locator('#mobileLevels');
                if(await list.isVisible()){
                    const label=await page.locator('#levels option').evaluateAll((opts,id)=>opts.find(o=>o.value===id).text,id);
                    await list.getByRole('button',{name:label,exact:true}).tap();
                }else await page.selectOption('#levels',id);
                await wait(id);await page.waitForTimeout(800);
            }
            try{
                await page.goto(base,{waitUntil:'domcontentloaded'});await wait(menu);requests.length=0;
                await open(anatomy);
                assert.equal(await page.evaluate(()=>window.veterinariaPhone),mode==='celular');
                const pick=page.getByRole('combobox',{name:'Sistema anatómico'});
                if(mode==='celular'){
                    assert(requests.some(s=>s.endsWith('/movil-nivel-14')),'Cargar la variante móvil');
                    assert.equal(await pick.locator('option').count(),11);
                    const before=requests.filter(s=>s.includes('/geo-anatomia-'));
                    await pick.selectOption({label:'Oseo'});
                    await page.waitForTimeout(2200);
                    const first=requests.filter(s=>s.includes('/geo-anatomia-'));
                    assert(first.length>before.length && first.length<10,'La primera selección carga solo la geometría que utiliza');
                    assert(states.some(s=>s.includes('Encendidos 1 sistemas')),'Iniciar con un sistema');
                    await pick.selectOption({label:'Muscular'});
                    await page.waitForTimeout(1800);
                    const second=requests.filter(s=>s.includes('/geo-anatomia-'));
                    assert(second.length>first.length,'Se solicita el siguiente sistema al seleccionarlo');
                    result.anatomia={alEntrar:before,primero:first,siguiente:second};
                }else{
                    assert(requests.some(s=>s.endsWith('/nivel-14')));
                    assert(!requests.some(s=>s.includes('/geo-')||s.includes('/movil-nivel')));
                    assert(!await pick.isVisible());
                    if(mode==='Quest'){
                        await page.locator('#xr').click();await page.waitForFunction(()=>document.documentElement.dataset.mode==='xr');
                        const views=await page.evaluate(async()=>{const s=testHeadset.activeSession;const ref=await s.requestReferenceSpace('local');
                            return new Promise(resolve=>s.requestAnimationFrame((_,frame)=>resolve(frame.getViewerPose(ref).views.length)));});
                        assert.equal(views,2);await page.evaluate(()=>testHeadset.activeSession.end());await page.waitForFunction(()=>document.documentElement.dataset.mode==='screen');
                        result.webXR={views};
                    }
                }
                requests.length=0;await open(surgery);
                assert(requests.some(s=>s.endsWith(mode==='celular'?'/movil-nivel-12':'/nivel-12')));
                result.cirugia={paquetes:[...new Set(requests.filter(s=>s.includes('/Content/')))],geometria:requests.filter(s=>s.includes('/geo-cirugia'))};
                await open(menu);requests.length=0;await open(theatre);await page.waitForTimeout(2500);
                if(mode==='celular' && !requests.some(s=>s.includes('/Media/Movil/'))){
                    const cdp=await ctx.newCDPSession(page);
                    for(let turn=0;turn<20 && !requests.some(s=>s.includes('/Media/Movil/'));turn++){
                        for(const [type,x] of [['touchStart',745],['touchMove',675],['touchMove',570],['touchMove',455],['touchEnd',0]]){
                            await cdp.send('Input.dispatchTouchEvent',{type,touchPoints:x?[{id:1,x,y:280}]:[]});await page.waitForTimeout(85);
                        }
                        await page.waitForTimeout(550);
                    }
                }
                result.quirofano={videos:[...new Set(requests.filter(s=>s.includes('/Media/')))]};
                if(mode==='celular')assert(result.quirofano.videos.some(s=>s.includes('/Media/Movil/')),'Al mirar un monitor debe cargarse su señal móvil');
                if(mode==='celular')assert(!result.quirofano.videos.some(s=>/\/Media\/(ad399131|9548baaf|1e218664|d7caee55|99f8e03d)/.test(s)));
                else assert(!result.quirofano.videos.some(s=>s.includes('/Movil/')));
                await open(menu);requests.length=0;await open(theatre);
                result.reentrada={solicitudes:requests.filter(s=>s.includes('/Content/')).length};
                assert.deepEqual(failures,[]);
                console.log(mode,JSON.stringify(result));
            }finally{await ctx.close();}
        }
    }finally{fs.writeFileSync(output,JSON.stringify(report,null,2));await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
