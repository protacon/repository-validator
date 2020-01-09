<#
    .SYNOPSIS
    Configures action group target for testing

    .DESCRIPTION
    This script is used to configure action group to send alerts to Azure
    function with webhook. Web hook is used so it can contain the target
    channel as part of the url

    .PARAMETER SettingsFile
    Json file containing environment settings.
#>
param(
    [Parameter(Mandatory = $true)][string]$SlackChannel,
    [Parameter(Mandatory = $true)][PSSite]$AlertHandlingApp,
    [Parameter(Mandatory = $true)][string]$ActionGroupResourceGroup,
    [Parameter(Mandatory = $true)][string]$ActionGroupName
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$webApp = Get-AzWebApp `
    -ResourceGroupName $AlertHandlingResourceGroup `
    -Name $AlertHandlingWebApp
 
$address = ./Deployment/Get-FunctionUri.ps1 `
    -WebApp $webApp `
    -FunctionName 'AlertEndpoint'

$url = $address -Replace '{channel}', $SlackChannel
$webHookReceiver = New-AzActionGroupReceiver -Name 'FunctionAppWebHook' -WebhookReceiver -ServiceUri $url -UseCommonAlertSchema

$actionGroup = Get-AzActionGroup -Name $ActionGroupResourceGroup -ResourceGroupName $ActionGroupName

Set-AzActionGroup `
    -Name $actionGroup.Name `
    -ResourceGroup $settingsJson.AlertTargetResourceGroup `
    -ShortName $actionGroup.GroupShortName `
    -Receiver $webHookReceiver
