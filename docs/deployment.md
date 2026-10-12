# Purchasing and Supplier deployment

The `main` workflow tests the .NET 10 solution, publishes immutable SHA and
`latest` images to GHCR, and updates only the existing Purchasing Container App.
It never creates Azure resources.

Repository-level Azure OIDC variables are required before a deployment run.

## Existing Azure architecture

- Resource group: `rg-MEDZO-NEW`
- Container Apps environment: `medzo-env-01`
- SQL server: `medzo-new-sql`
- SQL database: `medzo-purchasing-db`
- Intended container app: `medzo-purchasing-supplier-service`
- Container registry: `ghcr.io`

Do not create an ACR, Container Apps environment, SQL server, Event Hubs/Kafka,
private endpoint, NAT gateway, or monitoring resource for this service.

## Runtime configuration

Keep secrets in Container Apps and reference them from environment variables:

```text
ASPNETCORE_ENVIRONMENT=Production
Database__Provider=SqlServer
Database__MigrateOnStartup=true
ConnectionStrings__PurchasingSupplier=secretref:purchasing-db-connection
Jwt__Secret=secretref:jwt-signing-secret
Jwt__Issuer=MedzoAuthService
Jwt__Audience=MedzoClient
Kafka__Enabled=false
Kafka__BootstrapServers=<existing-broker-hosts>
Cors__AllowedOrigins__0=https://brave-forest-045498500.3.azurestaticapps.net
```

The service publishes received purchase-order events to
`purchasing.purchase-recorded.v1`. Keep Kafka disabled until an existing broker
and its credentials have been verified.

## Database initialization

The repository contains SQL Server EF Core migrations. The production app runs
them at startup. EF migration history makes this repeatable, and the app remains
limited to one replica.

After the database is initialized, verify `/health`, then create and retrieve a
supplier and purchase order through authenticated API requests. Never commit
the SQL connection string, JWT secret, or Kafka credentials.

## Azure deployment

The workflow first verifies that the named app already exists in `medzo-env-01`
and that its SQL and JWT environment variables reference existing secrets. It
then updates only that app to the SHA-tagged GHCR image. Consumption sizing is
preserved at 0.5 CPU, 1 GiB, minimum replicas 0, and maximum replicas 1.
