using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using OseoVR.Cirugia;
using OseoVR.Laboratorio;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class WebRig {
    [Serializable]
    class ClinicalState {
        public string mode,title;
        public int step,total,part,parts,point,points,progress;
        public bool ready,complete,start,next,held;
        public Vector3 instrument,site;
    }
    [DllImport("__Internal")] static extern void VeterinariaClinicalState(string json);
    readonly ClinicalState guideState=new ClinicalState();
    FrotisExtendidoSuave smear;
    DepositarGota deposit,progressDeposit;
    GameObject dropWitness;
    GuiaLaboratorio labGuide;
    ProtocoloCirugia protocol;
    Button startGuide,confirmStep;
    readonly Dictionary<string,Transform> guideTargets=new Dictionary<string,Transform>();
    string lastGuideJson;
    float nextGuideTime;
    int guidedStep=-99,guidePart;
    bool guidedContact;

    void EnterClinicalGuide(){
        var scene=gameObject.scene;
        if(scene.name=="7ScenaLaboratorioClinico"){
            labGuide=FindFirstObjectByType<GuiaLaboratorio>();
            foreach(var candidate in FindObjectsByType<FrotisExtendidoSuave>(FindObjectsSortMode.None)){
                if(!smear)smear=candidate;
                if(labGuide && labGuide.misiones.Count>1 && candidate.mascara &&
                   labGuide.misiones[1].testigo==candidate.mascara.gameObject){smear=candidate;break;}
            }
            if(!smear)return;

            dropWitness=labGuide && labGuide.misiones.Count>0?labGuide.misiones[0].testigo:null;
            foreach(var entry in FindObjectsByType<DepositarGota>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
                if(entry.gotaSangre==smear.gotaDeSangre)deposit=entry;
                if(dropWitness && entry.gotaSangre==dropWitness)progressDeposit=entry;
            }

            guideState.mode="lab";
        }else if(scene.name=="8ScenaCirugia"){
            protocol=FindFirstObjectByType<ProtocoloCirugia>();
            if(!protocol)return;
            foreach(var button in FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
                for(int i=0;i<button.onClick.GetPersistentEventCount();i++){
                    if(button.onClick.GetPersistentTarget(i)!=protocol)continue;
                    if(button.onClick.GetPersistentMethodName(i)=="Iniciar")startGuide=button;
                }
            }
            if(protocol.botonSiguiente)confirmStep=protocol.botonSiguiente.GetComponentInChildren<Button>(true);
            var wanted=new HashSet<string>{"ZonaCalculadora","SnapInteractable CableEnchufe","SnapInteractable CableOxigeno",
                "SnapInteractable CableAL","SnapInteractable CableAR","SnapInteractable CableLL","pinzasCerraSnapInteractable","bandeja testic","CubosTriggerCirugia"};
            foreach(var root in scene.GetRootGameObjects())
                foreach(var t in root.GetComponentsInChildren<Transform>(true))if(wanted.Contains(t.name) && (!guideTargets.ContainsKey(t.name) || t.gameObject.activeInHierarchy))guideTargets[t.name]=t;
            var valve=FindFirstObjectByType<LlaveTanqueOxigeno>(FindObjectsInactive.Include);
            guideValve=valve?(valve.valvulaGiratoria?valve.valvulaGiratoria:valve.transform):null;
            guideState.mode="surgery";
        }else return;
        lastGuideJson=null;nextGuideTime=0;guidedStep=-99;
    }
    void EndGuidedStroke(){
        if(smear && guidedContact)smear.SoltarLamina();
        guidedContact=false;nextGuideTime=0;
    }
    void LeaveClinicalGuide(){
        if(string.IsNullOrEmpty(guideState.mode))return;
        EndGuidedStroke();
        guideState.mode="";smear=null;deposit=null;progressDeposit=null;dropWitness=null;labGuide=null;protocol=null;
        startGuide=null;confirmStep=null;guideTargets.Clear();
        PublishGuide("{}");
    }
    static void PublishGuide(string json){
#if UNITY_WEBGL && !UNITY_EDITOR
        VeterinariaClinicalState(json);
#endif
    }
    public void ClinicalAction(string action){
        if(!OnScreen || string.IsNullOrEmpty(guideState.mode))return;
        if(smear){
            var parts=action.Split('/');
            float value=0;
            if(parts.Length==2 && (!float.TryParse(parts[1],NumberStyles.Float,CultureInfo.InvariantCulture,out value)
                 || float.IsNaN(value) || float.IsInfinity(value)))return;
            switch(parts[0]){
                case "drop":
                    if(CanGuideDrop()){
                        if(!smear.ListaParaExtender)deposit.Depositar();
                        if(progressDeposit && progressDeposit!=deposit)progressDeposit.Depositar();
                        else if(dropWitness && !dropWitness.activeSelf)dropWitness.SetActive(true);
                    }
                    break;
                case "begin":if(parts.Length==2 && !guidedContact && (!labGuide || labGuide.IndiceActual>=1))guidedContact=smear.ApoyarLamina(value);break;
                case "slide":if(parts.Length==2 && guidedContact)smear.MoverLamina(value);break;
                case "end":EndGuidedStroke();break;
            }
        }else if(protocol){
            if(action=="start" && protocol.PasoActual<0 && Available(startGuide))startGuide.onClick.Invoke();
            if(action=="next" && protocol.Paso!=null && protocol.Paso.mostrarBotonSiguiente && Available(confirmStep))confirmStep.onClick.Invoke();
            if(action=="part" && protocol.PasoActual==guidedStep)guidePart=(guidePart+1)%PartCount(guidedStep);
        }
        nextGuideTime=0;
    }
    bool CanGuideDrop()=>deposit && deposit.isActiveAndEnabled && (!smear.ListaParaExtender || (labGuide && labGuide.IndiceActual<1 && dropWitness && !dropWitness.activeSelf));
    static bool Available(Button button)=>button && button.isActiveAndEnabled && button.IsInteractable();
    static int PartCount(int index)=>index==3 || index==5?3:index==12 || index==13 || index==17?2:1;
    Transform Named(string key)=>guideTargets.TryGetValue(key,out var t)?t:null;
    static Transform Tool(PasoProtocolo step,int i){
        if(step.herramientas==null || i>=step.herramientas.Length || !step.herramientas[i])return null;
        return step.herramientas[i].transform;
    }
    Transform KnotPoint(SecuenciaNudos knots){
        if(!knots)return null;
        int i=knots.GetPuntoActual();
        guideState.points=knots.puntosSecuencia.Count;guideState.point=Mathf.Min(i+1,guideState.points);
        return i>=0 && i<guideState.points?knots.puntosSecuencia[i]:null;
    }
    void SurgicalTargets(PasoProtocolo step,out Transform instrument,out Transform site){
        instrument=Tool(step,0);site=null;
        switch(protocol.PasoActual){
            case 1:site=Named("ZonaCalculadora");break;
            case 2:site=protocol.pasos[1].objetoTestigo?protocol.pasos[1].objetoTestigo.transform:null;break;
            case 3:
                instrument=guidePart<2?Tool(step,guidePart):null;
                site=guidePart==2?guideValve:Named(guidePart==0?"SnapInteractable CableEnchufe":"SnapInteractable CableOxigeno");break;
            case 4:site=step.objetoTestigo?step.objetoTestigo.transform:null;break;
            case 5:
                instrument=Tool(step,guidePart);
                if(instrument && instrument.parent){
                    var name=instrument.parent.name;
                    site=Named("SnapInteractable Cable"+(name.Contains("AR")?"AR":name.Contains("AL")?"AL":"LL"));
                }break;
            case 6:site=Named("CubosTriggerCirugia");break;
            case 7:site=Named("pinzasCerraSnapInteractable");break;
            case 9:
                if(step.grupoSangre)foreach(Transform spot in step.grupoSangre.transform)if(spot.gameObject.activeInHierarchy){site=spot;break;}
                break;
            case 10:case 15:site=instrument;instrument=null;break;
            case 14:case 18:site=Named("bandeja testic");break;
        }
        if(step.sistemaTestigo is MultiCutSystem cut){
            int i=cut.GetCuboActual();guideState.points=cut.GetTotalCubos();guideState.point=Mathf.Min(i+1,guideState.points);
            if(i>=0 && i<cut.cubosCorte.Count && cut.cubosCorte[i])site=cut.cubosCorte[i].transform;
        }
        if(step.sistemaTestigo is SecuenciaNudos sequence){
            instrument=Tool(step,guidePart==0?1:0);
            if(step.habilitarAlEntrar!=null)foreach(var behaviour in step.habilitarAlEntrar)
                if(behaviour is SecuenciaNudos n && n.isActiveAndEnabled && !n.EstaCompleta()){sequence=n;break;}
            site=KnotPoint(sequence);
        }
        if(step.sistemaTestigo is SutureSystem suture){
            int i=suture.GetCuboActual();guideState.points=suture.GetTotalCubos();guideState.point=Mathf.Min(i+1,guideState.points);
            if(i>=0 && i<suture.cubosCorte.Count && suture.cubosCorte[i])site=suture.cubosCorte[i].transform;
        }
    }
    Transform guideValve;
    Vector3 GuidePosition(Transform target){
        if(!target)return Vector3.zero;
        Vector3 position=target.position;
        if(target is RectTransform rect)position=rect.TransformPoint(rect.rect.center);
        else if(target.TryGetComponent<Collider>(out var collider) && collider.enabled && target.gameObject.activeInHierarchy)position=collider.bounds.center;
        var pt=eye.WorldToViewportPoint(position);
        float x=pt.x-.5f,y=.5f-pt.y;
        if(pt.z<eye.nearClipPlane){x=-x;y=-y;if(Mathf.Abs(x)+Mathf.Abs(y)<.01f)y=.5f;}
        bool outside=pt.z<eye.nearClipPlane || Mathf.Abs(x)>.46f || Mathf.Abs(y)>.44f;
        if(outside){float edge=Mathf.Max(.0001f,Mathf.Max(Mathf.Abs(x)/.46f,Mathf.Abs(y)/.44f));x/=edge;y/=edge;}
        return new Vector3(Mathf.Round(1000*(x+.5f)),Mathf.Round(1000*(y+.5f)),outside?2:1);
    }
    void TickClinicalGuide(){
        if(!OnScreen || (!protocol && !smear) || Time.unscaledTime<nextGuideTime)return;
        nextGuideTime=Time.unscaledTime+.10f;
        var s=guideState;s.point=s.points=0;s.instrument=s.site=Vector3.zero;
        if(smear){
            s.ready=smear.ListaParaExtender;s.complete=smear.Terminada;s.start=CanGuideDrop();
            s.progress=Mathf.RoundToInt(100*smear.FraccionExtendida);s.held=guidedContact;s.next=!labGuide || labGuide.IndiceActual>=1;
            s.step=labGuide?labGuide.IndiceActual:-1;s.title="";
            if(labGuide && s.step>=0 && s.step<labGuide.misiones.Count)s.title=labGuide.misiones[s.step].titulo;
        }else{
            s.step=protocol.PasoActual;s.total=protocol.TotalPasos;s.complete=protocol.Terminado;
            if(s.step!=guidedStep){guidePart=0;guidedStep=s.step;}
            s.part=guidePart;s.parts=PartCount(s.step);s.start=Available(startGuide) && s.step<0;
            s.next=protocol.Paso!=null && protocol.Paso.mostrarBotonSiguiente && Available(confirmStep);
            s.title=protocol.Paso==null?"":protocol.Paso.titulo;
            if(s.step<0 && startGuide)s.site=GuidePosition(startGuide.transform);
            if(protocol.Paso!=null && !s.complete){
                SurgicalTargets(protocol.Paso,out var tool,out var site);
                s.instrument=GuidePosition(tool);s.site=GuidePosition(site);
            }
        }
        var json=JsonUtility.ToJson(s);
        if(json!=lastGuideJson){lastGuideJson=json;PublishGuide(json);}
    }
}