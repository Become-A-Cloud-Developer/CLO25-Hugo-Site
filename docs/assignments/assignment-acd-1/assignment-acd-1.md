# Assignment 1: Containerized Web Application — From Inner Loop to Azure Container Apps

In this assignment, you will demonstrate that you can independently develop, containerize, secure, and deploy a containerized .NET web application to Azure Container Apps using a CI/CD pipeline — without following a step-by-step guide. You have practiced each of the underlying techniques in the course exercises. Now show that you can put them together on your own and explain your decisions.

The intended application is the **CloudSoft Recruitment Portal** — a web application where candidates can browse and apply to job postings, and where administrators manage postings and applications. You may continue from the version you built during the exercises, develop your own variant of it, or pick a different application idea entirely — as long as your application follows the same structure and feature set: a .NET MVC web application with at least two user roles, persistent domain data, and a clear separation between local development and production.

Write your report as a cohesive narrative that explains both *what* you built and *why*. Use diagrams, code snippets, and screenshots to support the narrative. The report is not a step-by-step tutorial — focus on decisions, motivations, and structure.

## Sub-task 1: Agile workflow and inner loop

The application can do many things. Decide what your version will do, and capture that scope as a small set of user stories.

- Write **3–5 user stories** in a format such as *"As a \<role\>, I want \<capability\> so that \<value\>"*.
- Describe your **inner loop** — the cycle of writing code, running it, and getting feedback during development. What tools and commands do you use? How does the loop change once the application also depends on a database?

## Sub-task 2: Containerization and local development environment

Package the application as a container and set up a local development environment that runs everything you need.

Discuss:

- How your **Dockerfile** is structured and why
- How you run **the full stack locally** with all dependent services
- Where your image lives and how it gets there
- What trade-offs you considered along the way

## Sub-task 3: Authentication, authorization, and the data layer

Implement user management and persistent storage for your application.

Discuss:

- **Who the users are** and what each role is allowed to do
- How the application **authenticates** users and **protects itself** against common web threats (e.g., CSRF, weak credentials, leaked session cookies)
- How the **first administrator** gets into a fresh installation
- How the **data layer** is structured — what runs in local development versus production, and how the application connects to each

## Sub-task 4: CI/CD and deployment to Azure

Automate the path from a `git push` to a running revision in Azure Container Apps.

Discuss:

- How your **pipeline is structured** — what stages it has and what each stage produces
- How **secrets and identities** are handled (you should not commit secrets to the repository)
- How **Azure resources** (Container Registry, Container Apps environment, Container App) are created and updated
- How you **verify** that a deployment actually succeeded

## Sub-task 5: Verification of the deployed solution

Show that the deployed application works end-to-end from the public internet — not only that the deployment finished.

## Cross-cutting concerns

Throughout your report, address these areas. You may weave them into the sub-tasks or treat them in their own sections.

### Security

Security is not a single chapter — it influences how you build, package, configure, deploy, and operate the application. Reflect on the security implications of each step. Examples to consider: cookie configuration, role enforcement, secret handling, registry access, image origin, network exposure of the Container App.

### Infrastructure as Code

Describe how someone else can **recreate** your environment. Are the steps documented as scripts, templates, or workflow steps? What is automated and what is manual?

### Use of AI assistants

The course curriculum includes the use of AI assistants as a learning objective. Reflect on how you used AI assistants during the assignment — for what tasks they helped, where they got it wrong, and how you verified that the suggestions were correct.

## Submission requirements

- **Format:** PDF, single document
- **First page:** must include:
  - Your name
  - A screenshot of the deployed application's landing page with the public URL clearly visible in the browser address bar
  - A link to your public GitHub repository
- **GitHub repository:** must contain the application code, Dockerfile, Docker Compose file, infrastructure scripts or templates, and the GitHub Actions workflow
- **Diagrams:** any diagrams must be your own work — not screenshots of other people's diagrams
- **Code snippets:** must be in monospace font and copy-pasteable — not screenshots of code

> ## A note on scope
>
> Make a clear scope decision. Everything in this course can be done in a more advanced way than what we have practiced. The point of the assignment is to **show that you understand and can apply what we have covered**, not to add tools and patterns you haven't used. State what is in, what is out, and motivate the boundary.
