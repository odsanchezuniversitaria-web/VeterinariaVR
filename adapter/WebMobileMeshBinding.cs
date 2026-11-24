using UnityEngine;

public sealed class WebMobileMeshBinding : MonoBehaviour {
    public WebMobileGeometry loader;
    public Component target;
    public bool enableCollider;
    public string group,key;
    void OnEnable(){if(loader)loader.Request(this);}
    public void Apply(Mesh mesh){
        if(target is MeshFilter filter)filter.sharedMesh=mesh;
        else if(target is MeshCollider collider){collider.sharedMesh=mesh;if(enableCollider)collider.enabled=true;}
        else if(target is SkinnedMeshRenderer skin)skin.sharedMesh=mesh;
    }
}
