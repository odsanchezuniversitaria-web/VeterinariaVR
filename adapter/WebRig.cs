using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;
using Oculus.Interaction;
using WebXR;

public sealed class WebRig : MonoBehaviour {
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
    int touchWalk = -1, touchLook = -1;
    Vector2 walkStart, lookStart;
    bool wasVR, touchDragging;
    CharacterController body;
    UniversalAdditionalCameraData renderData;
    void Awake(){body=GetComponent<CharacterController>();WebLegacyInput.rig=this;renderData=eye.GetComponent<UniversalAdditionalCameraData>();}
    public void EnterVR(){if(WebXRManager.Instance)WebXRManager.Instance.ToggleVR();}
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void VeterinariaSceneReady(string scene);
    void Start(){
#if UNITY_WEBGL && !UNITY_EDITOR
        VeterinariaSceneReady(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
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
    public void SwapHand() { WebLegacyInput.useLeft=!WebLegacyInput.useLeft; }
    public void TogglePause() { FindFirstObjectByType<OseoVR.Boxeo.PausaBoxeo>()?.Alternar(); }

    void Update() {
        bool vr = WebXRManager.Instance != null && WebXRManager.Instance.XRState == WebXRState.VR;
        if(renderData && renderData.allowXRRendering!=vr)renderData.allowXRRendering=vr;
        if (vr) {
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (head.TryGetFeatureValue(CommonUsages.centerEyePosition, out Vector3 p)) eye.transform.localPosition = p;
            if (head.TryGetFeatureValue(CommonUsages.centerEyeRotation, out Quaternion q)) eye.transform.localRotation = q;
            walk = left.GetAxis2D(WebXRController.Axis2DTypes.Thumbstick);
            VRPointer(0,left,originalLeft); VRPointer(1,right,originalRight);
        } else {
            if (wasVR) { eye.transform.localPosition=Vector3.up*1.6f; eye.transform.localRotation=Quaternion.identity; }
            walk = Vector2.zero; look = Vector2.zero;
            if(Input.touchCount>0) TouchInput();
            else {
                if(touchWalk>=0||touchLook>=0) { Release(1,eye.transform.rotation); touchWalk=touchLook=-1; }
                walk = new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"));
                if(Input.GetMouseButton(1)) look = new Vector2(Input.GetAxis("Mouse X"),Input.GetAxis("Mouse Y"))*2.5f;
                if(Input.GetKeyDown(KeyCode.Tab))SwapHand();
                int hand=WebLegacyInput.useLeft?0:1;
                HandleRay(hand,eye.ScreenPointToRay(Input.mousePosition),Input.GetMouseButtonDown(0),
                    Input.GetMouseButtonUp(0),eye.transform.rotation,hand==0?originalLeft:originalRight);
            }
            transform.Rotate(Vector3.up,look.x,Space.World);
            pitch=Mathf.Clamp(pitch-look.y,-80,80); eye.transform.localRotation=Quaternion.Euler(pitch,0,0);
        }
        var forward=Vector3.ProjectOnPlane(eye.transform.forward,Vector3.up).normalized;
        walk=Vector2.ClampMagnitude(walk,1);
        var delta=(forward*walk.y+Vector3.Cross(Vector3.up,forward)*walk.x)*Time.unscaledDeltaTime*1.8f;
        if(body)body.Move(delta);else transform.position+=delta;
        if(originalEye) originalEye.SetPositionAndRotation(eye.transform.position,eye.transform.rotation);
        wasVR=vr;
    }
    void TouchInput() {
        for(int n=0;n<Input.touchCount;n++){
            var t=Input.GetTouch(n);
            if(t.phase==TouchPhase.Began) {
                if(t.position.x<Screen.width*.28f && t.position.y<Screen.height*.56f && touchWalk<0){
                    touchWalk=t.fingerId; walkStart=t.position;
                } else if(touchLook<0){touchLook=t.fingerId;lookStart=t.position;touchDragging=false;
                    HandleRay(WebLegacyInput.useLeft?0:1,eye.ScreenPointToRay(t.position),true,false,eye.transform.rotation,WebLegacyInput.useLeft?originalLeft:originalRight);}
            }
            bool end=t.phase==TouchPhase.Ended||t.phase==TouchPhase.Canceled;
            if(t.fingerId==touchWalk){walk=Vector2.ClampMagnitude((t.position-walkStart)/65f,1);if(end){touchWalk=-1;walk=Vector2.zero;}}
            if(t.fingerId!=touchLook)continue;
            if((t.position-lookStart).sqrMagnitude>64)touchDragging=true;
            int hand=WebLegacyInput.useLeft?0:1;
            if(!selected[hand] && touchDragging) look=t.deltaPosition*.14f;
            HandleRay(hand,eye.ScreenPointToRay(t.position),false,end,eye.transform.rotation,hand==0?originalLeft:originalRight);
            if(end)touchLook=-1;
        }
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
    void HandleRay(int i,Ray ray,bool press,bool release,Quaternion rotation,Transform mouseHand) {
        bool hit=Physics.Raycast(ray,out var nearest,10,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Collide);
        if(mouseHand)mouseHand.SetPositionAndRotation(ray.GetPoint(selected[i]?distances[i]:hit?nearest.distance:.65f),rotation);
        if(press && !selected[i] && !ClickUI(i,ray,hit?nearest.distance:10) && hit) {
            wrappers[i]=nearest.collider.GetComponentInParent<InteractableUnityEventWrapper>()??nearest.collider.GetComponentInChildren<InteractableUnityEventWrapper>();
            if(wrappers[i]){wrappers[i].WhenHover.Invoke();wrappers[i].WhenSelect.Invoke();}
            var point=nearest.collider.GetComponentInParent<PointableElement>() ?? nearest.collider.GetComponentInChildren<PointableElement>();
            if(point) {
                selected[i]=point; distances[i]=nearest.distance;
                var pose=new Pose(nearest.point,rotation);
                point.ProcessPointerEvent(new PointerEvent(700+i,PointerEventType.Hover,pose,this));
                point.ProcessPointerEvent(new PointerEvent(700+i,PointerEventType.Select,pose,this));
            }
        }
        if(selected[i]) {
            distances[i]=Mathf.Clamp(distances[i]+Input.mouseScrollDelta.y*.06f,.15f,10);
            selected[i].ProcessPointerEvent(new PointerEvent(700+i,PointerEventType.Move,new Pose(ray.GetPoint(distances[i]),rotation),this));
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
    void Release(int i,Quaternion rotation) {
        if(wrappers[i]){wrappers[i].WhenUnselect.Invoke();wrappers[i].WhenUnhover.Invoke();wrappers[i]=null;}
        if(uiTarget[i]){
            var data=uiData[i];
            ExecuteEvents.Execute(uiTarget[i],data,ExecuteEvents.pointerUpHandler);
            if(data.dragging)ExecuteEvents.Execute(data.pointerDrag,data,ExecuteEvents.endDragHandler);
            else ExecuteEvents.Execute(uiTarget[i],data,ExecuteEvents.pointerClickHandler);
            uiTarget[i]=null;uiCanvas[i]=null;uiData[i]=null;
        }
        if(!selected[i])return;
        var p=new Pose(selected[i].transform.position,rotation);
        selected[i].ProcessPointerEvent(new PointerEvent(700+i,PointerEventType.Unselect,p,this));
        selected[i].ProcessPointerEvent(new PointerEvent(700+i,PointerEventType.Unhover,p,this));
        selected[i]=null;
    }
}

