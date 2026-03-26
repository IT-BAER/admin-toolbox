#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Installs selected RSAT snap-in features for Admin Toolbox.
    Called automatically by the Inno Setup installer (already elevated).
.PARAMETER Features
    Comma-separated list of Windows capability names to install.
    If omitted, installs all supported RSAT features.
#>
param(
    [string]$Features
)

# Default to all supported features if none specified
if ([string]::IsNullOrWhiteSpace($Features)) {
    $featureList = @(
        "Rsat.ActiveDirectory.DS-LDS.Tools~~~~0.0.1.0",
        "Rsat.DHCP.Tools~~~~0.0.1.0",
        "Rsat.Dns.Tools~~~~0.0.1.0",
        "Rsat.GroupPolicy.Management.Tools~~~~0.0.1.0",
        "Rsat.PrintManagement.Tools~~~~0.0.1.0",
        "Rsat.CertificateServices.Tools~~~~0.0.1.0",
        "Rsat.FileServices.Tools~~~~0.0.1.0"
    )
} else {
    $featureList = $Features -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' }
}

$errors = @()

foreach ($feature in $featureList) {
    try {
        $cap = Get-WindowsCapability -Online -Name $feature -ErrorAction Stop
        if ($cap.State -ne "Installed") {
            Write-Host "Installing $feature ..."
            Add-WindowsCapability -Online -Name $feature -ErrorAction Stop | Out-Null
            Write-Host "  -> installed."
        } else {
            Write-Host "$feature already installed."
        }
    } catch {
        $errors += "Failed to install ${feature}: $_"
        Write-Warning $errors[-1]
    }
}

if ($errors.Count -gt 0) {
    # Non-fatal — app can still run if some features were already present
    $msg = "Some RSAT features could not be installed:`n" + ($errors -join "`n")
    Write-Warning $msg
    exit 1
}

exit 0
