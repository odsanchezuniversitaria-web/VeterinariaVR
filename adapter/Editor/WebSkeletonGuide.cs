using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Derive the screen guide from the photograph used by the original scene.
public static class WebSkeletonGuide {
    public static void Build(string folder) {
        var frameShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/VeterinariaWeb/Entornos/MarcoDevolucion.shader");
        if(!frameShader)throw new InvalidOperationException("Falta el marco de la zona de devolución");
        string framePath=folder+"/MarcoDevolucion.mat";
        if(!AssetDatabase.LoadAssetAtPath<Material>(framePath))
            AssetDatabase.CreateAsset(new Material(frameShader),framePath);
        string source=AssetDatabase.GUIDToAssetPath("6d1d6e1808f05e04388586258079b754");
        var photo=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
        try {
            if(!photo.LoadImage(File.ReadAllBytes(source)))throw new IOException(source);
            int width=photo.width,height=photo.height;
            var pixels=photo.GetPixels32();
            var background=new bool[pixels.Length];
            var queue=new Queue<int>();
            int Value(int i) => Math.Max(pixels[i].r,Math.Max(pixels[i].g,pixels[i].b));
            void Enqueue(int i){
                if(background[i] || Value(i)>34)return;
                background[i]=true;
                queue.Enqueue(i);
            }
            // Seed flat black areas, including gaps inside the ribs. Isolated dark
            // photographic details stay opaque. Only grow into the JPEG fringe.
            for(int y=1;y<height-1;y++)for(int x=1;x<width-1;x++){
                int i=y*width+x;
                if(Value(i)<=4 && Value(i-1)<=4 && Value(i+1)<=4 && Value(i-width)<=4 && Value(i+width)<=4)Enqueue(i);
            }
            while(queue.Count!=0){
                int i=queue.Dequeue(),x=i%width;
                if(x!=0)Enqueue(i-1);
                if(x+1<width)Enqueue(i+1);
                if(i>=width)Enqueue(i-width);
                if(i+width<pixels.Length)Enqueue(i+width);
            }
            for(int i=0;i<pixels.Length;i++){
                if(!background[i])continue;
                float alpha=Mathf.SmoothStep(0,1,Mathf.InverseLerp(8,34,Value(i)));
                if(alpha<=0){pixels[i]=new Color32(0,0,0,0);continue;}
                Color color=pixels[i];
                pixels[i]=new Color(color.r/alpha,color.g/alpha,color.b/alpha,alpha);
            }
            var output=new Texture2D(width,height,TextureFormat.RGBA32,false,true);
            string path=folder+"/GuiaOseaTransparente.png";
            try{
                output.SetPixels32(pixels);output.Apply();
                File.WriteAllBytes(path,output.EncodeToPNG());
            }finally{UnityEngine.Object.DestroyImmediate(output);}
            AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency=true;importer.sRGBTexture=true;importer.mipmapEnabled=true;
            importer.npotScale=TextureImporterNPOTScale.None;
            importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=1024;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }finally{UnityEngine.Object.DestroyImmediate(photo);}
    }
}
