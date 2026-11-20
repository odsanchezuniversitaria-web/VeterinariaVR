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
        WebAnatomyMeshes.Prepare(enabled);
        var usedPrefabs=AssetDatabase.GetDependencies(enabled,true).Where(p=>p.StartsWith("Assets/") && p.EndsWith(".prefab"));
        var videoPrefabs=Directory.EnumerateFiles("Assets","*.prefab",SearchOption.AllDirectories)
            .Where(p=>File.ReadLines(p).Any(line=>line.Contains("m_VideoClip: {fileID: 32900000") || line.Contains("videoPrincipal: {fileID: 32900000")))
            .Select(p=>p.Replace('\\','/'));
        foreach(var path in usedPrefabs.Concat(videoPrefabs).Distinct()){
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(!prefab || !(prefab.GetComponentsInChildren<VideoPlayer>(true).Any(v=>v.clip) ||
               prefab.GetComponentsInChildren<VideoInteractivoVeterinaria>(true).Any(v=>v.videoPrincipal||v.videoError)))continue;
            using(var edit=new PrefabUtility.EditPrefabContentsScope(path)){
                AdaptVideos(edit.prefabContentsRoot.GetComponentsInChildren<VideoInteractivoVeterinaria>(true),
                            edit.prefabContentsRoot.GetComponentsInChildren<VideoPlayer>(true));
            }
        }
        var levels=new List<AssetBundleBuild>();
        for(int i=0;i<enabled.Length;i++){
            var scene=EditorSceneManager.OpenScene(enabled[i]);
            AdaptScene(scene);
            string path=Scenes+"/"+Name(enabled[i])+".unity";
            if(!EditorSceneManager.SaveScene(scene,path,true))throw new IOException(path);
            levels.Add(new AssetBundleBuild{assetBundleName="nivel-"+i,assetNames=new[]{path}});
        }
        AssetDatabase.SaveAssets();
        // Only pack assets together if exactly the same activities use them. This
        // prevents the menu from indirectly downloading resources for later practices.
        var usage=new Dictionary<string,int>();
        var excluded=new HashSet<string>(StringComparer.OrdinalIgnoreCase){".cs",".dll",".unity",".asmdef",".cginc",".hlsl",".jslib",".mp4",".mov",".webm"};
        for(int i=0;i<levels.Count;i++){
            foreach(string path in AssetDatabase.GetDependencies(levels[i].assetNames,true)){
                if(!path.StartsWith("Assets/") || !File.Exists(path) || excluded.Contains(Path.GetExtension(path)))continue;
                usage.TryGetValue(path,out int mask);
                usage[path]=mask | (1<<i);
            }
        }
        var shared=new List<AssetBundleBuild>();
        foreach(var group in usage.GroupBy(x=>x.Value).OrderBy(g=>g.Key)){
            var batch=new List<string>();long bytes=0;int part=0;
            void Flush(){
                if(batch.Count==0)return;
                shared.Add(new AssetBundleBuild{assetBundleName=$"rec-{group.Key:x}-{part++}",assetNames=batch.ToArray()});
                batch.Clear();bytes=0;
            }
            foreach(var entry in group.OrderBy(x=>x.Key,StringComparer.Ordinal)){
                long size=new FileInfo(entry.Key).Length;
                bool unique=(group.Key & (group.Key-1))==0;
                if(unique && size<4*1024*1024)continue;
                if(bytes+size>12*1024*1024)Flush();
                batch.Add(entry.Key);bytes+=size;
            }
            Flush();
        }
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
        var required=levels.SelectMany(l=>manifest.GetAllDependencies(l.assetBundleName).Append(l.assetBundleName)).Distinct().OrderBy(n=>n).ToArray();
        File.WriteAllText(Path.Combine(Out,"bundles.json"),JsonUtility.ToJson(new Catalog{names=required}));
        var hashes=required.ToDictionary(n=>n,n=>manifest.GetAssetBundleHash(n).ToString());
        var levelDependencies=levels.ToDictionary(l=>l.assetBundleName,l=>manifest.GetAllDependencies(l.assetBundleName));
        var gates=new List<string>();
        foreach(var level in levels){
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var gate=new GameObject("Cargando práctica").AddComponent<WebSceneGate>();
            gate.sceneBundle=level.assetBundleName;gate.scenePath=level.assetNames[0];
            gate.dependencies=levelDependencies[gate.sceneBundle];
            gate.versions=gate.dependencies.Concat(new[]{gate.sceneBundle}).Select(n=>hashes[n]).ToArray();
            var path=Gates+"/"+Name(gate.scenePath)+".unity";
            if(!EditorSceneManager.SaveScene(scene,path))throw new IOException(path);gates.Add(path);
        }
        BuildPlayer(gates.ToArray(), enabled);
    }
    [MenuItem("Veterinaria Web/Recompilar reproductor con los paquetes existentes")]
    public static void RebuildPlayer(){
        if(!File.Exists(Path.Combine(Out,"bundles.json")))throw new InvalidOperationException("Falta el catálogo de paquetes");
        var enabled=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
        var gates=enabled.Select(p=>Gates+"/"+Name(p)+".unity").ToArray();
        if(gates.Length!=18 || gates.Any(p=>!File.Exists(p)))throw new InvalidOperationException("Faltan entradas de escena");
        SetupXR();BuildPlayer(gates,enabled);
    }
    static void BuildPlayer(string[] gates,string[] enabled){
        const string screenFolder="Assets/VeterinariaWeb/Resources/VeterinariaPantalla";
        Directory.CreateDirectory(screenFolder);
        AssetDatabase.Refresh();
        PrepareScreenEnvironment(screenFolder,enabled);
        WebSkeletonGuide.Build(screenFolder);
        var lab=AssetDatabase.LoadAssetAtPath<Cubemap>("Assets/Scenes/Scena7 LaboratorioClinico/ReflectionProbe-0.exr");
        var skyShader=Shader.Find("Skybox/Cubemap");
        if(!lab || !skyShader)throw new InvalidOperationException("Falta el entorno del laboratorio para pantalla");
        var skyPath=screenFolder+"/Laboratorio.mat";
        var sky=AssetDatabase.LoadAssetAtPath<Material>(skyPath);
        if(!sky){sky=new Material(skyShader);AssetDatabase.CreateAsset(sky,skyPath);}
        sky.SetTexture("_Tex",lab);sky.SetFloat("_Exposure",1.1f);
        EditorUtility.SetDirty(sky);
        const string shadersFolder="Assets/VeterinariaWeb/Resources/VeterinariaShaders";
        Directory.CreateDirectory(shadersFolder);
        AssetDatabase.Refresh();
        foreach(var shaderName in new[]{"UI/RoundedCorners/RoundedCorners","UI/RoundedCorners/IndependentRoundedCorners","Sprites/Default","UI/Default","Unlit/Color","Universal Render Pipeline/Simple Lit","Universal Render Pipeline/Unlit"}){
            var shader=Shader.Find(shaderName);
            if(!shader)throw new InvalidOperationException("Shader de práctica ausente: "+shaderName);
            var path=shadersFolder+"/"+shaderName.Replace('/','-')+".mat";
            if(!AssetDatabase.LoadAssetAtPath<Material>(path))AssetDatabase.CreateAsset(new Material(shader),path);
        }
        AssetDatabase.SaveAssets();
        PlayerSettings.WebGL.debugSymbolMode=WebGLDebugSymbolMode.External;
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=gates,target=BuildTarget.WebGL,
            locationPathName=Out,assetBundleManifestPath=Path.Combine(Content,"Content.manifest"),options=BuildOptions.StrictMode});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Falló el reproductor WebGL: "+report.summary.result);
        File.WriteAllText(Path.Combine(Out,"actividades.json"),JsonUtility.ToJson(new Catalog{names=enabled.Select(Name).ToArray()}));
    }
    static void PrepareScreenEnvironment(string folder,string[] scenes){
        var passthrough=new List<string>();
        foreach(string path in scenes){
            var original=EditorSceneManager.OpenPreviewScene(path);
            try{
                bool manager=All<OVRManager>(original).Any(m=>m.isActiveAndEnabled && m.isInsightPassthroughEnabled);
                bool layer=All<OVRPassthroughLayer>(original).Any(p=>p.isActiveAndEnabled && !p.hidden);
                Debug.Log($"Entorno original {Name(path)} | sistema={manager} | capa={layer}");
                if(manager && layer)passthrough.Add(Name(path));
            }finally{EditorSceneManager.ClosePreviewScene(original);}
        }
        if(!passthrough.Contains("1ScenaOseo") || passthrough.Contains("0ScenaMenu"))
            throw new InvalidOperationException("Revisar la detección de passthrough en las escenas originales");
        string catalog=folder+"/EscenasPassthrough.txt";
        File.WriteAllLines(catalog,passthrough);
        AssetDatabase.ImportAsset(catalog);
        const string imagePath="Assets/VeterinariaWeb/Entornos/Observatorio.jpg";
        var importer=(TextureImporter)AssetImporter.GetAtPath(imagePath);
        importer.textureShape=TextureImporterShape.Texture2D;
        importer.sRGBTexture=true;importer.mipmapEnabled=true;importer.maxTextureSize=2048;
        // Smooth gradients and the thin window rim should survive on mobile GPUs.
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.wrapModeU=TextureWrapMode.Repeat;importer.wrapModeV=TextureWrapMode.Clamp;
        importer.filterMode=FilterMode.Trilinear;
        importer.SaveAndReimport();
        var image=AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
        var shader=Shader.Find("Skybox/Panoramic");
        if(!image || !shader || !File.Exists("Assets/StreamingAssets/VeterinariaPantalla/observatorio-4k-v1.jpg"))
            throw new InvalidOperationException("Faltan las panorámicas del laboratorio");
        string previous=folder+"/ViaLactea.mat";
        if(AssetDatabase.LoadAssetAtPath<Material>(previous)){
            string error=AssetDatabase.MoveAsset(previous,AssetDatabase.GenerateUniqueAssetPath(Generated+"/ViaLactea.mat"));
            if(error.Length!=0)throw new IOException(error);
        }
        string pathMat=folder+"/Observatorio.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(pathMat);
        if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,pathMat);}
        material.SetTexture("_MainTex",image);material.SetFloat("_Exposure",1f);
        EditorUtility.SetDirty(material);
    }
    static string Media(VideoClip clip) {
        if(!clip)return "";
        string path="Media/"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(clip))+".mp4";
        if(!File.Exists(Path.Combine(Out,path)))throw new FileNotFoundException("Falta el vídeo web",path);
        return path;
    }
    static void AdaptVideos(IEnumerable<VideoInteractivoVeterinaria> activities,IEnumerable<VideoPlayer> players){
        foreach(var interactive in activities){
            if(interactive.videoPrincipal)interactive.urlPrincipal=Media(interactive.videoPrincipal);
            if(interactive.videoError)interactive.urlError=Media(interactive.videoError);
            interactive.videoPrincipal=null;interactive.videoError=null;
        }
        foreach(var video in players){
            if(!video.clip)continue;
            var url=Media(video.clip);video.clip=null;video.source=VideoSource.Url;video.url=url;
            var web=video.GetComponent<WebVideo>()??video.gameObject.AddComponent<WebVideo>();web.mediaPath=url;
        }
    }
    static void AdaptScene(Scene scene){
        AdaptVideos(All<VideoInteractivoVeterinaria>(scene),All<VideoPlayer>(scene));
        var videos=All<VideoPlayer>(scene).ToArray();
        foreach(var control in All<PausePlayVideo>(scene).Where(c=>!c.videoPlayer)){
            control.videoPlayer=control.GetComponentInParent<VideoPlayer>() ?? control.GetComponentInChildren<VideoPlayer>(true) ??
                videos.OrderBy(v=>(v.transform.position-control.transform.position).sqrMagnitude).FirstOrDefault();
        }
        foreach(var grab in All<Oculus.Interaction.GrabInteractable>(scene).Where(g=>!g.Rigidbody))
            grab.InjectRigidbody(GrabBody(grab));
        foreach(var hand in All<Oculus.Interaction.HandGrab.HandGrabInteractable>(scene).Where(g=>!g.Rigidbody))
            hand.InjectRigidbody(GrabBody(hand));
        foreach(var wrapper in All<Oculus.Interaction.InteractableUnityEventWrapper>(scene)){
            var data=new SerializedObject(wrapper);
            var target=data.FindProperty("_interactableView");
            if(target.objectReferenceValue is Oculus.Interaction.IInteractableView)continue;
            var view=wrapper.GetComponents<MonoBehaviour>().Concat(wrapper.GetComponentsInChildren<MonoBehaviour>(true))
                .Concat(wrapper.GetComponentsInParent<MonoBehaviour>(true)).FirstOrDefault(m=>m is Oculus.Interaction.IInteractableView);
            if(view){target.objectReferenceValue=view;data.ApplyModifiedPropertiesWithoutUndo();}
            else wrapper.enabled=false; // WebRig still invokes the stored UnityEvents directly for a ray selection.
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
        eye.nearClipPlane=.035f;eye.farClipPlane=200;
        var urp=go.AddComponent<UniversalAdditionalCameraData>();urp.allowXRRendering=false;urp.renderPostProcessing=false;urp.renderShadows=false;
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
    static Rigidbody GrabBody(Component grab){
        var body=grab.GetComponentInParent<Rigidbody>() ?? grab.GetComponentInChildren<Rigidbody>(true);
        if(body)return body;
        var collider=grab.GetComponentInParent<Collider>() ?? grab.GetComponentInChildren<Collider>(true);
        var owner=collider?collider.gameObject:grab.gameObject;
        body=owner.AddComponent<Rigidbody>();body.isKinematic=true;return body;
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
        foreach(var other in manager.activeLoaders.Where(l=>l!=loader).ToArray())
            if(!manager.TryRemoveLoader(other))throw new Exception("No se pudo retirar un cargador nativo de la configuración WebGL");
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

