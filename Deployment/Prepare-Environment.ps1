<#
    .SYNOPSIS
    Creates environment in Azure from given settings file

    .DESCRIPTION
    Creates and prepares and environment for development and testing.
    SettingsFile (default developer-settings.json) should contain all
    relevant information.

    This assumes that the user has already logged in to the Azure Powershell Module.

    .PARAMETER SettinsFile
    Settings file that contains environment settings. Defaults to 'developer-settings.json'
#>
param(
    [Parameter()][string]$SettingsFile = 'developer-settings.json'
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Write-Host "Reading settings from file $SettingsFile"
$settingsJson = Get-Content -Raw -Path $SettingsFile | ConvertFrom-Json

$tagsHashtable = @{ }
if ($settingsJson.Tags) {
    $settingsJson.Tags.psobject.properties | ForEach-Object { $tagsHashtable[$_.Name] = $_.Value }
}

Write-Host "Creating resource group $($settingsJson.ResourceGroupName) to location $($settingsJson.Location)..."
New-AzResourceGroup -Name $settingsJson.ResourceGroupName -Location $settingsJson.Location -Tag $tagsHashtable -Force

Write-Host 'Creating environment...'
New-AzResourceGroupDeployment `
    -Name 'test-deployment' `
    -TemplateFile 'Deployment/azuredeploy.json' `
    -ResourceGroupName $settingsJson.ResourceGroupName `
    -appName $settingsJson.ResourceGroupName `
    -gitHubToken (ConvertTo-SecureString -String $settingsJson.GitHubToken -AsPlainText -Force) `
    -gitHubOrganization $settingsJson.GitHubOrganization `
    -environment "Development"

Write-Host 'Publishing...'
.\Deployment\Publish.ps1 -ResourceGroup $settingsJson.ResourceGroupName

if ($settingsJson.AzureAlarmHandlerUrl) {
    Write-Host 'Creating action group'
    .\Deployment\Set-ActionGroup.ps1 `
        -AlertUrl $settingsJson.AzureAlarmHandlerUrl `
        -ActionGroupResourceGroup $settingsJson.ResourceGroupName `
        -ActionGroupName 'repo-alerts'

    .\Deployment\Add-Alerts.ps1 `
        -ResourceGroup $settingsJson.ResourceGroupName `
        -AlertTargetResourceGroup $settingsJson.ResourceGroupName `
        -AlertTargetGroupName 'repo-alerts'
}
else {
    Write-Host 'No Azure Alarm Webhook specified, creating only alarms'
    .\Deployment\Add-Alerts.ps1 `
        -ResourceGroup $settingsJson.ResourceGroupName
}
