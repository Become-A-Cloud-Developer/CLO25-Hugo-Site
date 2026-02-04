+++
title = "8. Minimal ARM Template: Ubuntu VM + Network"
program = "CLO"
cohort = "25"
courses = ["BCD"]
weight = 8
date = 2025-08-10
draft = false
+++

**Goal:** Author the *smallest viable* **ARM template** that deploys an **Ubuntu VM** with a minimal network (VNet/Subnet, NSG, Public IP, NIC). Preview with `what-if`, deploy, and capture the public IP as an output.

**Estimated time:** 40–60 minutes

---

## Learning outcomes
- Structure an ARM template: **parameters**, **variables**, **resources**, **outputs**.
- Deploy to a **resource group** with `az deployment group` and use **what-if**.
- Wire up **NSG → NIC → VM** and output the **Public IP**.

## Prerequisites
- Azure CLI, VS Code.
- A resource group (or create one).
- Your **SSH public key** (`~/.ssh/id_rsa.pub` or `id_ed25519.pub`).

## What you'll produce
- `main.json` — minimal template.
- `parameters.dev.json` — parameter file for your environment.
- A successful deployment and the **public IP** printed as an **output**.

---

## Steps

1) **Scaffold files**
Create a new folder and two files: `main.json` and `parameters.dev.json`.

**main.json (paste & save):**
```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "location": {"type": "string", "defaultValue": "[resourceGroup().location]"},
    "namePrefix": {"type": "string", "defaultValue": "demo"},
    "adminUsername": {"type": "string", "defaultValue": "azureuser"},
    "adminPublicKey": {"type": "string"},
    "addressSpace": {"type": "string", "defaultValue": "10.10.0.0/16"},
    "subnetPrefix": {"type": "string", "defaultValue": "10.10.1.0/24"}
  },
  "variables": {
    "vnetName": "[format('{0}-vnet', parameters('namePrefix'))]",
    "subnetName": "default",
    "nsgName": "[format('{0}-nsg', parameters('namePrefix'))]",
    "pipName": "[format('{0}-ip', parameters('namePrefix'))]",
    "nicName": "[format('{0}-nic', parameters('namePrefix'))]",
    "vmName": "[format('{0}-vm', parameters('namePrefix'))]"
  },
  "resources": [

    {
      "type": "Microsoft.Network/virtualNetworks",
      "apiVersion": "2024-07-01",
      "name": "[variables('vnetName')]",
      "location": "[parameters('location')]",
      "properties": {
        "addressSpace": {"addressPrefixes": ["[parameters('addressSpace')]"]},
        "subnets": [
          {
            "name": "[variables('subnetName')]",
            "properties": {"addressPrefix": "[parameters('subnetPrefix')]" }
          }
        ]
      }
    },

    {
      "type": "Microsoft.Network/networkSecurityGroups",
      "apiVersion": "2024-07-01",
      "name": "[variables('nsgName')]",
      "location": "[parameters('location')]",
      "properties": {
        "securityRules": [
          {
            "name": "SSH",
            "properties": {
              "protocol": "TCP",
              "sourcePortRange": "*",
              "destinationPortRange": "22",
              "sourceAddressPrefix": "*",
              "destinationAddressPrefix": "*",
              "access": "Allow",
              "priority": 300,
              "direction": "Inbound"
            }
          },
          {
            "name": "HTTP",
            "properties": {
              "protocol": "TCP",
              "sourcePortRange": "*",
              "destinationPortRange": "80",
              "sourceAddressPrefix": "*",
              "destinationAddressPrefix": "*",
              "access": "Allow",
              "priority": 320,
              "direction": "Inbound"
            }
          }
        ]
      }
    },

    {
      "type": "Microsoft.Network/publicIPAddresses",
      "apiVersion": "2024-07-01",
      "name": "[variables('pipName')]",
      "location": "[parameters('location')]",
      "sku": {"name": "Standard", "tier": "Regional"},
      "properties": {
        "publicIPAddressVersion": "IPv4",
        "publicIPAllocationMethod": "Static"
      }
    },

    {
      "type": "Microsoft.Network/networkInterfaces",
      "apiVersion": "2024-07-01",
      "name": "[variables('nicName')]",
      "location": "[parameters('location')]",
      "dependsOn": [
        "[resourceId('Microsoft.Network/publicIPAddresses', variables('pipName'))]",
        "[resourceId('Microsoft.Network/virtualNetworks', variables('vnetName'))]",
        "[resourceId('Microsoft.Network/networkSecurityGroups', variables('nsgName'))]"
      ],
      "properties": {
        "ipConfigurations": [
          {
            "name": "ipconfig1",
            "properties": {
              "subnet": {"id": "[resourceId('Microsoft.Network/virtualNetworks/subnets', variables('vnetName'), variables('subnetName'))]"},
              "publicIPAddress": {"id": "[resourceId('Microsoft.Network/publicIPAddresses', variables('pipName'))]"},
              "privateIPAllocationMethod": "Dynamic",
              "primary": true
            }
          }
        ],
        "networkSecurityGroup": {"id": "[resourceId('Microsoft.Network/networkSecurityGroups', variables('nsgName'))]"}
      }
    },

    {
      "type": "Microsoft.Compute/virtualMachines",
      "apiVersion": "2024-11-01",
      "name": "[variables('vmName')]",
      "location": "[parameters('location')]",
      "dependsOn": [
        "[resourceId('Microsoft.Network/networkInterfaces', variables('nicName'))]"
      ],
      "properties": {
        "hardwareProfile": {"vmSize": "Standard_B1s"},
        "storageProfile": {
          "imageReference": {
            "publisher": "canonical",
            "offer": "ubuntu-24_04-lts",
            "sku": "server",
            "version": "latest"
          },
          "osDisk": {
            "createOption": "FromImage",
            "managedDisk": {"storageAccountType": "Premium_LRS"},
            "diskSizeGB": 30
          }
        },
        "osProfile": {
          "computerName": "[variables('vmName')]",
          "adminUsername": "[parameters('adminUsername')]",
          "linuxConfiguration": {
            "disablePasswordAuthentication": true,
            "ssh": {
              "publicKeys": [
                {
                  "path": "[format('/home/{0}/.ssh/authorized_keys', parameters('adminUsername'))]",
                  "keyData": "[parameters('adminPublicKey')]"
                }
              ]
            }
          }
        },
        "networkProfile": {
          "networkInterfaces": [{ "id": "[resourceId('Microsoft.Network/networkInterfaces', variables('nicName'))]" }]
        }
      }
    }
  ],
  "outputs": {
    "publicIpAddress": {
      "type": "string",
      "value": "[reference(resourceId('Microsoft.Network/publicIPAddresses', variables('pipName')), '2024-07-01').ipAddress]"
    }
  }
}
```

**parameters.dev.json (paste & adjust):**
```json
{
  "$schema": "https://schema.management.azure.com/schemas/2015-01-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "namePrefix": {"value": "demo"},
    "adminUsername": {"value": "azureuser"},
    "adminPublicKey": {"value": "<paste your SSH public key>"}
  }
}
```

2) **Deploy**
```bash
# (optional) create an RG
az group create -n rg-arm-demo -l northeurope

# preview
az deployment group what-if -g rg-arm-demo -f main.json -p @parameters.dev.json

# deploy
az deployment group create -g rg-arm-demo -f main.json -p @parameters.dev.json
```

3) **Get the public IP (output)**
```bash
az deployment group show -g rg-arm-demo -n main --query properties.outputs.publicIpAddress.value -o tsv
```

4) **Verify**
```bash
ssh azureuser@<public-ip>   # should prompt to trust the host (no password)
```

---

## Verification
- Paste your **public IP** output.
- Show your **what-if** summary (brief snippet or screenshot).
- Short note: which resource **dependsOn** which?

---

## Stretch (optional)
- **Add cloud‑init** (Nginx home page): add to `osProfile` → `customData` with base64 of a small script file that installs Nginx.
- Add your default public SSH key programatically as a AZ CLI parameter.

---

## Cleanup
```bash
az group delete -n rg-arm-demo --yes --no-wait
```
