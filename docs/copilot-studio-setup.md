# Configuring DataFactory MCP Server for Copilot Studio

This guide explains how to connect the DataFactory MCP HTTP server to Microsoft Copilot Studio for AI-powered Fabric resource management.

## Prerequisites

- DataFactory MCP HTTP server deployed to Azure App Service
- Azure AD tenant with admin access to create app registrations
- Access to Microsoft Copilot Studio
- DLP policies configured to allow custom MCP connectors (see [Troubleshooting](#troubleshooting))

## Architecture Overview

```
┌─────────────────┐     OAuth Token      ┌─────────────────┐     Same Token     ┌─────────────────┐
│  Copilot Studio │ ─────────────────►   │   MCP Server    │ ─────────────────► │   Fabric APIs   │
│  (User's token) │   Authorization:     │  (Passthrough)  │                    │                 │
│                 │   Bearer eyJ...      │                 │                    │                 │
└─────────────────┘                      └─────────────────┘                    └─────────────────┘
```

The MCP server uses **OAuth token passthrough** - it extracts the Bearer token from Copilot Studio requests and uses it to authenticate with Fabric APIs. This means users access Fabric resources with their own permissions.

## Step 1: Create Azure AD App Registration

1. Go to [Azure Portal](https://portal.azure.com) → **Microsoft Entra ID** → **App registrations**

2. Click **+ New registration**

3. Configure the registration:
   - **Name**: `Copilot Studio MCP Connector`
   - **Supported account types**: Choose based on your needs (single tenant recommended)
   - **Redirect URI**: Leave blank for now (will add after Copilot Studio setup)

4. Click **Register**

5. Note the **Application (client) ID** and **Directory (tenant) ID** - you'll need these

### Create Client Secret

1. In your app registration, go to **Certificates & secrets**
2. Click **+ New client secret**
3. Add a description and expiration
4. Click **Add**
5. **Copy the secret value immediately** - it won't be shown again

### Configure API Permissions

1. Go to **API permissions** → **+ Add a permission**

2. Select **APIs my organization uses** → Search for `Power BI Service`

3. Select **Delegated permissions** and add:
   - `Capacity.Read.All`
   - `Capacity.ReadWrite.All`
   - `Connection.Read.All`
   - `Connection.ReadWrite.All`
   - `Dataflow.Read.All`
   - `Dataflow.ReadWrite.All`
   - `Gateway.Read.All`
   - `Gateway.ReadWrite.All`
   - `Workspace.Read.All`
   - `Workspace.ReadWrite.All`

4. Click **Add permissions**

5. If required, click **Grant admin consent for [your org]**

## Step 2: Configure Copilot Studio

1. Open [Copilot Studio](https://copilotstudio.microsoft.com)

2. Select or create an Agent

3. Go to **Tools** → **+ Add a tool** → **Model Context Protocol server**

4. Configure the MCP server:

   | Field | Value |
   |-------|-------|
   | **Server name** | `Microsoft Data Factory MCP Server` |
   | **Server description** | `Provides tools for accessing Microsoft Fabric resources` |
   | **Server URL** | `https://your-app-service.azurewebsites.net/mcp` |
   | **Authentication** | `OAuth 2.0` |
   | **Type** | `Manual` |

5. Fill in OAuth configuration:

   | Field | Value |
   |-------|-------|
   | **Client ID** | Your app registration's Application ID |
   | **Client secret** | The secret you created |
   | **Authorization URL** | `https://login.microsoftonline.com/{tenant-id}/oauth2/v2.0/authorize` |
   | **Token URL template** | `https://login.microsoftonline.com/{tenant-id}/oauth2/v2.0/token` |
   | **Refresh URL** | `https://login.microsoftonline.com/{tenant-id}/oauth2/v2.0/token` |
   | **Scopes** | `https://api.fabric.microsoft.com/.default` |

   > Replace `{tenant-id}` with your Azure AD tenant ID

6. Click **Create** or **Save**

7. **Copy the Redirect URL** that Copilot Studio generates

## Step 3: Add Redirect URI to App Registration

1. Go back to your app registration in Azure Portal

2. Go to **Authentication** → **+ Add a platform** → **Web**

3. Paste the **Redirect URI** from Copilot Studio

4. Click **Configure**

## Step 4: Test the Connection

1. In Copilot Studio, open the chat panel

2. Try a command like:
   - "List my Fabric workspaces"
   - "Show my connections"
   - "List available gateways"

3. You should be prompted to sign in on first use

4. After authentication, the MCP tools should work with your permissions

## Available Tools

Once connected, these tools are available to Copilot Studio:

| Tool | Description |
|------|-------------|
| `authenticate_interactive` | Authenticate with Azure AD |
| `get_authentication_status` | Check current auth status |
| `list_workspaces` | List Fabric workspaces |
| `list_connections` | List data connections |
| `list_gateways` | List on-premises gateways |
| `list_capacities` | List Fabric capacities |
| `list_dataflows` | List dataflows in a workspace |
| `get_decoded_dataflow_definition` | Get dataflow M code |

## Troubleshooting

### DLP Policy Blocking Connection

If you see "Connection blocked by Data Loss Prevention policy":

1. Go to [Power Platform Admin Center](https://admin.powerplatform.microsoft.com)
2. Navigate to **Security** → **Data and privacy** → **Data policy**
3. Either:
   - Create a new policy that allows your MCP server URL
   - Or modify an existing policy to add your URL pattern to the allowed list

### Authentication Errors

1. **Invalid client secret**: Regenerate the secret and update Copilot Studio
2. **Missing permissions**: Ensure API permissions are granted and admin consented
3. **Wrong redirect URI**: Verify the redirect URI matches exactly

### Server Not Responding

1. Check the App Service is running: `https://your-app-service.azurewebsites.net/health`
2. Verify the URL includes `/mcp` path
3. Check App Service logs for errors

### Token Expired

The OAuth token passthrough means tokens expire based on Azure AD settings. If tools fail after working initially:
1. Sign out and sign back in
2. Or refresh the page in Copilot Studio

## Security Considerations

1. **Token Passthrough**: The MCP server uses the user's token, so users only see resources they have access to

2. **No Stored Credentials**: The server doesn't store any credentials - all auth is handled by Azure AD

3. **HTTPS Only**: Always use HTTPS for the MCP server URL

4. **App Registration Security**:
   - Rotate client secrets regularly
   - Use minimal required permissions
   - Enable Conditional Access if needed

## Alternative: Service Principal Authentication

For server-to-server scenarios without user interaction, you can configure the MCP server to use a service principal:

1. Set environment variables on your App Service:
   ```
   AZURE_CLIENT_ID=<app-registration-id>
   AZURE_CLIENT_SECRET=<client-secret>
   AZURE_TENANT_ID=<tenant-id>
   ```

2. The server will authenticate on startup using these credentials

3. All users will access Fabric with the service principal's permissions

> ⚠️ This approach means all users share the same permissions, which may not be appropriate for production.
