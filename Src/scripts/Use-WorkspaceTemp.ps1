# Sets a writable workspace-local temp directory for this PowerShell process.
# The root /codex/ directory is ignored by Git.
$workspaceRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$workspaceTemp = Join-Path $workspaceRoot 'codex\temp'
New-Item -ItemType Directory -Path $workspaceTemp -Force | Out-Null
$env:TEMP = $workspaceTemp
$env:TMP = $workspaceTemp
Write-Host "Workspace temp: $workspaceTemp"
