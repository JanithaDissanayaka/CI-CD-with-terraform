pipeline {
    agent {
        docker {
            image 'mcr.microsoft.com/dotnet/sdk:8.0'
        }
    }

    stages {
        stage('Build') {
            steps {
                sh 'dotnet restore'
                sh 'dotnet build --no-restore'
            }
        }

        stage('Test') {
            steps {
                sh 'dotnet test --no-build --no-restore'
            }
        }

        stage('Deliver') {
            steps {
                sh 'dotnet publish "Project 1/Project 1.csproj" -c Release -o published'
            }
        }
    }
}
