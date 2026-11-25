(() => {
  const get=id=>document.getElementById(id);
  const panel=get('clinical'),body=get('clinicBody'),toggle=get('clinicToggle'),pins=get('guidePins');
  const slider=get('slideAngle'),rail=get('slideRail'),diagram=get('smearDiagram'),cue=get('labCue');
  const touch=matchMedia('(pointer:coarse)');
  // Indices follow the authored 20-step surgical protocol. Substeps select a tool and its matching marker.
  const actions=[
    [['Panel Empezar guía','Entrada del quirófano','Activa el botón para iniciar la práctica.']],
    [['Frasco de acepromacina','Campo detector de la mesa','Arrastra el frasco marcado hasta el detector y mantenlo allí.']],
    [['Panel de cálculo','Panel de preguntas','Selecciona la respuesta en el panel de la escena.']],
    [['Enchufe','Toma de la máquina','Arrastra el enchufe al conector.'],['Tubo de oxígeno','Conexión de oxígeno','Lleva el extremo del tubo a su toma.'],['Válvula del tanque','Tanque de oxígeno','Acciona la perilla y observa el flujómetro.']],
    [['Mascarilla','Indicador en el hocico','Lleva la mascarilla al indicador y suéltala al encajar.']],
    [['Pinza del monitor','Conector señalado del paciente','Lleva la primera pinza a su marca.'],['Pinza del monitor','Conector señalado del paciente','Lleva la segunda pinza a su marca.'],['Pinza del monitor','Conector señalado del paciente','Lleva la tercera pinza a su marca.']],
    [['Cabeza de la lámpara','Campo quirúrgico','Arrastra la lámpara para orientar la luz y pulsa Siguiente.']],
    [['Pinzas Forester','Marca del campo quirúrgico','Arrastra las pinzas al punto señalado hasta que encajen.']],
    [['Bisturí','Siguiente punto del trazado','Mantén el agarre y pasa la punta por las marcas en orden.']],
    [['Gasa','Manchas visibles','Desliza la gasa sobre cada mancha hasta que desaparezca.']],
    [['Presión con el puntero','Zona de presión marcada','Mantén pulsada la zona hasta que responda el modelo.']],
    [['Bisturí','Siguiente marca de la capa','Recorre las marcas en orden con la punta.']],
    [['Pinzas de sutura','Inicio de la primera ligadura','Lleva las pinzas al inicio del nudo.'],['Portaagujas','Siguiente punto del nudo','Sigue las marcas en orden con el portaagujas.']],
    [['Pinzas de sutura','Inicio de la segunda ligadura','Coloca las pinzas en el inicio.'],['Portaagujas','Siguiente punto del nudo','Recorre con la punta los puntos señalados.']],
    [['Pieza liberada','Bandeja de la práctica','Arrastra la pieza hasta la bandeja y suéltala.']],
    [['Presión con el puntero','Segunda zona de presión','Mantén pulsada la zona marcada hasta que responda.']],
    [['Bisturí','Siguiente marca de la segunda pieza','Recorre los puntos señalados en orden.']],
    [['Pinzas de sutura','Inicio del nudo activo','Coloca las pinzas en el inicio de cada ligadura.'],['Portaagujas','Siguiente punto del nudo activo','Sigue las marcas de cada ligadura con la punta.']],
    [['Segunda pieza liberada','Bandeja de la práctica','Arrastra la pieza hasta la bandeja y suéltala.']],
    [['Portaagujas','Siguiente punto de cierre','Guía la punta hasta cada punto de sutura y espera su confirmación.']]
  ];
  let state=null,mode='',stroke=null,passes=0,message='',snapshot='',pointKey='';
  const sceneFor={lab:'7ScenaLaboratorioClinico',surgery:'8ScenaCirugia'};
  const valid=()=>state && document.documentElement.dataset.mode==='screen' && !levels.disabled && view.dataset.scene===sceneFor[mode];
  function send(action){if(valid() && player)player.SendMessage('VeterinariaWeb','ClinicalAction',action);}
  function stopStroke(){
    if(!stroke)return;
    const id=stroke.id;
    if(stroke.last>stroke.from+.01)passes++;
    stroke=null;send('end');
    if(typeof id==='number' && rail.hasPointerCapture(id))rail.releasePointerCapture(id);
    draw();
  }
  function reset(){
    stopStroke();state=null;mode='';snapshot='';pointKey='';passes=0;message='';
    panel.hidden=pins.hidden=body.hidden=true;toggle.setAttribute('aria-expanded','false');
    slider.value=25;get('toolPin').hidden=get('sitePin').hidden=true;
  }
  function openBody(open){
    if(!open)stopStroke();
    body.hidden=!open;toggle.setAttribute('aria-expanded',String(open));pins.hidden=!(open && mode==='surgery');
    draw();
  }
  toggle.onclick=()=>openBody(body.hidden);
  const angle=()=>Number(slider.value);
  const angleOK=()=>angle()>=30 && angle()<=45;
  function labMessage(){
    if(state.complete)return 'Extendido completo. '+(passes>1?'Has retomado el contacto. ':passes===1?'Pasada continua. ':'')+'Continúa con la tinción en la escena.';
    if(!state.ready)return 'Deposita una gota en el extremo del portaobjetos.';
    if(!state.next)return 'La gota está sobre el portaobjetos. Espera la indicación de extendido del robot.';
    if(!angleOK())return angle()<30?'Eleva el extensor hasta la franja de 30–45°.':'Baja el extensor hasta la franja de 30–45°.';
    if(message)return message;
    if(stroke)return 'Contacto. Avanza hacia la derecha de forma continua.';
    return 'Apoya el borde sobre la marca y arrastra hacia la derecha.'+(touch.matches?'':' Con teclado, enfoca la lámina y usa →.');
  }
  function draw(){
    if(mode!=='lab' || !state || body.hidden)return;
    const w=rail.clientWidth,h=rail.clientHeight;if(!w || !h)return;
    const scale=Math.min(devicePixelRatio||1,2);
    if(diagram.width!==Math.round(w*scale) || diagram.height!==Math.round(h*scale)){
      diagram.width=Math.round(w*scale);diagram.height=Math.round(h*scale);
    }
    const c=diagram.getContext('2d');c.setTransform(scale,0,0,scale,0,0);c.clearRect(0,0,w,h);
    const x0=w*.14,x1=w*.86,y=h*.70,x=x0+(x1-x0)*(state.progress/100);
    c.fillStyle='#24474d';c.fillRect(w*.05,y+1,w*.90,8);
    c.strokeStyle='#9bc2c4';c.lineWidth=2;c.beginPath();c.moveTo(w*.05,y);c.lineTo(w*.95,y);c.stroke();
    if(state.ready || state.complete){
      c.fillStyle='#ed8583';c.beginPath();c.ellipse(x0,y-2,6,state.progress>0?2:5,0,0,Math.PI*2);c.fill();
      if(state.progress){const grad=c.createLinearGradient(x0,0,Math.max(x0+1,x),0);grad.addColorStop(0,'#d46b6f');grad.addColorStop(1,'#e5a0a28a');
        c.fillStyle=grad;c.beginPath();c.moveTo(x0,y-5);c.lineTo(x,y-1);c.lineTo(x,y+1);c.lineTo(x0,y+1);c.fill();}
    }
    c.save();c.translate(x,y-2);c.rotate(-angle()*Math.PI/180);
    c.fillStyle='#93e6da';c.fillRect(0,-5,w*.21,5);c.restore();
    c.strokeStyle=angleOK()?'#86edc5':'#ffd18c';c.lineWidth=2;c.beginPath();c.arc(x,y-2,18,-angle()*Math.PI/180,0);c.stroke();
    c.fillStyle=c.strokeStyle;c.beginPath();c.arc(x,y-2,4,0,Math.PI*2);c.fill();
    if(state.progress>0){
      const bottom=h*.88,half=h*.045;
      const film=c.createLinearGradient(x0,0,Math.max(x0+1,x),0);
      film.addColorStop(0,'#aa5963');film.addColorStop(.65,'#d88590');film.addColorStop(1,'#edbdc03d');
      c.fillStyle=film;c.beginPath();c.moveTo(x0,bottom-half);c.lineTo(x0+(x-x0)*.78,bottom-half);
      c.quadraticCurveTo(x,bottom-half*.7,x,bottom);c.quadraticCurveTo(x,bottom+half*.7,x0+(x-x0)*.78,bottom+half);
      c.lineTo(x0,bottom+half);c.closePath();c.fill();
    }
    c.fillStyle='#c2dad8';c.font='12px system-ui';c.fillText('→',w*.91,y-7);
    get('angleValue').textContent=angle()+'° · referencia 30–45°';
    slider.setAttribute('aria-valuetext',angle()+' grados');get('labProgress').textContent='Recorrido '+state.progress+' %';
    cue.textContent=labMessage();get('dropButton').hidden=state.complete || !state.start;
    slider.disabled=!!stroke || state.complete;rail.setAttribute('aria-disabled',String(!state.ready || state.complete));
  }
  get('dropButton').onclick=()=>{message='';send('drop')};
  slider.oninput=()=>{message='';draw()};
  function startStroke(id,x,y){
    if(!valid() || !state.ready || !state.next || state.complete || !angleOK())return false;
    message='';stroke={id,x,y,from:state.progress/100,last:state.progress/100};
    send('begin/'+angle());draw();return true;
  }
  function moveStroke(fraction){
    if(!stroke)return;
    const p=Math.max(0,Math.min(1,fraction));
    if(p<stroke.last-.035){message='Has retrocedido. Vuelve a apoyar el borde en la marca.';stopStroke();return;}
    stroke.last=Math.max(stroke.last,p);send('slide/'+stroke.last.toFixed(4));
  }
  rail.onpointerdown=e=>{
    if(stroke || e.button!==0)return;
    const b=rail.getBoundingClientRect(),edge=b.left+b.width*(.14+.72*state.progress/100);
    if(Math.abs(e.clientX-edge)>Math.max(24,b.width*.10)){message='Empieza en el borde del extensor, sobre la marca.';draw();return;}
    if(startStroke(e.pointerId,e.clientX,e.clientY)){rail.setPointerCapture(e.pointerId);e.preventDefault();}
  };
  rail.onpointermove=e=>{
    if(!stroke || stroke.id!==e.pointerId)return;
    if(Math.abs(e.clientY-stroke.y)>Math.max(21,rail.clientHeight*.28)){
      message='Se perdió el contacto. Apoya de nuevo la lámina donde quedó el extendido.';stopStroke();return;
    }
    moveStroke(stroke.from+(e.clientX-stroke.x)/(rail.clientWidth*.72));
  };
  function release(e){if(stroke && stroke.id===e.pointerId){stopStroke();}}
  rail.onpointerup=rail.onpointercancel=rail.onlostpointercapture=release;
  rail.onkeydown=e=>{if(e.key!=='ArrowRight')return;e.preventDefault();e.stopPropagation();if(!stroke)startStroke('keyboard',0,0);if(stroke)moveStroke(stroke.last+.05)};
  rail.onkeyup=e=>{if(e.key==='ArrowRight' && stroke?.id==='keyboard')stopStroke()};
  rail.onblur=()=>{if(stroke?.id==='keyboard')stopStroke()};
  function pin(el,pos){
    el.hidden=!pos?.z;if(el.hidden)return;
    const x=pos.x/10,y=pos.y/10;el.style.left=x+'%';el.style.top=y+'%';el.dataset.away=String(pos.z===2);
    el.style.setProperty('--dir',Math.atan2((y-50)*view.clientHeight,(x-50)*view.clientWidth)+'rad');
  }
  function renderSurgery(s){
    const idx=Math.max(0,s.step),variants=actions[idx]||[],copy=variants[s.part]||variants[0];
    get('surgeryTitle').textContent=s.complete?'Guía completada':'Paso '+(idx+1)+' de '+s.total+' · '+(s.title?s.title.replace(/^\d+\.\s*/,''):'Empezar guía');
    get('toolName').textContent=s.complete?'—':'① '+(copy?.[0]||'Según el panel');
    get('zoneName').textContent=s.complete?'—':'② '+(copy?.[1]||'Zona indicada por el robot');
    get('gestureName').textContent=s.complete?'Consulta el resumen en el panel de la práctica.':(copy?.[2]||s.title);
    const key=s.step+'/'+s.point+'/'+s.part;
    if(pointKey!==key){
      const prev=pointKey.split('/');const advanced=Number(prev[0])===s.step && Number(prev[1])>0 && Number(prev[1])<s.point;
      get('pointCue').textContent=(advanced?'Punto anterior confirmado. ':'')+'Punto '+s.point+' de '+s.points;
      pointKey=key;
    }
    get('pointCue').hidden=!s.points || s.complete;
    const part=get('partButton');part.hidden=s.parts<2 || s.complete;
    if(!part.hidden)part.textContent='Señalar '+variants[(s.part+1)%variants.length][0].toLowerCase()+' ('+(s.part+1)+'/'+s.parts+')';
    const advance=get('protocolButton');advance.hidden=(!s.start && !s.next) || s.complete;
    advance.textContent=s.start?'Empezar guía':'Siguiente';
  }
  get('partButton').onclick=()=>send('part');get('protocolButton').onclick=()=>send(state?.start?'start':'next');
  document.addEventListener('veterinaria-clinical',({detail:s})=>{
    if(!s.mode){reset();return;}
    if(mode!==s.mode){reset();mode=s.mode;panel.dataset.mode=mode;panel.hidden=false;
      toggle.textContent=mode==='lab'?'Mesa de frotis':'Guía de cirugía';
      get('labControls').hidden=mode!=='lab';get('surgeryControls').hidden=mode!=='surgery';
      if(mode==='surgery')openBody(true);
    }
    state=s;
    if(mode==='lab'){
      if(s.complete && stroke){stopStroke();}
      draw();return;
    }
    pin(get('toolPin'),s.instrument);pin(get('sitePin'),s.site);
    const next=[s.step,s.total,s.part,s.point,s.points,s.complete,s.start,s.next].join('/');
    if(snapshot!==next){snapshot=next;renderSurgery(s);}
  });
  new MutationObserver(()=>{if(document.documentElement.dataset.mode==='xr' || levels.disabled)reset()})
    .observe(document.documentElement,{attributes:true,attributeFilter:['data-mode']});
  levels.addEventListener('change',reset);
  document.addEventListener('veterinaria-scene',e=>{if(mode && e.detail!==sceneFor[mode])reset()});
  document.addEventListener('visibilitychange',()=>{if(document.hidden)stopStroke()});window.addEventListener('blur',stopStroke);
  new ResizeObserver(draw).observe(rail);
})();
