+++
title = "9. From ARM to Bicep: Ubuntu VM + Network"
program = "CLO"
cohort = "25"
courses = ["BCD"]
weight = 9
date = 2025-08-10
draft = false
+++

**Goal:** Rebuild **Exercise 8** using **Bicep** instead of ARM JSON. Produce a clean `main.bicep` and a `.bicepparam` file, preview with `what-if`, then deploy.

**Estimated time:** 40–60 minutes

---

## Learning outcomes
- Author resources in **Bicep** and understand how it compiles to ARM.
- Use **.bicepparam** files for environment-specific values.
- Output the **Public IP** and verify SSH/Nginx like in Exercise 8.

## Prerequisites
- Exercise 8 completed (or read through).
- **Azure CLI** + **Bicep** installed (`az bicep install` or standalone).
- SSH public key available.

## What you'll produce
- `main.bicep` — minimal VM + network in Bicep.
- `dev.bicepparam` — parameters for your environment.
- A successful deployment and the **public IP** printed as an output.

---

## Steps

1) **Create a bicep template**

Create `main.bicep` and paste:
```bicep
param location string = resourceGroup().location
param namePrefix string = 'demo'
param adminUsername string = 'azureuser'
param adminPublicKey string
param addressSpace string = '10.10.0.0/16'
param subnetPrefix string = '10.10.1.0/24'

var vnetName = '${namePrefix}-vnet'
var nsgName  = '${namePrefix}-nsg'
var pipName  = '${namePrefix}-ip'
var nicName  = '${namePrefix}-nic'
var vmName   = '${namePrefix}-vm'
var subnetName = 'default'

resource vnet 'Microsoft.Network/virtualNetworks@2024-07-01' = {
  name: vnetName
  location: location
  properties: {
    addressSpace: { addressPrefixes: [ addressSpace ] }
    subnets: [{ name: subnetName, properties: { addressPrefix: subnetPrefix } }]
  }
}

resource nsg 'Microsoft.Network/networkSecurityGroups@2024-07-01' = {
  name: nsgName
  location: location
  properties: {
    securityRules: [
      {
        name: 'SSH'
        properties: {
          protocol: 'TCP'
          sourcePortRange: '*'
          destinationPortRange: '22'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 300
          direction: 'Inbound'
        }
      }
      {
        name: 'HTTP'
        properties: {
          protocol: 'TCP'
          sourcePortRange: '*'
          destinationPortRange: '80'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 320
          direction: 'Inbound'
        }
      }
    ]
  }
}

resource pip 'Microsoft.Network/publicIPAddresses@2024-07-01' = {
  name: pipName
  location: location
  sku: { name: 'Standard', tier: 'Regional' }
  properties: {
    publicIPAddressVersion: 'IPv4'
    publicIPAllocationMethod: 'Static'
  }
}

resource nic 'Microsoft.Network/networkInterfaces@2024-07-01' = {
  name: nicName
  location: location
  properties: {
    ipConfigurations: [{
      name: 'ipconfig1'
      properties: {
        subnet: { id: resourceId('Microsoft.Network/virtualNetworks/subnets', vnet.name, subnetName) }
        publicIPAddress: { id: pip.id }
        privateIPAllocationMethod: 'Dynamic'
        primary: true
      }
    }]
    networkSecurityGroup: { id: nsg.id }
  }
}

resource vm 'Microsoft.Compute/virtualMachines@2024-11-01' = {
  name: vmName
  location: location
  properties: {
    hardwareProfile: { vmSize: 'Standard_B1s' }
    storageProfile: {
      imageReference: {
        publisher: 'canonical'
        offer: 'ubuntu-24_04-lts'
        sku: 'server'
        version: 'latest'
      }
      osDisk: {
        createOption: 'FromImage'
        managedDisk: { storageAccountType: 'Premium_LRS' }
        diskSizeGB: 30
      }
    }
    osProfile: {
      computerName: vmName
      adminUsername: adminUsername
      linuxConfiguration: {
        disablePasswordAuthentication: true
        ssh: {
          publicKeys: [{ path: '/home/${adminUsername}/.ssh/authorized_keys', keyData: adminPublicKey }]
        }
      }
    }
    networkProfile: {
      networkInterfaces: [{ id: nic.id }]
    }
  }
}

output publicIpAddress string = pip.properties.ipAddress
```

2) **Create `dev.bicepparam`**
```bicep
using './main.bicep'

param namePrefix = 'demo'
param adminUsername = 'azureuser'
param adminPublicKey = '<paste your SSH public key>'
```

3) **Preview & deploy**
```bash
# (optional) create an RG
az group create -n rg-bicep-demo -l northeurope

# preview
az deployment group what-if -g rg-bicep-demo -f main.bicep -p dev.bicepparam

# deploy
az deployment group create -g rg-bicep-demo -f main.bicep -p dev.bicepparam
```

4) **Get the public IP (output)**
```bash
az deployment group show -g rg-bicep-demo -n main --query properties.outputs.publicIpAddress.value -o tsv
```

5) **Verify**
```bash
ssh azureuser@<public-ip>
```

---

## Verification
- Paste your **public IP** output.
- Show your **what-if** summary.
- Short note: what changed moving from **ARM → Bicep** (readability, params, outputs).

---

## Cleanup
```bash
az group delete -n rg-bicep-demo --yes --no-wait
```
