#!/bin/bash

# Deploy FplBot with fplbot.local domain
# This script sets up ingress and provides instructions for DNS

set -e

echo "🚀 Deploying FplBot at fplbot.local"
echo "===================================="

# Check if ingress controller exists
if ! kubectl get pods -n ingress-nginx | grep -q controller; then
    echo "📦 Installing Nginx Ingress Controller..."
    kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.8.2/deploy/static/provider/baremetal/deploy.yaml
    
    echo "⏳ Waiting for ingress controller to be ready..."
    kubectl wait --namespace ingress-nginx \
      --for=condition=ready pod \
      --selector=app.kubernetes.io/component=controller \
      --timeout=300s
fi

# Get API tokens
if [ -z "$FOOTBALL_DATA_API_TOKEN" ]; then
    read -p "🔑 Enter Football Data API Token: " FOOTBALL_DATA_API_TOKEN
fi

if [ -z "$ODDS_API_TOKEN" ]; then
    read -p "🔑 Enter Odds API Token: " ODDS_API_TOKEN
fi

# Deploy FplBot
echo "🚀 Deploying FplBot..."
helm upgrade --install fplbot ./helm/fplbot \
  --namespace fplbot \
  --create-namespace \
  --values ./values-local.yaml \
  --set-string config.footballData.apiToken="$FOOTBALL_DATA_API_TOKEN" \
  --set-string config.oddsApi.apiToken="$ODDS_API_TOKEN" \
  --wait

# Get ingress controller port
INGRESS_PORT=$(kubectl get svc ingress-nginx-controller -n ingress-nginx -o jsonpath='{.spec.ports[?(@.name=="http")].nodePort}')
SERVER_IP=$(hostname -I | awk '{print $1}')

echo ""
echo "✅ FplBot deployed successfully!"
echo ""
echo "🌐 Setup Instructions:"
echo "====================="
echo ""
echo "1. Add this line to your hosts file:"
echo "   $SERVER_IP fplbot.local"
echo ""
echo "   Windows: C:\\Windows\\System32\\drivers\\etc\\hosts"
echo "   Linux/Mac: /etc/hosts"
echo ""
echo "2. Access your application:"
echo "   http://fplbot.local:$INGRESS_PORT/health"
echo "   http://fplbot.local:$INGRESS_PORT/Prem/WinningTeams"
echo ""
echo "📊 Status:"
kubectl get pods -n fplbot
kubectl get svc -n fplbot
kubectl get ingress -n fplbot

echo ""
echo "🔧 Useful Commands:"
echo "  View logs: kubectl logs -f deployment/fplbot -n fplbot"
echo "  Delete:    helm uninstall fplbot -n fplbot"