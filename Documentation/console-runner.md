# Console runner

This document describes what Console runner is and how it is used.

Console runner is a console interface for Repository Validator and it can be
used to validate single, multiple or all repositories in organization.

This is mainly used for testing or generating organization wide validation
reports.

## Configuration

For production isuage, just replace the values in `appsettings.json` with
correct values

When developing, create `appsettings.Development.json` and
replace configuration values with personal values
and use `setx ASPNETCORE_ENVIRONMENT "Development"` to set environment

```json
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

## Usage

Following examples assumes that program is ran from project folder

* Printing help
```
dotnet run -- help
```
* Scanning all repositories and writing results to CSV file
```
dotnet run -- scan-all --CsvFile results.csv
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

