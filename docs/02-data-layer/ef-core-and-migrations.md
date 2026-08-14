# EF Core & migrations

How persistence works in EventsPlatform: DbContext, migrations, seeding, and day-to-day schema changes.

← [Bootstrap reproduction](../01-overview/bootstrap-reproduction.md) · [Docs index](../README.md) · Next → [Verify kubeadm cluster](../03-infrastructure/verify-kubeadm-cluster.md)

## Components

| File | Role |
|------|------|
| `Persistence/AppDbContext.cs` | EF session; exposes `DbSet<Event>` |
| `Persistence/Migrations/` | Versioned schema history |
| `Persistence/DbInitializer.cs` | Development seed data |
| `API/Program.cs` | Registers Npgsql + runs `MigrateAsync()` at startup |

## Database schema (current)

Table: **`Events`**

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| Id | text | NO | PK, GUID string |
| Title | text | NO | |
| Date | timestamptz | NO | UTC |
| IsCancelled | boolean | NO | default false |
| Description | text | YES | added in `SyncAppDbContext` |
| Category | text | YES | added in `SyncAppDbContext` |
| City | text | NO | |
| Venue | text | NO | |
| Latitude | double precision | NO | |
| Longitude | double precision | NO | |

## Migration history

| Migration | Timestamp | Change |
|-----------|-----------|--------|
| `InitialCreate` | 20260813201851 | Create `Events` table (core columns) |
| `SyncAppDbContext` | 20260813222245 | Add `Description`, `Category` |

EF product version in snapshot: **10.0.11**.

---

## All EF CLI commands (reference)

Run from the **solution root** unless noted.

### Install / update global tool

```bash
dotnet tool install --global dotnet-ef
dotnet tool update --global dotnet-ef
dotnet ef --version
```

### Add a migration

```bash
dotnet ef migrations add <MigrationName> \
  --project Persistence \
  --startup-project API \
  --output-dir Migrations
```

Examples already applied in this project:

```bash
dotnet ef migrations add InitialCreate \
  --project Persistence \
  --startup-project API \
  --output-dir Migrations

dotnet ef migrations add SyncAppDbContext \
  --project Persistence \
  --startup-project API
```

### Apply migrations

```bash
dotnet ef database update \
  --project Persistence \
  --startup-project API
```

Apply up to a specific migration:

```bash
dotnet ef database update InitialCreate \
  --project Persistence \
  --startup-project API
```

### List migrations

```bash
dotnet ef migrations list \
  --project Persistence \
  --startup-project API
```

### Remove last migration (dev only — not applied to DB yet)

```bash
dotnet ef migrations remove \
  --project Persistence \
  --startup-project API
```

### Generate SQL script (CI/CD)

```bash
dotnet ef migrations script \
  --project Persistence \
  --startup-project API \
  -o migrate.sql
```

Script from empty database to latest:

```bash
dotnet ef migrations script \
  --project Persistence \
  --startup-project API \
  --idempotent \
  -o migrate-idempotent.sql
```

---

## How migrations are applied at runtime

**At startup** (`Program.cs`):

```csharp
await context.Database.MigrateAsync();
await DbInitializer.SeedData(context);
```

- `MigrateAsync()` applies any pending migrations.
- Errors are logged; the app still runs (consider failing fast in production).

---

## DbInitializer behavior

- Runs after migrations.
- **Idempotent:** `if (context.Events.Any()) return;` — skips if data exists.
- Seeds **10 events** (2 past, 8 future) across London/Paris with real lat/long.
- Uses `DateTime.UtcNow.AddMonths(n)` so dates stay relative.

Re-seed by truncating and restarting the API:

```sql
TRUNCATE TABLE "Events";
```

```bash
dotnet run --project API
```

---

## Connection strings

**Development:** `API/appsettings.Development.json` → `PostgresDefaultConnection`

**Recommended — User Secrets (no password in git):**

```bash
cd API
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:PostgresDefaultConnection" \
  "Host=localhost;Port=5432;Database=events_platform;Username=events_platform;Password=YOUR_PASSWORD;SSL Mode=Disable"
dotnet user-secrets list
cd ..
```

User secrets override `appsettings.Development.json` at runtime.

**When using K8s Patroni HA:** port-forward first, then point the connection string at `localhost` — see [Connect to Patroni HA](../04-database/connect-to-patroni-ha.md).

---

## Packages recap

```text
Persistence.csproj
  └── Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3
        └── pulls Microsoft.EntityFrameworkCore 10.x transitively

API.csproj
  └── Microsoft.EntityFrameworkCore.Design 10.0.11  (PrivateAssets — CLI only)
```

Restore after changes:

```bash
dotnet restore
dotnet list Persistence/Persistence.csproj package
dotnet list API/API.csproj package
```

---

## Conventions in this project

- Migrations live in **Persistence**, not API (infrastructure owns schema).
- Table name: `Events` (EF pluralizes `DbSet<Event>` by default).
- String IDs (GUID) instead of int identity — API-friendly for distributed clients.
- No Fluent API yet — conventions only; add `OnModelCreating` when rules grow.

---

## Next steps

- Add repository interface in **Application**, implementation in **Persistence**
- Add indexes (e.g. on `Date`, `City`) via migration when queries exist
- Deploy Postgres to K8s → [Verify kubeadm](../03-infrastructure/verify-kubeadm-cluster.md) → [Argo CD GitOps](../03-infrastructure/argocd-gitops-patroni-ha.md)
