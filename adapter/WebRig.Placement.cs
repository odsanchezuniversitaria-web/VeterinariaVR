using System.Collections.Generic;
using Oculus.Interaction;
using OseoVR;
using OseoVR.Oseo;
using OseoVR.EPP;
using UnityEngine;

public sealed partial class WebRig {
    sealed class Placement {
        public Behaviour rule;
        public Transform point,source;
        public Collider zone;
        public Vector3 localPoint,localSource;
        public SnapInteractable socket;
        public ZonaSnap mask;
        public bool leftStart;
    }
    readonly List<Placement> placements=new List<Placement>();
    SnapInteractor movingSnap;
    Vector2 placementPress;
    Vector3 smoothedPlacement;
    Placement pulling;

    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void VeterinariaPlacement(int x,int y,int state);
    Vector3Int shownPlacement=new Vector3Int(-1,-1,-1);
    void MarkPlacement(Vector3 screen,int state){
        var hint=new Vector3Int(Mathf.RoundToInt(1000*screen.x/eye.pixelWidth),Mathf.RoundToInt(1000*(1-screen.y/eye.pixelHeight)),state);
        if(state==0)hint=Vector3Int.zero;
        if(hint==shownPlacement)return;
        shownPlacement=hint;
#if UNITY_WEBGL && !UNITY_EDITOR
        VeterinariaPlacement(hint.x,hint.y,hint.z);
#endif
    }
    void AddPlacement(Behaviour rule,Transform point,Vector3 localPoint,Transform source,Vector3 localSource,Collider zone=null){
        if(!rule || !point || !source)return;
        placements.Add(new Placement{rule=rule,point=point,source=source,localPoint=localPoint,localSource=localSource,zone=zone});
    }
    void BeginPlacement(Grabbable grab,Collider touched) {
        placements.Clear();pulling=null;movingSnap=null;placementPress=pressPosition;
        var body=touched.attachedRigidbody;
        var root=body?body.transform:dragPiece;
        var center=root.InverseTransformPoint(touched.bounds.center);
        foreach(var snap in root.GetComponentsInChildren<SnapInteractor>(true))
            if(snap.PointableElement==grab || snap.Rigidbody==body){movingSnap=snap;break;}
        if(movingSnap){
            foreach(var socket in FindObjectsByType<SnapInteractable>(FindObjectsSortMode.None)){
                if(!movingSnap.CanSelect(socket) || !socket.CanBeSelectedBy(movingSnap))continue;
                placements.Add(new Placement{rule=socket,socket=socket,point=socket.transform,source=root});
            }
        }
        foreach(var zone in FindObjectsByType<ZonaDevolverHuesos>(FindObjectsSortMode.None)){
            if(zone.huesos==null || System.Array.FindIndex(zone.huesos,t=>t && (root.IsChildOf(t)||t.IsChildOf(root)))<0)continue;
            var c=zone.GetComponent<Collider>();if(c)AddPlacement(zone,c.transform,c.transform.InverseTransformPoint(c.bounds.center),root,center,c);
        }
        foreach(var zone in FindObjectsByType<RestaurarPosicionAlTocar>(FindObjectsSortMode.None)){
            // The authored return trigger restores the saved local pose on release.
            var c=zone.GetComponent<Collider>();if(c)AddPlacement(zone,c.transform,c.transform.InverseTransformPoint(c.bounds.center),root,center,c);
        }
        foreach(var microscope in FindObjectsByType<CuboMicroscopioDetector>(FindObjectsSortMode.None)){
            bool match=microscope.portaMuestra==touched.gameObject || microscope.portaMuestrasExtra!=null && System.Array.IndexOf(microscope.portaMuestrasExtra,touched.gameObject)>=0;
            var c=microscope.GetComponent<Collider>();
            if(match && c)AddPlacement(microscope,c.transform,c.transform.InverseTransformPoint(c.bounds.center),root,center,c);
        }
        foreach(var mannequin in FindObjectsByType<ZonaEPPUnica>(FindObjectsSortMode.None)){
            var c=mannequin.GetComponent<Collider>();
            if(c && mannequin.tagsDelRol!=null && System.Array.IndexOf(mannequin.tagsDelRol,touched.tag)>=0)
                AddPlacement(mannequin,c.transform,c.transform.InverseTransformPoint(c.bounds.center),root,center,c);
        }
        var waste=root.GetComponent<ResiduoDesecho>();
        if(waste)foreach(var bin in FindObjectsByType<CanecaDesechos>(FindObjectsSortMode.None)){
            var c=bin.GetComponent<Collider>();if(bin.acepta==waste.tipo && c)AddPlacement(bin,c.transform,c.transform.InverseTransformPoint(c.bounds.center),root,center,c);
        }
        foreach(var zone in FindObjectsByType<ZonaColocacion>(FindObjectsSortMode.None)){
            if(touched.tag==zone.tagObjetoCorrecto && zone.posicionObjetivo)AddPlacement(zone,zone.posicionObjetivo,Vector3.zero,root,center);
        }
        foreach(var zone in FindObjectsByType<ZonaSnap>(FindObjectsSortMode.None)){
            var item=root.GetComponent<ObjetoColocable>();
            if(string.IsNullOrEmpty(zone.objetoEsperado) || !(root.tag==zone.objetoEsperado || root.name.Contains(zone.objetoEsperado) || item && item.tipoObjeto==zone.objetoEsperado))continue;
            var t=zone.puntoColocacion?zone.puntoColocacion:zone.transform;
            placements.Add(new Placement{rule=zone,mask=zone,point=t,source=root,localSource=center});
        }
        foreach(var p in placements){
            var a=eye.WorldToScreenPoint(p.point.TransformPoint(p.localPoint));
            p.leftStart=a.z<=0 || Vector2.Distance(a,placementPress)>PlacementRadius()*1.3f;
        }
    }
    float PlacementRadius()=>Mathf.Clamp(Mathf.Min(eye.pixelWidth,eye.pixelHeight)*(Input.touchSupported ? .085f : .045f),18,65);
    Vector3 AssistPlacement(Ray ray,Vector3 free) {
        if(placements.Count==0)return free;
        var pointer=(Vector2)eye.WorldToScreenPoint(ray.origin+ray.direction);
        float radius=PlacementRadius();
        bool moved=Vector2.Distance(pointer,placementPress)>=radius*.30f;
        float hintDistance=float.MaxValue;Vector3 hint=Vector3.zero;
        float best=float.MaxValue;Placement chosen=null;Vector3 desired=free;bool aligned=false;
        foreach(var p in placements){
            if(!p.rule || !p.rule.isActiveAndEnabled || !p.source || !p.point || p.zone && !p.zone.enabled || p.mask && p.mask.soloPuedeColocarseUno && p.mask.EstaOcupada())continue;
            var target=p.point.TransformPoint(p.localPoint);
            var source=p.source.TransformPoint(p.localSource);
            if(p.socket){
                if(!movingSnap || !movingSnap.isActiveAndEnabled || !movingSnap.CanSelect(p.socket) || !p.socket.CanBeSelectedBy(movingSnap) || !p.socket.PoseForInteractor(movingSnap,out var pose))continue;
                target=pose.position;source=movingSnap.SnapPose.position;
            }
            var screen=eye.WorldToScreenPoint(target);
            if(screen.z<eye.nearClipPlane || screen.z>10f)continue;
            float pixels=Vector2.Distance(pointer,screen);
            if(pixels<hintDistance && (!(p.rule is ZonaEPPUnica || p.rule is CanecaDesechos) || pixels<=radius*1.3f)){hintDistance=pixels;hint=screen;}
            if(pixels>radius*1.3f)p.leftStart=true;
            if(!moved || !p.leftStart || pixels>radius*(pulling==p?1.25f:1f) || pixels>=best)continue;
            // Use the registered grab contact, not the pivot. Many bones have remote model origins.
            var contact=dragPiece.TransformPoint(dragAnchor);
            desired=contact+target-source;best=pixels;chosen=p;
            aligned=p.zone?p.zone.bounds.Contains(source):(target-source).sqrMagnitude<.025f*.025f;
        }
        MarkPlacement(hint,hintDistance<float.MaxValue && hint.x>=0 && hint.y>=0 && hint.x<=eye.pixelWidth && hint.y<=eye.pixelHeight?(chosen!=null?(aligned?3:2):1):0);
        if(chosen==null){pulling=null;return free;}
        if(chosen!=pulling)smoothedPlacement=free;
        pulling=chosen;
        float weight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(radius*1.25f,radius*.35f,best));
        var aim=Vector3.Lerp(free,desired,weight);
        smoothedPlacement=Vector3.Lerp(smoothedPlacement,aim,1-Mathf.Exp(-24*Time.unscaledDeltaTime));
        return smoothedPlacement;
    }
}
