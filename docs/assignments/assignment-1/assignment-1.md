# Assignment 1: Deploy a Web Application to the Cloud

In this assignment, you will demonstrate that you can independently provision, configure, and deploy a .NET web application to Azure — without following a step-by-step guide. You have practiced each of these steps in the course exercises. Now, show that you can put it all together on your own and explain your process.

Write your report as a cohesive tutorial that someone else in your class could follow to reproduce your solution. Use text, screenshots, and code snippets to describe each sub-task below.

## Sub-task 1: Develop the application

Use the .NET MVC application you built during the web development exercises, or create a new one. Make sure your name is clearly visible on the landing page.

Briefly describe:
- Which .NET project template you used
- What architectural pattern the application uses (e.g., MVC) and why this pattern is suitable for web applications
- How the client-server model applies to your solution — what acts as the client and what acts as the server
- How you modified the landing page to display your name
- How you verified the application runs locally

## Sub-task 2: Provision a hosting environment

Provision an Ubuntu-based virtual machine in Azure to host your application.

Choose one of the provisioning methods covered in the exercises (Azure Portal, Azure CLI, or ARM/Bicep). If you used a scripted or template-based approach, explain the advantages of Infrastructure as Code (IaC) compared to manual provisioning.

Describe:
- Which method you chose and why
- Which cloud service model your solution uses (IaaS, PaaS, or SaaS) and what that means in practice
- The key configuration choices you made (VM size, region, networking)
- How you verified the VM was created and accessible

## Sub-task 3: Configure the hosting environment

Prepare the VM to run your .NET application. This includes installing the .NET Runtime and creating a systemd service file.

Describe:
- How you installed the .NET Runtime
- The contents of your service file and what each directive does
- How you verified the runtime was installed correctly

## Sub-task 4: Deploy the application

Deploy your application to the VM you provisioned and configured in the previous sub-tasks.

Describe:
- How you transferred the application files to the server
- How you started the application as a service
- How you verified the service is running

## Sub-task 5: Verify the solution

Verify that your web application is running in Azure and reachable from the internet.

Describe how you confirmed the application is running and accessible from outside your local network.

## Security

Throughout your report, address how you handled security at each stage. At minimum, discuss:

- **Server access:** How did you authenticate with the VM? Why is this method preferred?
- **Network security:** Which ports did you open and why? Which ports remain closed?
- **Application security:** Are there any security considerations in how the application is exposed to the internet?

You may weave security into each sub-task or write a separate security section — either approach is acceptable.

## Submission Requirements

- **Format:** PDF
- **First page:** Must include:
  - Your name
  - A screenshot of your landing page with your name — the browser address bar must be clearly readable
  - A link to your public GitHub repository containing the complete application code
- **Git repository:** Push your application code to a public GitHub repository. The repository should include all source code and configuration files needed to build and deploy the application (e.g., project files, service file, any provisioning scripts).
- **Report body:** Focus on the interesting parts — key decisions, relevant screenshots, and important code snippets. You do not need to include every line of code in the report itself since the full code is available in your repository.
