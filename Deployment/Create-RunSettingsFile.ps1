<#
    .SYNOPSIS
    Retrieves necessary values from Azure and creates .runsettings-file

    .DESCRIPTION
    The Azure environment should already exist and the webapp already deployed.

    .PARAMETER SettinsFile
    Settings file that contains resource group name, web app name etc. Defaults to 'developer-settings.json'
#>
param(
    [Parameter()][string]$SettingsFile = 'developer-settings.json'
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Write-Host "Reading settings from file $SettingsFile"
$settingsJson = Get-Content -Raw -Path $SettingsFile | ConvertFrom-Json

. "./Deployment/FunctionUtil.ps1"

Write-Host "Fetch credentials..."
$kuduCreds = Get-KuduCredentials $settingsJson.ResourceGroupName $settingsJson.ResourceGroupName
$code = Get-DefaultKey -AppName $settingsJson.ResourceGroupName -EncodedCreds $kuduCreds

[xml]$document = New-Object System.Xml.XmlDocument
$declaration = $document.CreateXmlDeclaration('1.0', 'UTF-8', $null)
$document.AppendChild($declaration)
$root = $document.CreateElement('RunSettings')
$document.AppendChild($root)

$parameters = $document.CreateElement('TestRunParameters')
$root.AppendChild($parameters)

$appNameNode = $document.CreateElement('Parameter')
$appNameNode.SetAttribute('name', 'FunctionAppName')
$appNameNode.SetAttribute('value', $settingsJson.ResourceGroupName)
$parameters.AppendChild($appNameNode);

$codeNode = $document.CreateElement('Parameter')
$codeNode.SetAttribute('name', 'FunctionAppCode')
$codeNode.SetAttribute('value', $code)
$parameters.AppendChild($codeNode);

Write-Host "Create settings..."
$document.save("AcceptanceTests/.runsettings")
