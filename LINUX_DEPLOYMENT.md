# FplBot Linux Deployment

Deploy your FplBot application on any Linux machine using Docker.

## 🚀 Quick Start

### 1. Copy Files to Linux Machine
Transfer these files to your Linux server:
- `docker-compose.yml`
- `.env.example`
- `deploy-linux.sh`

### 2. Setup Environment
```bash
# Copy and edit environment file
cp .env.example .env
nano .env  # Add your actual API tokens
```

### 3. Make Script Executable
```bash
chmod +x deploy-linux.sh
```

### 4. Deploy
```bash
./deploy-linux.sh
```

## 📋 Manual Deployment

If you prefer manual steps:

### 1. Install Docker (if not installed)
```bash
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker $USER
# Log out and back in
```

### 2. Install Docker Compose (if not installed)
```bash
sudo curl -L "https://github.com/docker/compose/releases/download/v2.20.0/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose
```

### 3. Create Environment File
```bash
# Create .env file with your API tokens
cat > .env << EOF
FOOTBALL_DATA_API_TOKEN=your-actual-football-data-token
ODDS_API_TOKEN=your-actual-odds-api-token
EOF
```

### 4. Deploy with Docker Compose
```bash
# Pull and start
docker-compose pull
docker-compose up -d

# Check status
docker-compose ps
docker-compose logs -f
```

## 🌐 Access Your Application

Once deployed, your FplBot will be available at:
- **Health Check**: http://your-server-ip:8080/health
- **API Endpoint**: http://your-server-ip:8080/Prem/WinningTeams

## 🔧 Management Commands

```bash
# View logs
docker-compose logs -f

# Restart service
docker-compose restart

# Stop service
docker-compose down

# Update to latest image
docker-compose pull
docker-compose up -d

# View container status
docker-compose ps
```

## 🔒 Security Considerations

### Firewall
```bash
# Allow HTTP traffic (Ubuntu/Debian)
sudo ufw allow 8080

# Or use specific IP range
sudo ufw allow from 192.168.1.0/24 to any port 8080
```

### Reverse Proxy (Optional)
For production, consider using Nginx:

```nginx
server {
    listen 80;
    server_name your-domain.com;
    
    location / {
        proxy_pass http://localhost:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

## 📊 Monitoring

### Health Check
```bash
curl http://localhost:8080/health
```

### API Test
```bash
curl http://localhost:8080/Prem/WinningTeams | jq
```

### Container Stats
```bash
docker stats fplbot
```

## 🔄 Auto-Updates

Create a cron job for automatic updates:
```bash
# Edit crontab
crontab -e

# Add this line to update daily at 2 AM
0 2 * * * cd /path/to/fplbot && docker-compose pull && docker-compose up -d
```

## 🐛 Troubleshooting

### Container Won't Start
```bash
# Check logs
docker-compose logs

# Check environment variables
docker-compose exec fplbot env
```

### API Not Responding
```bash
# Check if ports are open
netstat -tlnp | grep 8080

# Test health endpoint
curl -v http://localhost:8080/health
```

### Image Pull Issues
```bash
# Login to GHCR (if private)
docker login ghcr.io

# Manual pull
docker pull ghcr.io/justinmarkmarshall/fplbot:latest
```