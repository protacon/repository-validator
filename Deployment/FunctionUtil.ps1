function getKuduCreds($appName, $resourceGroup) {
    $user = az webapp deployment list-publishing-profiles -n $appName -g $resourceGroup `
        --query "[?publishMethod=='MSDeploy'].userName" -o tsv

    $pass = az webapp deployment list-publishing-profiles -n $appName -g $resourceGroup `
        --query "[?publishMethod=='MSDeploy'].userPWD" -o tsv

    $pair = "$($user):$($pass)"
    $encodedCreds = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes($pair))
    return $encodedCreds
}

function getFunctionKey([string]$appName, [string]$functionName, [string]$encodedCreds) {
    $jwt = Invoke-RestMethod -Uri "https://$appName.scm.azurewebsites.net/api/functions/admin/token" -Headers @{Authorization = ("Basic {0}" -f $encodedCreds) } -Method GET

    $keys = Invoke-RestMethod -Method GET -Headers @{Authorization = ("Bearer {0}" -f $jwt) } `
        -Uri "https://$appName.azurewebsites.net/admin/functions/$functionName/keys" 

    $code = $keys.keys[0].value
    return $code
}
function getDefaultCode([string]$appName, [string]$encodedCreds) {
    $jwt = Invoke-RestMethod -Uri "https://$appName.scm.azurewebsites.net/api/functions/admin/token" -Headers @{Authorization = ("Basic {0}" -f $encodedCreds) } -Method GET

    $keys = Invoke-RestMethod -Method GET -Headers @{Authorization = ("Bearer {0}" -f $jwt) } `
        -Uri "https://$appName.azurewebsites.net/admin/host/keys" 

    $code = $keys.keys[0].value
    return $code
}