+++
title = "Common Server Roles"
program = "CLO"
cohort = "25"
courses = ["BCD"]
type = "slide"
date = 2024-11-17
draft = false
hidden = true

theme = "sky"
[revealOptions]
controls= true
progress= true
history= true
center= true
+++

## Server Roles
- Servers often carry out a specific task tailored to business and application needs. This is commonly known as the _server role_
---
## Key Server Roles
- **Web Server**: Serves web pages or applications to clients.
- **Application Server**: Manages and runs business logic for apps.
- **Proxy Server**: Intermediary enhancing security and performance.
- **Bastion Host**: Secure access point for internal network management.
- **Database Server**: Stores and manages structured data.
- **File Server**: Centralized file storage and sharing.
- **DHCP Server**, **Mail Server**, **DNS Server**, **Print Server**, **FTP/SFTP Server**, **Domain Controller**, **Media Server**, etc
---
## Web Server
- Processes HTTP/HTTPS requests and delivers web content.
- Handles static (e.g., HTML) and dynamic (e.g., server-side) content.
- Examples:
    - Apache
    - Nginx
    - Microsoft IIS.
---
## Application Server
- Executes business logic and integrates backend services.
- Ideal for multi-tier architectures and complex transactions.
- Examples:
    - Apache Tomcat
    - JBoss
    - Microsoft IIS.
---
## Proxy Server
- **Forward Proxy**: Masks clients and handles _outbound_ requests.
- **Reverse Proxy**: Directs _inbound_ traffic to backend servers for load balancing and security.
- Examples:
    - Squid
    - HAProxy
    - Nginx
---
## Bastion Host
- Provides secure access to private networks from external sources like the Internet
- Acts as a gateway for _remote administration_.
- Examples:
    - AWS Bastion Host
    - Azure Bastion
    - Fail2Ban
---
## Summary
- **Web Server**: For serving _web content_.
- **Application Server**: For running _business applications_.
- **Proxy Server**: For added security and _traffic management_.
- **Bastion Host**: For secure access to _remote administration_.
