# FplBot Kubernetes Deployment Guide

Deploy your FplBot application to any Kubernetes cluster using Helm.

## 🚀 Quick Deployment

### Prerequisites
- Kubernetes cluster access
- `kubectl` configured
- `helm` installed
- Your API tokens

### One-Command Deployment
```bash
# Make script executable
chmod +x deploy-k8s.sh

# Deploy to development
./deploy-k8s.sh --football-token "your-football-token" --odds-token "your-odds-token"

# Deploy to production
./deploy-k8s.sh --env prod --football-token "your-token" --odds-token "your-token"
```

## 📋 Manual Deployment Steps

### 1. Install Prerequisites

**Install kubectl:**
```bash
# Linux
curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
sudo install -o root -g root -m 0755 kubectl /usr/local/bin/kubectl

# macOS
brew install kubectl
```

**Install Helm:**
```bash
# Linux
curl https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash

# macOS
brew install helm
```

### 2. Configure Kubernetes Access
```bash
# Verify cluster access
kubectl cluster-info

# List available contexts
kubectl config get-contexts

# Switch context if needed
kubectl config use-context your-cluster-context
```

### 3. Deploy with Helm

**Development Deployment:**
```bash
# Create namespace
kubectl create namespace fplbot

# Install/upgrade
helm upgrade --install fplbot ./helm/fplbot \
  --namespace fplbot \
  --values ./helm/fplbot/values-dev.yaml \
  --set-string config.footballData.apiToken="your-football-token" \
  --set-string config.oddsApi.apiToken="your-odds-token" \
  --wait
```

**Production Deployment:**
```bash
helm upgrade --install fplbot ./helm/fplbot \
  --namespace fplbot \
  --values ./helm/fplbot/values-prod.yaml \
  --set-string config.footballData.apiToken="your-football-token" \
  --set-string config.oddsApi.apiToken="your-odds-token" \
  --wait
```

## 🌐 Accessing Your Application

### NodePort (Development)
```bash
# Get the NodePort
kubectl get svc fplbot -n fplbot

# Access via NodePort (typically 30080)
curl http://your-node-ip:30080/health
```

### Port Forward (Any Environment)
```bash
# Forward local port to service
kubectl port-forward svc/fplbot 8080:80 -n fplbot

# Access locally
curl http://localhost:8080/health
```

### Ingress (Production)
Configure your ingress controller and DNS to point to the service.

## 🔧 Management Commands

### View Status
```bash
# Helm releases
helm list -n fplbot

# Pod status
kubectl get pods -n fplbot

# Service status
kubectl get svc -n fplbot

# Deployment status
kubectl get deployment -n fplbot
```

### View Logs
```bash
# Application logs
kubectl logs -f deployment/fplbot -n fplbot

# Previous container logs
kubectl logs deployment/fplbot -n fplbot --previous
```

### Scale Application
```bash
# Scale to 3 replicas
kubectl scale deployment fplbot --replicas=3 -n fplbot

# Or update via Helm
helm upgrade fplbot ./helm/fplbot \
  --namespace fplbot \
  --reuse-values \
  --set replicaCount=3
```

### Update Configuration
```bash
# Update API tokens
helm upgrade fplbot ./helm/fplbot \
  --namespace fplbot \
  --reuse-values \
  --set-string config.footballData.apiToken="new-token"

# Update image tag
helm upgrade fplbot ./helm/fplbot \
  --namespace fplbot \
  --reuse-values \
  --set image.tag="commit-abc1234"
```

## 🛠️ Customization

### Custom Values File
Create a custom values file for your environment:

```yaml
# my-values.yaml
replicaCount: 2

image:
  tag: "latest"
  pullPolicy: Always

service:
  type: LoadBalancer

ingress:
  enabled: true
  hosts:
    - host: fplbot.yourdomain.com
      paths:
        - path: /
          pathType: Prefix

resources:
  limits:
    cpu: 500m
    memory: 512Mi
  requests:
    cpu: 250m
    memory: 256Mi
```

Then deploy with:
```bash
helm upgrade --install fplbot ./helm/fplbot \
  --namespace fplbot \
  --values my-values.yaml \
  --set-string config.footballData.apiToken="your-token" \
  --set-string config.oddsApi.apiToken="your-token"
```

### Environment-Specific Deployments

**Multiple Environments:**
```bash
# Development
helm upgrade --install fplbot-dev ./helm/fplbot \
  --namespace fplbot-dev \
  --values ./helm/fplbot/values-dev.yaml

# Staging  
helm upgrade --install fplbot-staging ./helm/fplbot \
  --namespace fplbot-staging \
  --values ./helm/fplbot/values.yaml

# Production
helm upgrade --install fplbot-prod ./helm/fplbot \
  --namespace fplbot-prod \
  --values ./helm/fplbot/values-prod.yaml
```

## 🔒 Security Best Practices

### Use Kubernetes Secrets
Instead of passing tokens via command line:

```bash
# Create secret
kubectl create secret generic fplbot-api-tokens \
  --from-literal=football-data-token="your-token" \
  --from-literal=odds-api-token="your-token" \
  -n fplbot

# Reference in values
# (The Helm chart already supports this)
```

### Network Policies
```yaml
# network-policy.yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: fplbot-netpol
  namespace: fplbot
spec:
  podSelector:
    matchLabels:
      app.kubernetes.io/name: fplbot
  policyTypes:
  - Ingress
  - Egress
  ingress:
  - from: []
    ports:
    - protocol: TCP
      port: 80
  egress:
  - {}
```

## 🐛 Troubleshooting

### Pod Not Starting
```bash
# Check pod status
kubectl describe pod -l app.kubernetes.io/name=fplbot -n fplbot

# Check events
kubectl get events -n fplbot --sort-by='.lastTimestamp'

# Check logs
kubectl logs deployment/fplbot -n fplbot
```

### Service Not Accessible
```bash
# Check service
kubectl describe svc fplbot -n fplbot

# Test from within cluster
kubectl run debug --image=curlimages/curl -it --rm -- sh
# Then: curl http://fplbot.fplbot.svc.cluster.local/health
```

### Image Pull Issues
```bash
# Check if image exists
docker pull ghcr.io/justinmarkmarshall/fplbot:latest

# Create image pull secret if needed
kubectl create secret docker-registry ghcr-secret \
  --docker-server=ghcr.io \
  --docker-username=your-github-username \
  --docker-password=your-github-token \
  -n fplbot
```

## 🔄 CI/CD Integration

### GitOps with ArgoCD
```yaml
# argocd-app.yaml
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: fplbot
  namespace: argocd
spec:
  project: default
  source:
    repoURL: https://github.com/justinmarkmarshall/FplBot
    path: helm/fplbot
    targetRevision: main
  destination:
    server: https://kubernetes.default.svc
    namespace: fplbot
  syncPolicy:
    automated:
      prune: true
      selfHeal: true
```

### Automated Updates
```bash
# Use Renovate or Dependabot to auto-update image tags
# Or create a simple update script:
#!/bin/bash
LATEST_TAG=$(curl -s https://api.github.com/repos/justinmarkmarshall/fplbot/packages/container/fplbot/versions | jq -r '.[0].metadata.container.tags[0]')
helm upgrade fplbot ./helm/fplbot --reuse-values --set image.tag="$LATEST_TAG" -n fplbot
```

Your FplBot is now ready for professional Kubernetes deployment! 🚀