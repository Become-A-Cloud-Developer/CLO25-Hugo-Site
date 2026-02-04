+++
title = "2. Creating a Virtual Network with Enhanced Security"
program = "CLO"
cohort = "25"
courses = ["BCD"]
weight = 2
date = 2024-12-02
draft = false
+++

## Goal

Build a secure virtual network infrastructure using Azure Virtual Networks with Application Security Groups and Network Security Groups to control traffic between servers with different roles.

> **What you'll learn:**
>
> - How to create and configure Azure Virtual Networks with subnets
> - When to use Application Security Groups versus Network Security Groups
> - Best practices for implementing defense-in-depth security architecture
> - How to automate server configuration using cloud-init

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ Active Azure subscription with permissions to create resources
> - ✓ Basic understanding of networking concepts (IP addresses, ports, protocols)
> - ✓ Familiarity with Azure Portal navigation
> - ✓ Understanding of cloud-init from previous exercises

## Exercise Steps

### Overview

1. **Create the Virtual Network**
2. **Configure Application Security Groups**
3. **Configure Network Security Group Rules**
4. **Provision Web Server**
5. **Provision Reverse Proxy**
6. **Provision Bastion Host**
7. **Test Your Implementation**

![NetworkOverview](/images/NetworkOverview.png)

### **Step 1:** Create the Virtual Network

Create the foundational network infrastructure that will host your servers. The Virtual Network provides isolated network space in Azure where resources can communicate securely using private IP addresses.

1. **Navigate to** the Azure Portal at <https://portal.azure.com>

2. **Search for** "Virtual Networks" in the top search bar

3. **Click** the **+ Create** button

4. **Configure** the virtual network on the Basics tab:
   - **Subscription**: Select your subscription
   - **Resource Group**: Select or create `DemoRG`
   - **Name**: Enter `DemoVNet`
   - **Region**: Select `North Europe` or your preferred region

5. **Click** **Review + Create** and then **Create**

> ℹ **Concept Deep Dive**
>
> A Virtual Network (VNet) is the fundamental building block for private networks in Azure. It functions like a traditional network in your datacenter but provides additional benefits of Azure infrastructure such as scale, availability, and isolation. Resources in the same VNet can communicate by default, while traffic between VNets or to the Internet must be explicitly configured.
>
> ⚠ **Common Mistakes**
>
> - Choosing different regions for VNet and VMs increases latency and may incur bandwidth costs
> - Using overlapping IP address ranges prevents VNet peering in the future
> - Forgetting to note the default subnet CIDR range for later configuration
>
> ✓ **Quick check:** Virtual Network appears in your resource group with a default subnet

### **Step 2:** Configure Application Security Groups

Create logical groupings for your servers based on their roles. Application Security Groups allow you to define fine-grained network security policies based on workload patterns rather than explicit IP addresses.

1. **Search for** "Application Security Groups" in the Azure Portal

2. **Click** **+ Create** to create the first ASG

3. **Configure** the Reverse Proxy ASG:
   - **Resource Group**: Select `DemoRG`
   - **Name**: Enter `ReverseProxyASG`
   - **Region**: Select the same region as your VNet

4. **Click** **Review + Create** and then **Create**

5. **Repeat** steps 2-4 to create the Bastion Host ASG:
   - **Name**: Enter `BastionHostASG`
   - Keep all other settings the same

> ℹ **Concept Deep Dive**
>
> Application Security Groups (ASGs) enable you to group virtual machines by function and define security rules based on those groups. This abstraction means you don't need to manually maintain lists of IP addresses in security rules. When you add a VM to an ASG, it automatically inherits all rules associated with that group. This approach scales better and reduces configuration errors in complex environments.
>
> ⚠ **Common Mistakes**
>
> - Creating ASGs in different regions than the VMs causes association errors
> - Not creating ASGs before NSG rules means you'll need to modify rules later
> - Confusing ASGs with availability sets or VM scale sets
>
> ✓ **Quick check:** Both ASGs appear in your resource group with no associated NICs yet

### **Step 3:** Configure Network Security Group Rules

Create and configure firewall rules that control traffic to your servers based on their Application Security Groups. Network Security Groups act as distributed firewalls that filter traffic at the network interface or subnet level.

1. **Search for** "Network Security Groups" in the Azure Portal

2. **Click** **+ Create**

3. **Configure** the NSG:
   - **Name**: Enter `DemoNSG`
   - **Resource Group**: Select `DemoRG`
   - **Region**: Select the same region as your VNet

4. **Click** **Review + Create** and then **Create**

5. **Navigate to** the newly created NSG and **click** **Go to resource**

6. **Select** **Inbound security rules** under Settings

7. **Click** **+ Add** to create the SSH rule

8. **Configure** the SSH rule:
   - **Source**: Select `Service Tag`
   - **Source service tag**: Select `Internet`
   - **Destination**: Select `Application security group`
   - **Destination application security group**: Select `BastionHostASG`
   - **Service**: Select `SSH`
   - **Name**: Enter `Allow-SSH-Bastion`

9. **Click** **Add**

10. **Click** **+ Add** again to create the HTTP rule

11. **Configure** the HTTP rule:
    - **Source**: Select `Service Tag`
    - **Source service tag**: Select `Internet`
    - **Destination**: Select `Application security group`
    - **Destination application security group**: Select `ReverseProxyASG`
    - **Service**: Select `HTTP`
    - **Name**: Enter `Allow-HTTP-ReverseProxy`

12. **Click** **Add**

13. **Select** **Subnets** under Settings

14. **Click** **+ Associate**

15. **Configure** the subnet association:
    - **Virtual network**: Select `DemoVNet`
    - **Subnet**: Select `default`

16. **Click** **OK**

> ℹ **Concept Deep Dive**
>
> Network Security Groups provide stateful packet filtering at Layer 4 (Transport layer) of the OSI model. Rules are evaluated by priority number, with lower numbers evaluated first. The combination of NSGs with ASGs creates a powerful security model: NSGs define what traffic is allowed, while ASGs define which servers the rules apply to. This separation of concerns makes security policies more maintainable as your infrastructure grows.
>
> **Security Architecture Explanation:**
>
> - SSH access is limited to the Bastion Host, creating a single, auditable entry point
> - HTTP traffic is only allowed to the Reverse Proxy, not directly to backend servers
> - The Web Server has no public IP and no inbound rules from Internet, creating defense-in-depth
> - All outbound traffic remains allowed by default for package installation and updates
>
> ⚠ **Common Mistakes**
>
> - Attaching NSG to both subnet and NIC creates confusing rule interactions
> - Forgetting to associate the NSG with the subnet means rules won't take effect
> - Creating rules with same priority causes unpredictable behavior
> - Using "Any" source for SSH creates security vulnerabilities
>
> ✓ **Quick check:** NSG shows two custom inbound rules and is associated with the default subnet

### **Step 4:** Provision Web Server

Create the backend web server that hosts your application content. This server runs on a non-standard port and has no public IP address, making it accessible only through the internal network.

1. **Navigate to** Virtual Machines in the Azure Portal

2. **Click** **+ Create** and select **Azure virtual machine**

3. **Configure** the instance details:
   - **Resource group**: Select `DemoRG`
   - **Virtual machine name**: Enter `WebServer`
   - **Region**: Select the same region as your VNet
   - **Image**: Select `Ubuntu Server 24.04 LTS`
   - **Size**: Select `Standard_B1s`

4. **Configure** administrator account:
   - **Authentication type**: Select `SSH public key`
   - **Username**: Enter `azureuser`
   - **SSH public key source**: Generate new key pair or use existing

5. **Click** **Next: Disks** then **Next: Networking**

6. **Configure** networking:
   - **Virtual network**: Select `DemoVNet`
   - **Subnet**: Select `default`
   - **Public IP**: Select `None`
   - **NIC network security group**: Select `None`
   - **Application security groups**: Leave empty (no ASG needed for internal-only server)

7. **Click** **Next: Management** then **Next: Monitoring** then **Next: Advanced**

8. **Add** the following cloud-init script in the Custom data field:

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

9. **Click** **Review + Create** and then **Create**

> ℹ **Concept Deep Dive**
>
> This server demonstrates the principle of least privilege - it has no public IP address and no ASG assignment because it doesn't need inbound Internet access. The web server listens on port 8080 instead of the standard port 80, illustrating how internal services can use non-standard ports safely. The cloud-init script automates the complete server configuration, making the deployment repeatable and eliminating manual configuration steps.
>
> **Why port 8080?**
>
> Using a non-standard port for the backend server provides several benefits: it clearly distinguishes internal from external services, prevents accidental direct access even if firewall rules fail, and follows the microservices pattern where backend services use different ports.
>
> ⚠ **Common Mistakes**
>
> - Adding a public IP "just in case" defeats the security architecture
> - Selecting an NSG at the NIC level conflicts with subnet-level NSG
> - Incorrect indentation in cloud-init YAML causes configuration failures
> - Forgetting to restart nginx means the new configuration won't take effect
>
> ✓ **Quick check:** VM is running, has a private IP address but no public IP, and shows no ASG associations

### **Step 5:** Provision Reverse Proxy

Create the reverse proxy server that routes external HTTP traffic to the internal web server. This server acts as a security boundary and load distribution point in your architecture.

1. **Navigate to** Virtual Machines in the Azure Portal

2. **Click** **+ Create** and select **Azure virtual machine**

3. **Configure** the instance details:
   - **Resource group**: Select `DemoRG`
   - **Virtual machine name**: Enter `ReverseProxy`
   - **Region**: Select the same region as your VNet
   - **Image**: Select `Ubuntu Server 24.04 LTS`
   - **Size**: Select `Standard_B1s`

4. **Configure** administrator account:
   - **Authentication type**: Select `SSH public key`
   - **Username**: Enter `azureuser`
   - Use the same SSH key as the Web Server

5. **Click** **Next: Disks** then **Next: Networking**

6. **Configure** networking:
   - **Virtual network**: Select `DemoVNet`
   - **Subnet**: Select `default`
   - **Public IP**: Select `Create new` or use existing
   - **NIC network security group**: Select `None`
   - **Application security groups**: **Select** `ReverseProxyASG`

7. **Click** **Next: Management** then **Next: Monitoring** then **Next: Advanced**

8. **Add** the following cloud-init script in the Custom data field:

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

9. **Click** **Review + Create** and then **Create**

> ℹ **Concept Deep Dive**
>
> A reverse proxy serves as an intermediary that forwards client requests to backend servers. This architecture provides several production benefits: it hides the internal server topology, enables SSL termination at a single point, allows for load balancing across multiple backends, and provides a centralized location for request logging and caching.
>
> **Azure Internal DNS:**
>
> The configuration uses `webserver.internal.cloudapp.net` which is Azure's internal DNS service. Azure automatically creates DNS records for VMs in the format `vmname.internal.cloudapp.net` within the same VNet. This means you don't need to hardcode IP addresses, making your infrastructure more resilient to IP changes.
>
> **Nginx Proxy Headers:**
>
> The proxy headers preserve client information through the proxy chain. `X-Real-IP` contains the original client IP, while `X-Forwarded-For` maintains the chain of proxies. These headers are essential for logging, security, and application behavior that depends on client information.
>
> ⚠ **Common Mistakes**
>
> - Forgetting to attach `ReverseProxyASG` means HTTP traffic will be blocked by NSG rules
> - Using the wrong internal hostname causes connection failures to the backend
> - Missing the trailing slash in `proxy_pass` can cause incorrect request routing
> - Not including proxy headers breaks features that depend on client IP information
>
> ✓ **Quick check:** VM is running with both public and private IPs and shows `ReverseProxyASG` in the networking section

### **Step 6:** Provision Bastion Host

Create the bastion host that provides secure SSH access to internal servers. This server serves as the single entry point for administrative access, following security best practices.

1. **Navigate to** Virtual Machines in the Azure Portal

2. **Click** **+ Create** and select **Azure virtual machine**

3. **Configure** the instance details:
   - **Resource group**: Select `DemoRG`
   - **Virtual machine name**: Enter `BastionHost`
   - **Region**: Select the same region as your VNet
   - **Image**: Select `Ubuntu Server 24.04 LTS`
   - **Size**: Select `Standard_B1s`

4. **Configure** administrator account:
   - **Authentication type**: Select `SSH public key`
   - **Username**: Enter `azureuser`
   - Use the same SSH key as previous servers

5. **Click** **Next: Disks** then **Next: Networking**

6. **Configure** networking:
   - **Virtual network**: Select `DemoVNet`
   - **Subnet**: Select `default`
   - **Public IP**: Select `Create new` or use existing
   - **NIC network security group**: Select `None`
   - **Application security groups**: **Select** `BastionHostASG`

7. **Click** **Review + Create** and then **Create** (no cloud-init needed)

> ℹ **Concept Deep Dive**
>
> A bastion host (also called jump box) is a hardened server that provides access to internal infrastructure. In production environments, bastion hosts would have additional security measures: hardened OS configuration, multi-factor authentication, session recording, and restricted access hours. By routing all administrative access through a single point, you create an auditable security boundary and reduce the attack surface of your infrastructure.
>
> **Why no cloud-init?**
>
> The bastion host requires no special configuration - it's simply an SSH gateway. Its security comes from its placement in the network architecture and the NSG rules that control access, not from software configuration. This simplicity is by design: fewer installed packages means fewer potential security vulnerabilities.
>
> ⚠ **Common Mistakes**
>
> - Forgetting to attach `BastionHostASG` means SSH access will be blocked
> - Installing unnecessary software on the bastion increases attack surface
> - Using password authentication instead of SSH keys weakens security
> - Not monitoring bastion host logs misses potential security incidents
>
> ✓ **Quick check:** VM is running with both public and private IPs and shows `BastionHostASG` in the networking section

### **Step 7:** Test Your Implementation

Verify that your security architecture works correctly and that all servers can communicate as designed. This systematic testing approach validates both the functionality and the security boundaries of your infrastructure.

1. **Verify NSG rule effectiveness:**

   ```bash
   # From your local machine, test the Web Server (should fail - no public IP)
   curl http://<WebServer_PublicIP>
   # Expected: Connection timeout or DNS resolution failure

   # Test Reverse Proxy HTTP access (should succeed)
   curl http://<ReverseProxy_PublicIP>
   # Expected: "Hello World!" HTML response
   ```

2. **Test internal DNS resolution:**

   **Connect** to the Bastion Host:

   ```bash
   ssh azureuser@<BastionHost_PublicIP>
   ```

   **Test** internal name resolution:

   ```bash
   # Verify Web Server DNS name resolves
   nslookup webserver.internal.cloudapp.net
   # Expected: Returns the private IP of WebServer

   # Test direct access to Web Server
   curl http://webserver.internal.cloudapp.net:8080
   # Expected: "Hello World!" HTML response
   ```

3. **Test SSH access restrictions:**

   **From your local machine**, attempt SSH to Reverse Proxy (should fail):

   ```bash
   ssh azureuser@<ReverseProxy_PublicIP>
   # Expected: Connection timeout or refused
   ```

   **From Bastion Host**, SSH to other servers (should succeed):

   ```bash
   # Get private IPs from Azure Portal first
   ssh azureuser@<ReverseProxy_PrivateIP>
   # Expected: Successful connection
   ```

4. **Verify Application Security Group associations:**
   - **Navigate to** `ReverseProxyASG` in Azure Portal
   - **Check** that the Reverse Proxy's NIC appears under Associated network interfaces
   - **Navigate to** `BastionHostASG`
   - **Check** that the Bastion Host's NIC appears under Associated network interfaces

5. **Test the complete traffic flow:**
   - **Open** a browser and navigate to `http://<ReverseProxy_PublicIP>`
   - **Verify** you see the "Hello World!" message
   - **Check** that the traffic is being proxied through to the Web Server

> ✓ **Success indicators:**
>
> - Reverse Proxy responds to HTTP requests from the Internet
> - Web Server is accessible only via internal network
> - SSH works only to Bastion Host from Internet
> - Internal DNS resolves VM hostnames correctly
> - Can SSH from Bastion to internal servers using private IPs
> - Web Server directly inaccessible from Internet
>
> ✓ **Final verification checklist:**
>
> - ☐ Virtual Network created with default subnet
> - ☐ Two Application Security Groups created and properly named
> - ☐ Network Security Group has two custom inbound rules
> - ☐ NSG is associated with the default subnet
> - ☐ Web Server has no public IP and no ASG
> - ☐ Reverse Proxy has public IP and ReverseProxyASG
> - ☐ Bastion Host has public IP and BastionHostASG
> - ☐ HTTP request to Reverse Proxy returns "Hello World!"
> - ☐ Direct HTTP access to Web Server fails
> - ☐ SSH to Bastion Host succeeds from Internet
> - ☐ SSH to Reverse Proxy fails from Internet
> - ☐ Internal DNS resolution works from any VM

## Common Issues

> **If you encounter problems:**
>
> **"Connection timeout" when accessing Reverse Proxy:** Check that the NSG rule exists and that the VM is in the ReverseProxyASG. Verify the NSG is associated with the subnet.
>
> **"Unable to resolve host" for internal DNS:** Azure internal DNS takes 1-2 minutes to propagate. Wait a few minutes and try again. Ensure VMs are in the same VNet.
>
> **Cloud-init script didn't run:** Check `/var/log/cloud-init-output.log` on the VM. Common issues include YAML indentation errors or network connectivity problems during package installation.
>
> **SSH to Bastion works but can't reach internal servers:** Verify you're using private IP addresses (10.x.x.x) for internal connections, not public IPs. Check that VMs are in the same subnet.
>
> **Reverse Proxy returns 502 Bad Gateway:** The backend Web Server may not be responding. SSH to Bastion, then to Web Server, and check nginx status with `sudo systemctl status nginx`.
>
> **All HTTP requests fail:** Verify the NSG is associated with the subnet, not just existing as a standalone resource. Check NSG effective rules on the VM's network interface.
>
> **Still stuck?** Check Azure Activity Log for deployment errors. Verify all resources are in the same region. Ensure you're using the correct SSH key for authentication.

## Summary

You've successfully implemented a defense-in-depth network security architecture which:

- ✓ Isolates backend services from direct Internet access
- ✓ Uses Application Security Groups for role-based security policies
- ✓ Implements a bastion host for secure administrative access
- ✓ Demonstrates reverse proxy pattern for traffic routing
- ✓ Leverages Azure internal DNS for service discovery

> **Key takeaway:** Network security is implemented in layers - NSGs provide firewall rules, ASGs provide logical grouping, network topology provides isolation, and the bastion pattern provides controlled access. This defense-in-depth approach means a single security control failure doesn't compromise the entire system. You'll use these patterns whenever building production infrastructure that must protect sensitive workloads.

## Going Deeper (Optional)

> **Want to explore more?**
>
> - Try adding a second web server and configure the reverse proxy for load balancing
> - Implement Azure Firewall for centralized network traffic inspection
> - Add NSG flow logs to monitor and analyze network traffic patterns
> - Configure Azure Bastion service as a managed alternative to the bastion host VM
> - Add User Defined Routes to force traffic through a network virtual appliance
> - Implement VNet peering to connect multiple virtual networks securely

## Done!

Excellent work! You've learned how to design and implement secure network architectures in Azure using multiple layers of security controls. This foundation will help you build production-grade infrastructure that follows cloud security best practices. Understanding these networking and security primitives is essential for any cloud infrastructure project.
