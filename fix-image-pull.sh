#!/bin/bash

# FplBot Image Pull Fix Script

echo "🔍 Diagnosing ImagePullBackOff issue..."

# Check if image exists and is accessible
echo "📥 Testing image pull..."
if docker pull ghcr.io/justinmarkmarshall/fplbot:latest; then
    echo "✅ Image pull successful - image is public"
    echo "🔄 The issue might be with Kubernetes image pull policy"
    
    # Try redeploying with Always pull policy
    echo "🚀 Redeploying with imagePullPolicy: Always..."
    helm upgrade fplbot ./helm/fplbot \
      --namespace fplbot \
      --reuse-values \
      --set image.pullPolicy=Always
      
else
    echo "❌ Image pull failed - image is likely private"
    echo ""
    echo "💡 Solutions:"
    echo "1. Make the GitHub package public:"
    echo "   - Go to https://github.com/Justinmarkmarshall/FplBot"
    echo "   - Click 'Packages' tab"
    echo "   - Click 'fplbot' package"
    echo "   - Package settings → Change visibility → Make public"
    echo ""
    echo "2. Or create image pull secret:"
    echo "   kubectl create secret docker-registry ghcr-secret \\"
    echo "     --docker-server=ghcr.io \\"
    echo "     --docker-username=Justinmarkmarshall \\"
    echo "     --docker-password=YOUR_GITHUB_TOKEN \\"
    echo "     --namespace fplbot"
    echo ""
    echo "   Then redeploy with:"
    echo "   helm upgrade fplbot ./helm/fplbot \\"
    echo "     --namespace fplbot \\"
    echo "     --reuse-values \\"
    echo "     --set imagePullSecrets[0].name=ghcr-secret"
fi

echo ""
echo "🔍 Current pod status:"
kubectl get pods -n fplbot

echo ""
echo "📋 Recent events:"
kubectl get events -n fplbot --sort-by='.lastTimestamp' | tail -10