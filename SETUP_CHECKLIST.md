# Post-GitHub Setup Checklist

After pushing your FplBot repository to GitHub, follow these steps to complete the automated CI/CD setup.

## ✅ Immediate Setup (Required)

### 1. Update Image Repository References
Replace `YOUR_GITHUB_USERNAME` with your actual GitHub username in these files:

**File: `helm/fplbot/values.yaml`**
```yaml
image:
  repository: ghcr.io/YOUR_ACTUAL_USERNAME/fplbot  # ← Change this
```

**File: `helm/fplbot/values-prod.yaml`**
```yaml
image:
  repository: ghcr.io/YOUR_ACTUAL_USERNAME/fplbot  # ← Change this
```

### 2. Enable GitHub Actions Permissions
1. Go to your repository on GitHub
2. Click **Settings** → **Actions** → **General**
3. Under **Workflow permissions**, select **Read and write permissions**
4. Click **Save**

### 3. Enable Package Registry
1. In repository **Settings** → **Actions** → **General**
2. Under **Fork pull request workflows**, check **Send write tokens to workflows from fork pull requests**
3. Click **Save**

## 🚀 First Build

### Push Your Code
```bash
git add .
git commit -m "Add GitHub Actions CI/CD pipeline"
git push origin main
```

### Watch the Build
1. Go to your repository → **Actions** tab
2. You'll see the workflow running
3. Wait for it to complete (usually 2-5 minutes)

### Verify the Image
1. Go to your repository → **Packages** tab (or **Code** tab → **Packages** on the right)
2. You should see `fplbot` package
3. Click on it to see all versions

## 🐳 Using Your Images

### Latest Image
```bash
docker pull ghcr.io/YOUR_USERNAME/fplbot:latest
```

### Specific Commit
```bash
docker pull ghcr.io/YOUR_USERNAME/fplbot:main-abc1234
```

### Deploy with Helm
```bash
helm install fplbot helm/fplbot \
  --set-string config.footballData.apiToken="your-token" \
  --set-string config.oddsApi.apiToken="your-token"
```

## 📝 Available Workflows

We've created 3 workflow options - you can delete the ones you don't need:

### `docker-simple.yml` (Recommended)
- ✅ Simple and reliable
- ✅ Builds on every commit
- ✅ Multi-platform images
- ✅ Good for most users

### `docker-build.yml` (Enhanced)
- ✅ All features of simple
- ✅ Semantic version tagging
- ✅ Build attestations
- ✅ Advanced tagging strategy

### `ci-cd.yml` (Full Pipeline)
- ✅ Everything above plus:
- ✅ Automated testing
- ✅ Security scanning
- ✅ Auto-update Helm values
- ✅ Code coverage

**Recommendation**: Start with `docker-simple.yml`, delete the others if you don't need them.

## 🔧 Customization

### Change Build Branches
Edit the workflow file:
```yaml
on:
  push:
    branches: [ main, develop, feature/* ]  # Add your branches
```

### Add Build Arguments
```yaml
- name: Build and push
  uses: docker/build-push-action@v5
  with:
    build-args: |
      BUILD_VERSION=${{ github.ref_name }}
      BUILD_DATE=${{ github.run_number }}
```

## 🚨 Troubleshooting

### Build Fails with Permission Error
1. Check **Settings** → **Actions** → **General** → **Workflow permissions**
2. Ensure **Read and write permissions** is selected

### Package Not Visible
1. The package may be private by default
2. Go to **Packages** → **Package settings** → **Change visibility**

### Docker Pull Fails
1. For private repositories, you need to authenticate:
```bash
echo $GITHUB_TOKEN | docker login ghcr.io -u USERNAME --password-stdin
```

## 🎉 Success!

Once setup is complete, every commit to `main` or `develop` will:
1. ✅ Automatically build your Docker image
2. ✅ Push it to GitHub Container Registry
3. ✅ Tag it with branch and commit info
4. ✅ Make it available for deployment

Your FplBot is now fully automated! 🚀