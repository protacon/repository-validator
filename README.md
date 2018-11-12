# Repository Validator
[![Build Status](https://jenkins.protacon.cloud/buildStatus/icon?job=www.github.com/repository-validator/master)](https://jenkins.protacon.cloud/blue/organizations/jenkins/www.github.com%2Frepository-validator/activity)
Checks that organization repositories conforms to policies defined by organization

For example, repositories should have
  * Descriptions
  * README.MD-files
  * Licenses 

## Build
This project requires dotnet core 2.1
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

## License
See LICENSE-file
