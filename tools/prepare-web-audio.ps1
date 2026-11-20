param(
    [Parameter(Mandatory=$true)][string]$Framework,
    [string]$Brotli = "$env:ProgramFiles/Unity/Hub/Editor/6000.0.20f1/Editor/Data/PlaybackEngines/WebGLSupport/BuildTools/Brotli/win_x86_64/brotli.exe"
)
$ErrorActionPreference = 'Stop'
$inputJs = [IO.Path]::GetTempFileName()
$outputFile = [IO.Path]::GetTempFileName()
try {
    & $Brotli --decompress --force --input $Framework --output $inputJs
    if ($LASTEXITCODE) { throw 'No se pudo leer el reproductor de Unity.' }
    $js = [IO.File]::ReadAllText($inputJs)
    # Unity 6000.0.20f1 retains blocked audio until the next user gesture.
    # A scene can release that clip while the start is queued or its play promise is pending.
    $changes = [ordered]@{
        'this.mediaElement.src="";URL.revokeObjectURL(this.url)' = 'delete WEBAudio.pendingAudioSources[this.url];this.mediaElement.src="";URL.revokeObjectURL(this.url)'
        'source._startPlayback=function(offset){' = 'source._startPlayback=function(offset){if(!self.url||source.isStopped)return;'
        'source.playPromise=null;if(error.name!=="NotAllowedError")throw error;' = 'source.playPromise=null;if(!self.url||source.isStopped)return;if(error.name!=="NotAllowedError")throw error;'
    }
    foreach ($entry in $changes.GetEnumerator()) {
        $needle = $entry.Key
        if ($js.IndexOf($needle) -lt 0 -or $js.IndexOf($needle) -ne $js.LastIndexOf($needle)) {
            throw 'El reproductor ha cambiado. Revisar la gestión del audio al descargar escenas.'
        }
        $js = $js.Replace($needle,$entry.Value)
    }
    [IO.File]::WriteAllText($inputJs,$js,(New-Object Text.UTF8Encoding($false)))
    & $Brotli --force --quality 11 --comment 'UnityWeb Compressed Content (brotli)' --input $inputJs --output $outputFile
    if ($LASTEXITCODE) { throw 'No se pudo comprimir el reproductor.' }
    [IO.File]::Copy($outputFile,$Framework,$true)
} finally {
    [IO.File]::Delete($inputJs)
    [IO.File]::Delete($outputFile)
}
