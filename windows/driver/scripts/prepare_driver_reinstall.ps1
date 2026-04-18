param(
    [string]$DevconPath = "$PSScriptRoot\devcon.exe",
    [string]$RootHardwareId = "Root\sysvad_ComponentizedAudioSample",
    [string]$TrayProcessName = "RifeZAudioBridge.App"
)

$ErrorActionPreference = "Stop"

function Write-Section($text) {
    Write-Host ""
    Write-Host "==== $text ====" -ForegroundColor Cyan
}

function Write-Critical($text) {
    Write-Host ""
    Write-Host "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!" -ForegroundColor Yellow
    Write-Host $text -ForegroundColor Yellow
    Write-Host "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!" -ForegroundColor Yellow
    Write-Host ""
}

function Test-IsAdmin {
    $currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentIdentity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Ensure-Exists($path, $label) {
    if (-not (Test-Path $path)) {
        throw "$label not found: $path"
    }
}

function Ensure-TrayClosed {
    $procs = Get-Process -Name $TrayProcessName -ErrorAction SilentlyContinue
    if ($procs) {
        Write-Host "Tray app process '$TrayProcessName' appears to be running." -ForegroundColor Yellow
        Write-Host "Please close the tray app before preparing driver reinstall." -ForegroundColor Yellow
        throw "Tray app is still running."
    }
}

if (-not (Test-IsAdmin)) {
    throw "This script must be run as Administrator."
}

Ensure-Exists $DevconPath "DevCon"

Write-Section "RifeZ driver reinstall - Stage 1 (prepare)"
Write-Host "DevCon path: $DevconPath"
Write-Host "Root hardware ID: $RootHardwareId"

Write-Section "Pre-checks"
Ensure-TrayClosed

Write-Section "Removing existing root device"
& $DevconPath remove $RootHardwareId

Write-Critical "IMPORTANT: If the output above says 'Removed on reboot', YOU MUST REBOOT WINDOWS NOW before running the complete reinstall script."

Write-Host "Next step after reboot:"
Write-Host "  Run complete_driver_reinstall.bat as Administrator." -ForegroundColor Green