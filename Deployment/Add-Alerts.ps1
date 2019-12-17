param(
    [Parameter(Mandatory = $true)][string]$AlertTargetResourceGroup,
    [Parameter(Mandatory = $true)][string]$AlertTargetGroupName,
    [Parameter(Mandatory = $true)][string]$ResourceGroup,
    [Parameter(Mandatory = $true)][string]$SiteName
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$alertParameters = @(
    [PSCustomObject]@{
        Name         = 'Bad requests'
        Description  = 'Too many bad requests received'
        MetricName   = 'Http4xx'
        ResourceName = 'Microsoft.Web/Sites'
        ResourceType = $SiteName
    }
)

Write-Host 'Creating alerts'
Foreach ($alertParameter in $alertParameters) {
    $alertTargetActual = Get-AzActionGroup -ResourceGroupName $AlertTargetResourceGroup -Name $AlertTargetGroupName
    $alertRef = New-AzActionGroup -ActionGroupId $alertTargetActual.Id
    
    $badRequests = New-AzMetricAlertRuleV2Criteria -MetricName $alertParameter.MetricName -TimeAggregation Total -Operator GreaterThan -Threshold 5
    $site = Get-AzResource -Name $alertParameter.ResourceName -ResourceType $alertParameter.ResourceType
    
    Add-AzMetricAlertRuleV2 `
        -Name 'TestAlert' `
        -ResourceGroupName $ResourceGroup `
        -WindowSize 0:5 `
        -Frequency 0:5 `
        -TargetResourceScope $site.ResourceId `
        -TargetResourceType $site.ResourceType `
        -TargetResourceRegion "northeurope" `
        -Description "This is description" `
        -Severity 4 `
        -ActionGroup $alertRef `
        -Condition $badRequests
}
Write-Host 'Alerts created'