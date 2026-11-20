param([string]$CopyRoot, [string]$SourceRoot)
$ErrorActionPreference='Stop'
$unity=Join-Path $CopyRoot 'Unity'
if(!(Test-Path "$unity/Assets/Scenes/0ScenaMenu.unity") -or !(Test-Path "$CopyRoot/tools/export_pieces.py")){throw 'Se requiere la copia VeterinariaVR'}
$adapter=Join-Path $unity 'Assets/VeterinariaWeb'
$template=Join-Path $unity 'Assets/WebGLTemplates/Veterinaria'
New-Item -ItemType Directory -Path $adapter,$template -Force | Out-Null
Copy-Item -Path "$SourceRoot/adapter/*" -Destination $adapter -Recurse -Force
Copy-Item -LiteralPath "$SourceRoot/template/index.html" -Destination $template -Force
$mrName='com.meta.xr.mrutilitykit'
$mr=Join-Path "$unity/Packages" $mrName
if(!(Test-Path -LiteralPath $mr)){
    $cached=Join-Path "$unity/Library/PackageCache" $mrName
    if(!(Test-Path -LiteralPath $cached)){$cached=(Get-ChildItem "$unity/Library/PackageCache" -Directory -Filter "$mrName@*"|Select-Object -First 1).FullName}
    if((Get-Content -Raw -LiteralPath "$cached/package.json"|ConvertFrom-Json).version -ne '201.0.0'){throw 'Se requiere MR Utility Kit 201.0.0'}
    Copy-Item -LiteralPath $cached -Destination $mr -Recurse -Force
}
foreach($component in @(@('MRUK.Shared.cs','BeforeSplashScreen'),@('MRUKGlobalContext.cs','BeforeSceneLoad'))){
    $file=Join-Path "$mr/Core/Scripts" $component[0]
    $attr='[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.'+$component[1]+')]'
    $old=[IO.File]::ReadAllText($file)
    $replacement="#if !UNITY_WEBGL || UNITY_EDITOR`n        "+$attr+"`n#endif"
    if(!$old.Contains($replacement)){
        if(!$old.Contains($attr)){throw "Inicializador de MRUK inesperado: $file"}
        (Get-Item -LiteralPath $file).IsReadOnly=$false
        [IO.File]::WriteAllText($file,$old.Replace($attr,$replacement),[Text.UTF8Encoding]::new($false))
    }
}
$instrumentDir=Join-Path $unity 'Assets/Huesos 3D/Quirofano/Materials'
$convertedDir=Join-Path $CopyRoot 'ModelosWeb/Instrumentos'
if(Test-Path -LiteralPath $convertedDir){
    $archive=Join-Path $CopyRoot 'FuentesMax'
    New-Item -ItemType Directory -Path $archive -Force |Out-Null
    foreach($fbx in Get-ChildItem -LiteralPath $convertedDir -File -Filter '*.fbx'){
        $source=Join-Path $instrumentDir ($fbx.BaseName+'.max')
        $destination=Join-Path $instrumentDir $fbx.Name
        if(Test-Path -LiteralPath "$destination.meta"){continue}
        if(!(Test-Path -LiteralPath "$source.meta")){throw "Falta el meta original de $source"}
        $saved=Join-Path $archive ($fbx.BaseName+'.max')
        if(Test-Path -LiteralPath $source){
            if(Test-Path -LiteralPath $saved){throw "Ya existe la fuente archivada $saved"}
            Move-Item -LiteralPath $source -Destination $saved
        }
        if(!(Test-Path -LiteralPath $saved)){throw "Falta la fuente del instrumento $saved"}
        Copy-Item -LiteralPath $fbx.FullName -Destination $destination -Force
        Move-Item -LiteralPath "$source.meta" -Destination "$destination.meta"
    }
}
$wit=Join-Path "$unity/Packages/com.meta.xr.sdk.voice" 'Lib/Wit.ai/Lib/third-party/NativeWebSocket/WebSocket'
foreach($suffix in @('.jslib','.jslib.meta')){
    $src=Join-Path $wit ("WebSocket"+$suffix)
    $dst=Join-Path $wit ("WitWebSocket"+$suffix)
    if((Test-Path -LiteralPath $src) -and !(Test-Path -LiteralPath $dst)){Move-Item -LiteralPath $src -Destination $dst}
}
$simp=Join-Path "$CopyRoot/tools" 'UnityMeshSimplifier'
if(!(Test-Path -LiteralPath $simp)){
    git clone --depth 1 --branch v3.1.1 https://github.com/Whinarn/UnityMeshSimplifier.git $simp
    if($LASTEXITCODE -ne 0){throw 'No se pudo descargar UnityMeshSimplifier'}
}
if((git -C $simp rev-parse HEAD) -ne '53fdb3122645bcd3ad2c258235200dff3dfbaa9b'){throw 'Versión inesperada de UnityMeshSimplifier'}
$pkg=Join-Path "$unity/Packages" 'com.whinarn.unitymeshsimplifier'
if(!(Test-Path -LiteralPath $pkg)){
    New-Item -ItemType Directory -Path $pkg | Out-Null
    foreach($part in @('Runtime','Runtime.meta','LICENSE.md','Third Party Notices.md','package.json')){
        Copy-Item -LiteralPath "$simp/$part" -Destination $pkg -Recurse
    }
}
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
foreach($rel in @('CuerdaFisica.cs','DetectarAgarre.cs','PuntoSuturaSimple.cs','PuntoSuturaPiel.cs','ControladorJuego.cs','MaquinaGasa.cs','MaquinaRasurar.cs','HuesoInteractivo.cs','PerillaMicroscopioMeta.cs','SecuenciaNudos.cs','SistemaSuturaPiel.cs','SutureTrigger.cs','Scripts/Cirugia/RetroalimentacionCirugia.cs','Huesos 3D/Quirofano/PerroCirugia/TesticleExtractionSystem.cs','Scripts/Boxeo/PausaBoxeo.cs','Scripts/Laminitis/LetreroToggle.cs','Scripts/Laminitis/SecuenciaLaminitisManual.cs')){
    $file=Join-Path "$unity/Assets" $rel
    $old=[IO.File]::ReadAllText($file)
    $new=$old -replace 'OVRInput\.(GetDown|Get|GetLocalControllerPosition|SetControllerVibration)\(', 'WebLegacyInput.$1('
    if($new -cne $old){[IO.File]::WriteAllText($file,$new,[Text.UTF8Encoding]::new($false))}
}
$net=Join-Path $unity 'Assets/Scripts/Multijugador/ConectorMultijugador.cs'
$walker=Join-Path $unity 'Packages/com.meta.xr.sdk.voice/Lib/Wit.ai/Scripts/Editor/Conduit/AssemblyWalker.cs'
$telemetry=Join-Path $core 'Editor/Utils/SampleMetadata/UpdateManifestWithCodeSample.cs'
foreach($fix in @(
    @($walker,'_assemblies.Add(conduitAssembly.FullName.Split('','').First(), conduitAssembly);','_assemblies.TryAdd(conduitAssembly.FullName.Split('','').First(), conduitAssembly);'),
    @($telemetry,'foreach (var type in assembly.GetTypes())',"System.Type[] available; try { available = assembly.GetTypes(); } catch (System.Reflection.ReflectionTypeLoadException e) { available = e.Types; }`n                foreach (var type in available)"),
    @($telemetry,'var codeSampleAttributes = type.GetCustomAttributes','if (type == null) continue; var codeSampleAttributes = type.GetCustomAttributes')
)){
    $file=$fix[0];$old=[IO.File]::ReadAllText($file)
    if($old.Contains($fix[2])){continue}
    if(!$old.Contains($fix[1])){throw "Firma de herramienta de compilación inesperada: $file"}
    (Get-Item -LiteralPath $file).IsReadOnly=$false
    [IO.File]::WriteAllText($file,$old.Replace($fix[1],$fix[2]),[Text.UTF8Encoding]::new($false))
}
$old=[IO.File]::ReadAllText($net)
$new=$old.Replace('soporte.AddComponent<NetworkSceneManagerDefault>()','soporte.AddComponent<WebNetworkScenes>()').Replace('SceneRef.FromIndex(escenaActual.buildIndex)','gestorEscena.GetSceneRef(escenaActual.path)')
if($new -cne $old){[IO.File]::WriteAllText($net,$new,[Text.UTF8Encoding]::new($false))}
foreach($change in @(
    @('SlingshotTrajectoryPredictor.cs','new Material(Shader.Find("Standard"))','new Material(Shader.Find("Universal Render Pipeline/Simple Lit"))'),
    @('Scripts/pausaVideo.cs','if (isPaused)','if (!videoPlayer) return; if (isPaused)'),
    @('MostrarPanelConBotonX.cs','if (dispositivoIzquierdo.TryGetFeatureValue(CommonUsages.primaryButton, out bool xPresionado))','bool xPresionado = WebLegacyInput.GetDown(OVRInput.Button.Three); if (WebLegacyInput.rig || dispositivoIzquierdo.TryGetFeatureValue(CommonUsages.primaryButton, out xPresionado))'),
    @('OcultarPanelLetra.cs','bool botonActualmentePresionado = EstaBotonPresionado(dispositivo);','bool botonActualmentePresionado = WebLegacyInput.rig ? WebLegacyInput.ReadPanelButton(manoAUsar, botonAUsar) : EstaBotonPresionado(dispositivo);'),
    @('ApareceObjPanel.cs','bool pulsado = false;','bool pulsado = WebLegacyInput.GetDown(OVRInput.Button.Four) || WebLegacyInput.GetDown(OVRInput.Button.Two);'),
    @('Scripts/Multijugador/InterruptorMultijugador.cs','if (Input.GetKey(TECLA_PRUEBA)) ahora = true;','if (Input.GetKey(TECLA_PRUEBA) || WebLegacyInput.GetDown(OVRInput.Button.Start)) ahora = true;'),
    @('Scripts/Multijugador/PoseManoLocal.cs','if (_rig == null) _rig = Object.FindAnyObjectByType<OVRCameraRig>();','if (WebLegacyInput.rig) { var hand = _izquierda ? WebLegacyInput.rig.originalLeft : WebLegacyInput.rig.originalRight; if (hand) { pose = new Pose(hand.position, hand.rotation); return true; } } if (_rig == null) _rig = Object.FindAnyObjectByType<OVRCameraRig>();')
)){
    $file=Join-Path "$unity/Assets" $change[0]
    $old=[IO.File]::ReadAllText($file)
    if($old.Contains($change[2])){continue}
    if(!$old.Contains($change[1])){throw "Entrada de mando inesperada en $file"}
    [IO.File]::WriteAllText($file,$old.Replace($change[1],$change[2]),[Text.UTF8Encoding]::new($false))
}

