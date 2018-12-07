 ##############################################################################
 #.SYNOPSIS
 # Packs ValidationLibrary.AzureFunctions to Zip
 #
 #.DESCRIPTION
 # Packs ValidationLibrary.AzureFunctions to Zip
 #
 #.PARAMETER Destination
 # Target destination
 #
 #.PARAMETER PublishFolder
 # Folder that is packaged
 #
 #.EXAMPLE
 # .\Zip.ps1 -PublishFolder "target-folder" -Destination "test.zip"
 ##############################################################################
 param(
    [Parameter(Mandatory=$true)][string]$Destination,
    [Parameter(Mandatory=$true)][string]$PublishFolder)

$ErrorActionPreference = "Stop"
if(Test-path $Destination) {Remove-item $Destination}
Add-Type -assembly "system.io.compression.filesystem"
[io.compression.zipfile]::CreateFromDirectory($PublishFolder, $Destination)