# Restart environment (day-2 cheat sheet)

Use this when you sit down to work. It assumes kubeadm, Argo CD, and Patroni HA are **already installed**. For first-time setup see the [docs index](../README.md).

Keep **two extra terminals** open after the checks: one for **Argo CD UI**, one for **Postgres**.

← [Connect to Patroni HA](../04-database/connect-to-patroni-ha.md) · [Docs index](../README.md)

---

## 0. kubectl context

```bash
kubectl config current-context
kubectl cluster-info
```

Expected API: `https://192.168.178.20:6443`.

If no context (on control plane `ih-entpr-tpad`):

```bash
mkdir -p ~/.kube
sudo cp /etc/kubernetes/admin.conf ~/.kube/config
sudo chown "$(id -u)":"$(id -g)" ~/.kube/config
```

---

## 1. Cluster checkup

```bash
kubectl get nodes -o wide
kubectl get pods -A
```

**Nodes — all Ready:**

| Node | Role |
|------|------|
| `ih-entpr-tpad` | control-plane |
| `tmkns-2-tbook` | worker |
| `tmkns-3-x550ln` | worker |

Anything stuck:

```bash
kubectl get pods -A --field-selector=status.phase!=Running
kubectl get storageclass
```

Optional prereq script:

```bash
cd /home/ih-entpr/Documents/projects/pers/scripts/patroni-postgres-ha
./scripts/check-prereqs.sh
```

---

## 2. Argo CD — UI password + port-forward

```bash
kubectl -n argocd get pods
kubectl -n argocd get applications
```

Expected apps **Synced / Healthy**:

- `postgres-operator-local`
- `postgres-ha-local`

### Password (admin)

```bash
kubectl -n argocd get secret argocd-initial-admin-secret \
  -o jsonpath='{.data.password}' | base64 -d; echo
```

Login: **`admin`** + that password.

### Port-forward UI (leave running)

```bash
kubectl -n argocd port-forward svc/argocd-server 8080:443
```

Open **https://localhost:8080** (accept the self-signed cert).

If 443 mapping fails on your install, try HTTP:

```bash
kubectl -n argocd port-forward svc/argocd-server 8080:80
```

---

## 3. Patroni HA checkup

```bash
kubectl -n postgres-operator get pods
kubectl -n postgres-ha get postgresql,pods,svc,pvc
```

**Expected:**

- Operator pod(s) `Running`
- CR: `acid-postgres-cluster`
- Pods: `acid-postgres-cluster-0`, `-1`, `-2` — `Running` / `READY 1/1`
- PVCs: Bound

---

## 4. Find the Patroni leader

```bash
kubectl -n postgres-ha exec -it acid-postgres-cluster-0 -- patronictl list
```

Look for **Role = Leader** (others **Replica**). Note the **Member** pod name, e.g. `acid-postgres-cluster-1`.

Non-interactive:

```bash
kubectl -n postgres-ha exec acid-postgres-cluster-0 -- patronictl list
```

---

## 5. Database password

Do **not** commit this. Operator secret:

```text
events_platform.acid-postgres-cluster.credentials.postgresql.acid.zalan.do
```

```bash
kubectl -n postgres-ha get secret \
  events_platform.acid-postgres-cluster.credentials.postgresql.acid.zalan.do \
  -o jsonpath='{.data.username}' | base64 -d; echo

kubectl -n postgres-ha get secret \
  events_platform.acid-postgres-cluster.credentials.postgresql.acid.zalan.do \
  -o jsonpath='{.data.password}' | base64 -d; echo
```

| Field | Value |
|-------|--------|
| Database | `events_platform` |
| User | `events_platform` |
| Host | `localhost` (after port-forward) |
| Port | `5432` (or `5433` if local 5432 is busy) |
| SSL | Disable |

---

## 6. Port-forward Postgres (leader **pod**, not Service)

Zalando’s primary Service (`acid-postgres-cluster`) has **no selector**. This fails:

```bash
kubectl -n postgres-ha port-forward svc/acid-postgres-cluster 5432:5432
# error: Service is defined without a selector
```

Forward the **Leader pod** instead (leave running):

```bash
# replace with the Leader from patronictl list
kubectl -n postgres-ha port-forward pod/acid-postgres-cluster-0 5432:5432
```

If local 5432 is taken:

```bash
kubectl -n postgres-ha port-forward pod/acid-postgres-cluster-0 5433:5432
```

Quick leader + forward (if `jq` is installed):

```bash
LEADER=$(kubectl -n postgres-ha exec acid-postgres-cluster-0 -- \
  patronictl list -f json 2>/dev/null \
  | jq -r '.[] | select(.Role=="Leader") | .Member' | head -1)
echo "Leader: $LEADER"
kubectl -n postgres-ha port-forward "pod/$LEADER" 5432:5432
```

Test in another terminal:

```bash
psql "host=localhost port=5432 dbname=events_platform user=events_platform password=YOUR_PASSWORD sslmode=disable"
```

```sql
SELECT current_user, current_database();
SELECT COUNT(*) FROM "Events";
\q
```

---

## 7. Run EventsPlatform API (optional)

With Postgres port-forward active:

```bash
cd /home/ih-entpr/Documents/projects/pers/fairmas/dotnet-practice/EventsPlatform-DotNet-React
dotnet run --project API
```

API: `https://localhost:5001`. Connection string in user secrets or `API/appsettings.Development.json` must match host/port/password from steps 5–6.

---

## Terminal layout

```text
Terminal 1  kubectl get nodes / apps   (checks, then free)
Terminal 2  argocd port-forward 8080   KEEP OPEN
Terminal 3  postgres pod port-forward  KEEP OPEN
Terminal 4  psql / Rider / dotnet run
```

---

## Copy-paste sequence

```bash
# 1 cluster
kubectl get nodes -o wide
kubectl -n argocd get applications
kubectl -n postgres-ha get pods

# 2 Argo CD password + UI
kubectl -n argocd get secret argocd-initial-admin-secret \
  -o jsonpath='{.data.password}' | base64 -d; echo
kubectl -n argocd port-forward svc/argocd-server 8080:443

# 3 Patroni leader (other terminal)
kubectl -n postgres-ha exec acid-postgres-cluster-0 -- patronictl list

# 4 DB password
kubectl -n postgres-ha get secret \
  events_platform.acid-postgres-cluster.credentials.postgresql.acid.zalan.do \
  -o jsonpath='{.data.password}' | base64 -d; echo

# 5 Postgres (Leader pod — other terminal)
kubectl -n postgres-ha port-forward pod/acid-postgres-cluster-0 5432:5432
```

---

## If something is down

| Symptom | Check |
|---------|--------|
| Nodes NotReady | [Verify kubeadm](../03-infrastructure/verify-kubeadm-cluster.md) |
| Argo CD apps Degraded | [Argo CD GitOps](../03-infrastructure/argocd-gitops-patroni-ha.md) |
| Postgres pods Pending | StorageClass / PVCs |
| `port-forward svc/...` no selector | Use **pod** of the Leader (step 6) |
| `password authentication failed` | Re-read the secret (cluster recreate rotates it) |
| Port already allocated | Use `5433:5432` or `8081:443` |

---

## Related

- [Verify kubeadm cluster](../03-infrastructure/verify-kubeadm-cluster.md)
- [Argo CD GitOps](../03-infrastructure/argocd-gitops-patroni-ha.md)
- [Connect to Patroni HA](../04-database/connect-to-patroni-ha.md)
