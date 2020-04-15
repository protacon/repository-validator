<#
    .SYNOPSIS
    Send json to validation endpoint to test validation

    .DESCRIPTION
    This functions can be used the test Azure Functions validation endpoint
    without making changes in GitHub. This is also used as the last step of
    production deployment to warm up the app and verify that validation
    endpoint returns correct answer

    .PARAMETER Organization
    Organization/user containing the repository (default by-pinja)

    .PARAMETER Repository
    Name of the repository to be validated  (default repository-validator)

    .PARAMETER SettinsFile
    Settings file that contains environment settings.
    Defaults to 'developer-settings.json'
#>
[CmdLetBinding()]
param(
    [Parameter()][string]$Organization = 'by-pinja',
    [Parameter()][string]$Repository = 'repository-validator',
    [Parameter()][string]$SettingsFile = 'developer-settings.json'
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Write-Host "Reading settings from file $SettingsFile"
$settingsJson = Get-Content -Raw -Path $SettingsFile | ConvertFrom-Json

$validationAddress = ./Deployment/Get-FunctionUri.ps1 `
    -ResourceGroup $settingsJson.ResourceGroupName `
    -FunctionName 'RepositoryValidatorTrigger'

# This should match the webhook content sent by github, but we are only using
# required properties
$params = @{
    'repository' = @{
        'name'  = $Repository
        'owner' = @{
            'login' = $Organization
        }
    }
} | ConvertTo-Json

Write-Host 'Send validation request'
Invoke-RestMethod -Method POST -Uri $validationAddress -Body $params -ContentType 'application/json;charset=UTF-8'

$statusCheckAddress = ./Deployment/Get-FunctionUri.ps1 `
    -ResourceGroup $settingsJson.ResourceGroupName `
    -FunctionName 'StatusCheck'

Write-Host 'Send status check request'
Invoke-RestMethod -Method GET -Uri $statusCheckAddress -ContentType 'application/json;charset=UTF-8'
