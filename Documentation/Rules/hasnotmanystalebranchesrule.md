## `HasNotManyStaleBranchesRule`

This rule checks that repository does not have too many stale branches.

To ignore HasNotManyStaleBranchesRule validation, use following `repository-validator.json`

```json
{
    "Version":"1",
    "IgnoredRules": [
        "HasNotManyStaleBranchesRule"
    ]
}
```

