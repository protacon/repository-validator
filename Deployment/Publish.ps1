 ##############################################################################
 #.SYNOPSIS
 # Packs and publishes ValidationLibrary.AzureFunctions to Azure.
 #
 #.DESCRIPTION
 # Packs and publishes ValidationLibrary.AzureFunctions to Azure.
 # Scripts expects that the web app is already created.
 #
 # This assumes that user has already logged in with az login.
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
    [Parameter(Mandatory=$true)][string]$WebAppName)

$ErrorActionPreference = "Stop"
	
$publishFolder = "publish"

# delete any previous publish
if(Test-path $publishFolder) {Remove-Item -Recurse -Force $publishFolder}

dotnet publish -c Release -o $publishFolder ".\ValidationLibrary.AzureFunctions"

$destination = "publish.zip"
if(Test-path $destination) {Remove-item $destination}
Add-Type -assembly "system.io.compression.filesystem"

$fullSourcePath = (Resolve-Path ".\ValidationLibrary.AzureFunctions\$publishFolder").Path
$fullTargetPath = (Resolve-Path ".\").Path
$fullZipTarget = "$fullTargetPath\$destination"
[io.compression.zipfile]::CreateFromDirectory($fullSourcePath, $fullZipTarget)

az webapp deployment source config-zip -n $WebAppName -g $ResourceGroup --src $destination