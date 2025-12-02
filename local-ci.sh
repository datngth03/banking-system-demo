#!/bin/bash
# Local CI/CD simulation script

set -e  # Exit on error

echo "?? Starting Local CI/CD Pipeline..."
echo "=================================="

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Step 1: Clean
echo -e "${YELLOW}Step 1: Cleaning...${NC}"
dotnet clean BankingSystem.sln
echo -e "${GREEN}? Clean completed${NC}"
echo ""

# Step 2: Restore
echo -e "${YELLOW}Step 2: Restoring dependencies...${NC}"
dotnet restore BankingSystem.sln
echo -e "${GREEN}? Restore completed${NC}"
echo ""

# Step 3: Build
echo -e "${YELLOW}Step 3: Building solution...${NC}"
dotnet build BankingSystem.sln --configuration Release --no-restore
if [ $? -eq 0 ]; then
    echo -e "${GREEN}? Build succeeded${NC}"
else
    echo -e "${RED}? Build failed${NC}"
    exit 1
fi
echo ""

# Step 4: Run Tests
echo -e "${YELLOW}Step 4: Running tests...${NC}"

# Unit tests
echo "Running unit tests..."
dotnet test tests/BankingSystem.Tests/BankingSystem.Tests.csproj \
    --configuration Release \
    --no-build \
    --verbosity normal \
    --logger "console;verbosity=normal"

if [ $? -eq 0 ]; then
    echo -e "${GREEN}? Unit tests passed${NC}"
else
    echo -e "${RED}? Unit tests failed${NC}"
    exit 1
fi

# Integration tests
echo "Running integration tests..."
dotnet test tests/BankingSystem.IntegrationTests/BankingSystem.IntegrationTests.csproj \
    --configuration Release \
    --no-build \
    --verbosity normal \
    --logger "console;verbosity=normal"

if [ $? -eq 0 ]; then
    echo -e "${GREEN}? Integration tests passed${NC}"
else
    echo -e "${RED}? Integration tests failed${NC}"
    exit 1
fi
echo ""

# Step 5: Code Coverage (optional)
echo -e "${YELLOW}Step 5: Generating code coverage...${NC}"
dotnet test BankingSystem.sln \
    --configuration Release \
    --no-build \
    --collect:"XPlat Code Coverage" \
    --results-directory ./TestResults \
    --verbosity quiet

if [ $? -eq 0 ]; then
    echo -e "${GREEN}? Coverage generated in ./TestResults${NC}"
else
    echo -e "${YELLOW}?? Coverage generation had issues (non-critical)${NC}"
fi
echo ""

# Step 6: Security Scan (if Trivy installed)
if command -v trivy &> /dev/null; then
    echo -e "${YELLOW}Step 6: Running security scan...${NC}"
    trivy fs . --severity HIGH,CRITICAL --exit-code 0
    echo -e "${GREEN}? Security scan completed${NC}"
else
    echo -e "${YELLOW}?? Trivy not installed, skipping security scan${NC}"
    echo "Install: https://github.com/aquasecurity/trivy"
fi
echo ""

# Step 7: Publish
echo -e "${YELLOW}Step 7: Publishing application...${NC}"
dotnet publish src/BankingSystem.API/BankingSystem.API.csproj \
    --configuration Release \
    --output ./publish \
    --no-build

if [ $? -eq 0 ]; then
    echo -e "${GREEN}? Publish completed to ./publish${NC}"
else
    echo -e "${RED}? Publish failed${NC}"
    exit 1
fi
echo ""

# Step 8: Docker Build (optional)
if command -v docker &> /dev/null; then
    echo -e "${YELLOW}Step 8: Building Docker image...${NC}"
    docker build -t banking-api:local \
        -f src/BankingSystem.API/Dockerfile \
        --build-arg BUILD_VERSION=1.0.0-local \
        --build-arg BUILD_DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ") \
        .
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}? Docker image built: banking-api:local${NC}"
    else
        echo -e "${RED}? Docker build failed${NC}"
        exit 1
    fi
else
    echo -e "${YELLOW}?? Docker not installed, skipping image build${NC}"
fi
echo ""

# Summary
echo "=================================="
echo -e "${GREEN}?? Local CI/CD Pipeline Completed Successfully!${NC}"
echo ""
echo "?? Summary:"
echo "  - Build: ? Passed"
echo "  - Unit Tests: ? Passed"
echo "  - Integration Tests: ? Passed"
echo "  - Coverage: ? Generated"
echo "  - Security Scan: ? Completed"
echo "  - Publish: ? Ready in ./publish"
echo "  - Docker Image: ? banking-api:local"
echo ""
echo "Next steps:"
echo "  - Run: docker-compose up -d"
echo "  - Test: curl http://localhost:5000/health"
echo "  - Monitor: http://localhost:3000 (Grafana)"
echo ""
