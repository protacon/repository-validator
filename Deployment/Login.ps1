 ##############################################################################
 #.SYNOPSIS
 # Connects to Azure
 #
 #.PARAMETER ApplicationId
 # Application ID ("username" of Service Principal)
 #
 #.PARAMETER ApplicationKey
 # Key ("password" of Service Principal)
 #
 #.PARAMETER TenantId
 # Tenant ID
 ##############################################################################
param(
    [Parameter(Mandatory=$true)][string]$ApplicationId,
    [Parameter(Mandatory=$true)][string]$ApplicationKey,
	[Parameter(Mandatory=$true)][string]$TenantId)

$ErrorActionPreference = "Stop"

$securePassword = ConvertTo-SecureString -String $ApplicationKey -AsPlainText -Force
$credential = New-Object -TypeName System.Management.Automation.PSCredential -ArgumentList $ApplicationId, $securePassword
Add-AzureRmAccount -ServicePrincipal -ApplicationId $ApplicationId -Credential $credential -TenantId $TenantId