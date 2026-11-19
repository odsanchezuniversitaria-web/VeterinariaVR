param([Parameter(Mandatory=$true)][string]$CopyRoot,[Parameter(Mandatory=$true)][string]$Destination)
$ErrorActionPreference='Stop'
$src=Join-Path $CopyRoot 'Delivery'
$catalog=Get-Content -Raw -LiteralPath (Join-Path $src 'actividades.json') | ConvertFrom-Json
$bundles=(Get-Content -Raw -LiteralPath (Join-Path $src 'bundles.json') | ConvertFrom-Json).names
if($catalog.names.Count -ne 18){throw 'Catálogo incompleto'}
$levels=@(Get-ChildItem -LiteralPath "$src/Content" -Filter 'nivel-*' -File | Where-Object Extension -ne '.manifest')
if($levels.Count -ne 18){throw 'Faltan paquetes de escenas'}
if(!(Test-Path "$src/index.html") -or !(Get-ChildItem -LiteralPath "$src/Build" -Filter '*.loader.js')){throw 'Falta el reproductor web'}
if(Test-Path -LiteralPath $Destination){throw 'Elegir una carpeta de distribución nueva'}
New-Item -ItemType Directory -Path $Destination | Out-Null
foreach($file in @('index.html','actividades.json')){ Copy-Item -LiteralPath (Join-Path $src $file) -Destination $Destination }
foreach($dir in @('Build','Media','StreamingAssets','Content')){
    if(!(Test-Path "$src/$dir")){continue}
    $target=Join-Path $Destination $dir
    New-Item -ItemType Directory -Path $target | Out-Null
    if($dir -eq 'Content'){
        foreach($name in $bundles){Copy-Item -LiteralPath "$src/$dir/$name" -Destination $target}
    }elseif($dir -eq 'StreamingAssets'){
        foreach($entry in Get-ChildItem -LiteralPath "$src/$dir"){
            if($entry.Name -ne 'SampleAssets'){Copy-Item -LiteralPath $entry.FullName -Destination $target -Recurse;continue}
            $sampleTarget=Join-Path $target $entry.Name
            foreach($sample in Get-ChildItem -LiteralPath $entry.FullName){
                if($sample.Name -in @('PresetAvatars_Rift.zip','PresetAvatars_Rift_Light.zip')){continue}
                New-Item -ItemType Directory -Path $sampleTarget -Force|Out-Null
                Copy-Item -LiteralPath $sample.FullName -Destination $sampleTarget -Recurse
            }
        }
    }else{Get-ChildItem -LiteralPath "$src/$dir" | Copy-Item -Destination $target -Recurse}
}
New-Item -ItemType File -Path "$Destination/.nojekyll" | Out-Null
$built=Get-ChildItem -LiteralPath $Destination -File -Recurse
$tooBig=$built|Where-Object Length -ge 100MB
if($tooBig){throw ("Archivos mayores de 100 MiB: "+($tooBig.Name -join ', '))}
if(($built|Measure-Object Length -Sum).Sum -gt 1000000000){throw 'La distribución excede el tamaño de GitHub Pages'}

