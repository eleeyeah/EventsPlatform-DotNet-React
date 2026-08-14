# Bootstrap & reproduction guide

Step-by-step instructions to recreate **EventsPlatform-DotNet-React** from scratch on a clean machine.

← [Architecture](architecture.md) · [Docs index](../README.md) · Next → [EF Core & migrations](../02-data-layer/ef-core-and-migrations.md)

## Prerequisites

| Tool | Version used | Check |
|------|--------------|-------|
| .NET SDK | 10.x | `dotnet --version` |
| IDE | Rider or VS Code | optional |
| PostgreSQL | 16.x | local instance or K8s Patroni (later steps) |
| EF Core CLI | matches EF 10 | `dotnet ef --version` |

Install EF global tool:

```bash
dotnet tool install --global dotnet-ef
```

Update if already installed:

```bash
dotnet tool update --global dotnet-ef
```

Verify:

```bash
dotnet ef --version
```

---

## Step 1 — Create folder and solution

```bash
mkdir EventsPlatform-DotNet-React
cd EventsPlatform-DotNet-React

dotnet new sln -n EventsPlatform-DotNet-React
```

---

## Step 2 — Create the four projects

```bash
dotnet new webapi -n API -o API --use-controllers
dotnet new classlib -n Domain -o Domain
dotnet new classlib -n Application -o Application
dotnet new classlib -n Persistence -o Persistence
```

Add all projects to the solution:

```bash
dotnet sln add API/API.csproj
dotnet sln add Domain/Domain.csproj
dotnet sln add Application/Application.csproj
dotnet sln add Persistence/Persistence.csproj
```

Each `.csproj` should include (edit if the template omitted them):

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
</PropertyGroup>
```

---

## Step 3 — Wire project references

Clean Architecture dependency direction:

```bash
dotnet add Persistence/Persistence.csproj reference Domain/Domain.csproj

dotnet add Application/Application.csproj reference Domain/Domain.csproj
dotnet add Application/Application.csproj reference Persistence/Persistence.csproj

dotnet add API/API.csproj reference Application/Application.csproj
dotnet add API/API.csproj reference Persistence/Persistence.csproj
```

Verify references:

```bash
dotnet list Persistence/Persistence.csproj reference
dotnet list Application/Application.csproj reference
dotnet list API/API.csproj reference
```

---

## Step 4 — Add NuGet packages

### Persistence — PostgreSQL provider

```bash
dotnet add Persistence/Persistence.csproj package Npgsql.EntityFrameworkCore.PostgreSQL --version 10.0.3
```

### API — design-time migrations tooling

```bash
dotnet add API/API.csproj package Microsoft.EntityFrameworkCore.Design --version 10.0.11
```

| Package | Project | Purpose |
|---------|---------|---------|
| `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 | Persistence | EF Core provider for PostgreSQL |
| `Microsoft.EntityFrameworkCore.Design` 10.0.11 | API | `dotnet ef migrations` (PrivateAssets) |

Restore packages:

```bash
dotnet restore
```

---

## Step 5 — Domain entity

Create `Domain/Event.cs`:

```csharp
namespace Domain;

public class Event
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required string Title { get; init; }
    public DateTime Date { get; init; }
    public bool IsCancelled { get; init; }
    public string? Description { get; init; }
    public string? Category { get; init; }

    public required string City { get; init; }
    public required string Venue { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}
```

---

## Step 6 — Persistence (DbContext)

Create `Persistence/AppDbContext.cs`:

```csharp
using Domain;
using Microsoft.EntityFrameworkCore;

namespace Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Event> Events { get; set; }
}
```

---

## Step 7 — PostgreSQL database (local dev)

Connect as a superuser and create the database role:

```bash
sudo -u postgres psql
```

```sql
CREATE USER events_platform WITH PASSWORD 'your-secure-password';
CREATE DATABASE events_platform OWNER events_platform;
\q
```

Or if PostgreSQL accepts local peer auth differently on your machine:

```bash
psql -U postgres -h localhost
```

Configure connection string — prefer **User Secrets** over committing passwords:

```bash
cd API
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:PostgresDefaultConnection" \
  "Host=localhost;Port=5432;Database=events_platform;Username=events_platform;Password=your-secure-password;SSL Mode=Disable"
cd ..
```

Alternatively, add to `API/appsettings.Development.json` (dev only — do not commit real passwords to git):

```json
{
  "ConnectionStrings": {
    "PostgresDefaultConnection": "Host=localhost;Port=5432;Database=events_platform;Username=events_platform;Password=YOUR_PASSWORD;SSL Mode=Disable"
  }
}
```

---

## Step 8 — Register DbContext in API

Replace `API/Program.cs` with:

```csharp
using Microsoft.EntityFrameworkCore;
using Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresDefaultConnection")));

var app = builder.Build();

app.MapControllers();

app.Run();
```

---

## Step 9 — First EF migration (`InitialCreate`)

From the **solution root**:

```bash
dotnet ef migrations add InitialCreate \
  --project Persistence \
  --startup-project API \
  --output-dir Migrations
```

Apply to the database:

```bash
dotnet ef database update \
  --project Persistence \
  --startup-project API
```

List migrations to confirm:

```bash
dotnet ef migrations list \
  --project Persistence \
  --startup-project API
```

---

## Step 10 — Add `Description` and `Category` (second migration)

After adding those properties to `Domain/Event.cs`:

```bash
dotnet ef migrations add SyncAppDbContext \
  --project Persistence \
  --startup-project API

dotnet ef database update \
  --project Persistence \
  --startup-project API
```

---

## Step 11 — Seed data + auto-migrate on startup

Copy `Persistence/DbInitializer.cs` from this repo (10 sample events with London/Paris venues).

Extend `API/Program.cs` **before** `app.Run()`:

```csharp
using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;

try
{
    var context = services.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    await DbInitializer.SeedData(context);
}
catch (Exception ex)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while migrating or seeding the database.");
}
```

---

## Step 12 — Build and run

```bash
dotnet build
dotnet run --project API
```

Expected: API listens on `https://localhost:5001` (see `API/Properties/launchSettings.json`).

Verify data in PostgreSQL:

```bash
psql "host=localhost port=5432 dbname=events_platform user=events_platform sslmode=disable"
```

```sql
SELECT COUNT(*) FROM "Events";
-- expect 10 after first successful run
\q
```

---

## Step 13 — Optional cleanup (template files)

Remove unused ASP.NET template files if desired:

```bash
rm API/Controllers/WeatherForecastController.cs
rm API/WeatherForecast.cs
```

Not required for reproduction; the project builds with or without them.

---

## Accomplished checklist

- [x] Solution with 4 projects (Clean Architecture layout)
- [x] Project references following layer rules
- [x] NuGet: `Npgsql.EntityFrameworkCore.PostgreSQL` + `Microsoft.EntityFrameworkCore.Design`
- [x] `Event` domain entity with location fields
- [x] `AppDbContext` + PostgreSQL connection
- [x] Migration `InitialCreate` (Events table)
- [x] Migration `SyncAppDbContext` (Description, Category)
- [x] `DbInitializer` with 10 sample events
- [x] Auto-migrate + seed on API startup
- [ ] Application use cases (next)
- [ ] Events API endpoints (next)
- [ ] React frontend (next)
- [ ] Connect to Patroni HA in K8s → [verify cluster](../03-infrastructure/verify-kubeadm-cluster.md) → [Argo CD GitOps](../03-infrastructure/argocd-gitops-patroni-ha.md) → [connect](../04-database/connect-to-patroni-ha.md)

---

## Troubleshooting

| Problem | Fix |
|---------|-----|
| `dotnet ef` not found | `dotnet tool install --global dotnet-ef` |
| Migration fails: no connection | Check Postgres is running, DB/user exist, connection string |
| `Unable to create DbContext` | Use `--startup-project API`; ensure Design package on API |
| Build: Persistence missing EF | Add `Npgsql.EntityFrameworkCore.PostgreSQL` to Persistence |
| Port 5432 in use | Stop local Postgres or use K8s port-forward on another port |

## Related docs

- [Architecture](architecture.md)
- [EF Core & migrations](../02-data-layer/ef-core-and-migrations.md)
