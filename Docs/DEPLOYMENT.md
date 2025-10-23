# Production Deployment

This guide covers deploying QRStickers to Azure with SQL Database.

## Deployment Options

QRStickers supports two Azure deployment strategies:

1. **Azure Web Apps** (Windows) - Recommended for simplicity
2. **Azure Container Apps** (Linux) - Recommended for containerized workloads

Both options support Azure SQL Database with managed identity authentication.

## Prerequisites

- Azure subscription with active credits
- Azure CLI installed (optional, can use Portal)
- .NET 9.0 SDK (for migrations)
- Git (for GitHub Actions CI/CD)

---

## Option 1: Azure Service Connector (Recommended - Passwordless)

Azure Service Connector automates the connection between your Web App and Azure SQL Database using **managed identity** (no passwords needed).

### Step 1: Create Azure SQL Database

1. Navigate to **Azure Portal** → **Create a resource** → **SQL Database**

2. **Configure database**:
   - **Subscription:** Your active subscription
   - **Resource group:** Create new or select existing
   - **Database name:** `qrstickers`
   - **Server:** Create new or select existing
     - **Server name:** `qrstickers-sql-server` (globally unique)
     - **Location:** Select region (e.g., UK South)
     - **Authentication:** SQL authentication (for admin access)

3. **Configure compute tier** (Free Tier):
   - Click **Configure database**
   - **Service tier:** General Purpose
   - **Compute tier:** **Serverless**
   - **Hardware:** Standard-series (Gen5)
   - **vCores:** 0.5 - 4 vCores (adjust based on load)
   - **Data max size:** 32 GB (within free tier)
   - **Auto-pause delay:** 1 hour (pauses when inactive)
   - Click **Apply**

4. **Networking**:
   - Allow Azure services: **Yes**
   - Add your current IP address for management access

5. **Review + Create** → **Create**

### Step 2: Create Azure Web App

1. Navigate to **Create a resource** → **Web App**

2. **Configure app**:
   - **App name:** `qrstickers` (globally unique)
   - **Runtime stack:** .NET 9 (LTS)
   - **Operating system:** Windows or Linux
   - **Region:** Same as SQL server
   - **App Service Plan:** Create new or select existing (B1 or higher recommended)

3. **Review + Create** → **Create**

### Step 3: Connect Web App to SQL Database (Service Connector)

This step automatically sets up managed identity authentication.

1. Go to your **Azure Web App** → **Service Connector** (left menu)

2. Click **+ Create** → **SQL Database**

3. **Configure connection**:
   - **Subscription:** Your subscription
   - **SQL server:** Select `qrstickers-sql-server`
   - **Database:** Select `qrstickers`
   - **Client type:** **.NET**

4. **Authentication** (choose one):
   - **System-assigned managed identity** (recommended - passwordless)
   - **User-assigned managed identity**
   - **Connection string** (SQL authentication)

5. Click **Next: Networking** → Keep defaults

6. Click **Next: Review + Create** → **Create**

**What Service Connector Does Automatically:**
- ✅ Creates connection string as `AZURE_SQL_CONNECTIONSTRING` environment variable
- ✅ Enables managed identity on the Web App
- ✅ Grants database permissions to the managed identity
- ✅ Configures Microsoft Entra (Azure AD) authentication on SQL Database
- ✅ No secrets stored in configuration

### Step 4: Configure Application Settings

1. Go to **Web App** → **Configuration** → **Application settings**

2. **Rename connection string variable**:
   - Find: `AZURE_SQL_CONNECTIONSTRING`
   - Click **Edit**
   - Change name to: `ConnectionStrings__DefaultConnection`
     - **Note:** Double underscore `__` is Azure's separator for nested JSON (translates to `ConnectionStrings:DefaultConnection`)
   - Click **OK**

3. **Add Meraki OAuth credentials** (if using Meraki integration):
   - Click **+ New application setting**
   - **Name:** `meraki_client_id`
   - **Value:** Your Meraki client ID
   - Click **OK**
   - Repeat for `meraki_client_secret`

4. Click **Save** → **Continue**

5. **Restart the app**

### Step 5: Deploy Application

**Option A: GitHub Actions (Recommended)**

1. Go to **Web App** → **Deployment Center**
2. **Source:** GitHub
3. Select your repository and branch
4. Azure will create a workflow file (`.github/workflows/main_qrstickers.yml`)
5. Commit and push → Deployment starts automatically

**Option B: Azure CLI**

```bash
# Login to Azure
az login

# Deploy from local directory
az webapp up --name qrstickers --resource-group <your-rg> --runtime "DOTNET:9.0"
```

**Option C: Visual Studio**

1. Right-click project → **Publish**
2. **Target:** Azure
3. **Specific target:** Azure App Service (Windows or Linux)
4. Select your app service
5. Click **Publish**

### Step 6: Apply Database Migrations

The application automatically runs migrations on startup (`Program.cs`), but you can also run manually:

**Option A: Automatic (Recommended)**

- Migrations run on first app startup
- Check logs to verify: **Web App** → **Log stream**

**Option B: Manual (Azure Data Studio or SSMS)**

```bash
# Generate SQL migration script
dotnet ef migrations script -o migrations.sql

# Connect to Azure SQL Database with Azure Data Studio
# Run the migrations.sql script
```

### Step 7: Verify Deployment

1. Navigate to `https://<your-app-name>.azurewebsites.net`
2. Register a new account
3. Connect Meraki account (if configured)
4. Verify sync works and templates load

**Success indicators:**
- ✅ Home page loads
- ✅ Registration works
- ✅ Login persists across browser sessions
- ✅ Meraki connection syncs data
- ✅ Template designer loads
- ✅ Export generates stickers

---

## Option 2: Manual Connection String (SQL Authentication)

If you prefer username/password authentication instead of managed identity:

### Step 1: Create SQL Database

Same as Option 1, Step 1.

### Step 2: Get Connection String

1. Go to **Azure Portal** → **SQL Database** → `qrstickers`
2. Click **Connection strings** (left menu)
3. Copy the **ADO.NET** connection string
4. Replace `{your_password}` with your SQL admin password

Example:
```
Server=tcp:qrstickers-sql-server.database.windows.net,1433;Initial Catalog=qrstickers;Persist Security Info=False;User ID=sqladmin;Password=<your_password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

### Step 3: Configure Web App

1. Go to **Web App** → **Configuration** → **Connection strings**

2. **Add connection string**:
   - **Name:** `DefaultConnection`
   - **Value:** Paste the connection string from Step 2
   - **Type:** `SQLServer`
   - Click **OK**

3. **Add application settings** (if using Meraki):
   - Add `meraki_client_id` and `meraki_client_secret`

4. Click **Save** → Restart app

### Step 4: Deploy

Same as Option 1, Step 5.

---

## Docker Deployment

### Build Docker Image

```bash
# Build multi-stage image
docker build -t qrstickers:latest .

# Tag for Azure Container Registry (ACR)
docker tag qrstickers:latest <your-acr>.azurecr.io/qrstickers:latest

# Push to ACR
az acr login --name <your-acr>
docker push <your-acr>.azurecr.io/qrstickers:latest
```

### Deploy to Azure Container Apps

**Prerequisites:**
- Azure Container Registry created
- Container App Environment created

**Steps:**

1. **Create Container App**:
   ```bash
   az containerapp create \
     --name qrstickers \
     --resource-group <your-rg> \
     --environment <your-env> \
     --image <your-acr>.azurecr.io/qrstickers:latest \
     --target-port 8080 \
     --ingress external \
     --registry-server <your-acr>.azurecr.io \
     --registry-username <acr-username> \
     --registry-password <acr-password>
   ```

2. **Configure environment variables**:
   ```bash
   az containerapp update \
     --name qrstickers \
     --resource-group <your-rg> \
     --set-env-vars "ConnectionStrings__DefaultConnection=<connection-string>"
   ```

3. **Deploy updates**:
   - GitHub Actions workflow automatically builds and pushes Docker images
   - Workflow file: `.github/workflows/qrstickers-AutoDeployTrigger-*.yml`

---

## Azure SQL Database Free Tier

### Free Tier Limits (Monthly)

- ✅ **100,000 vCore seconds** - Enough for continuous low-traffic apps
- ✅ **32 GB data storage**
- ✅ **32 GB backup storage**
- ✅ **Auto-pause when inactive** - No charges during pause (1 hour delay)
- ✅ **Forever free** - Renews monthly, no expiration

### Cost Breakdown

**Within free tier:**
- Serverless database auto-pauses after 1 hour of inactivity
- Only charged for compute when active
- Storage always free within 32 GB limit

**Exceeding free tier:**
- Compute charges apply for usage beyond 100,000 vCore seconds/month
- Configure alerts at 80% usage threshold

### Monitoring Free Tier Usage

1. Go to **Azure Portal** → **SQL Database** → **Metrics**
2. **Add metric**: "vCore seconds used"
3. Set up **Alert Rules**:
   - Threshold: 80,000 vCore seconds (80% of free tier)
   - Action: Send email notification
4. **Review monthly usage**:
   - Azure Cost Management + Billing
   - Filter by resource: `qrstickers` database

---

## GitHub Actions CI/CD

### Web Apps Workflow

Workflow file: `.github/workflows/main_qrstickers.yml`

**Triggers:**
- Push to `main` branch
- Manual workflow dispatch

**Steps:**
1. Checkout code
2. Setup .NET 9.0
3. Build project
4. Publish artifacts
5. Deploy to Azure Web App using OIDC authentication

**Configuration:**
- Uses **OIDC federated credentials** (no password needed)
- Configured in Azure Portal → App Service → Deployment Center

### Container Apps Workflow

Workflow file: `.github/workflows/qrstickers-AutoDeployTrigger-*.yml`

**Triggers:**
- Push to `main` branch

**Steps:**
1. Checkout code
2. Login to Azure Container Registry
3. Build Docker image
4. Push to ACR
5. Deploy to Container Apps

**Configuration:**
- Uses ACR credentials stored in GitHub Secrets
- Auto-generated by Azure Container Apps

---

## Troubleshooting

### Database Connection Fails

**Symptom:** App throws "Cannot connect to database"

**Solutions:**
1. Check firewall rules allow Azure services
2. Verify connection string is correct (`ConnectionStrings__DefaultConnection`)
3. Ensure managed identity has database permissions
4. Check SQL server is not paused (auto-pause after inactivity)

### Migrations Don't Run

**Symptom:** Database schema is outdated

**Solutions:**
1. Check app logs for migration errors: **Web App** → **Log stream**
2. Manually apply migrations using Azure Data Studio
3. Ensure connection string has sufficient permissions (`db_owner` role)

### OAuth Callback Fails

**Symptom:** Meraki OAuth redirects but fails to save token

**Solutions:**
1. Verify redirect URI matches exactly:
   - Meraki Dashboard: `https://<your-app>.azurewebsites.net/Meraki/Callback`
   - Code: `Pages/Meraki/Connect.cshtml.cs` and `Callback.cshtml.cs`
2. Check `meraki_client_secret` is set in Application Settings
3. Review app logs for detailed error messages

### App Runs Slowly

**Symptom:** Pages load slowly or timeout

**Solutions:**
1. Check if SQL database is paused (cold start takes 10-20 seconds)
2. Increase auto-pause delay: **SQL Database** → **Compute + storage** → 2+ hours
3. Upgrade App Service Plan to higher tier (B1 → S1)
4. Enable Application Insights for performance monitoring

### Container Apps Won't Start

**Symptom:** Container app shows "Provisioning failed"

**Solutions:**
1. Check container logs: **Container Apps** → **Logs**
2. Verify image exists in ACR and is accessible
3. Check environment variables are set correctly
4. Ensure ingress port matches container port (8080)

---

## Security Best Practices

### Production Checklist

- ✅ Use **managed identity** for database authentication (no passwords)
- ✅ Store secrets in **Azure Key Vault** (not Application Settings)
- ✅ Enable **HTTPS only** on Web App
- ✅ Configure **custom domain** with SSL certificate
- ✅ Set up **Azure Application Insights** for monitoring
- ✅ Enable **diagnostic logs** for SQL Database
- ✅ Configure **firewall rules** to restrict database access
- ✅ Use **Azure Front Door** or **CDN** for static assets
- ✅ Set up **backup and disaster recovery** for SQL Database
- ✅ Enable **Microsoft Defender for Cloud** for security recommendations

### OAuth Security

1. Rotate client secrets regularly (every 90 days)
2. Use HTTPS for all OAuth redirects
3. Validate state parameter to prevent CSRF attacks
4. Store refresh tokens encrypted in database
5. Never log or expose tokens in error messages

---

## Next Steps

- **Monitoring:** Set up Application Insights for performance tracking
- **Scaling:** Configure autoscaling rules based on traffic
- **Backups:** Enable automated SQL database backups
- **CDN:** Add Azure CDN for static assets (wwwroot/*)
- **Custom Domain:** Configure custom domain with SSL

For development setup, see [SETUP.md](SETUP.md).
