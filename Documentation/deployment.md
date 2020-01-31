# Deployment

This document describes how `repository-validator` can be deployed to Azure.

Project `ValidationLibrary.AzureFunctions` can be deployed to Azure as Azure
Function. This function validates repositories and reports to configured
channel when it receives HTTP POST with json content from GitHub (Webhook)

These deployment examples uses powershell, but this can also be done otherwise.

NOTE: Azure PowerShell needs to have an authenticated sessions. This can be
done with `Connect-AzAccount`.

To quickly create & deploy to the environment, use
`Deployment\Prepare-Environment.ps1`. It reads the configurations from
`developer-settings.json`
```powershell
./Deployment/Prepare-Environment.ps1
```

## Creating environment with Powershell

`Deployment\azuredeploy.json` contains definition of environment. It can be
deployed in multiple ways, but here are few examples. Read
`Deployment\azuredeploy.json` for additional parameters and documentation.

Handle secrets with care, following secure string example is just for convience.

Azure PowerShell module
```powershell
New-AzResourceGroupDeployment `
    -Name 'deployment-name' `
    -TemplateFile 'Deployment/azuredeploy.json' `
    -ResourceGroupName 'my-resource-group' `
    -appName 'my-app-name' `
    -gitHubToken (ConvertTo-SecureString -String "your github token here" -AsPlainText -Force) `
    -gitHubOrganization 'your github organization here' `
    -environment 'Development'
```

## Deploying site

Build and pack `ValidationLibrary.AzureFunctions` into a directory
```
dotnet publish -c Release -o my-publish-directory ValidationLibrary.AzureFunctions
```

Create Zip file for deployment
```powershell
Compress-Archive `
    -DestinationPath "publish.zip" `
    -Path "ValidationLibrary.AzureFunctions/my-publish-directory/*"
```

Use [Publish-AzWebApp](https://docs.microsoft.com/en-us/powershell/module/az.websites/publish-azwebapp)
to deploy the application
```powershell
Publish-AzWebApp -ResourceGroupName $ResourceGroup -Name $WebAppName -ArchivePath $fullZipTarget -Force
```

## Testing the deployment

To test the deployment, you can use `Test-Validation.ps1`. This calls the validation
endpoint with example json, which validates this repository
```powershell
./Testing/Test-Validation.ps1 -ResourceGroup 'my-resource-group' -WebAppName 'my-app-name'
```