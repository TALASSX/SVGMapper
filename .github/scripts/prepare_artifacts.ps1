param()
$out = Join-Path $PSScriptRoot "..\..\artifacts"
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Path $out | Out-Null
# Prefer existing release/ folder (packaged output), else copy compiled output
$repoRoot = Resolve-Path "$PSScriptRoot\..\.."
$releasePath = Join-Path $repoRoot "release"
if (Test-Path $releasePath) {
    Copy-Item -Path (Join-Path $releasePath '*') -Destination $out -Recurse -Force
} else {
    $candidates = Get-ChildItem -Path $repoRoot -Recurse -Filter "SVGMapper.Minimal.exe" -ErrorAction SilentlyContinue
    if ($candidates) {
        foreach($c in $candidates){ Copy-Item -Path (Join-Path $c.DirectoryName '*') -Destination $out -Recurse -Force }
    } else {
        Write-Output "No compiled EXE found; ensure the project builds or add outputs to /release."
    }
}
