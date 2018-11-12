library 'jenkins-ptcs-library@docker-depencies'

podTemplate(label: pod.label,
  containers: pod.templates + [
    containerTemplate(name: 'dotnet', image: 'microsoft/dotnet:2.1-sdk', ttyEnabled: true, command: '/bin/sh -c', args: 'cat')
  ]
) {
    def branch = (env.BRANCH_NAME)

    node(pod.label) {
        stage('Checkout') {
            checkout_with_tags()
        }
        container('dotnet') {
            stage('Build') {
                sh """
                    dotnet build -c Release
                """
            }
        }
    }
  }