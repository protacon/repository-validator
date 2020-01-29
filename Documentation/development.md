# Development guide

This document describes basics for `repository-validator` development

## Development flow

1. Create branch from `test`-branch with name `feature/my-feature-name-here` or
`fix/my-fix-name-here`
1. Make your changes to that branch
1. Create pull request to `test`-branch
1. Wait for review
1. Make fixes and/or merge the pull request if it was accepted. If you don't
ahve permission for that, wait for a person who does.

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

To run acceptances tests, create `.runsettings`-file with test parameters
using following script
```powershell
./Deployment/Create-RunSettingsFile -ResourceGroup 'resource-group-name'
```

Testing development environment can be created by creating your own version of
`developer-settings.example.json` as `developer-settings.json` and
then running `.\Deployment\Prepare-Envrionment.ps1`. For more details,
see the script.

You can also use `.\Testing\Test-Validation.ps1` to test the Azure Function