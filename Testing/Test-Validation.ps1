<#
    .SYNOPSIS
    Send json to validation endpoint to test validation

    .DESCRIPTION
    This functions can be used the test Azure Functions validation endpoint
    without making changes in GitHub. This is also used as the last step of
    production deployment to warm up the app and verify that validation
    endpoint returns correct answer

    .PARAMETER ResourceGroup
    Resource group name

    .PARAMETER WebAppName
    Name of the web app

    .PARAMETER Organization
    Organization/user containing the repository (default protacon)

    .PARAMETER Repository
    Name of the repository to be validated  (default repository-validator)
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

$validationAddress = ./Deployment/Get-FunctionUri.ps1 `
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

Write-Host 'Send validation request'
Invoke-RestMethod -Method POST -Uri $validationAddress -Body $params -ContentType 'application/json;charset=UTF-8'

$statusCheckAddress = ./Deployment/Get-FunctionUri.ps1 `
    -WebApp $webApp `
    -FunctionName 'StatusCheck'

Write-Host 'Send status check request'
Invoke-RestMethod -Method GET -Uri $statusCheckAddress -ContentType 'application/json;charset=UTF-8'
