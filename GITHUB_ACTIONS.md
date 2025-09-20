# GitHub Actions CI/CD Setup

This repository includes automated CI/CD pipelines that build and publish Docker images to GitHub Container Registry (GHCR) on every commit.

## 🚀 What Happens Automatically

### On Every Push to `main` or `develop`:
1. **Docker Build**: Creates multi-platform Docker image (linux/amd64, linux/arm64)
2. **Push to GHCR**: Publishes image to `ghcr.io/YOUR_USERNAME/fplbot`
3. **Tagging Strategy**:
   - `latest` - Latest main branch build
   - `main-abc1234` - Branch name + short commit SHA
   - `develop-def5678` - Branch name + short commit SHA

### On Pull Requests:
1. **Build Only**: Builds Docker image without pushing
2. **Validation**: Ensures Docker build works

### On Git Tags (v*):
1. **Semantic Versioning**: Creates version tags like `v1.0.0`, `1.0`, `1`

## 📦 Published Images

Your images will be available at:
```
ghcr.io/YOUR_USERNAME/fplbot:latest
ghcr.io/YOUR_USERNAME/fplbot:main-abc1234
```

## 🔧 Setup Instructions

### 1. Update Repository References
After pushing to GitHub, update the image repository in Helm values:

**In `helm/fplbot/values.yaml`:**
```yaml
image:
  repository: ghcr.io/YOUR_ACTUAL_USERNAME/fplbot  # Replace YOUR_ACTUAL_USERNAME
  tag: "latest"
```

**In `helm/fplbot/values-prod.yaml`:**
```yaml
image:
  repository: ghcr.io/YOUR_ACTUAL_USERNAME/fplbot  # Replace YOUR_ACTUAL_USERNAME
  tag: "latest"
```

### 2. Enable GitHub Container Registry
1. Go to your GitHub repository
2. Navigate to **Settings** → **Actions** → **General**
3. Under **Workflow permissions**, ensure **Read and write permissions** is selected
4. Go to **Settings** → **Developer settings** → **Personal access tokens**
5. Ensure your repository has package permissions

### 3. Make Repository Public (Optional)
For public images, make your repository public, or configure private registry access.

## 🏗️ Workflow Files

### `.github/workflows/docker-build.yml`
- **Purpose**: Simple Docker build and push
- **Triggers**: Push to main/develop, PRs to main, version tags
- **Output**: Multi-platform Docker images in GHCR

### `.github/workflows/ci-cd.yml` (Optional Advanced Pipeline)
- **Purpose**: Full CI/CD with testing and security scanning
- **Features**: 
  - .NET testing
  - Security vulnerability scanning
  - Container image scanning
  - Automatic Helm values updates

## 🐳 Using the Images

### Pull and Run Locally
```bash
# Pull latest image
docker pull ghcr.io/YOUR_USERNAME/fplbot:latest

# Run with environment variables
docker run -p 8080:8080 \
  -e FOOTBALL_DATA_API_TOKEN="your-token" \
  -e ODDS_API_TOKEN="your-token" \
  ghcr.io/YOUR_USERNAME/fplbot:latest
```

### Deploy with Helm
```bash
# Deploy using the published image
helm install fplbot helm/fplbot \
  --set image.tag="main-abc1234" \
  --set-string config.footballData.apiToken="your-token" \
  --set-string config.oddsApi.apiToken="your-token"
```

### Use Specific Versions
```bash
# Use a specific commit
docker pull ghcr.io/YOUR_USERNAME/fplbot:main-abc1234

# Use semantic version (if tagged)
docker pull ghcr.io/YOUR_USERNAME/fplbot:v1.0.0
```

## 🔍 Monitoring Builds

### View Build Status
- Go to your repository → **Actions** tab
- See all workflow runs and their status
- Click on runs to see detailed logs

### Check Published Images
- Go to your repository → **Packages** tab
- View all published container images
- See download statistics and vulnerability reports

## 🛠️ Customizing Workflows

### Change Build Triggers
Edit `.github/workflows/docker-build.yml`:
```yaml
on:
  push:
    branches: [ main, develop, feature/* ]  # Add more branches
    tags: [ 'v*', 'release-*' ]            # Add more tag patterns
```

### Add Build Arguments
```yaml
- name: Build and push Docker image
  uses: docker/build-push-action@v5
  with:
    build-args: |
      BUILD_VERSION=${{ github.sha }}
      BUILD_DATE=$(date -u +'%Y-%m-%dT%H:%M:%SZ')
```

### Matrix Builds (Multiple .NET Versions)
```yaml
strategy:
  matrix:
    dotnet-version: ['8.0.x', '9.0.x']
```

## 🔒 Security Features

- **Automatic vulnerability scanning** with Trivy
- **Build provenance attestation** for supply chain security
- **Multi-platform builds** for broader compatibility
- **Cache optimization** for faster builds

## 📚 Next Steps

1. **Push your code** to GitHub
2. **Update image repositories** in Helm values with your actual username
3. **Watch the Actions tab** for your first automated build
4. **Deploy using the published images**

Your FplBot will now automatically build and publish Docker images on every commit! 🎉