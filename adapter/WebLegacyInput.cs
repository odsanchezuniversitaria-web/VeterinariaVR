using UnityEngine;
using WebXR;

// Existing veterinary scripts consume OVR-style semantic actions and camera-local poses.
public static class WebLegacyInput {
    public static WebRig rig;
    public static bool useLeft;
    static readonly System.Collections.Generic.Dictionary<OVRInput.Button,int> taps=new System.Collections.Generic.Dictionary<OVRInput.Button,int>();
    public static void Tap(OVRInput.Button button)=>taps[button]=Time.frameCount+1;
    static bool XR => WebXRManager.Instance && WebXRManager.Instance.XRState==WebXRState.VR;
    public static bool GetDown(OVRInput.Button button){
        if(!rig)return false;
        if(taps.TryGetValue(button,out int frame) && Time.frameCount==frame)return true;
        switch(button){
            case OVRInput.Button.One:return Input.GetKeyDown(KeyCode.E) || (XR&&rig.right.GetButtonDown(WebXRController.ButtonTypes.ButtonA));
            case OVRInput.Button.Two:return Input.GetKeyDown(KeyCode.Q) || (XR&&rig.right.GetButtonDown(WebXRController.ButtonTypes.ButtonB));
            case OVRInput.Button.Three:return Input.GetKeyDown(KeyCode.X)||(XR&&rig.left.GetButtonDown(WebXRController.ButtonTypes.ButtonA));
            case OVRInput.Button.Four:return Input.GetKeyDown(KeyCode.Y)||(XR&&rig.left.GetButtonDown(WebXRController.ButtonTypes.ButtonB));
            case OVRInput.Button.Start:return Input.GetKeyDown(KeyCode.P)||(XR&&rig.left.GetButtonDown(WebXRController.ButtonTypes.Thumbstick));
            default:return false;
        }
    }
    public static bool ReadPanelButton(ManoControlador hand,TipoBoton type){
        bool left=hand==ManoControlador.Izquierda;
        var side=left?OVRInput.Controller.LTouch:OVRInput.Controller.RTouch;
        switch(type){
            case TipoBoton.BotonPrimario:return GetDown(left?OVRInput.Button.Three:OVRInput.Button.One);
            case TipoBoton.BotonSecundario:return GetDown(left?OVRInput.Button.Four:OVRInput.Button.Two);
            case TipoBoton.Gatillo:return Get(OVRInput.Button.PrimaryIndexTrigger,side);
            case TipoBoton.Grip:return Get(OVRInput.Button.PrimaryHandTrigger,side);
            default:return GetDown(OVRInput.Button.Start);
        }
    }
    public static bool Get(OVRInput.Button button,OVRInput.Controller device=OVRInput.Controller.Touch){
        if(!rig)return false;
        if(XR){
            var action=button==OVRInput.Button.PrimaryHandTrigger?WebXRController.ButtonTypes.Grip:WebXRController.ButtonTypes.Trigger;
            return device!=OVRInput.Controller.RTouch && rig.left.GetButton(action) ||
                   device!=OVRInput.Controller.LTouch && rig.right.GetButton(action);
        }
        return (device==OVRInput.Controller.Touch || (device==OVRInput.Controller.LTouch)==useLeft) &&
            (Input.GetMouseButton(0)||Input.touchCount>0);
    }
    public static Vector3 GetLocalControllerPosition(OVRInput.Controller device){
        if(!rig)return Vector3.zero;
        var hand=device==OVRInput.Controller.LTouch?rig.originalLeft:rig.originalRight;
        return hand?rig.transform.InverseTransformPoint(hand.position):Vector3.zero;
    }
    public static void SetControllerVibration(float frequency,float strength,OVRInput.Controller device){
        if(!rig||!XR)return;
        if(device!=OVRInput.Controller.RTouch)rig.left.Pulse(strength,80);
        if(device!=OVRInput.Controller.LTouch)rig.right.Pulse(strength,80);
    }
}

