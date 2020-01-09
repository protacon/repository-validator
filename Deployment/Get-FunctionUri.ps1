<#
    .SYNOPSIS
    Retrieves function uri that needs to be configured to Slack API

    This assumes that user has already logged in with az login.

    NOTE: Invoke-RestMethod may fail if wrong SecurityProtocol is used.
    Tls12 should work

    .PARAMETER WebApp
    PSSite web app

    .EXAMPLE
    .\Publish.ps1 -ResourceGroup "github-test" -WebAppName "test-app"
#>
param(
    [Parameter(Mandatory = $true)][Microsoft.Azure.Commands.WebApps.Models.PSSite]$WebApp,
    [Parameter()][string]$FunctionName = 'RepositoryValidator'
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

. "./Deployment/FunctionUtil.ps1"

$kuduCreds = Get-KuduCredentials -App $WebApp
$code = Get-FunctionKey $WebApp.Name $FunctionName $kuduCreds
$url = Get-InvokeUrl $WebApp.Name $FunctionName $kuduCreds
return $url + "?code=" + $code
