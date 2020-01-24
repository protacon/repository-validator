library 'jenkins-ptcs-library@2.3.0'

def isDependabot(branchName) { return branchName.toString().startsWith("dependabot/nuget") }
def isMaster(branchName) { return branchName == "master" }
def isTest(branchName) { return branchName == "test" }

podTemplate(label: pod.label,
  containers: pod.templates + [
    containerTemplate(name: 'dotnet', image: 'ptcos/multi-netcore-sdk:0.0.2', ttyEnabled: true, command: '/bin/sh -c', args: 'cat'),
    containerTemplate(name: 'powershell', image: 'azuresdk/azure-powershell-core:master', ttyEnabled: true, command: '/bin/sh -c', args: 'cat')
  ]
) {

    def branch = (env.BRANCH_NAME)
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
                    cd $functionsProject
                    dotnet publish -c Release -o $publishFolder --version-suffix ${env.BUILD_NUMBER}
                    cd ..
                """
            }
            stage('Test') {
                sh """
                    dotnet test
                """
            }
        }
        if (isTest(branch) || isMaster(branch) || isDependabot(branch)){
            container('powershell') {
                stage('Package') {
                    sh """
                        pwsh -command "Compress-Archive -DestinationPath $zipName -Path $functionsProject/$publishFolder/*"
                    """
                }

                if (isTest(branch) || isDependabot(branch)){
                    toAzureTestEnv {
                        def now = new Date().getTime()
                        def ciRg = 'repo-ci-' + now
                        def ciAppName = 'repo-ci-' + now

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
                                    pwsh -command "Publish-AzWebApp -ResourceGroupName $ciRg -Name $ciAppName -ArchivePath $zipName -Force"
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
                    toAzureEnv("PTCG_Azure_SP") {
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
                                pwsh -command "Publish-AzWebApp -ResourceGroupName $resourceGroup -Name $appName -ArchivePath $zipName -Force"
                                pwsh -command "&./Deployment/Configure-Alarms.ps1 -MonitoredWebAppResourceGroup $resourceGroup -MonitoredWebAppName $appName -AlertHandlingResourceGroup 'protacon-slack-alarm-service' -AlertSlackChannel 'hjni-testi'"
                            """
                        }
                        stage('Warmup and validate'){
                            sh """
                                pwsh -command "&./Testing/Test-Validation.ps1 -ResourceGroup $resourceGroup -WebAppName $appName"
                            """
                        }
                    }
                }
            }
        }
    }
}
