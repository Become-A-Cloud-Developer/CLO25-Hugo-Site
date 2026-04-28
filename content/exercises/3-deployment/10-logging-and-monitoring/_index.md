+++
title = "10. Logging and Monitoring"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Three exercises on observability for a cloud-deployed .NET app: structured logging with ILogger<T>, centralised container logs in Log Analytics queryable via Kusto, and Application Insights for requests, exceptions, and custom metrics. The lab tears down at the end."
weight = 10
+++

# Logging and Monitoring

The goal of this chapter is to give you eyes on a running cloud app. The same .NET MVC app travels through all three exercises, gaining one observability layer at a time — each layer answers a question the previous one cannot.

The arc moves from local intentional logging, to centralised queryable logs, to APM-style telemetry:

- **First**, replace ad-hoc framework logging with intentional, structured **`ILogger<T>`** calls. Message templates capture fields, not frozen strings. Filter levels per category. The output stays local — `dotnet run` and `docker run`.
- **Second**, push the running container's stdout to a **Log Analytics** workspace and query it with **Kusto Query Language**. Add a request correlation ID. Scale to two replicas to feel the value of centralised logs over `docker logs`.
- **Third**, layer **Application Insights** on top of the same workspace. Get Live Metrics, an Application Map, a Failures blade for exceptions, and a custom metric. End by tearing down the entire lab — the resource group plus the Entra app registration left over from the previous chapter.

> ℹ **Where this fits**
>
> This subsection sits inside the broader **Deployment** chapter. The previous subsection got the app deployed and continuously delivered; this one gives you eyes on it once it's running. Observability is what lets you debug *production* issues without redeploying — the natural follow-on to CI/CD.

{{< children />}}
