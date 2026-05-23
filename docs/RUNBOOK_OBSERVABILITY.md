# Runbook: observability stack

**Status:** Phase 4 ✅ complete (LGTM+ in cluster). Install/upgrade: [deployment/k8s/observability/README.md](../deployment/k8s/observability/README.md).

## Namespace

`observability` — Grafana, Mimir, Loki, Tempo, Pyroscope, Alloy, MinIO, OpenMaxIO Console.

## Access Grafana

| Method | URL |
|--------|-----|
| Ingress (demo) | `https://demo-grafana.1node.tech` |
| Port-forward | `kubectl -n observability port-forward svc/grafana 3000:80` → http://localhost:3000 |

Admin credentials: Secret `grafana-admin` (`admin-user`, `admin-password`). See [deployment/k8s/observability/grafana/secret.yaml](../deployment/k8s/observability/grafana/secret.yaml).

```bash
kubectl -n observability get secret grafana-admin -o jsonpath='{.data.admin-password}' | base64 -d
```

## Access OpenMaxIO (MinIO Console)

Community UI for MinIO S3 — [OpenMaxIO/openmaxio-object-browser](https://github.com/OpenMaxIO/openmaxio-object-browser) / [huncrys/minio-console](https://github.com/huncrys/minio-console).

| Method | URL |
|--------|-----|
| Ingress (demo) | `https://demo-openmaxio.1node.tech` |
| Port-forward | `kubectl -n observability port-forward svc/openmaxio 9002:9002` → http://localhost:9002 |

Login: **MinIO credentials** from Secret `minio-credentials` (`MINIO_ROOT_USER`, `MINIO_ROOT_PASSWORD`).

Console session encryption: Secret `openmaxio-console` (`CONSOLE_PBKDF_PASSPHRASE`, `CONSOLE_PBKDF_SALT`) — see [openmaxio/secret.yaml](../deployment/k8s/observability/openmaxio/secret.yaml).

## Internal service endpoints

| Backend | Cluster DNS |
|---------|-------------|
| Mimir (Prometheus API) | `http://mimir-gateway.observability.svc:80/prometheus` |
| Mimir remote_write | `http://mimir-gateway.observability.svc:80/api/v1/push` |
| Loki | `http://loki-gateway.observability.svc:80` |
| Tempo | `http://tempo.observability.svc:3200` |
| Pyroscope | `http://pyroscope.observability.svc:4040` |

## Alloy OTLP gateway (Phase 5 apps)

Applications send OTLP to:

```text
alloy-gateway.observability.svc:4317   # gRPC
alloy-gateway.observability.svc:4318   # HTTP
```

Phase 4: gateway listens on OTLP; signals dropped until Phase 5. Phase 5: update Alloy config from [observability/alloy/config.alloy](../observability/alloy/config.alloy).

## Health checks

1. **Pods:** `kubectl -n observability get pods` — all Running/Completed (MinIO bucket job).
2. **Grafana:** `curl http://localhost:3000/api/health` (via port-forward).
3. **Connections:** Grafana → **Connections** → **Data sources** — Mimir, Loki, Tempo healthy.
4. **Alloy:** `kubectl -n observability get pods -l app.kubernetes.io/instance=alloy` and `...=alloy-gateway`.
5. **Pyroscope:** port-forward `svc/pyroscope 4040:4040` or query via Grafana Pyroscope datasource.

## Install / upgrade

```bash
./deployment/k8s/observability/install.sh
```

**Tempo** and **Grafana** use [grafana-community/helm-charts](https://github.com/grafana-community/helm-charts) (`grafana-community/tempo`, `grafana-community/grafana`), not deprecated charts in `grafana/helm-charts`. Repo: `helm repo add grafana-community https://grafana-community.github.io/helm-charts`.

Upgrade single component:

```bash
helm upgrade mimir grafana/mimir-distributed -n observability \
  -f deployment/k8s/observability/helm/values-mimir.yaml
```

## Phase 4 vs 5

| Phase | Scope |
|-------|--------|
| **4** ✅ | LGTM+ infra, placeholder Alloy, Grafana datasources |
| **5** | OTel in Umbraco/Next.js, Faro, full Alloy River config, profiles push |

### Phase 5 rollout (after merging instrumentation)

1. **Alloy** — upgrade gateway + DaemonSet with River config from [observability/alloy/](../../observability/alloy/):

```powershell
helm upgrade alloy-gateway grafana/alloy -n observability `
  -f deployment/k8s/observability/helm/values-alloy-gateway.yaml --wait
helm upgrade alloy grafana/alloy -n observability `
  -f deployment/k8s/observability/helm/values-alloy-daemonset.yaml --wait
kubectl apply -f deployment/k8s/observability/alloy/gateway-service.yaml
```

2. **Apps** — rebuild/push CMS and web images (OTel + Pyroscope + Faro), then apply ConfigMaps and roll Deployments in `demo-services`.

3. **Verify** (Grafana Explore):
   - Tempo: `{ resource.service.name = "umbraco-web" }` → child span to `umbraco-cms`
   - Loki: CMS JSON with `trace_id` — `{service_name="demo-umbraco"} | json` (label from pod `app.kubernetes.io/name`, not OTel `service.name`)
   - Mimir: `traces_spanmetrics_latency_bucket` or OTel HTTP metrics for both services
   - Pyroscope: `umbraco-cms` / `umbraco-web` profiles after a few page loads
   - Faro: sessions after opening the public web UI (collector via `https://<web>/api/faro` → alloy-gateway:12347)

**Endpoints (in-cluster):**

| Signal | App env | Collector |
|--------|---------|-----------|
| OTLP gRPC | `OTEL_EXPORTER_OTLP_ENDPOINT=http://alloy-gateway.observability.svc.cluster.local:4317` | alloy-gateway:4317 |
| Faro | Browser → `/api/faro` → `FARO_COLLECTOR_UPSTREAM` | alloy-gateway:12347 |
| Pyroscope .NET | `PYROSCOPE_SERVER_ADDRESS` (native profiler in image) | pyroscope:4040 |
| Pyroscope Node | `PYROSCOPE_SERVER_ADDRESS` in `instrumentation.node.ts` | pyroscope:4040 |
| Pod logs | stdout JSON (CMS) | Alloy DaemonSet → Loki |

## Loki: which labels exist?

`loki.source.kubernetes` does **not** use OTel `service.name`. Until relabel is deployed, streams are often only:

```logql
{job="loki.source.kubernetes.demo_pods"}
```

After DaemonSet relabel (see `config-daemonset.alloy`):

```logql
{namespace="demo-services", service_name="demo-umbraco"}
{namespace="demo-services", service_name="demo-umbr-fe"}
```

CMS Serilog JSON + `trace_id` (do not require `| json` until lines appear):

```logql
{service_name="demo-umbraco"} |= "trace_id"
```

**Helm:** `values-alloy-daemonset.yaml` must match `observability/alloy/config-daemonset.alloy`. If they diverge, `helm upgrade` keeps an old inline config and `namespace`/`pod` queries return nothing.

**Diagnostics:**

```bash
kubectl -n observability get pods -l app.kubernetes.io/name=alloy
kubectl -n observability logs ds/alloy --tail=30
kubectl -n demo-services logs deploy/demo-umbraco --tail=5
```

In Grafana → Loki → Label browser: start from `job`, then `service_name` / `pod` / `namespace`.

## Mimir: find real metric names

OTel → Prometheus names rarely match `http_server_request_duration_seconds_count`. Discover first:

```promql
topk(20, count by (__name__) ({__name__=~".*http.*"}))
count by (__name__, service_name) ({__name__=~".*http.*"})
```

Use the `__name__` and `service_name` values from that table, e.g.:

```promql
rate(<exact_name_from_table>{service_name="<exact_value>"}[5m])
```

Span metrics (if Tempo metrics generator is on):

```promql
sum(rate(traces_spanmetrics_calls_total{service=~"umbraco.*"}[5m])) by (service)
```

## Troubleshooting

| Symptom | Check |
|---------|--------|
| Loki: no logs with `namespace`/`pod` | Query `{job="loki.source.kubernetes.demo_pods"}`; sync + `helm upgrade alloy` DaemonSet config |
| mimir-gateway / loki-gateway nginx: `host not found in resolver "kube-dns..."` | Set `global.dnsService` to your cluster DNS Service (`kubectl get svc -n kube-system`). **RKE2:** `rke2-coredns-rke2-coredns` (see [values-mimir.yaml](../deployment/k8s/observability/helm/values-mimir.yaml)). Then `helm upgrade mimir ...` |
| Loki install: `more than zero replicas` for single binary and scalable | In [values-loki.yaml](../deployment/k8s/observability/helm/values-loki.yaml) set `write`/`read`/`backend` `replicas: 0` with `singleBinary.replicas: 1` |
| Tempo: `Access Key Id ... does not exist` | Chart must set `tempo.extraArgs.config.expand-env: "true"` and `tempo.extraEnv` from `minio-credentials` ([values-tempo.yaml](../deployment/k8s/observability/helm/values-tempo.yaml)); Secret must match running MinIO |
| Tempo/Grafana deprecated chart WARN | Use `grafana-community/tempo` / `grafana-community/grafana`; uninstall old release if upgrade fails |
| Alloy: `otelcol.exporter.debug` experimental | Phase 5 uses wired exporters in [values-alloy-gateway.yaml](../deployment/k8s/observability/helm/values-alloy-gateway.yaml); do not use debug exporter with `stabilityLevel: generally-available` |
| Alloy gateway CrashLoop after Phase 5 | `kubectl logs -n observability deploy/alloy-gateway` — fix River syntax; validate against [config.alloy](../../observability/alloy/config.alloy) |
| No traces in Tempo | Apps `OTEL_EXPORTER_OTLP_ENDPOINT` → `alloy-gateway:4317`; gateway pods Running; generate traffic to web + CMS |
| Faro empty | Browser network tab POST to `/api/faro`; `FARO_COLLECTOR_URL` on web pod; alloy-gateway port 12347 |
| Mimir/Loki CrashLoop | MinIO buckets exist (`job/minio-create-buckets`); secret keys match values files |
| Datasource error in Grafana | `kubectl -n observability get svc` — gateway service names match [values-grafana.yaml](../deployment/k8s/observability/helm/values-grafana.yaml) |
| Alloy gateway no OTLP | `kubectl -n observability get svc alloy-gateway` ports 4317/4318; apply [gateway-service.yaml](../deployment/k8s/observability/alloy/gateway-service.yaml) |
| Grafana 404 via ingress | DNS `demo-grafana.1node.tech` → nginx → Traefik; IngressRoute applied in `observability` namespace |
| OpenMaxIO blank / login fail | `CONSOLE_MINIO_SERVER` must reach `minio:9000`; check `openmaxio-console` secret; use MinIO root credentials |

See also [MONITORING_STACK.md](./MONITORING_STACK.md).
