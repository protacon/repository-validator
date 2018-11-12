# Repository Validator
Checks that organization repositories conforms to policies defined by organization

For example, repositories should have
  * descriptions
  * README.MD-files
  * Licenses 

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


## License
See LICENSE-file