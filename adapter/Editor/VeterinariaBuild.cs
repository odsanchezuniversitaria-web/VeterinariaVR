using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.XR.Management;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using WebXR;

public static class VeterinariaBuild {
    const string Generated="Assets/WebPortGenerated";
    const string Scenes=Generated+"/Scenes";
    const string Gates=Generated+"/Entries";
    static readonly string Out=Path.GetFullPath("../Delivery");
    static readonly string Content=Path.Combine(Out,"Content");
    static string Name(string path)=>Path.GetFileNameWithoutExtension(path);
    [Serializable] class Catalog { public string[] names; }
    static IEnumerable<T> All<T>(Scene s) where T:Component =>
        s.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<T>(true));

    [MenuItem("Veterinaria Web/Compilar las escenas habilitadas")]
    public static void Build(){
        if(!File.Exists("../tools/export_pieces.py") || !File.Exists("Assets/Scenes/0ScenaMenu.unity"))
            throw new InvalidOperationException("Ejecutar desde la copia web independiente.");
        var enabled=EditorBuildSettings.scenes.Where(x=>x.enabled).Select(x=>x.path).ToArray();
        if(enabled.Length!=18 || enabled.Any(p=>!p.StartsWith("Assets/Scenes/")||!File.Exists(p)))
            throw new InvalidOperationException("Se requieren las 18 escenas originales habilitadas.");
        Directory.CreateDirectory(Scenes);Directory.CreateDirectory(Gates);Directory.CreateDirectory(Content);
        AssetDatabase.Refresh();
        SetupXR();
        PlayerSettings.WebGL.template="PROJECT:Veterinaria";
        PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback=true;
        PlayerSettings.WebGL.dataCaching=true;
        PlayerSettings.WebGL.initialMemorySize=128;PlayerSettings.WebGL.maximumMemorySize=1536;
        PlayerSettings.runInBackground=false;
        var levels=new List<AssetBundleBuild>();
        for(int i=0;i<enabled.Length;i++){
            var scene=EditorSceneManager.OpenScene(enabled[i]);
            AdaptScene(scene);
            string path=Scenes+"/"+Name(enabled[i])+".unity";
            if(!EditorSceneManager.SaveScene(scene,path,true))throw new IOException(path);
            levels.Add(new AssetBundleBuild{assetBundleName="nivel-"+i,assetNames=new[]{path}});
        }
        AssetDatabase.SaveAssets();
        // Put multiply-used geometry, audio, prefabs and materials in shared bundles by GUID.
        // Individual heavyweight imports get independent requests instead of duplicating them across levels.
        var owners=levels.SelectMany(l=>AssetDatabase.GetDependencies(l.assetNames,true).Distinct())
            .Where(p=>p.StartsWith("Assets/") && !p.StartsWith(Generated+"/")
                && !new[]{".cs",".dll",".unity",".asmdef",".cginc",".hlsl",".jslib"}.Contains(Path.GetExtension(p).ToLowerInvariant()))
            .GroupBy(p=>p).Where(g=>g.Count()>1).Select(g=>g.Key).OrderBy(p=>p).ToArray();
        var shared=owners.GroupBy(p=>new FileInfo(p).Length>=4*1024*1024?"modelo-"+AssetDatabase.AssetPathToGUID(p):
            "comun-"+AssetDatabase.AssetPathToGUID(p).Substring(0,2)).Select(g=>
            new AssetBundleBuild{assetBundleName=g.Key,assetNames=g.ToArray()});
        var dependencies=AssetDatabase.GetDependencies(levels.SelectMany(l=>l.assetNames).ToArray(),true);
        foreach(string path in dependencies){
            if(!path.StartsWith("Assets/") || !(AssetImporter.GetAtPath(path) is TextureImporter tex))continue;
            var web=tex.GetPlatformTextureSettings("WebGL");
            int size=Math.Min(tex.maxTextureSize,tex.textureType==TextureImporterType.Sprite?1024:512);
            if(web.overridden && web.maxTextureSize<=size)continue;
            web.name="WebGL";web.overridden=true;web.maxTextureSize=size;
            web.format=TextureImporterFormat.Automatic;web.textureCompression=TextureImporterCompression.Compressed;
            tex.SetPlatformTextureSettings(web);tex.SaveAndReimport();
        }
        var manifest=BuildPipeline.BuildAssetBundles(Content,levels.Concat(shared).ToArray(),
            BuildAssetBundleOptions.ChunkBasedCompression|BuildAssetBundleOptions.StrictMode,BuildTarget.WebGL);
        if(!manifest)throw new Exception("Falló la compilación de recursos por escena.");
        var gates=new List<string>();
        foreach(var level in levels){
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var gate=new GameObject("Cargando práctica").AddComponent<WebSceneGate>();
            gate.sceneBundle=level.assetBundleName;gate.scenePath=level.assetNames[0];
            gate.dependencies=manifest.GetAllDependencies(gate.sceneBundle);
            gate.versions=gate.dependencies.Concat(new[]{gate.sceneBundle}).Select(n=>manifest.GetAssetBundleHash(n).ToString()).ToArray();
            var path=Gates+"/"+Name(gate.scenePath)+".unity";
            if(!EditorSceneManager.SaveScene(scene,path))throw new IOException(path);gates.Add(path);
        }
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=gates.ToArray(),target=BuildTarget.WebGL,
            locationPathName=Out,assetBundleManifestPath=Path.Combine(Content,"Content.manifest"),options=BuildOptions.StrictMode});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Falló el reproductor WebGL: "+report.summary.result);
        File.WriteAllText(Path.Combine(Out,"actividades.json"),JsonUtility.ToJson(new Catalog{names=enabled.Select(Name).ToArray()}));
    }
    static string Media(VideoClip clip) {
        if(!clip)return "";
        string path="Media/"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(clip))+".mp4";
        if(!File.Exists(Path.Combine(Out,path)))throw new FileNotFoundException("Falta el vídeo web",path);
        return path;
    }
    static void AdaptScene(Scene scene){
        foreach(var interactive in All<VideoInteractivoVeterinaria>(scene)){
            interactive.urlPrincipal=Media(interactive.videoPrincipal);interactive.urlError=Media(interactive.videoError);
            interactive.videoPrincipal=null;interactive.videoError=null;
        }
        foreach(var video in All<VideoPlayer>(scene)){
            if(!video.clip)continue;
            var url=Media(video.clip);video.clip=null;video.source=VideoSource.Url;video.url=url;
            video.gameObject.AddComponent<WebVideo>().mediaPath=url;
        }
        var oldCamera=All<Camera>(scene).FirstOrDefault(c=>c.CompareTag("MainCamera")) ?? All<Camera>(scene).FirstOrDefault();
        var rig=All<OVRCameraRig>(scene).FirstOrDefault(r=>r.gameObject.activeInHierarchy);
        var start=rig?rig.transform.position:oldCamera?oldCamera.transform.position-Vector3.up*1.6f:Vector3.zero;
        var yaw=rig?rig.transform.eulerAngles.y:oldCamera?oldCamera.transform.eulerAngles.y:0;
        foreach(var r in All<OVRCameraRig>(scene)) r.gameObject.SetActive(false);
        foreach(var m in All<OVRManager>(scene)) m.enabled=false;
        if(oldCamera){oldCamera.enabled=false;oldCamera.tag="Untagged";}
        foreach(var l in All<AudioListener>(scene)) l.enabled=false;
        foreach(var m in All<BaseInputModule>(scene)) m.enabled=false;
        if(!All<EventSystem>(scene).Any(e=>e.isActiveAndEnabled))new GameObject("Eventos web",typeof(EventSystem));
        var player=new GameObject("VeterinariaWeb").AddComponent<WebRig>();
        player.transform.position=start;player.transform.rotation=Quaternion.Euler(0,yaw,0);
        player.tag="Player";
        var body=player.gameObject.AddComponent<CharacterController>();
        body.radius=.16f;body.height=1.6f;body.center=new Vector3(0,.82f,0);
        player.originalEye=rig?rig.centerEyeAnchor:null;
        player.originalLeft=rig?rig.leftHandAnchor:null;player.originalRight=rig?rig.rightHandAnchor:null;
        var go=new GameObject("Cámara web",typeof(Camera),typeof(AudioListener));
        go.tag="MainCamera";go.transform.SetParent(player.transform,false);go.transform.localPosition=Vector3.up*1.6f;
        var eye=go.GetComponent<Camera>();player.eye=eye;
        if(oldCamera){eye.clearFlags=oldCamera.clearFlags;eye.backgroundColor=oldCamera.backgroundColor;eye.cullingMask=oldCamera.cullingMask;}
        eye.nearClipPlane=.035f;eye.farClipPlane=200;eye.stereoTargetEye=StereoTargetEyeMask.None;
        var urp=go.AddComponent<UniversalAdditionalCameraData>();urp.renderPostProcessing=false;urp.renderShadows=false;
        foreach(var canvas in All<Canvas>(scene))if(canvas.renderMode!=RenderMode.ScreenSpaceOverlay)canvas.worldCamera=eye;
        new GameObject("WebXR",typeof(WebXRManager));
        player.left=Hand(player.transform,WebXRControllerHand.LEFT);player.right=Hand(player.transform,WebXRControllerHand.RIGHT);
        player.leftRay=RayVisual(player.left.transform);player.rightRay=RayVisual(player.right.transform);
        var leftPalm=Palm(player.transform,"LeftHandAnchor",-1);
        var rightPalm=Palm(player.transform,"RightHandAnchor",1);
        if(rig){
            var anchors=new Dictionary<Transform,Transform>();
            if(player.originalLeft)anchors[player.originalLeft]=leftPalm;
            if(player.originalRight)anchors[player.originalRight]=rightPalm;
            if(player.originalEye)anchors[player.originalEye]=eye.transform;
            foreach(var behaviour in All<MonoBehaviour>(scene)){
                if(!behaviour||behaviour==player||behaviour.transform.IsChildOf(rig.transform)||behaviour.GetType().Assembly!=typeof(WebRig).Assembly)continue;
                var so=new SerializedObject(behaviour);var prop=so.GetIterator();bool changed=false;
                while(prop.Next(true)){
                    if(prop.propertyType!=SerializedPropertyType.ObjectReference || !(prop.objectReferenceValue is Transform t))continue;
                    if(anchors.TryGetValue(t,out var mapped)){prop.objectReferenceValue=mapped;changed=true;}
                }
                if(changed)so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        player.originalLeft=leftPalm;player.originalRight=rightPalm;player.originalEye=null;
    }
    static Transform Palm(Transform parent,string name,int side){
        var palm=new GameObject(name);palm.tag="Hand";palm.layer=LayerMask.NameToLayer("Ignore Raycast");
        palm.transform.SetParent(parent,false);palm.transform.localPosition=new Vector3(side*.22f,1.25f,.4f);
        var sphere=palm.AddComponent<SphereCollider>();sphere.radius=.045f;sphere.isTrigger=true;
        var rb=palm.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
        return palm.transform;
    }
    static LineRenderer RayVisual(Transform hand){
        string path=Generated+"/Puntero.mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.color=new Color(.3f,.96f,.82f);AssetDatabase.CreateAsset(mat,path);}
        var line=hand.gameObject.AddComponent<LineRenderer>();line.sharedMaterial=mat;line.positionCount=2;
        line.useWorldSpace=true;line.startWidth=.002f;line.endWidth=.005f;line.enabled=false;return line;
    }
    static WebXRController Hand(Transform parent,WebXRControllerHand which){
        var obj=new GameObject(which.ToString());obj.transform.SetParent(parent,false);
        var c=obj.AddComponent<WebXRController>();c.hand=which;return c;
    }
    static void SetupXR(){
        EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,out XRGeneralSettingsPerBuildTarget all);
        if(!all){all=ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();AssetDatabase.CreateAsset(all,Generated+"/Targets.asset");EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey,all,true);}
        var group=BuildTargetGroup.WebGL;
        if(!all.HasSettingsForBuildTarget(group))all.CreateDefaultSettingsForBuildTarget(group);
        if(!all.HasManagerSettingsForBuildTarget(group))all.CreateDefaultManagerSettingsForBuildTarget(group);
        var manager=all.ManagerSettingsForBuildTarget(group);var general=all.SettingsForBuildTarget(group);
        var loader=AssetDatabase.LoadAssetAtPath<WebXRLoader>(Generated+"/Loader.asset");
        if(!loader){loader=ScriptableObject.CreateInstance<WebXRLoader>();AssetDatabase.CreateAsset(loader,Generated+"/Loader.asset");}
        if(!manager.activeLoaders.Contains(loader)&&!manager.TryAddLoader(loader))throw new Exception("Falta el cargador WebXR");
        manager.automaticLoading=true;manager.automaticRunning=true;general.AssignedSettings=manager;general.InitManagerOnStart=true;
        all.SetSettingsForBuildTarget(group,general);
        var xr=WebXRSettings.GetSettings();
        if(!xr){xr=ScriptableObject.CreateInstance<WebXRSettings>();AssetDatabase.CreateAsset(xr,Generated+"/WebXR.asset");EditorBuildSettings.AddConfigObject("WebXR.Settings",xr,true);}
        xr.AutoLoadWebXRManager=false;xr.DisableXRDisplaySubsystem=false;xr.UseFramebufferScaleFactor=true;xr.FramebufferScaleFactor=.8f;
        xr.VRRequiredReferenceSpace=WebXRSettings.ReferenceSpaceTypes.local_floor;
        foreach(var item in new UnityEngine.Object[]{xr,all,general,manager,loader})EditorUtility.SetDirty(item);
        AssetDatabase.SaveAssets();
    }
}

