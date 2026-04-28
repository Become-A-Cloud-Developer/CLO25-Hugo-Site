+++
title = "Containers vs Virtual Machines"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 10
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/7-containers/1-containers-vs-vms.html)

[Se presentationen på svenska](/presentations/course-book/7-containers/1-containers-vs-vms-swe.html)

---

An application that runs cleanly on a developer's laptop often fails on the test server, then fails again on the production host. The cause is rarely the code — it is the surrounding environment: a different operating system patch level, a missing system library, an interpreter at version 6.0.404 instead of 6.0.418, an environment variable nobody documented. The phrase "works on my machine" describes a real engineering cost: hours of debugging that produce no new functionality. Containers attack this problem at its root by packaging the application together with the exact runtime environment it requires, so the same artifact runs identically wherever it lands. The sections below develop how containers achieve that property, how they differ from the older virtual-machine approach to the same problem, and when each model is the right choice.

## The reproducibility problem

Two strategies have dominated server-side reproducibility. Both isolate workloads from one another and from the host, but they isolate at different layers of the stack and pay very different costs to do so.

The older strategy, **virtualization**, emulates an entire computer in software. Each workload runs on its own simulated hardware with its own operating system. The newer strategy, **containerization**, isolates processes within a single shared operating system, packaging only what the application itself needs. Both work; they just sit at different points on the trade-off curve between isolation strength and runtime overhead.

Understanding the difference begins with revisiting how a virtual machine builds its illusion of a separate computer.

## How a virtual machine virtualizes hardware

A [virtual machine](/course-book/2-infrastructure/compute/4-inside-a-virtual-server/) (VM) emulates a physical computer in software, complete with its own operating system, its own kernel, its own simulated CPU, memory, disk, and network interface. The component that creates this illusion is the **hypervisor** — a layer that sits between physical hardware and the virtual machines, allocating real CPU time, real memory pages, and real I/O bandwidth to each VM while keeping them strictly isolated from each other.

Part II treats the VM layer in depth, including the distinction between Type 1 (bare-metal) and Type 2 (hosted) hypervisors and how virtual CPUs, memory, and disks map to physical resources. For the present discussion, two consequences matter.

First, every VM carries the weight of a complete operating system. A minimal Linux VM image is hundreds of megabytes; a Windows Server VM is several gigabytes. Booting that VM means booting the kernel, initialising device drivers, starting system services, and only then launching the application. Startup time is measured in tens of seconds at best, often minutes.

Second, isolation is hardware-grade. Two VMs on the same host share nothing above the hypervisor: not the kernel, not the filesystem, not the process table. A kernel exploit inside one VM does not directly compromise another, because each VM runs its own kernel. This is the strongest isolation a single physical host can provide.

## How a container isolates processes on a shared kernel

A **container** is a lightweight, isolated process running on a shared kernel; it bundles an application and its dependencies into a standardized unit that starts in milliseconds, provides process and network isolation, and runs identically across development, testing, and production environments. The crucial phrase is *shared kernel*. There is no second operating system inside the container — only the application, its libraries, and the files it expects. The container reuses the kernel of the host machine.

What stops a container from seeing the rest of the host? Two Linux kernel features do the work, and a third executes them.

**Isolation** in containers is the kernel-enforced separation of processes, filesystems, and networks from the host and other containers; process namespaces hide other processes, network namespaces provide separate network stacks, and cgroups limit CPU and memory to prevent one container from starving others. *Namespaces* are the kernel's way of giving a process its own private view of system resources: its own process tree, its own network interfaces, its own mount points, its own user IDs. Inside the container, process ID 1 looks like the only process on the system, even though dozens of other processes are running on the host. *Cgroups* (control groups) are a separate kernel mechanism that places hard ceilings on resource consumption — this container may use at most 0.5 CPU and 512 MB of memory, period. Together, namespaces hide and cgroups limit.

A **container runtime** is the software that executes containers on a host machine; Docker is the most common runtime, using the Linux kernel's containerization features (namespaces and cgroups) to enforce isolation without the overhead of a full virtual machine. The runtime is what reads an image, sets up the namespaces and cgroups, mounts the container's filesystem, and starts the application process. The kernel does the actual isolation work; the runtime orchestrates it.

This architectural choice — reusing the host kernel rather than booting a new one — is the single decision that drives every other difference between containers and VMs. **Kernel sharing** is the architectural difference between containers and virtual machines: containers all use the same host operating system kernel (but isolated via namespaces), whereas virtual machines each run their own full kernel, trading isolation for size and startup overhead.

## What each model gives up and gains

Sharing the kernel makes containers small and fast. There is no second operating system to ship, so a typical container image is tens of megabytes rather than gigabytes, and starting it means starting a single process rather than booting a kernel. The cost is that isolation is now software-enforced inside one shared kernel rather than hardware-enforced across two separate ones. A serious kernel vulnerability is a problem for every container on the host, in a way it would not be for VMs.

The virtual machine pays the opposite price. Each VM duplicates the kernel, the init system, the system libraries, and the OS-level services, regardless of whether the application needs them. That duplication buys stronger isolation and the ability to run completely different operating systems side-by-side — a Windows VM and a Linux VM on the same physical host, both behaving exactly as their respective operating systems should.

The portability story also splits. A VM image is portable in the sense that any hypervisor of the right type can run it, and what runs is a complete operating system the team chose and configured. A container image is portable in a stricter sense: the same image runs on any host with a compatible kernel and a container runtime, and the surface area exposed to environment drift is reduced to the kernel ABI. Containers cannot, however, change their kernel — a container built to run on Linux cannot run natively on Windows, and a container built for the AMD64 architecture cannot run natively on an ARM64 host without emulation.

## Density and startup time in practice

The architectural difference shows up most visibly in two operational metrics: how many workloads fit on a host, and how quickly a workload becomes ready.

Consider a single physical host with 32 CPU cores and 128 GB of RAM. Running ten VMs, each with 4 vCPUs and 12 GB of RAM, fills the box and leaves headroom for the hypervisor — and each VM permanently consumes the memory of its own kernel and OS-level services, perhaps 1–2 GB before the application has loaded a single byte. The same host can comfortably run a hundred or more containers of equivalent applications, because each container only carries the application's own working set. Density on the order of 10× is routine.

Startup time tells the same story. A VM cold-starts in 30 seconds to several minutes: BIOS, bootloader, kernel, init, services, application. A container cold-starts in milliseconds to a few seconds: the runtime sets up namespaces and cgroups, mounts the image's filesystem, and execs the application. The difference is not a tuning question — it is structural. A VM has a kernel to boot and a container does not.

### A worked example: dotnet run vs the same app as a VM

The CloudSoft Recruitment app from the [Docker exercise track](/exercises/20-docker/) is a small ASP.NET Core service. Run it directly on a developer machine:

```bash
$ time dotnet run --project src/CloudSoft.Web
Building...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000

real    0m0.213s
```

The .NET runtime starts, the application initialises, and the HTTP listener is ready in roughly 200 ms. Wrap that same application as a container image and `docker run` it: the runtime mounts the image's filesystem, the application process starts under its namespaces, and the container is serving requests in roughly the same 200 ms — the dotnet startup time dominates and the container overhead is negligible.

Now provision the same application as an Azure VM. The VM creation completes in about a minute; first boot of the operating system takes another 30–60 seconds; configuration management installs the .NET runtime and copies the application; finally the application starts in its 200 ms. Wall-clock time from "go" to "serving requests" is two to three minutes for a fresh VM, versus a fraction of a second for a container on a host that already has the image. Both deployments end up running the exact same compiled application — the difference is entirely in what surrounds it.

## When each model fits

Neither model is universally better. The right question is which trade-off matches the workload.

| Dimension | Virtual machine | Container |
|-----------|-----------------|-----------|
| Isolation boundary | Hardware-virtualised, separate kernel | Kernel-enforced namespaces, shared kernel |
| Image size | Gigabytes (full OS) | Tens to hundreds of MB (app + libs) |
| Startup time | Tens of seconds to minutes | Milliseconds to seconds |
| Density per host | Tens of VMs | Hundreds of containers |
| Guest OS flexibility | Any compatible OS, including Windows | Must match host kernel family |
| Per-instance overhead | Dedicated kernel, init, OS services | Application process only |
| Failure blast radius | One VM, contained by hypervisor | One container, contained by namespaces |

A VM is the right choice when the workload needs its own operating system. Legacy Windows desktop applications that depend on the Win32 API run as Windows VMs, not containers, on a Linux host. Workloads with strict regulatory isolation requirements may demand the hardware-grade boundary that only a separate kernel provides. Database engines tuned for specific kernel parameters, or workloads that need direct access to specialised hardware drivers, often live in VMs because the abstraction layer of containers hides what they want to reach.

A container is the right choice when the workload is a stateless service that should start fast, scale horizontally, and ship as a self-contained artifact. A web API serving HTTP requests, a background worker consuming a message queue, a scheduled job that processes a file and exits — all are textbook container workloads. The application is small, the dependencies are well-defined, and operational value comes from running many cheap instances rather than a few expensive ones.

The two models also compose. Cloud platforms run containers *on top of* VMs: a managed Kubernetes cluster, or Azure Container Apps, provisions VMs as the underlying compute and packs containers onto them. The VM provides the hardware-grade tenant boundary between customers, and the container provides the lightweight unit of deployment within that boundary. Neither model has to win for both to be useful.

## Summary

Virtual machines and containers solve the same problem — running isolated workloads on shared hardware — at different layers. A VM virtualises hardware: a hypervisor allocates physical resources to guests, each running a full operating system, paying gigabytes of image size and tens of seconds of boot time in exchange for hardware-grade isolation and full OS flexibility. A container shares the host kernel: namespaces and cgroups isolate a single process tree, its filesystem, and its network, and a container runtime such as Docker executes the resulting unit. Containers start in milliseconds, pack an order of magnitude denser per host, and ship as compact images, but they cannot mix kernel families and they share a kernel-vulnerability blast radius. The fit follows the trade-off: VMs for legacy desktop OSes, regulated isolation, or hardware-specific workloads; containers for stateless services that want fast starts, high density, and a single artifact that runs identically everywhere. The remaining chapters of this Part build on the container model — how images are layered, how Dockerfiles describe them, how Compose orchestrates them locally, and how registries distribute them.
