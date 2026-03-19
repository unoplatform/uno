# Spec 002: MCP Setup — Cross-IDE Server Registration

> **Status**: Approved
> **Author**: Carl de Billy
> **Date**  : 2026-02-27

---

## Executive Summary

### The Problem

The Uno DevServer exposes two MCP servers (UnoApp for in-app tooling, UnoDocs for documentation). Today, these are registered via `vscode.lm.registerMcpServerDefinitionProvider` — an API exclusive to the VS Code Copilot flow. Users on **Cursor, Gemini Antigravity, Gemini CLI, Windsurf, Kiro, Junie in Rider, Visual Studio**, desktop clients like **Claude Desktop**, and CLI agents like **Claude Code, GitHub Copilot CLI, and OpenCode** get no MCP integration at all.

Each editor has its own MCP config format and file location. There is no cross-editor registration API.

### What We're Changing

The DevServer CLI (`uno.devserver`) gains MCP setup subcommands that **scan, register, and unregister MCP servers** by writing to each client's native config files.

All MCP functionality is unified under a single `mcp` command group:

```
uno.devserver mcp start      # STDIO proxy (was --mcp-app)
uno.devserver mcp status     # Report installation state of MCP servers across clients
uno.devserver mcp install    # Register servers in client configs
uno.devserver mcp uninstall  # Remove servers from client configs
```

### Why This Approach

- **The tool is the canonical source of truth for Uno definitions**: The CLI owns the list of Uno MCP servers and the canonical definitions it writes. The VS Code extension (and any future client integration) does not hardcode server names, transports, or definitions — it processes whatever `status` returns. `status` must also recognize compatible registrations that were persisted by the client's own tooling in the profile's documented config files.
- **Adding servers requires no extension update**: Adding, removing, or modifying servers is done entirely in the tool.
- **Works for all editors**: Config-file-based registration works for every editor, regardless of whether it exposes a programmatic API.
- **Backward compatible**: `--mcp-app` continues to work as an alias for `mcp start`.

### Scope

- 13 MCP client profiles in v1 (`copilot-vscode`, `copilot-vs`, `copilot-cli`, `cursor`, `windsurf`, `kiro`, `gemini-antigravity`, `gemini-cli`, `junie-rider`, `claude-code`, `claude-desktop`, `opencode`, `unknown`)
- 2 MCP servers (UnoApp stdio, UnoDocs HTTP) — extensible without protocol changes
- 3 operations: status, install, uninstall
- Config file merge with preservation of existing entries

---

## 1. CLI Surface

### Command Group

```
uno.devserver mcp <subcommand> [options]
```

### Subcommands

| Subcommand | Description |
|------------|-------------|
| `start` | Start the MCP STDIO proxy (existing functionality, was `--mcp-app`) |
| `status` | Report installation state of MCP servers across all detected clients |
| `install` | Register MCP servers in the target client's config files |
| `uninstall` | Remove MCP servers from the target client's config files |

### Backward Compatibility

`--mcp-app` remains as an alias for `mcp start`. All existing `--mcp-app` flags and arguments continue to work unchanged. The existing MCP STDIO proxy logic is not modified — only the routing changes.

### Wire Format Stability

The server definitions written to client config files use `--mcp-app` (not `mcp start`) in the args array. This is the **stable wire format** — it works with all versions of the tool, including versions that predate the `mcp` command group. The `mcp start` syntax is CLI sugar for interactive use only and is never written to config files.

### Subcommand: `mcp start`

```
uno.devserver mcp start [--port <port>] [--mcp-wait-tools-list] [--force-roots-fallback] [--force-generate-tool-cache] [--solution-dir <path>]
```

This is the existing MCP STDIO proxy. All current options remain:

| Option | Description |
|--------|-------------|
| `--port <port>` | Port for DevServer (default: auto-allocate) |
| `--mcp-wait-tools-list` | Wait for upstream server tools before responding to `list_tools` |
| `--force-roots-fallback` | Disable MCP roots feature (for clients that don't support it) |
| `--force-generate-tool-cache` | Force tool discovery and persist cache immediately |
| `--solution-dir <path>` | Explicit solution root |

### Subcommand: `mcp status`

```
uno.devserver mcp status [<client>] [--workspace <path>] [--channel <stable|prerelease>|--tool-version <ver>] [--json]
```

### Subcommand: `mcp install`

```
uno.devserver mcp install <client> [--workspace <path>] [--channel <stable|prerelease>|--tool-version <ver>] [--servers UnoApp,UnoDocs] [--json]
```

### Subcommand: `mcp uninstall`

```
uno.devserver mcp uninstall <client> [--workspace <path>] [--servers UnoApp,UnoDocs] [--all-scopes] [--json]
```

### Shared Parameters

| Parameter | Required | Applies to | Description |
|-----------|----------|------------|-------------|
| `<client>` | See below | status, install, uninstall | MCP client identifier as a positional argument (see [Client Profiles](#5-client-profiles)). For `install` and `uninstall`: required unless `--all-ides` is used. For `status`: optional — when omitted, reports on all known client profiles (the `detected` field indicates which have config paths on disk). |
| `--workspace <path>` | No | status, install, uninstall | Absolute path to workspace root. **Default: current working directory.** Must be an existing directory. Filesystem root paths (`/`, `C:\`, etc.) are rejected (exit code 2). |
| `--channel <stable|prerelease>` | No | status, install | Force the expected Uno MCP definition channel (overrides auto-detection) |
| `--tool-version <ver>` | No | status, install | Pin a specific Uno MCP tool version in the server definition (for QA). Mutually exclusive with `--channel` |
| `--servers <list>` | No | install, uninstall | Comma-separated server names from the server definitions. Unknown names are rejected (exit code 2). Duplicates are permitted but do not change behavior. Default: all servers. |
| `--all-scopes` | No | uninstall | Remove matching registrations from every configured path for the target client. Without this flag, uninstall only modifies the client profile's `writeTarget`, matching the default install scope. |
| `--json` | No | status, install, uninstall | Emit JSON output to stdout. Without this flag, output is human-readable text to stdout |
| `--ide-definitions <path>` | No | status, install, uninstall | Path to an external `ide-profiles.json`, replacing the embedded client profiles (see [Definitions Files](#12-definitions-files)) |
| `--server-definitions <path>` | No | status, install, uninstall | Path to an external `server-definitions.json`, replacing the embedded server definitions (see [Definitions Files](#12-definitions-files)) |

### Client Identifiers

`copilot-vscode`, `copilot-vs`, `copilot-cli`, `cursor`, `windsurf`, `kiro`, `gemini-antigravity`, `gemini-cli`, `junie-rider`, `claude-code`, `claude-desktop`, `opencode`, `unknown`

> **v2 (deferred):** `continue`, `zed` — excluded from initial implementation due to non-standard config formats (see [Client Profiles §5](#5-client-profiles)).

### Output Convention

By default, output is **human-readable text** to stdout. When `--json` is passed, output is **JSON** to stdout. Diagnostic messages always go to **stderr** (via ILogger). This matches the existing `disco` / `disco --json` pattern.

---

## 2. Status Response

**Command:**
```
uno.devserver mcp status [<client>] [--workspace <path>] [--channel <stable|prerelease>|--tool-version <ver>] [--json]
```

When `<client>` is provided, it identifies the **caller client** and is included in the output as `callerIde`. The tool scans config paths for **all known client profiles** and returns per-client status for each server, giving a complete picture of what is already configured and what could be configured.

`status` is the **scanner of record**. It is not limited to entries previously written by `uno.devserver`: it must recognize Uno server registrations persisted by the client's own tooling, as long as they are written to the profile's documented config files and use the documented schema variants. It does **not** promise to detect transient, internal, or non-persisted client state.

The **expected variant** (used to determine `registered` vs `outdated`) is resolved as follows:
1. `--tool-version <ver>` → expected variant is `pinned:<ver>`
2. `--channel prerelease` → expected variant is `prerelease`
3. `--channel stable` → expected variant is `stable`
4. None of the above → **auto-detect** from the running tool's own version (`AssemblyInformationalVersionAttribute`). A version containing `-` (e.g., `5.6.0-dev.42`) means `prerelease`; otherwise `stable`.

The options `--channel` and `--tool-version` are **mutually exclusive**.

`--channel` and `--tool-version` select the expected Uno MCP definition. Any `dnx --prerelease` or `dnx --version` written to IDE config files is derived output from that selected definition, not a user-facing MCP setup flag contract.

### Human-Readable Output (default)

```
MCP Server Status
=================

Clients detected: copilot-vscode, cursor
Caller client:     cursor
Expected:      prerelease (auto-detected from tool v5.6.0-dev.42)

UnoApp (stdio)
  copilot-vscode outdated stable         .vscode/mcp.json
                          prerelease     ~/.vscode/mcp.json
               ⚠ Registered in multiple config files
  cursor       missing
  claude-code  registered prerelease     .mcp.json

UnoDocs (http)
  copilot-vscode registered stable       ~/.vscode/mcp.json
  cursor       registered stable         ~/.cursor/mcp.json
  claude-code  missing
```

### JSON Output (`--json`)

All paths in the JSON output are **fully resolved absolute paths** — no tokens (`{ws}`, `~`) or relative paths. This ensures callers can use them directly without resolution logic.

```json
{
  "version": "1.0",
  "callerIde": "cursor",
  "toolVersion": "5.6.0-dev.42",
  "expectedVariant": "prerelease",
  "detectedIdes": ["copilot-vscode", "cursor"],
  "supportedIdes": [
    { "ide": "copilot-vscode", "strategy": "file", "detected": true },
    { "ide": "copilot-vs", "strategy": "file", "detected": false },
    { "ide": "copilot-cli", "strategy": "native", "detected": false },
    { "ide": "cursor", "strategy": "file", "detected": true }
  ],
  "servers": [
    {
      "name": "UnoApp",
      "transport": "stdio",
      "definition": {
        "command": "dnx",
        "args": ["-y", "--prerelease", "uno.devserver", "--mcp-app"]
      },
      "ides": [
        {
          "ide": "copilot-vscode",
          "status": "outdated",
          "locations": [
            { "path": "/home/user/myproject/.vscode/mcp.json", "variant": "stable", "transport": "stdio" },
            { "path": "/home/user/.vscode/mcp.json", "variant": "prerelease", "transport": "stdio" }
          ],
          "warnings": ["Registered in multiple config files"]
        },
        { "ide": "cursor", "status": "missing" },
        { "ide": "junie-rider", "status": "missing" },
        {
          "ide": "claude-code",
          "status": "registered",
          "locations": [
            { "path": "/home/user/myproject/.mcp.json", "variant": "prerelease", "transport": "stdio" }
          ]
        }
      ]
    },
    {
      "name": "UnoDocs",
      "transport": "http",
      "definition": {
        "url": "https://mcp.platform.uno/v1"
      },
      "ides": [
        {
          "ide": "copilot-vscode",
          "status": "registered",
          "locations": [
            { "path": "/home/user/.vscode/mcp.json", "variant": "stable", "transport": "http" }
          ]
        },
        {
          "ide": "cursor",
          "status": "registered",
          "locations": [
            { "path": "/home/user/.cursor/mcp.json", "variant": "stable", "transport": "http" }
          ]
        },
        { "ide": "claude-code", "status": "missing" },
        { "ide": "junie-rider", "status": "missing" }
      ]
    }
  ]
}
```

### Top-Level Fields

| Field | Type | Description |
|-------|------|-------------|
| `version` | string | Protocol version (`"1.0"`) |
| `callerIde` | string? | Client identifier as passed via `<client>`. `null` when omitted (status only). |
| `toolVersion` | string | Version of the running `uno.devserver` tool |
| `expectedVariant` | string | The variant that `install` would write: `stable`, `prerelease`, or `pinned:<ver>` |
| `detectedIdes` | string[] | Client identifiers for profiles whose config path or parent directory was found on disk |
| `supportedIdes` | array | Supported MCP clients, with their registration strategy and current detection state |
| `servers` | array | All MCP servers the tool manages, with per-client status |

### Server Entry Fields (`servers[]`)

| Field | Type | Description |
|-------|------|-------------|
| `name` | string | Server name (e.g., `"UnoApp"`, `"UnoDocs"`) |
| `transport` | string | `"stdio"` or `"http"` |
| `definition` | object | The canonical server definition that `install` would write (reflects `expectedVariant`) |
| `ides` | array | Per-client registration status |

### Per-client Status Fields (`servers[].ides[]`)

| Field | Type | Description |
|-------|------|-------------|
| `ide` | string | MCP client identifier |
| `status` | string | `"registered"`, `"missing"`, or `"outdated"` |
| `locations` | array? | Config files where the server was found. Each entry has `path` (string), `variant` (string), and `transport` (string). Absent when `missing`. |
| `warnings` | string[]? | Diagnostic messages (e.g., duplicate registration across scopes). Absent when empty. |

All known client profiles are included in `servers[].ides[]`. `detectedIdes` tells consumers which of those profiles were found on disk.

#### Location Entry Fields (`servers[].ides[].locations[]`)

| Field | Type | Description |
|-------|------|-------------|
| `path` | string | Config file path where the server was found |
| `variant` | string | Version variant detected: `stable`, `prerelease`, `pinned:<ver>`, or `legacy-http` |
| `transport` | string | Transport inferred from the matching entry: `"stdio"` or `"http"` |

### Status Values

The `status` is determined by examining the **effective configuration** — the highest-priority entry found across all scanned config paths (first match in scan order = most local scope):

| Status | Meaning |
|--------|---------|
| `registered` | Server found, and the effective (highest-priority) entry's content matches the expected definition |
| `missing` | Server not found in any scanned config path for this client |
| `outdated` | Server found in at least one config, but the effective entry's content does not match the expected definition |

> **Rationale**: Clients resolve config by precedence — a workspace entry shadows a global entry. The status reflects what the client will actually use. If the workspace has a `stable` entry and global has `prerelease`, the effective config is `stable` (workspace wins), so the status is `outdated` when `prerelease` is expected. Conversely, if only the global has the server and it matches, the status is `registered` — the global IS the effective config when no workspace entry shadows it.
>
> **Content matching**: For status determination, the tool compares the effective entry's actual content (command, args, url) against the expected definition. Two entries with identical content are considered matching regardless of variant label. This means UnoDocs (where all variants have the same URL) is always `registered` if found.

### Warning Values

| Warning | Trigger |
|---------|---------|
| `Registered in multiple config files` | Server found in 2+ config files for the same client (e.g., workspace and global) |
| `Multiple entries found in the same config file` | 2+ keys in the same config file match the detection patterns for the same server |

### Variant Values

| Variant | Meaning | Config signature |
|---------|---------|------------------|
| `stable` | Standard release definition | `dnx -y uno.devserver ...` (no version flags) |
| `prerelease` | Prerelease definition | `dnx -y --prerelease uno.devserver ...` |
| `pinned:<ver>` | Specific version pinned | `dnx -y --version <ver> uno.devserver ...` |
| `legacy-http` | Old HTTP-based registration | `localhost:{port}/mcp` URL |

### Extension Usage

The caller filters the response to its own client to decide what to register:

```typescript
const myClient = response.callerIde;
if (myClient) {
    for (const server of response.servers) {
        const myStatus = server.ides.find(i => i.ide === myClient);
        if (!myStatus || myStatus.status !== 'registered') {
            // needs registration for this client
        }
    }
}
```

The full multi-client data (available when `callerIde` is `null`, i.e., no client specified) can be used for diagnostics, status display, or telemetry.

---

## 3. Install Response

**Command:**
```
uno.devserver mcp install <client> [--workspace <path>] [--channel <stable|prerelease>|--tool-version <ver>] [--servers UnoApp,UnoDocs] [--json]
```

### Human-Readable Output (default)

```
MCP Install
===========

  UnoApp   created  /home/user/myproject/.cursor/mcp.json
  UnoDocs  skipped  /home/user/.cursor/mcp.json             Already registered and up-to-date
```

### JSON Output (`--json`)

All paths are fully resolved absolute paths.

```json
{
  "version": "1.0",
  "operations": [
    {
      "server": "UnoApp",
      "ide": "cursor",
      "action": "created",
      "path": "/home/user/myproject/.cursor/mcp.json",
      "reason": null
    },
    {
      "server": "UnoDocs",
      "ide": "cursor",
      "action": "skipped",
      "path": "/home/user/.cursor/mcp.json",
      "reason": "Already registered and up-to-date"
    }
  ]
}
```

### Action Values

| Action | Meaning |
|--------|---------|
| `created` | Server entry was added to a new or existing config file |
| `updated` | Existing server entry was modified (e.g., upgraded from HTTP to stdio) |
| `skipped` | Server already registered correctly, no changes made |
| `error` | Operation failed (e.g., file is read-only, malformed JSON) |

### Multi-Scope Behavior

Before writing, `install` scans **all** configPaths for the target client and determines the effective status (see [Status Values](#status-values)):
- If `registered` (effective config matches expected) → `skipped`
- If `outdated` (effective config doesn't match) → writes the updated definition to the `writeTarget` and reports `updated`. This shadows any global entry with a correct workspace entry.
- If `missing` (not found anywhere) → writes to the `writeTarget` and reports `created`

### Error in Operations

If an individual operation fails, it is reported in the operations array:

```json
{
  "server": "UnoApp",
  "ide": "cursor",
  "action": "error",
  "path": "/home/user/myproject/.cursor/mcp.json",
  "reason": "File is read-only"
}
```

---

## 4. Uninstall Response

**Command:**
```
uno.devserver mcp uninstall <client> [--workspace <path>] [--servers UnoApp,UnoDocs] [--json]
```

### Human-Readable Output (default)

```
MCP Uninstall
=============

  UnoApp   removed    /home/user/myproject/.cursor/mcp.json
  UnoApp   removed    /home/user/.cursor/mcp.json
  UnoDocs  not_found
```

### JSON Output (`--json`)

All paths are fully resolved absolute paths.

```json
{
  "version": "1.0",
  "operations": [
    {
      "server": "UnoApp",
      "ide": "cursor",
      "action": "removed",
      "path": "/home/user/myproject/.cursor/mcp.json",
      "reason": null
    },
    {
      "server": "UnoApp",
      "ide": "cursor",
      "action": "removed",
      "path": "/home/user/.cursor/mcp.json",
      "reason": null
    },
    {
      "server": "UnoDocs",
      "ide": "cursor",
      "action": "not_found",
      "path": null,
      "reason": null
    }
  ]
}
```

### Action Values

| Action | Meaning |
|--------|---------|
| `removed` | Server entry was removed from a config file. One operation per file — if the server appears in multiple scopes, there will be multiple `removed` entries for the same server. |
| `not_found` | Server was not found in any scanned config path |
| `error` | Operation failed |

Each operation entry also includes the target `ide` identifier.

### Scope Behavior

By default, uninstall removes the server only from the client profile's `writeTarget` scope, matching where `install` writes. Passing `--all-scopes` removes matching registrations from **all** configured paths (workspace and global) for the target client.

> **Note for global-only clients** (for example `gemini-antigravity`): since the only config is global, uninstall still has workspace-spanning effect even without `--all-scopes`. This is inherent to the client's config model, not a tool limitation.

---

## 5. Client Profiles

Each MCP client has a profile that defines config file locations, write targets, JSON format, and registration strategy.

> **Scan vs Write**: All config locations listed are scanned during `status` and before `install`/`uninstall`. The write target is where `install` creates or updates entries — always the most local (workspace-specific) path. Scan order indicates priority (first = most specific/local).

### Copilot / Editor Family

| Client | Config Locations (scan order) | Write Target | JSON Format |
|-----|-------------------------------|--------------|-------------|
| **copilot-vscode** | `{ws}/.vscode/mcp.json`, `~/.vscode/mcp.json`, global OS path | `{ws}/.vscode/mcp.json` | `servers` |
| **copilot-vs** | `{ws}/.vs/mcp.json`, `{ws}/.vscode/mcp.json` | `{ws}/.vs/mcp.json` | `servers` |
| **Cursor** | `{ws}/.cursor/mcp.json`, `~/.cursor/mcp.json` | `{ws}/.cursor/mcp.json` | `mcpServers` |
| **Windsurf** | `{ws}/.windsurf/mcp.json`, `~/.codeium/windsurf/mcp_config.json` | `{ws}/.windsurf/mcp.json` | `mcpServers` with `serverUrl` for HTTP servers |
| **Kiro** | `{ws}/.kiro/settings/mcp.json`, `~/.kiro/settings/mcp.json` | `{ws}/.kiro/settings/mcp.json` | `mcpServers` |
| **gemini-antigravity** | `~/.gemini/antigravity/mcp_config.json` | same (global only) | `mcpServers` with `type` and `serverUrl` |

VS Code global OS paths:
- Windows: `%APPDATA%/Code/User/settings.json`
- macOS: `~/Library/Application Support/Code/User/settings.json`
- Linux: `~/.config/Code/User/settings.json`

> **Shared workspace note**: Visual Studio can also consume the shared workspace-level `.vscode/mcp.json` registration. As a result, `copilot-vscode` and `copilot-vs` may both report `registered` against the same `.vscode/mcp.json` file. `copilot-vs` still keeps `{ws}/.vs/mcp.json` as its dedicated write target so it can diverge when needed.

### JetBrains / Agent Hosts

| Client | Config Locations | Write Target | JSON Format |
|-----|------------------|--------------|-------------|
| **junie-rider** | `{ws}/.idea/mcpServers.json` | `{ws}/.idea/mcpServers.json` | `mcpServers` |

> **Note**: Junie's global Rider MCP config lives under a version-specific directory (e.g., `~/.config/JetBrains/Rider2025.1/mcpServers.json`). This path varies per Rider version and is excluded from v1 to avoid complex version-enumeration logic. Workspace-level config is sufficient for the primary use case.

### CLI Agents (tool only, no extension)

| Agent | Config Locations | Write Target | JSON Format |
|-------|------------------|--------------|-------------|
| **Claude Code** | `{ws}/.mcp.json`, `~/.claude.json` | `{ws}/.mcp.json` | `mcpServers` |
| **copilot-cli** | *(no file-backed config contract in v1)* | *(native/manual registration only)* | Native/manual flow |
| **OpenCode** | `{ws}/opencode.json`, `{ws}/opencode.jsonc` | `{ws}/opencode.json` | `mcp` with `type` and command-array format |
| **Continue** *(v2)* | `{ws}/.continue/config.json` (MCP section) | same | `mcpServers` (nested) |
| **Zed** *(v2)* | `{ws}/.zed/settings.json` (section) | same | `context_servers` |

> **v2 note:** Continue and Zed are excluded from the initial implementation. Continue uses an array-based `mcpServers` format (and is migrating to YAML), while Zed nests `context_servers` inside a multi-purpose `settings.json`. Both require specialized merge logic beyond the simple "write under root key" approach used for all other IDEs.

### Generic (unknown client)

| Client | Config Locations | Write Target | JSON Format |
|-----|------------------|--------------|-------------|
| `unknown` | `{ws}/.vscode/mcp.json` | `{ws}/.vscode/mcp.json` | `servers` |

### Native/manual clients

Some clients expose a native registration workflow instead of a stable writable config-file contract. In v1:

- `copilot-cli` is a **supported native/manual client**
- `status --json` exposes it in `supportedIdes` with `strategy: "native"`
- `install` / `uninstall` return guidance instead of pretending to edit a config file

### Notation

- `{ws}` = workspace root (value of `--workspace`, or current working directory)
- `~` = user home directory

---

## 6. Server Definitions

### UnoApp (stdio)

The UnoApp definition varies based on the resolved variant. The MCP setup CLI selects a Uno-specific channel or pinned tool version; the `dnx` runner's `--prerelease` and `--version` flags only appear as derived output in the generated definition.

#### Invocation Syntaxes

The tool **writes** the canonical dnx runner syntax, but **detects** all known invocation forms:

| Syntax | Form | Written by install | Detected |
|--------|------|--------------------|----------|
| dnx runner (canonical) | `dnx -y uno.devserver --mcp-app` | Yes | Yes |
| Global tool | `uno.devserver --mcp-app` | No | Yes |
| dotnet dnx | `dotnet dnx -y uno.devserver --mcp-app` | No | Yes |
| Legacy HTTP | `http://localhost:{port}/mcp` | No | Yes (as `legacy-http` variant) |

> **v1 scope note**: `dotnet tool run` (local install) is not detected in v1.

**Stable** (default when tool is a release version):
```json
{
  "command": "dnx",
  "args": ["-y", "uno.devserver", "--mcp-app"]
}
```

**Prerelease** (default when tool is a prerelease version, or when `--channel prerelease` is passed):
```json
{
  "command": "dnx",
  "args": ["-y", "--prerelease", "uno.devserver", "--mcp-app"]
}
```

**Pinned** (when `--tool-version <ver>` is passed):
```json
{
  "command": "dnx",
  "args": ["-y", "--version", "5.6.0-dev.42", "uno.devserver", "--mcp-app"]
}
```

### Version Resolution

| Running tool version | No override | `--channel stable` | `--channel prerelease` | `--tool-version 1.2.3` |
|---------------------|-------------|--------------------|-------------------------|-------------------------|
| `5.6.0` (stable) | stable | stable | prerelease | pinned:1.2.3 |
| `5.6.0-dev.42` (prerelease) | prerelease | stable | prerelease | pinned:1.2.3 |

The tool reads its own version from `AssemblyInformationalVersionAttribute` (already used in `CliManager.ShowBanner()`). A version containing `-` is considered prerelease.

### UnoDocs (HTTP)

```json
{
  "url": "https://mcp.platform.uno/v1"
}
```

### Format Mapping

The tool maps these abstract definitions into the concrete JSON format required by each client profile.

**VS Code format (`servers`)** — includes `type` field:
```json
{
  "servers": {
    "UnoApp": {
      "type": "stdio",
      "command": "dnx",
      "args": ["-y", "uno.devserver", "--mcp-app"]
    },
    "UnoDocs": {
      "type": "http",
      "url": "https://mcp.platform.uno/v1"
    }
  }
}
```

**Cursor/mcpServers format** — no `type` field:
```json
{
  "mcpServers": {
    "UnoApp": {
      "command": "dnx",
      "args": ["-y", "uno.devserver", "--mcp-app"]
    },
    "UnoDocs": {
      "url": "https://mcp.platform.uno/v1"
    }
  }
}
```

---

## 7. Duplicate Detection

The tool scans config paths for **all known client profiles** and detects existing registrations. This provides a complete picture across all supported clients, not just the caller's.

The scan is **exhaustive**: all configPaths are checked for every client, not just the first match. A server may appear in multiple config files for the same client (e.g., workspace and global). This is reported as a warning in the status response.

Detection is intentionally **writer-agnostic**. A matching entry may have been created by `uno-devserver`, by the client's own registration workflow, or by a user editing the official config file manually. The guarantee is limited to the documented file locations and supported schema shapes for each profile; hidden or ephemeral client state is out of scope.

### Detection Patterns

A config entry is considered a match for a server when **either** of these conditions is met:
1. **Key name match**: The entry's key matches a `keyPattern` (exact name match — high confidence)
2. **Content match**: The entry's command/args or URL matches a `commandPattern` or `urlPattern` (content-based — catches renamed entries)

This two-tier approach avoids false positives: a key-only match (`^UnoApp$`) is safe because the name is specific, while content patterns are only checked against command/URL fields (not key names), and are specific enough to avoid matching non-Uno servers.

| Server | Key Name Match | Content Match |
|--------|---------------|---------------|
| **UnoApp** | Key is `UnoApp` | Command contains `uno.devserver` + `--mcp-app` (any invocation syntax), OR URL matches `localhost:{port}/mcp` (legacy HTTP — any local MCP endpoint) |
| **UnoDocs** | Key is `UnoDocs` | URL contains `mcp.platform.uno` |

#### Multiple Matches in Same File

If multiple keys in the same config file match the same server's detection patterns, the **first match** (in JSON key order) is used for status/update. A warning `"Multiple entries found in the same config file"` is emitted. During `install`, only the first match is updated; during `uninstall`, all matches are removed.

### Outdated Detection

A server is `outdated` when:
- UnoApp is registered as HTTP (`localhost:{port}/mcp`) instead of stdio (`legacy-http` variant)
- UnoApp variant does not match the expected variant (e.g., `stable` config but tool expects `prerelease`)
- UnoDocs URL has changed

---

## 8. Config Merge Rules

| Scenario | Behavior |
|----------|----------|
| Parent directory does not exist | Create parent directories, then create file with Uno servers only |
| File does not exist | Create file with Uno servers only |
| File exists, valid JSON | Add/update Uno server entries, **preserve all other entries** |
| File exists, malformed JSON | **Error** — do not overwrite. Report in operations as `action: "error"` |
| File exists, read-only | **Error** — report in operations as `action: "error"` |
| File exists, server already present and up-to-date | **Skip** — report as `action: "skipped"` |
| File exists, server present but outdated | **Update** — replace entry, report as `action: "updated"` |

### Key Behavior: Duplicate Under Different Key

When an existing duplicate is found under a different key name (e.g., user named it `"Uno"` instead of `"UnoDocs"`), the merge updates *that key's entry* rather than creating a second entry. The key name is preserved to respect the user's naming choice.

### Shallow Merge of Server Entries

When updating an existing server entry, the merge uses **shallow merge** semantics: keys from the new definition are added or overwritten, but **unknown keys already present** in the entry (e.g., `inputs`, `env`, `disabled`, `alwaysAllow`) are preserved. This respects user customizations that the tool does not manage.

Example — existing entry:
```json
{
  "UnoApp": {
    "command": "dnx",
    "args": ["-y", "uno.devserver", "--mcp-app"],
    "env": { "DOTNET_ROOT": "/usr/share/dotnet" },
    "disabled": false
  }
}
```

After `install --channel prerelease`, the entry becomes:
```json
{
  "UnoApp": {
    "command": "dnx",
    "args": ["-y", "--prerelease", "uno.devserver", "--mcp-app"],
    "env": { "DOTNET_ROOT": "/usr/share/dotnet" },
    "disabled": false
  }
}
```

The `env` and `disabled` keys are preserved; `command` and `args` are updated.

### Relationship to Native Client Tooling

`install` and `uninstall` are the canonical Uno writers, but the merge and scan rules must remain compatible with config files that were originally produced by the client's own MCP tooling. In practice:

- `status` must recognize supported Uno registrations even when `uno-devserver` did not create them
- `install` may update an existing compatible entry in place rather than creating a duplicate
- `uninstall` removes matching Uno registrations regardless of which writer originally created them

This compatibility applies only to persisted config files in the documented locations and shapes for each client profile.

### JSONC Compatibility

Client config files may contain JSONC extensions (comments and trailing commas). The tool handles them as follows:

- **Reading**: `JsonCommentHandling.Skip` and `AllowTrailingCommas = true`, matching the existing pattern in `EntryPoint.Mcp.cs`
- **Writing**: Comments are preserved when rewriting existing JSONC files. Trailing commas may still be normalized away during the rewrite.

### Formatting

- All writes use **2-space indentation** (`WriteIndented = true`). Existing indentation is not preserved — the file is reformatted on every write. This matches the existing `EntryPoint.Mcp.cs` behavior.
- All files are written as **UTF-8 without BOM**, regardless of existing encoding.
- Trailing newline is appended after the closing `}`.

### Atomic Writes

All file writes use a temp-file-then-rename pattern to prevent corruption on interrupted writes:

1. Write content to `<target-path>.tmp`
2. `File.Move(tempPath, targetPath, overwrite: true)`
3. On error: best-effort cleanup of the `.tmp` file

This matches the existing pattern in `ToolListManager.cs`. The atomic write is implemented inside `FileSystem.WriteAllText()` (the production `IFileSystem`), not in the caller. `InMemoryFileSystem` does not need atomic writes.

### Concurrency

No file locking. Concurrent writes use last-write-wins semantics. This is acceptable because MCP setup operations are user-initiated (not automatic background tasks), and the risk of concurrent modification is negligible.

### Source Control

MCP config files are workspace-specific and typically safe to commit (they contain no secrets — only tool commands and public URLs). The tool does not modify `.gitignore`.

---

## 9. Exit Codes

| Code | Meaning | Examples |
|------|---------|----------|
| `0` | Success — the command produced an exploitable payload with at least one successful or non-terminal operation | Install where 1 of 2 servers failed (JSON contains per-operation status) |
| `1` | Failure — no useful work was done, a fatal error occurred, or every install/uninstall operation was terminal (`error` / `not_found`) | Unknown `<client>`, invalid `--workspace`, malformed definitions file, uninstall where every selected server is `not_found` |
| `2` | Usage error — invalid arguments or parameter combinations | Missing required `<client>` for install, mutually exclusive flags, unknown `--servers` name |

> **Design note**: Exit code 0 with per-operation error details in JSON (action `"error"`) is preferred for partial failures. This preserves the richest possible contract for callers. Exit code 1 is also used when install/uninstall completes but every operation is terminal (`"error"` or `"not_found"`), so shell scripts can distinguish total failure from partial success without losing the rich payload.

---

## 10. Protocol Versioning

Every response includes `"version": "1.0"`.

The caller checks compatibility by parsing the major version:

```typescript
const response = JSON.parse(stdout);
const major = parseInt(response.version?.split('.')[0] ?? '0', 10);
if (major !== 1) {
    log.appendLine(`[Warn] Unsupported MCP tool protocol version: ${response.version}`);
    return; // skip processing
}
```

Future protocol versions:
- `1.x` — backwards-compatible additions (new fields, new status values)
- `2.0` — breaking changes (would require caller update)

---

## 11. Manual QA Plan

Manual QA is performed against the **distributed .NET tool package**, not by running the CLI from source. This validates the real installation path used by customers.

### Package Under Test

- Package ID: `Uno.DevServer`
- Tool command: `uno-devserver`
- Install shape: global .NET tool from a test feed or package source

### Environment Preparation

1. Prepare a feed containing the package version under test.
2. Remove any previously installed global tool version:

```powershell
dotnet tool uninstall -g Uno.DevServer
```

3. Install the candidate package:

```powershell
dotnet tool install -g Uno.DevServer --add-source <feed> --version <version>
```

4. Create a disposable workspace:

```powershell
$qa = Join-Path $env:TEMP "uno-mcp-qa"
Remove-Item $qa -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $qa | Out-Null
Set-Location $qa
```

### Required Scenarios

1. **Empty workspace status**
   - Run:
   ```powershell
   uno-devserver mcp status --json
   ```
   - Verify:
     - JSON payload is valid
- `servers[].ides[]` is exhaustive for all known client profiles
     - `detectedIdes` only lists profiles whose config paths or parent directories exist on disk

2. **copilot-vscode workspace install**
   - Run:
   ```powershell
   New-Item -ItemType Directory -Path ".vscode" | Out-Null
   uno-devserver mcp install copilot-vscode --workspace $qa --servers UnoApp --json
   Get-Content .\.vscode\mcp.json
   ```
   - Verify:
     - `.vscode\mcp.json` is created
     - root key is `servers`
     - `UnoApp` uses the expected stdio shape

3. **Detection of client-persisted config**
   - Manually create an official config file in one supported shape without using `uno-devserver`, for example:
     - copilot-vscode: `.vscode\mcp.json`
     - Cursor: `.cursor\mcp.json`
     - Claude Code: `.mcp.json`
   - Run:
   ```powershell
   uno-devserver mcp status --json
   ```
   - Verify:
     - the Uno server is detected
     - the server appears in `locations`
     - `transport` is correct
- This confirms the scanner recognizes registrations persisted by native client tooling or manual edits to the official file.

4. **Windsurf HTTP format**
   - Run:
   ```powershell
   uno-devserver mcp install windsurf --workspace $qa --servers UnoDocs --json
   ```
   - Verify:
     - output file is `.windsurf\mcp.json`
     - root key is `mcpServers`
     - HTTP endpoint uses `serverUrl`
     - no `url` key is written for the UnoDocs entry

5. **Gemini Antigravity HTTP format**
   - Run:
   ```powershell
   uno-devserver mcp install gemini-antigravity --workspace $qa --servers UnoDocs --json
   ```
   - Verify:
     - output file is `%USERPROFILE%\.gemini\antigravity\mcp_config.json`
     - root key is `mcpServers`
     - entry includes `type`
     - HTTP endpoint uses `serverUrl`

6. **OpenCode format**
   - Run:
   ```powershell
   uno-devserver mcp install opencode --workspace $qa --servers UnoApp,UnoDocs --json
   ```
   - Verify:
     - output file is `opencode.json`
     - root key is `mcp`
     - `UnoApp` uses `type: "local"` and a single `command` array
     - `UnoDocs` uses `type: "remote"`

7. **Duplicate detection / no-op install**
   - Re-run an install for an already up-to-date entry:
   ```powershell
   uno-devserver mcp install copilot-vscode --workspace $qa --servers UnoApp --json
   ```
   - Verify:
     - no duplicate entry is created
     - operation is reported as `skipped`
     - process exit code is `0`

8. **Uninstall current scope by default**
- Ensure the same server exists in both a workspace and a global config for one client, then run:
   ```powershell
   uno-devserver mcp uninstall copilot-vscode --workspace $qa --servers UnoApp --json
   ```
   - Verify:
     - only the write-target scope is modified by default
     - the other scope remains untouched
     - payload remains valid JSON

9. **Uninstall across scopes**
- Ensure the same server exists in both a workspace and a global config for one client, then run:
   ```powershell
   uno-devserver mcp uninstall copilot-vscode --workspace $qa --servers UnoApp --all-scopes --json
   ```
   - Verify:
     - all matching persisted registrations are reported
     - multiple `removed` operations may be emitted for the same server
     - payload remains valid JSON

10. **Partial failure with usable payload**
   - Create a mixed scenario where one target file is writable and one is read-only, then run install or uninstall.
   - Verify:
     - at least one operation reports `action: "error"`
      - the command still emits the full JSON payload
      - the process exit code is `0` because the payload is still actionable

11. **CLI help**
    - Run:
    ```powershell
    uno-devserver --help
    ```
    - Verify:
      - help text documents the `mcp` command group
      - `trae` and `aider` are absent

### Evidence To Capture

For each scenario, capture:

- exact command line
- resulting exit code
- affected config file path
- relevant JSON excerpt from stdout or the written config file
- any mismatch between expected and observed behavior

---

## 12. Implementation Architecture

### File Structure

All new code under `src/Uno.UI.DevServer.Cli/Mcp/Setup/`:

| File | Responsibility |
|------|----------------|
| `McpSetupModels.cs` | All model records: `IdeProfile` (config paths, write target, JSON root key, strategy, per-client formatting controls, `excludeFromDetection`), `ServerDefinition`, `Definitions`, response DTOs (`StatusResponse`, `OperationResponse`, `ServerIdeStatus`, `LocationEntry`, `OperationEntry`) |
| `DefinitionsLoader.cs` | Loads `ide-profiles.json` and `server-definitions.json` from embedded resources or external files (`--ide-definitions`, `--server-definitions`). See [Definitions Files](#12-definitions-files) |
| `ServerDefinitionResolver.cs` | Builds concrete `JsonObject` definitions from a `ServerDefinition` template. Applies variant resolution (stable/prerelease/pinned) based on auto-detect + CLI overrides |
| `DuplicateDetector.cs` | Pure static methods: `IsUnoAppEntry(keyName, jsonObj)`, `IsUnoDocsEntry(keyName, jsonObj)`, `IsUpToDate(existing, expected)` |
| `ConfigScanner.cs` | Reads config files via `IFileSystem`, uses `DuplicateDetector` to determine per-server status |
| `ConfigWriter.cs` | Pure JSON merge with JSONC reading (`CommentHandling.Skip`, `AllowTrailingCommas`), shallow merge of server entries, and `RemoveServer`. Methods: `MergeServer(content, rootKey, serverName, definition)` and `RemoveServer(content, rootKey, serverName)` |
| `IFileSystem.cs` | I/O abstraction: `FileExists`, `DirectoryExists`, `ReadAllText`, `WriteAllText`, `CreateDirectory`, `IsReadOnly`, `GetUserHomePath`, `GetAppDataPath` |
| `FileSystem.cs` | Production `IFileSystem` wrapping `System.IO` |
| `McpSetupOrchestrator.cs` | `Status()`, `Install()`, `Uninstall()` — orchestrates scanner + writer via `IFileSystem` |
| `CliCommandRunner.cs` | Detects agent CLIs in PATH, expands argument templates with placeholders (`{name}`, `{command}`, `{args...}`, `{url}`), executes processes, captures output. Used by orchestrator for CLI-first registration |
| `McpSetupModels.cs` | *(updated)* Added `CliProfile` record (`Executable`, `Detect`, `AddStdio`, `AddHttp`, `List`, `Remove`) and optional `Cli` field on `IdeProfile` |

### Modified Files

| File | Change |
|------|--------|
| `CliManager.cs` | `RunMcpSubcommand()` dispatcher: routes `status/install/uninstall` to orchestrator. `mcp start` routed in `RunAsync()` via workspace resolution. `--mcp-app` remains as alias |
| `Program.cs` | DI registration for `IFileSystem`, `CliCommandRunner`, and `McpSetupOrchestrator`. Updated help text with `mcp` command group |

### Testability

Single I/O seam via `IFileSystem`. All business logic is fully testable with an `InMemoryFileSystem` dictionary-backed fake. Tests go in `src/Uno.UI.DevServer.Cli.Tests/Mcp/Setup/`:

| Test File | Coverage |
|-----------|----------|
| `InMemoryFileSystem.cs` | `IFileSystem` fake for tests |
| `Given_DuplicateDetector.cs` | Key/command/URL matching, negatives |
| `Given_ConfigWriter.cs` | Create/update/skip/remove, malformed JSON, preserve entries, format mapping |
| `Given_ConfigScanner.cs` | Registered/outdated/missing status per client |
| `Given_McpSetupOrchestrator.cs` | End-to-end status/install/uninstall with `InMemoryFileSystem` |
| `Given_DefinitionsLoader.cs` | Embedded loading, external override, malformed JSON handling, schema validation |
| `Given_CliCommandRunner.cs` | Placeholder expansion (`{name}`, `{command}`, `{args...}`, `{url}`), `IsAvailable` detection |
| `Given_McpCliStrategy.cs` | CLI-first delegation, fallback on CLI failure/unavailability, dry-run, uninstall with null Remove |

### Design Principles

- **Pure logic extraction**: follows the `MonitorDecisions.cs` pattern — no I/O in business logic
- **Minimal dependency surface**: primary JSON manipulation uses `System.Text.Json.Nodes` (`JsonNode`, `JsonObject`, `JsonArray`) already available in net9.0; `Newtonsoft.Json` is added only where required to preserve comments when updating JSONC config files
- **Manual arg parsing**: consistent with existing `CliManager` patterns (no System.CommandLine migration)
- **JSON options**: reuses `McpJsonUtilities.DefaultOptions` for deserialization; camelCase + indented for output serialization

---

## 13. Definitions Files

Client profiles and server definitions are stored in two separate embedded JSON files rather than in static C# registries. This makes it possible to add or modify client support and server definitions without recompiling the tool.

| File | Content | Override parameter |
|------|---------|--------------------|
| `ide-profiles.json` | MCP client config paths, write targets, strategy, and JSON format | `--ide-definitions <path>` |
| `server-definitions.json` | MCP server invocation, variants, detection patterns | `--server-definitions <path>` |

### Resolution Order

For each file independently:

1. **External override**: If the corresponding `--*-definitions <path>` parameter is passed, load from that file path. If the file does not exist or is malformed JSON, fail with exit code 1.
2. **Embedded resource**: Otherwise, load from the embedded resource compiled into the CLI assembly.

Each external override **completely replaces** its corresponding embedded file — the two are not merged. The two files are independent: overriding one does not affect the other.

### Use Cases for External Override

- **Testing**: Supply a minimal definitions file scoped to the client/server under test
- **QA**: Validate a new client profile or server definition before shipping it in the embedded file
- **Customization**: Add a non-standard client or server not yet supported by the tool

### Embedded Resource Configuration

```xml
<!-- Uno.UI.DevServer.Cli.csproj -->
<ItemGroup>
  <EmbeddedResource Include="Mcp\Setup\ide-profiles.json" LogicalName="ide-profiles.json" />
  <EmbeddedResource Include="Mcp\Setup\server-definitions.json" LogicalName="server-definitions.json" />
</ItemGroup>
```

Loaded at runtime via:

```csharp
var ideStream = typeof(DefinitionsLoader).Assembly
    .GetManifestResourceStream("ide-profiles.json");
var serverStream = typeof(DefinitionsLoader).Assembly
    .GetManifestResourceStream("server-definitions.json");
```

---

### Client Profiles (`ide-profiles.json`)

The root is a JSON object keyed by MCP client identifier (matching the CLI client identifiers).

```json
{
  "<ide-id>": {
    "configPaths": ["<path-template>", ...],
    "writeTarget": "<path-template>",
    "jsonRootKey": "<root-key>",
    "includeType": false,
    "urlKey": null,
    "typeMap": null,
    "mergeCommandArgs": false
  }
}
```

#### Fields

| Field | Type | Description |
|-------|------|-------------|
| `configPaths` | string[] | Ordered list of config file path templates to scan (all are checked). Order indicates priority — first entry is the most local/specific scope. |
| `writeTarget` | string | Path template where `install` writes new entries. Must be one of the `configPaths`. |
| `jsonRootKey` | string | JSON root key used by this client: `"servers"`, `"mcpServers"`, or `"mcp"` |
| `includeType` | boolean | Whether install writes a `type` field alongside the server definition. Used by `copilot-vscode`, `copilot-vs`, `gemini-antigravity`, and OpenCode. |
| `strategy` | string | Registration strategy for the client. `"file"` means the tool scans and writes config files; `"native"` means the tool surfaces guidance but does not write files directly. |
| `manualRegistrationMessage` | string? | Guidance emitted for native/manual clients when install or uninstall is requested. |
| `urlKey` | string? | Alternate key for HTTP endpoints. When set to `"serverUrl"`, install writes `serverUrl` instead of `url`. |
| `typeMap` | object? | Optional transport remapping for `type`. Used by OpenCode to map `stdio -> local` and `http -> remote`. |
| `mergeCommandArgs` | boolean | When true, `command` and `args` are merged into a single command array. Used by OpenCode. |

#### Client-specific format notes

- **copilot-vscode** writes under `servers` and includes `type` for both stdio and HTTP registrations.
- **copilot-vs** writes under `servers`, includes `type`, and scans both `.vs/mcp.json` and the shared workspace `.vscode/mcp.json`.
- **gemini-antigravity** writes under `mcpServers`, includes `type`, and stores HTTP endpoints under `serverUrl`.
- **Windsurf** writes under `mcpServers` and stores HTTP endpoints under `serverUrl` without a `type` field.
- **OpenCode** writes under `mcp`, includes `type`, stores HTTP endpoints under `url`, and rewrites stdio definitions from `command + args` into a single command array.

#### Path Template Tokens

| Token | Expansion |
|-------|-----------|
| `{workspace}` | Value of `--workspace` parameter (or current working directory if omitted) |
| `{home}` | User home directory (`Environment.GetFolderPath(UserProfile)`) |
| `{appdata}` | OS-specific application data path (Windows: `%APPDATA%`, macOS: `~/Library/Application Support`, Linux: `~/.config`) |

> **Implementation note — `{appdata}` on macOS**: `Environment.GetFolderPath(SpecialFolder.ApplicationData)` returns `~/.config` on macOS (XDG convention), **not** `~/Library/Application Support`. The `IFileSystem.GetAppDataPath()` implementation must use OS-specific logic:
> ```csharp
> public string GetAppDataPath() =>
>     OperatingSystem.IsMacOS()
>         ? Path.Combine(Environment.GetFolderPath(
>               Environment.SpecialFolder.UserProfile), "Library", "Application Support")
>         : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
> ```

> **Implementation note — `{appdata}` on WSL**: when the tool runs inside WSL, `{appdata}` should resolve to the **Windows roaming AppData path** (for example `/mnt/c/Users/<user>/AppData/Roaming`), not Linux `~/.config`. This ensures Windows-hosted desktop clients such as Claude Desktop and VS Code remain detectable and writable from a WSL workspace.

#### Complete `ide-profiles.json`

```json
{
  "gemini-antigravity": {
    "configPaths": [
      "{home}/.gemini/antigravity/mcp_config.json"
    ],
    "writeTarget": "{home}/.gemini/antigravity/mcp_config.json",
    "jsonRootKey": "mcpServers",
    "includeType": true,
    "urlKey": "serverUrl"
  },
  "gemini-cli": {
    "configPaths": [
      "{home}/.gemini/settings.json"
    ],
    "writeTarget": "{home}/.gemini/settings.json",
    "jsonRootKey": "mcpServers"
  },
  "copilot-cli": {
    "configPaths": [],
    "writeTarget": "",
    "jsonRootKey": "mcpServers",
    "strategy": "native",
    "manualRegistrationMessage": "Use the native registration flow for GitHub Copilot CLI."
  },
  "claude-code": {
    "configPaths": [
      "{workspace}/.mcp.json",
      "{home}/.claude.json"
    ],
    "writeTarget": "{workspace}/.mcp.json",
    "jsonRootKey": "mcpServers"
  },
  "claude-desktop": {
    "configPaths": [
      "{appdata}/Claude/claude_desktop_config.json"
    ],
    "writeTarget": "{appdata}/Claude/claude_desktop_config.json",
    "jsonRootKey": "mcpServers"
  },
  "cursor": {
    "configPaths": [
      "{workspace}/.cursor/mcp.json",
      "{home}/.cursor/mcp.json"
    ],
    "writeTarget": "{workspace}/.cursor/mcp.json",
    "jsonRootKey": "mcpServers"
  },
  "kiro": {
    "configPaths": [
      "{workspace}/.kiro/settings/mcp.json",
      "{home}/.kiro/settings/mcp.json"
    ],
    "writeTarget": "{workspace}/.kiro/settings/mcp.json",
    "jsonRootKey": "mcpServers"
  },
  "junie-rider": {
    "configPaths": [
      "{workspace}/.idea/mcpServers.json"
    ],
    "writeTarget": "{workspace}/.idea/mcpServers.json",
    "jsonRootKey": "mcpServers"
  },
  "copilot-vscode": {
    "configPaths": [
      "{workspace}/.vscode/mcp.json",
      "{home}/.vscode/mcp.json",
      "{appdata}/Code/User/settings.json"
    ],
    "writeTarget": "{workspace}/.vscode/mcp.json",
    "jsonRootKey": "servers",
    "includeType": true
  },
  "copilot-vs": {
    "configPaths": [
      "{workspace}/.vs/mcp.json",
      "{workspace}/.vscode/mcp.json"
    ],
    "writeTarget": "{workspace}/.vs/mcp.json",
    "jsonRootKey": "servers",
    "includeType": true
  },
  "windsurf": {
    "configPaths": [
      "{workspace}/.windsurf/mcp.json",
      "{home}/.codeium/windsurf/mcp_config.json"
    ],
    "writeTarget": "{workspace}/.windsurf/mcp.json",
    "jsonRootKey": "mcpServers",
    "urlKey": "serverUrl"
  },
  "opencode": {
    "configPaths": [
      "{workspace}/opencode.json",
      "{workspace}/opencode.jsonc"
    ],
    "writeTarget": "{workspace}/opencode.json",
    "jsonRootKey": "mcp",
    "includeType": true,
    "typeMap": {"stdio": "local", "http": "remote"},
    "mergeCommandArgs": true
  },
  "unknown": {
    "configPaths": [
      "{workspace}/.vscode/mcp.json"
    ],
    "writeTarget": "{workspace}/.vscode/mcp.json",
    "jsonRootKey": "servers"
  }
}
```

---

### Server Definitions (`server-definitions.json`)

The root is a JSON object keyed by server name. Each entry defines the server's transport, canonical invocation variants, and patterns used to detect existing registrations across client config files.

```json
{
  "<server-name>": {
    "transport": "stdio | http",
    "variants": { ... },
    "detection": { ... }
  }
}
```

#### Fields

| Field | Type | Description |
|-------|------|-------------|
| `transport` | string | `"stdio"` or `"http"` |
| `variants` | object | Per-variant definition templates. Keys: `stable`, `prerelease`, `pinned` |
| `detection` | object | Patterns for duplicate detection across config files |

#### Variant Templates

Each variant contains the JSON properties that will be written into the client config file (the "inner" definition, without the server name key).

For `pinned` variants, the placeholder `{version}` is replaced with the value of `--tool-version <ver>` at runtime.

#### Detection Patterns

| Field | Type | Description |
|-------|------|-------------|
| `keyPatterns` | string[] | Regex patterns matched against the config entry's key name (case-insensitive) |
| `commandPatterns` | string[]? | Regex patterns matched against the `command` + `args` fields (stdio servers only) |
| `urlPatterns` | string[]? | Regex patterns matched against the `url` field (HTTP servers or legacy entries) |

A config entry is considered a match if **any** `keyPattern` matches the entry's key name, **or** if **any** `commandPattern` or `urlPattern` matches the entry's content (see [Detection Patterns §7](#detection-patterns)).

#### Complete `server-definitions.json`

```json
{
  "UnoApp": {
    "transport": "stdio",
    "variants": {
      "stable": {
        "command": "dnx",
        "args": ["-y", "uno.devserver", "--mcp-app"]
      },
      "prerelease": {
        "command": "dnx",
        "args": ["-y", "--prerelease", "uno.devserver", "--mcp-app"]
      },
      "pinned": {
        "command": "dnx",
        "args": ["-y", "--version", "{version}", "uno.devserver", "--mcp-app"]
      }
    },
    "detection": {
      "keyPatterns": ["^UnoApp$"],
      "commandPatterns": [
        "dnx.*uno\\.devserver.*--mcp-app",
        "uno\\.devserver.*--mcp-app",
        "dotnet\\s+dnx.*uno\\.devserver.*--mcp-app"
      ],
      "urlPatterns": ["localhost.*uno[._-]?devserver.*/mcp", "localhost:\\d+/mcp"]
    }
  },
  "UnoDocs": {
    "transport": "http",
    "variants": {
      "stable": {
        "url": "https://mcp.platform.uno/v1"
      },
      "prerelease": {
        "url": "https://mcp.platform.uno/v1"
      },
      "pinned": {
        "url": "https://mcp.platform.uno/v1"
      }
    },
    "detection": {
      "keyPatterns": ["^UnoDocs$"],
      "urlPatterns": ["mcp\\.platform\\.uno"]
    }
  }
}
```

#### UnoApp Detection Notes

The canonical invocation (`dnx` runner) is what `install` writes, but `detection` recognizes multiple invocation syntaxes:

| Syntax | Form | Detection Pattern |
|--------|------|-------------------|
| dnx runner (canonical) | `dnx -y uno.devserver --mcp-app` | `dnx.*uno\.devserver.*--mcp-app` |
| Global tool | `uno.devserver --mcp-app` | `uno\.devserver.*--mcp-app` |
| dotnet dnx | `dotnet dnx -y uno.devserver --mcp-app` | `dotnet\s+dnx.*uno\.devserver.*--mcp-app` |
| Legacy HTTP (Uno-specific URL) | `http://localhost:{port}/uno.devserver/mcp` | `localhost.*uno[._-]?devserver.*/mcp` |
| Legacy HTTP (generic local) | `http://localhost:{port}/mcp` | `localhost:\d+/mcp` |

> **v1 scope**: `dotnet tool run` (local install) is not detected.

### DefinitionsLoader API

```csharp
internal static class DefinitionsLoader
{
    /// <summary>
    /// Loads MCP client profiles and server definitions from embedded resources,
    /// with optional external file overrides.
    /// </summary>
    public static Definitions Load(
        IFileSystem? fs = null,
        string? ideDefinitionsPath = null,
        string? serverDefinitionsPath = null);
}

internal sealed record Definitions(
    IReadOnlyDictionary<string, IdeProfile> Ides,
    IReadOnlyDictionary<string, ServerDefinition> Servers);

internal sealed record IdeProfile(
    string[] ConfigPaths,
    string WriteTarget,
    string JsonRootKey,
    bool IncludeType = false,
    string? UrlKey = null,
    IReadOnlyDictionary<string, string>? TypeMap = null,
    bool MergeCommandArgs = false,
    string Strategy = "file",
    string? ManualRegistrationMessage = null,
    bool ExcludeFromDetection = false);

internal sealed record ServerDefinition(
    string Transport,
    Dictionary<string, JsonObject> Variants,
    DetectionPatterns Detection);

internal sealed record DetectionPatterns(
    string[] KeyPatterns,
    string[]? CommandPatterns,
    string[]? UrlPatterns);
```

### Impact on Other Components

| Component | Before | After |
|-----------|--------|-------|
| `IdeProfileRegistry.cs` | Static C# registry of `IdeProfile` records | **Removed** — profiles come from `DefinitionsLoader` (loaded from `ide-profiles.json`) |
| `ServerDefinitionRegistry.cs` | Static C# methods building `JsonObject` per variant | **Replaced by** `ServerDefinitionResolver.cs` — reads templates from `server-definitions.json`, applies variant + `{version}` substitution |
| `DuplicateDetector.cs` | Hardcoded command/URL patterns | Reads `detection` patterns from `server-definitions.json` and compiles to `Regex` (cached) |
| `ConfigScanner.cs` | Takes `IdeProfile` from registry | Takes `IdeProfile` from `DefinitionsLoader.Load()` |
| `McpSetupOrchestrator.cs` | Depends on both registries | Depends on `DefinitionsLoader` (single data source) |
| `CliManager.cs` | Passes no definitions arg | Reads `--ide-definitions` and `--server-definitions` and passes to `DefinitionsLoader.Load()` |

---

## 14. CLI-First Registration Strategy

### Motivation

Some agents expose their own CLI for MCP management (e.g., `claude mcp add`, `codex mcp add`, `gemini mcp add`). Using the agent's own CLI for registration:

- Delegates format ownership to the agent (eliminates format mismatch bugs)
- Provides better forward-compatibility when agents update their config format
- Enables bidirectional validation: our `mcp status` can verify CLI-registered servers, and agent CLIs can verify our file writes

### Flow

```
mcp install <client>
  │
  ├─ profile.Strategy == "native"?  → return manual registration message
  │
  ├─ profile.Cli != null AND executable in PATH?
  │   ├─ YES: execute agent CLI (e.g., `claude mcp add ...`)
  │   │   ├─ exit 0 → report "created" (via CLI)
  │   │   └─ exit ≠ 0 → log warning, fall through to file
  │   └─ NO: fall through to file
  │
  └─ File-based: ConfigWriter.MergeServer (existing behavior)
```

The same pattern applies to `mcp uninstall`: CLI first if `cli.Remove` is defined, otherwise file-based.

### CliProfile Schema

Added as an optional `cli` field in `ide-profiles.json`:

```json
{
  "cli": {
    "executable": "claude",
    "detect": ["--version"],
    "addStdio": ["mcp", "add", "--scope", "project", "--transport", "stdio", "{name}", "--", "{command}", "{args...}"],
    "addHttp": ["mcp", "add", "--scope", "project", "--transport", "http", "{name}", "{url}"],
    "list": ["mcp", "list"],
    "remove": ["mcp", "remove", "{name}"]
  }
}
```

**Placeholders:**
| Placeholder | Type | Description |
|---|---|---|
| `{name}` | string | Server name (e.g., `UnoApp`, `UnoDocs`) |
| `{command}` | string | Executable command (e.g., `dnx`) |
| `{args...}` | string[] | Arguments expanded in-place as individual items |
| `{url}` | string | HTTP server URL |

### Agent CLI Support Matrix

| Agent | add | list | remove | Executable | Notes |
|-------|:---:|:----:|:------:|------------|-------|
| Claude Code | ✅ | ✅ | ✅ | `claude` | `--scope project`, full bidirectional |
| Gemini CLI | ✅ | ✅ | ✅ | `gemini` | `-s project`, full bidirectional |
| Codex CLI | ✅ | ✅ | ✅ | `codex` | `--json` on list, global scope only |
| Cursor | — | ✅ | — | `cursor-agent` | Read-only verification |
| OpenCode | — | ✅ | ✅ | `opencode` | List + remove only |
| Others | — | — | — | — | File-based only |

### Status Coherence

`mcp status` always scans on-disk config files as the source of truth. When a server is registered via the agent's CLI:

- **Claude Code** (`--scope project`): writes to `{workspace}/.mcp.json` — already in our `configPaths`, detected automatically
- **Gemini CLI** (`-s project`): writes to `{workspace}/.gemini/settings.json` — added to `configPaths` in this update
- **Codex CLI**: writes to `~/.codex/config.toml` (TOML format) — not scannable by our JSON scanner. Future work: use `codex mcp list --json` for status
