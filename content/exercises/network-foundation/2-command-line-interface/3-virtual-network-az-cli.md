+++
title = "Virtual Network with Enhanced Security"
program = "CLO"
cohort = "25"
courses = ["BCD"]
weight = 3
date = 2024-12-05
draft = false
+++

## Goal

Build a complete three-tier network infrastructure on Azure to enable secure communication between application components while protecting them from unauthorized access.

> **What you'll learn:**
>
> - How to create virtual networks and subnets for network isolation
> - When to use Network Security Groups (NSGs) and Application Security Groups (ASGs) together
> - Best practices for implementing defense-in-depth security architecture
> - How to automate server configuration using cloud-init

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ Azure CLI installed and configured on your machine
> - ✓ An active Azure subscription
> - ✓ Basic understanding of IP addresses and network security concepts
> - ✓ SSH key pair generated (or ability to generate one)

## Exercise Steps

### Overview

1. **Create Foundation Resources**
2. **Configure Network Security**
3. **Provision Virtual Machines**
4. **Attach Security Groups to Network Interfaces**
5. **Test Your Implementation**

### **Step 1:** Create Foundation Resources

Establish the basic Azure infrastructure that will host your network and virtual machines. This includes a resource group to organize resources and a virtual network to enable communication between components.

1. **Create** a resource group to contain all resources:

   ```bash
   az group create --name DemoRG --location northeurope
   ```

2. **Create** a virtual network with a default subnet:

   ```bash
   az network vnet create \
     --resource-group DemoRG \
     --name DemoVNet \
     --address-prefix 10.0.0.0/16 \
     --subnet-name default \
     --subnet-prefix 10.0.0.0/24
   ```

> ℹ **Concept Deep Dive**
>
> Resource groups are logical containers that group related Azure resources for management and billing. Virtual networks (VNets) provide isolated network environments in Azure, similar to traditional on-premises networks. The address prefix `10.0.0.0/16` provides 65,536 IP addresses for the entire VNet, while the subnet prefix `10.0.0.0/24` allocates 256 addresses for the default subnet.
>
> The CIDR notation `/16` and `/24` determines the network size. A `/16` network has 16 fixed bits and 16 variable bits (2^16 = 65,536 addresses). A `/24` network has 24 fixed bits and 8 variable bits (2^8 = 256 addresses).
>
> ⚠ **Common Mistakes**
>
> - Using overlapping address ranges with existing networks causes routing conflicts
> - Choosing too small a subnet (like `/28`) limits growth to 16 addresses
> - Forgetting that Azure reserves 5 addresses in each subnet for internal use
>
> ✓ **Quick check:** Run `az network vnet list --resource-group DemoRG` to verify the VNet was created

### **Step 2:** Configure Network Security

Implement a defense-in-depth security model using both Network Security Groups and Application Security Groups. This layered approach provides fine-grained control over network traffic to protect your infrastructure.

1. **Create** the Application Security Groups for logical grouping:

   ```bash
   az network asg create \
     --resource-group DemoRG \
     --name ReverseProxyASG

   az network asg create \
     --resource-group DemoRG \
     --name BastionHostASG
   ```

2. **Create** the Network Security Group:

   ```bash
   az network nsg create \
     --resource-group DemoRG \
     --name DemoNSG
   ```

3. **Add** the SSH access rule for the bastion host:

   ```bash
   az network nsg rule create \
     --resource-group DemoRG \
     --nsg-name DemoNSG \
     --name AllowSSH \
     --priority 1000 \
     --access Allow \
     --protocol Tcp \
     --direction Inbound \
     --source-address-prefixes Internet \
     --source-port-ranges "*" \
     --destination-asg BastionHostASG \
     --destination-port-ranges 22
   ```

4. **Add** the HTTP access rule for the reverse proxy:

   ```bash
   az network nsg rule create \
     --resource-group DemoRG \
     --nsg-name DemoNSG \
     --name AllowHTTP \
     --priority 2000 \
     --access Allow \
     --protocol Tcp \
     --direction Inbound \
     --source-address-prefixes Internet \
     --source-port-ranges "*" \
     --destination-asg ReverseProxyASG \
     --destination-port-ranges 80
   ```

5. **Associate** the NSG with the subnet:

   ```bash
   az network vnet subnet update \
     --resource-group DemoRG \
     --vnet-name DemoVNet \
     --name default \
     --network-security-group DemoNSG
   ```

> ℹ **Concept Deep Dive**
>
> Application Security Groups (ASGs) let you group virtual machines by their role in your application architecture, rather than by IP address. This makes security rules more maintainable because you can add or remove VMs from groups without updating firewall rules.
>
> Network Security Groups (NSGs) act as virtual firewalls that control inbound and outbound traffic. Rules are evaluated in priority order (lowest number first), and the first matching rule is applied. Once a rule matches, evaluation stops.
>
> By attaching the NSG to the subnet, all resources in that subnet inherit the rules. This is more efficient than attaching NSGs to individual network interfaces, especially in larger deployments.
>
> **Defense-in-depth architecture:** The web server has no public IP and can only be reached through the reverse proxy. The reverse proxy accepts HTTP traffic from the internet but can't be accessed via SSH. Only the bastion host accepts SSH connections, creating a secure entry point for administration.
>
> ⚠ **Common Mistakes**
>
> - Setting the same priority for multiple rules causes conflicts
> - Using `*` for destination port ranges is too permissive for production
> - Forgetting to associate the NSG with the subnet means rules won't be enforced
> - Creating ASGs without actually attaching them to VM network interfaces (Step 4)
>
> ✓ **Quick check:** Run `az network nsg rule list --resource-group DemoRG --nsg-name DemoNSG --output table` to verify both rules exist

### **Step 3:** Provision Virtual Machines

Deploy the three servers that make up your infrastructure: a web server to host content, a reverse proxy to handle external requests, and a bastion host for secure administrative access.

1. **Create** the web server configuration file named `web_server_config.yaml`:

   > `web_server_config.yaml`

   ```yaml
   #cloud-config
   packages:
     - nginx
   write_files:
     - path: /var/www/html/index.html
       content: |
         <!DOCTYPE html>
         <html>
         <head>
             <title>Hello World!</title>
         </head>
         <body>
             <h1>Hello World!</h1>
         </body>
         </html>
     - path: /etc/nginx/sites-available/default
       content: |
         server {
           listen 8080 default_server;
           server_name _;
           root /var/www/html;
           index index.html;
         }
   runcmd:
     - systemctl restart nginx
   ```

2. **Provision** the web server virtual machine:

   ```bash
   az vm create \
     --resource-group DemoRG \
     --name WebServer \
     --image Ubuntu2204 \
     --size Standard_B1s \
     --admin-username azureuser \
     --vnet-name DemoVNet \
     --subnet default \
     --nsg "" \
     --public-ip-address "" \
     --generate-ssh-keys \
     --custom-data @web_server_config.yaml
   ```

3. **Create** the reverse proxy configuration file named `reverse_proxy_config.yaml`:

   > `reverse_proxy_config.yaml`

   ```yaml
   #cloud-config
   packages:
     - nginx
   write_files:
     - path: /etc/nginx/sites-available/default
       content: |
         server {
           listen 80;
           location / {
             proxy_pass http://webserver.internal.cloudapp.net:8080/;
             proxy_set_header Host $host;
             proxy_set_header X-Real-IP $remote_addr;
             proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
           }
         }
   runcmd:
     - systemctl restart nginx
   ```

4. **Provision** the reverse proxy virtual machine:

   ```bash
   az vm create \
     --resource-group DemoRG \
     --name ReverseProxy \
     --image Ubuntu2204 \
     --size Standard_B1s \
     --admin-username azureuser \
     --vnet-name DemoVNet \
     --subnet default \
     --nsg "" \
     --generate-ssh-keys \
     --custom-data @reverse_proxy_config.yaml
   ```

5. **Provision** the bastion host virtual machine:

   ```bash
   az vm create \
     --resource-group DemoRG \
     --name BastionHost \
     --image Ubuntu2204 \
     --size Standard_B1s \
     --admin-username azureuser \
     --vnet-name DemoVNet \
     --subnet default \
     --nsg "" \
     --generate-ssh-keys
   ```

> ℹ **Concept Deep Dive**
>
> Cloud-init is an industry-standard tool for cloud instance initialization. The YAML configuration files define packages to install, files to create, and commands to run during first boot. This automation ensures consistent server configuration and eliminates manual setup steps.
>
> The web server listens on port 8080 (non-standard) because it's an internal service not exposed to the internet. The reverse proxy accepts traffic on port 80 (standard HTTP) and forwards requests to the web server's internal address.
>
> Using Azure's internal DNS (`webserver.internal.cloudapp.net`) provides name resolution within the VNet without requiring static IP addresses. This makes the infrastructure more maintainable.
>
> The `--nsg ""` parameter prevents Azure from automatically creating network interface-level NSGs, since we're using a subnet-level NSG instead. The `--public-ip-address ""` parameter ensures the web server has no public IP, making it only accessible internally.
>
> The `--generate-ssh-keys` parameter creates SSH key pairs automatically if they don't exist at `~/.ssh/id_rsa`. This is convenient for development but in production you should use explicitly managed keys.
>
> ⚠ **Common Mistakes**
>
> - Forgetting the `@` symbol in `@web_server_config.yaml` causes Azure CLI to interpret the filename as configuration text
> - Using `http://WebServer:8080` in the proxy config fails because the hostname must match Azure's internal DNS format
> - Missing `systemctl restart nginx` in cloud-init means configuration changes won't take effect
> - Creating VMs before the VNet exists results in creation failures
>
> ✓ **Quick check:** Run `az vm list --resource-group DemoRG --output table` to verify all three VMs are running

### **Step 4:** Attach Security Groups to Network Interfaces

Connect the Application Security Groups to the network interfaces of your virtual machines to enforce the security rules. This crucial step activates the NSG rules you created earlier.

1. **Retrieve** and store the reverse proxy's NIC information:

   ```bash
   REVERSE_PROXY_NIC_ID=$(az vm show \
     --resource-group DemoRG \
     --name ReverseProxy \
     --query 'networkProfile.networkInterfaces[0].id' \
     --output tsv)

   REVERSE_PROXY_NIC_NAME=$(basename $REVERSE_PROXY_NIC_ID)

   REVERSE_PROXY_NIC_IP_CONFIG=$(az network nic show \
     --resource-group DemoRG \
     --name $REVERSE_PROXY_NIC_NAME \
     --query 'ipConfigurations[0].name' \
     --output tsv)
   ```

2. **Attach** the ReverseProxyASG to the reverse proxy's NIC:

   ```bash
   az network nic ip-config update \
     --resource-group DemoRG \
     --nic-name $REVERSE_PROXY_NIC_NAME \
     --name $REVERSE_PROXY_NIC_IP_CONFIG \
     --application-security-groups ReverseProxyASG
   ```

3. **Retrieve** and store the bastion host's NIC information:

   ```bash
   BASTION_HOST_NIC_ID=$(az vm show \
     --resource-group DemoRG \
     --name BastionHost \
     --query 'networkProfile.networkInterfaces[0].id' \
     --output tsv)

   BASTION_HOST_NIC_NAME=$(basename $BASTION_HOST_NIC_ID)

   BASTION_HOST_NIC_IP_CONFIG=$(az network nic show \
     --resource-group DemoRG \
     --name $BASTION_HOST_NIC_NAME \
     --query 'ipConfigurations[0].name' \
     --output tsv)
   ```

4. **Attach** the BastionHostASG to the bastion host's NIC:

   ```bash
   az network nic ip-config update \
     --resource-group DemoRG \
     --nic-name $BASTION_HOST_NIC_NAME \
     --name $BASTION_HOST_NIC_IP_CONFIG \
     --application-security-groups BastionHostASG
   ```

> ℹ **Concept Deep Dive**
>
> Network Interface Cards (NICs) are the virtual network adapters attached to VMs. Each NIC has one or more IP configurations that define its network settings. Attaching ASGs to these IP configurations tells Azure which security rules apply to that VM.
>
> The `--query` parameter uses JMESPath syntax to extract specific fields from JSON output. This enables shell scripting by capturing resource IDs and names in variables.
>
> The `basename` command extracts just the filename from a full resource path. For example, it converts `/subscriptions/.../networkInterfaces/ReverseProxyVMNIC` to just `ReverseProxyVMNIC`.
>
> Azure allows multiple IP configurations per NIC (for multi-homing scenarios), which is why we query `ipConfigurations[0]` to get the first (and typically only) configuration.
>
> **Why this matters:** Until you complete this step, the NSG rules targeting ASGs have no effect. The VMs are essentially unprotected by your custom rules. This step activates your security architecture.
>
> ⚠ **Common Mistakes**
>
> - Forgetting the `$` before variable names causes literal strings to be used instead of values
> - Running these commands before VMs are fully provisioned results in "resource not found" errors
> - Attaching ASGs to the wrong NIC associates security rules with the wrong server
> - Not storing the NIC name correctly causes the update command to fail
>
> ✓ **Quick check:** Run `az network nic show --resource-group DemoRG --name $REVERSE_PROXY_NIC_NAME --query 'ipConfigurations[0].applicationSecurityGroups'` to verify the ASG is attached

### **Step 5:** Test Your Implementation

Verify that your infrastructure is working correctly and that the security rules are properly enforced. This systematic testing ensures all components are configured properly and communicate as designed.

1. **Retrieve** the reverse proxy's public IP address:

   ```bash
   REVERSE_PROXY_IP=$(az vm show \
     --resource-group DemoRG \
     --name ReverseProxy \
     --show-details \
     --query 'publicIps' \
     --output tsv)

   echo "Reverse Proxy IP: $REVERSE_PROXY_IP"
   ```

2. **Test** HTTP access to the reverse proxy:

   Open your browser and navigate to `http://<REVERSE_PROXY_IP>` or use curl:

   ```bash
   curl http://$REVERSE_PROXY_IP
   ```

   You should see the "Hello World!" page served by the web server through the reverse proxy.

3. **Retrieve** the bastion host's public IP address:

   ```bash
   BASTION_IP=$(az vm show \
     --resource-group DemoRG \
     --name BastionHost \
     --show-details \
     --query 'publicIps' \
     --output tsv)

   echo "Bastion Host IP: $BASTION_IP"
   ```

4. **Test** SSH access to the bastion host:

   ```bash
   ssh azureuser@$BASTION_IP
   ```

   You should successfully connect to the bastion host.

5. **Verify** security isolation by testing blocked access:

   Try to SSH directly to the web server (this should fail since it has no public IP):

   ```bash
   # This command will fail - web server has no public IP
   az vm show \
     --resource-group DemoRG \
     --name WebServer \
     --show-details \
     --query 'publicIps'
   ```

   Try to SSH to the reverse proxy (this should be blocked by NSG rules):

   ```bash
   # This connection will timeout - SSH is blocked by NSG
   ssh azureuser@$REVERSE_PROXY_IP
   ```

6. **Test** internal connectivity from the bastion host:

   SSH into the bastion host, then test connectivity to internal servers:

   ```bash
   # From the bastion host:
   curl http://webserver.internal.cloudapp.net:8080
   curl http://reverseproxy.internal.cloudapp.net
   ```

> ✓ **Success indicators:**
>
> - The reverse proxy serves the "Hello World!" page when accessed via HTTP
> - You can SSH into the bastion host from your local machine
> - SSH to the reverse proxy is blocked (connection timeout)
> - The web server has no public IP address
> - Internal communication works from the bastion host to both servers
> - NSG rules show correct hit counts when you view them in the portal
>
> ✓ **Final verification checklist:**
>
> - ☐ Virtual network and subnet created successfully
> - ☐ NSG rules allow SSH to bastion and HTTP to reverse proxy only
> - ☐ ASGs attached to the correct VM network interfaces
> - ☐ All three VMs provisioned and running
> - ☐ Web server accessible only through reverse proxy
> - ☐ Bastion host accessible via SSH from internet
> - ☐ Internal DNS resolution works between VMs

## Common Issues

> **If you encounter problems:**
>
> **"Resource not found" when attaching ASGs:** Wait 1-2 minutes after VM creation for all resources to be fully provisioned, then retry
>
> **Connection timeout to reverse proxy:** Verify the ASG is attached to the NIC using `az network nic show`. Check NSG rule priority and ensure the rule targets the correct ASG
>
> **"Hello World!" page doesn't load:** SSH into the reverse proxy and check nginx status with `sudo systemctl status nginx`. Verify the web server is reachable with `curl http://webserver.internal.cloudapp.net:8080`
>
> **Can't SSH to bastion host:** Check that the NSG rule allows your source IP address (it should allow "Internet" source). Verify your SSH key is correctly configured
>
> **Internal DNS not working:** Ensure all VMs are in the same VNet. Azure's internal DNS only works within VNets, not across different VNets without peering
>
> **Variable commands fail:** Make sure you run all commands in sequence within the same terminal session. Variables are session-specific and don't persist across terminal restarts
>
> **Still stuck?** Run `az network watcher show-topology --resource-group DemoRG` to visualize your network architecture and identify configuration issues

## Summary

You've successfully built a production-ready three-tier network architecture which:

- ✓ Implements defense-in-depth security with NSGs and ASGs
- ✓ Isolates internal services from direct internet access
- ✓ Provides secure administrative access through a bastion host
- ✓ Uses cloud-init for automated server configuration

> **Key takeaway:** Application Security Groups combined with Network Security Groups create flexible, maintainable security policies based on application roles rather than IP addresses. This architecture pattern is essential in production environments where servers are frequently added, removed, or replaced. The bastion host pattern provides secure administrative access while keeping production systems isolated from the internet.

## Going Deeper (Optional)

> **Want to explore more?**
>
> - Try adding a fourth VM to the WebServerASG and update the reverse proxy to load balance between multiple web servers
> - Implement Azure Bastion (a managed PaaS solution) instead of a self-managed bastion host for enhanced security
> - Add HTTPS support to the reverse proxy using Let's Encrypt certificates
> - Create a separate management subnet with its own NSG for administrative traffic
> - Implement network flow logs to monitor and analyze traffic patterns
> - Add a Web Application Firewall (WAF) in front of the reverse proxy for additional protection

## Cleanup

> **To avoid ongoing charges, delete all resources:**

```bash
az group delete --name DemoRG --yes --no-wait
```

> This command deletes the resource group and all contained resources (VMs, NICs, NSGs, ASGs, VNet, and public IPs). The `--no-wait` flag allows the command to return immediately while deletion continues in the background.

## Done! 🎉

Excellent work! You've learned how to design and implement secure network architectures on Azure using infrastructure as code. You can now build multi-tier applications with proper network isolation and security controls. This foundation will help you deploy production-grade applications in the cloud.
