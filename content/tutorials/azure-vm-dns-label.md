+++
title = "Configure Azure VM DNS Labels"
program = "CLO"
cohort = "25"
courses = ["BCD"]
weight = 2
+++

## Goal

Configure consistent DNS names for Azure Ubuntu VMs to get predictable URLs like `myapp.westeurope.cloudapp.azure.com` that persist even when VM IP addresses change.

> **What you'll learn:**
>
> - How DNS name labels work in Azure
> - Setting labels via Azure CLI (for existing VMs)
> - Provisioning VMs with labels using Bicep (Infrastructure as Code)
> - When to use static vs dynamic public IPs

## Prerequisites

> **Before starting, ensure you have:**
>
> - Azure subscription with Contributor access
> - Azure CLI installed and authenticated (`az login`)
> - SSH key pair configured (`~/.ssh/id_rsa` and `~/.ssh/id_rsa.pub`)
> - Basic understanding of Azure networking (Public IPs, NICs)

## Tutorial Steps

### Overview

1. **Understand Azure DNS Labels**
2. **Set DNS Label via Azure CLI** (for existing VMs)
3. **Create Bicep Template with DNS Label** (for new VMs)
4. **Deploy the Bicep Template**
5. **Test and Verify**

### **Step 1:** Understand Azure DNS Labels

Azure DNS name labels provide a human-readable hostname for your VM's public IP address. Instead of remembering `20.123.45.67`, you can use `myapp.westeurope.cloudapp.azure.com`.

Key concepts:

- **DNS labels attach to Public IP resources**, not VMs directly
- **Format:** `<label>.<region>.cloudapp.azure.com`
- **Uniqueness:** Labels must be unique within each Azure region
- **Persistence:** The DNS name stays the same even if the underlying IP changes (with dynamic allocation)

The relationship works like this:

```text
VM → NIC → Public IP (with DNS label) → myapp.westeurope.cloudapp.azure.com
```

> ℹ **Why use DNS labels?**
>
> - **Consistent access:** Share a stable URL with users or configure in applications
> - **No DNS management:** No need to set up external DNS records
> - **Free:** Included with your Public IP resource at no extra cost
> - **Works with dynamic IPs:** The label resolves correctly even after IP changes
>
> ⚠ **Limitations**
>
> - You cannot customize the domain suffix (always `.<region>.cloudapp.azure.com`)
> - Labels must be 3-63 characters, lowercase alphanumeric and hyphens only
> - Not suitable if you need a custom domain (use Azure DNS or external DNS instead)

### **Step 2:** Set DNS Label via Azure CLI

If you already have a VM without a DNS label, you can add one to its existing Public IP.

1. **Identify** your VM's public IP resource name:

   ```bash
   az vm show \
       --resource-group YOUR_RESOURCE_GROUP \
       --name YOUR_VM_NAME \
       --query "networkProfile.networkInterfaces[0].id" -o tsv
   ```

2. **Get** the public IP name from the NIC:

   ```bash
   # First get the NIC name from the previous output, then:
   az network nic show \
       --ids <NIC_ID_FROM_PREVIOUS_COMMAND> \
       --query "ipConfigurations[0].publicIPAddress.id" -o tsv
   ```

3. **Or use this combined command** to get the public IP name directly:

   ```bash
   az network public-ip list \
       --resource-group YOUR_RESOURCE_GROUP \
       --query "[?contains(name, 'YOUR_VM_NAME')].name" -o tsv
   ```

4. **Update** the public IP with your DNS label:

   ```bash
   az network public-ip update \
       --resource-group YOUR_RESOURCE_GROUP \
       --name YOUR_PUBLIC_IP_NAME \
       --dns-name your-unique-label
   ```

5. **Verify** the DNS label was set:

   ```bash
   az network public-ip show \
       --resource-group YOUR_RESOURCE_GROUP \
       --name YOUR_PUBLIC_IP_NAME \
       --query "dnsSettings.fqdn" -o tsv
   ```

> ℹ **Concept Deep Dive**
>
> The `--dns-name` parameter sets the `dnsSettings.domainNameLabel` property on the Public IP resource. Azure automatically combines this with the region and `cloudapp.azure.com` suffix to create the FQDN.
>
> ⚠ **Common Mistakes**
>
> - Using uppercase letters (labels must be lowercase)
> - Using underscores instead of hyphens
> - Choosing a label that's already taken in your region (you'll get an error)
> - Trying to set the DNS label on the VM resource instead of the Public IP
>
> ✓ **Quick check:** Run `nslookup your-unique-label.<region>.cloudapp.azure.com` to verify DNS resolution

### **Step 3:** Create Bicep Template with DNS Label

For new VMs, the best practice is to include the DNS label in your Infrastructure as Code. This Bicep template creates a complete Ubuntu 24.04 VM with a DNS name label.

1. **Create** a new file named `main.bicep`:

   > `main.bicep`

   ```bicep
   @description('Name of the virtual machine')
   param vmName string

   @description('DNS label for the public IP (must be unique in the region)')
   param dnsLabel string

   @description('Admin username for the VM')
   param adminUsername string = 'azureuser'

   @description('SSH public key for authentication')
   @secure()
   param sshPublicKey string

   @description('Azure region for all resources')
   param location string = resourceGroup().location

   @description('VM size')
   param vmSize string = 'Standard_B1s'

   // Public IP with DNS label
   resource publicIP 'Microsoft.Network/publicIPAddresses@2023-09-01' = {
     name: '${vmName}-pip'
     location: location
     sku: {
       name: 'Standard'
     }
     properties: {
       publicIPAllocationMethod: 'Static'
       dnsSettings: {
         domainNameLabel: dnsLabel
       }
     }
   }

   // Network Security Group
   resource nsg 'Microsoft.Network/networkSecurityGroups@2023-09-01' = {
     name: '${vmName}-nsg'
     location: location
     properties: {
       securityRules: [
         {
           name: 'SSH'
           properties: {
             priority: 1000
             protocol: 'Tcp'
             access: 'Allow'
             direction: 'Inbound'
             sourceAddressPrefix: '*'
             sourcePortRange: '*'
             destinationAddressPrefix: '*'
             destinationPortRange: '22'
           }
         }
         {
           name: 'HTTP'
           properties: {
             priority: 1001
             protocol: 'Tcp'
             access: 'Allow'
             direction: 'Inbound'
             sourceAddressPrefix: '*'
             sourcePortRange: '*'
             destinationAddressPrefix: '*'
             destinationPortRange: '80'
           }
         }
       ]
     }
   }

   // Virtual Network
   resource vnet 'Microsoft.Network/virtualNetworks@2023-09-01' = {
     name: '${vmName}-vnet'
     location: location
     properties: {
       addressSpace: {
         addressPrefixes: [
           '10.0.0.0/16'
         ]
       }
       subnets: [
         {
           name: 'default'
           properties: {
             addressPrefix: '10.0.0.0/24'
             networkSecurityGroup: {
               id: nsg.id
             }
           }
         }
       ]
     }
   }

   // Network Interface
   resource nic 'Microsoft.Network/networkInterfaces@2023-09-01' = {
     name: '${vmName}-nic'
     location: location
     properties: {
       ipConfigurations: [
         {
           name: 'ipconfig1'
           properties: {
             privateIPAllocationMethod: 'Dynamic'
             publicIPAddress: {
               id: publicIP.id
             }
             subnet: {
               id: vnet.properties.subnets[0].id
             }
           }
         }
       ]
     }
   }

   // Virtual Machine
   resource vm 'Microsoft.Compute/virtualMachines@2023-09-01' = {
     name: vmName
     location: location
     properties: {
       hardwareProfile: {
         vmSize: vmSize
       }
       osProfile: {
         computerName: vmName
         adminUsername: adminUsername
         linuxConfiguration: {
           disablePasswordAuthentication: true
           ssh: {
             publicKeys: [
               {
                 path: '/home/${adminUsername}/.ssh/authorized_keys'
                 keyData: sshPublicKey
               }
             ]
           }
         }
       }
       storageProfile: {
         imageReference: {
           publisher: 'Canonical'
           offer: '0001-com-ubuntu-server-noble'
           sku: '24_04-lts-gen2'
           version: 'latest'
         }
         osDisk: {
           createOption: 'FromImage'
           managedDisk: {
             storageAccountType: 'Standard_LRS'
           }
         }
       }
       networkProfile: {
         networkInterfaces: [
           {
             id: nic.id
           }
         ]
       }
     }
   }

   // Outputs
   output fqdn string = publicIP.properties.dnsSettings.fqdn
   output publicIPAddress string = publicIP.properties.ipAddress
   output sshCommand string = 'ssh ${adminUsername}@${publicIP.properties.dnsSettings.fqdn}'
   ```

> ℹ **Concept Deep Dive**
>
> The key section is the `dnsSettings` block in the Public IP resource:
>
> ```bicep
> dnsSettings: {
>   domainNameLabel: dnsLabel
> }
> ```
>
> This Bicep template uses a **Static** IP allocation with Standard SKU, which is recommended for production. Static IPs don't change even when the VM is deallocated, providing extra stability for DNS.
>
> ⚠ **Common Mistakes**
>
> - Using Basic SKU public IPs (being deprecated, always use Standard)
> - Forgetting to add NSG rules for SSH (port 22) — you won't be able to connect
> - Hardcoding the DNS label instead of parameterizing it
> - Using `Microsoft.Network/virtualMachines` instead of `Microsoft.Compute/virtualMachines` (typo in namespace)
>
> ✓ **Quick check:** The template should have no syntax errors when you run `az bicep build --file main.bicep`

### **Step 4:** Deploy the Bicep Template

Deploy your template to create a new VM with the DNS label configured.

1. **Set** your deployment variables:

   ```bash
   resource_group="DNSLabelDemo"
   location="westeurope"
   vm_name="mywebserver"
   dns_label="myapp-$(openssl rand -hex 4)"  # Adds random suffix for uniqueness
   ```

2. **Create** the resource group:

   ```bash
   az group create --name $resource_group --location $location
   ```

3. **Read** your SSH public key:

   ```bash
   ssh_key=$(cat ~/.ssh/id_rsa.pub)
   ```

4. **Deploy** the Bicep template:

   ```bash
   az deployment group create \
       --resource-group $resource_group \
       --template-file main.bicep \
       --parameters \
           vmName=$vm_name \
           dnsLabel=$dns_label \
           sshPublicKey="$ssh_key"
   ```

5. **Get** the deployment outputs:

   ```bash
   az deployment group show \
       --resource-group $resource_group \
       --name main \
       --query "properties.outputs" -o json
   ```

> ℹ **Concept Deep Dive**
>
> The deployment creates all resources in the correct dependency order: NSG → VNet → Public IP → NIC → VM. Bicep automatically determines this order based on resource references. The outputs provide the FQDN and SSH command for easy access.
>
> ⚠ **Common Mistakes**
>
> - Forgetting to quote the SSH key variable (breaks if the key contains spaces in the comment)
> - Using a DNS label that's already taken in the region
> - Not waiting for deployment to complete before testing
>
> ✓ **Quick check:** Deployment should complete in 2-3 minutes with "provisioningState": "Succeeded"

### **Step 5:** Test and Verify

Confirm your DNS label is working correctly.

1. **Get** your FQDN from the deployment output:

   ```bash
   fqdn=$(az deployment group show \
       --resource-group $resource_group \
       --name main \
       --query "properties.outputs.fqdn.value" -o tsv)
   echo "FQDN: $fqdn"
   ```

2. **Test** DNS resolution:

   ```bash
   nslookup $fqdn
   ```

3. **Connect** via SSH using the DNS name:

   ```bash
   ssh azureuser@$fqdn
   ```

4. **Verify** in the Azure Portal:
   - **Navigate** to your resource group
   - **Click** on the Public IP resource (`mywebserver-pip`)
   - **Check** the "DNS name" field shows your configured FQDN

5. **Test** persistence (optional):
   - Deallocate and start the VM
   - Verify the DNS name still resolves (IP may change with dynamic allocation, but DNS stays the same)

> ✓ **Success indicators:**
>
> - `nslookup` returns the correct IP address for your FQDN
> - SSH connection works using the DNS name
> - Azure Portal shows the DNS name on the Public IP resource
> - DNS name resolves correctly after VM restart
>
> ✓ **Final verification checklist:**
>
> - DNS label configured on Public IP resource
> - FQDN resolves to correct IP address
> - SSH access works via FQDN
> - NSG allows required ports (22 for SSH, 80 for HTTP)
> - Template deployed successfully with all outputs available

## Static vs Dynamic Public IPs

When configuring DNS labels, consider your IP allocation strategy:

| Allocation | Behavior | Best For |
|------------|----------|----------|
| **Dynamic** | IP may change when VM is deallocated | Development, testing |
| **Static** | IP never changes | Production, external DNS records |

With DNS labels, both work well because the DNS name stays constant. However, static IPs provide additional stability if external systems cache IP addresses.

## Cleanup

When you're finished, remove the Azure resources to avoid charges:

```bash
az group delete --name DNSLabelDemo --yes --no-wait
```

## TL;DR

**Add DNS label to existing VM:**

```bash
az network public-ip update \
    --resource-group YOUR_RG \
    --name YOUR_PUBLIC_IP_NAME \
    --dns-name your-unique-label
```

**Key Bicep snippet for new VMs:**

```bicep
resource publicIP 'Microsoft.Network/publicIPAddresses@2023-09-01' = {
  name: '${vmName}-pip'
  location: location
  sku: { name: 'Standard' }
  properties: {
    publicIPAllocationMethod: 'Static'
    dnsSettings: {
      domainNameLabel: dnsLabel  // This is the key setting!
    }
  }
}
```

**Quick deployment:**

```bash
# Set variables
resource_group="DNSLabelDemo"
vm_name="mywebserver"
dns_label="myapp-unique123"

# Create resource group
az group create --name $resource_group --location westeurope

# Deploy (assumes main.bicep exists)
az deployment group create \
    --resource-group $resource_group \
    --template-file main.bicep \
    --parameters vmName=$vm_name dnsLabel=$dns_label sshPublicKey="$(cat ~/.ssh/id_rsa.pub)"

# Get FQDN
az deployment group show --resource-group $resource_group --name main \
    --query "properties.outputs.fqdn.value" -o tsv
```

**Result:** `myapp-unique123.westeurope.cloudapp.azure.com`
