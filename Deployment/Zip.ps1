 ##############################################################################
 #.SYNOPSIS
 # Packs ValidationLibrary.AzureFunctions to Zip
 #
 #.DESCRIPTION
 # Packs ValidationLibrary.AzureFunctions to Zip
 #
 #.EXAMPLE
 # .\Zip.ps1
 ##############################################################################

$ErrorActionPreference = "Stop"
$publishFolder = "publish"
$destination = "publish.zip"
if(Test-path $destination) {Remove-item $destination}
Add-Type -assembly "system.io.compression.filesystem"
[io.compression.zipfile]::CreateFromDirectory("ValidationLibrary.AzureFunctions/$publishFolder", $destination)