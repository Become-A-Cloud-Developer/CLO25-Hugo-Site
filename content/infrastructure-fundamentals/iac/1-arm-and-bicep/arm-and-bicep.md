+++
title = "ARM & Bicep"
program = "CLO"
cohort = "25"
courses = ["BCD"]
weight = 1
date = 2025-08-10
draft = false
+++

[Watch the presentation](/presentations/infrastructure-fundamentals/iac/1-arm-and-bicep.html)

[Se presentationen på svenska](/presentations/infrastructure-fundamentals/iac/1-arm-and-bicep-swe.html)

<!-- Listen to the article -->

<!-- <audio controls>
<source src="/audio/arm-bicep-lecture1.mp3" type="audio/mpeg">
Your browser does not support the audio element.
</audio> -->

---

<!-- # ARM & Bicep: Why IaC (Lecture 1) -->

Infrastructure as Code (**IaC**) lets us define Azure resources **declaratively** and deploy them **repeatably**. This week we move from **Portal & CLI** to **templates** that describe our Ubuntu VM and its network—clean, versioned, and testable.

## Why IaC (for our VM)
- **Consistent**: same template → same VM, NIC, NSG, Public IP every time
- **Auditable**: templates live in Git; PR review replaces "click ops"
- **Safe changes**: use `what-if` to see impact before applying

## ARM vs Bicep (quick compare)
- **ARM**: verbose JSON; native to Azure Resource Manager
- **Bicep**: concise DSL that compiles to ARM; better ergonomics, modules, linter
- **Engine**: both deploy via **Azure Resource Manager**

## Template anatomy (mini-map)
- **parameters**: inputs (location, namePrefix, adminUser, sshKey, myIp)
- **resources**: VNet/Subnet, NSG rules, Public IP, NIC, **Ubuntu VM**
- **outputs**: public IP address (so we can browse Nginx)

## Commands you'll use
- `az deployment group what-if -f main.bicep -p dev.bicepparam`
- `az deployment group create -f main.bicep -p dev.bicepparam`
- `bicep decompile main.json` and `bicep build main.bicep`

