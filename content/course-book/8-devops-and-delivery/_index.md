+++
title = "Part VIII — DevOps and Delivery"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Continuous integration and delivery: the DevOps philosophy, CI vs CD, pipelines as code, build and test gates, deployment strategies, OIDC federation for pipeline identity, and Azure Container Apps as a deployment target."
weight = 80
chapter = true
head = "<label>Part VIII</label>"
+++

# Part VIII — DevOps and Delivery

The Part that closes the loop between writing code and running it. Seven chapters cover the philosophy that motivates DevOps, the practices it relies on (CI, CD, pipelines as code, gated tests), the operational choices that make releases safe (deployment strategies, federated pipeline identity), and one concrete deployment target — Azure Container Apps — that ties the previous Parts together.

The companion exercise is the [CI/CD to Container Apps chapter](/exercises/3-deployment/9-cicd-to-container-apps/), three exercises that progress from public registry + manual deploy through private registry + automated deploy to passwordless OIDC federation.

{{< children />}}
