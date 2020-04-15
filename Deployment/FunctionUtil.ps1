function Get-KuduCredentials() {
    param(
        [Parameter(Mandatory = $true)][string]$AppName,
        [Parameter(Mandatory = $true)][string]$ResourceGroup
    )
    Write-Host "Getting credentials from RG $ResourceGroup for APP $AppName"

    $xml = [xml](Get-AzWebAppPublishingProfile -Name $AppName `
            -ResourceGroupName $ResourceGroup `
            -OutputFile $null)

    # Extract connection information from publishing profile
    $username = $xml.SelectNodes("//publishProfile[@publishMethod=`"MSDeploy`"]/@userName").value
    $password = $xml.SelectNodes("//publishProfile[@publishMethod=`"MSDeploy`"]/@userPWD").value

    $base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(("{0}:{1}" -f $username, $password)))
    return $base64AuthInfo
}

function Get-FunctionKey() {
    param(
        [Parameter(Mandatory = $true)][string]$AppName,
        [Parameter(Mandatory = $true)][string]$FunctionName,
        [Parameter(Mandatory = $true)][string]$EncodedCreds
    )

    $jwt = Get-Token -AppName $AppName -EncodedCreds $EncodedCreds

    $keys = Invoke-RestMethod -Method GET -Headers @{Authorization = ("Bearer {0}" -f $jwt) } `
        -Uri "https://$AppName.azurewebsites.net/admin/functions/$FunctionName/keys" 

    $code = $keys.keys[0].value
    return $code
}

function Get-Token() {
    param(
        [Parameter(Mandatory = $true)][string]$AppName,
        [Parameter(Mandatory = $true)][string]$EncodedCreds
    )
    $jwt = Invoke-RestMethod -Uri "https://$AppName.scm.azurewebsites.net/api/functions/admin/token" `
        -Headers @{Authorization = ("Basic {0}" -f $EncodedCreds) } `
        -Method GET

    return $jwt
}

function Get-InvokeUrl() {
    param(
        [Parameter(Mandatory = $true)][string]$AppName,
        [Parameter(Mandatory = $true)][string]$FunctionName,
        [Parameter(Mandatory = $true)][string]$EncodedCreds
    )

    $jwt = Get-Token -AppName $AppName -EncodedCreds $EncodedCreds

    $response = Invoke-RestMethod -Method GET -Headers @{Authorization = ("Bearer {0}" -f $jwt) } `
        -Uri "https://$AppName.azurewebsites.net/admin/functions/$FunctionName"

    $url = $response.invoke_url_template
    return $url
}
