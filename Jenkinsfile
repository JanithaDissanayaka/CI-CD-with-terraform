pipeline {
    agent any
    
    tools {
        dotnetsdk 'dotnet-8'
    }
    stages {
        stage('Build') {
            steps {
                sh 'echo "Build Stage"'
                sh 'dotnet restore'
                sh 'dotnet build --no-restore'
            }
        }
        stage('Test') { 
            steps {
                sh 'echo "Test Stage"'
                sh 'dotnet test --no-build --no-restore --collect "XPlat Code Coverage"' 
            }
            post {
                always {
                    recordCoverage(tools: [[parser: 'COBERTURA', pattern: '**/*.xml']], sourceDirectories: [[path: 'SimpleWebApi.Test/TestResults']])  
                }
            }
        }
    }
}