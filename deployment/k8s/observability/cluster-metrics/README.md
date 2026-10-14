# Cluster metrics (Phase 7 Step 3)

Lightweight exporters → scraped by Alloy DaemonSet → Mimir.

## Install

```bash
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update

helm upgrade --install node-exporter prometheus-community/prometheus-node-exporter \
  -n observability -f values-node-exporter.yaml --wait

helm upgrade --install kube-state-metrics prometheus-community/kube-state-metrics \
  -n observability -f values-kube-state-metrics.yaml --wait
```

PowerShell: see `install.ps1` root observability script (cluster-metrics section).

## Series (examples)

| Job / source | Metrics |
|--------------|---------|
| node-exporter | `node_cpu_seconds_total`, `node_memory_MemAvailable_bytes` |
| kube-state-metrics | `kube_pod_status_phase`, `kube_node_status_condition`, `kube_deployment_status_replicas_*` |
| cadvisor (Alloy) | `container_cpu_usage_seconds_total`, `container_memory_working_set_bytes` |
