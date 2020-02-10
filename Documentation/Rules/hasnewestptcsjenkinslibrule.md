## `HasNewestPtcsJenkinsLibRule`

Rule validates that Jenkinsfile has newest jenkins-ptcs-library is used if jenkins-ptcs-library is used at all.  jenkins-ptcs-library is an internal company library that offers utilities for CI pipelines.

To ignore HasNewestPtcsJenkinsLibRule validation, use following `repository-validator.json`

```json
{
    "Version":"1",
    "IgnoredRules": [
        "HasNewestPtcsJenkinsLibRule"
    ]
}
```

