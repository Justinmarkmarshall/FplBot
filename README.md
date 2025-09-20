# FplBot

A .NET 9 ASP.NET Core web API for Fantasy Premier League data, integrating with Football Data API and The Odds API.

## 🚀 Quick Start

### Prerequisites
- .NET 9 SDK
- Docker (optional)
- Kubernetes cluster (optional)
- API tokens for:
  - [Football Data API](https://www.football-data.org/client/register)
  - [The Odds API](https://the-odds-api.com/)

### Local Development

1. **Clone and setup:**
   ```bash
   git clone <your-repo-url>
   cd FplBot
   cp FplBot/appsettings.example.json FplBot/appsettings.Development.json
   ```

2. **Configure API keys:**
   Edit `FplBot/appsettings.Development.json` with your API tokens:
   ```json
   {
     "FootballDataClient": {
       "BaseUrl": "https://api.football-data.org/v4/",
       "ApiToken": "your-football-data-token"
     },
     "OddsClient": {
       "BaseUrl": "https://api.the-odds-api.com/v4/sports/soccer_epl/odds",
       "ApiToken": "your-odds-api-token"
     }
   }
   ```

3. **Run the application:**
   ```bash
   cd FplBot
   dotnet run
   ```

4. **Test the API:**
   - Health check: http://localhost:5000/health
   - Premier League teams: http://localhost:5000/Prem/WinningTeams

### Docker Deployment

#### Using Pre-built Images (Recommended)
```bash
# Pull from GitHub Container Registry (automatically built on every commit)
docker pull ghcr.io/justinmarkmarshall/fplbot:latest

# Run container
docker run -p 8080:8080 \
  -e FOOTBALL_DATA_API_TOKEN="your-football-token" \
  -e ODDS_API_TOKEN="your-odds-token" \
  ghcr.io/justinmarkmarshall/fplbot:latest
```

#### Building Locally
```bash
# Build image locally
docker build -t fplbot:latest .

# Run container
docker run -p 8080:8080 \
  -e FOOTBALL_DATA_API_TOKEN="your-football-token" \
  -e ODDS_API_TOKEN="your-odds-token" \
  fplbot:latest
```

### Kubernetes Deployment

See the comprehensive [Helm chart documentation](helm/fplbot/README.md) for Kubernetes deployment.

**Quick deploy:**
```bash
# Using environment variables
helm install fplbot helm/fplbot \
  --set-string config.footballData.apiToken="your-football-token" \
  --set-string config.oddsApi.apiToken="your-odds-token"
```

## 📁 Project Structure

```
FplBot/
├── Controllers/         # API controllers
├── Services/           # Business logic
├── Clients/            # External API clients
├── Model/              # Data models
├── Configuration/      # App configuration
├── Utilities/          # Helper utilities
└── Properties/         # Launch settings

FplBot.UnitTests/       # Unit tests
helm/fplbot/           # Kubernetes Helm chart
.github/workflows/     # GitHub Actions CI/CD
```

## 🚀 Automated CI/CD

Every commit automatically:
- ✅ Builds multi-platform Docker images
- ✅ Publishes to GitHub Container Registry (GHCR)
- ✅ Tags with branch and commit info
- ✅ Runs security scans

See [GITHUB_ACTIONS.md](GITHUB_ACTIONS.md) for setup details.

## 🔒 Security

This repository is configured for safe public hosting:
- ✅ No API tokens in source code
- ✅ Environment variable configuration
- ✅ Kubernetes secrets integration
- ✅ Comprehensive .gitignore
- ✅ Automated security scanning

See [GITHUB_SECURITY.md](GITHUB_SECURITY.md) for detailed security information.

## 🧪 Testing

```bash
# Run unit tests
dotnet test

# Run specific test project
dotnet test FplBot.UnitTests/
```

## 📊 API Endpoints

- `GET /health` - Health check endpoint
- `GET /health/ready` - Readiness probe
- `GET /Prem/WinningTeams` - Get Premier League winning team predictions

## 🛠️ Development

### Adding New Features

1. Create feature branch
2. Add tests in `FplBot.UnitTests/`
3. Implement feature
4. Update documentation
5. Submit pull request

### Configuration

The application uses a layered configuration approach:
1. `appsettings.json` - Base configuration
2. `appsettings.Development.json` - Development overrides
3. Environment variables - Runtime overrides (Kubernetes/Docker)

## 📦 Dependencies

- ASP.NET Core 9.0
- Microsoft.Extensions.Http
- Microsoft.AspNetCore.OpenApi
- Scalar.AspNetCore

## 🚢 Deployment Environments

- **Development**: Local with `appsettings.Development.json`
- **Docker**: Container with environment variables
- **Kubernetes**: Helm chart with secrets management

## 📝 License

[Add your license here]

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## 📞 Support

For issues and questions:
- Create an issue in this repository
- Check the [security documentation](GITHUB_SECURITY.md)
- Review the [Helm chart README](helm/fplbot/README.md)