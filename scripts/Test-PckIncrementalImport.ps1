param(
    [Parameter(Mandatory = $true)]
    [string] $TargetPck,

    [string] $TargetPkx,

    [Parameter(Mandatory = $true)]
    [string] $StagingDirectory,

    [string] $HelperPath = "tools\FWPck\FWPckUpdater.exe",

    [string] $WorkRoot = ".tmp-pck-probe",

    [switch] $UseOriginalTarget
)

$ErrorActionPreference = "Stop"

function Resolve-FullPath([string] $PathValue) {
    if ([string]::IsNullOrWhiteSpace($PathValue)) {
        return ""
    }

    return [System.IO.Path]::GetFullPath($PathValue)
}

function Copy-TargetPackage {
    param(
        [string] $PckPath,
        [string] $PkxPath,
        [string] $DestinationRoot
    )

    New-Item -ItemType Directory -Force -Path $DestinationRoot | Out-Null
    $targetPckCopy = Join-Path $DestinationRoot ([System.IO.Path]::GetFileName($PckPath))
    Copy-Item -LiteralPath $PckPath -Destination $targetPckCopy -Force

    $targetPkxCopy = ""
    if (![string]::IsNullOrWhiteSpace($PkxPath) -and (Test-Path -LiteralPath $PkxPath)) {
        $targetPkxCopy = Join-Path $DestinationRoot ([System.IO.Path]::GetFileName($PkxPath))
        Copy-Item -LiteralPath $PkxPath -Destination $targetPkxCopy -Force
    }

    return $targetPckCopy
}

function Get-RelativePackagePath {
    param(
        [string] $Root,
        [string] $PathValue
    )

    $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    $pathFull = [System.IO.Path]::GetFullPath($PathValue)
    if ($pathFull.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $pathFull.Substring($rootFull.Length).TrimStart('\', '/').Replace('/', '\')
    }

    return [System.IO.Path]::GetFileName($PathValue)
}

$targetPckFull = Resolve-FullPath $TargetPck
$targetPkxFull = Resolve-FullPath $TargetPkx
$stagingFull = Resolve-FullPath $StagingDirectory
$helperFull = Resolve-FullPath $HelperPath
$workRootFull = Resolve-FullPath $WorkRoot

if (!(Test-Path -LiteralPath $targetPckFull)) {
    throw "Target PCK not found: $targetPckFull"
}
if (!(Test-Path -LiteralPath $stagingFull)) {
    throw "Staging directory not found: $stagingFull"
}
if (!(Test-Path -LiteralPath $helperFull)) {
    throw "FWPckUpdater not found: $helperFull"
}

$testRoot = $workRootFull
if (!$UseOriginalTarget) {
    $testRoot = Join-Path $workRootFull ("run-" + [guid]::NewGuid().ToString("N"))
    $targetPckFull = Copy-TargetPackage -PckPath $targetPckFull -PkxPath $targetPkxFull -DestinationRoot $testRoot
}

$files = Get-ChildItem -LiteralPath $stagingFull -Recurse -File
Write-Host "Target: $targetPckFull"
Write-Host "Staging: $stagingFull"
Write-Host "Files: $($files.Count)"
foreach ($file in $files | Select-Object -First 12) {
    Write-Host ("  " + (Get-RelativePackagePath -Root $stagingFull -PathValue $file.FullName))
}
if ($files.Count -gt 12) {
    Write-Host "  ..."
}

& $helperFull update $stagingFull $targetPckFull 1
$exitCode = $LASTEXITCODE
Write-Host "Exit: $exitCode"

if ($exitCode -ne 0) {
    exit $exitCode
}

Write-Host "Incremental import completed."
