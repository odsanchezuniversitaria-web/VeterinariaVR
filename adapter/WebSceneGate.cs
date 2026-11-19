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
        for (int i = 0; i <= dependencies.Length; i++) {
            string name = i == dependencies.Length ? sceneBundle : dependencies[i];
            if (resident.ContainsKey(name)) continue;
            using (var request = UnityWebRequestAssetBundle.GetAssetBundle(FileUrl("Content/" + name), Hash128.Parse(versions[i]))) {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success) {
                    status = "No se pudo descargar " + name + ". " + request.error; failed = true; yield break;
                }
                var bundle = DownloadHandlerAssetBundle.GetContent(request);
                if (!bundle) { status = "Archivo de actividad incompleto"; failed = true; yield break; }
                resident.Add(name, bundle);
            }
        }
        // Use the full path, distinct from the small scene with the same basename.
        yield return SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
    }
    void OnGUI() {
        GUI.Box(new Rect(15,15, Mathf.Min(520, Screen.width-30),40), status);
        if (failed && GUI.Button(new Rect(15,60,180,48),"Volver a intentar")) {
            failed = false; StartCoroutine(Start());
        }
    }
}

