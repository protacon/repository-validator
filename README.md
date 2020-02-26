# Repository Validator
[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](./LICENSE)
[![Build Status](https://jenkins.protacon.cloud/buildStatus/icon?job=www.github.com/repository-validator/master)](https://jenkins.protacon.cloud/blue/organizations/jenkins/www.github.com%2Frepository-validator/activity)
[![Dependabot Status](https://api.dependabot.com/badges/status?host=github&repo=protacon/repository-validator)](https://dependabot.com)

Checks that organization repositories conforms to policies defined by organization

For example, repositories should have
  * Descriptions
  * README.MD-files
  * Licenses, if public

Read more about [Rules](rules.md)

If you have received an *Issue* or a *Pull Request* created by this application, see
[repository validation configuration guide](https://github.com/protacon/repository-validator/wiki/Validation-Configuration)

Read project [Wiki](https://github.com/protacon/repository-validator/wiki) for
more information about project usage and development

## Contributing

If you want to help, see [contributing guidelines](CONTRIBUTING.md)

To learn how `repository-validator` is built and tested, see [Development guide](https://github.com/protacon/repository-validator/wiki/Development)

## Overview

There are 2 main ways to use this project

Console interface is describe [here](https://github.com/protacon/repository-validator/wiki/Console-Runner)

### Azure Functions interface

Azure functions received (GitHub WebHook for pushes) and scans selected
repository and report to GitHub issues and application insights

Azure Function configurations are read from Web site config.
See `Deployment/azuredeploy.json` and `Deployment`-section for configuration.

For local development and testing of Azure functions, see [function local development documentation](https://docs.microsoft.com/en-us/azure/azure-functions/functions-develop-local)

## Deployment

This repository is deployed to productions for every push to `master`-branch.
For other environments, or your own environments, see
[Deployment guide](https://github.com/protacon/repository-validator/wiki/Deployment)

## GitHub Configuration

To configure GitHub Webhooks, see [GitHub configuration](https://github.com/protacon/repository-validator/wiki/Github)

## License

[The MIT License (MIT)](LICENSE)
