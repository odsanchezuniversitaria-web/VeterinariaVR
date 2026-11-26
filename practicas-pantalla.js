const guides = {
  '0ScenaMenu': ['Elegir una práctica', 'Elige un destino en Escenas. Puedes cambiar de práctica desde el mismo selector.', 'Elige una actividad en la lista vertical o abre Escenas si sostienes el teléfono en horizontal.'],
  '7ScenaMusculo': ['Explorar los músculos', 'Activa los modelos con los botones de la escena. Agarra una pieza para examinarla y usa Girar para ver otra cara.'],
  '4ScenaBoxeo': ['Completar una ronda de boxeo', 'Haz clic en los guantes flotantes y pulsa Empezar. Arrastra sobre la escena o usa los botones de giro para recorrer el círculo de huesos. Apunta al hueso indicado y golpea con Q o E. Al terminar, los guantes vuelven a sus campos.', 'Toca cada campo para ponerte el guante. Pulsa Empezar. Desliza a un lado o usa los botones de giro para buscar los huesos a tu alrededor. Toca el hueso y pulsa Izquierda o Derecha para golpear.'],
  '2ScenaCraneos': ['Comparar los cráneos', 'Abre los paneles de la escena y selecciona un cráneo para moverlo, ampliarlo o girarlo. Usa ↺ para recuperar su orientación.'],
  '1ScenaOseo': ['Examinar y devolver huesos', 'Selecciona un hueso y mantenlo agarrado para examinarlo. Acércalo a la marca de devolución y suelta cuando aparezca «Soltar aquí».'],
  '6ScenaTodoRuleta': ['Girar y responder', 'Pulsa Girar y luego Frenar. Cuando aparezca una pregunta, elige una de las respuestas que aparecen bajo los controles. La ronda consta de cuatro preguntas.'],
  '5ScenaCerebro': ['Inspeccionar el cerebro', 'Selecciona una parte y arrástrala para verla de cerca. Usa Girar o abre los paneles para identificar las estructuras.'],
  '10ScenaReflejoPerro': ['Observar la respuesta al contacto', 'Agarra la herramienta y acércala a la zona del perro. Mantén el contacto para observar la respuesta de la escena.'],
  'Scena6.1 Quirofano': ['Reconocer el equipo de quirófano', 'Examina los monitores y las conexiones. Selecciona los paneles para responder y usa los controles de Vista para leer de cerca.'],
  '7ScenaLaboratorioClinico': ['Seguir las misiones de laboratorio', 'Abre Mesa de frotis para trabajar sobre la lámina. Sigue la misión que aparece tras el extendido y consulta Acciones para mostrar el panel de la escena.'],
  'escena resortera': ['Practicar el lanzamiento', 'Haz clic en la resortera para equiparla. Las bolas se cargan solas. Sobre el blanco, arrastra hacia abajo y suelta. Pulsa F o haz doble clic para devolverla.', 'Toca la resortera. Las bolas se cargan solas. Arrastra hacia abajo sobre el blanco y suelta. Toca dos veces o pulsa Soltar resortera para devolverla.'],
  '3ScenaResortera': ['Acertar en el blanco anatómico', 'Inicia la ronda y toca o haz clic en la resortera. Las bolas de la mesa se cargan en orden aleatorio. Arrastra hacia abajo sobre el blanco indicado y suelta. Un acierto suma 10 puntos y un error resta 5.'],
  '8ScenaCirugia': ['Seguir el protocolo de cirugía', 'Abre Guía de cirugía. El indicador ① señala el instrumento y ② la zona. Sigue el gesto del paso activo. El aumento cambia la vista.'],
  '11ScenaVaca': ['Auscultar la vaca', 'Usa Vista de detalle para acercarte y Vista inicial para volver. Elige una zona para apoyar el estetoscopio. Compara pulmón, corazón y rumen; pulsa Retirar para apartarlo y escuchar la diferencia.'],
  '13ScenaAnatomia': ['Explorar los sistemas del perro', 'Elige un sistema por nombre. Agarra una pieza para verla de cerca, gírala y devuélvela al detector al terminar.'],
  '17ScenaVacaItis': ['Recorrer las fases de laminitis', 'Usa Anterior y Siguiente para comparar fases. En la fase de inspección, sujeta la pezuña, observa la suela y suéltala antes de avanzar.'],
  '9ScenaElementoP': ['Completar el equipo y clasificar residuos', 'Consulta el rol activo. Arrastra la prenda a la zona compatible del maniquí y clasifica los residuos en la caneca correspondiente. La escena registra errores, tiempo y racha.'],
  'EscenaPuente': ['Cambio de práctica', 'Esta sala se utiliza durante la carga. Selecciona un destino en Escenas si entraste directamente.']
};

export function install(send) {
  const el = id => document.getElementById(id);
  const view=el('view'),levels=el('levels'),help=el('help');
  const panel=document.createElement('section');
  panel.id='practicaPantalla';panel.hidden=true;panel.setAttribute('aria-label','Controles de la práctica');
  const controls=document.createElement('div'),message=document.createElement('output');
  message.setAttribute('aria-live','polite'); panel.append(controls,message);
  const aim=document.createElement('div');aim.id='punteriaPantalla';aim.hidden=true;aim.tabIndex=0;
  aim.setAttribute('aria-label','Apuntar. Arrastra hacia abajo y suelta. F o doble toque devuelve la resortera.');
  const mark=document.createElement('span'),power=document.createElement('progress');
  mark.setAttribute('aria-hidden','true');power.hidden=true;power.max=100;power.value=0;power.setAttribute('aria-label','Intensidad del lanzamiento');
  aim.append(mark,power);document.body.append(aim,panel);
  const css=document.createElement('style');
  css.textContent=`
  #practicaPantalla{position:fixed;top:max(8px,env(safe-area-inset-top));left:max(8px,env(safe-area-inset-left));max-width:min(510px,calc(100vw - 16px));box-sizing:border-box;z-index:2;padding:6px;background:#143532f2;color:white;border:1px solid #759c90;border-radius:10px}
  #practicaPantalla>div{display:flex;align-items:center;gap:4px;flex-wrap:wrap}#practicaPantalla button,#practicaPantalla select{min-height:44px;max-width:100%}#practicaPantalla output:not(:empty){display:block;padding:4px 5px;font-size:14px}#practicaPantalla [aria-pressed=true]{background:#a2e2c6;color:#143532}
  #punteriaPantalla{position:fixed;inset:0;z-index:1;touch-action:none;user-select:none;cursor:crosshair}#punteriaPantalla span{display:none;position:absolute;width:26px;height:26px;border:2px solid white;box-shadow:0 0 0 2px #193d39;border-radius:50%;transform:translate(-50%,-50%);pointer-events:none}
  #punteriaPantalla progress{position:absolute;bottom:26%;left:35%;width:30%;pointer-events:none;accent-color:#70dfbe}#punteriaPantalla[hidden],#practicaPantalla[hidden],html[data-mode=xr] #practicaPantalla,html[data-mode=xr] #punteriaPantalla{display:none}
  #respuestasRuleta:not(:empty){display:grid;gap:4px;flex-basis:100%;max-height:39vh;overflow:auto;padding:3px}#respuestasRuleta strong{font-size:15px}#respuestasRuleta button{text-align:left;color:#102d2b;background:#eaf7f2}#miraBoxeo{position:fixed;z-index:1;width:15px;height:15px;box-sizing:border-box;border:2px solid #8ffce5;border-radius:50%;box-shadow:0 0 0 1px #19252d;transform:translate(-50%,-50%);pointer-events:none}#miraBoxeo[hidden]{display:none}\n  html[data-mode=screen] #practicaPantalla[data-group=boxeo]{position:static;max-width:100%;padding:2px 0 0;background:none;border:0}#soltarPieza[hidden]{display:none} #practicaPantalla select{font-size:16px}
  @media (max-height:510px) and (orientation:landscape){html[data-mode=screen] #boxing:has([data-group=boxeo]){box-sizing:border-box;max-width:calc(100vw - 24px);max-height:36vh;overflow-y:auto;padding:5px 9px}#boxing:has([data-group=boxeo]) h2,#boxing:has([data-group=boxeo]) p{margin:0}#boxing:has([data-group=boxeo]) .score{float:left;margin-right:9px}#boxing:has([data-group=boxeo]) output{clear:both}}
  @media(max-width:520px){#practicaPantalla select{max-width:55vw}#practicaPantalla{max-width:calc(100vw - 16px)}}
  `;
  document.head.append(css);
  const drop=document.createElement('button');drop.textContent='Soltar';drop.title='Soltar (F)';drop.hidden=true;drop.id='soltarPieza';el('help').before(drop);
  const hands=a=>{if(flat())send('ManosPantalla',a);};drop.onclick=releaseHeld;
  const sight=document.createElement('i');sight.id='miraBoxeo';sight.hidden=true;sight.setAttribute('aria-hidden','true');document.body.append(sight);
  // During a round, tapping a bone must not also activate a Unity world-space menu behind it.
  const ringInput=document.createElement('div');
  ringInput.hidden=true;
  ringInput.setAttribute('aria-label','Apuntar a los huesos');
  ringInput.style.cssText='position:fixed;inset:0;z-index:1;touch-action:none;user-select:none;cursor:crosshair';
  document.body.insertBefore(ringInput,sight);
  let ringDrag=null;
  function lookBy(dx,dy,height){
    const scale=100/Math.max(320,height);
    hands('look,'+(dx*scale).toFixed(3)+','+(-dy*scale).toFixed(3));
  }
  ringInput.oncontextmenu=e=>e.preventDefault();
  ringInput.addEventListener('pointerdown',e=>{
    if(ringDrag || (e.button!==0 && e.button!==2))return;
    e.preventDefault();e.stopPropagation();
    ringDrag={id:e.pointerId,x:e.clientX,y:e.clientY,lastX:e.clientX,lastY:e.clientY,moved:e.button===2};
    ringInput.setPointerCapture(e.pointerId);
  });
  ringInput.addEventListener('pointermove',e=>{
    e.stopPropagation();
    if(!ringDrag){if(e.pointerType==='mouse')point(e);return;}
    if(ringDrag.id!==e.pointerId)return;
    if(Math.hypot(e.clientX-ringDrag.x,e.clientY-ringDrag.y)>7)ringDrag.moved=true;
    if(ringDrag.moved){lookBy(e.clientX-ringDrag.lastX,e.clientY-ringDrag.lastY,view.clientHeight);sight.hidden=true;}
    ringDrag.lastX=e.clientX;ringDrag.lastY=e.clientY;
  });
  ringInput.addEventListener('pointerup',e=>{
    e.preventDefault();e.stopPropagation();
    if(ringDrag?.id===e.pointerId && !ringDrag.moved)point(e,true);
    ringDrag=null;
  });
  ringInput.addEventListener('pointercancel',()=>{ringDrag=null;});
  ringInput.addEventListener('lostpointercapture',()=>{ringDrag=null;});
  let loaded=-1,lastAim='',boxState='',leftButton,rightButton,quizView,quizKey='',quizUnlock=0,spinWheel,brakeWheel;
  let scene='',currentGroup='',gesture=null,equipButton,roundButton,score,equipped=false,lastTap=null,poll,handshake;
  let launcherPoint, tableTap;
  view.addEventListener('pointerdown',e=>{
    if(!flat() || currentGroup!=='resortera' || equipped || !(launcherPoint?.z>0) || e.button!==0)return;
    const rect=view.getBoundingClientRect();
    const distance=Math.hypot(e.clientX-rect.left-launcherPoint.x*rect.width,e.clientY-rect.top-(1-launcherPoint.y)*rect.height);
    if(distance<Math.min(68,Math.max(35,rect.height*.096)))tableTap={id:e.pointerId,x:e.clientX,y:e.clientY};
  });
  view.addEventListener('pointerup',e=>{
    const tap=tableTap;tableTap=null;
    if(tap?.id===e.pointerId && !equipped && Math.hypot(e.clientX-tap.x,e.clientY-tap.y)<12)command('equip');
  });
  view.addEventListener('pointercancel',()=>{tableTap=null;});
  const flat=()=>window.veterinariaPracticasPantalla && document.documentElement.dataset.mode==='screen' && !levels.disabled;
  function command(action){if(flat())send('ScreenPractice',action);}
  function button(label,fn){const b=document.createElement('button');b.type='button';b.textContent=label;b.onclick=fn;controls.append(b);return b;}
  function picker(label,names,changed){
    const s=document.createElement('select');s.setAttribute('aria-label',label);for(const [i,n] of names.entries())s.add(new Option(n.replaceAll('_',' '),String(i)));
    s.onchange=()=>changed(Number(s.value));controls.append(s);return s;
  }
  function dropGesture(){
    const last=gesture;
    gesture=null;
    mark.style.display='none';
    power.value=0;
    power.hidden=true;
    if(last && aim.hasPointerCapture(last.id))aim.releasePointerCapture(last.id);
  }
  function endAim(){
    if(gesture)hands('pull,0');
    dropGesture();
  }
  function releaseHeld(){
    endAim();
    lastTap=null;
    hands('drop');
  }
  function reset(){
    ringInput.hidden=true;ringDrag=null;quizView=spinWheel=brakeWheel=null;quizKey='';quizUnlock=0;
    sight.hidden=true;boxState='';loaded=-1;equipped=false;launcherPoint=tableTap=null;lastTap=null;aim.hidden=true;drop.hidden=true;
    lastAim='';leftButton=rightButton=null;panel.removeAttribute('data-group');
    if(panel.parentNode!==document.body)document.body.append(panel);
    clearInterval(poll);clearInterval(handshake);currentGroup='';endAim();controls.replaceChildren();
    message.textContent='';panel.hidden=true;equipButton=roundButton=score=null;
  }
  aim.oncontextmenu=e=>e.preventDefault();
  aim.onpointerdown=e=>{
    if(!flat() || !equipped || gesture || (e.button!==0 && e.button!==2))return;
    e.preventDefault();
    gesture={id:e.pointerId,x:e.clientX,y:e.clientY,lastX:e.clientX,lastY:e.clientY,
      mode:e.button===2?'look':null,r:aim.getBoundingClientRect()};
    aim.setPointerCapture(e.pointerId);
  };
  function strength(y,g){return Math.max(0,Math.min(1,(y-g.y)/Math.max(65,Math.min(150,g.r.height*.23))));}
  aim.onpointermove=e=>{
    const g=gesture;
    if(g?.id!==e.pointerId)return;
    const dx=e.clientX-g.x,dy=e.clientY-g.y;
    if(!g.mode && Math.hypot(dx,dy)>=10){
      g.mode=dy>0 && Math.abs(dy)>Math.abs(dx) && loaded>=0?'launch':'look';
      if(g.mode==='launch')mark.style.cssText='display:block;left:'+(g.x-g.r.left)+'px;top:'+(g.y-g.r.top)+'px';
    }
    if(g.mode==='launch'){
      const f=strength(e.clientY,g);
      power.hidden=false;
      power.value=f*100;
      hands('pull,'+f.toFixed(3));
    }else if(g.mode==='look')lookBy(e.clientX-g.lastX,e.clientY-g.lastY,g.r.height);
    g.lastX=e.clientX;g.lastY=e.clientY;
  };
  aim.onpointerup=e=>{
    const g=gesture;
    if(g?.id!==e.pointerId)return;
    const f=strength(e.clientY,g);
    const x=(g.x-g.r.left)/g.r.width,y=1-(g.y-g.r.top)/g.r.height;
    dropGesture();
    if(g.mode==='launch'){
      lastTap=null;
      if(f>0.08)command(['launch',x.toFixed(5),y.toFixed(5),f.toFixed(3)].join(','));
      hands('pull,0');
    }else if(!g.mode){
      const now=performance.now();
      if(lastTap && now-lastTap.time<380 && Math.hypot(g.x-lastTap.x,g.y-lastTap.y)<35){
        releaseHeld();
      }else{
        lastTap={time:now,x:g.x,y:g.y};
        hands('panel,'+x.toFixed(5)+','+y.toFixed(5));
      }
    }
  };
  aim.onpointercancel=aim.onlostpointercapture=e=>{if(gesture?.id===e.pointerId)endAim();};
  function cancelPointer(){endAim();ringDrag=null;lastTap=null;hands('cancel');}
  window.addEventListener('blur',cancelPointer);
  document.addEventListener('visibilitychange',()=>{if(document.hidden)cancelPointer();});
  function group(name,choices){
    controls.replaceChildren();currentGroup=name;panel.hidden=!name;panel.dataset.group=name;
    if(name==='boxeo'){ringInput.hidden=false;el('boxing').append(panel);leftButton=button('Tomar izquierdo',()=>hands(leftButton.dataset.worn==='true'?'hitL':'equipL'));rightButton=button('Tomar derecho',()=>hands(rightButton.dataset.worn==='true'?'hitR':'equipR'));button('Girar ←',()=>hands('turnLeft')).setAttribute('aria-label','Girar a la izquierda');button('Girar →',()=>hands('turnRight')).setAttribute('aria-label','Girar a la derecha');}
    if(name==='vaca' || name==='reflejo'){button('Vista de detalle',()=>hands('detail'));button('Vista inicial',()=>hands('overview'));}
    if(name==='ruleta'){spinWheel=button('Girar',()=>{quizKey='';quizView.replaceChildren();quizUnlock=Infinity;spinWheel.disabled=true;brakeWheel.disabled=false;command('spin');});brakeWheel=button('Frenar',()=>{brakeWheel.disabled=true;quizUnlock=performance.now()+12500;command('brake');});brakeWheel.disabled=true;quizView=document.createElement('div');quizView.id='respuestasRuleta';controls.append(quizView);poll=setInterval(()=>command('state'),350);}
    if(name==='vaca'){
      for(const [i,n] of choices.entries())button(n==='Pulmon'?'Pulmón':n==='Corazon'?'Corazón':n,()=>command('zone,'+i));
      button('Retirar',()=>command('stop'));
    }
    if(name==='anatomia'){const systems=picker('Sistema anatómico',choices,i=>command('system,'+i));systems.add(new Option('Ver sistema…','',true,true),0);systems.options[0].disabled=true;}
    if(name==='resortera'){
      if(scene==='3ScenaResortera')roundButton=button('Iniciar ronda',()=>command('start'));
      equipButton=button('Tomar resortera',()=>command('equip'));
      if(roundButton){score=document.createElement('span');score.setAttribute('aria-label','Puntaje');controls.append(score);}
      poll=setInterval(()=>{if(roundButton || !equipped)command('state');},480);
    }
    if(name==='laminitis'){button('Anterior',()=>send('ScreenAction','prev'));button('Siguiente',()=>send('ScreenAction','next'));}
  }
  document.addEventListener('veterinaria-practica',({detail:s})=>{
    if(!flat() || s.scene!==scene)return;
    clearInterval(handshake);
    if(s.group && currentGroup!==s.group)group(s.group,s.choices||[]);
    drop.hidden=!s.held && !s.launcherEquipped;
    drop.textContent=s.launcherEquipped?'Soltar resortera':'Soltar';
    if(s.message==='Nilo · guía'){if(help.getAttribute('aria-expanded')!=='true')help.click();return;}
    if(currentGroup==='boxeo' && leftButton){for(const [b,isWorn,side,hit] of [[leftButton,s.leftGlove,'izquierdo','Izquierda · Q'],[rightButton,s.rightGlove,'derecho','Derecha · E']]){b.dataset.worn=String(isWorn);b.textContent=isWorn?(matchMedia('(pointer:coarse)').matches?hit.split(' · ')[0]:hit):'Tomar '+side;}}
    if(currentGroup==='resortera'){
      launcherPoint=s.launcherPoint;
      equipped=!!s.launcherEquipped;
      aim.hidden=!equipped;
      equipButton.hidden=equipped;
      if(!equipped)endAim();
      loaded=s.loadedBall;
    }
    if(currentGroup==='ruleta' && quizView && (s.finish || performance.now()>=quizUnlock)){
      spinWheel.disabled=!!s.feedback||!!s.finish;
      const key=JSON.stringify([s.question,s.answers,s.feedback,s.finish]);if(key!==quizKey){quizKey=key;quizView.replaceChildren();
        if(s.question || s.finish){const q=document.createElement('strong');q.textContent=s.finish||s.question;quizView.append(q);if(s.question && !s.feedback && !s.finish)message.textContent='Elige una respuesta';}
        if(s.question && !s.finish){for(const [i,answer] of (s.answers||[]).entries()){if(!answer)continue;const b=document.createElement('button');b.textContent=answer;b.disabled=!!s.feedback;b.onclick=()=>command('answer,'+i);quizView.append(b);}}
        if(s.finish || s.feedback)message.textContent=s.finish||s.feedback;
      }}
    if(roundButton){roundButton.disabled=s.playing;score.textContent=s.score+' pts';}
    if(s.message)message.textContent=s.message;
  });
  function enter(id){
    reset();scene=id;
    if(!flat())return;
    if(scene==='10ScenaReflejoPerro'){group('reflejo',[]);return;}
    if(scene==='17ScenaVacaItis'){group('laminitis',[]);return;}
    if(!['4ScenaBoxeo','6ScenaTodoRuleta','11ScenaVaca','13ScenaAnatomia','escena resortera','3ScenaResortera'].includes(scene))return;
    // A scene can announce Start before the flat camera enters its first Update.
    let tries=0;handshake=setInterval(()=>{if(++tries>20||!flat())clearInterval(handshake);else command('open');},150);
  }
  document.addEventListener('veterinaria-scene',e=>enter(e.detail));
  new MutationObserver(()=>{if(document.documentElement.dataset.mode==='xr')reset();else if(view.dataset.scene)enter(view.dataset.scene);})
    .observe(document.documentElement,{attributes:true,attributeFilter:['data-mode']});
  levels.addEventListener('change',reset);
  function point(e,markTarget=false){if(currentGroup!=='boxeo' || !flat())return;const b=view.getBoundingClientRect();sight.hidden=boxState!=='jugando';sight.style.left=e.clientX+'px';sight.style.top=e.clientY+'px';const a=(markTarget || e.type==='pointerdown'?'mark,':'aim,')+((e.clientX-b.x)/b.width).toFixed(3)+','+(1-(e.clientY-b.y)/b.height).toFixed(3);if(lastAim!==a || a.startsWith('mark,')){hands(a);lastAim=a;}}
  view.addEventListener('pointermove',point);view.addEventListener('pointerdown',point);
  document.addEventListener('keydown',e=>{if(!flat() || e.repeat || e.altKey || e.ctrlKey || e.metaKey || e.target.closest?.('input,textarea,select,[contenteditable=true]'))return;
    const k=e.code;if(k==='KeyF' || k==='KeyR' || k==='Escape'){releaseHeld();}
    else if(k==='ArrowLeft' || k==='ArrowRight'){e.stopImmediatePropagation();hands(k==='ArrowLeft'?'look,-12,0':'look,12,0');}
    else if(currentGroup==='boxeo' && (k==='KeyQ'||k==='KeyE')){hands(k==='KeyQ'?'hitL':'hitR');}else return;e.preventDefault();},{capture:true});
  document.addEventListener('veterinaria-boxeo',({detail:s})=>{boxState=s.estado;ringInput.hidden=!['inicio','cuenta','jugando','fin'].includes(boxState) || !flat();if(ringInput.hidden)ringDrag=null;if(boxState!=='jugando')sight.hidden=true;for(const b of [leftButton,rightButton])if(b)b.disabled=['fin','pausa','cuenta'].includes(boxState);if(['inicio','jugando'].includes(s.estado))el('boxingHint').textContent=s.estado==='inicio'?'Toca los guantes flotantes y pulsa Empezar.':matchMedia('(pointer:coarse)').matches?'Toca un hueso para marcarlo y pulsa Izquierda o Derecha.':'Apunta al hueso y golpea con Q o E.';});
  help.addEventListener('click',()=>{
    if(!flat() || help.getAttribute('aria-expanded')!=='true')return;
    const g=guides[view.dataset.scene];if(!g)return;
    const box=el('status'),h=box.querySelector('h2'),p=document.createElement('p');
    if(h){hands('guide');for(const li of box.querySelectorAll('li')){if(/Empezar y|Mantén el clic|Mantén un dedo/.test(li.textContent)){if(scene==='4ScenaBoxeo')li.remove();else if(!['8ScenaCirugia','Scena6.1 Quirofano','7ScenaLaboratorioClinico'].includes(scene))li.textContent='Haz clic o toca una pieza para sujetarla. Vuelve a tocar para soltarla, o usa F o Soltar. También puedes arrastrarla y soltarla en un solo gesto.';}}h.textContent=g[0];p.textContent=(matchMedia('(pointer:coarse)').matches && g[2])||g[1];h.after(p);}
  });
  function destinations(){const bridge=levels.querySelector('option[value="EscenaPuente"]');if(bridge)bridge.remove();}
  new MutationObserver(destinations).observe(levels,{childList:true});destinations();
}
