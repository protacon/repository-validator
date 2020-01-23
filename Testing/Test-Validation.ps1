<#
    .SYNOPSIS
    Send json to validation endpoint to test validation

    .DESCRIPTION
    This functions can be used the test Azure Functions validation endpoint
    without making changes in GitHub. This is also used as the last step of
    production deployment to warm up the app and verify that validation
    endpoint returns correct answer

    .PARAMETER ResourceGroup
    Json file containing environment settings.

    .PARAMETER AlertFile
    Alert json that is sent to alert endpoint
#>
[CmdLetBinding()]
param(
    [Parameter(Mandatory)][string]$ResourceGroup,
    [Parameter()][string]$WebAppName = $ResourceGroup,
    [Parameter()][string]$Organization = 'protacon',
    [Parameter()][string]$Repository = 'repository-validator'
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$webApp = Get-AzWebApp `
    -ResourceGroupName $ResourceGroup `
    -Name $WebAppName

$address = ./Deployment/Get-FunctionUri.ps1 `
    -WebApp $webApp `
    -FunctionName 'RepositoryValidator'

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

Write-Host 'Send alert'
Invoke-RestMethod -Method POST -Uri $address -Body $params -ContentType 'application/json;charset=UTF-8'
