param([string]$CopyRoot, [string]$SourceRoot)
$ErrorActionPreference='Stop'
$unity=Join-Path $CopyRoot 'Unity'
if(!(Test-Path "$unity/Assets/Scenes/0ScenaMenu.unity") -or !(Test-Path "$CopyRoot/tools/export_pieces.py")){throw 'Se requiere la copia VeterinariaVR'}
$adapter=Join-Path $unity 'Assets/VeterinariaWeb'
$template=Join-Path $unity 'Assets/WebGLTemplates/Veterinaria'
New-Item -ItemType Directory -Path $adapter,$template -Force | Out-Null
Copy-Item -Path "$SourceRoot/adapter/*" -Destination $adapter -Recurse -Force
Copy-Item -LiteralPath "$SourceRoot/template/index.html" -Destination $template -Force
$coreName='com.meta.xr.sdk.core'
$core=Join-Path "$unity/Packages" $coreName
if(!(Test-Path -LiteralPath $core)){
    $cached=Join-Path "$unity/Library/PackageCache" $coreName
    if(!(Test-Path -LiteralPath $cached)){
        $cached=(Get-ChildItem "$unity/Library/PackageCache" -Directory -Filter "$coreName@*" | Select-Object -First 1).FullName
    }
    if((Get-Content -Raw -LiteralPath "$cached/package.json" | ConvertFrom-Json).version -ne '201.0.0'){throw 'Se requiere Meta Core 201.0.0'}
    Copy-Item -LiteralPath $cached -Destination $core -Recurse -Force
}
# Neither optional capture component is referenced in the veterinary scenes or prefabs.
# Leave the SDK's native implementation intact for every other build target.
foreach($name in @('SpeechToTextAgent','LipSyncMicInput')){
    $file=(Get-ChildItem -LiteralPath $core -Filter "$name.cs" -File -Recurse).FullName
    $old=[IO.File]::ReadAllText($file)
    if($old.Contains('#if UNITY_WEBGL && !UNITY_EDITOR')){continue}
    if($name -eq 'LipSyncMicInput'){
        $new="#if UNITY_WEBGL && !UNITY_EDITOR`n#define OVR_DISABLE_MICROPHONE`n#endif`n"+$old
    }else{
        $pattern='(?s)(private IEnumerator CoListenOnce\(\)\s*\{)(.*?)(\r?\n        \}\s+private float EstimateNoiseDb)'
        if(![regex]::IsMatch($old,$pattern)){throw 'Captura de voz inesperada'}
        $new=[regex]::Replace($old,$pattern,('$1'+"`n#if UNITY_WEBGL && !UNITY_EDITOR`n            Debug.LogWarning(`"La captura nativa de este componente de Meta no está disponible en WebGL.`");`n            yield break;`n#else"+'$2'+"`n#endif"+'$3'))
    }
    (Get-Item -LiteralPath $file).IsReadOnly=$false
    [IO.File]::WriteAllText($file,$new,[Text.UTF8Encoding]::new($false))
}
$file=Join-Path $unity 'Assets/VideoInteractivoVeterinaria.cs'
$text=[IO.File]::ReadAllText($file)
if(!$text.Contains('public string urlPrincipal;')){
    foreach($pair in @(@('public VideoClip videoPrincipal;', "public VideoClip videoPrincipal;\n    public string urlPrincipal;"),@('public VideoClip videoError;', "public VideoClip videoError;\n    public string urlError;"),@('videoPlayer.clip = videoPrincipal;', 'if (!string.IsNullOrEmpty(urlPrincipal)) videoPlayer.url = WebSceneGate.FileUrl(urlPrincipal); else videoPlayer.clip = videoPrincipal;'),@('videoPlayer.clip = videoError;', 'if (!string.IsNullOrEmpty(urlError)) videoPlayer.url = WebSceneGate.FileUrl(urlError); else videoPlayer.clip = videoError;'))){
        if(!$text.Contains($pair[0])){throw "Patrón de vídeo ausente: $($pair[0])"}
        $text=$text.Replace($pair[0],$pair[1].Replace('\n',[Environment]::NewLine))
    }
    [IO.File]::WriteAllText($file,$text,[Text.UTF8Encoding]::new($false))
}
foreach($rel in @('CuerdaFisica.cs','DetectarAgarre.cs','PuntoSuturaSimple.cs','PuntoSuturaPiel.cs','Huesos 3D/Quirofano/PerroCirugia/TesticleExtractionSystem.cs','Scripts/Boxeo/PausaBoxeo.cs','Scripts/Laminitis/LetreroToggle.cs','Scripts/Laminitis/SecuenciaLaminitisManual.cs')){
    $file=Join-Path "$unity/Assets" $rel
    $old=[IO.File]::ReadAllText($file)
    $new=$old -replace 'OVRInput\.(GetDown|Get|GetLocalControllerPosition|SetControllerVibration)\(', 'WebLegacyInput.$1('
    if($new -cne $old){[IO.File]::WriteAllText($file,$new,[Text.UTF8Encoding]::new($false))}
}
$net=Join-Path $unity 'Assets/Scripts/Multijugador/ConectorMultijugador.cs'
$old=[IO.File]::ReadAllText($net)
$new=$old.Replace('soporte.AddComponent<NetworkSceneManagerDefault>()','soporte.AddComponent<WebNetworkScenes>()').Replace('SceneRef.FromIndex(escenaActual.buildIndex)','gestorEscena.GetSceneRef(escenaActual.path)')
if($new -cne $old){[IO.File]::WriteAllText($net,$new,[Text.UTF8Encoding]::new($false))}

