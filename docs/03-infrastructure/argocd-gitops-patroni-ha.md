# Argo CD GitOps — Patroni Postgres HA

Deploy **high-availability PostgreSQL** (Zalando Postgres Operator + Patroni) to your local kubeadm cluster using **Argo CD GitOps**.

← [Verify kubeadm cluster](verify-kubeadm-cluster.md) · [Docs index](../README.md) · Next → [Connect to Patroni HA](../04-database/connect-to-patroni-ha.md)

## Overview

```text
Git repo (patroni-postgres-ha)
  └── Argo CD (sync wave 0) → Zalando postgres-operator
  └── Argo CD (sync wave 1) → postgres-ha Helm chart → postgresql CR (3 Patroni pods)
```

| Item | Value |
|------|-------|
| Infrastructure repo | [`scripts/patroni-postgres-ha/`](../../../../../scripts/patroni-postgres-ha/) |
| Git remote | `git@github.com:eleeyeah/patroni-postgres-ha.git` |
| Operator namespace | `postgres-operator` |
| Postgres namespace | `postgres-ha` |
| Cluster name | `acid-postgres-cluster` |
| DB / user (for EventsPlatform) | `events_platform` |

Upstream docs in that repo:

- [`argocd/README.md`](../../../../../scripts/patroni-postgres-ha/argocd/README.md)
- [`argocd/CONNECT-REPO.md`](../../../../../scripts/patroni-postgres-ha/argocd/CONNECT-REPO.md)
- [`docs/02-local-kubernetes.md`](../../../../../scripts/patroni-postgres-ha/docs/02-local-kubernetes.md)

---

## Prerequisites

Complete [Verify kubeadm cluster](verify-kubeadm-cluster.md) first.

You also need:

- `kubectl` — configured for the kubeadm cluster
- `helm` — for installing Argo CD
- `git` — push access to the patroni-postgres-ha remote
- SSH key tooling — for private repo deploy key

---

## Phase A — Install Argo CD

Add the Helm repo and install Argo CD into the `argocd` namespace:

```bash
helm repo add argo https://argoproj.github.io/argo-helm
helm repo update

helm upgrade --install argocd argo/argo-cd \
  --namespace argocd \
  --create-namespace \
  --version 7.3.11
```

Wait for pods:

```bash
kubectl -n argocd get pods
kubectl -n argocd wait --for=condition=Available deployment/argocd-server --timeout=300s
```

Get the initial admin password:

```bash
kubectl -n argocd get secret argocd-initial-admin-secret \
  -o jsonpath='{.data.password}' | base64 -d; echo
```

Port-forward the Argo CD UI:

```bash
kubectl -n argocd port-forward svc/argocd-server 8080:443
```

Open `https://localhost:8080` — login: `admin` / password from above.

(Optional) Install Argo CD CLI:

```bash
# Linux example — adjust for your OS
curl -sSL -o argocd https://github.com/argoproj/argo-cd/releases/latest/download/argocd-linux-amd64
chmod +x argocd
sudo mv argocd /usr/local/bin/
```

---

## Phase B — Configure git repository

All commands from the patroni-postgres-ha repo root:

```bash
cd ../../../../../scripts/patroni-postgres-ha
```

### B1 — Confirm repoURL

Every Argo CD manifest must point at your git remote. Default in this project:

```text
git@github.com:eleeyeah/patroni-postgres-ha.git
```

See [`argocd/REPO.md`](../../../../../scripts/patroni-postgres-ha/argocd/REPO.md) for files that reference `repoURL`.

### B2 — Generate deploy key (private repo)

```bash
ssh-keygen -t ed25519 -C "argocd@patroni-postgres-ha" \
  -f argocd/deploy-key -N ""
```

Show the public key:

```bash
cat argocd/deploy-key.pub
```

### B3 — Add deploy key on GitHub

1. Open `https://github.com/eleeyeah/patroni-postgres-ha/settings/keys`
2. **Add deploy key**
3. Title: `argocd-local`
4. Paste contents of `argocd/deploy-key.pub`
5. Leave **Allow write access** unchecked
6. Save

### B4 — Build the repository Secret (local only — gitignored)

```bash
./scripts/create-git-repo-secret.sh
git check-ignore -v argocd/git-repo-secret.yaml
```

### B5 — Apply Secret to cluster

```bash
kubectl apply -f argocd/git-repo-secret.yaml

kubectl -n argocd get secret patroni-postgres-ha-repo \
  -o jsonpath='{.metadata.labels}'; echo
```

Expected label: `argocd.argoproj.io/secret-type: repository`

### B6 — Verify repo connection

In Argo CD UI: **Settings → Repositories** — status should be **Successful**.

CLI alternative:

```bash
kubectl -n argocd port-forward svc/argocd-server 8080:443
# separate terminal:
argocd login localhost:8080 --username admin --password <ADMIN_PASSWORD> --insecure
argocd repo list
```

---

## Phase C — Configure StorageClass

Edit [`helm-charts/postgres-ha/values-local.yaml`](../../../../../scripts/patroni-postgres-ha/helm-charts/postgres-ha/values-local.yaml):

```yaml
numberOfInstances: 3

volume:
  size: 10Gi
  storageClass: local-path   # must match a class on YOUR cluster
```

Confirm your cluster has that class:

```bash
kubectl get storageclass
```

Commit and push if you changed values:

```bash
git add helm-charts/postgres-ha/values-local.yaml
git commit -m "Set storageClass for local cluster"
git push
```

---

## Phase D — Deploy via Argo CD (GitOps)

Push any local commits so Argo CD can clone the latest:

```bash
git push
```

Apply the AppProject and ApplicationSet:

```bash
kubectl apply -f argocd/project/project.yaml
kubectl apply -f argocd/project/applicationset.yaml
```

Optional App-of-Apps pattern instead:

```bash
kubectl apply -f argocd/root-app.yaml
```

Watch applications:

```bash
kubectl -n argocd get applicationset
kubectl -n argocd get applications
kubectl -n argocd get applications -w
```

**Sync waves** (order matters):

| App | Wave | Namespace | Source |
|-----|------|-----------|--------|
| `postgres-operator-local` | 0 | `postgres-operator` | Zalando upstream Helm chart |
| `postgres-ha-local` | 1 | `postgres-ha` | This repo's `helm-charts/postgres-ha` |

Manual sync if needed:

```bash
argocd app sync postgres-operator-local
argocd app sync postgres-ha-local
```

Or wait for automated sync (configured in Application manifests).

---

## Phase E — Verify Patroni HA is running

```bash
kubectl -n postgres-operator get pods
kubectl get crd postgresqls.acid.zalan.do
kubectl -n postgres-ha get postgresql,pods,svc,pvc
```

**Expected:**

- Operator pod(s): `Running`
- CR: `acid-postgres-cluster`
- Pods: `acid-postgres-cluster-0`, `-1`, `-2` — all `Running`, `READY 1/1`
- Services: `acid-postgres-cluster`, `acid-postgres-cluster-repl`, `acid-postgres-cluster-config`
- PVCs: 3 bound

Patroni cluster state:

```bash
kubectl -n postgres-ha exec -it acid-postgres-cluster-0 -- patronictl list
```

Expected: one **Leader**, others **Replica**.

Run the prereq script again:

```bash
./scripts/check-prereqs.sh
```

---

## Phase F — Helm CLI fallback (debug only)

Use only if Argo CD is not ready. From `patroni-postgres-ha` repo root:

```bash
helm repo add postgres-operator-charts https://opensource.zalando.com/postgres-operator/charts/postgres-operator
helm repo update

helm upgrade --install postgres-operator postgres-operator-charts/postgres-operator \
  -n postgres-operator --create-namespace

helm upgrade --install postgres-ha ./helm-charts/postgres-ha \
  -n postgres-ha --create-namespace \
  -f ./helm-charts/postgres-ha/values-local.yaml
```

Prefer switching to Argo CD Applications under `envs/local/` afterward so git remains the source of truth.

---

## Failover smoke test (optional)

```bash
kubectl -n postgres-ha exec -it acid-postgres-cluster-0 -- patronictl list
# note the Leader pod name

kubectl -n postgres-ha delete pod <leader-pod-name>

kubectl -n postgres-ha exec -it acid-postgres-cluster-0 -- patronictl list
# confirm a new Leader was elected
```

---

## Reset (lab only — destroys data)

```bash
helm -n postgres-ha uninstall postgres-ha
kubectl -n postgres-ha delete pvc --all
```

Or delete the Argo CD Application from the UI/CLI.

---

## Troubleshooting

| Symptom | Check |
|---------|--------|
| Argo CD repo connection failed | Deploy key on GitHub, Secret applied, `argocd repo list` |
| `postgres-ha-local` Degraded | Operator synced first? StorageClass set? `kubectl -n postgres-ha describe pod` |
| PVC Pending | `kubectl get storageclass`; fix `values-local.yaml` |
| Apps OutOfSync | `git push`; `argocd app sync <name>` |
| CRD missing | Sync `postgres-operator-local` (wave 0) first |

Operator logs:

```bash
kubectl -n postgres-operator logs -l app.kubernetes.io/name=postgres-operator --tail=50
```

Postgres pod logs:

```bash
kubectl -n postgres-ha logs acid-postgres-cluster-0 --tail=50
```

---

## Checklist

- [ ] Argo CD installed — `kubectl -n argocd get pods`
- [ ] Git repo registered — Successful in Argo CD UI
- [ ] `values-local.yaml` storageClass matches cluster
- [ ] `postgres-operator-local` Synced / Healthy
- [ ] `postgres-ha-local` Synced / Healthy
- [ ] 3 Postgres pods Running, PVCs bound
- [ ] `patronictl list` shows Leader + Replicas

When all pass → [Connect to Patroni HA](../04-database/connect-to-patroni-ha.md)

## Related docs

- [Patroni HA README](../../../../../scripts/patroni-postgres-ha/README.md)
- [PHASES checklist](../../../../../scripts/patroni-postgres-ha/PHASES.md)
- [CONNECT-REPO.md](../../../../../scripts/patroni-postgres-ha/argocd/CONNECT-REPO.md)
