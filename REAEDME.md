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
    │  ├─ Program.css
    │  ├─ ordersvc.csproj
    │  └─ Dockerfile
    └─ paymentsvc/
        ├─ Program.cs
        ├─ paymentsvc.csproj
        └─ Dockerfiles
```

## 日志说明

### 字段映射

| 字段          | 来源           | 说明                                  |
| ------------- | -------------- | ------------------------------------- |
| scope.name    | Logger的Source | 对应到.Net Logger的Source，通常是类名 |
| severity_text | Level          | Information、Warn、Error等            |

## 请求链路

```mermaid
flowchart TD
    subgraph 上游服务
        UReq[HTTP 请求\nHeader: traceparent]
    end

    subgraph ASP.NET Core 应用
        A[ASP.NET Core Middleware]
        AI[AspNetCoreInstrumentation:<br/>Extract 上下文]
        ACT[System.Diagnostics.Activity\nTraceId/SpanId/ParentId]
        UserCode[你的业务代码]
        HC[HttpClientInstrumentation:<br/>Inject 上下文]
    end

    subgraph 下游服务
        DReq[HTTP 请求<br/>Header: traceparent]
    end

    UReq --> A --> AI --> ACT --> UserCode --> HC --> DReq
```
