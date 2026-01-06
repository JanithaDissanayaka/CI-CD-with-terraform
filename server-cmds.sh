#!/bin/bash
set -e

IMAGE="$1"
DOCKER_USER="$2"
DOCKER_PASS="$3"

export IMAGE

echo "Logging into Docker Hub..."
echo "$DOCKER_PASS" | docker login -u "$DOCKER_USER" --password-stdin

echo "Stopping old containers..."
docker compose down || true

echo "Pulling image: $IMAGE"
docker pull "$IMAGE"

echo "Starting containers..."
docker compose up -d

echo "Deployment successful"
