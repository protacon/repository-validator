<#
    .SYNOPSIS
    Generates code coverage HTML report

    .DESCRIPTION
    This runs tests, generates coverage report and HTML documentation from it.
    This uses ReportGenerator (https://github.com/danielpalme/ReportGenerator)
    as a global dotnet tool, so this may fail if the tool is not installed
    globally. 

    "dotnet tool install --global dotnet-reportgenerator-globaltool" installs
    it globally, but you should read the documentation of ReportGenerator
    before this.

    .PARAMETER DeleteOldTestResults
    Deletes old test results before running tests
#>
param(
  [Parameter()][switch]$DeleteOldTestResults
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($DeleteOldTestResults) {
  Get-ChildItem *\*\ -Filter TestResults | Remove-Item -Recurse
}

dotnet test --collect:"XPlat Code Coverage"

reportgenerator `
  "-reports:**/TestResults/*/coverage.cobertura.xml" `
  "-targetdir:coveragereport" `
  -reporttypes:Html
