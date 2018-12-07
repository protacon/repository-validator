library 'jenkins-ptcs-library@docker-depencies'

podTemplate(label: pod.label,
  containers: pod.templates + [
    containerTemplate(name: 'dotnet', image: 'microsoft/dotnet:2.1-sdk', ttyEnabled: true, command: '/bin/sh -c', args: 'cat'),
    containerTemplate(name: 'powershell', image: 'mcr.microsoft.com/powershell:6.1.0-ubuntu-18.04', ttyEnabled: true, command: '/bin/sh -c', args: 'cat'),
    containerTemplate(name: 'az-cli', image: 'microsoft/azure-cli:2.0.50', ttyEnabled: true, command: '/bin/sh -c', args: 'cat')
  ]
) {
    def branch = (env.BRANCH_NAME)
    def resourceGroup = 'ptcs-github-validator'
    def appName = 'ptcs-github-validator'
    def gitHubOrganization = 'protacon'

    def zipName = "publish.zip"
    def publishFolder = "publish"

    node(pod.label) {
        stage('Checkout') {
            checkout_with_tags()
        }
        container('dotnet') {
            stage('Build') {
                sh """
                    dotnet publish -c Release -o $publishFolder ValidationLibrary.AzureFunctions
                """
            }
        }
        container('powershell') {
            stage('Package') {
                sh """
                    pwsh Deployment/Zip.ps1
                """
            }
        }
        container('az-cli') {
            withCredentials([
                string(credentialsId: 'hjni_azure_sp_id', variable: 'SP_APPLICATION'),
                string(credentialsId: 'hjni_azure_sp_key', variable: 'SP_KEY'),
                string(credentialsId: 'hjni_azure_sp_tenant', variable: 'SP_TENANT'),
                ]){
                stage('Login'){
                    sh """
                        az login --service-principal --username $SP_APPLICATION --password $SP_KEY --tenant $SP_TENANT
                    """
                }
            }
            withCredentials([
                string(credentialsId: 'hjni_github_token', variable: 'GH_TOKEN'),
                string(credentialsId: 'hjni_slack_webhook', variable: 'SLACK_WEBHOOK')
            ]){
                stage('Create environment') {
                    sh """
                        az group deployment create -g $resourceGroup --template-file Deployment/azuredeploy.json --parameters appName=$appName --parameters gitHubToken=$GH_TOKEN --parameters gitHubOrganization=$gitHubOrganization --parameters slackWebhookUrl=$SLACK_WEBHOOK
                    """
                }
            }
            stage('Publish') {
                sh """
                    az webapp deployment source config-zip -n $appName -g $resourceGroup --src $destination
                """
            }
        }
    }
  }