# Grading Rubric: Assignment 1

## Instructions for Evaluators

This rubric is used to evaluate student submissions for Assignment 1. Evaluate each criterion independently. The overall grade is determined as follows:

- **Väl Godkänt (VG):** All criteria are met at Godkänt level AND at least 4 of 6 criteria are met at VG level.
- **Godkänt (G):** All criteria are met at Godkänt level.
- **Underkänt (U):** One or more criteria are not met at Godkänt level.

For each criterion, assess the student's report and repository against the indicators listed below. Provide a brief justification for each assessment.

---

## Criterion 1: Application Development

**Learning objectives:** Develop and deploy a simpler application on a cloud platform. Describe the client-server model and common design patterns such as MVC.

**Godkänt:**

- A .NET web application is created and the student's name is visible on the landing page.
- The student identifies the project template used.
- The student names the architectural pattern (MVC) used by the application.
- The student describes the client-server model in the context of their solution — identifying what acts as the client and what acts as the server.
- The application code is available in the linked GitHub repository.

**Väl Godkänt:**

- The student explains why MVC is suitable for web applications, demonstrating conceptual understanding beyond naming the pattern.
- The student shows a clear understanding of the client-server model, e.g., describing the request-response flow between the browser and the Kestrel web server on the VM.
- The code in the repository is clean and well-organized (no unnecessary files, reasonable project structure).

---

## Criterion 2: Provisioning

**Learning objectives:** Perform basic system administration in portal and CLI. Use appropriate cloud services and tools. Describe cloud service models (IaaS, PaaS, SaaS).

**Godkänt:**

- An Ubuntu-based VM is provisioned in Azure.
- The student describes which provisioning method was used (Portal, Azure CLI, or ARM/Bicep).
- The student identifies the cloud service model (IaaS) being used.
- Key configuration choices are mentioned (VM size, region, networking).
- The student shows how they verified the VM was created and accessible.

**Väl Godkänt:**

- The student explains why they chose the specific provisioning method, comparing it to alternatives.
- The student explains what IaaS means in practice and how it differs from other service models.
- If using a scripted or template-based approach (Azure CLI script, ARM, or Bicep), the student explains the advantages of Infrastructure as Code compared to manual provisioning.
- Provisioning scripts or templates are included in the repository, making the process repeatable without manual portal steps.

---

## Criterion 3: Configuration

**Learning objective:** Develop and deploy a simpler application on a cloud platform and configure its basic resources.

**Godkänt:**

- The .NET Runtime is installed on the VM.
- A systemd service file is created to run the application.
- The student describes the installation steps and the service file contents.
- The student shows how they verified the runtime was installed correctly.

**Väl Godkänt:**

- The student explains what the key directives in the service file do (e.g., Restart, WorkingDirectory, ExecStart).
- The configuration is clean and structured — the service file and installation steps could be reused without modification.
- Configuration files (service file, cloud-init, or setup scripts) are included in the repository.

---

## Criterion 4: Deployment

**Learning objective:** Independently develop, deploy, and troubleshoot simpler cloud applications according to established practices.

**Godkänt:**

- The application is transferred to the server and runs as a systemd service.
- The student describes how files were transferred (e.g., SCP).
- The student shows how the service was started and verified.

**Väl Godkänt:**

- The deployment process is described clearly enough that it could be repeated with minimal effort.
- The student demonstrates methodical verification (e.g., checking service status, reading logs, testing the endpoint) rather than only stating "it works."
- The overall process from provisioning to deployment has fewer manual steps and shows a structured approach.

---

## Criterion 5: Security

**Learning objective:** Use appropriate cloud services and tools, and apply good security principles.

**Godkänt:**

- The student addresses server authentication (SSH keys) and explains why this method is used.
- The student identifies which ports are open and why.
- The student mentions at least one application-level security consideration.

**Väl Godkänt:**

- The student demonstrates understanding of security beyond listing facts — e.g., explains the risk of password authentication, discusses principle of least privilege for open ports, or identifies what is missing (such as HTTPS).
- Security considerations are consistently addressed throughout the report, not only in a separate section.

---

## Criterion 6: Report Quality

**Learning objective:** (Cross-cutting — supports all learning objectives through documentation and communication.)

**Godkänt:**

- All five sub-tasks are addressed with sufficient detail.
- The report is submitted as PDF with the required screenshot and repository link on the first page.
- The report is understandable and follows a logical structure.
- Screenshots and code snippets are used to support the text.

**Väl Godkänt:**

- The report reads as a cohesive tutorial that another student could follow to reproduce the solution.
- The writing is concise — no unnecessary repetition or filler.
- The student focuses on decisions and reasoning, not just step-by-step commands.
- The repository is well-organized and complements the report (the reader can move between report and code easily).
