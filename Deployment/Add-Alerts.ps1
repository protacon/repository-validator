<#
    .SYNOPSIS
    Creates alerts for given resource group hosting Repository-Validator

    .DESCRIPTION
    Creates and prepares and environmnet for development and testing.
    SettingsFile (default developer-settings.json) should contain all
    relat

    .PARAMETER AlertTargetResourceGroup
    This is the resource group that has the target group for alerts (email etc.)

    .PARAMETER AlertTargetGroupName
    This is the name tof the target group for alerts (email etc.)

    .PARAMETER ResourceGroup
    Resource group hosting the Repository Validator solution
#>
param(
    [Parameter()][string]$AlertTargetResourceGroup,
    [Parameter()][string]$AlertTargetGroupName,
    [Parameter(Mandatory)][string]$ResourceGroup
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$resourceGroupObject = Get-AzResourceGroup -Name $ResourceGroup

Write-Host 'Retrieving resources and creating criterias'
$alertParameters = @(
    [PSCustomObject]@{
        Name        = 'Bad requests'
        Description = 'Too many bad requests received'
        Criteria    = New-AzMetricAlertRuleV2Criteria -MetricName 'Http4xx' -TimeAggregation Total -Operator GreaterThan -Threshold 5
        Resource    = (Get-AzResource -ResourceType 'Microsoft.Web/Sites' -ResourceGroupName $ResourceGroup)
    }
    [PSCustomObject]@{
        Name        = 'Exceptions'
        Description = 'Exceptions'
        Criteria    = New-AzMetricAlertRuleV2Criteria -MetricName 'exceptions/count' -TimeAggregation Count -Operator GreaterThan -Threshold 1
        Resource    = (Get-AzResource -ResourceType 'Microsoft.Insights/components' -ResourceGroupName $ResourceGroup)
    }
)

if ($AlertTargetResourceGroup) {
    Write-Host 'Retrieving alert action group...'
    $alertTargetActual = Get-AzActionGroup -ResourceGroupName $AlertTargetResourceGroup -Name $AlertTargetGroupName
    $alertRef = New-AzActionGroup -ActionGroupId $alertTargetActual.Id

    Write-Host 'Creating alerts'
    Foreach ($alertParameter in $alertParameters) {
        Write-Host "Creating alert for $($alertParameter.Name)"
        $resource = $alertParameter.Resource
        Add-AzMetricAlertRuleV2 `
            -Name $alertParameter.Name `
            -ResourceGroupName $ResourceGroup `
            -WindowSize 0:5 `
            -Frequency 0:5 `
            -TargetResourceScope $resource.ResourceId `
            -TargetResourceType $resource.ResourceType `
            -TargetResourceRegion $resourceGroupObject.Location `
            -Description $alertParameter.Description `
            -Severity 4 `
            -ActionGroup $alertRef `
            -Condition $alertParameter.Criteria
    }
}
else {
    Write-Host 'Creating alerts without target'
    Foreach ($alertParameter in $alertParameters) {
        Write-Host "Creating alert for $($alertParameter.Name)"
        $resource = $alertParameter.Resource
        Add-AzMetricAlertRuleV2 `
            -Name $alertParameter.Name `
            -ResourceGroupName $ResourceGroup `
            -WindowSize 0:5 `
            -Frequency 0:5 `
            -TargetResourceScope $resource.ResourceId `
            -TargetResourceType $resource.ResourceType `
            -TargetResourceRegion $resourceGroupObject.Location `
            -Description $alertParameter.Description `
            -Severity 4 `
            -Condition $alertParameter.Criteria
    }
}


Write-Host 'Alerts created'
