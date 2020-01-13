<#
    .SYNOPSIS
    Configures action group target for alerts

    .DESCRIPTION
    This script is used to configure action group to send alerts to Azure
    function with webhook. Web hook is used so it can contain the target
    channel as part of the url
#>
param(
    [Parameter(Mandatory)][string]$AlertUrl,
    [Parameter(Mandatory)][string]$ActionGroupResourceGroup,
    [Parameter(Mandatory)][string]$ActionGroupName
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$webHookReceiver = New-AzActionGroupReceiver -Name 'FunctionAppWebHook' -WebhookReceiver -ServiceUri $AlertUrl -UseCommonAlertSchema

# this also creates the target group
Set-AzActionGroup `
    -Name $ActionGroupName `
    -ResourceGroup $ActionGroupResourceGroup `
    -ShortName $ActionGroupName `
    -Receiver $webHookReceiver
