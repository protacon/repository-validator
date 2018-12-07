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

    def zipName = "publish.zip"
    def publishFolder = "publish"

    def testCred = credentials('hjni_azure_sp_id')

    node(pod.label) {
        stage('Checkout') {
            checkout_with_tags()
        }
        container('dotnet') {
            stage('Build') {
                sh """
                    echo $testCred
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
            stage('Create environment') {
                sh """
                    az group deployment create -g "$resourceGroup" --template-file Deployment/azuredeploy.json --parameters appName=$appName
                """
            }
            stage('Publish') {
                sh """
                    az webapp deployment source config-zip -n $appName -g $resourceGroup --src $destination
                """
            }
        }
    }
  }