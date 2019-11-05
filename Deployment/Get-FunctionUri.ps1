<#
    .SYNOPSIS
    Retrieves function uri that needs to be configured to Slack API

    This assumes that user has already logged in with az login.

    NOTE: Invoke-RestMethod may fail if wrong SecurityProtocol is used.
    Tls12 should work

    .PARAMETER ResourceGroup
    Name of the resource group that has the web app deployed

    .PARAMETER WebAppName
    Name of the target web app

    .EXAMPLE
    .\Publish.ps1 -ResourceGroup "github-test" -WebAppName "test-app"
#>
param(
    [Parameter(Mandatory = $true)][string]$ResourceGroup,
    [Parameter()][string]$WebAppName = $ResourceGroup,
    [Parameter()][string]$FunctionName = 'RepositoryValidator'
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

. "./Deployment/FunctionUtil.ps1"

#$protocols = [System.Net.ServicePointManager]::SecurityProtocol
#Write-Host 'Your security protocol(s): ' $protocols
$kuduCreds = Get-KuduCredentials $WebAppName $ResourceGroup
$code = Get-FunctionKey $WebAppName $FunctionName $kuduCreds
# For some reason "https://$WebAppName.azurewebsites.net/api/$functionName?code=$code" did not work
[string]::Format('https://{0}.azurewebsites.net/api/{1}?code={2}', $WebAppName, $FunctionName, $code)
