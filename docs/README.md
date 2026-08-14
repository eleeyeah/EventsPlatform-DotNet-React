# EventsPlatform — documentation

Reproducible guide from **Clean Architecture bootstrap** through **PostgreSQL + EF Core**, **kubeadm + Argo CD GitOps (Patroni HA)**, and **connecting this app** to the cluster database.

Work through the docs **in order** — each step builds on the previous one.

**Coming back to work?** Start here: [Restart environment](05-ops/restart-environment.md) (cluster, Argo CD UI, Patroni leader, DB port-forward).

## Reading order

| Step | Doc | What you accomplish |
|------|-----|---------------------|
| 0 | [Restart environment](05-ops/restart-environment.md) | Day-2: cluster checkup, Argo CD + DB passwords, leader, port-forwards |
| 1 | [Clean Architecture overview](01-overview/architecture.md) | Understand the four layers and dependency rules |
| 2 | [Bootstrap & reproduction](01-overview/bootstrap-reproduction.md) | Recreate the .NET solution, NuGet packages, and project wiring from scratch |
| 3 | [EF Core & migrations](02-data-layer/ef-core-and-migrations.md) | DbContext, migrations, seeding, and day-to-day schema changes |
| 4 | [Verify kubeadm cluster](03-infrastructure/verify-kubeadm-cluster.md) | Confirm the homelab Kubernetes cluster is healthy |
| 5 | [Argo CD GitOps — Patroni HA](03-infrastructure/argocd-gitops-patroni-ha.md) | Deploy Postgres HA via Argo CD on the local cluster |
| 6 | [Connect to Patroni HA](04-database/connect-to-patroni-ha.md) | Port-forward, retrieve credentials, connect EventsPlatform |

## Current project state

| Area | Status |
|------|--------|
| 4-layer solution (Domain, Application, Persistence, API) | Done |
| `Event` entity + EF Core + PostgreSQL | Done |
| Migrations `InitialCreate`, `SyncAppDbContext` | Done |
| `DbInitializer` (10 sample events) | Done |
| Auto-migrate + seed on API startup | Done |
| Application use cases / Events API | Not yet |
| React frontend | Not yet |
| Connection to K8s Patroni HA | Documented — wire when ready |

## External repos (infrastructure)

Infrastructure lives outside this repo:

| Path | Purpose |
|------|---------|
| [`scripts/cluster/`](../../../../scripts/cluster/) | kubeadm homelab cluster bootstrap |
| [`scripts/patroni-postgres-ha/`](../../../../scripts/patroni-postgres-ha/) | Zalando Postgres Operator + Patroni HA Helm + Argo CD manifests |

## Quick commands (after setup)

```bash
# Build and run API (local Postgres or port-forward active)
dotnet build
dotnet run --project API

# EF migration (from repo root)
dotnet ef migrations add <Name> --project Persistence --startup-project API
dotnet ef database update --project Persistence --startup-project API

# Patroni prereq check (from patroni-postgres-ha repo)
cd ../../../../scripts/patroni-postgres-ha
./scripts/check-prereqs.sh
```
