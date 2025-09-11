## 疑问

- OTel Collector是什么

```pre
.
├─ docker-compose.yml
├─ otel-collector.yaml
├─ prometheus.yml
├─ grafana-provisioning/
│   ├─ datasources.yaml
│   └─ dashboards/
│       └─ red.json
├─ dapr/
│   ├─ components/
│   │ ├─ pubsub.yaml
│   │ └─ statestore.yaml
│   └─ config/
│       └─ tracing.yaml
└─ apps/
    ├─ gateway/
    │  ├─ Program.cs
    │  ├─ gateway.csproj
    │  └─ Dockerfile
    ├─ ordersvc/
    │  ├─ Program.cs
    │  ├─ ordersvc.csproj
    │  └─ Dockerfile
    └─ paymentsvc/
        ├─ Program.cs
        ├─ paymentsvc.csproj
        └─ Dockerfiles
```