# Repository Validation Configuration guide

Each repository may contain `repository-validator.json` file which can be used
to configure the way that repository is validated. Currently it can be used to
ignore certain rules by adding the class name to `IgnoredRules` array.

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

[Rules](rules.md) contains specific details for each available rule.