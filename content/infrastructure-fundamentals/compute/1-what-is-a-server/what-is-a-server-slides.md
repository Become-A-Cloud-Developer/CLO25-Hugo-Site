+++
title = "What is a Server?"
program = "CLO"
cohort = "25"
courses = ["BCD"]
type = "slide"
date = 2024-11-17
draft = false
hidden = true

theme = "sky"
[revealOptions]
controls= true
progress= true
history= true
center= true
+++

## What is a Server?
- Servers provide the computing power behind applications, websites databases and much more.
- A server processes client requests and delivers services across a network.
---
## Key Characteristics
- **Processing Power**: Equipped with CPU, memory, and storage to execute tasks.
- **Networking**: Enables communication with clients and other services.
- **Service-Oriented**: Provides applications, databases, file storage, etc to users.
---
## Types of Servers
- While traditionally physical, servers now include virtual machines, containers, and serverless functions.
---
## Physical Servers
(Bare-Metal)
- Dedicated hardware running applications and storing data.
- Provides full control and performance but requires more management.
- Examples: AWS Bare Metal Instances, Azure BareMetal Servers.
---
## Virtual Machines
(VMs)
- Software-based emulation running on a physical host.
- Allows multiple isolated environments on shared hardware, increasing efficiency.
- Examples: AWS EC2 Instances, Azure VMs, Google Cloud Compute Engine.
---
## Containers
(Docker)
- Lightweight, portable environments for running applications.
- Share the host's OS but keep processes isolated for efficiency.
- Ideal for microservices architectures.
- Examples: AWS ECS Tasks, Azure Container Instances, GKE Pods.
---
## Serverless Functions
- Abstract the need for infrastructure management.
- Code is event-triggered and managed by cloud providers.
- Suitable for scalable tasks with minimal overhead.
- Examples: AWS Lambda, Azure Functions, Google Cloud Functions.
---
## Choosing the Right Type
- **Physical Servers**: Offer maximum control, suitable for specialized needs.
- **Virtual Machines**: Provide flexibility and isolation without the physical hardware overhead.
- **Containers**: Efficient, scalable, ideal for modern, distributed applications.
- **Serverless**: Eliminates infrastructure concerns, focusing on code and rapid scaling.
---
## Factors to Consider
- **Performance**: High-demand tasks favor physical servers or VMs.
- **Scalability**: Containers and serverless functions excel in handling varying workloads.
- **Cost**: Serverless options offer pay-as-you-go models; VMs provide predictable pricing.
- **Control**: Physical servers and VMs offer more environmental control, critical for specific applications.
