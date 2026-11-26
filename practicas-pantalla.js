const guides = {
  '0ScenaMenu': ['Elegir una práctica', 'Elige un destino en Escenas. Puedes cambiar de práctica desde el mismo selector.', 'Elige una actividad en la lista vertical o abre Escenas si sostienes el teléfono en horizontal.'],
  '7ScenaMusculo': ['Explorar los músculos', 'Activa los modelos con los botones de la escena. Agarra una pieza para examinarla y usa Girar para ver otra cara.'],
  '4ScenaBoxeo': ['Completar una ronda de boxeo', 'Haz clic en los guantes flotantes y pulsa Empezar. Arrastra para centrar el hueso en la mira. Haz clic para golpear con los guantes alternados. Al terminar, vuelven a sus campos.', 'Toca los guantes flotantes y pulsa Empezar. Desliza para centrar el hueso en la mira. Toca la escena para golpear con los guantes alternados.'],
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

const fichasEscena={ 
  '1ScenaOseo':['Sistema óseo','Explora las piezas del esqueleto canino. Selecciona un hueso y abre de nuevo la información para ver su ficha.'],
  '2ScenaCraneos':['Cráneos caninos','Compara la longitud y el ancho del cráneo. El braquicéfalo es corto y ancho, el mesocéfalo tiene proporciones intermedias y el dolicocéfalo es más alargado.'],
  '5ScenaCerebro':['Encéfalo canino','En el modelo, distingue los hemisferios cerebrales del cerebelo. La corteza cerebral procesa estímulos y el cerebelo participa en el equilibrio y la coordinación del movimiento.'],
  '7ScenaMusculo':['Sistema muscular canino','Gira el modelo para ver los músculos desde distintos ángulos. Observa qué articulaciones cruzan y cómo se relacionan con los huesos que mueven.'],
  '13ScenaAnatomia':['Anatomía canina','Elige un sistema del selector para consultar su función junto al modelo.']
};
const fichasHuesos=[
  [/tibia.*perone|perone/, 'Tibia y peroné', 'Son los huesos largos de la parte inferior del miembro posterior. La tibia soporta la mayor carga y el peroné es más delgado y lateral.'],
  [/tibia/, 'Tibia', 'Está entre la rodilla y el corvejón. Se articula con el fémur por arriba y con el tarso por abajo.'],
  [/lumbar|pelvis|cola/, 'Lumbares, pelvis y cola', 'El sacro conecta la columna con la pelvis. Las vértebras lumbares permiten flexionar el tronco y las caudales forman la cola.'],
  [/vertebra/, 'Vértebras', 'Forman la columna vertebral, protegen la médula espinal y sirven de punto de unión para músculos y ligamentos.'],
  [/costilla/, 'Costillas', 'Forman la caja torácica y ayudan a proteger el corazón y los pulmones. Se articulan con las vértebras torácicas.'],
  [/mandibula/, 'Mandíbula', 'Sostiene los dientes inferiores y se articula con el cráneo en la articulación temporomandibular, que participa en la masticación.'],
  [/craneo|^crane(?:$|[0-9])/, 'Cráneo', 'Protege el encéfalo y da forma a la cara. Entre sus huesos se encuentran el frontal, el maxilar y el occipital.'],
  [/escapula|homoplato/, 'Escápula', 'Es el hueso plano del miembro anterior. Su cavidad glenoidea se articula con el húmero para formar el hombro.'],
  [/radio|cubito/, 'Radio y cúbito', 'Son los huesos del antebrazo, entre el codo y el carpo. El olécranon del cúbito sirve de inserción al tríceps.'],
  [/metacarpo|carpo|mano|falangescarpo/, 'Carpo, metacarpo y falanges', 'Forman parte del extremo del miembro anterior. El metacarpo conecta el carpo con los dedos y las falanges forman los dedos.'],
  [/pata|tarso|metatarso/, 'Tarso, metatarso y falanges', 'Forman el extremo distal del miembro posterior. El tarso participa en el movimiento del corvejón y el metatarso conecta con los dedos.'],
  [/femur/, 'Fémur', 'Es el hueso del muslo. Se articula con la pelvis en la cadera y con la tibia en la rodilla.']
];
const fichasSistemas={
  piel:['Piel','Recubre el cuerpo y forma una barrera entre los tejidos y el ambiente.'],
  muscular:['Sistema muscular','Los músculos esqueléticos mueven las articulaciones y ayudan a mantener la postura.'],
  oseo:['Sistema óseo','Los huesos sostienen el cuerpo, protegen órganos y se unen en las articulaciones para permitir el movimiento.'],
  linfatico:['Sistema linfático','Los vasos linfáticos recogen líquido de los tejidos. Los ganglios filtran la linfa y participan en la defensa del organismo.'],
  digestivo:['Sistema digestivo','El tubo digestivo procesa los alimentos y absorbe nutrientes y agua.'],
  nervioso:['Sistema nervioso','El encéfalo, la médula espinal y los nervios reciben información y coordinan las respuestas del cuerpo.'],
  sistemacirculatorio:['Sistema circulatorio','El corazón impulsa la sangre por los vasos para transportar oxígeno y nutrientes.'],
  corazon:['Corazón','Bombea sangre hacia los pulmones y hacia el resto del cuerpo.'],
  cerebro:fichasEscena['5ScenaCerebro'],
  ojo:['Ojo','La córnea y el cristalino enfocan la luz sobre la retina, donde se inicia la señal visual.']
};
function normalizarAnatomia(texto){
  return (texto||'').normalize('NFD').replace(/[\u0300-\u036f]/g,'').toLowerCase().replace(/\([^)]*\)/g,'').replace(/[^a-z0-9]/g,'');
}
function fichaAnatomica(scene,piece,system){
  if(scene==='13ScenaAnatomia')return fichasSistemas[normalizarAnatomia(system)]||fichasSistemas[normalizarAnatomia(piece)]||fichasEscena[scene];
  const id=normalizarAnatomia(piece);
  if(scene==='2ScenaCraneos'){
    if(id.includes('braqui'))return ['Cráneo braquicéfalo','Tiene un cráneo corto y ancho, con huesos faciales acortados.'];
    if(id.includes('meso'))return ['Cráneo mesocéfalo','Tiene una proporción intermedia entre la longitud y el ancho del cráneo.'];
    if(id.includes('dolico'))return ['Cráneo dolicocéfalo','Su forma es más larga y estrecha que la de los otros tipos de cráneo de la escena.'];
  }
  if((scene==='1ScenaOseo'||scene==='2ScenaCraneos') && id){
    const hit=fichasHuesos.find(([pattern])=>pattern.test(id));
    if(hit && (scene==='1ScenaOseo'||hit[1]==='Mandíbula'))return hit.slice(1);
  }
  return fichasEscena[scene];
}

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
  #respuestasRuleta:not(:empty){display:grid;gap:4px;flex-basis:100%;max-height:39vh;overflow:auto;padding:3px}#respuestasRuleta strong{font-size:15px}#respuestasRuleta button{text-align:left;color:#102d2b;background:#eaf7f2} #miraBoxeo{position:fixed;z-index:1;left:50%;top:50%;width:15px;height:15px;box-sizing:border-box;border:2px solid #8ffce5;border-radius:50%;box-shadow:0 0 0 1px #19252d;transform:translate(-50%,-50%);pointer-events:none}#miraBoxeo[hidden]{display:none}\n  html[data-mode=screen] #practicaPantalla[data-group=boxeo]{position:static;max-width:100%;padding:2px 0 0;background:none;border:0}#soltarPieza[hidden]{display:none} #practicaPantalla select{font-size:16px}
  @media (max-height:510px) and (orientation:landscape){html[data-mode=screen] #boxing:has([data-group=boxeo]){box-sizing:border-box;max-width:calc(100vw - 24px);max-height:36vh;overflow-y:auto;padding:5px 9px}#boxing:has([data-group=boxeo]) h2,#boxing:has([data-group=boxeo]) p{margin:0}#boxing:has([data-group=boxeo]) .score{float:left;margin-right:9px}#boxing:has([data-group=boxeo]) output{clear:both}}
  #infoAnatomia{min-width:44px;padding-inline:.6rem;font-size:1.25rem}
  #fichaAnatomica{position:fixed;z-index:5;left:max(12px,env(safe-area-inset-left));top:max(70px,calc(env(safe-area-inset-top) + 65px));width:min(360px,calc(100vw - 24px));max-height:60vh;overflow:auto;box-sizing:border-box;padding:14px;background:#163939f5;color:#f1faf8;border:1px solid #759c90;border-radius:12px}
  #fichaAnatomica[hidden],#infoAnatomia[hidden]{display:none}
  #fichaAnatomica h2{margin:0 0 6px;font-size:18px}#fichaAnatomica p{margin:0 0 10px;line-height:1.45}
  @media(max-width:520px){#practicaPantalla select{max-width:55vw}#practicaPantalla{max-width:calc(100vw - 16px)}}
  `;
  document.head.append(css);
  const infoButton=document.createElement('button');
  infoButton.type='button';
  infoButton.id='infoAnatomia';
  infoButton.textContent='ⓘ';
  infoButton.title='Información (I)';
  infoButton.setAttribute('aria-label','Información anatómica');
  infoButton.setAttribute('aria-controls','fichaAnatomica');
  infoButton.setAttribute('aria-expanded','false');
  infoButton.hidden=true;
  help.before(infoButton);
  const anatomyCard=document.createElement('section');
  anatomyCard.id='fichaAnatomica';
  anatomyCard.setAttribute('aria-labelledby','fichaAnatomicaTitulo');
  anatomyCard.hidden=true;
  const anatomyTitle=document.createElement('h2');
  anatomyTitle.id='fichaAnatomicaTitulo';
  const anatomyText=document.createElement('p');
  const closeCard=document.createElement('button');
  closeCard.type='button';
  closeCard.textContent='Cerrar';
  anatomyCard.append(anatomyTitle,anatomyText,closeCard);
  document.body.append(anatomyCard);
  let anatomyPiece='',anatomySystem='';
  function updateAnatomyCard(){
    const card=fichaAnatomica(scene,anatomyPiece,anatomySystem);
    if(!card)return;
    anatomyTitle.textContent=card[0];anatomyText.textContent=card[1];
  }
  function showAnatomyCard(open){
    anatomyCard.hidden=!open;
    infoButton.setAttribute('aria-expanded',String(open));
    if(open)updateAnatomyCard();
  }
  function toggleAnatomyCard(){
    if(infoButton.hidden || !flat())return;
    if(help.getAttribute('aria-expanded')==='true')help.click();
    showAnatomyCard(anatomyCard.hidden);
  }
  closeCard.onclick=()=>{showAnatomyCard(false);infoButton.focus({preventScroll:true});};
  infoButton.onclick=toggleAnatomyCard;
  document.addEventListener('veterinaria-inspection',({detail})=>{
    anatomyPiece=detail.mode==='pieza'?detail.target:'';
    if(!anatomyCard.hidden)updateAnatomyCard();
  });
  document.addEventListener('change',e=>{
    if(scene!=='13ScenaAnatomia' || e.target.getAttribute('aria-label')!=='Sistema anatómico')return;
    anatomySystem=e.target.value?e.target.selectedOptions[0]?.textContent:'';
    if(!anatomyCard.hidden)updateAnatomyCard();
  });

  const drop=document.createElement('button');drop.textContent='Soltar';drop.title='Soltar (F)';drop.hidden=true;drop.id='soltarPieza';el('help').before(drop);
  const hands=a=>{if(flat())send('ManosPantalla',a);};drop.onclick=releaseHeld;
  const sight=document.createElement('i');sight.id='miraBoxeo';sight.hidden=true;sight.setAttribute('aria-hidden','true');document.body.append(sight);
  // A short press punches; a drag turns the camera without touching Unity panels behind the ring.
  const ringInput=document.createElement('div');
  ringInput.hidden=true;
  ringInput.setAttribute('aria-label','Boxeo. Arrastra para apuntar y toca para golpear.');
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
    if(ringDrag?.id!==e.pointerId)return;
    if(Math.hypot(e.clientX-ringDrag.x,e.clientY-ringDrag.y)>7)ringDrag.moved=true;
    if(ringDrag.moved)lookBy(e.clientX-ringDrag.lastX,e.clientY-ringDrag.lastY,view.clientHeight);
    ringDrag.lastX=e.clientX;ringDrag.lastY=e.clientY;
  });
  ringInput.addEventListener('pointerup',e=>{
    e.preventDefault();e.stopPropagation();
    if(ringDrag?.id===e.pointerId && !ringDrag.moved){
      const b=view.getBoundingClientRect();
      const x=((e.clientX-b.x)/b.width).toFixed(4);
      const y=(1-(e.clientY-b.y)/b.height).toFixed(4);
      hands('boxingTap,'+x+','+y);
    }
    ringDrag=null;
  });
  ringInput.addEventListener('pointercancel',()=>{ringDrag=null;});
  ringInput.addEventListener('lostpointercapture',()=>{ringDrag=null;});
  let loaded=-1,boxState='',leftButton,rightButton,punchButton,hasGlove=false,quizView,quizKey='',quizUnlock=0,spinWheel,brakeWheel;
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
    infoButton.hidden=true;showAnatomyCard(false);anatomyPiece=anatomySystem='';
    ringInput.hidden=true;ringDrag=null;quizView=spinWheel=brakeWheel=null;quizKey='';quizUnlock=0;
    sight.hidden=true;boxState='';loaded=-1;equipped=false;launcherPoint=tableTap=null;lastTap=null;aim.hidden=true;drop.hidden=true;
    hasGlove=false;leftButton=rightButton=punchButton=null;panel.removeAttribute('data-group');
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
    if(name==='boxeo'){
      ringInput.hidden=false;
      el('boxing').append(panel);
      leftButton=button('Tomar izquierdo',()=>hands('equipL'));
      rightButton=button('Tomar derecho',()=>hands('equipR'));
      punchButton=button('Golpear',()=>hands('punch'));
      punchButton.hidden=true;
      button('Girar ←',()=>hands('turnLeft')).setAttribute('aria-label','Girar a la izquierda');
      button('Girar →',()=>hands('turnRight')).setAttribute('aria-label','Girar a la derecha');
    }
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
    if(currentGroup==='boxeo' && leftButton){
      leftButton.hidden=s.leftGlove;
      rightButton.hidden=s.rightGlove;
      hasGlove=s.leftGlove || s.rightGlove;
      displayCenterPunch();
    }
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
    infoButton.hidden=!fichasEscena[scene];
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
  document.addEventListener('keydown',e=>{if(!flat() || e.repeat || e.altKey || e.ctrlKey || e.metaKey || e.target.closest?.('input,textarea,[contenteditable=true]'))return;
    const k=e.code;
    if(k==='KeyI' && !infoButton.hidden){toggleAnatomyCard();}
    else if(k==='Escape' && !anatomyCard.hidden){showAnatomyCard(false);}
    else if(e.target.closest?.('select'))return;
    else if(k==='KeyF' || k==='KeyR' || k==='Escape'){releaseHeld();}
    else if(k==='ArrowLeft' || k==='ArrowRight'){e.stopImmediatePropagation();hands(k==='ArrowLeft'?'look,-12,0':'look,12,0');}
    else return;e.preventDefault();},{capture:true});
  function displayCenterPunch(){
    const ready=currentGroup==='boxeo' && boxState==='jugando' && hasGlove && flat();
    sight.hidden=!ready;
    if(punchButton)punchButton.hidden=!ready;
  }
  document.addEventListener('veterinaria-boxeo',({detail:s})=>{
    boxState=s.estado;
    ringInput.hidden=!['inicio','cuenta','jugando','fin'].includes(boxState) || !flat();
    if(ringInput.hidden)ringDrag=null;
    for(const b of [leftButton,rightButton])if(b)b.disabled=['fin','pausa','cuenta'].includes(boxState);
    displayCenterPunch();
    if(boxState==='inicio')el('boxingHint').textContent='Toma los guantes flotantes y pulsa Empezar.';
    if(boxState==='jugando')el('boxingHint').textContent=matchMedia('(pointer:coarse)').matches
      ?'Desliza para centrar el hueso. Toca para golpear.'
      :'Arrastra para centrar el hueso. Haz clic para golpear.';
  });
  help.addEventListener('click',()=>{
    if(help.getAttribute('aria-expanded')==='true')showAnatomyCard(false);
    if(!flat() || help.getAttribute('aria-expanded')!=='true')return;
    const g=guides[view.dataset.scene];if(!g)return;
    const box=el('status'),h=box.querySelector('h2'),p=document.createElement('p');
    if(h){hands('guide');for(const li of box.querySelectorAll('li')){if(/Empezar y|Mantén el clic|Mantén un dedo/.test(li.textContent)){if(scene==='4ScenaBoxeo')li.remove();else if(!['8ScenaCirugia','Scena6.1 Quirofano','7ScenaLaboratorioClinico'].includes(scene))li.textContent='Haz clic o toca una pieza para sujetarla. Vuelve a tocar para soltarla, o usa F o Soltar. También puedes arrastrarla y soltarla en un solo gesto.';}}h.textContent=g[0];p.textContent=(matchMedia('(pointer:coarse)').matches && g[2])||g[1];h.after(p);}
  });
  function destinations(){const bridge=levels.querySelector('option[value="EscenaPuente"]');if(bridge)bridge.remove();}
  new MutationObserver(destinations).observe(levels,{childList:true});destinations();
}
