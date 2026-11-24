using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// Mobile monitors are loaded when their corresponding signal can actually be seen.
[DefaultExecutionOrder(-30000)]
public sealed class WebVideo : MonoBehaviour {
    public string mediaPath;
    public RawImage[] mobileDisplays;
    VideoPlayer player;
    Camera viewer;
    float nextCheck,hiddenSince;
    bool managed,visible,started,resume;
    double savedTime;
    readonly Vector3[] corners=new Vector3[4];
    void Awake() {
#if UNITY_WEBGL && !UNITY_EDITOR
        player=GetComponent<VideoPlayer>();
        managed=WebSceneGate.Phone && mobileDisplays!=null && mobileDisplays.Length>0;
        player.source=VideoSource.Url;
        if(managed){
            player.playOnAwake=false;
            player.enabled=false;
            player.url=WebSceneGate.FileUrl("Media/Movil/"+System.IO.Path.GetFileName(mediaPath));
            player.errorReceived+=OriginalOnError;
        }else player.url=WebSceneGate.FileUrl(mediaPath);
#endif
    }
    void OriginalOnError(VideoPlayer video,string reason){
        string original=WebSceneGate.FileUrl(mediaPath);
        if(video.url==original)return;
        video.url=original;
        if(visible)video.Play();
    }
    bool Appears(RawImage image){
        if(!image || !image.isActiveAndEnabled || !image.canvas || !image.canvas.isActiveAndEnabled || image.color.a<=0)return false;
        image.rectTransform.GetWorldCorners(corners);
        float x0=1,y0=1,x1=0,y1=0;bool inFront=false;
        for(int i=0;i<4;i++){
            var p=viewer.WorldToViewportPoint(corners[i]);
            if(p.z<=viewer.nearClipPlane)continue;
            inFront=true;x0=Mathf.Min(x0,p.x);x1=Mathf.Max(x1,p.x);y0=Mathf.Min(y0,p.y);y1=Mathf.Max(y1,p.y);
        }
        return inFront && x1>0 && x0<1 && y1>0 && y0<1;
    }
    void Update(){
        if(!managed || Time.unscaledTime<nextCheck)return;
        nextCheck=Time.unscaledTime+.23f;
        if(!viewer)viewer=Camera.main;
        if(!viewer)return;
        bool needed=System.Array.Exists(mobileDisplays,Appears);
        if(needed){
            hiddenSince=0;if(visible)return;visible=true;
            player.enabled=true;
            if(!started || resume){
                player.Play();
                if(started && savedTime>0 && player.canSetTime)player.time=savedTime;
            }
            started=true;
        }else if(visible){
            if(hiddenSince==0)hiddenSince=Time.unscaledTime;
            if(Time.unscaledTime-hiddenSince<.8f)return;
            resume=player.isPlaying || !player.isPrepared;savedTime=player.time;
            player.Pause();player.enabled=false;visible=false;
        }
    }
    void OnDestroy(){if(managed && player)player.errorReceived-=OriginalOnError;}
}
