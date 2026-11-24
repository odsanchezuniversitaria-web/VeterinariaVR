using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// Each active system or surgical model requests only its own geometry.
public sealed class WebMobileGeometry : MonoBehaviour {
    sealed class Group {
        public readonly List<WebMobileMeshBinding> waiting = new List<WebMobileMeshBinding>();
        public readonly Dictionary<string,Mesh> meshes = new Dictionary<string,Mesh>();
        public AssetBundle bundle;
        public bool loading;
    }

    readonly Dictionary<string,Group> groups = new Dictionary<string,Group>();
    readonly List<UnityWebRequest> active = new List<UnityWebRequest>();
    string failedGroup;

    public void Request(WebMobileMeshBinding binding) {
        if (!groups.TryGetValue(binding.group, out var group)) {
            group = new Group();
            groups.Add(binding.group, group);
        }
        if (group.meshes.TryGetValue(binding.key,out var mesh)) { binding.Apply(mesh); return; }
        if (!group.waiting.Contains(binding)) group.waiting.Add(binding);
        if (!group.loading && !group.bundle && failedGroup != binding.group)
            StartCoroutine(Load(binding.group, group));
    }

    IEnumerator Load(string name, Group group) {
        group.loading=true;
        for(int attempt=0;attempt<2 && !group.bundle;attempt++) {
            using(var req=WebSceneGate.RequestBundle(name, WebSceneGate.MobileGeometryVersion(name))) {
                active.Add(req);
                yield return req.SendWebRequest();
                active.Remove(req);
                if(req.result==UnityWebRequest.Result.Success)group.bundle=DownloadHandlerAssetBundle.GetContent(req);
                if(!group.bundle)Debug.LogWarning("Geometría móvil "+name+" · "+req.error);
            }
        }
        if(group.bundle) {
            var pending=group.bundle.LoadAllAssetsAsync<Mesh>();
            yield return pending;
            foreach(var asset in pending.allAssets)if(asset is Mesh mesh)group.meshes.Add(mesh.name,mesh);
            foreach(var binding in group.waiting)if(binding)binding.Apply(group.meshes[binding.key]);
            group.waiting.Clear();
        } else failedGroup=name;
        group.loading=false;
    }
    void OnGUI() {
        if(failedGroup!=null && GUI.Button(new Rect(12,62,265,48),"Reintentar carga del modelo")) {
            var name=failedGroup;failedGroup=null;StartCoroutine(Load(name,groups[name]));
        }
    }
    void OnDestroy() {
        foreach(var req in active)req.Abort();
        foreach(var group in groups.Values)if(group.bundle)group.bundle.Unload(true);
    }
}
