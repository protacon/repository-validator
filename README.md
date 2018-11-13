# Repository Validator
[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](./LICENSE)
[![Build Status](https://jenkins.protacon.cloud/buildStatus/icon?job=www.github.com/repository-validator/master)](https://jenkins.protacon.cloud/blue/organizations/jenkins/www.github.com%2Frepository-validator/activity)

NOTE: This is still WIP.

Checks that organization repositories conforms to policies defined by organization

For example, repositories should have
  * Descriptions
  * README.MD-files
  * Licenses 

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

These deployment examples use az cli, but this can also be done otherwise.,

### Creating environment

Use Deployment\azuredeploy.json, this can be deployed with az cli.
```
az group deployment create -g "github-test" --template-file Deployment/azuredeploy.json --parameters appName=hjni-test
```

### Deploying site

Use Deployment\Publish.ps1
```
.\Deployment\Publish.ps1 -ResourceGroup "github-test" -WebAppName "hjni-test"
```

## License

[The MIT License (MIT)](LICENSE)
