# TDS Reverse Proxy for SQL Server

This project provides a simple reverse proxy for Microsoft SQL Server<sup>®</sup> using 
the Tabular Data Stream (TDS) protocol. It allows you to forward SQL Server 
connections from one server to another, enabling scenarios such as load 
balancing, failover, caching, interception, or routing traffic through a 
secure gateway.

Currently, the proxy supports TDS versions 7.0 through 7.4, which correspond
to SQL Server versions from 2000 to 2019. It also implements the SQL Server
Resolution Protocol (SSRP) to resolve server names and ports for named
instances.

## Maturity

⚠️ This project is in development and is not suitable for production use.
It runs, it works. It will process queries. But eventually you will find a 
bug and it will crash.

## Overview

- `TdsServerListener`: Listens for incoming TDS connections from clients, 
  accepts them, and forwards them to the provided `ITdsServerHandlerFactory`.
- `TdsClientConnection`: Client connection that connects to the target SQL 
  Server and forwards TDS packets.
- `UnicastSsrpClient`: Implements the SQL Server Resolution Protocol 
  (SSRP) to resolve the server name and port for a given SQL Server instance.

## Test Proxy

A test proxy is included in Rmg.Tds.TestProxy.  It listens on a specified port
and forwards connections to a target SQL Server instance.  Each batch received
is checked if it matches a "translation".  If it does, the batch is replaced 
with the translation before being forwarded to the server.

## Related

- [[MS-TDS]: Tabular Data Stream Protocol Specification](https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-tds/b46a581a-39de-4745-b076-ec4dbb7d13ec)
- [[MS-SQLR]: SQL Server Resolution Protocol](https://learn.microsoft.com/en-us/openspecs/windows_protocols/mc-sqlr/1ea6e25f-bff9-4364-ba21-5dc449a601b7)

## License

This software is licensed under the AGPL-3.0 License. See the LICENSE file 
for details.  For commercial licensing, please contact the author.

This project is a third-party, independent effort and has no relationship 
with Microsoft Corporation, based on public protocol specification documents.
Microsoft® and SQL Server® are registered trademarks of Microsoft Corporation.