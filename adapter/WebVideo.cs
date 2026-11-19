using UnityEngine;
using UnityEngine.Video;

[DefaultExecutionOrder(-30000)]
public sealed class WebVideo : MonoBehaviour {
    public string mediaPath;
    void Awake() {
#if UNITY_WEBGL && !UNITY_EDITOR
        var player=GetComponent<VideoPlayer>();
        player.source=VideoSource.Url;player.url=WebSceneGate.FileUrl(mediaPath);
#endif
    }
}

