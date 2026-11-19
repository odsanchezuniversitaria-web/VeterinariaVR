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
    string status;
    bool failed;
    public static string FileUrl(string relative) => new Uri(new Uri(Application.absoluteURL), relative).AbsoluteUri;

    IEnumerator Start() {
        status = "Abriendo " + System.IO.Path.GetFileNameWithoutExtension(scenePath);
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
                    var req=UnityWebRequestAssetBundle.GetAssetBundle(FileUrl("Content/"+names[i]),Hash128.Parse(versions[i]));
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

