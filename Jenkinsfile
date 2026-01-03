pipeline {
    agent any

    options {
        skipStagesAfterUnstable()
    }

    stages {

        stage('Build & Test (.NET)') {
            agent {
                docker {
                    image 'mcr.microsoft.com/dotnet/sdk:8.0'
                    reuseNode true
                }
            }
            steps {
                sh 'dotnet --version'
                sh 'dotnet restore'
                sh 'dotnet build --no-restore'
                sh 'dotnet test --no-build --no-restore --collect "XPlat Code Coverage"'
            }
        }

        stage('Build Docker Image') {
            steps {
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
