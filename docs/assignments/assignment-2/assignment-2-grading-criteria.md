# Grading Rubric: Assignment 2

## Instructions for Evaluators

This rubric is used to evaluate student submissions for Assignment 2. Evaluate each criterion independently. The overall grade is determined as follows:

- **Väl Godkänt (VG):** All criteria are met at Godkänt level AND at least 5 of 7 criteria are met at VG level.
- **Godkänt (G):** All criteria are met at Godkänt level.
- **Underkänt (U):** One or more criteria are not met at Godkänt level.

For each criterion, assess the student's report and repository against the indicators listed below. Provide a brief justification for each assessment.

> **Aspirational goal (beyond VG):** A "one-click solution" where, given the application code and all IaC in a GitHub repository, a single command provisions the entire architecture including CI/CD pipeline setup. This is not required for any grade level but represents the ultimate automation goal of the course.

---

## Criterion 1: Architecture Design

**Learning objectives:** Design a secure production environment for a web application. Describe components and their purpose.

**Godkänt:**

- The student presents an architecture diagram (hand-drawn, digital, or any format) that covers the main components: VMs, reverse proxy, app server, bastion host, and networking.
- The student describes what each component does and why it is part of the design.
- The diagram shows how traffic flows from users through the internet to the application.
- The student identifies which cloud service models are used (IaaS, PaaS).

**Väl Godkänt:**

- The architecture diagram is detailed and clearly communicates the full solution, including network boundaries (vNet/Subnet), security groups (NSG/ASG), and PaaS services.
- The student explains design decisions — why components are separated across VMs, why a bastion host is used, and how the reverse proxy and app server interact.
- The diagram distinguishes between IaaS and PaaS components and shows how they connect.

---

## Criterion 2: Provisioning

**Learning objectives:** Provision cloud infrastructure. Use Infrastructure as Code and automation tools.

**Godkänt:**

- Azure resources are provisioned (VMs, Virtual Network, Subnet, NSG).
- The student describes the provisioning method used (Portal, Azure CLI, ARM, or Bicep).
- The student shows how they verified that resources were created correctly.
- Key configuration choices are documented (VM size, region, networking, CIDR).

**Väl Godkänt:**

- Provisioning is done primarily through Infrastructure as Code (Azure CLI scripts at minimum, Bicep as the more advanced approach).
- IaC scripts or templates are included in the repository, making provisioning repeatable.
- The student explains why IaC is preferable over manual provisioning and what advantages it provides.
- The student demonstrates that the provisioning can be re-run to recreate the environment.

---

## Criterion 3: Configuration

**Learning objectives:** Configure a production environment with reverse proxy, application runtime, and service management.

**Godkänt:**

- Nginx is installed and configured as a reverse proxy (server block forwards traffic on port 80 to the app on port 5000).
- The .NET Runtime is installed on the application server.
- A SystemD service unit file is created to manage the application as a service.
- The student describes the configuration steps and shows verification.

**Väl Godkänt:**

- Configuration is automated using Cloud-Init or setup scripts rather than performed entirely manually via SSH.
- The student explains the key directives in the Nginx server block and the SystemD service unit file.
- Configuration files are included in the repository and could be reused without modification.
- The student demonstrates a structured approach to configuration, with clear separation between the reverse proxy VM and the application server VM.

---

## Criterion 4: Deployment & CI/CD

**Learning objectives:** Deploy applications using established practices. Implement continuous integration and continuous deployment.

**Godkänt:**

- The application is deployed to the app server and runs as a SystemD service.
- The student describes the deployment method (e.g., SCP, manual file transfer).
- The student shows how the deployment was verified (service status, logs, endpoint testing).

**Väl Godkänt:**

- Deployment is automated using a CI/CD pipeline with GitHub Actions.
- The pipeline includes CI (build) and CD (deployment to the VM).
- A self-hosted GitHub Actions runner is set up on the application server VM to pull and deploy artifacts. (The installation of the self-hosted runner itself may be manual.)
- The student explains the pipeline workflow and how artifacts flow from GitHub to the VM.
- The pipeline is included in the repository (workflow YAML file).

---

## Criterion 5: Cloud Services (PaaS)

**Learning objectives:** Use appropriate cloud services. Integrate PaaS services with an IaaS-based application.

**Godkänt:**

- The application uses Azure Cosmos DB (MongoDB API) as its database.
- The student describes how the application connects to Cosmos DB (connection string).
- The application is the newsletter application developed during the course (MVC pattern with service layer, repository pattern, options pattern).

**Väl Godkänt:**

- Azure Blob Storage is integrated for serving images or static assets.
- The student implements or describes the SAS token pattern — the app server generates temporary SAS URLs and the browser fetches files directly from Blob Storage.
- The student explains the difference between accessing PaaS services over the public internet versus using Azure Service Endpoints for private connectivity.
- The student demonstrates understanding of the application architecture (MVC, service/repository pattern, options pattern) and how it connects to the cloud services.

---

## Criterion 6: Security

**Learning objectives:** Apply security principles to a cloud production environment.

**Godkänt:**

- A bastion host is used as the sole SSH entry point — application and proxy VMs are not directly accessible from the internet via SSH.
- SSH key authentication is used (no password authentication).
- NSG rules are configured to restrict traffic to necessary ports only (HTTP 80, SSH 22).
- The student describes their security measures and why they are important.

**Väl Godkänt:**

- The student uses ASG (Application Security Groups) in addition to NSGs to logically group VMs and define granular network rules.
- Security is addressed throughout the report, not just in a dedicated section — provisioning, configuration, and deployment steps all consider security implications.
- The student demonstrates defence-in-depth thinking, e.g., discussing what happens if one layer is compromised, why the bastion host is separated, or why PaaS services should use private connectivity.
- Private keys and secrets are handled securely (not committed to the repository, stored appropriately).

---

## Criterion 7: Report Quality

**Learning objective:** Document and communicate a technical solution clearly and completely.

**Godkänt:**

- Both tasks (design and step-by-step guide) are addressed with sufficient detail.
- The report is submitted as a single PDF document.
- Architecture diagrams are created by the student (not screenshots from others).
- Code snippets are in monospace font and copy-pasteable (not screenshots of code).
- The report follows a logical structure and is understandable.

**Väl Godkänt:**

- The report reads as a cohesive guide that another student could follow to reproduce the entire solution.
- Step-by-step instructions include verification steps so the reader can confirm they are on track.
- The writing focuses on decisions and reasoning, not just listing commands.
- The scope is clearly defined — the student explains what is included and what is excluded, and why.
- The repository is well-organized and complements the report (IaC scripts, configuration files, workflow YAML, application code).
