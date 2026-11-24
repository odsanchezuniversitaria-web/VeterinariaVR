using System.Collections.Generic;
using System.Globalization;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Rendering;

public sealed partial class WebRig {
    enum Inspection { None, Piece, View }
    Inspection inspection;
    readonly Dictionary<Transform,float> magnifications=new Dictionary<Transform,float>();
    readonly Dictionary<Transform,Vector3> physicalScales=new Dictionary<Transform,Vector3>();
    readonly Dictionary<Transform,Quaternion> screenRotations=new Dictionary<Transform,Quaternion>();
    readonly Dictionary<Transform,Quaternion> physicalRotations=new Dictionary<Transform,Quaternion>();
    Transform inspected,dragPiece;
    Vector3 dragAnchor;
    float planeDepth,requestedDepth,minimumGrabDepth,baseFov,viewFactor=1f;
    int grabDepthFrame=-1;
    Vector2 fingerPosition,pendingLook;
    float lastTouch=-10f;
    bool canvasFinger;
    int tapEndFrame=-1;

    [System.Runtime.InteropServices.DllImport("__Internal")]
    static extern void VeterinariaInspectionState(string mode,string target,int percent);

    void EnterInspection(){
        switch(gameObject.scene.name){
            case "1ScenaOseo": case "2ScenaCraneos": case "5ScenaCerebro":
            case "7ScenaMusculo": case "13ScenaAnatomia":
                inspection=Inspection.Piece;break;
            case "8ScenaCirugia": case "Scena6.1 Quirofano":
                inspection=Inspection.View;break;
            default:inspection=Inspection.None;break;
        }
        if(inspection==Inspection.View)ApplyClinicalLens();
        if(inspection==Inspection.Piece){
            RenderPipelineManager.beginCameraRendering+=BeginScreenDraw;
            RenderPipelineManager.endCameraRendering+=EndScreenDraw;
        }
        PublishInspection();
    }
    void LeaveInspection(){
        if(inspection==Inspection.None)return;
        RenderPipelineManager.beginCameraRendering-=BeginScreenDraw;
        RenderPipelineManager.endCameraRendering-=EndScreenDraw;
        RestorePhysicalScales();
        if(inspection==Inspection.View && eye){eye.ResetProjectionMatrix();eye.fieldOfView=baseFov;}
        inspection=Inspection.None;viewFactor=1f;inspected=null;dragPiece=null;
        magnifications.Clear();screenRotations.Clear();canvasFinger=false;tapEndFrame=-1;pendingLook=Vector2.zero;
        PublishInspection();
    }
    void PublishInspection(){
#if UNITY_WEBGL && !UNITY_EDITOR
        string mode=inspection==Inspection.View?"vista":inspection==Inspection.Piece?"pieza":"";
        VeterinariaInspectionState(mode,inspected?inspected.name:"",Mathf.RoundToInt(CurrentFactor()*100));
#endif
    }
    float CurrentFactor(){
        if(inspection==Inspection.View)return viewFactor;
        return inspected && magnifications.TryGetValue(inspected,out var f)?f:1f;
    }
    void BeginScreenDraw(ScriptableRenderContext context,Camera camera){
        if(camera==eye && OnScreen && !camera.stereoEnabled)ApplyScreenScales();
    }
    void EndScreenDraw(ScriptableRenderContext context,Camera camera){
        if(camera==eye)RestorePhysicalScales();
    }
    // Scale for the flat camera and hit tests only. Simulation and multiplayer retain the authored sizes.
    void ApplyScreenScales(){
        foreach(var item in screenRotations){
            if(!item.Key)continue;
            physicalRotations.Add(item.Key,item.Key.localRotation);
            item.Key.localRotation*=item.Value;
        }
        foreach(var item in magnifications){
            if(!item.Key)continue;
            physicalScales.Add(item.Key,item.Key.localScale);
            item.Key.localScale*=item.Value;
        }
    }
    void RestorePhysicalScales(){
        foreach(var item in physicalScales)if(item.Key)item.Key.localScale=item.Value;
        physicalScales.Clear();
        foreach(var item in physicalRotations)if(item.Key)item.Key.localRotation=item.Value;
        physicalRotations.Clear();
    }
    bool PickScreenRay(Ray ray,out RaycastHit hit,out float limit,out Vector3 localHit){
        localHit=Vector3.zero;
        bool enlarged=inspection==Inspection.Piece && (magnifications.Count>0 || screenRotations.Count>0);
        try{
            if(enlarged){ApplyScreenScales();Physics.SyncTransforms();}
            if(!ScreenHit(ray,out hit,out limit))return false;
            var grab=ScreenGrab(hit.collider);
            if(grab && grab.isActiveAndEnabled && grab.Transform)localHit=grab.Transform.InverseTransformPoint(hit.point);
            return true;
        }finally{
            if(enlarged){RestorePhysicalScales();Physics.SyncTransforms();}
        }
    }
    static Grabbable ScreenGrab(Collider hit){
        var grip=hit.GetComponentInParent<Grabbable>()??hit.GetComponentInChildren<Grabbable>();
        return !grip && hit.attachedRigidbody?hit.attachedRigidbody.GetComponentInChildren<Grabbable>():grip;
    }
    bool PermiteAcercamiento => inspection!=Inspection.View
        && gameObject.scene.name!="7ScenaLaboratorioClinico" && gameObject.scene.name!="0ScenaMenu";

    Vector3 BeginPieceDrag(Grabbable grab,RaycastHit hit,Vector3 localPoint){
        dragPiece=grab.Transform;
        dragAnchor=localPoint;
        BeginPlacement(grab,hit.collider);
        planeDepth=Vector3.Dot(hit.point-eye.transform.position,eye.transform.forward);
        minimumGrabDepth=Mathf.Clamp(hit.collider.bounds.extents.magnitude+.32f,.40f,.85f);
        requestedDepth=PermiteAcercamiento?Mathf.Min(planeDepth,Mathf.Max(minimumGrabDepth,planeDepth*.68f)):planeDepth;
        grabDepthFrame=-1;
        if(inspection==Inspection.Piece){
            inspected=dragPiece;
            PublishInspection();
        }
        return dragPiece.TransformPoint(dragAnchor);
    }

    Vector3 PiecePointer(Ray ray,float wheel){
        if(grabDepthFrame!=Time.frameCount){
            grabDepthFrame=Time.frameCount;
            requestedDepth=Mathf.Clamp(requestedDepth+wheel*.06f,Mathf.Min(planeDepth,minimumGrabDepth),10f);
            planeDepth=Mathf.MoveTowards(planeDepth,requestedDepth,Time.unscaledDeltaTime*2.8f);
        }
        var forward=eye.transform.forward;
        float distance=(planeDepth-Vector3.Dot(ray.origin-eye.transform.position,forward))/Vector3.Dot(ray.direction,forward);
        distances[cursor]=distance;
        Vector3 physicalAnchor=dragPiece.TransformVector(dragAnchor);
        Quaternion offset=screenRotations.TryGetValue(dragPiece,out var saved)?saved:Quaternion.identity;
        var corrected=dragPiece.localRotation*(offset*Vector3.Scale(dragPiece.localScale,dragAnchor))*CurrentFactor();
        if(dragPiece.parent)corrected=dragPiece.parent.TransformVector(corrected);
        return AssistPlacement(ray,ray.GetPoint(distance)+physicalAnchor-corrected);
    }

    public void ScreenRotate(string angles){
        if(!OnScreen || inspection!=Inspection.Piece || !inspected)return;
        if(angles=="reset"){screenRotations.Remove(inspected);return;}
        var parts=angles.Split(',');
        if(parts.Length!=3 || !ReadAngle(parts[0],out float yaw) || !ReadAngle(parts[1],out float pitch)
            || !ReadAngle(parts[2],out float roll))return;
        var basis=Quaternion.Inverse(inspected.rotation)*eye.transform.rotation;
        var turn=basis*Quaternion.Euler(pitch,yaw,roll)*Quaternion.Inverse(basis);
        screenRotations[inspected]=turn*(screenRotations.TryGetValue(inspected,out var old)?old:Quaternion.identity);
    }
    static bool ReadAngle(string value,out float degrees)=>float.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out degrees)
        && !float.IsNaN(degrees) && !float.IsInfinity(degrees) && Mathf.Abs(degrees)<=180f;
    void LateUpdate(){
        if(inspection==Inspection.View && OnScreen)ApplyClinicalLens();
        UpdateMobileStart();
        TickClinicalGuide();
        ActualizarBoxeoPantalla();
    }
    void ApplyClinicalLens(){
        // An immersive session can leave its projection on this camera after returning to the canvas.
        float fov=2f*Mathf.Atan(Mathf.Tan(baseFov*Mathf.Deg2Rad*.5f)/viewFactor)*Mathf.Rad2Deg;
        float aspect=(float)eye.pixelWidth/Mathf.Max(1,eye.pixelHeight);
        eye.fieldOfView=fov;
        eye.projectionMatrix=Matrix4x4.Perspective(fov,Mathf.Max(.01f,aspect),eye.nearClipPlane,eye.farClipPlane);
    }
    public void ScreenZoom(float multiplier){
        if(!OnScreen || inspection==Inspection.None || float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier<0)return;
        float factor=multiplier==0?1f:CurrentFactor()*multiplier;
        if(inspection==Inspection.View){
            viewFactor=Mathf.Clamp(factor,1f,2f);
            ApplyClinicalLens();
        }else if(inspected){
            factor=Mathf.Clamp(factor,.5f,2f);
            if(Mathf.Approximately(factor,1f))magnifications.Remove(inspected);
            else magnifications[inspected]=factor;
        }
        PublishInspection();
    }
    public void ScreenFinger(string message){
        if(!OnScreen || inspection==Inspection.None)return;
        var parts=message.Split(',');
        if(parts.Length!=3 || !float.TryParse(parts[1],NumberStyles.Float,CultureInfo.InvariantCulture,out float x)
            || !float.TryParse(parts[2],NumberStyles.Float,CultureInfo.InvariantCulture,out float y))return;
        var pos=new Vector2(x*Screen.width,y*Screen.height);
        lastTouch=Time.unscaledTime;
        switch(parts[0]){
            case "tap": case "down":
                tapEndFrame=parts[0]=="tap"?Time.frameCount+1:-1;
                if(canvasFinger)EndScreenPointer(true);
                canvasFinger=true;lookStart=fingerPosition=pos;touchDragging=false;ScreenPointer(pos,true);break;
            case "move":
                if(!canvasFinger)return;
                touchDragging|=Vector2.Distance(pos,lookStart)>Mathf.Max(8,Screen.height*.011f);
                if(touchDragging && !manipulating && !selected[cursor] && !wrappers[cursor] && !uiTarget[cursor])
                    pendingLook+=(pos-fingerPosition)*(90f/Mathf.Max(320,Screen.height));
                fingerPosition=pos;ScreenPointer(pos,false);break;
            case "pinch":
                pendingLook=Vector2.zero;touchDragging=true;
                if(!dragPiece && canvasFinger){EndScreenPointer(true);canvasFinger=false;}break;
            case "up": case "cancel":
                if(canvasFinger)EndScreenPointer(parts[0]=="cancel" || touchDragging);
                canvasFinger=false;break;
        }
    }
}
