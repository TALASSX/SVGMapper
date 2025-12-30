param()
$repoRoot = Resolve-Path "$PSScriptRoot\..\.."
$ReleaseDir = Join-Path $repoRoot "release"
if (-not (Test-Path $ReleaseDir)) { Write-Output "No release folder found; skipping MSI build"; exit 0 }
$art = Join-Path $repoRoot "artifacts"
if (-not (Test-Path $art)) { New-Item -ItemType Directory -Path $art | Out-Null }
$productWxs = Join-Path $repoRoot "installer\Product.wxs"
$releaseWxs = Join-Path $art "ReleaseFiles.wxs"
$prodWixobj = Join-Path $art "Product.wixobj"
$relWixobj = Join-Path $art "ReleaseFiles.wixobj"
& heat dir $ReleaseDir -ag -sfrag -scom -ke -cg ReleaseFiles -dr INSTALLFOLDER -var var.ReleaseDir -out $releaseWxs
& candle.exe -out $prodWixobj $productWxs -dReleaseDir="$ReleaseDir"
& candle.exe -out $relWixobj $releaseWxs -dReleaseDir="$ReleaseDir"
& light.exe -sval -ext WixUIExtension -out (Join-Path $art "SVGMapper.Installer.msi") $prodWixobj $relWixobj
Write-Output "MSI created at: $(Join-Path $art 'SVGMapper.Installer.msi')"
