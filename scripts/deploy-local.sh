#!/bin/bash

# ===================================
# Build and Deploy to Local K8s
# Usage: ./deploy-local.sh
# ===================================

set -e

echo "?? Building and Deploying Banking System to Local Kubernetes..."

# Configuration
IMAGE_NAME="banking-system-api"
IMAGE_TAG="local-$(date +%Y%m%d-%H%M%S)"
NAMESPACE="banking-system"
DOCKER_REGISTRY="localhost:5000"  # For local registry

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Step 1: Build Docker Image
echo -e "${BLUE}?? Step 1: Building Docker image...${NC}"
cd "$(dirname "$0")/.."

docker build \
  -f src/BankingSystem.API/Dockerfile \
  -t ${IMAGE_NAME}:${IMAGE_TAG} \
  -t ${IMAGE_NAME}:latest \
  --build-arg BUILD_VERSION=${IMAGE_TAG} \
  --build-arg BUILD_DATE=$(date -u +'%Y-%m-%dT%H:%M:%SZ') \
  .

echo -e "${GREEN}? Docker image built successfully${NC}"

# Step 2: Load image to Minikube/Kind (if using)
echo -e "${BLUE}?? Step 2: Loading image to Kubernetes...${NC}"

# Detect if using Minikube
if command -v minikube &> /dev/null && minikube status &> /dev/null; then
    echo "Using Minikube..."
    minikube image load ${IMAGE_NAME}:latest
# Detect if using Kind
elif command -v kind &> /dev/null; then
    CLUSTER_NAME=$(kind get clusters | head -n 1)
    if [ ! -z "$CLUSTER_NAME" ]; then
        echo "Using Kind cluster: $CLUSTER_NAME"
        kind load docker-image ${IMAGE_NAME}:latest --name $CLUSTER_NAME
    fi
# Use Docker Desktop Kubernetes
else
    echo "Using Docker Desktop Kubernetes (image already available)"
fi

echo -e "${GREEN}? Image loaded to Kubernetes${NC}"

# Step 3: Create Namespace
echo -e "${BLUE}???  Step 3: Creating namespace...${NC}"
kubectl create namespace ${NAMESPACE} --dry-run=client -o yaml | kubectl apply -f -

# Step 4: Update secrets
echo -e "${BLUE}?? Step 4: Applying secrets and configs...${NC}"
kubectl apply -f k8s/postgres.yml

# Wait for PostgreSQL to be ready
echo -e "${YELLOW}? Waiting for PostgreSQL to be ready...${NC}"
kubectl wait --for=condition=ready pod -l app=postgres -n ${NAMESPACE} --timeout=180s
kubectl wait --for=condition=ready pod -l app=postgres-hangfire -n ${NAMESPACE} --timeout=180s

echo -e "${GREEN}? PostgreSQL is ready${NC}"

# Step 5: Update deployment with new image tag
echo -e "${BLUE}?? Step 5: Updating deployment.yml with image...${NC}"
cat k8s/deployment.yml | \
  sed "s|image: your-docker-registry/banking-system-api:latest|image: ${IMAGE_NAME}:latest|g" | \
  sed "s|imagePullPolicy: Always|imagePullPolicy: IfNotPresent|g" | \
  sed "s|replicas: 3|replicas: 2|g" | \
  kubectl apply -f -

echo -e "${GREEN}? Deployment applied${NC}"

# Step 6: Wait for deployment
echo -e "${YELLOW}? Waiting for API deployment to be ready...${NC}"
kubectl rollout status deployment/banking-api -n ${NAMESPACE} --timeout=300s

# Step 7: Display status
echo -e "${BLUE}?? Step 7: Deployment Status${NC}"
kubectl get all -n ${NAMESPACE}

# Step 8: Port forward for local access
echo ""
echo -e "${GREEN}? Deployment completed!${NC}"
echo ""
echo -e "${YELLOW}?? Access the application:${NC}"
echo ""
echo "Run one of these commands to access the API:"
echo ""
echo "  ${BLUE}# Port forward to localhost:8080${NC}"
echo "  kubectl port-forward -n ${NAMESPACE} svc/banking-api-service 8080:80"
echo ""
echo "  ${BLUE}# Or get Minikube service URL${NC}"
echo "  minikube service banking-api-service -n ${NAMESPACE}"
echo ""
echo "  ${BLUE}# Check logs${NC}"
echo "  kubectl logs -n ${NAMESPACE} -l app=banking-api --tail=100 -f"
echo ""
echo "  ${BLUE}# Access Swagger UI (after port-forward)${NC}"
echo "  http://localhost:8080/swagger"
echo ""
echo "  ${BLUE}# Access Hangfire Dashboard (after port-forward)${NC}"
echo "  http://localhost:8080/hangfire"
echo "  Username: admin"
echo "  Password: HangfireAdmin@2024"
echo ""
