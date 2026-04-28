+++
title = "Containers vs Virtual Machines"
program = "CLO"
cohort = "25"
courses = ["ACD"]
type = "slide"
date = 2026-04-28
draft = false
hidden = true

theme = "sky"
[revealOptions]
controls = true
progress = true
history = true
center = true
+++

## Containers vs Virtual Machines
Part VII — Containers

---

## The "works on my machine" problem
- Same code, different environment, different result
- Drift hides in **OS patches**, system libraries, runtime versions
- Reproducibility is an engineering cost, not just a cliché
- Containers and VMs both attack the problem — at different layers

---

## How a VM virtualizes hardware
- A **hypervisor** sits between hardware and guests
- Each VM runs its own **kernel** and full operating system
- Image size in gigabytes, boot time in tens of seconds
- Strong isolation: no kernel sharing across VMs

---

## How a container isolates a process
- One **container runtime**, one shared host kernel
- **Namespaces** hide other processes, networks, mounts
- **cgroups** cap CPU and memory per container
- The application is the only thing that starts

---

## What each model gives up and gains
- VM: stronger **isolation**, full OS flexibility, heavier footprint
- Container: lighter, faster, but tied to the host kernel family
- VMs portable across hypervisors; containers across hosts with the same kernel ABI
- A kernel CVE is a fleet-wide problem for containers

---

## Density and startup time
- A VM cold-starts in 30 s to minutes; a container in milliseconds
- 10 VMs per host vs 100+ containers on the same hardware
- Each VM permanently carries its own kernel and OS services
- Each container carries only the application's working set

---

## Worked example: dotnet run
- `dotnet run` ready to serve in ~200 ms
- Same app in a container: ~200 ms (runtime overhead negligible)
- Same app as a fresh VM: 2–3 minutes wall-clock
- The application is identical — the surrounding stack is not

---

## When each model fits
- VM: legacy Windows app, regulated isolation, kernel-tuned databases
- Container: stateless web service, queue worker, scheduled job
- Cloud platforms compose both: containers run **on** VMs
- Choose the trade-off, not the trend
- Cross-link: `/exercises/20-docker/`

---

## Questions?
