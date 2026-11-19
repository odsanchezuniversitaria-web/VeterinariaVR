using System.IO;
using Fusion;
using UnityEngine.SceneManagement;

public sealed class WebNetworkScenes : NetworkSceneManagerDefault {
    public override SceneRef GetSceneRef(string sceneNameOrPath) {
        if(sceneNameOrPath.StartsWith("Assets/WebPortGenerated/Scenes/",System.StringComparison.OrdinalIgnoreCase)){
            var name=Path.GetFileNameWithoutExtension(sceneNameOrPath);
            for(int i=0;i<SceneManager.sceneCountInBuildSettings;i++)
                if(string.Equals(Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i)),name,System.StringComparison.OrdinalIgnoreCase))
                    return SceneRef.FromIndex(i);
        }
        return base.GetSceneRef(sceneNameOrPath);
    }
}

