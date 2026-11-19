param([Parameter(Mandatory=$true)][string]$CopyRoot,[string]$Ffmpeg='ffmpeg',[string]$Ffprobe='ffprobe',[string]$VideoEncoder='libx264')
$ErrorActionPreference='Stop'
$media=Join-Path $CopyRoot 'Delivery/Media'
New-Item -ItemType Directory -Path $media -Force | Out-Null
foreach($clip in Get-ChildItem -LiteralPath "$CopyRoot/Unity/Assets" -Filter *.mp4 -File -Recurse){
    $guid=((Select-String -LiteralPath ($clip.FullName+'.meta') -Pattern '^guid:').Line -split ' ')[1]
    if($guid -notmatch '^[a-f0-9]{32}$'){throw "GUID inválido para $($clip.Name)"}
    $dst=Join-Path $media ($guid+'.mp4')
    if(Test-Path $dst){continue}
    $seconds=[double]::Parse((& $Ffprobe -v error -show_entries format=duration -of default=nw=1:nk=1 $clip.FullName),[CultureInfo]::InvariantCulture)
    $rate=[int][Math]::Max(100,[Math]::Min(1100,(650000/$seconds)-64))
    $temp=Join-Path $media ($guid+'.partial.mp4')
    & $Ffmpeg -nostdin -loglevel error -y -i $clip.FullName -map 0:v:0 -map '0:a:0?' -vf "scale=w='min(854,iw)':h=-2" -c:v $VideoEncoder -b:v ("${rate}k") -maxrate ("${rate}k") -bufsize ("{0}k" -f (2*$rate)) -pix_fmt yuv420p -c:a aac -b:a 64k -movflags +faststart $temp
    if($LASTEXITCODE){throw "Falló la conversión de $($clip.Name)"}
    Move-Item -LiteralPath $temp -Destination $dst
}

