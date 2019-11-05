<#
    .SYNOPSIS
    Connects to Azure

    .PARAMETER ApplicationId
    Application ID ("username" of Service Principal)

    .PARAMETER ApplicationKey
    Key ("password" of Service Principal)

    .PARAMETER TenantId
    Tenant ID

    .PARAMETER SubscriptionId
    Subscription Id
#>
param(
    [Parameter(Mandatory = $true)][string]$ApplicationId,
    [Parameter(Mandatory = $true)][string]$ApplicationKey,
    [Parameter(Mandatory = $true)][string]$TenantId,
    [Parameter(Mandatory = $true)][string]$SubscriptionId
)

$ErrorActionPreference = "Stop"

$securePassword = ConvertTo-SecureString -String $ApplicationKey -AsPlainText -Force
$credential = New-Object -TypeName System.Management.Automation.PSCredential -ArgumentList $ApplicationId, $securePassword
Connect-AzAccount -Credential $credential -TenantId $TenantId -Subscription $SubscriptionId -ServicePrincipal