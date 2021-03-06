<#
    .SYNOPSIS
    Retrieves necessary values from Azure and creates .runsettings-file

    .DESCRIPTION
    The Azure environment should already exist and the webapp already deployed.

    .PARAMETER ResourceGroup
    Name of the resource group that has the web app deployed.
    
    .PARAMETER WebAppName
    Name of the target web app. If not set, resource group name is used.
#>
param(
    [Parameter(Mandatory)][string]$ResourceGroup,
    [Parameter()][string]$WebAppName = $ResourceGroup
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

. "./Deployment/FunctionUtil.ps1"

Write-Host "Fetch credentials..."
$kuduCreds = Get-KuduCredentials $WebAppName $ResourceGroup
$code = Get-DefaultKey $WebAppName $kuduCreds

[xml]$document = New-Object System.Xml.XmlDocument
$declaration = $document.CreateXmlDeclaration('1.0', 'UTF-8', $null)
$document.AppendChild($declaration)
$root = $document.CreateElement('RunSettings')
$document.AppendChild($root)

$parameters = $document.CreateElement('TestRunParameters')
$root.AppendChild($parameters)

$appNameNode = $document.CreateElement('Parameter')
$appNameNode.SetAttribute('name', 'FunctionAppName')
$appNameNode.SetAttribute('value', $WebAppName)
$parameters.AppendChild($appNameNode);

$codeNode = $document.CreateElement('Parameter')
$codeNode.SetAttribute('name', 'FunctionAppCode')
$codeNode.SetAttribute('value', $code)
$parameters.AppendChild($codeNode);

Write-Host "Create settings..."
$document.save("AcceptanceTests/.runsettings")
