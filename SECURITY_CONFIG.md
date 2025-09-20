# Example: Deploying FplBot with API Secrets

## 🔐 **Security Configuration Complete!**

Your FplBot application is now configured to use Kubernetes secrets for API configuration. Here's how the security works:

### **What Changed:**

1. **Application Configuration** (`Program.cs`):
   - Reads from environment variables for sensitive data
   - Falls back to appsettings.json for defaults
   - Environment variables override configuration files

2. **Kubernetes Secrets** (`secret.yaml`):
   - Stores API tokens and base URLs as base64-encoded secrets
   - Mounted as environment variables in the container
   - Follows Kubernetes security best practices

3. **Deployment Templates** (`deployment.yaml`):
   - Injects secret values as environment variables
   - Uses `secretKeyRef` to reference Kubernetes secrets
   - Maintains separation of config and secrets

### **Environment Variables Used:**
```
FOOTBALL_DATA_BASE_URL    - Football Data API base URL
FOOTBALL_DATA_API_TOKEN   - Football Data API token
ODDS_API_BASE_URL         - Odds API base URL  
ODDS_API_TOKEN           - Odds API token
```

### **Deployment Examples:**

#### **Development (without API keys):**
```powershell
.\deploy.ps1 dev
# Access: http://localhost:30080
```

#### **Production (with API keys):**
```powershell
# Option 1: Via deployment script (will prompt for keys)
.\deploy.ps1 prod

# Option 2: Via environment variables
$env:FOOTBALL_DATA_API_TOKEN = "your-football-data-token"
$env:ODDS_API_TOKEN = "your-odds-api-token"
.\deploy.ps1 prod

# Option 3: Direct Helm command
helm upgrade fplbot .\helm\fplbot `
  --namespace fplbot `
  --values .\helm\fplbot\values-prod.yaml `
  --set-string config.footballData.baseUrl="https://api.football-data.org/v4" `
  --set-string config.footballData.apiToken="your-football-token" `
  --set-string config.oddsApi.baseUrl="https://api.the-odds-api.com/v4" `
  --set-string config.oddsApi.apiToken="your-odds-token"
```

### **Verify Secret Configuration:**
```powershell
# Check if secrets are created
kubectl get secrets -n fplbot

# View secret keys (values are base64 encoded)
kubectl describe secret fplbot-secrets -n fplbot

# Check environment variables in pod
kubectl exec -n fplbot deployment/fplbot -- env | findstr API
```

### **Security Benefits:**

✅ **API tokens stored as Kubernetes secrets**  
✅ **Base64 encoded for basic obfuscation**  
✅ **Environment variable injection**  
✅ **No sensitive data in configuration files**  
✅ **Separate secrets per environment**  
✅ **Easy rotation via Helm upgrades**

### **Current Status:**
- ✅ Health checks working (`/health`, `/health/ready`)
- ✅ Application deployed and running
- ✅ Security configuration complete
- ✅ Ready for API token configuration

Your FplBot is now production-ready with proper secret management! 🚀

### Deployment
for local deploynment, use 
helm install fplbot .\helm\fplbot --namespace fplbot --values .\helm\fplbot\values-dev.yaml

or the deployment script .\deploy.ps1 dev