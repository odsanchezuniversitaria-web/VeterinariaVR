param([string]$CopyRoot, [string]$SourceRoot)
$ErrorActionPreference='Stop'
$unity=Join-Path $CopyRoot 'Unity'
if(!(Test-Path "$unity/Assets/Scenes/0ScenaMenu.unity") -or !(Test-Path "$CopyRoot/tools/export_pieces.py")){throw 'Se requiere la copia VeterinariaVR'}
$adapter=Join-Path $unity 'Assets/VeterinariaWeb'
$template=Join-Path $unity 'Assets/WebGLTemplates/Veterinaria'
New-Item -ItemType Directory -Path $adapter,$template -Force | Out-Null
Copy-Item -Path "$SourceRoot/adapter/*" -Destination $adapter -Recurse -Force
Copy-Item -LiteralPath "$SourceRoot/template/index.html" -Destination $template -Force
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

