<#
    .SYNOPSIS
    Packs and publishes ValidationLibrary.AzureFunctions to Azure.
    This is meant to be an from repository root.

    .DESCRIPTION
    Packs and publishes ValidationLibrary.AzureFunctions to Azure.
    Scripts expects that the web app is already created.

    This assumes that user has already logged in with az login.

    .PARAMETER ResourceGroup
    Name of the resource group that has the web app deployed

    .PARAMETER WebAppName
    Name of the target web app

    .EXAMPLE
    .\Publish.ps1 -ResourceGroup "github-test" -WebAppName "hjni-test"
#>
param(
    [Parameter(Mandatory = $true)][string]$ResourceGroup,
    [Parameter()][string]$WebAppName = $ResourceGroup,
    [Parameter()][string]$VersionSuffx = "DEV")

$ErrorActionPreference = "Stop"
	
$publishFolder = "publish"
$azureFunctionProject = 'ValidationLibrary.AzureFunctions';

# delete any previous publish
if (Test-path $publishFolder) { Remove-Item -Recurse -Force $publishFolder }

dotnet publish -c Release -o $publishFolder $azureFunctionProject --version-suffix $VersionSuffx

$destination = "publish.zip"
if (Test-path $destination) { Remove-item $destination }
Add-Type -assembly "system.io.compression.filesystem"

$fullSourcePath = (Resolve-Path "$publishFolder").Path
$fullTargetPath = (Resolve-Path ".\").Path
$fullZipTarget = "$fullTargetPath\$destination"
[io.compression.zipfile]::CreateFromDirectory($fullSourcePath, $fullZipTarget)

Write-Host 'Fetching publishing profiles...'
# Get publishing profile for the web app
$xml = [xml](Get-AzWebAppPublishingProfile -Name $WebAppName `
        -ResourceGroupName $ResourceGroup `
        -OutputFile $null)

# Extract connection information from publishing profile
$username = $xml.SelectNodes("//publishProfile[@publishMethod=`"MSDeploy`"]/@userName").value
$password = $xml.SelectNodes("//publishProfile[@publishMethod=`"MSDeploy`"]/@userPWD").value
$url = $xml.SelectNodes("//publishProfile[@publishMethod=`"MSDeploy`"]/@publishUrl").value
$apiUrl = "https://$url/api/zipdeploy"
$base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(("{0}:{1}" -f $username, $password)))
$userAgent = "powershell/1.0"

Write-Host "Deploying new version to $apiUrl..."
Invoke-RestMethod -Uri $apiUrl -Headers @{Authorization = ("Basic {0}" -f $base64AuthInfo) } -UserAgent $userAgent -Method POST -InFile $fullZipTarget -ContentType "multipart/form-data"
Write-Host 'Version deployed'