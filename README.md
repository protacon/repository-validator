# Repository Validator
[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](./LICENSE)
[![Build Status](https://jenkins.protacon.cloud/buildStatus/icon?job=www.github.com/repository-validator/master)](https://jenkins.protacon.cloud/blue/organizations/jenkins/www.github.com%2Frepository-validator/activity)
[![Dependabot Status](https://api.dependabot.com/badges/status?host=github&repo=protacon/repository-validator)](https://dependabot.com)

Checks that organization repositories conforms to policies defined by organization

For example, repositories should have
  * Descriptions
  * README.MD-files
  * License, if public

## Build
This project requires [dotnet core](https://www.microsoft.com/net/download),
see image used in Jenkinsfile for specific requirements.
```
dotnet build
```

## Testing

```
dotnet test
```

To run acceptances tests, set following environment variables
```
$Env:TEST_FunctionAppName = "name of the function app"
$Env:TEST_FunctionAppCode = "default code for function"
```

## Usage

There are 2 main ways to use this project
  * Console program
    * Scanning all repositories and writing results to CSV file
    ```
    dotnet run --project Runner -- scan-all --CsvFile results.csv
    ```
    * Scanning single repository and reporting to GitHub issues
    ```
    dotnet run -- scan-selected --GitHubReporting -r repository-validator
    ```
    * Scanning single repository and reporting to console (validation logic testing)
    ```
    dotnet run -- scan-selected -r repository-validator
    ```
    * Scanning single repository, reporting to console and creating pull requests when possible.
    ```
    dotnet run -- scan-selected -r repository-validator
    ```
  * Azure functions (GitHub WebHook for pushes)
    * Scan selected repository and report to GitHub issues and application insights

For usage instructions:
```
dotnet run --project Runner -- --help
```

### Configuration (this project)

Configuration parameters are read from appsettings.json-file for both, ValidationLibrary.AzureFunctions and Runner.
#### Console runner
For Runner
```
{
    "GitHub": {
        "Token": "token generated in github",
        "Organization": "organization name"
    },
    "Slack": {
        "WebHookUrl": "slack web hook url here",
        "ReportLimit": 20
    },
    "Logging": {
        "LogLevel": {
            "Default": "Trace"
        }
    }
}
```

When developing, create appsettings.Development.json and
replace configuration values with personal values
and use `setx ASPNETCORE_ENVIRONMENT "Development"` to set environment

#### Azure Functions
For Azure functions, configurations are read from Web site config.
See `Deployment/azuredeploy.json` and `Deployment`-section for configuration.

For local development and testing of Azure functions, see [function local development documentation](https://docs.microsoft.com/en-us/azure/azure-functions/functions-develop-local)

GitHub needs Azure Function's webhook url for `ValidationLibrary.AzureFunctions.RepositoryValidator.cs`.
This can be fetched with `Deployment/GetFunctionUri.ps1` or manually from Azure Portal. This url can be configured organization wide or seperately for each repository. See GitHub documentation for instructions.

### Configuration (validated repositories)

Each repository may contain `repository-validator.json` file which can be used to configure the way that repository is validated.
Currently it can be used to ignore certain rules by adding the class name to `IgnoredRules` array.

Example `repository-validator.json` file which ignores 4 rules.
```
{
    "Version": "1",
    "IgnoredRules": [
        "HasDescriptionRule",
        "HasNewestPtcsJenkinsLibRule",
        "HasReadmeRule"
        "HasLicenseRule"
    ]
}
```

#### Github webhook

To validate repositories after every push GitHub webhook needs to be configured. Webhooks can be configured on repository level and organization level.
Repository validator supports both ways but organization level webhook is recommended for production usage so webhook doesn't need to be configured for each
repository separately. Repository level webhook should be used for developing.

Webhook settings
* Payload URL should be something like `https://<web app name here>.azurewebsites.net/api/RepositoryValidator?code=<code here>`.
See [Azure Functions](#Azure-functions) payload URL generation
* Content type should be `application/json`
* Only push events should be sent. Validation probably works with other events too but this has not been tested. This can also
lead to extra trafic and events when we don't want to perform validation, like pull request reviews.

Read [GitHub developer guide](https://developer.github.com/webhooks/) for more information about webhooks.

## Deployment
Project ValidationLibrary.AzureFunctions can be deployed to Azure as Azure Function.
This function periodically validates repositories and reports to configured channel

These deployment examples uses powershell, but this can also be done otherwise.

NOTE: PowerShell needs to have an authenticated sessions. This can be done with `Connect-AzureRmAccount`.

### Creating environment

Deployment\azuredeploy.json contains definition of environment. It can be deployed in multiple ways, but here are few examples.
Read Deployment\azuredeploy.json for additional parameters and documentation.

Handle secrets with care, following secure string example is just for convience.

Azure PowerShell module
```
New-AzResourceGroupDeployment `
    -Name deployment-name `
    -TemplateFile Deployment/azuredeploy.json `
    -ResourceGroupName my-resource-group `
    -appName my-app-name `
    -gitHubToken (ConvertTo-SecureString -String "your github token here" -AsPlainText -Force) `
    -gitHubOrganization "your github organization here" `
    -environment "Development"
```

az cli
```
az group deployment create -g "github-test" --template-file Deployment/azuredeploy.json --parameters appName=hjni-test --parameters gitHubToken=<tokenhere> --parameters gitHubOrganization=protacon --parameters Development
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
