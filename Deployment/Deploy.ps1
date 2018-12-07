 ##############################################################################
 #.SYNOPSIS
 # Deploys zip file to web app.
 #
 #.DESCRIPTION
 # Deploys packaged zip app to web app.
 # Assumes that PowerShell session is already authenticated
 #
 #.PARAMETER ResourceGroup
 # Name of the resource group that has the web app deployed
 #
 #.PARAMETER WebAppName
 # Name of the target web app
 #
 #.PARAMETER ZipFilePath
 # Path of the zip file
 #
 #.EXAMPLE
 # .\Deploy.ps1 -ResourceGroup "github-test" -WebAppName "hjni-test" -ZipFilePath "publish.zip"
 ##############################################################################
param(
    [Parameter(Mandatory=$true)][string]$ResourceGroup,
    [Parameter(Mandatory=$true)][string]$WebAppName,
	[Parameter(Mandatory=$true)][string]$ZipFilePath)

$ErrorActionPreference = "Stop"

# Get publishing profile for the web app
$xml = [xml](Get-AzureRmWebAppPublishingProfile -Name $WebAppName `
	-ResourceGroupName $ResourceGroup `
	-OutputFile null)

# Extract connection information from publishing profile
$username = $xml.SelectNodes("//publishProfile[@publishMethod=`"MSDeploy`"]/@userName").value
$password = $xml.SelectNodes("//publishProfile[@publishMethod=`"MSDeploy`"]/@userPWD").value
$url = $xml.SelectNodes("//publishProfile[@publishMethod=`"MSDeploy`"]/@publishUrl").value
$apiUrl = "https://$url/api/zipdeploy"
$base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(("{0}:{1}" -f $username, $password)))
$userAgent = "powershell/1.0"
Invoke-RestMethod -Uri $apiUrl -Headers @{Authorization=("Basic {0}" -f $base64AuthInfo)} -UserAgent $userAgent -Method POST -InFile $ZipFilePath -ContentType "multipart/form-data"