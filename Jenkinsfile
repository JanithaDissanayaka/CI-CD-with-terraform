pipeline {
    agent any

    options {
        skipStagesAfterUnstable()
    }

    environment {
        IMAGE = 'janithadissanayaka/learn:auctionsite'
    }

    stages {

        stage('Build & Test') {
            agent {
                docker {
                    image 'mcr.microsoft.com/dotnet/sdk:8.0'
                    args '-u root'
                }
            }
            steps {
                sh 'dotnet --version'
                sh 'dotnet restore'
                sh 'dotnet build --no-restore'
                sh 'dotnet test --no-build --no-restore'
                sh 'rm -rf published'
                sh 'dotnet publish "Project 1.csproj" --no-restore -o published'
            }
            post {
                success {
                    archiveArtifacts artifacts: 'published/**'
                }
            }
        }

        stage('Docker Build & Push') {
            agent {
                docker {
                    image 'docker:cli'
                    args '-u root -v /var/run/docker.sock:/var/run/docker.sock'
                }
            }
            steps {
                withCredentials([
                    usernamePassword(
                        credentialsId: 'docker-registry-creds',
                        usernameVariable: 'DOCKER_USER',
                        passwordVariable: 'DOCKER_PASS'
                    )
                ]) {
                    sh '''
                      echo "$DOCKER_PASS" | docker login -u "$DOCKER_USER" --password-stdin
                      docker build -t $IMAGE .
                      docker push $IMAGE
                    '''
                }
            }
        }

        stage('Provision Server') {
            agent {
                docker {
                    image 'hashicorp/terraform:1.6'
                    args '--entrypoint="" -u root'
                }
            }
            steps {
                withCredentials([
                    [$class: 'AmazonWebServicesCredentialsBinding',
                     credentialsId: 'AWS_CRED']
                ]) {
                    dir('Terraform') {
                        sh 'terraform init'
                        sh 'terraform apply --auto-approve'

                        script {
                            env.EC2_PUBLIC_IP = sh(
                                script: 'terraform output -raw ec2_public_ip',
                                returnStdout: true
                            ).trim()
                        }

                        echo "EC2 Public IP: ${env.EC2_PUBLIC_IP}"
                    }
                }
            }
        }

        stage('Deploy') {
    steps {
        withCredentials([
            usernamePassword(
                credentialsId: 'docker-registry-creds',
                usernameVariable: 'DOCKER_USER',
                passwordVariable: 'DOCKER_PASS'
            )
        ]) {
            script {
                sleep time: 60, unit: 'SECONDS'
                echo "Deploying Docker image to EC2"

                def ec2Instance = "ec2-user@${env.EC2_PUBLIC_IP}"

                sshagent(['ec2-user']) {

                    sh "scp -o StrictHostKeyChecking=no server-cmds.sh ${ec2Instance}:/home/ec2-user"
                    sh "scp -o StrictHostKeyChecking=no docker-compose.yaml ${ec2Instance}:/home/ec2-user"

                    sh """
                        ssh -o StrictHostKeyChecking=no ${ec2Instance} '
                        cd /home/ec2-user &&
                        chmod +x server-cmds.sh &&
                        ./server-cmds.sh ${IMAGE} ${DOCKER_USER} ${DOCKER_PASS}
                        '
                        """

                         }
                    }
                 }       
            }
        }

    }
}
