# Verify kubeadm cluster

Confirm your homelab **Kubernetes cluster** is healthy before deploying Patroni HA via Argo CD.

← [EF Core & migrations](../02-data-layer/ef-core-and-migrations.md) · [Docs index](../README.md) · Next → [Argo CD GitOps](argocd-gitops-patroni-ha.md)

## What you are checking

| Component | Location |
|-----------|----------|
| kubeadm cluster | [`scripts/cluster/`](../../../../../scripts/cluster/) |
| Control plane | `ih-entpr-tpad` — `192.168.178.20` |
| Workers | `tmkns-2-tbook`, `tmkns-3-x550ln` |
| CNI | Flannel (`10.244.0.0/16`) |
| CoreDNS | `10.96.0.10` |

Full cluster rebuild guide: [`scripts/cluster/README.md`](../../../../../scripts/cluster/README.md)

---

## Prerequisites

```bash
kubectl version --client
kubectl config current-context
```

If no context is configured, copy kubeconfig from the control plane:

```bash
# on control plane (ih-entpr-tpad), as your user:
mkdir -p ~/.kube
sudo cp /etc/kubernetes/admin.conf ~/.kube/config
sudo chown "$(id -u)":"$(id -g)" ~/.kube/config
```

---

## Step 1 — Cluster info

```bash
kubectl config current-context
kubectl cluster-info
```

Expected: API server reachable (e.g. `https://192.168.178.20:6443`).

---

## Step 2 — All nodes Ready

```bash
kubectl get nodes -o wide
```

**Expected:** 3 nodes, all `STATUS=Ready`:

| Node | Role |
|------|------|
| `ih-entpr-tpad` | control-plane |
| `tmkns-2-tbook` | worker |
| `tmkns-3-x550ln` | worker |

If any node is `NotReady`:

```bash
kubectl describe node <node-name>
kubectl get pods -A -o wide | grep -v Running
```

---

## Step 3 — System pods running

```bash
kubectl get pods -A
```

**Expected:** Running pods in `kube-system`:

- CoreDNS
- Flannel (or your CNI)
- kube-proxy

Quick filter for problems:

```bash
kubectl get pods -A --field-selector=status.phase!=Running
```

---

## Step 4 — DNS sanity check

```bash
kubectl run dnstest --rm -it --restart=Never --image=busybox:1.36 -- \
  nslookup kubernetes.default.svc.cluster.local
```

Expected: resolves to the ClusterIP of the `kubernetes` service.

---

## Step 5 — StorageClass (required for Postgres PVCs)

Patroni HA needs persistent volumes. List available storage classes:

```bash
kubectl get storageclass
```

Note the name you will use (e.g. `local-path`). You will set it in:

```text
scripts/patroni-postgres-ha/helm-charts/postgres-ha/values-local.yaml
```

If no StorageClass exists, install one (e.g. local-path provisioner) before proceeding.

---

## Step 6 — Optional tools

```bash
helm version
kubectl get ns argocd 2>/dev/null || echo "Argo CD not installed yet"
```

Helm is optional for day-to-day GitOps (Argo CD deploys Helm charts), but useful for debugging.

---

## Step 7 — Patroni prereq script

From the patroni-postgres-ha repo:

```bash
cd ../../../../../scripts/patroni-postgres-ha
./scripts/check-prereqs.sh
```

This script checks (read-only):

- `kubectl` and optional `helm`
- Current context
- Nodes Ready
- StorageClasses
- Argo CD namespace (warns if missing)
- Zalando `postgresqls.acid.zalan.do` CRD (warns if missing — normal before deploy)

---

## Day-2 cheat sheet

```bash
kubectl get nodes -o wide
kubectl get pods -A
ssh tmkns-2 'hostname'
ssh tmkns-3 'hostname'

# new worker join token (on control plane)
sudo kubeadm token create --print-join-command
```

---

## Checklist before Argo CD deploy

- [ ] `kubectl get nodes` — all Ready
- [ ] `kubectl get pods -A` — no stuck system pods
- [ ] DNS test passes
- [ ] At least one StorageClass available
- [ ] `./scripts/check-prereqs.sh` — no FAIL lines

When all pass → [Argo CD GitOps — Patroni HA](argocd-gitops-patroni-ha.md)

## Related docs

- [Cluster README](../../../../../scripts/cluster/README.md)
- [Patroni HA PHASES](../../../../../scripts/patroni-postgres-ha/PHASES.md)
