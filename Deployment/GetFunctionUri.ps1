 ##############################################################################
 #.SYNOPSIS
 # Retrieves function uri that needs to be configured to Slack API
 #
 # This assumes that user has already logged in with az login.
 #
 # NOTE: Invoke-RestMethod may fail if wrong SecurityProtocol is used.
 # Tls12 should work
 #
 #.PARAMETER ResourceGroup
 # Name of the resource group that has the web app deployed
 #
 #.PARAMETER WebAppName
 # Name of the target web app
 #
 #.EXAMPLE
 # .\Publish.ps1 -ResourceGroup "github-test" -WebAppName "hjni-test"
 ##############################################################################
param(
    [Parameter(Mandatory=$true)][string]$ResourceGroup,
    [Parameter(Mandatory=$true)][string]$WebAppName,
    [Parameter()][string]$FunctionName = 'RepositoryValidator'
    )

    
$ErrorActionPreference = "Stop"
function getKuduCreds($appName, $resourceGroup)
{
    $user = az webapp deployment list-publishing-profiles -n $appName -g $resourceGroup `
            --query "[?publishMethod=='MSDeploy'].userName" -o tsv

    $pass = az webapp deployment list-publishing-profiles -n $appName -g $resourceGroup `
            --query "[?publishMethod=='MSDeploy'].userPWD" -o tsv

    $pair = "$($user):$($pass)"
    $encodedCreds = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes($pair))
    return $encodedCreds
}

function getFunctionKey([string]$appName, [string]$functionName, [string]$encodedCreds)
{
    $jwt = Invoke-RestMethod -Uri "https://$appName.scm.azurewebsites.net/api/functions/admin/token" -Headers @{Authorization=("Basic {0}" -f $encodedCreds)} -Method GET

    $keys = Invoke-RestMethod -Method GET -Headers @{Authorization=("Bearer {0}" -f $jwt)} `
            -Uri "https://$appName.azurewebsites.net/admin/functions/$functionName/keys" 

    $code = $keys.keys[0].value
    return $code
}

$protocols = [System.Net.ServicePointManager]::SecurityProtocol
Write-Host 'Your security protocol(s): ' $protocols
$kuduCreds = getKuduCreds $WebAppName $ResourceGroup
$code =  getFunctionKey $WebAppName $FunctionName $kuduCreds
# For some reason "https://$WebAppName.azurewebsites.net/api/$functionName?code=$code" did not work
[string]::Format('https://{0}.azurewebsites.net/api/{1}?code={2}', $WebAppName, $FunctionName, $code)
