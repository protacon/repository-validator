param(
    [Parameter(Mandatory = $true)][string]$AlertTargetResourceGroup,
    [Parameter(Mandatory = $true)][string]$AlertTargetGroupName,
    [Parameter(Mandatory = $true)][string]$ResourceGroup,
    [Parameter(Mandatory = $true)][string]$SiteName
)

$alertTargetActual = Get-AzActionGroup -ResourceGroupName $AlertTargetResourceGroup -Name $AlertTargetGroupName
$alertRef = New-AzActionGroup -ActionGroupId $alertTargetActual.Id

$badRequests = New-AzMetricAlertRuleV2Criteria -MetricName "Http4xx" -TimeAggregation Total -Operator GreaterThan -Threshold 5
$site = Get-AzResource -Name $SiteName -ResourceType 'Microsoft.Web/Sites'

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