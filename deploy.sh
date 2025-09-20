#!/bin/bash

# FplBot Helm Deployment Script
# Usage: ./deploy.sh [environment] [namespace] [release-name]

set -e

# Default values
ENVIRONMENT=${1:-dev}
NAMESPACE=${2:-fplbot}
RELEASE_NAME=${3:-fplbot}
CHART_PATH="./helm/fplbot"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Validate inputs
if [[ ! "$ENVIRONMENT" =~ ^(dev|staging|prod)$ ]]; then
    print_error "Invalid environment. Use: dev, staging, or prod"
    exit 1
fi

print_status "Deploying FplBot to $ENVIRONMENT environment"
print_status "Namespace: $NAMESPACE"
print_status "Release: $RELEASE_NAME"

# Check if Helm is installed
if ! command -v helm &> /dev/null; then
    print_error "Helm is not installed. Please install Helm first."
    exit 1
fi

# Check if kubectl is configured
if ! kubectl cluster-info &> /dev/null; then
    print_error "kubectl is not configured or cluster is not accessible."
    exit 1
fi

# Create namespace if it doesn't exist
print_status "Creating namespace if it doesn't exist..."
kubectl create namespace "$NAMESPACE" --dry-run=client -o yaml | kubectl apply -f -

# Determine values file
VALUES_FILE="$CHART_PATH/values-$ENVIRONMENT.yaml"
if [[ ! -f "$VALUES_FILE" ]]; then
    print_warning "Values file $VALUES_FILE not found, using default values.yaml"
    VALUES_FILE="$CHART_PATH/values.yaml"
fi

# Check if this is an upgrade or install
if helm list -n "$NAMESPACE" | grep -q "$RELEASE_NAME"; then
    ACTION="upgrade"
    print_status "Upgrading existing release..."
else
    ACTION="install"
    print_status "Installing new release..."
fi

# Prompt for API keys if in production
if [[ "$ENVIRONMENT" == "prod" ]]; then
    if [[ -z "$FOOTBALL_DATA_API_KEY" ]]; then
        print_warning "FOOTBALL_DATA_API_KEY environment variable not set."
        read -s -p "Enter Football Data API Key: " FOOTBALL_DATA_API_KEY
        echo
    fi
    
    if [[ -z "$ODDS_API_KEY" ]]; then
        print_warning "ODDS_API_KEY environment variable not set."
        read -s -p "Enter Odds API Key: " ODDS_API_KEY
        echo
    fi
fi

# Build Helm command
HELM_CMD="helm $ACTION $RELEASE_NAME $CHART_PATH"
HELM_CMD="$HELM_CMD --namespace $NAMESPACE"
HELM_CMD="$HELM_CMD --values $VALUES_FILE"
HELM_CMD="$HELM_CMD --wait --timeout=5m"

# Add API keys if provided
if [[ -n "$FOOTBALL_DATA_API_KEY" ]]; then
    HELM_CMD="$HELM_CMD --set-string config.footballDataApiKey=$FOOTBALL_DATA_API_KEY"
fi

if [[ -n "$ODDS_API_KEY" ]]; then
    HELM_CMD="$HELM_CMD --set-string config.oddsApiKey=$ODDS_API_KEY"
fi

# Execute deployment
print_status "Running: $HELM_CMD"
eval "$HELM_CMD"

# Verify deployment
print_status "Verifying deployment..."
kubectl rollout status deployment/$RELEASE_NAME -n "$NAMESPACE" --timeout=300s

# Show deployment info
print_status "Deployment completed successfully!"
echo
print_status "Release Information:"
helm list -n "$NAMESPACE" | grep "$RELEASE_NAME"

echo
print_status "Pod Status:"
kubectl get pods -n "$NAMESPACE" -l app.kubernetes.io/instance="$RELEASE_NAME"

echo
print_status "Service Information:"
kubectl get svc -n "$NAMESPACE" -l app.kubernetes.io/instance="$RELEASE_NAME"

# Show access information
if [[ "$ENVIRONMENT" == "dev" ]]; then
    NODE_PORT=$(kubectl get svc "$RELEASE_NAME" -n "$NAMESPACE" -o jsonpath='{.spec.ports[0].nodePort}')
    if [[ -n "$NODE_PORT" ]]; then
        print_status "Development access: http://localhost:$NODE_PORT"
    fi
elif helm get values "$RELEASE_NAME" -n "$NAMESPACE" | grep -q "enabled: true" && helm get values "$RELEASE_NAME" -n "$NAMESPACE" | grep -A5 ingress | grep -q "host:"; then
    INGRESS_HOST=$(helm get values "$RELEASE_NAME" -n "$NAMESPACE" | grep -A10 ingress | grep "host:" | head -1 | awk '{print $3}')
    print_status "Production access: https://$INGRESS_HOST"
fi

print_status "Deployment script completed!"