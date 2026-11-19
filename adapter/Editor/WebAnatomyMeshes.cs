using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityMeshSimplifier;

public sealed class WebAnatomyMeshes : AssetPostprocessor
{
    const string Marker = "VeterinariaVR_StaticMesh_1";
    const long Threshold = 85000;
    static long TriangleCount(Mesh mesh) {
        long count=0;
        for(int s=0;s<mesh.subMeshCount;s++) {
            if(mesh.GetTopology(s)!=MeshTopology.Triangles)return 0;
            count+=mesh.GetIndexCount(s)/3;
        }
        return count;
    }

    public static void Prepare(string[] scenes) {
        foreach(string path in AssetDatabase.GetDependencies(scenes,true)){
            if(!path.StartsWith("Assets/",StringComparison.Ordinal) || !(AssetImporter.GetAtPath(path) is ModelImporter model))continue;
            if(model.userData.Contains(Marker))continue;
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(!prefab)continue;
            if(!prefab.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh && TriangleCount(f.sharedMesh)>Threshold))continue;
            model.userData=(model.userData+"\n"+Marker).Trim();
            model.meshCompression=ModelImporterMeshCompression.Medium;
            model.SaveAndReimport();
        }
    }

    void OnPostprocessModel(GameObject root) {
        if(!assetImporter.userData.Contains(Marker))return;
        var animated=new HashSet<Mesh>(root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r=>r.sharedMesh));
        foreach(var mesh in root.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).Where(m=>m).Distinct()){
            if(animated.Contains(mesh) || mesh.blendShapeCount!=0 || mesh.bindposes.Length!=0)continue;
            long count=TriangleCount(mesh);
            if(count<=Threshold)continue;
            var reducer=new MeshSimplifier();
            var options=reducer.SimplificationOptions;
            options.PreserveBorderEdges=true;
            options.PreserveUVSeamEdges=true;
            reducer.SimplificationOptions=options;
            reducer.Initialize(mesh);
            reducer.SimplifyMesh(55000f/count);
            var result=reducer.ToMesh();
            try {
                if(result.subMeshCount!=mesh.subMeshCount || result.vertexCount==0)
                    throw new InvalidOperationException("Cambió la estructura de la malla "+assetPath);
                for(int i=0;i<mesh.subMeshCount;i++)
                    if(mesh.GetIndexCount(i)>0 && result.GetIndexCount(i)==0)
                        throw new InvalidOperationException("Se perdió parte del modelo "+assetPath);
                result.name=mesh.name;
                EditorUtility.CopySerialized(result,mesh);
                Debug.Log($"[VeterinariaVR] {assetPath} / {mesh.name}: {count} → {TriangleCount(mesh)} triángulos");
            } finally {UnityEngine.Object.DestroyImmediate(result);}
        }
    }
}
