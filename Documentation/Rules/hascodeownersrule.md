## `HasCodeownersRule`

This rule checks that repository has CODEOWNERS defined

To ignore HasCodeownersRule validation, use following `repository-validator.json`

```json
{
    "Version":"1",
    "IgnoredRules": [
        "HasCodeownersRule"
    ]
}
```

