# Helm Chart for FplBot

This Helm chart deploys the FplBot Fantasy Premier League API application to Kubernetes.

## Prerequisites

- Kubernetes 1.19+
- Helm 3.2.0+

## Installing the Chart

To install the chart with the release name `fplbot`:

```bash
# Basic installation
helm install fplbot ./helm/fplbot

# Installation with custom values
helm install fplbot ./helm/fplbot --values ./helm/fplbot/values-prod.yaml

# Installation with API keys
helm install fplbot ./helm/fplbot \
  --set-string config.footballDataApiKey="your-football-data-api-key" \
  --set-string config.oddsApiKey="your-odds-api-key"
```

## Uninstalling the Chart

To uninstall the `fplbot` deployment:

```bash
helm uninstall fplbot
```

## Configuration

The following table lists the configurable parameters and their default values:

| Parameter | Description | Default |
|-----------|-------------|---------|
| `replicaCount` | Number of replicas | `1` |
| `image.repository` | Image repository | `fplbot` |
| `image.tag` | Image tag | `latest` |
| `image.pullPolicy` | Image pull policy | `IfNotPresent` |
| `service.type` | Service type | `ClusterIP` |
| `service.port` | Service port | `80` |
| `service.targetPort` | Container port | `80` |
| `ingress.enabled` | Enable ingress | `false` |
| `ingress.hosts[0].host` | Hostname | `fplbot.local` |
| `config.footballData.baseUrl` | Football Data API base URL | `""` |
| `config.footballData.apiToken` | Football Data API token | `""` |
| `config.oddsApi.baseUrl` | Odds API base URL | `""` |
| `config.oddsApi.apiToken` | Odds API token | `""` |
| `resources.limits.cpu` | CPU limit | `500m` |
| `resources.limits.memory` | Memory limit | `512Mi` |
| `resources.requests.cpu` | CPU request | `250m` |
| `resources.requests.memory` | Memory request | `256Mi` |
| `autoscaling.enabled` | Enable HPA | `false` |

## Examples

### Production Deployment with Ingress

```yaml
# values-prod.yaml
replicaCount: 3

image:
  repository: your-registry/fplbot
  tag: "v1.0.0"
  pullPolicy: Always

ingress:
  enabled: true
  className: "nginx"
  annotations:
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
  hosts:
    - host: api.fplbot.com
      paths:
        - path: /
          pathType: Prefix
  tls:
    - secretName: fplbot-tls
      hosts:
        - api.fplbot.com

autoscaling:
  enabled: true
  minReplicas: 3
  maxReplicas: 10
  targetCPUUtilizationPercentage: 70

resources:
  limits:
    cpu: 1000m
    memory: 1Gi
  requests:
    cpu: 500m
    memory: 512Mi
```

### Development Deployment

```yaml
# values-dev.yaml
replicaCount: 1

aspnetcore:
  environment: Development

service:
  type: NodePort
  nodePort: 30080

resources:
  limits:
    cpu: 200m
    memory: 256Mi
  requests:
    cpu: 100m
    memory: 128Mi
```

## Health Checks

The chart includes liveness and readiness probes:

- **Liveness Probe**: `/health` - Checks if the application is running
- **Readiness Probe**: `/health/ready` - Checks if the application is ready to serve traffic

## Secrets Management

API keys should be provided as secrets. You can either:

1. Set them via Helm values (not recommended for production):
```bash
helm install fplbot ./helm/fplbot \
  --set-string config.footballData.baseUrl="https://api.football-data.org/v4" \
  --set-string config.footballData.apiToken="your-token" \
  --set-string config.oddsApi.baseUrl="https://api.the-odds-api.com/v4" \
  --set-string config.oddsApi.apiToken="your-token"
```

2. Create secrets manually and reference them in custom values file (recommended for production).

## Monitoring

The application exposes metrics and health endpoints that can be monitored by Prometheus and other monitoring solutions.

## Troubleshooting

### Common Issues

1. **Image Pull Errors**: Ensure the image repository and tag are correct
2. **API Key Issues**: Verify that API keys are properly set as secrets
3. **Health Check Failures**: Check that the health endpoints are properly configured in your application

### Debugging

```bash
# Check pod status
kubectl get pods -l app.kubernetes.io/name=fplbot

# Check logs
kubectl logs -l app.kubernetes.io/name=fplbot

# Check events
kubectl get events --sort-by=.metadata.creationTimestamp
```