param([Parameter(Mandatory=$true)][string]$CopyRoot,[string]$Ffmpeg='ffmpeg',[string]$VideoEncoder='libx264')
$ErrorActionPreference='Stop'
$media=Join-Path $CopyRoot 'Delivery/Media'
$mobile=Join-Path $media 'Movil'
New-Item -ItemType Directory -Path $mobile -Force | Out-Null
foreach($clip in '1','1.1','2.0','2.1','3.0','3.1'){
    $meta=Join-Path $CopyRoot "Unity/Assets/videos/$clip.mp4.meta"
    $guid=((Select-String -LiteralPath $meta -Pattern '^guid:').Line -split ' ')[1]
    $original=Join-Path $media "$guid.mp4"
    $output=Join-Path $mobile "$guid.mp4"
    $temporary=Join-Path $mobile "$guid.partial.mp4"
    & $Ffmpeg -nostdin -loglevel error -y -i $original -map 0:v:0 -map '0:a:0?' -vf 'fps=15,scale=576:-2' -c:v $VideoEncoder -b:v 290k -maxrate 420k -bufsize 840k -pix_fmt yuv420p -c:a aac -b:a 48k -movflags +faststart $temporary
    if($LASTEXITCODE -ne 0){throw "Falló la conversión del monitor $clip"}
    Move-Item -LiteralPath $temporary -Destination $output -Force
}
