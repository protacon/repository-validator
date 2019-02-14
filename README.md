# Repository Validator
[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](./LICENSE)
[![Build Status](https://jenkins.protacon.cloud/buildStatus/icon?job=www.github.com/repository-validator/master)](https://jenkins.protacon.cloud/blue/organizations/jenkins/www.github.com%2Frepository-validator/activity)

Checks that organization repositories conforms to policies defined by organization

For example, repositories should have
  * Descriptions
  * README.MD-files

## Build
This project requires [dotnet core 2.1](https://www.microsoft.com/net/download)
```
dotnet build
```

## Usage

In development (while in solution folder)

```
dotnet run --project Runner
```

Running folder also needs Notice.md -file. It is added in the end of issues created by Runner.

### Configuration

Configuration parameters are read from appsettings.json-file. 
```
{
    "Token": "token generated in github",
    "Organization": "organization name"
}
```

When developing, create appsettings.Development.json and
replace configuration values with personal values
and use `setx ASPNETCORE_ENVIRONMENT "Development"` to set environment

## Deployment
Project ValidationLibrary.AzureFunctions can be deployed to Azure as Azure Function.
This function periodically validates repositories and reports to configured channel

These deployment examples uses powershell, but this can also be done otherwise.

NOTE: PowerShell needs to have an authenticated sessions. This can be done with `Connect-AzureRmAccount`.

### Creating environment

Deployment\azuredeploy.json contains definition of environment. It can be deployed in multiple ways, but here are few examples

Azure PowerShell module
```
New-AzResourceGroupDeployment `
    -Name deployment-name `
    -TemplateFile Deployment/azuredeploy.json `
    -ResourceGroupName my-resource-group `
    -appName my-app-name `
    -gitHubToken "your github token here" `
    -gitHubOrganization "your github organization here" `
    -slackWebhookUrl "your slack webhook url here" `
    -environment "Development"
```

az cli
```
az group deployment create -g "github-test" --template-file Deployment/azuredeploy.json --parameters appName=hjni-test --parameters gitHubToken=<tokenhere> --parameters gitHubOrganization=protacon --parameters slackWebhookUrl=<slackwebhook>
```

### Deploying site

Pack ValidationLibrary.AzureFunctions into directory
```
dotnet publish -c Release -o my-publish-directory ValidationLibrary.AzureFunctions
```

Create Zip file for deployment
```
./Deployment/Zip.ps1 -Destination "publish.zip" -PublishFolder "ValidationLibrary.AzureFunctions/my-publish-directory"
```

Use Deployment/Deploy.ps1
```
./Deployment/Deploy.ps1 -ResourceGroup "my-resource-group" -WebAppName "my-app-name" -ZipFilePath "publish.zip"
```

## License

[The MIT License (MIT)](LICENSE)
