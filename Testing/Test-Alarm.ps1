<#
    .SYNOPSIS
    This will call Azure Function App multiple times with invalid requests
    which should cause an alarm

    .PARAMETER SettinsFile
    Settings file that contains environment settings.
    Defaults to 'developer-settings.json'
#>
param(
    [Parameter()][string]$SettingsFile = 'developer-settings.json'
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Write-Host "Reading settings from file $SettingsFile"
$settingsJson = Get-Content -Raw -Path $SettingsFile | ConvertFrom-Json

$address = ./Deployment/Get-FunctionUri.ps1 -ResourceGroup $settingsJson.ResourceGroupName

For ($i = 0; $i -le 10; $i++) {
    Try {
        Invoke-RestMethod -Method POST -Uri $address
    }
    Catch {
        # Do nothing
    }
}