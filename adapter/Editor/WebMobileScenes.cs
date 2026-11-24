using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityMeshSimplifier;
using OseoVR;

public sealed class WebMobileScenes {
    const string Folder="Assets/WebPortGenerated/MobileGeometry";
    const string SceneFolder="Assets/WebPortGenerated/Scenes/Movil";
    readonly Dictionary<string,List<string>> files=new Dictionary<string,List<string>>();
    readonly Dictionary<string,string> owners=new Dictionary<string,string>();
    readonly Dictionary<Material,Material> materials=new Dictionary<Material,Material>();
    public readonly Dictionary<string,AssetBundleBuild> scenes=new Dictionary<string,AssetBundleBuild>();

    public IEnumerable<AssetBundleBuild> Geometry => files.Select(p=>new AssetBundleBuild{
        assetBundleName=p.Key,assetNames=p.Value.ToArray()
    });
    public void Prepare(AssetBundleBuild level){
        string source=level.assetNames[0];
        bool anatomy=source.EndsWith("/13ScenaAnatomia.unity");
        if(!anatomy && !source.EndsWith("/8ScenaCirugia.unity"))return;
        Directory.CreateDirectory(SceneFolder);
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        string path=SceneFolder+"/"+Path.GetFileName(source);
        // Copy to the variant FIRST. Never edit the generated full scene or the author's scene.
        var original=EditorSceneManager.OpenScene(source);
        if(!EditorSceneManager.SaveScene(original,path,true))throw new IOException(path);
        var scene=EditorSceneManager.OpenScene(path);
        // The baked lighting asset points back to its authored scene. Retarget a copy so Unity
        // can keep the same lightmaps and probes without following that scene to all original models.
        var lighting=Lightmapping.lightingDataAsset;
        if(lighting){
            string dest=Folder+"/"+Path.GetFileNameWithoutExtension(path)+"-Iluminacion.asset";
            var copy=AssetDatabase.LoadAssetAtPath<LightingDataAsset>(dest);
            if(!copy){
                if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(lighting),dest))throw new IOException(dest);
                copy=AssetDatabase.LoadAssetAtPath<LightingDataAsset>(dest);
            }else EditorUtility.CopySerialized(lighting,copy);
            var data=new SerializedObject(copy);
            var owner=data.FindProperty("m_Scene");
            if(owner==null)throw new InvalidDataException("Falta la referencia de escena en "+dest);
            owner.objectReferenceValue=AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(copy);
            Lightmapping.lightingDataAsset=copy;
        }
        var roots=scene.GetRootGameObjects();
        var instances=roots.SelectMany(root=>root.GetComponentsInChildren<Transform>(true))
            .Select(t=>t.gameObject).Where(PrefabUtility.IsOutermostPrefabInstanceRoot).ToArray();
        foreach(var instance in instances)
            if(PrefabUtility.IsOutermostPrefabInstanceRoot(instance))
                PrefabUtility.UnpackPrefabInstance(instance,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        var rig=roots.SelectMany(o=>o.GetComponentsInChildren<WebRig>(true)).Single();
        var host=rig.gameObject.AddComponent<WebMobileGeometry>();
        var panel=roots.SelectMany(o=>o.GetComponentsInChildren<PanelSistemaCaninoController>(true)).FirstOrDefault();
        if(anatomy){
            if(!panel || panel.sistemas.Count==0)throw new InvalidDataException("Panel anatómico ausente");
            var launch=roots.SelectMany(o=>o.GetComponentsInChildren<ArranqueSistemasConBoton>(true)).Single();
            var first=panel.sistemas.First(s=>s.objeto && s.nombre=="Oseo");
            launch.soloEstos=new List<GameObject>{first.objeto};
            panel.permitirSoloUnoActivo=true;
            rig.mobileSystems=panel;rig.mobileStart=launch;
        }
        foreach(var root in roots)foreach(var component in root.GetComponentsInChildren<Component>(true)){
            Mesh mesh=null;
            if(component is MeshFilter filter)mesh=filter.sharedMesh;
            else if(component is MeshCollider collider)mesh=collider.sharedMesh;
            else if(component is SkinnedMeshRenderer skin)mesh=skin.sharedMesh;
            if(!mesh || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh,out string guid,out long local))continue;
            string sourcePath=AssetDatabase.GetAssetPath(mesh);
            string group;
            if(anatomy){
                int system=panel.sistemas.FindIndex(s=>s.objeto && component.transform.IsChildOf(s.objeto.transform));
                if(system<0)continue;
                group="geo-anatomia-"+system;
            }else{
                if(!sourcePath.Contains("/PerroCirugia/") || !Path.GetFileNameWithoutExtension(sourcePath).ToLowerInvariant().Contains("perrocirugia"))continue;
                group="geo-cirugia-"+guid.Substring(0,10);
            }
            bool exact=component is MeshCollider trigger && trigger.isTrigger;
            string key=guid+"-"+local+(exact?"-contacto":"");
            if(!owners.TryGetValue(key,out var owner)){
                owner=group;owners.Add(key,owner);
                if(!files.ContainsKey(owner))files.Add(owner,new List<string>());
                string asset=Folder+"/"+key+".asset";
                Mesh result=MobileMesh(mesh,exact);
                result.name=key;
                var previous=AssetDatabase.LoadAssetAtPath<Mesh>(asset);
                if(previous){EditorUtility.CopySerialized(result,previous);UnityEngine.Object.DestroyImmediate(result);}
                else AssetDatabase.CreateAsset(result,asset);
                files[owner].Add(asset);
            }
            var binding=component.gameObject.AddComponent<WebMobileMeshBinding>();
            binding.loader=host;binding.target=component;binding.key=key;binding.group=owner;
            if(component is MeshFilter f)f.sharedMesh=null;
            else if(component is MeshCollider c){
                // Unity can restore the imported mesh when a MeshCollider is saved with a null mesh.
                binding.enableCollider=c.enabled;c.enabled=false;c.sharedMesh=PendingCollider();
            }
            else ((SkinnedMeshRenderer)component).sharedMesh=null;
            var renderer=component is MeshFilter ? component.GetComponent<MeshRenderer>() : component as Renderer;
            if(renderer)DetachEmbeddedMaterials(renderer);
        }
        foreach(var root in roots)foreach(var animator in root.GetComponentsInChildren<Animator>(true)){
            var avatar=animator.avatar;
            if(!avatar || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(avatar,out string model,out long id) ||
               !owners.Keys.Any(k=>k.StartsWith(model+"-",StringComparison.Ordinal)))continue;
            string dest=Folder+"/"+model+"-avatar-"+id+".asset";
            var detached=AssetDatabase.LoadAssetAtPath<Avatar>(dest);
            if(!detached){detached=UnityEngine.Object.Instantiate(avatar);AssetDatabase.CreateAsset(detached,dest);}
            animator.avatar=detached;
        }
        if(!anatomy)DetachPatientAnimations(roots);
        if(!EditorSceneManager.SaveScene(scene))throw new IOException(path);
        Debug.Log("Variante móvil "+path+" · "+roots.Sum(o=>o.GetComponentsInChildren<WebMobileMeshBinding>(true).Length)+" enlaces");
        scenes.Add(level.assetBundleName,new AssetBundleBuild{assetBundleName="movil-"+level.assetBundleName,assetNames=new[]{path}});
    }

    public static void DetachPatientAnimations(GameObject[] roots){
        var copies=new Dictionary<RuntimeAnimatorController,RuntimeAnimatorController>();
        foreach(var root in roots)foreach(var animator in root.GetComponentsInChildren<Animator>(true)){
            var source=animator.runtimeAnimatorController;
            if(!source || !AssetDatabase.GetAssetPath(source).Contains("/PerroCirugia/"))continue;
            var embedded=source.animationClips.Where(clip=>AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip)) is ModelImporter).Distinct().ToArray();
            if(embedded.Length==0)continue;
            if(!copies.TryGetValue(source,out var controller)){
                var sourcePath=AssetDatabase.GetAssetPath(source);
                string dest=Folder+"/"+AssetDatabase.AssetPathToGUID(sourcePath)+".controller";
                if(File.Exists(dest)){
                    File.Copy(sourcePath,dest,true);
                    AssetDatabase.ImportAsset(dest,ImportAssetOptions.ForceUpdate);
                }else if(!AssetDatabase.CopyAsset(sourcePath,dest))throw new IOException(dest);
                controller=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(dest);

                var motions=new Dictionary<AnimationClip,AnimationClip>();
                foreach(var clip in embedded){
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(clip,out string guid,out long id);
                    string clipPath=Folder+"/"+guid+"-"+id+".anim";
                    var motion=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                    if(!motion){motion=UnityEngine.Object.Instantiate(clip);motion.name=clip.name;AssetDatabase.CreateAsset(motion,clipPath);}
                    else EditorUtility.CopySerialized(clip,motion);
                    motions[clip]=motion;
                }
                foreach(var part in AssetDatabase.LoadAllAssetsAtPath(dest)){
                    var serialized=new SerializedObject(part);var field=serialized.GetIterator();
                    while(field.Next(true)){
                        if(field.propertyType==SerializedPropertyType.ObjectReference && field.objectReferenceValue is AnimationClip old && motions.TryGetValue(old,out var replacement))field.objectReferenceValue=replacement;
                    }
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(dest,ImportAssetOptions.ForceUpdate);
                controller=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(dest);
                copies.Add(source,controller);
                var bound=controller.animationClips;
                foreach(var item in motions){
                    if(!bound.Contains(item.Value) || bound.Contains(item.Key) ||
                       item.Value.length!=item.Key.length || item.Value.events.Length!=item.Key.events.Length)
                        throw new InvalidDataException("Animación del paciente incompleta: "+item.Key.name);
                }
            }
            animator.runtimeAnimatorController=controller;
        }
    }
    static Mesh PendingCollider(){
        const string path=Folder+"/ColliderPendiente.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh)return mesh;
        mesh=new Mesh{name="ColliderPendiente"};
        mesh.vertices=new[]{Vector3.zero,Vector3.right*.01f,Vector3.up*.01f,Vector3.forward*.01f};
        mesh.triangles=new[]{0,2,1,0,1,3,0,3,2,1,2,3};
        mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static Mesh MobileMesh(Mesh mesh,bool exact){
        if(exact || mesh.vertexCount<1700 || mesh.blendShapeCount>0)return UnityEngine.Object.Instantiate(mesh);
        for(int i=0;i<mesh.subMeshCount;i++)if(mesh.GetTopology(i)!=MeshTopology.Triangles)return UnityEngine.Object.Instantiate(mesh);
        var reducer=new MeshSimplifier();
        var options=reducer.SimplificationOptions;
        options.PreserveBorderEdges=true;options.PreserveUVSeamEdges=true;
        reducer.SimplificationOptions=options;reducer.Initialize(mesh);reducer.SimplifyMesh(.40f);
        Mesh result=reducer.ToMesh();
        bool valid=result.vertexCount>0 && result.subMeshCount==mesh.subMeshCount && result.bindposes.Length==mesh.bindposes.Length;
        for(int i=0;valid && i<mesh.subMeshCount;i++)valid=mesh.GetIndexCount(i)==0 || result.GetIndexCount(i)>0;
        if(valid)return result;
        UnityEngine.Object.DestroyImmediate(result);return UnityEngine.Object.Instantiate(mesh);
    }
    void DetachEmbeddedMaterials(Renderer renderer){
        var list=renderer.sharedMaterials;bool changed=false;
        for(int i=0;i<list.Length;i++){
            var mat=list[i];if(!mat || !(AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(mat)) is ModelImporter))continue;
            if(!materials.TryGetValue(mat,out var copy)){
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mat,out string guid,out long id);
                string path=Folder+"/"+guid+"-"+id+".mat";
                copy=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!copy){copy=new Material(mat);AssetDatabase.CreateAsset(copy,path);}
                else EditorUtility.CopySerialized(mat,copy);
                materials.Add(mat,copy);
            }
            list[i]=copy;changed=true;
        }
        if(changed)renderer.sharedMaterials=list;
    }
}
