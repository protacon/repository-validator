library 'jenkins-ptcs-library@docker-depencies'

podTemplate(label: pod.label,
  containers: pod.templates + [
    containerTemplate(name: 'dotnet', image: 'microsoft/dotnet:2.1-sdk', ttyEnabled: true, command: '/bin/sh -c', args: 'cat')
    
  ]
) {
    def branch = (env.BRANCH_NAME)
    def resourceGroup = 'ptcs-github-validator'
    def appName = 'ptcs-github-validator'

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
            stage('Package') {
                sh """
                    zip -r $zipName ValidationLibrary.AzureFunctions
                """
            }
        }
    }
  }