library 'jenkins-ptcs-library@docker-depencies'

podTemplate(label: pod.label,
  containers: pod.templates + [
    containerTemplate(name: 'dotnet', image: 'microsoft/dotnet:2.1-sdk', ttyEnabled: true, command: '/bin/sh -c', args: 'cat'),
    containerTemplate(name: 'powershell', image: 'azuresdk/azure-powershell-core:master', ttyEnabled: true, command: '/bin/sh -c', args: 'cat')
  ]
) {
    def branch = (env.BRANCH_NAME)
    def resourceGroup = 'ptcs-github-validator'
    def appName = 'ptcs-github-validator'
    def gitHubOrganization = 'protacon'

    def functionsProject = 'ValidationLibrary.AzureFunctions'
    def zipName = 'publish.zip'
    def publishFolder = 'publish'

    node(pod.label) {
        stage('Checkout') {
            checkout_with_tags()
        }
        container('dotnet') {
            stage('Build') {
                sh """
                    dotnet publish -c Release -o $publishFolder $functionsProject
                """
            }
        }
        container('powershell') {
            stage('Package') {
                pwsh """
                    ./Deployment/Zip.ps1 -Destination $zipName -PublishFolder $functionsProject/$publishFolder
                """
            }
            withCredentials([
                string(credentialsId: 'hjni_azure_sp_id', variable: 'SP_APPLICATION'),
                string(credentialsId: 'hjni_azure_sp_key', variable: 'SP_KEY'),
                string(credentialsId: 'hjni_azure_sp_tenant', variable: 'SP_TENANT'),
                ]){
                stage('Login'){
                    pwsh """
                        $credential = New-Object -TypeName System.Management.Automation.PSCredential -ArgumentList $SP_APPLICATION, $SP_KEY
                        Connect-AzureRmAccount -ServicePrincipal -Credential $credential -TenantId $SP_TENANT
                    """
                }
            }
            withCredentials([
                string(credentialsId: 'hjni_github_token', variable: 'GH_TOKEN'),
                string(credentialsId: 'hjni_slack_webhook', variable: 'SLACK_WEBHOOK')
            ]){
                stage('Create environment') {
                    pwsh """
                        New-AzureRmResourceGroupDeployment `
                            -Name github-validator `
                            -TemplateFile Deployment/azuredeploy.json `
                            -ResourceGroupName $resourceGroup `
                            -appName $appName `
                            -gitHubToken $GH_TOKEN `
                            -gitHubOrganization $gitHubOrganization `
                            -slackWebhookUrl $SLACK_WEBHOOK
                    """
                }
            }
            stage('Publish') {
                pwsh """
                    ./Deployment/Deploy.ps1 -ResourceGroup $resourceGroup  -WebAppName $appName -ZipFilePath $zipName
                """
            }
        }
    }
  }