# Connect to Patroni HA

Connect **EventsPlatform** (and SQL clients) to the PostgreSQL cluster running in Kubernetes via port-forward.

← [Argo CD GitOps](../03-infrastructure/argocd-gitops-patroni-ha.md) · [Docs index](../README.md) · Next → [Restart environment](../05-ops/restart-environment.md)

## Prerequisites

Complete these docs first:

1. [Verify kubeadm cluster](../03-infrastructure/verify-kubeadm-cluster.md)
2. [Argo CD GitOps — Patroni HA](../03-infrastructure/argocd-gitops-patroni-ha.md)

Patroni HA must be Running in namespace `postgres-ha`.

---

## Step 1 — Verify Postgres is up

```bash
kubectl -n postgres-ha get postgresql,pods,svc,pvc
kubectl -n postgres-ha exec -it acid-postgres-cluster-0 -- patronictl list
```

Expected services:

| Service | Purpose |
|---------|---------|
| `acid-postgres-cluster` | Primary (writes) — use this for port-forward |
| `acid-postgres-cluster-repl` | Replicas (reads) |
| `acid-postgres-cluster-config` | Patroni config |

---

## Step 2 — Find the database password

The Zalando operator generates credentials into Kubernetes Secrets. **Never commit these to git.**

List secrets:

```bash
kubectl -n postgres-ha get secrets
```

The EventsPlatform user secret follows this pattern:

```text
events_platform.acid-postgres-cluster.credentials.postgresql.acid.zalan.do
```

Read username and password:

```bash
kubectl -n postgres-ha get secret \
  events_platform.acid-postgres-cluster.credentials.postgresql.acid.zalan.do \
  -o jsonpath='{.data.username}' | base64 -d; echo

kubectl -n postgres-ha get secret \
  events_platform.acid-postgres-cluster.credentials.postgresql.acid.zalan.do \
  -o jsonpath='{.data.password}' | base64 -d; echo
```

One-liner summary:

```bash
USER=$(kubectl -n postgres-ha get secret \
  events_platform.acid-postgres-cluster.credentials.postgresql.acid.zalan.do \
  -o jsonpath='{.data.username}' | base64 -d)
PASS=$(kubectl -n postgres-ha get secret \
  events_platform.acid-postgres-cluster.credentials.postgresql.acid.zalan.do \
  -o jsonpath='{.data.password}' | base64 -d)
echo "User:     $USER"
echo "Password: $PASS"
echo "Database: events_platform"
echo "Host:     localhost (after port-forward)"
echo "Port:     5432"
```

---

## Step 3 — Port-forward to the primary

Leave this terminal open while connecting:

```bash
kubectl -n postgres-ha port-forward svc/acid-postgres-cluster 5432:5432
```

**Port conflict:** if local port 5432 is already in use (another Postgres instance):

```bash
kubectl -n postgres-ha port-forward svc/acid-postgres-cluster 5433:5432
```

Use `Port=5433` in connection strings below.

---

## Step 4 — Test with psql

In a **second** terminal (with port-forward running):

```bash
psql "host=localhost port=5432 dbname=events_platform user=events_platform password=YOUR_PASSWORD sslmode=disable"
```

```sql
SELECT current_user, current_database(), version();
\q
```

---

## Step 5 — Connect EventsPlatform (.NET)

### Option A — User Secrets (recommended)

From the EventsPlatform solution root:

```bash
cd /home/ih-entpr/Documents/projects/pers/fairmas/dotnet-practice/EventsPlatform-DotNet-React/API

dotnet user-secrets set "ConnectionStrings:PostgresDefaultConnection" \
  "Host=localhost;Port=5432;Database=events_platform;Username=events_platform;Password=YOUR_PASSWORD;SSL Mode=Disable"
```

Verify:

```bash
dotnet user-secrets list
```

### Option B — appsettings.Development.json

Update `ConnectionStrings:PostgresDefaultConnection` in `API/appsettings.Development.json`.

Use placeholders in git; keep real passwords in user secrets or environment variables only.

### Run the API

With port-forward active:

```bash
cd /home/ih-entpr/Documents/projects/pers/fairmas/dotnet-practice/EventsPlatform-DotNet-React
dotnet build
dotnet run --project API
```

On startup, `Program.cs` runs:

```csharp
await context.Database.MigrateAsync();
await DbInitializer.SeedData(context);
```

This applies EF migrations and seeds 10 events if the table is empty.

---

## Step 6 — Connect from Rider / DBeaver / pgAdmin

| Field | Value |
|-------|--------|
| Host | `localhost` |
| Port | `5432` (or `5433` if forwarded that way) |
| Database | `events_platform` |
| User | `events_platform` |
| Password | From kubectl secret (Step 2) |
| SSL | Disabled |

---

## Full verification checklist

```text
[ ] kubectl -n postgres-ha get pods          → 3 Running
[ ] patronictl list                          → 1 Leader, 2 Replicas
[ ] kubectl get secret ...                   → password retrieved
[ ] kubectl port-forward svc/...             → tunnel active
[ ] psql connects                            → OK
[ ] dotnet run --project API                 → migrations + seed OK
[ ] SELECT COUNT(*) FROM "Events"            → 10 (after first run)
```

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| `connection refused` on localhost | Port-forward running? Correct service (`acid-postgres-cluster`)? |
| `password authentication failed` | Re-read secret; cluster may have been recreated |
| Secret not found | Wait for `postgres-ha-local` app to sync; check `kubectl -n postgres-ha get postgresql` |
| EF migration fails | Port-forward active; DB/user `events_platform` created by operator Helm values |
| Local port in use | Forward to `5433:5432` instead |

Postgres pod events:

```bash
kubectl -n postgres-ha get events --sort-by='.lastTimestamp'
kubectl -n postgres-ha describe pod acid-postgres-cluster-0
```

---

## Architecture recap

```text
EventsPlatform API (localhost)
  → kubectl port-forward :5432
    → svc/acid-postgres-cluster (K8s primary Service)
      → Patroni Leader pod (Spilo/PostgreSQL 16)
        → 2 Replica pods (async replication)
```

For in-cluster apps (future), use the service DNS directly — no port-forward:

```text
acid-postgres-cluster.postgres-ha.svc.cluster.local:5432
```

---

## Related docs

- [Bootstrap reproduction](../01-overview/bootstrap-reproduction.md)
- [EF Core & migrations](../02-data-layer/ef-core-and-migrations.md)
- [Patroni HA local runbook](../../../../../scripts/patroni-postgres-ha/docs/02-local-kubernetes.md)
