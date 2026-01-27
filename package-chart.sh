#!/bin/bash

# Package Helm Chart for Transfer
# This script packages your Helm chart for easy transfer to another machine

set -e

echo "📦 Packaging FplBot Helm Chart"
echo "=============================="

# Check if helm is available
if ! command -v helm &> /dev/null; then
    echo "❌ Helm is not installed"
    echo "Please install Helm: https://helm.sh/docs/intro/install/"
    exit 1
fi

# Create package directory
mkdir -p packages

# Package the chart
echo "📦 Packaging chart..."
helm package ./helm/fplbot -d packages

# Get the package name
PACKAGE_NAME=$(ls packages/fplbot-*.tgz | head -1)
echo "✅ Chart packaged: $PACKAGE_NAME"

# Create deployment script
cat > packages/deploy-packaged.sh << 'EOF'
#!/bin/bash

# Deploy FplBot from packaged Helm chart

set -e

CHART_PACKAGE=$(ls fplbot-*.tgz | head -1)

if [ -z "$CHART_PACKAGE" ]; then
    echo "❌ No FplBot chart package found"
    exit 1
fi

echo "🚀 Deploying FplBot from package: $CHART_PACKAGE"

# Prompt for API tokens
read -p "🔑 Enter Football Data API Token: " FOOTBALL_DATA_API_TOKEN
read -p "🔑 Enter Odds API Token: " ODDS_API_TOKEN

# Create namespace
kubectl create namespace fplbot --dry-run=client -o yaml | kubectl apply -f -

# Deploy
helm upgrade --install fplbot "./$CHART_PACKAGE" \
  --namespace fplbot \
  --create-namespace \
  --wait \
  --set-string config.footballData.baseUrl="https://api.football-data.org/v4/" \
  --set-string config.footballData.apiToken="$FOOTBALL_DATA_API_TOKEN" \
  --set-string config.oddsApi.baseUrl="https://api.the-odds-api.com/v4/sports/soccer_epl/odds" \
  --set-string config.oddsApi.apiToken="$ODDS_API_TOKEN"

echo "✅ FplBot deployed successfully!"

# Show status
kubectl get pods -n fplbot
kubectl get svc -n fplbot
EOF

chmod +x packages/deploy-packaged.sh

echo ""
echo "📋 Transfer Instructions:"
echo "========================"
echo "1. Copy the entire 'packages' folder to your Linux machine"
echo "2. On Linux machine, run:"
echo "   cd packages"
echo "   ./deploy-packaged.sh"
echo ""
echo "📂 Files to transfer:"
ls -la packages/