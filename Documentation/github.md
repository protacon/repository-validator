# GitHub configuration

To validate repositories after every push GitHub webhook needs to be
configured .Webhooks can be configured on repository level and organization
level. Repository validator supports both ways but organization level webhook
is recommended for production usage so webhook doesn't need to be configured
for each repository separately. Repository level webhook should be used for
developing.

Webhook settings
* Payload URL should be something like `https://<web app name here>.azurewebsites.net/api/RepositoryValidator?code=<code here>`.
See [Azure Functions](#Azure-functions) payload URL generation
* Content type should be `application/json`
* Only push events should be sent. Validation probably works with other events
too but this has not been tested. This can also lead to extra trafic and events
when we don't want to perform validation, like pull request reviews.

Read [GitHub developer guide](https://developer.github.com/webhooks/)
for more information about webhooks.

## Azure functions endpoint URL

GitHub needs Azure Function's webhook url for
`ValidationLibrary.AzureFunctions.RepositoryValidator.cs`.
This can be fetched with `Deployment/GetFunctionUri.ps1` or manually from Azure
Portal. This url can be configured organization wide or seperately for each
repository. See GitHub documentation for instructions.

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




--

For Azure functions, configurations are read from Web site config.
See `Deployment/azuredeploy.json` and `Deployment`-section for configuration.

For local development and testing of Azure functions, see [function local development documentation](https://docs.microsoft.com/en-us/azure/azure-functions/functions-develop-local)