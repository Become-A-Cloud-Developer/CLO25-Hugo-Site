+++
title = "Creating a Virtual Network"
program = "CLO"
cohort = "25"
courses = ["BCD"]
weight = 1
date = 2024-12-02
draft = false
+++

## Goal

Build a secure cloud network infrastructure with three specialized servers to demonstrate network segmentation, security groups, and the principle of least privilege in Azure.

> **What you'll learn:**
>
> - How to create and configure Azure Virtual Networks and subnets
> - When to use Network Security Groups for firewall rules
> - Best practices for server role separation and secure access patterns
> - How reverse proxies and bastion hosts protect internal infrastructure

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ Active Azure subscription with resource creation permissions
> - ✓ Basic understanding of IP addresses and CIDR notation
> - ✓ SSH client installed on your local machine
> - ✓ Familiarity with Linux command line basics

## Exercise Steps

### Overview

1. **Create the Virtual Network Foundation**
2. **Deploy and Configure Web Server**
3. **Deploy and Configure Reverse Proxy**
4. **Deploy Bastion Host**
5. **Secure with Network Security Groups**
6. **Test Your Implementation**

### **Step 1:** Create the Virtual Network Foundation

Establish a Virtual Network (VNet) as the foundation for your cloud infrastructure. A VNet provides isolated network space in Azure where resources can communicate securely, similar to a traditional network in a data center but with cloud scalability and flexibility.

1. **Navigate to** the Azure Portal at <https://portal.azure.com>

2. **Sign in** with your Azure account credentials

3. **Search for** "Virtual Networks" using the search bar at the top

4. **Select** Virtual Networks from the search results

5. **Click** the **+ Create** button to start the creation wizard

6. **Configure** the Basics tab with the following settings:

   - **Subscription**: Select your subscription
   - **Resource Group**: Create new named `DemoRG`
   - **Name**: Enter `DemoVNet`
   - **Region**: Choose `North Europe` or a region close to you

7. **Click** Review + Create, then **click** Create to deploy the Virtual Network

> ℹ **Concept Deep Dive**
>
> A Virtual Network (VNet) is the foundation of cloud networking. The default address space `10.0.0.0/16` provides over 65,000 IP addresses, allowing extensive growth. Azure automatically creates a default subnet `10.0.0.0/24` with 256 addresses, which is sufficient for small to medium deployments. Subnets enable logical segmentation within the VNet for security and resource management.
>
> ⚠ **Common Mistakes**
>
> - Choosing overlapping address spaces with on-premises networks causes VPN connectivity issues
> - Selecting different regions for VNet and VMs increases latency and may incur bandwidth charges
> - Forgetting to create a resource group first interrupts the workflow
>
> ✓ **Quick check:** Navigate to Virtual Networks in the portal and verify `DemoVNet` shows Address Space `10.0.0.0/16` and default subnet `10.0.0.0/24`

![Network Overview](/images/NetworkOverview.png)

### **Step 2:** Deploy and Configure Web Server

Create an Ubuntu VM that will host your web application on a non-standard port. This simulates a typical application server that runs behind a reverse proxy and isn't directly exposed to the internet.

1. **Navigate to** Virtual Machines in the Azure Portal

2. **Click** + Create to start the VM creation wizard

3. **Configure** the Basics tab:

   - **Subscription**: Select the subscription used in Step 1
   - **Resource Group**: Select `DemoRG`
   - **Virtual Machine Name**: Enter `WebServer`
   - **Region**: Use the same region as the VNet
   - **Image**: Choose **Ubuntu Server 24.04 LTS**
   - **Size**: Select `Standard_B1s` (cost-effective for learning)
   - **Authentication Type**: Select **SSH Public Key**
   - **Username**: Enter `azureuser`
   - **SSH Public Key**: Choose **Generate new key pair**

4. **Navigate to** the Networking tab

5. **Configure** networking settings:

   - **Virtual Network**: Select `DemoVNet`
   - **Subnet**: Select `default (10.0.0.0/24)`
   - **Public IP**: Ensure a public IP is assigned
   - **NIC Network Security Group**: Select **Basic**
   - **Public Inbound Ports**: Select **Allow SSH (22)**

6. **Navigate to** the Advanced tab

7. **Add** the following custom data to install nginx:

   ```bash
   #!/bin/bash

   apt update
   apt install nginx -y
   ```

8. **Click** Review + create, then **click** Create

9. **Download** the SSH private key when prompted and save it to your Downloads folder

10. **Wait** for the deployment to complete (approximately 2-3 minutes)

11. **Connect** to the WebServer using SSH:

    ```bash
    chmod 400 ~/Downloads/WebServer_key.pem
    ssh -i ~/Downloads/WebServer_key.pem azureuser@<WebServer_PublicIP>
    ```

12. **Configure** nginx to listen on port 8080 and customize the welcome page:

    ```bash
    sudo sed -i 's/listen 80 default_server;/listen 8080 default_server;/' /etc/nginx/sites-available/default
    sudo sed -i '/listen \[::\]:80 default_server;/d' /etc/nginx/sites-available/default
    sudo nginx -s reload
    sudo sed -i 's/Welcome to nginx/Hello World/g' /var/www/html/index.nginx-debian.html
    ```

13. **Verify** nginx is responding:

    ```bash
    curl localhost:8080
    ```

> ℹ **Concept Deep Dive**
>
> Using custom data (cloud-init) automates software installation during VM provisioning. This Infrastructure-as-Code approach ensures consistent configuration and reduces manual errors. Changing nginx to port 8080 demonstrates that application servers can run on non-standard ports - a common practice when multiple services share a machine or when a reverse proxy handles standard ports.
>
> The `sed` commands perform in-place editing of nginx configuration files. The first two commands change the listening port, while the last command customizes the default page content.
>
> ⚠ **Common Mistakes**
>
> - Forgetting to reload nginx after configuration changes means changes won't take effect
> - Incorrect SSH key permissions (must be 400 or 600) cause "permissions are too open" errors
> - Using the wrong public IP address results in connection timeouts
> - Not removing the IPv6 listener can cause port conflicts on systems with dual-stack networking
>
> ✓ **Quick check:** The `curl localhost:8080` command returns HTML containing "Hello World"

### **Step 3:** Deploy and Configure Reverse Proxy

Create a second VM that acts as a reverse proxy, forwarding external HTTP requests to your internal web server. This architecture pattern provides a security boundary and enables advanced features like load balancing, SSL termination, and caching.

1. **Repeat** the VM creation process from Step 2 with these changes:

   - **Virtual Machine Name**: Enter `ReverseProxy`
   - **All other settings**: Keep identical to WebServer
   - **Custom data**: Use the same nginx installation script

2. **Download** the SSH private key for ReverseProxy

3. **Connect** to the ReverseProxy:

   ```bash
   chmod 400 ~/Downloads/ReverseProxy_key.pem
   ssh -i ~/Downloads/ReverseProxy_key.pem azureuser@<ReverseProxy_PublicIP>
   ```

4. **Test** internal connectivity to the WebServer:

   ```bash
   curl <WebServer_PrivateIP>:8080
   ```

5. **Verify** you receive the "Hello World" response

6. **Configure** nginx as a reverse proxy by editing the default site configuration:

   ```bash
   sudo nano /etc/nginx/sites-available/default
   ```

7. **Replace** the entire file contents with the following configuration (update the IP and port):

   ```nginx
   server {
     listen        80 default_server;
     location / {
       proxy_pass         http://<WebServer_PrivateIP>:8080/;
       proxy_http_version 1.1;
       proxy_set_header   Upgrade $http_upgrade;
       proxy_set_header   Connection keep-alive;
       proxy_set_header   Host $host;
       proxy_cache_bypass $http_upgrade;
       proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
       proxy_set_header   X-Forwarded-Proto $scheme;
     }
   }
   ```

8. **Test** the nginx configuration syntax:

   ```bash
   sudo nginx -t
   ```

9. **Reload** nginx to apply changes:

   ```bash
   sudo nginx -s reload
   ```

10. **Verify** the reverse proxy is working:

    ```bash
    curl localhost
    ```

> ℹ **Concept Deep Dive**
>
> A reverse proxy sits between clients and backend servers, forwarding requests and responses. This architecture provides several benefits: security (backend servers aren't directly exposed), performance (caching, compression), and flexibility (load balancing, SSL termination).
>
> The nginx configuration uses `proxy_pass` to forward requests to the backend. The `proxy_set_header` directives preserve important request information like the original client IP (`X-Forwarded-For`) and protocol (`X-Forwarded-Proto`). The `Connection keep-alive` header enables persistent connections for better performance.
>
> Resources within the same VNet can communicate using private IP addresses without traversing the internet. No firewall exists between VMs in the same VNet by default, enabling seamless internal communication.
>
> ⚠ **Common Mistakes**
>
> - Forgetting to update `<WebServer_PrivateIP>` and port causes proxy errors
> - Missing semicolons in nginx configuration cause syntax errors
> - Not testing configuration before reload can break the service
> - Using the public IP instead of private IP routes traffic through the internet unnecessarily
>
> ✓ **Quick check:** The `curl localhost` command on ReverseProxy returns the "Hello World" page from WebServer

### **Step 4:** Deploy Bastion Host

Create a jump server (bastion host) that provides secure SSH access to internal servers. This implements the principle of least privilege by reducing the attack surface - only the bastion host is exposed to SSH from the internet.

1. **Create** a third VM with these settings:

   - **Virtual Machine Name**: Enter `BastionHost`
   - **All other settings**: Keep identical to previous VMs
   - **Custom data**: Leave empty (nginx not needed)

2. **Download** the SSH private key for BastionHost

3. **Test** SSH access to the BastionHost:

   ```bash
   chmod 400 ~/Downloads/BastionHost_key.pem
   ssh -i ~/Downloads/BastionHost_key.pem azureuser@<BastionHost_PublicIP>
   ```

4. **Exit** the SSH session to return to your local machine:

   ```bash
   exit
   ```

5. **Start** the SSH agent on your local machine:

   ```bash
   eval $(ssh-agent)
   ```

6. **Add** all three private keys to the agent:

   ```bash
   ssh-add ~/Downloads/WebServer_key.pem
   ssh-add ~/Downloads/ReverseProxy_key.pem
   ssh-add ~/Downloads/BastionHost_key.pem
   ```

7. **Verify** keys are loaded:

   ```bash
   ssh-add -l
   ```

8. **Connect** to BastionHost with agent forwarding enabled:

   ```bash
   ssh -A azureuser@<BastionHost_PublicIP>
   ```

9. **From BastionHost**, SSH into WebServer using its private IP:

   ```bash
   ssh -A azureuser@<WebServer_PrivateIP>
   ```

> ℹ **Concept Deep Dive**
>
> A bastion host (jump server) is a security best practice for accessing internal infrastructure. Instead of exposing SSH on every server, only the bastion is publicly accessible. All other servers accept SSH only from internal network addresses.
>
> SSH agent forwarding (`-A` flag) allows you to use your local SSH keys on remote servers without copying private keys to those servers. The agent holds keys in memory and authenticates on your behalf. This is crucial for security - never store private keys on bastion hosts or other shared systems.
>
> Each VM has its own SSH key pair for security. If one key is compromised, it doesn't affect other servers. The agent provides convenience without sacrificing this security boundary.
>
> ⚠ **Common Mistakes**
>
> - Forgetting the `-A` flag means agent forwarding won't work
> - Adding keys after connecting to bastion won't help - keys must be added locally first
> - Using `ssh-copy-id` to copy keys to the bastion violates security best practices
> - Not verifying keys are loaded (`ssh-add -l`) leads to confusing authentication failures
>
> ✓ **Quick check:** You can successfully SSH from your laptop → BastionHost → WebServer using private IPs

### **Step 5:** Secure with Network Security Groups

Apply Network Security Groups (NSGs) to implement firewall rules that enforce the principle of least privilege. Each server will accept only the minimum required network traffic for its role.

1. **Navigate to** the WebServer VM in the Azure Portal

2. **Select** Networking → Network settings from the left menu

3. **Click** the link for `WebServer-nsg` (the Network Security Group)

4. **Navigate to** Settings → Inbound security rules

5. **Delete** the SSH rule by clicking the ellipsis (...) and selecting Delete

6. **Verify** deletion by refreshing the page

7. **Test** that SSH is now blocked from the internet:

   ```bash
   ssh -i ~/Downloads/WebServer_key.pem azureuser@<WebServer_PublicIP>
   ```

8. **Verify** SSH still works from BastionHost:

   ```bash
   ssh -A azureuser@<BastionHost_PublicIP>
   ssh azureuser@<WebServer_PrivateIP>
   ```

9. **Navigate to** the ReverseProxy VM in the Azure Portal

10. **Follow** the link to `ReverseProxy-nsg`

11. **Delete** the SSH rule for ReverseProxy

12. **Add** a new inbound security rule:

    - **Click** + Add
    - **Service**: Select **HTTP**
    - **Click** Add

13. **Test** HTTP access from the internet by opening a browser:

    ```text
    http://<ReverseProxy_PublicIP>
    ```

14. **Verify** you see the "Hello World" page served by WebServer

> ℹ **Concept Deep Dive**
>
> Network Security Groups (NSGs) act as virtual firewalls controlling inbound and outbound traffic. Each NSG contains security rules that allow or deny traffic based on source, destination, port, and protocol. Rules are evaluated by priority number (lower numbers first).
>
> This exercise implements a layered security model:
>
> - **WebServer**: No public access (no SSH, no HTTP) - completely internal
> - **ReverseProxy**: HTTP only (no SSH) - serves web traffic
> - **BastionHost**: SSH only (no HTTP) - administrative access point
>
> This architecture follows the principle of least privilege - each component has exactly the permissions it needs and no more. The WebServer is completely isolated from the internet, reducing its attack surface. All administrative access flows through the hardened bastion host.
>
> ⚠ **Common Mistakes**
>
> - Deleting all rules locks you out of VMs completely
> - Forgetting to add the HTTP rule to ReverseProxy breaks web access
> - Testing with wrong IPs (using WebServer public IP instead of ReverseProxy) gives false results
> - Not verifying rules are applied immediately after changes can cause confusion
>
> ✓ **Quick check:** You cannot SSH to WebServer or ReverseProxy from internet, but you can SSH from BastionHost, and HTTP works on ReverseProxy from internet

### **Step 6:** Test Your Implementation

Verify that your secure network architecture functions correctly and enforces the intended security boundaries. This comprehensive testing ensures all components work together as designed.

1. **Test WebServer isolation:**

   - Try to SSH to WebServer from your laptop (should fail)
   - Try to access `http://<WebServer_PublicIP>:8080` in browser (should timeout)
   - SSH through BastionHost to WebServer (should succeed)
   - Run `curl localhost:8080` on WebServer (should return "Hello World")

2. **Test ReverseProxy security:**

   - Try to SSH to ReverseProxy from your laptop (should fail)
   - SSH through BastionHost to ReverseProxy (should succeed)
   - Access `http://<ReverseProxy_PublicIP>` in browser (should return "Hello World")
   - Verify the content is served from WebServer, not ReverseProxy itself

3. **Test BastionHost access:**

   - SSH directly to BastionHost from your laptop (should succeed)
   - From BastionHost, SSH to both WebServer and ReverseProxy using private IPs (should succeed)
   - Try to access any HTTP service on BastionHost (should fail - no web server installed)

4. **Test internal connectivity:**

   - From ReverseProxy, run `curl <WebServer_PrivateIP>:8080` (should succeed)
   - From WebServer, run `ping <ReverseProxy_PrivateIP>` (should succeed)
   - Verify all servers can communicate internally

5. **Test security boundaries:**

   - Confirm WebServer has no direct internet exposure
   - Confirm only ReverseProxy accepts HTTP from internet
   - Confirm only BastionHost accepts SSH from internet

> ✓ **Success indicators:**
>
> - WebServer is completely inaccessible from the internet (SSH and HTTP both blocked)
> - ReverseProxy serves web content but blocks SSH from internet
> - BastionHost allows SSH but has no HTTP service
> - All servers are accessible from BastionHost using private IPs
> - Web traffic flows: Internet → ReverseProxy → WebServer
> - Administrative traffic flows: Internet → BastionHost → Internal Servers
>
> ✓ **Final verification checklist:**
>
> - ☐ Virtual Network created with address space 10.0.0.0/16
> - ☐ All three VMs deployed and running
> - ☐ WebServer responds on port 8080 with "Hello World"
> - ☐ ReverseProxy forwards HTTP traffic to WebServer
> - ☐ SSH agent forwarding works through BastionHost
> - ☐ NSG rules enforce correct security boundaries
> - ☐ No direct SSH or HTTP access to WebServer from internet
> - ☐ HTTP works on ReverseProxy, SSH blocked
> - ☐ SSH works on BastionHost

## Common Issues

> **If you encounter problems:**
>
> **SSH connection timeout:** Verify you're using the correct public IP and that your local firewall allows outbound SSH
>
> **Permission denied (publickey):** Check SSH key file permissions with `ls -l`, should be 400 or 600. Use `chmod 400` to fix
>
> **nginx: configuration file test failed:** Review nginx config file for missing semicolons or curly braces
>
> **Reverse proxy returns 502 Bad Gateway:** Verify WebServer_PrivateIP and port 8080 are correct in ReverseProxy nginx config
>
> **Agent forwarding not working:** Ensure you started ssh-agent and added keys before connecting, use `ssh-add -l` to verify
>
> **Cannot access WebServer from BastionHost:** Verify you're using the private IP address, not the public IP
>
> **HTTP access blocked after NSG changes:** Wait 30-60 seconds for NSG rule changes to propagate across Azure infrastructure
>
> **Still stuck?** Check Azure Activity Log for deployment errors, and verify all VMs are in the same VNet and subnet

## Summary

You've successfully implemented a secure cloud network architecture which:

- ✓ Creates isolated network space with Azure Virtual Networks
- ✓ Implements server role separation for security and maintainability
- ✓ Uses Network Security Groups for defense-in-depth security
- ✓ Applies the principle of least privilege to minimize attack surface

> **Key takeaway:** Proper network architecture is essential for security because it creates defense layers that protect your infrastructure even if one layer is compromised. This pattern of using reverse proxies and bastion hosts is industry-standard for production deployments in cloud environments.

## Going Deeper (Optional)

> **Want to explore more?**
>
> - Try adding a fourth VM and isolating it in a separate subnet with its own NSG
> - Research how Azure Application Gateway differs from nginx as a reverse proxy
> - Implement SSL/TLS termination on the ReverseProxy using Let's Encrypt
> - Add Azure Bastion service as an alternative to your custom bastion host
> - Create a custom NSG that allows SSH only from your specific public IP address
> - Explore VNet peering to connect multiple virtual networks

## Done! 🎉

Excellent work! You've learned how to build secure network architecture and can now design and implement cloud infrastructure that follows security best practices. This foundation will help you create production-ready systems that protect sensitive workloads and data.
