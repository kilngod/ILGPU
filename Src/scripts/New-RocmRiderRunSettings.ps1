param(
    [Parameter(Mandatory = $true)]
    [string] $Architecture,
    [string] $HipccPath,
    [string] $VcvarsPath,
    [string] $MSVCToolsetVersion = '14.44',
    [string] $OutputPath
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$programFiles64 = if ($env:ProgramW6432) { $env:ProgramW6432 } else { $env:ProgramFiles }
if (-not $OutputPath) { $OutputPath = Join-Path $repoRoot 'codex\rocm-tests.runsettings' }
if (-not $HipccPath) { $HipccPath = $env:ILGPU_HIPCC_PATH }
if (-not $HipccPath -and $env:HIP_PATH) {
    $HipccPath = Join-Path $env:HIP_PATH 'bin\hipcc.exe'
}
if (-not $HipccPath) {
    $installRoot = Join-Path $programFiles64 'AMD\ROCm'
    $sdk = Get-ChildItem -LiteralPath $installRoot -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match '^7\.(\d+)(?:\.(\d+))?$' } |
        Sort-Object { [version]($_.Name + $(if ($_.Name -match '^7\.\d+$') { '.0' })) } -Descending |
        Select-Object -First 1
    if ($sdk) { $HipccPath = Join-Path $sdk.FullName 'bin\hipcc.exe' }
}
if (-not $HipccPath -or -not (Test-Path -LiteralPath $HipccPath -PathType Leaf)) {
    throw 'No hipcc.exe found. Pass -HipccPath or set ILGPU_HIPCC_PATH.'
}
$HipccPath = (Resolve-Path -LiteralPath $HipccPath).Path

if (-not $VcvarsPath) {
    $vsRoot = Join-Path $programFiles64 'Microsoft Visual Studio'
    $VcvarsPath = Get-ChildItem -LiteralPath $vsRoot -Recurse -Filter vcvars64.bat `
        -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
}
if (-not $VcvarsPath -or -not (Test-Path -LiteralPath $VcvarsPath -PathType Leaf)) {
    throw 'No vcvars64.bat found. Pass -VcvarsPath.'
}

$vcEnvironment = @{}
$lines = & cmd.exe /c "`"$VcvarsPath`" -vcvars_ver=$MSVCToolsetVersion >nul && set"
if ($LASTEXITCODE -ne 0) { throw "vcvars64.bat failed for MSVC $MSVCToolsetVersion" }
foreach ($line in $lines) {
    if ($line -match '^([^=]+)=(.*)$') { $vcEnvironment[$matches[1]] = $matches[2] }
}
if ($vcEnvironment['VCToolsVersion'] -notlike "$MSVCToolsetVersion*") {
    throw "Expected MSVC $MSVCToolsetVersion; got $($vcEnvironment['VCToolsVersion'])"
}

$tempPath = Join-Path $repoRoot 'codex\temp'
New-Item -ItemType Directory -Path $tempPath -Force | Out-Null
$settings = New-Object System.Xml.XmlDocument
$declaration = $settings.CreateXmlDeclaration('1.0', 'utf-8', $null)
[void] $settings.AppendChild($declaration)
$root = $settings.CreateElement('RunSettings')
[void] $settings.AppendChild($root)
$runConfiguration = $settings.CreateElement('RunConfiguration')
[void] $root.AppendChild($runConfiguration)
$variables = $settings.CreateElement('EnvironmentVariables')
[void] $runConfiguration.AppendChild($variables)

$wanted = @(
    'PATH', 'INCLUDE', 'LIB', 'LIBPATH', 'VCToolsVersion',
    'VCToolsInstallDir', 'VCINSTALLDIR', 'VSINSTALLDIR',
    'WindowsSdkDir', 'WindowsSDKVersion', 'UniversalCRTSdkDir',
    'UCRTVersion', 'VSCMD_ARG_TGT_ARCH', 'VSCMD_ARG_HOST_ARCH',
    'VSCMD_ARG_VCVARS_VER'
)
foreach ($name in $wanted) {
    if ($vcEnvironment[$name]) {
        $element = $settings.CreateElement($name)
        $element.InnerText = $vcEnvironment[$name]
        [void] $variables.AppendChild($element)
    }
}
$custom = @{
    ILGPU_ROCM_COMPILER = 'local'
    ILGPU_ROCM_ARCH = $Architecture
    ILGPU_HIPCC_PATH = $HipccPath
    TEMP = $tempPath
    TMP = $tempPath
}
foreach ($name in $custom.Keys) {
    $element = $settings.CreateElement($name)
    $element.InnerText = $custom[$name]
    [void] $variables.AppendChild($element)
}
New-Item -ItemType Directory -Path (Split-Path -Parent $OutputPath) -Force |
    Out-Null
$settings.Save($OutputPath)
Write-Host "Rider test settings: $OutputPath"
Write-Host "HIP compiler: $HipccPath"
Write-Host "Architecture: $Architecture; MSVC: $($vcEnvironment['VCToolsVersion'])"
