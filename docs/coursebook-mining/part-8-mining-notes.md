# Part VIII — Mining Notes

Terminology contract for the seven chapters of Part VIII — DevOps and Delivery (ACD track).

## Chapter 1: The DevOps Philosophy

**Slug:** `1-the-devops-philosophy`

### Owned terms
- DevOps
- Lead time
- Mean time to recovery (MTTR)
- Deployment frequency
- Change-failure rate
- Value stream

### Borrowed terms
- (None in Week 4 focus)

### Reflection questions from ACD Week 4 (v.18)
- Vilka steg ingår i en typisk CI/CD-pipeline?
- Hur hänger alla delar ihop: lokal utveckling → Docker → CI/CD → Azure?

### Worked examples from exercise 3.9
- The arc of automation: manual deployment → partial pipeline → full CI/CD

### Slide pair
Yes — EN/SWE

### Course tag
ACD

### Cross-link target
/course-book/8-devops-and-delivery/1-the-devops-philosophy/

---

## Chapter 2: Continuous Integration vs Continuous Deployment

**Slug:** `2-ci-vs-cd`

### Owned terms
- Continuous integration (CI)
- Continuous delivery
- Continuous deployment
- Trunk-based development
- Pull-request gate

### Borrowed terms
- (None primary)

### Reflection questions from ACD Week 4 (v.18)
- Vad är skillnaden mellan Docker Hub och Azure Container Registry?
- Vilka steg ingår i en typisk CI/CD-pipeline?

### Worked examples from exercise 3.9
- Ex 1: CI only (build + push, manual deploy)
- Ex 2: CI + CD (push + auto-deploy with smoke test)
- Ex 3: Same as Ex 2, but passwordless

### Slide pair
Yes — EN/SWE

### Course tag
ACD

### Cross-link target
/course-book/8-devops-and-delivery/2-ci-vs-cd/

---

## Chapter 3: Pipelines as Code

**Slug:** `3-pipelines-as-code`

### Owned terms
- Pipeline
- Workflow (GitHub Actions)
- Job
- Step
- Runner
- Action (GitHub)
- Artifact (CI)

### Borrowed terms
- Docker image (Part VII Ch 2)

### Reflection questions from ACD Week 4 (v.18)
- Vilka steg ingår i en typisk CI/CD-pipeline?

### Worked examples from exercise 3.9
- Ex 1: First workflow that builds and pushes to Docker Hub
- Ex 2: Workflow that updates Container App revision
- Ex 3: Workflow with OIDC authentication

### Slide pair
Yes — EN/SWE

### Course tag
ACD

### Cross-link target
/course-book/8-devops-and-delivery/3-pipelines-as-code/

---

## Chapter 4: Build, Test, and Smoke Gates

**Slug:** `4-build-test-and-smoke-gates`

### Owned terms
- Build stage
- Unit test
- Integration test
- Smoke test
- Gate (CI)
- Test-result publisher

### Borrowed terms
- Docker image (Part VII Ch 2)
- Multi-stage build (Part VII Ch 3)

### Reflection questions from ACD Week 4 (v.18)
- Hur kan man verifiera att en driftsättning lyckades automatiskt?

### Worked examples from exercise 3.9
- Ex 2: Smoke test that curls the live FQDN and fails if not 200
- Ex 2: Pipeline gates so broken releases fail before production

### Slide pair
Yes — EN/SWE

### Course tag
ACD

### Cross-link target
/course-book/8-devops-and-delivery/4-build-test-and-smoke-gates/

---

## Chapter 5: Deployment Strategies

**Slug:** `5-deployment-strategies`

### Owned terms
- Deployment strategy
- Manual gate
- Blue-green deployment
- Canary deployment
- Rolling deployment
- Feature flag

### Borrowed terms
- Container (Part VII Ch 1)
- Container Apps (definition, architecture)

### Reflection questions from ACD Week 4 (v.18)
- Vad är skillnaden mellan Docker Hub och Azure Container Registry?

### Worked examples from exercise 3.9
- Ex 1: Manual gate (user clicks "Create new revision")
- Ex 2: Automatic deploy via pipeline
- Ex 3: Same automatic deploy, healthier secret handling

### Slide pair
Yes — EN/SWE

### Course tag
ACD

### Cross-link target
/course-book/8-devops-and-delivery/5-deployment-strategies/

---

## Chapter 6: Pipeline Secrets and OIDC Federation

**Slug:** `6-pipeline-secrets-and-oidc`

### Owned terms
- GitHub secret
- Service principal
- Federated credential
- OIDC federation (workload)
- Short-lived token (pipeline)

### Borrowed terms
- Bearer token (Part V Ch 5)
- JWT (Part V Ch 5)
- Managed identity (Part V Ch 8)
- Container registry (Part VII Ch 6)

### Reflection questions from ACD Week 4 (v.18)
- Vilka steg ingår i en typisk CI/CD-pipeline?

### Worked examples from exercise 3.9
- Ex 1: GitHub secret storing Docker Hub PAT
- Ex 2: GitHub secret storing service principal JSON
- Ex 3: Federated credential + OIDC token (no secret stored)

### Slide pair
Yes — EN/SWE

### Course tag
ACD

### Cross-link target
/course-book/8-devops-and-delivery/6-pipeline-secrets-and-oidc/

---

## Chapter 7: Azure Container Apps as a Deployment Target

**Slug:** `7-azure-container-apps`

### Owned terms
- Azure Container Apps
- Revision (ACA)
- Ingress
- Scale rule
- Container App environment

### Borrowed terms
- Container (Part VII Ch 1)
- Image (Part VII Ch 2)
- Container registry (Part VII Ch 6)
- Managed identity (Part V Ch 8)

### Reflection questions from ACD Week 4 (v.18)
- Vad är skillnaden mellan Docker Hub och Azure Container Registry?
- Hur kan man verifiera att en driftsättning lyckades automatiskt?

### Worked examples from exercise 3.9
- Ex 1: Create Container App from Docker Hub image, manually update revision
- Ex 2: Container App pulls from ACR with managed identity, auto-update from pipeline
- Ex 3: Same topology, federated pipeline

### Slide pair
Yes — EN/SWE

### Course tag
ACD

### Cross-link target
/course-book/8-devops-and-delivery/7-azure-container-apps/

