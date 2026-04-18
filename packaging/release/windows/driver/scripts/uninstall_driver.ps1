param(
    [string]$DevconPath = "$PSScriptRoot\devcon.exe",
    [string]$RootHardwareId = "Root\sysvad_ComponentizedAudioSample",
    [string]$ProviderMatch = "RifeZ Studio",
    [string]$OriginalInfMatch = "componentizedaudiosample.inf",
    [string]$DeviceDescriptionMatch = "RifeZ Audio Bridge Driver",
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
        Write-Host "Please close the tray app before uninstalling the driver." -ForegroundColor Yellow
        throw "Tray app is still running."
    }
}

function Get-Blocks {
    param([string[]]$Lines)

    $blocks = @()
    $current = @()

    foreach ($line in $Lines) {
        if ($line.Trim() -eq "") {
            if ($current.Count -gt 0) {
                $blocks += , @($current)
                $current = @()
            }
        }
        else {
            $current += $line
        }
    }

    if ($current.Count -gt 0) {
        $blocks += , @($current)
    }

    return $blocks
}

function Get-RifezPublishedNames {
    $lines = pnputil /enum-drivers
    $blocks = Get-Blocks -Lines $lines
    $results = @()

    foreach ($block in $blocks) {
        $published = $null
        $original = $null
        $provider = $null

        foreach ($line in $block) {
            if ($line -match '^\s*Published Name\s*:\s*(.+)$') {
                $published = $Matches[1].Trim()
            }
            elseif ($line -match '^\s*Original Name\s*:\s*(.+)$') {
                $original = $Matches[1].Trim()
            }
            elseif ($line -match '^\s*Provider Name\s*:\s*(.+)$') {
                $provider = $Matches[1].Trim()
            }
        }

        if ($published -and (
            ($provider -and $provider -like "*$ProviderMatch*") -or
            ($original -and $original -ieq $OriginalInfMatch)
        )) {
            $results += [pscustomobject]@{
                PublishedName = $published
                OriginalName  = $original
                ProviderName  = $provider
            }
        }
    }

    return $results
}

function Get-RifezDeviceInstanceIds {
    $allLines = pnputil /enum-devices
    $blocks = Get-Blocks -Lines $allLines
    $results = @()

    foreach ($block in $blocks) {
        $instanceId = $null
        $deviceDescription = $null
        $manufacturer = $null
        $driverName = $null
        $status = $null

        foreach ($line in $block) {
            if ($line -match '^\s*Instance ID\s*:\s*(.+)$') {
                $instanceId = $Matches[1].Trim()
            }
            elseif ($line -match '^\s*Device Description\s*:\s*(.+)$') {
                $deviceDescription = $Matches[1].Trim()
            }
            elseif ($line -match '^\s*Manufacturer Name\s*:\s*(.+)$') {
                $manufacturer = $Matches[1].Trim()
            }
            elseif ($line -match '^\s*Driver Name\s*:\s*(.+)$') {
                $driverName = $Matches[1].Trim()
            }
            elseif ($line -match '^\s*Status\s*:\s*(.+)$') {
                $status = $Matches[1].Trim()
            }
        }

        if ($instanceId -and
            $instanceId -like "ROOT\MEDIA\*" -and
            (
                ($manufacturer -and $manufacturer -like "*$ProviderMatch*") -or
                ($deviceDescription -and $deviceDescription -like "*$DeviceDescriptionMatch*")
            )) {
            $results += [pscustomobject]@{
                InstanceId        = $instanceId
                DeviceDescription = $deviceDescription
                ManufacturerName  = $manufacturer
                DriverName        = $driverName
                Status            = $status
            }
        }
    }

    return $results
}

function Remove-RifezDevices {
    $devices = Get-RifezDeviceInstanceIds

    if (-not $devices -or $devices.Count -eq 0) {
        Write-Host "No active/stale RifeZ device instances found."
        return
    }

    foreach ($dev in $devices) {
        Write-Host "Removing device instance: $($dev.InstanceId)  Driver=$($dev.DriverName)  Status=$($dev.Status)"
        pnputil /remove-device "$($dev.InstanceId)"
        Start-Sleep -Seconds 2
    }
}

function Remove-RifezDriverPackages {
    $drivers = Get-RifezPublishedNames

    if (-not $drivers -or $drivers.Count -eq 0) {
        Write-Host "No existing RifeZ driver package found."
        return
    }

    foreach ($drv in $drivers) {
        Write-Host "Removing driver package: $($drv.PublishedName)  Provider=$($drv.ProviderName)  Original=$($drv.OriginalName)"
        pnputil /delete-driver $drv.PublishedName /uninstall /force
        Start-Sleep -Seconds 3
    }
}

if (-not (Test-IsAdmin)) {
    throw "This script must be run as Administrator."
}

Ensure-Exists $DevconPath "DevCon"

Write-Section "RifeZ driver uninstall"

Write-Section "Pre-checks"
Ensure-TrayClosed

Write-Section "Step 1 - Remove current device instance(s)"
Remove-RifezDevices

Write-Section "Step 2 - Remove root device via DevCon"
& $DevconPath remove $RootHardwareId

Write-Critical "IMPORTANT: If the output above says 'Removed on reboot', YOU MUST REBOOT WINDOWS before reinstalling or before expecting the old device to be fully gone."

Write-Section "Step 3 - Remove RifeZ driver package(s)"
Remove-RifezDriverPackages

Write-Section "Step 4 - Rescan after uninstall"
pnputil /scan-devices
Start-Sleep -Seconds 3

Write-Host ""
Write-Host "Driver uninstall completed." -ForegroundColor Green