using System.IO;
using Fusion;
using UnityEngine.SceneManagement;

public sealed class WebNetworkScenes : NetworkSceneManagerDefault {
    const string RegionDePracticas = "sa";

    public static Fusion.Photon.Realtime.FusionAppSettings CrearAjustesDeSala() {
        var ajustes = Fusion.Photon.Realtime.PhotonAppSettings.Global.AppSettings.GetCopy();
        // El nombre de una práctica define una sala dentro de una región de Photon.
        ajustes.FixedRegion = RegionDePracticas;
        return ajustes;
    }
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

