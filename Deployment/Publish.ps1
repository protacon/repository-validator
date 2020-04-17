<#
    .SYNOPSIS
    Packs and publishes ValidationLibrary.AzureFunctions to Azure.
    This is meant to be an from repository root.

    .DESCRIPTION
    Packs and publishes ValidationLibrary.AzureFunctions to Azure.
    Scripts expects that the web app is already created.

    This assumes that user has already logged in to the Azure Powershell Module.

    .PARAMETER ResourceGroup
    Name of the resource group that has the web app deployed

    .PARAMETER WebAppName
    Name of the target web app

    .EXAMPLE
    .\Publish.ps1 -ResourceGroup "github-test" -WebAppName "hjni-test"
#>
param(
    [Parameter(Mandatory)][string]$ResourceGroup,
    [Parameter()][string]$WebAppName = $ResourceGroup,
    [Parameter()][string]$VersionSuffx = "DEV")

$ErrorActionPreference = "Stop"
	
$publishFolder = "publish"
$azureFunctionProject = 'ValidationLibrary.AzureFunctions';

# delete any previous publish
if (Test-path $publishFolder) { Remove-Item -Recurse -Force $publishFolder }

dotnet publish -c Release -o $publishFolder $azureFunctionProject --version-suffix $VersionSuffx

$destination = "publish.zip"
$fullSourcePath = (Resolve-Path "$publishFolder").Path
$fullTargetPath = (Resolve-Path ".\").Path
$fullZipTarget = Join-Path -Path $fullTargetPath -ChildPath $destination

Compress-Archive -DestinationPath $fullZipTarget -Path "$fullSourcePath/*" -Force

Write-Host "Deploying new version."
Publish-AzWebApp -ResourceGroupName $ResourceGroup -Name $WebAppName -ArchivePath $fullZipTarget -Force
Write-Host 'Version deployed'
