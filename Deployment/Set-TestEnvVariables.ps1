<#
    .SYNOPSIS
    Retrieves necessary values from Azure and sets environment variables
    for testing.

    .PARAMETER ResourceGroup
    Name of the resource group that has the web app deployed

    .PARAMETER WebAppName
    Name of the target web app. If not set, resource group name is used.
#>
param(
    [Parameter(Mandatory = $true)][string]$ResourceGroup,
    [Parameter()][string]$WebAppName = $ResourceGroup)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

. "./Deployment/FunctionUtil.ps1"

Write-Host "Current Function app name $Env:TEST_FunctionAppName"
Write-Host "Current Function app code $Env:TEST_FunctionAppCode"

$kuduCreds = getKuduCreds $WebAppName $ResourceGroup
$code = getDefaultCode $WebAppName $kuduCreds

$Env:TEST_FunctionAppName = $WebAppName
$Env:TEST_FunctionAppCode = $code
Write-Host "New codes set."