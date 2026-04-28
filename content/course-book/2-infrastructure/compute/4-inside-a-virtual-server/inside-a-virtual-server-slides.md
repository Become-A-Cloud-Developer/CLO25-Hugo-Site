+++
title = "Inside a Virtual Server"
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

## Inside a Virtual Server
- Virtual servers offer scalability and flexibility beyond physical hardware.
- Components emulate physical resources but are managed by a hypervisor.
---
## Core Components of a Virtual Server
- **vCPU**: Shares the host's physical CPU resources.
- **Virtual RAM**: Allocated memory from the host's physical RAM.
- **Virtual Storage**: Virtual disks appear as independent drives.
- **vNIC**: Connects the virtual server to networks.
- **Hypervisor**: Software enabling resource virtualization.
---
## Virtual CPUs (vCPUs)
- Emulates a physical CPU, sharing resources with other VMs.
- Managed by the hypervisor to ensure efficient task execution.
---
## Virtual RAM
- Allocated portion of the physical server's memory.
- Provides isolated and reliable memory for VMs.
---
## Virtual Storage
- Virtual disks stored as files on the host's physical drives.
---
## Virtual Network Interface (vNIC)
- Provides network connectivity to the VM.
- Managed to ensure secure and isolated communication.
---
## Hypervisor
- Manages the virtualization of hardware resources.
- **Type 1**: Runs directly on physical hardware.
- **Type 2**: Runs on top of an operating system.
---
## Differences: Physical vs. Virtual Servers
- **Resource Sharing**: Physical servers dedicate resources; virtual servers share them.
- **Scalability**: Virtual servers can scale resources dynamically.
- **Cost**: Virtual servers lower costs with shared infrastructure.
- **Management**: Virtual servers simplify management by outsourcing to cloud providers.
---
## Components in Azure VMs
(Examples)
- **vCPU and RAM**: Choose configurations based on workload (e.g., B-series for burstable workloads).
- **Virtual Disks**: Azure Managed Disks for optimized storage.
- **Networking**: Utilize VNets, NSGs, and Azure networking tools.
- **Management Tools**: Use Azure Monitor and ARM templates for automation and monitoring.
