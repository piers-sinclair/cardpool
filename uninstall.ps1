#Requires -Version 5.1
[CmdletBinding()]
param()
$ErrorActionPreference = "Stop"

$installDir = "$env:LOCALAPPDATA\Programs\cpool"

if (Test-Path $installDir) {
    Remove-Item -Path $installDir -Recurse -Force
    Write-Host "Removed $installDir"
} else {
    Write-Host "Nothing to remove - $installDir does not exist."
}

$userPath = [Environment]::GetEnvironmentVariable("Path", "User")
if ($null -ne $userPath) {
    $newPath = ($userPath -split ";" | Where-Object { $_ -ne $installDir }) -join ";"
    if ($newPath -ne $userPath) {
        [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
        Write-Host "Removed $installDir from your PATH."
    }
}

Write-Host "Done. Open a new terminal for the PATH change to take effect."
