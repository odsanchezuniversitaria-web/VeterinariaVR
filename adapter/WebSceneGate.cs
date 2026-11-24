using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

// Small built-in scenes keep existing scene names, indices and LoadScene calls valid.
public sealed class WebSceneGate : MonoBehaviour
{
    public string sceneBundle, scenePath;
    public string[] dependencies, versions;
    static readonly Dictionary<string, AssetBundle> resident = new Dictionary<string, AssetBundle>();
    public string phoneBundle;
    public string[] phoneDependencies,phoneVersions,geometryNames,geometryVersions;
    static readonly Dictionary<string,string> geometryHashes=new Dictionary<string,string>();
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern int VeterinariaPhone();
    public static bool Phone {
        get {
#if UNITY_WEBGL && !UNITY_EDITOR
            return VeterinariaPhone()!=0;
#else
            return false;
#endif
        }
    }
    public static string MobileGeometryVersion(string group)=>geometryHashes[group];
    public static UnityWebRequest RequestBundle(string name,string version)=>
        UnityWebRequestAssetBundle.GetAssetBundle(FileUrl("Content/"+name+"?v="+version),Hash128.Parse(version));
    string status;
    bool failed;
    public static string FileUrl(string relative) => new Uri(new Uri(Application.absoluteURL), relative).AbsoluteUri;

    IEnumerator Start() {
        status = "Abriendo " + System.IO.Path.GetFileNameWithoutExtension(scenePath);
        if(Phone && !string.IsNullOrEmpty(phoneBundle)){
            sceneBundle=phoneBundle;dependencies=phoneDependencies;versions=phoneVersions;
            geometryHashes.Clear();
            for(int i=0;i<geometryNames.Length;i++)geometryHashes.Add(geometryNames[i],geometryVersions[i]);
        }
        var wanted = new HashSet<string>(dependencies) { sceneBundle };
        foreach (string old in new List<string>(resident.Keys)) {
            if (wanted.Contains(old)) continue;
            resident[old].Unload(true);
            resident.Remove(old);
        }
        var names=new List<string>(dependencies){sceneBundle};
        var pending=new Dictionary<string,UnityWebRequest>();
        int next=0;
        try {
            while(next<names.Count || pending.Count>0){
                while(next<names.Count && pending.Count<4){
                    int i=next++;
                    if(resident.ContainsKey(names[i]))continue;
                    var req=RequestBundle(names[i],versions[i]);
                    pending.Add(names[i],req);req.SendWebRequest();
                }
                yield return null;
                foreach(string name in new List<string>(pending.Keys)){
                    var req=pending[name];
                    if(!req.isDone)continue;
                    var asset=req.result==UnityWebRequest.Result.Success?DownloadHandlerAssetBundle.GetContent(req):null;
                    if(!asset){status="No se pudo abrir "+name+". "+req.error;failed=true;yield break;}
                    resident.Add(name,asset);
                    req.Dispose();pending.Remove(name);
                }
                status="Cargando "+System.IO.Path.GetFileNameWithoutExtension(scenePath)+" · "+(next-pending.Count)+"/"+names.Count;
            }
        } finally {
            foreach(var req in pending.Values)req.Dispose();
        }
        var paths=resident[sceneBundle].GetAllScenePaths();
        if(paths.Length!=1){status="Paquete de escena incompleto";failed=true;yield break;}
        yield return SceneManager.LoadSceneAsync(paths[0], LoadSceneMode.Single);
    }
    void OnGUI() {
        GUI.Box(new Rect(15,15, Mathf.Min(520, Screen.width-30),40), status);
        if (failed && GUI.Button(new Rect(15,60,180,48),"Volver a intentar")) {
            failed = false; StartCoroutine(Start());
        }
    }
}
