#Requires -Version 5.1
[CmdletBinding()]
param()
$ErrorActionPreference = "Stop"

$exeName    = "cpool.exe"
$installDir = "$env:LOCALAPPDATA\Programs\cpool"
$exeSrc     = Join-Path $PSScriptRoot $exeName

if (-not (Test-Path $exeSrc)) {
    Write-Error "$exeName not found next to this script. Make sure both files are in the same folder."
    exit 1
}

Write-Host "Installing cpool to $installDir ..."
New-Item -ItemType Directory -Path $installDir -Force | Out-Null
Copy-Item -Path $exeSrc -Destination $installDir -Force

$userPath = [Environment]::GetEnvironmentVariable("Path", "User")
if ($null -eq $userPath) { $userPath = "" }

if (($userPath -split ";") -notcontains $installDir) {
    [Environment]::SetEnvironmentVariable("Path", ($userPath.TrimEnd(";") + ";$installDir"), "User")
    Write-Host "Added $installDir to your PATH."
} else {
    Write-Host "$installDir is already in your PATH."
}

Write-Host ""
Write-Host "Done! Open a new terminal and run: cpool --help"
