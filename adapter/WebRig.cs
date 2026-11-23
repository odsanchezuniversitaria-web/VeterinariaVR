using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;
using Oculus.Interaction;
using WebXR;

public sealed partial class WebRig : MonoBehaviour {
    static Material[] materialsForDynamicShaders;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CargarShaders(){ materialsForDynamicShaders=Resources.LoadAll<Material>("VeterinariaShaders"); }
    public Camera eye;
    public Transform originalEye, originalLeft, originalRight;
    public WebXRController left, right;
    readonly PointableElement[] selected = new PointableElement[2];
    readonly float[] distances = new float[2];
    readonly List<RaycastResult> ui = new List<RaycastResult>();
    readonly GameObject[] uiTarget=new GameObject[2];
    readonly GraphicRaycaster[] uiCanvas=new GraphicRaycaster[2];
    readonly PointerEventData[] uiData=new PointerEventData[2];
    readonly InteractableUnityEventWrapper[] wrappers=new InteractableUnityEventWrapper[2];
    public LineRenderer leftRay,rightRay;
    Vector2 look, walk;
    float pitch;
    int touchLook = -1;
    Vector2 lookStart, stick;
    bool touchDragging;
    WebScreenView screenView;
    CharacterController body;
    UniversalAdditionalCameraData renderData;
    void Awake(){baseFov=eye.fieldOfView;body=GetComponent<CharacterController>();WebLegacyInput.rig=this;renderData=eye.GetComponent<UniversalAdditionalCameraData>();}
    public void EnterVR(){if(WebXRManager.Instance)WebXRManager.Instance.ToggleVR();}
    void OnDestroy(){LeaveClinicalGuide();LeaveInspection();screenView?.CancelPanorama();}
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void VeterinariaSceneReady(string scene);
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void VeterinariaScreenMode(int flat);
    static void ReportMode(bool flat){
#if UNITY_WEBGL && !UNITY_EDITOR
        VeterinariaScreenMode(flat?1:0);
#endif
    }
    void Start(){
#if UNITY_WEBGL && !UNITY_EDITOR
        VeterinariaSceneReady(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        AnnounceActions();
#endif
    }
    public void OpenLevel(string level){
        for(int n=0;n<UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;n++){
            var path=UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(n);
            if(System.IO.Path.GetFileNameWithoutExtension(path)==level){
                UnityEngine.SceneManagement.SceneManager.LoadScene(n);return;
            }
        }
    }
    public void ActionA() { WebLegacyInput.Tap(OVRInput.Button.One); }
    public void ActionB() { WebLegacyInput.Tap(OVRInput.Button.Two); }
    public void ActionX() { WebLegacyInput.Tap(OVRInput.Button.Three); }
    public void ActionY() { WebLegacyInput.Tap(OVRInput.Button.Four); }
    public void ActionMenu() { WebLegacyInput.Tap(OVRInput.Button.Start); }
    public void TogglePause() { FindFirstObjectByType<OseoVR.Boxeo.PausaBoxeo>()?.Alternar(); }

    void Update() {
        bool vr = WebXRManager.Instance != null && WebXRManager.Instance.XRState == WebXRState.VR;
        if(renderData && renderData.allowXRRendering!=vr)renderData.allowXRRendering=vr;
        if (vr) {
            if(screenView!=null && screenView.Active){EndScreenPointer(true);stick=Vector2.zero;LeaveClinicalGuide();LeaveInspection();screenView.Leave();}
            if(!wasImmersive)ReportMode(false);
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (head.TryGetFeatureValue(CommonUsages.centerEyePosition, out Vector3 p)) eye.transform.localPosition = p;
            if (head.TryGetFeatureValue(CommonUsages.centerEyeRotation, out Quaternion q)) eye.transform.localRotation = q;
            walk = left.GetAxis2D(WebXRController.Axis2DTypes.Thumbstick);
            VRPointer(0,left,originalLeft); VRPointer(1,right,originalRight);
        } else {
            if(screenView==null)screenView=new WebScreenView(this,body);
            if(!screenView.Active){Release(0,eye.transform.rotation);Release(1,eye.transform.rotation);screenView.Enter();EnterInspection();EnterClinicalGuide();ReportMode(true);}
            if(leftRay)leftRay.enabled=false;
            if(rightRay)rightRay.enabled=false;
            ScreenInput();
            transform.Rotate(Vector3.up,look.x,Space.World);
            pitch=Mathf.Clamp(pitch-look.y,-80,80); eye.transform.localRotation=Quaternion.Euler(pitch,0,0);
        }
        var forward=Vector3.ProjectOnPlane(eye.transform.forward,Vector3.up).normalized;
        walk=Vector2.ClampMagnitude(walk,1);
        var delta=(forward*walk.y+Vector3.Cross(Vector3.up,forward)*walk.x)*Time.unscaledDeltaTime*1.8f;
        if(vr || !screenView.MoveOnMenuPlatform(delta)){
            if(body)body.Move(delta);else transform.position+=delta;
        }
        if(originalEye) originalEye.SetPositionAndRotation(eye.transform.position,eye.transform.rotation);
        wasImmersive=vr;
    }
    void VRPointer(int i,WebXRController c,Transform hand) {
        var line=i==0?leftRay:rightRay;
        if(line)line.enabled=c.isControllerActive;
        if(!c.isControllerActive){Release(i,c.transform.rotation);return;}
        if(line){
            line.SetPosition(0,c.transform.position);
            line.SetPosition(1,c.transform.position+c.transform.forward*(selected[i]?distances[i]:2));
        }
        if(hand) hand.SetPositionAndRotation(c.gripPosition==Vector3.zero?c.transform.position:transform.TransformPoint(c.gripPosition),c.transform.rotation);
        bool press=c.GetButtonDown(WebXRController.ButtonTypes.Trigger)||c.GetButtonDown(WebXRController.ButtonTypes.Grip);
        bool release=!c.GetButton(WebXRController.ButtonTypes.Trigger)&&!c.GetButton(WebXRController.ButtonTypes.Grip);
        HandleRay(i,new Ray(c.transform.position,c.transform.forward),press,release,c.transform.rotation,null);
    }
    bool ClickUI(int i,Ray ray,float limit) {
        var events=EventSystem.current;
        if(!events)return false;
        var data=new PointerEventData(events){button=PointerEventData.InputButton.Left};
        // For both controller and mouse rays, intersect the canvas plane first.
        foreach(var canvas in FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None)) {
            if(!canvas.isActiveAndEnabled)continue;
            var surface=canvas.GetComponent<Canvas>();
            var plane=new Plane(canvas.transform.forward,canvas.transform.position);
            if(surface.renderMode!=RenderMode.ScreenSpaceOverlay){
                if(!plane.Raycast(ray,out float t)||t>limit+.1f)continue;
                surface.worldCamera=eye; data.position=eye.WorldToScreenPoint(ray.GetPoint(t));
            } else data.position=eye.WorldToScreenPoint(ray.GetPoint(1));
            ui.Clear(); canvas.Raycast(data,ui);
            foreach(var hit in ui) {
                var target=ExecuteEvents.GetEventHandler<IPointerDownHandler>(hit.gameObject)
                    ??ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject);
                if(!target)continue;
                data.pressPosition=data.position;data.pointerPressRaycast=hit;data.pointerCurrentRaycast=hit;
                data.pointerPress=target;data.pointerDrag=ExecuteEvents.GetEventHandler<IDragHandler>(hit.gameObject);
                data.pointerId=700+i;uiTarget[i]=target;uiData[i]=data;uiCanvas[i]=canvas;
                ExecuteEvents.Execute(target,data,ExecuteEvents.pointerDownHandler);
                if(data.pointerDrag)ExecuteEvents.Execute(data.pointerDrag,data,ExecuteEvents.initializePotentialDrag);
                return true;
            }
        }
        return false;
    }
    void HandleRay(int i,Ray ray,bool press,bool release,Quaternion rotation,Transform mouseHand,bool screen=false) {
        bool hit=Physics.Raycast(ray,out var nearest,10,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Collide);
        float limit=hit?nearest.distance:10;
        Vector3 localHit=Vector3.zero;
        if(screen && press)hit=PickScreenRay(ray,out nearest,out limit,out localHit);
        if(screen && press && hit){
            screenDepth=nearest.distance;
        }
        if(mouseHand)mouseHand.SetPositionAndRotation(ray.GetPoint(selected[i]?distances[i]:screen && ScreenHeld?screenDepth:hit?nearest.distance:.65f),rotation);
        if(press && !selected[i] && !ClickUI(i,ray,limit) && hit) {
            if(screen)PickSuture(nearest.collider);
            wrappers[i]=nearest.collider.GetComponentInParent<InteractableUnityEventWrapper>()??nearest.collider.GetComponentInChildren<InteractableUnityEventWrapper>();
            if(wrappers[i]){wrappers[i].WhenHover.Invoke();wrappers[i].WhenSelect.Invoke();}
            var grip=screen && inspection==Inspection.Piece?ScreenGrab(nearest.collider):null;
            var point=(PointableElement)grip ?? nearest.collider.GetComponentInParent<PointableElement>() ?? nearest.collider.GetComponentInChildren<PointableElement>();
            if(point) {
                selected[i]=point; distances[i]=nearest.distance;
                var contact=grip && grip.Transform?BeginPieceDrag(grip,nearest.point,localHit):nearest.point;
                var pose=new Pose(contact,rotation);
                point.ProcessPointerEvent(new PointerEvent(700+i,PointerEventType.Hover,pose,this));
                point.ProcessPointerEvent(new PointerEvent(700+i,PointerEventType.Select,pose,this));
            }
        }
        if(selected[i]) {
            float wheel=Input.mouseScrollDelta.y;
            if(screen && inspection!=Inspection.None && (Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift)))wheel=0;
            Vector3 position;
            if(screen && dragPiece)position=PiecePointer(ray,wheel);
            else{
                distances[i]=Mathf.Clamp(distances[i]+wheel*.06f,.15f,10);
                position=ray.GetPoint(distances[i]);
            }
            selected[i].ProcessPointerEvent(new PointerEvent(700+i,PointerEventType.Move,new Pose(position,rotation),this));
        }
        if(uiTarget[i] && uiCanvas[i]){
            var pane=uiCanvas[i].transform;var plane=new Plane(pane.forward,pane.position);
            if(plane.Raycast(ray,out float distance)){
                var data=uiData[i];var pos=(Vector2)eye.WorldToScreenPoint(ray.GetPoint(distance));data.delta=pos-data.position;data.position=pos;
                if(data.pointerDrag && !release && (pos-data.pressPosition).sqrMagnitude>9){
                    if(!data.dragging){data.dragging=true;ExecuteEvents.Execute(data.pointerDrag,data,ExecuteEvents.beginDragHandler);}
                    ExecuteEvents.Execute(data.pointerDrag,data,ExecuteEvents.dragHandler);
                }
            }
        }
        if(release)Release(i,rotation);
    }
    void Release(int i,Quaternion rotation,bool cancel=false) {
        if(wrappers[i]){wrappers[i].WhenUnselect.Invoke();wrappers[i].WhenUnhover.Invoke();wrappers[i]=null;}
        if(uiTarget[i]){
            var data=uiData[i];
            ExecuteEvents.Execute(uiTarget[i],data,ExecuteEvents.pointerUpHandler);
            if(data.dragging)ExecuteEvents.Execute(data.pointerDrag,data,ExecuteEvents.endDragHandler);
            else if(!cancel)ExecuteEvents.Execute(uiTarget[i],data,ExecuteEvents.pointerClickHandler);
            uiTarget[i]=null;uiCanvas[i]=null;uiData[i]=null;
        }
        if(!selected[i])return;
        var p=new Pose(selected[i].transform.position,rotation);
        selected[i].ProcessPointerEvent(new PointerEvent(700+i,PointerEventType.Unselect,p,this));
        selected[i].ProcessPointerEvent(new PointerEvent(700+i,PointerEventType.Unhover,p,this));
        selected[i]=null;
    }
}

