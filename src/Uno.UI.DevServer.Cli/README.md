# Uno Platform DevServer CLI

The Uno DevServer CLI (`uno-devserver`) manages the local development server for [Uno Platform](https://platform.uno) projects. It handles DevServer lifecycle, IDE channel communication, add-in discovery, and exposes an MCP (Model Context Protocol) interface for AI-powered development tools.

## Installation

The CLI is distributed as a .NET tool and is typically invoked via `dotnet dnx`:

```bash
dotnet dnx -y uno.devserver <command>
```

## Commands

| Command | Description |
|---------|-------------|
| `start` | Start or reuse a DevServer for the current solution |
| `stop` | Stop all running DevServer instances |
| `list` | Display running DevServer instances with process ancestry |
| `cleanup` | Remove stale DevServer registrations |
| `disco` | Discover environment details (SDK, packages, active servers) |
| `health` | Report DevServer health for the current workspace |
| `login` | Open Uno Platform settings |
| `mcp serve` | Start the MCP STDIO proxy for AI tool integration |
| `mcp status` | Report MCP server registration state across IDE clients |
| `mcp install` | Register MCP servers in IDE client config files |
| `mcp uninstall` | Remove MCP server registrations |

## How it works

The DevServer is a lightweight local HTTP server that provides bidirectional communication between the IDE and your running Uno Platform application:

1. **IDE opens a solution** — the DevServer reserves a TCP port and writes it to `.csproj.user`
2. **App is built in Debug** — connection details are embedded in the build output
3. **App launches** — it connects back to the DevServer via WebSocket
4. **Development features activate** — Hot Reload, XAML updates, and design-time tools start working

### IDE channel

The IDE channel is a named pipe (`\\.\pipe\{guid}`) providing a direct JSON-RPC link between the IDE extension and the DevServer Host. It carries:

- Development environment status notifications
- Hot Reload coordination messages
- Application launch tracking

When a DevServer is already running (e.g. started by MCP), the CLI reuses it and rebinds the IDE channel without restarting the Host process.

### MCP integration

The CLI exposes an MCP server (`mcp serve`) that AI coding assistants (Claude, Codex, Copilot, Cursor, etc.) can connect to. This enables AI tools to inspect your app, query diagnostics, and interact with the DevServer programmatically.

## Diagnostics

Use `disco` for a full environment diagnostic:

```bash
dotnet dnx -y uno.devserver disco
```

This shows: SDK version, DevServer package version, Host path, .NET version, resolved add-ins, and active server instances with their process ancestry chain.

Use `health` for a quick status check:

```bash
dotnet dnx -y uno.devserver health
```

## Security

The DevServer binds to local ports only and is intended for local development. Do not expose it to untrusted networks.

## Documentation

- [Dev Server overview](https://platform.uno/docs/articles/dev-server.html)
- [Building Uno Platform](https://platform.uno/docs/articles/uno-development/building-uno-ui.html)
- [Contributing](https://platform.uno/docs/articles/uno-development/contributing-intro.html)
