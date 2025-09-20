# FplBot Helm Deployment Script for PowerShell
# Usage: .\deploy.ps1 [environment] [namespace] [release-name]

param(
    [Parameter(Position=0)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment = "dev",
    
    [Parameter(Position=1)]
    [string]$Namespace = "fplbot",
    
    [Parameter(Position=2)]
    [string]$ReleaseName = "fplbot"
)

$ChartPath = ".\helm\fplbot"
$ErrorActionPreference = "Stop"

# Function to print colored output
function Write-Status {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

try {
    Write-Status "Deploying FplBot to $Environment environment"
    Write-Status "Namespace: $Namespace"
    Write-Status "Release: $ReleaseName"

    # Check if Helm is installed
    if (-not (Get-Command helm -ErrorAction SilentlyContinue)) {
        Write-Error "Helm is not installed. Please install Helm first."
        exit 1
    }

    # Check if kubectl is configured
    try {
        kubectl cluster-info | Out-Null
    }
    catch {
        Write-Error "kubectl is not configured or cluster is not accessible."
        exit 1
    }

    # Create namespace if it doesn't exist
    Write-Status "Creating namespace if it doesn't exist..."
    kubectl create namespace $Namespace --dry-run=client -o yaml | kubectl apply -f -

    # Determine values file
    $ValuesFile = "$ChartPath\values-$Environment.yaml"
    if (-not (Test-Path $ValuesFile)) {
        Write-Warning "Values file $ValuesFile not found, using default values.yaml"
        $ValuesFile = "$ChartPath\values.yaml"
    }

    # Check if this is an upgrade or install
    $ExistingRelease = helm list -n $Namespace | Select-String $ReleaseName
    if ($ExistingRelease) {
        $Action = "upgrade"
        Write-Status "Upgrading existing release..."
    }
    else {
        $Action = "install"
        Write-Status "Installing new release..."
    }

    # Prompt for API keys if in production
    $FootballDataApiKey = $env:FOOTBALL_DATA_API_KEY
    $OddsApiKey = $env:ODDS_API_KEY

    # Prompt for API configuration
    $FootballDataApiToken = $env:FOOTBALL_DATA_API_TOKEN
    $OddsApiToken = $env:ODDS_API_TOKEN
    
    if ($Environment -eq "prod") {
        if (-not $FootballDataApiToken) {
            Write-Warning "FOOTBALL_DATA_API_TOKEN environment variable not set."
            $secureToken = Read-Host "Enter Football Data API Token" -AsSecureString
            $FootballDataApiToken = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureToken))
        }
        
        if (-not $OddsApiToken) {
            Write-Warning "ODDS_API_TOKEN environment variable not set."
            $secureToken = Read-Host "Enter Odds API Token" -AsSecureString
            $OddsApiToken = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureToken))
        }
    }

    # Build Helm command arguments
    $HelmArgs = @(
        $Action
        $ReleaseName
        $ChartPath
        "--namespace", $Namespace
        "--values", $ValuesFile
        "--wait"
        "--timeout", "5m"
    )

    # Add API configuration if provided
    if ($FootballDataApiToken) {
        $HelmArgs += "--set-string", "config.footballData.baseUrl=https://api.football-data.org/v4/"
        $HelmArgs += "--set-string", "config.footballData.apiToken=$FootballDataApiToken"
    }

    if ($OddsApiToken) {
        $HelmArgs += "--set-string", "config.oddsApi.baseUrl=https://api.the-odds-api.com/v4/sports/soccer_epl/odds"
        $HelmArgs += "--set-string", "config.oddsApi.apiToken=$OddsApiToken"
    }

    # Execute deployment
    Write-Status "Running: helm $($HelmArgs -join ' ')"
    & helm $HelmArgs

    # Verify deployment
    Write-Status "Verifying deployment..."
    kubectl rollout status "deployment/$ReleaseName" -n $Namespace --timeout=300s

    # Show deployment info
    Write-Status "Deployment completed successfully!"
    Write-Host ""
    Write-Status "Release Information:"
    helm list -n $Namespace | Select-String $ReleaseName

    Write-Host ""
    Write-Status "Pod Status:"
    kubectl get pods -n $Namespace -l "app.kubernetes.io/instance=$ReleaseName"

    Write-Host ""
    Write-Status "Service Information:"
    kubectl get svc -n $Namespace -l "app.kubernetes.io/instance=$ReleaseName"

    # Show access information
    if ($Environment -eq "dev") {
        $NodePort = kubectl get svc $ReleaseName -n $Namespace -o jsonpath='{.spec.ports[0].nodePort}'
        if ($NodePort) {
            Write-Status "Development access: http://localhost:$NodePort"
        }
    }

    Write-Status "Deployment script completed!"
}
catch {
    Write-Error "Deployment failed: $($_.Exception.Message)"
    exit 1
}