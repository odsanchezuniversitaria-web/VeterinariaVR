using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;

public sealed class WebScreenView {
    static HashSet<string> passthroughScenes;
    static Material observatorio4K;
    static Material observatorioTactil;
    readonly WebRig owner;
    Coroutine loadingPanorama;
    UnityWebRequest panoramaRequest;
    readonly Dictionary<RawImage,Texture> guideTextures=new Dictionary<RawImage,Texture>();
    Renderer returnBox;
    Material originalReturnMaterial;
    static readonly Vector3[] Footprint={Vector3.zero,Vector3.right,Vector3.left,Vector3.forward,Vector3.back};
    readonly bool isMenu;
    readonly GameObject upperStartButton;
    bool upperStartWasActive;
    readonly UniversalAdditionalCameraData renderData;
    readonly bool authoredPost;
    readonly bool authoredBodyEnabled;
    Collider menuFloor;
    Vector3 menuStart;
    readonly Transform rig;
    readonly Camera camera;
    readonly CharacterController body;
    readonly Vector3 authoredPosition;
    readonly Quaternion authoredRotation;
    readonly Vector3 authoredEye;
    readonly Quaternion authoredEyeRotation;
    readonly CameraClearFlags authoredClear;
    readonly Skybox sky;
    readonly Material authoredSky;
    readonly bool skyWasEnabled;
    Vector3 screenPosition;
    Quaternion screenRotation;
    bool visited;
    public bool Active { get; private set; }
    public WebScreenView(WebRig owner, CharacterController controller) {
        this.owner=owner;
        rig=owner.transform; camera=owner.eye; body=controller;
        isMenu=rig.gameObject.scene.name=="0ScenaMenu";
        if(isMenu){
            foreach(var root in rig.gameObject.scene.GetRootGameObjects()){
                var menu=root.GetComponentInChildren<MenuPrincipalManager>(true);
                if(menu){upperStartButton=menu.boton1;break;}
            }
        }
        authoredBodyEnabled=body && body.enabled;
        renderData=camera.GetComponent<UniversalAdditionalCameraData>();
        authoredPost=renderData && renderData.renderPostProcessing;
        authoredPosition=rig.position; authoredRotation=rig.rotation;
        authoredEye=camera.transform.localPosition; authoredEyeRotation=camera.transform.localRotation;
        authoredClear=camera.clearFlags;
        sky=camera.GetComponent<Skybox>();
        if(sky){authoredSky=sky.material;skyWasEnabled=sky.enabled;}
        else {sky=camera.gameObject.AddComponent<Skybox>();sky.enabled=false;}
    }
    public void Enter() {
        if(!visited) {
            Physics.SyncTransforms();
            screenPosition=rig.position;
            screenRotation=rig.rotation; visited=true;
            if(TryFindFloor(screenPosition,out float floor,out var support)){
                screenPosition.y=floor+.025f;
                if(isMenu){menuFloor=support;}
                else if(!CanStand(screenPosition)){
                    var centre=screenPosition;
                    foreach(float radius in new[]{.65f,1.15f,1.65f,2.2f}){
                        foreach(float angle in new[]{0f,-45f,45f,-90f,90f,-135f,135f,180f}){
                            var candidate=centre+Quaternion.AngleAxis(angle,Vector3.up)*(rig.rotation*Vector3.back)*radius;
                            if(!TryFindFloor(candidate,out float nearby,out _) || Mathf.Abs(nearby-floor)>.30f)continue;
                            candidate.y=nearby+.025f;
                            if(!CanStand(candidate))continue;
                            screenPosition=candidate;
                            screenRotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(centre-candidate,Vector3.up));
                            break;
                        }
                        if(screenPosition!=centre)break;
                    }
                }
            }else if(screenPosition.y<0)screenPosition.y=0;
            menuStart=screenPosition;
        }
        if(upperStartButton){
            upperStartWasActive=upperStartButton.activeSelf;
            upperStartButton.SetActive(false);
        }
        if(isMenu && body)body.enabled=false;
        SetPose(screenPosition,screenRotation);
        camera.transform.localPosition=Vector3.up*1.6f;
        var sceneSky=authoredSky?authoredSky:RenderSettings.skybox;
        bool defaultSky=!sceneSky || sceneSky.name.StartsWith("Default",System.StringComparison.OrdinalIgnoreCase);
        if(passthroughScenes==null){
            var catalog=Resources.Load<TextAsset>("VeterinariaPantalla/EscenasPassthrough");
            passthroughScenes=new HashSet<string>(catalog.text.Split(new[]{'\r','\n'},System.StringSplitOptions.RemoveEmptyEntries));
        }
        bool observatorio=passthroughScenes.Contains(rig.gameObject.scene.name);
        bool large=observatorio && UseLargePanorama() && SystemInfo.maxTextureSize>=4096;
        sky.material=observatorio?ScreenLaboratory(large):
            defaultSky?Resources.Load<Material>("VeterinariaPantalla/Laboratorio"):sceneSky;
        sky.enabled=true;camera.clearFlags=CameraClearFlags.Skybox;
        if(isMenu && renderData){renderData.renderPostProcessing=true;Debug.Log("Posprocesado del menú en pantalla activo");}
        Active=true;
        if(rig.gameObject.scene.name=="1ScenaOseo")ShowScreenGuide();
        if(observatorio){
            ReportPanorama();
            if(!observatorio4K && large)
                loadingPanorama=owner.StartCoroutine(LoadLargePanorama(sky.material));
        }
        Debug.Log("Vista de pantalla "+rig.gameObject.scene.name+" · pies "+screenPosition.ToString("F2")+" · ojos "+camera.transform.position.y.ToString("F2")+" · fondo "+sky.material.name);
    }
    public void Leave() {
        screenPosition=rig.position;screenRotation=rig.rotation;
        CancelPanorama();
        foreach(var entry in guideTextures)if(entry.Key)entry.Key.texture=entry.Value;
        if(returnBox){returnBox.sharedMaterial=originalReturnMaterial;returnBox=null;originalReturnMaterial=null;}
        if(guideTextures.Count>0)Debug.Log("Guía ósea original restaurada para el visor");
        guideTextures.Clear();
        sky.material=authoredSky;sky.enabled=skyWasEnabled;camera.clearFlags=authoredClear;
        if(passthroughScenes.Contains(rig.gameObject.scene.name))
            Debug.Log("Cámara de visor restaurada · "+rig.gameObject.scene.name+" · observatorio="+
                (sky.enabled && sky.material && sky.material.name=="Observatorio"));
        if(isMenu && renderData)renderData.renderPostProcessing=authoredPost;
        SetPose(authoredPosition,authoredRotation);
        if(upperStartButton)upperStartButton.SetActive(upperStartWasActive);
        if(isMenu && body)body.enabled=authoredBodyEnabled;
        camera.transform.SetLocalPositionAndRotation(authoredEye,authoredEyeRotation);
        Active=false;
    }
    void ShowScreenGuide(){
        var cutout=Resources.Load<Texture2D>("VeterinariaPantalla/GuiaOseaTransparente");
        foreach(var image in UnityEngine.Object.FindObjectsByType<RawImage>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
            if(image.gameObject.scene!=rig.gameObject.scene || !image.texture || image.texture.name!="oseoGrandeCanino")continue;
            guideTextures.Add(image,image.texture);
            image.texture=cutout;
        }
        foreach(var root in rig.gameObject.scene.GetRootGameObjects()){
            var zone=root.GetComponentInChildren<OseoVR.Oseo.ZonaDevolverHuesos>(true);
            if(!zone || !zone.caja)continue;
            returnBox=zone.caja;
            originalReturnMaterial=returnBox.sharedMaterial;
            returnBox.sharedMaterial=Resources.Load<Material>("VeterinariaPantalla/MarcoDevolucion");
            break;
        }
        Debug.Log("Guía ósea transparente en pantalla · "+guideTextures.Count);
    }
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern int VeterinariaUseLargePanorama();
    static Material ScreenLaboratory(bool large){
        if(large && observatorio4K)return observatorio4K;
        if(!large && observatorioTactil)return observatorioTactil;
        var panorama=Resources.Load<Material>("VeterinariaPantalla/Observatorio");
        if(large)return panorama;
        // Keep the edge of the viewing window in the narrower phone field of view.
        observatorioTactil=new Material(panorama){name="Observatorio"};
        observatorioTactil.SetFloat("_Rotation",8f);
        return observatorioTactil;
    }
    static bool UseLargePanorama(){
#if UNITY_WEBGL && !UNITY_EDITOR
        return VeterinariaUseLargePanorama()!=0;
#else
        return !Application.isMobilePlatform;
#endif
    }
    IEnumerator LoadLargePanorama(Material baseMaterial){
        string url=Application.streamingAssetsPath+"/VeterinariaPantalla/observatorio-4k-v1.jpg";
        var request=UnityWebRequestTexture.GetTexture(url,true);
        panoramaRequest=request;
        try{
            yield return request.SendWebRequest();
            loadingPanorama=null;
            if(request.result!=UnityWebRequest.Result.Success){
                Debug.LogWarning("Se mantiene la panorámica de 2K: "+request.error);
                yield break;
            }
            if(!Active)yield break;
            var texture=DownloadHandlerTexture.GetContent(request);
            texture.wrapModeU=TextureWrapMode.Repeat;texture.wrapModeV=TextureWrapMode.Clamp;
            texture.filterMode=FilterMode.Trilinear;
            observatorio4K=new Material(baseMaterial){name="Observatorio",mainTexture=texture};
            sky.material=observatorio4K;
            ReportPanorama();
        }finally{
            if(panoramaRequest==request){panoramaRequest=null;request.Dispose();}
        }
    }
    public void CancelPanorama(){
        if(panoramaRequest!=null)panoramaRequest.Abort();
        if(loadingPanorama!=null){owner.StopCoroutine(loadingPanorama);loadingPanorama=null;}
        if(panoramaRequest!=null){panoramaRequest.Dispose();panoramaRequest=null;}
    }
    void ReportPanorama(){
        var image=sky.material.mainTexture;
        Debug.Log("Laboratorio panorámico · "+rig.gameObject.scene.name+" · "+image.width+"x"+image.height);
    }
    // The menu's overlapping scenery pushes a CharacterController sideways even on Move(0).
    // Follow only the actual platform, with enough space to navigate around the menu panels.
    public bool MoveOnMenuPlatform(Vector3 delta){
        if(!isMenu)return false;
        if(!menuFloor || delta.sqrMagnitude<.00000001f)return true;
        var offset=Quaternion.Inverse(authoredRotation)*(rig.position+Vector3.ClampMagnitude(delta,.18f)-menuStart);
        offset.z=Mathf.Min(offset.z,.7f);
        offset=Vector3.ClampMagnitude(new Vector3(offset.x,0,offset.z),2.5f);
        var target=menuStart+authoredRotation*offset;
        float height=menuStart.y;
        foreach(var direction in Footprint){
            var ray=new Ray(target+direction*.22f+Vector3.up*.35f,Vector3.down);
            if(!menuFloor.Raycast(ray,out var hit,.65f) || hit.normal.y<.85f)return true;
            if(direction==Vector3.zero)height=hit.point.y+.025f;
        }
        target.y=height;
        rig.position=target;
        return true;
    }
    void SetPose(Vector3 position,Quaternion rotation) {
        bool enabled=body && body.enabled;
        if(enabled)body.enabled=false;
        rig.SetPositionAndRotation(position,rotation);
        if(enabled)body.enabled=true;
    }
    bool CanStand(Vector3 feet){
        foreach(var c in Physics.OverlapCapsule(feet+Vector3.up*.28f,feet+Vector3.up*1.48f,.21f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            if(!c.transform.IsChildOf(rig))return false;
        return true;
    }
    static bool TryFindFloor(Vector3 start,out float height,out Collider support) {
        height=start.y;support=null;float best=float.PositiveInfinity;
        foreach(var hit in Physics.RaycastAll(start+Vector3.up*8,Vector3.down,16,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)){
            var c=hit.collider;var bounds=c.bounds;
            if(hit.normal.y<.8f || c is CharacterController || c.attachedRigidbody && !c.attachedRigidbody.isKinematic)continue;
            var name=c.name.ToLowerInvariant();
            bool floor=name.Contains("floor")||name.Contains("suelo")||name.Contains("piso")||name.Contains("ground")||
                name=="plane" && bounds.size.x>2 && bounds.size.z>2;
            if(!floor && (bounds.size.x<3 || bounds.size.z<3 || bounds.size.y>1))continue;
            if(name.Contains("roof")||name.Contains("techo")||name.Contains("ceiling"))continue;
            float score=Mathf.Abs(hit.point.y-start.y)+(floor?0:1);
            if(score<best){height=hit.point.y;best=score;support=c;}
        }
        return best<float.PositiveInfinity;
    }
}
