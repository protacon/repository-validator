library 'jenkins-ptcs-library@2.1.0'

def isMaster(branchName) {return branchName == "master"}
def isTest(branchName) {return branchName == "test"}

podTemplate(label: pod.label,
  containers: pod.templates + [
    containerTemplate(name: 'dotnet', image: 'microsoft/dotnet:2.2-sdk', ttyEnabled: true, command: '/bin/sh -c', args: 'cat'),
    containerTemplate(name: 'powershell', image: 'azuresdk/azure-powershell-core:master', ttyEnabled: true, command: '/bin/sh -c', args: 'cat')
  ]
) {

    def branch = (env.BRANCH_NAME)
    def buildNumber = (env.BUILD_NUMBER)
    def resourceGroup = 'repository-validator-prod'
    def appName = 'ptcs-github-validator'
    def gitHubOrganization = 'protacon'

    def functionsProject = 'ValidationLibrary.AzureFunctions'
    def zipName = 'publish.zip'
    def publishFolder = 'publish'
    def environment = isMaster(branch) ? 'Production' : 'Development'

    node(pod.label) {
        stage('Checkout') {
            checkout scm
        }
        container('dotnet') {
            stage('Build') {
                sh """
                    dotnet publish -c Release -o $publishFolder $functionsProject --version-suffix ${env.BUILD_NUMBER}
                """
            }
            stage('Test') {
                sh """
                    dotnet test
                """
            }
        }
        if (isTest(branch) || isMaster(branch)){
            container('powershell') {
                stage('Package') {
                    sh """
                        pwsh -command "&./Deployment/Zip.ps1 -Destination $zipName -PublishFolder $functionsProject/$publishFolder"
                    """
                }

                if (isTest(branch)){
                    withCredentials([azureServicePrincipal('PTCS_Development_use_SP')]) {
                        def ciRg = 'repo-ci-' + buildNumber
                        def ciAppName = 'repo-ci-' + buildNumber

                        stage('Login to test'){
                            sh """
                                pwsh -command "./Deployment/Login.ps1 -ApplicationId '$AZURE_CLIENT_ID' -ApplicationKey '$AZURE_CLIENT_SECRET' -TenantId '$AZURE_TENANT_ID' -SubscriptionId $AZURE_SUBSCRIPTION_ID"
                            """
                        }
                        stage('Create temporary Resource Group'){
                            sh """
                                pwsh -command "New-AzResourceGroup -Name '$ciRg' -Location 'North Europe' -Tag @{subproject='2026956'; Description='Continuous Integration'}"
                            """
                        }
                        withCredentials([
                            string(credentialsId: 'hjni_github_token', variable: 'GH_TOKEN')
                        ]) {
                            stage('Create test environment'){
                                sh """
                                    pwsh -command "New-AzResourceGroupDeployment -Name github-validator -TemplateFile Deployment/azuredeploy.json -ResourceGroupName $ciRg -appName $ciAppName -gitHubToken (ConvertTo-SecureString -String $GH_TOKEN -AsPlainText -Force) -gitHubOrganization $gitHubOrganization -environment $environment"
                                """
                            }
                        }
                        try {
                            stage('Publish to test environment') {
                                sh """
                                    pwsh -command "&./Deployment/Deploy.ps1 -ResourceGroup $ciRg -WebAppName $ciAppName -ZipFilePath $zipName"
                                """
                            }
                            stage('Create .runsettings-file acceptance tests') {
                                sh """
                                    pwsh -command "&./Deployment/Create-RunSettingsFile.ps1 -ResourceGroup $ciRg -WebAppName $ciAppName"
                                """
                            }
                            container('dotnet') {
                                stage('Acceptance tests') {
                                    sh """
                                        cd AcceptanceTests
                                        dotnet test --settings '.runsettings'
                                    """
                                }
                            }
                        }
                        finally {
                            stage('Delete test environment'){
                                sh """
                                    pwsh -command "Remove-AzResourceGroup -Name '$ciRg' -Force"
                                """
                            }
                        }
                    }
                }
                if (isMaster(branch)){
                    withCredentials([azureServicePrincipal('PTCG_Azure_SP')]){
                        stage('Login to production'){
                            sh """
                                pwsh -command "./Deployment/Login.ps1 -ApplicationId '$AZURE_CLIENT_ID' -ApplicationKey '$AZURE_CLIENT_SECRET' -TenantId '$AZURE_TENANT_ID' -SubscriptionId $AZURE_SUBSCRIPTION_ID"
                            """
                        }
                    }
                    withCredentials([
                        string(credentialsId: 'hjni_github_token', variable: 'GH_TOKEN')
                    ]){
                        stage('Create production environment') {
                            sh """
                                pwsh -command "New-AzResourceGroupDeployment -Name github-validator -TemplateFile Deployment/azuredeploy.json -ResourceGroupName $resourceGroup -appName $appName -gitHubToken (ConvertTo-SecureString -String $GH_TOKEN -AsPlainText -Force) -gitHubOrganization $gitHubOrganization -environment Development"
                            """
                        }
                    }
                    stage('Publish to production environment') {
                        sh """
                            pwsh -command "&./Deployment/Deploy.ps1 -ResourceGroup $resourceGroup -WebAppName $appName -ZipFilePath $zipName"
                        """
                    }
                }
            }
        }
    }
  }
