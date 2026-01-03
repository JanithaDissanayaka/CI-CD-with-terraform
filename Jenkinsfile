pipeline {
    agent any

    stages {
        stage('Build') {
            steps {
                sh 'dotnet --version'
                sh 'dotnet restore'
                sh 'dotnet build --no-restore'
            }
        }

        stage('Test') {
            steps {
                sh 'dotnet test --no-build --no-restore --collect "XPlat Code Coverage"'
            }
        }

        stage('build image'){
            steps{
                sh 'docker build -t auctionsite .'
            }
        }

        stage('Deliver') {
            steps {
                sh 'dotnet publish SimpleWebApi --no-restore -o published'
            }
            post {
                success {
                    archiveArtifacts artifacts: 'published/**'
                }
            }
        }
    }
}
