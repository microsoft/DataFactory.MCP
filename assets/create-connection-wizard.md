---
title: "Data Factory MCP Server — Create Connection Wizard"
subtitle: "Interactive guided UI for creating data source connections in VS Code"
date: "March 2026"
geometry: margin=1in
---

# Create Connection Wizard

The **Create Connection** wizard is an interactive 3-step form rendered directly inside VS Code chat via the Data Factory MCP Server. It guides users through setting up a new data source connection — no need to remember parameter names, credential fields, or API details.

**To open the wizard**, ask in VS Code chat:

> *open the create connection form*

Or reference the resource URI: `ui://datafactory/create-connection`

---

## Step 1 — Choose Connectivity Type

![](New%20Connection%20Type.jpg)

The user picks how they want to connect:

- **Cloud** — Shareable cloud connection (default)
- **On-Premises** — Requires an on-premises data gateway
- **Virtual Network** — Connects through a VNet gateway

Selecting a mode automatically advances to the next step.

---

## Step 2 — Connection Details

![](New%20Connection%20Details.jpg)

The user fills in the connection configuration:

- **Gateway** — Shown only for on-prem / VNet modes; dropdown of available gateways
- **Connection name** — Display name for the connection
- **Data source type** — Searchable dropdown with 45+ supported types, including:

| Type | Description |
|------|-------------|
| SQL | SQL Server & Azure SQL Database |
| AzureBlobs | Azure Blob Storage |
| AzureDataLakeStorage | Azure Data Lake Storage Gen2 |
| Lakehouse | Microsoft Fabric Lakehouse |
| Warehouse | Microsoft Fabric Warehouse |
| Web | REST APIs, SharePoint Online, web endpoints |
| PostgreSql | PostgreSQL databases |
| MySql | MySQL databases |
| Oracle | Oracle databases |
| Snowflake | Snowflake Data Cloud |

- **Connection parameters** — Fields appear dynamically based on the selected type (e.g. server, database for SQL; account name, account domain for Azure Blobs)

The full list of supported types is loaded dynamically from the Fabric API at runtime.

---

## Step 3 — Credentials & Settings

![](New%20Connection%20Credentials.jpg)

The user configures authentication and connection options:

- **Credential type** — Only valid options for the chosen data source are shown (e.g. Basic, OAuth2, Key, Anonymous, Windows, ServicePrincipal)
- **Credential fields** — Adapts dynamically to the selected auth method (e.g. username/password for Basic, account key for Key)
- **Privacy level** — None, Organizational, Public, or Private
- **Connection encryption** — NotEncrypted, Encrypted, or Any
- **Skip test connection** — Toggle to bypass the connectivity test on creation

On submit, the connection is created via the Fabric API and a confirmation banner shows the new connection ID and name.

---

## How It Works

The wizard is built as an **MCP App resource** — a React application bundled into a single HTML file and served through the MCP protocol. It:

1. **Loads data dynamically** — Fetches available gateways and supported connection types from the Fabric API on mount
2. **Validates at each step** — Required fields, gateway selection for non-cloud modes, and credential completeness
3. **Calls MCP tools** — Submits via the `create_connection` tool, which handles the Fabric API call
4. **Updates model context** — On success, stores the new connection metadata so downstream tools (e.g. dataflow creation) can reference it

## Supported Connectivity Types

| Mode | API Value | Requires Gateway |
|------|-----------|:---:|
| Cloud | `ShareableCloud` | No |
| On-Premises | `OnPremisesGateway` | Yes |
| Virtual Network | `VirtualNetworkGateway` | Yes |

## Supported Credential Types

| Credential Type | Typical Use |
|----------------|-------------|
| Anonymous | Public endpoints, anonymous access |
| Basic | Username/password authentication |
| Windows | Windows/domain authentication (on-prem) |
| OAuth2 | OAuth 2.0 token-based authentication |
| Key | API keys, storage account keys |
| SharedAccessSignature | Azure SAS tokens |
| ServicePrincipal | App registration (client ID + secret) |
| WorkspaceIdentity | Fabric workspace managed identity |
