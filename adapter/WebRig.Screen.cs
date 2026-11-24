using System.Globalization;
using Oculus.Interaction;
using OseoVR.Laminitis;
using UnityEngine;

public sealed partial class WebRig {
    public bool ScreenHeld {get;private set;}
    public bool OnScreen => screenView!=null && screenView.Active;
    float screenDepth=.65f;
    Ray screenRay;
    Vector2 pressPosition;
    bool manipulating;
    bool wasImmersive;
    public Transform ScreenSuture {get;private set;}
    public Vector3 ScreenPoint => screenRay.GetPoint(screenDepth);
    void PickSuture(Collider c){
        var simple=c.GetComponentInParent<PuntoSuturaSimple>();
        var other=c.GetComponentInParent<PuntoSuturaPiel>();
        ScreenSuture=simple?simple.transform:other?other.transform:null;
        if(ScreenSuture)manipulating=true;
    }
    const int cursor=1;
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void VeterinariaScreenActions(string actions);
    void AnnounceActions(){
#if UNITY_WEBGL && !UNITY_EDITOR
        var names=new System.Collections.Generic.List<string>();
        if(FindFirstObjectByType<SecuenciaLaminitisManual>()){names.Add("prev");names.Add("next");}
        if(FindFirstObjectByType<ApareceObjPanel>())names.Add("equipment");
        if(FindFirstObjectByType<MostrarPanelConBotonX>())names.Add("panel");
        if(FindFirstObjectByType<OcultarPanelLetra>())names.Add("legend");
        if(FindFirstObjectByType<LetreroToggle>())names.Add("sign");
        if(FindFirstObjectByType<CuerdaFisica>())names.Add("rope");
        if(FindFirstObjectByType<OseoVR.Boxeo.PausaBoxeo>())names.Add("pause");
        else if(FindFirstObjectByType<OseoVR.Multijugador.InterruptorMultijugador>())names.Add("network");
        VeterinariaScreenActions(string.Join(",",names));
#endif
    }
    public void ScreenAction(string action){
        if(!OnScreen)return;
        switch(action){
            case "prev":case "next":
                var steps=FindFirstObjectByType<SecuenciaLaminitisManual>();
                if(steps)WebLegacyInput.Tap(action=="next"?steps.botonAvanzar:steps.botonRetroceder);
                break;
            case "equipment":ActionB();break;
            case "panel":ActionX();break;
            case "legend":foreach(var panel in FindObjectsByType<OcultarPanelLetra>(FindObjectsSortMode.None))panel.Toggle();break;
            case "sign":
                var sign=FindFirstObjectByType<LetreroToggle>();if(sign)WebLegacyInput.Tap(sign.boton);
                break;
            case "rope":ActionA();break;
            case "pause":TogglePause();break;
            case "network":ActionMenu();break;
        }
    }

    public void SetWalk(string value){
        var xy=value.Split(',');
        if(xy.Length==2 && float.TryParse(xy[0],NumberStyles.Float,CultureInfo.InvariantCulture,out var x)
            && float.TryParse(xy[1],NumberStyles.Float,CultureInfo.InvariantCulture,out var y))stick=Vector2.ClampMagnitude(new Vector2(x,y),1);
    }
    public bool HoldingZone(Collider zone){
        if(!ScreenHeld || !zone || !zone.enabled || !zone.Raycast(screenRay,out var h,10))return false;
        return !Physics.Raycast(screenRay,out var solid,10,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)
            || solid.distance+.12f>=h.distance;
    }
    bool ScreenHit(Ray ray,out RaycastHit target,out float limit){
        bool solid=Physics.Raycast(ray,out target,10,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        limit=solid?target.distance:10f;
        float best=limit+.02f;
        foreach(var hit in Physics.RaycastAll(ray,best,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Collide)){
            if(!hit.collider.isTrigger || hit.distance>best)continue;
            var c=hit.collider;
            if(juegoBoxeo && (c.GetComponentInParent<HuesoInteractivo>() || c.GetComponentInParent<OseoVR.Boxeo.BotonInicioBoxeo>())){
                target=hit;best=hit.distance;solid=true;continue;
            }
            if(!c.GetComponentInParent<PointableElement>() && !c.GetComponentInChildren<PointableElement>() &&
                !c.GetComponentInParent<InteractableUnityEventWrapper>() && !c.GetComponentInChildren<InteractableUnityEventWrapper>() &&
                !c.GetComponentInParent<PuntoSuturaPiel>() && !c.GetComponentInParent<PuntoSuturaSimple>())continue;
            target=hit;best=hit.distance;solid=true;
        }
        return solid;
    }
    void ScreenInput(){
        walk=stick+new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"));look=Vector2.zero;
        if(inspection!=Inspection.None && (canvasFinger || Input.touchCount>0 || Time.unscaledTime-lastTouch<.4f)){
            look=pendingLook;pendingLook=Vector2.zero;
            if(canvasFinger && ScreenHeld)ScreenPointer(fingerPosition,false);
            if(tapEndFrame>=0 && Time.frameCount>tapEndFrame){EndScreenPointer(false);canvasFinger=false;tapEndFrame=-1;}
            return;
        }
        if(Input.touchCount>0){
            for(int i=0;i<Input.touchCount;i++){
                var t=Input.GetTouch(i);
                if(t.phase==TouchPhase.Began && touchLook<0){touchLook=t.fingerId;lookStart=t.position;touchDragging=false;ScreenPointer(t.position,true);}
                if(t.fingerId!=touchLook)continue;
                if(Vector2.Distance(t.position,lookStart)>Mathf.Max(8,Screen.height*.011f))touchDragging=true;
                if(t.phase!=TouchPhase.Began)ScreenPointer(t.position,false);
                if(touchDragging && !manipulating && !selected[cursor] && !wrappers[cursor] && (uiData[cursor]==null || !uiData[cursor].pointerDrag))
                    look=t.deltaPosition*(90f/Mathf.Max(320,Screen.height));
                if(t.phase==TouchPhase.Ended || t.phase==TouchPhase.Canceled)
                    EndScreenPointer(t.phase==TouchPhase.Canceled || touchDragging);
            }
            return;
        }
        if(touchLook>=0){EndScreenPointer(true);return;}
        ScreenPointer(Input.mousePosition,Input.GetMouseButtonDown(0));
        if(Input.GetMouseButton(1) || ScreenHeld && !manipulating && !selected[cursor] && !wrappers[cursor] && !uiTarget[cursor])
            look=new Vector2(Input.GetAxis("Mouse X"),Input.GetAxis("Mouse Y"))*2.5f;
        if(Input.GetMouseButtonUp(0))EndScreenPointer(Vector2.Distance(pressPosition,Input.mousePosition)>Mathf.Max(8,Screen.height*.011f));
    }
    void ScreenPointer(Vector2 pos,bool press){
        screenRay=eye.ScreenPointToRay(pos);
        if(press){
            ScreenHeld=true;screenDepth=.65f;pressPosition=pos;manipulating=false;ScreenSuture=null;
            foreach(var task in FindObjectsByType<TesticleExtractionSystem>(FindObjectsSortMode.None))
                if(HoldingZone(task.zonaExtraccion)){manipulating=true;break;}
        }
        HandleRay(cursor,screenRay,press,false,eye.transform.rotation,originalRight,true);
    }
    void EndScreenPointer(bool cancel){
        if(shownPlacement.z>0)MarkPlacement(Vector3.zero,0);
        ScreenHeld=false;touchLook=-1;ScreenSuture=null;dragPiece=null;
        Release(cursor,eye.transform.rotation,cancel);
    }
    void OnApplicationFocus(bool focus){
        if(!focus && OnScreen){stick=Vector2.zero;canvasFinger=false;pendingLook=Vector2.zero;EndScreenPointer(true);}
    }
}
