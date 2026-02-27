# Spec 002: MCP Setup — Cross-IDE Server Registration

> **Status**: Approved
> **Author**: Carl de Billy
> **Date**  : 2026-02-27

---

## Executive Summary

### The Problem

The Uno DevServer exposes two MCP servers (UnoApp for in-app tooling, UnoDocs for documentation). Today, these are registered via `vscode.lm.registerMcpServerDefinitionProvider` — an API exclusive to VS Code. Users on **Cursor, Anti-Gravity, Windsurf, Kiro, Trae, Rider**, and CLI agents like **Claude Code, Aider, OpenCode** get no MCP integration at all.

Each editor has its own MCP config format and file location. There is no cross-editor registration API.

### What We're Changing

The DevServer CLI (`uno.devserver`) gains MCP setup subcommands that **scan, register, and unregister MCP servers** by writing to each IDE's native config files.

All MCP functionality is unified under a single `mcp` command group:

```
uno.devserver mcp start      # STDIO proxy (was --mcp-app)
uno.devserver mcp status     # Report installation state of MCP servers across IDEs
uno.devserver mcp install    # Register servers in IDE configs
uno.devserver mcp uninstall  # Remove servers from IDE configs
```

### Why This Approach

- **The tool is the single source of truth**: The CLI owns the list of MCP servers. The VS Code extension (and any future IDE extension) does not hardcode server names, transports, or definitions — it processes whatever `status` returns.
- **Adding servers requires no extension update**: Adding, removing, or modifying servers is done entirely in the tool.
- **Works for all editors**: Config-file-based registration works for every editor, regardless of whether it exposes a programmatic API.
- **Backward compatible**: `--mcp-app` continues to work as an alias for `mcp start`.

### Scope

- 13+ IDE profiles (VS Code, Cursor, Windsurf, Kiro, Trae, Anti-Gravity, Rider, Claude Code, OpenCode, Aider, Continue, Zed, unknown)
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
| `status` | Report installation state of MCP servers across all detected IDEs |
| `install` | Register MCP servers in the target IDE's config file |
| `uninstall` | Remove MCP servers from the target IDE's config file |

### Backward Compatibility

`--mcp-app` remains as an alias for `mcp start`. All existing `--mcp-app` flags and arguments continue to work unchanged. The existing MCP STDIO proxy logic is not modified — only the routing changes.

### Wire Format Stability

The server definitions written to IDE config files use `--mcp-app` (not `mcp start`) in the args array. This is the **stable wire format** — it works with all versions of the tool, including versions that predate the `mcp` command group. The `mcp start` syntax is CLI sugar for interactive use only and is never written to config files.

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
uno.devserver mcp status [<ide>] [--workspace <path>] [--release|--prerelease|--version <ver>] [--json]
```

### Subcommand: `mcp install`

```
uno.devserver mcp install <ide> [--workspace <path>] [--release|--prerelease|--version <ver>] [--servers UnoApp,UnoDocs] [--json]
```

### Subcommand: `mcp uninstall`

```
uno.devserver mcp uninstall <ide> [--workspace <path>] [--servers UnoApp,UnoDocs] [--json]
```

### Shared Parameters

| Parameter | Required | Applies to | Description |
|-----------|----------|------------|-------------|
| `<ide>` | See below | status, install, uninstall | IDE identifier as positional argument (see [IDE Profiles](#5-ide-profiles)). Also accepted as `--ide <ide>`. If both positional and `--ide` are provided and differ, exit code 2. For `install` and `uninstall`: required. For `status`: optional — when omitted, reports on all known IDE profiles (the `detected` field indicates which have config paths on disk). |
| `--workspace <path>` | No | status, install, uninstall | Absolute path to workspace root. **Default: current working directory.** Must be an existing directory. Filesystem root paths (`/`, `C:\`, etc.) are rejected (exit code 2). |
| `--release` | No | status, install | Force stable (release) server definitions (overrides auto-detection) |
| `--prerelease` | No | status, install | Force prerelease server definitions (overrides auto-detection) |
| `--version <ver>` | No | status, install | Pin a specific tool version in the server definition (for QA). Mutually exclusive with `--release` and `--prerelease` |
| `--servers <list>` | No | install, uninstall | Comma-separated server names from the server definitions. Unknown names are rejected (exit code 2). Duplicates are silently deduplicated. Default: all servers. |
| `--json` | No | status, install, uninstall | Emit JSON output to stdout. Without this flag, output is human-readable text to stdout |
| `--ide-definitions <path>` | No | status, install, uninstall | Path to an external `ide-profiles.json`, replacing the embedded IDE profiles (see [Definitions Files](#12-definitions-files)) |
| `--server-definitions <path>` | No | status, install, uninstall | Path to an external `server-definitions.json`, replacing the embedded server definitions (see [Definitions Files](#12-definitions-files)) |

### IDE Identifiers

`vscode`, `cursor`, `windsurf`, `kiro`, `trae`, `antigravity`, `rider`, `claude-code`, `opencode`, `aider`, `unknown`

> **v2 (deferred):** `continue`, `zed` — excluded from initial implementation due to non-standard config formats (see [IDE Profiles §5](#5-ide-profiles)).

### Output Convention

By default, output is **human-readable text** to stdout. When `--json` is passed, output is **JSON** to stdout. Diagnostic messages always go to **stderr** (via ILogger). This matches the existing `disco` / `disco --json` pattern.

---

## 2. Status Response

**Command:**
```
uno.devserver mcp status [<ide>] [--workspace <path>] [--release|--prerelease|--version <ver>] [--json]
```

When `<ide>` is provided, it identifies the **caller's IDE** and is included in the output as `callerIde`. The tool scans config paths for **all known IDE profiles** and returns per-IDE status for each server, giving a complete picture of what is already configured and what could be configured.

The **expected variant** (used to determine `registered` vs `outdated`) is resolved as follows:
1. `--version <ver>` → expected variant is `pinned:<ver>`
2. `--prerelease` → expected variant is `prerelease`
3. `--release` → expected variant is `stable`
4. None of the above → **auto-detect** from the running tool's own version (`AssemblyInformationalVersionAttribute`). A version containing `-` (e.g., `5.6.0-dev.42`) means `prerelease`; otherwise `stable`.

The flags `--release`, `--prerelease`, and `--version` are **mutually exclusive**.

### Human-Readable Output (default)

```
MCP Server Status
=================

IDEs detected: vscode, cursor
Caller IDE:    cursor
Expected:      prerelease (auto-detected from tool v5.6.0-dev.42)

UnoApp (stdio)
  vscode       outdated   stable         .vscode/mcp.json
                          prerelease     ~/.vscode/mcp.json
               ⚠ Registered in multiple config files
  cursor       missing
  claude-code  registered prerelease     .mcp.json

UnoDocs (http)
  vscode       registered stable         ~/.vscode/mcp.json
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
  "ides": [
    {
      "id": "vscode",
      "detected": true,
      "configPaths": ["/home/user/myproject/.vscode/mcp.json", "/home/user/.vscode/mcp.json"],
      "writeTarget": "/home/user/myproject/.vscode/mcp.json"
    },
    {
      "id": "cursor",
      "detected": true,
      "configPaths": ["/home/user/myproject/.cursor/mcp.json", "/home/user/.cursor/mcp.json"],
      "writeTarget": "/home/user/myproject/.cursor/mcp.json"
    },
    {
      "id": "rider",
      "detected": false,
      "configPaths": ["/home/user/myproject/.idea/mcpServers.json"],
      "writeTarget": "/home/user/myproject/.idea/mcpServers.json"
    }
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
          "ide": "vscode",
          "status": "outdated",
          "locations": [
            { "path": "/home/user/myproject/.vscode/mcp.json", "variant": "stable" },
            { "path": "/home/user/.vscode/mcp.json", "variant": "prerelease" }
          ],
          "warnings": ["Registered in multiple config files"]
        },
        { "ide": "cursor", "status": "missing" },
        {
          "ide": "claude-code",
          "status": "registered",
          "locations": [
            { "path": "/home/user/myproject/.mcp.json", "variant": "prerelease" }
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
          "ide": "vscode",
          "status": "registered",
          "locations": [
            { "path": "/home/user/.vscode/mcp.json", "variant": "stable" }
          ]
        },
        {
          "ide": "cursor",
          "status": "registered",
          "locations": [
            { "path": "/home/user/.cursor/mcp.json", "variant": "stable" }
          ]
        },
        { "ide": "claude-code", "status": "missing" }
      ]
    }
  ]
}
```

### Top-Level Fields

| Field | Type | Description |
|-------|------|-------------|
| `version` | string | Protocol version (`"1.0"`) |
| `callerIde` | string? | IDE identifier as passed via `<ide>` or `--ide`. `null` when omitted (status only). |
| `toolVersion` | string | Version of the running `uno.devserver` tool |
| `expectedVariant` | string | The variant that `install` would write: `stable`, `prerelease`, or `pinned:<ver>` |
| `ides` | array | All IDE profiles known by the tool, with detection info |
| `servers` | array | All MCP servers the tool manages, with per-IDE status |

### IDE Entry Fields (`ides[]`)

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | IDE identifier |
| `detected` | boolean | `true` if at least one config path exists on disk (directory or file) |
| `configPaths` | string[] | All config paths the tool scans for this IDE |
| `writeTarget` | string | Where `install` would write for this IDE |

When `<ide>` is provided, the caller's IDE is **always included** in the `ides` array, even if not detected. When omitted, all known IDE profiles are listed.

### Server Entry Fields (`servers[]`)

| Field | Type | Description |
|-------|------|-------------|
| `name` | string | Server name (e.g., `"UnoApp"`, `"UnoDocs"`) |
| `transport` | string | `"stdio"` or `"http"` |
| `definition` | object | The canonical server definition that `install` would write (reflects `expectedVariant`) |
| `ides` | array | Per-IDE registration status |

### Per-IDE Status Fields (`servers[].ides[]`)

| Field | Type | Description |
|-------|------|-------------|
| `ide` | string | IDE identifier |
| `status` | string | `"registered"`, `"missing"`, or `"outdated"` |
| `locations` | array? | Config files where the server was found. Each entry has `path` (string) and `variant` (string). Absent when `missing`. |
| `warnings` | string[]? | Diagnostic messages (e.g., duplicate registration across scopes). Absent when empty. |

Only IDEs where at least one config path was found on disk **or** matching the caller's IDE are included in `servers[].ides[]`.

#### Location Entry Fields (`servers[].ides[].locations[]`)

| Field | Type | Description |
|-------|------|-------------|
| `path` | string | Config file path where the server was found |
| `variant` | string | Version variant detected: `stable`, `prerelease`, `pinned:<ver>`, or `legacy-http` |

### Status Values

The `status` is determined by examining the **effective configuration** — the highest-priority entry found across all scanned config paths (first match in scan order = most local scope):

| Status | Meaning |
|--------|---------|
| `registered` | Server found, and the effective (highest-priority) entry's content matches the expected definition |
| `missing` | Server not found in any scanned config path for this IDE |
| `outdated` | Server found in at least one config, but the effective entry's content does not match the expected definition |

> **Rationale**: IDEs resolve config by precedence — a workspace entry shadows a global entry. The status reflects what the IDE will actually use. If the workspace has a `stable` entry and global has `prerelease`, the effective config is `stable` (workspace wins), so the status is `outdated` when `prerelease` is expected. Conversely, if only the global has the server and it matches, the status is `registered` — the global IS the effective config when no workspace entry shadows it.
>
> **Content matching**: For status determination, the tool compares the effective entry's actual content (command, args, url) against the expected definition. Two entries with identical content are considered matching regardless of variant label. This means UnoDocs (where all variants have the same URL) is always `registered` if found.

### Warning Values

| Warning | Trigger |
|---------|---------|
| `Registered in multiple config files` | Server found in 2+ config files for the same IDE (e.g., workspace and global) |
| `Multiple entries match server {name}` | 2+ keys in the same config file match the detection patterns for the same server |

### Variant Values

| Variant | Meaning | Config signature |
|---------|---------|------------------|
| `stable` | Standard release definition | `dnx -y uno.devserver ...` (no version flags) |
| `prerelease` | Prerelease definition | `dnx -y --prerelease uno.devserver ...` |
| `pinned:<ver>` | Specific version pinned | `dnx -y --version <ver> uno.devserver ...` |
| `legacy-http` | Old HTTP-based registration | `localhost:{port}/mcp` URL |

### Extension Usage

The extension filters the response to its own IDE to decide what to register:

```typescript
const myIde = response.callerIde;
if (myIde) {
    for (const server of response.servers) {
        const myStatus = server.ides.find(i => i.ide === myIde);
        if (!myStatus || myStatus.status !== 'registered') {
            // needs registration for this IDE
        }
    }
}
```

The full multi-IDE data (available when `callerIde` is `null`, i.e., no IDE specified) can be used for diagnostics, status display, or telemetry.

---

## 3. Install Response

**Command:**
```
uno.devserver mcp install <ide> [--workspace <path>] [--release|--prerelease|--version <ver>] [--servers UnoApp,UnoDocs] [--json]
```

### Human-Readable Output (default)

```
MCP Install
===========

  UnoApp   created  /home/user/myproject/.cursor/mcp.json   Added UnoApp stdio server
  UnoDocs  skipped  /home/user/.cursor/mcp.json             Already registered
```

### JSON Output (`--json`)

All paths are fully resolved absolute paths.

```json
{
  "version": "1.0",
  "operations": [
    {
      "server": "UnoApp",
      "action": "created",
      "path": "/home/user/myproject/.cursor/mcp.json",
      "reason": "Added UnoApp stdio server"
    },
    {
      "server": "UnoDocs",
      "action": "skipped",
      "path": "/home/user/.cursor/mcp.json",
      "reason": "Already registered"
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

Before writing, `install` scans **all** configPaths for the target IDE and determines the effective status (see [Status Values](#status-values)):
- If `registered` (effective config matches expected) → `skipped`
- If `outdated` (effective config doesn't match) → writes the updated definition to the `writeTarget` and reports `updated`. This shadows any global entry with a correct workspace entry.
- If `missing` (not found anywhere) → writes to the `writeTarget` and reports `created`

### Error in Operations

If an individual operation fails, it is reported in the operations array:

```json
{
  "server": "UnoApp",
  "action": "error",
  "path": "/home/user/myproject/.cursor/mcp.json",
  "reason": "File is read-only"
}
```

---

## 4. Uninstall Response

**Command:**
```
uno.devserver mcp uninstall <ide> [--workspace <path>] [--servers UnoApp,UnoDocs] [--json]
```

### Human-Readable Output (default)

```
MCP Uninstall
=============

  UnoApp   removed    /home/user/myproject/.cursor/mcp.json   Removed from workspace config
  UnoApp   removed    /home/user/.cursor/mcp.json             Removed from user config
  UnoDocs  not_found                                          Not found in any config file
```

### JSON Output (`--json`)

All paths are fully resolved absolute paths.

```json
{
  "version": "1.0",
  "operations": [
    {
      "server": "UnoApp",
      "action": "removed",
      "path": "/home/user/myproject/.cursor/mcp.json",
      "reason": "Removed from workspace config"
    },
    {
      "server": "UnoApp",
      "action": "removed",
      "path": "/home/user/.cursor/mcp.json",
      "reason": "Removed from user config"
    },
    {
      "server": "UnoDocs",
      "action": "not_found",
      "path": null,
      "reason": "Not found in any config file"
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

### Scope Behavior

Uninstall removes the server from **all** scopes (workspace and global) for the target IDE. This means removing a server from a global config affects all workspaces using that IDE.

> **Note for global-only IDEs** (e.g., Anti-Gravity): since the only config is global, uninstall always has workspace-spanning effect. This is inherent to the IDE's config model, not a tool limitation.

---

## 5. IDE Profiles

Each IDE has a profile that defines config file locations, write targets, and JSON format.

> **Scan vs Write**: All config locations listed are scanned during `status` and before `install`/`uninstall`. The write target is where `install` creates or updates entries — always the most local (workspace-specific) path. Scan order indicates priority (first = most specific/local).

### VS Code Family (extension runs inside)

| IDE | Config Locations (scan order) | Write Target | JSON Format |
|-----|-------------------------------|--------------|-------------|
| **VS Code** | `{ws}/.vscode/mcp.json`, `~/.vscode/mcp.json`, global OS path | `{ws}/.vscode/mcp.json` | `servers` |
| **Cursor** | `{ws}/.cursor/mcp.json`, `~/.cursor/mcp.json` | `{ws}/.cursor/mcp.json` | `mcpServers` |
| **Windsurf** | `{ws}/.windsurf/mcp.json`, `~/.codeium/windsurf/mcp_config.json` | `{ws}/.windsurf/mcp.json` | `mcpServers` |
| **Kiro** | `{ws}/.kiro/settings/mcp.json`, `~/.kiro/settings/mcp.json` | `{ws}/.kiro/settings/mcp.json` | `mcpServers` |
| **Trae** | `{ws}/.trae/mcp.json` | `{ws}/.trae/mcp.json` | `mcpServers` |
| **Anti-Gravity** | `~/.gemini/antigravity/mcp_config.json` | same (global only) | `mcpServers` |

VS Code global OS paths:
- Windows: `%APPDATA%/Code/User/mcp.json`
- macOS: `~/Library/Application Support/Code/User/mcp.json`
- Linux: `~/.config/Code/User/mcp.json`

### JetBrains (tool only, no extension)

| IDE | Config Locations | Write Target | JSON Format |
|-----|------------------|--------------|-------------|
| **Rider (Junie)** | `{ws}/.idea/mcpServers.json` | `{ws}/.idea/mcpServers.json` | `mcpServers` |

> **Note**: Rider's global MCP config lives under a version-specific directory (e.g., `~/.config/JetBrains/Rider2025.1/mcpServers.json`). This path varies per Rider version and is excluded from v1 to avoid complex version-enumeration logic. Workspace-level config is sufficient for the primary use case.

### CLI Agents (tool only, no extension)

| Agent | Config Locations | Write Target | JSON Format |
|-------|------------------|--------------|-------------|
| **Claude Code** | `{ws}/.mcp.json`, `~/.claude/mcp.json` | `{ws}/.mcp.json` | `mcpServers` |
| **OpenCode** | `{ws}/.opencode/mcp.json` | `{ws}/.opencode/mcp.json` | `mcpServers` |
| **Aider** | `{ws}/.aider/mcp.json` | `{ws}/.aider/mcp.json` | `mcpServers` |
| **Continue** *(v2)* | `{ws}/.continue/config.json` (MCP section) | same | `mcpServers` (nested) |
| **Zed** *(v2)* | `{ws}/.zed/settings.json` (section) | same | `context_servers` |

> **v2 note:** Continue and Zed are excluded from the initial implementation. Continue uses an array-based `mcpServers` format (and is migrating to YAML), while Zed nests `context_servers` inside a multi-purpose `settings.json`. Both require specialized merge logic beyond the simple "write under root key" approach used for all other IDEs.

### Generic (unknown IDE)

| IDE | Config Locations | Write Target | JSON Format |
|-----|------------------|--------------|-------------|
| `unknown` | `{ws}/.vscode/mcp.json` | `{ws}/.vscode/mcp.json` | `servers` |

### Out of Scope: Visual Studio `.vs/mcp.json`

The Visual Studio extension (`Uno.UI.RemoteControl.VS`) writes its own MCP config to `{ws}/.vs/mcp.json` with a dynamic HTTP URL (`localhost:{port}/mcp`). This path is **not included** in any IDE profile — it is managed entirely by the VS extension and is not affected by `mcp install/uninstall`.

### Notation

- `{ws}` = workspace root (value of `--workspace`, or current working directory)
- `~` = user home directory

---

## 6. Server Definitions

### UnoApp (stdio)

The UnoApp definition varies based on the resolved variant. The `dnx` runner's `--prerelease` and `--version` flags control which version of the `uno.devserver` package is resolved at runtime.

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

**Prerelease** (default when tool is a prerelease version, or when `--prerelease` is passed):
```json
{
  "command": "dnx",
  "args": ["-y", "--prerelease", "uno.devserver", "--mcp-app"]
}
```

**Pinned** (when `--version <ver>` is passed):
```json
{
  "command": "dnx",
  "args": ["-y", "--version", "5.6.0-dev.42", "uno.devserver", "--mcp-app"]
}
```

### Version Resolution

| Running tool version | No flag | `--release` | `--prerelease` | `--version 1.2.3` |
|---------------------|---------|-------------|----------------|---------------------|
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

The tool maps these abstract definitions into the concrete JSON format required by each IDE profile.

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

The tool scans config paths for **all known IDE profiles** and detects existing registrations. This provides a complete picture across all IDEs, not just the caller's.

The scan is **exhaustive**: all configPaths are checked for every IDE, not just the first match. A server may appear in multiple config files for the same IDE (e.g., workspace and global). This is reported as a warning in the status response.

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

If multiple keys in the same config file match the same server's detection patterns, the **first match** (in JSON key order) is used for status/update. A warning `"Multiple entries match server {name}"` is emitted. During `install`, only the first match is updated; during `uninstall`, all matches are removed.

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

After `install --prerelease`, the entry becomes:
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

### JSONC Compatibility

IDE config files may contain JSONC extensions (comments and trailing commas). The tool handles them as follows:

- **Reading**: `JsonCommentHandling.Skip` and `AllowTrailingCommas = true`, matching the existing pattern in `EntryPoint.Mcp.cs`
- **Writing**: Standard JSON only — comments are **not preserved** on round-trip. This is an accepted trade-off (simplicity over preservation), consistent with how the VS extension already handles `.vs/mcp.json`.

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
| `0` | Success — all operations completed, skipped, or partially failed with details in JSON | Install where 1 of 2 servers failed (JSON contains per-operation status) |
| `1` | Failure — no useful work was done, or a fatal error occurred | Unknown `--ide`, invalid `--workspace`, malformed definitions file |
| `2` | Usage error — invalid arguments or parameter combinations | Missing required `<ide>` for install, mutually exclusive flags, unknown `--servers` name |

> **Design note**: Exit code 0 with per-operation error details in JSON (action `"error"`) is preferred over exit code 1 for partial failures. This avoids confusing scripts that check `$? -ne 0` while still providing full diagnostic data in the output. The caller should always parse stdout for details.

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

## 11. Implementation Architecture

### File Structure

All new code under `src/Uno.UI.DevServer.Cli/Mcp/Setup/`:

| File | Responsibility |
|------|----------------|
| `IdeId.cs` | Enum of 13+ IDE identifiers |
| `IdeProfile.cs` | Record: config paths (templates with `{workspace}`/`{home}`/`{appdata}` tokens), write target, JSON root key |
| `DefinitionsLoader.cs` | Loads `ide-profiles.json` and `server-definitions.json` from embedded resources or external files (`--ide-definitions`, `--server-definitions`). See [Definitions Files](#12-definitions-files) |
| `ServerDefinitionResolver.cs` | Builds concrete `JsonObject` definitions from a `ServerDefinition` template. Applies variant resolution (stable/prerelease/pinned) based on auto-detect + CLI overrides |
| `DuplicateDetector.cs` | Pure static methods: `IsUnoAppEntry(keyName, jsonObj)`, `IsUnoDocsEntry(keyName, jsonObj)`, `IsUpToDate(existing, expected)` |
| `ConfigScanner.cs` | Reads config files via `IFileSystem`, uses `DuplicateDetector` to determine per-server status |
| `ConfigWriter.cs` | Pure JSON merge with JSONC reading (`CommentHandling.Skip`, `AllowTrailingCommas`), shallow merge of server entries, and `RemoveServer`. Methods: `MergeServer(content, rootKey, serverName, definition)` and `RemoveServer(content, rootKey, serverName)` |
| `IFileSystem.cs` | I/O abstraction: `FileExists`, `DirectoryExists`, `ReadAllText`, `WriteAllText`, `CreateDirectory`, `IsReadOnly`, `GetUserHomePath`, `GetAppDataPath` |
| `FileSystem.cs` | Production `IFileSystem` wrapping `System.IO` |
| `McpSetupOrchestrator.cs` | `Status()`, `Install()`, `Uninstall()` — orchestrates scanner + writer via `IFileSystem` |
| `McpSetupModels.cs` | Response DTOs: `StatusResponse`, `InstallResponse`, `UninstallResponse` and their child records |

### Modified Files

| File | Change |
|------|--------|
| `CliManager.cs` | `RunMcpSubcommandAsync()` dispatcher: routes `start` to existing proxy, `status/install/uninstall` to orchestrator. `--mcp-app` becomes alias |
| `Program.cs` | DI registration for `IFileSystem` and `McpSetupOrchestrator`. Updated help text with `mcp` command group |

### Testability

Single I/O seam via `IFileSystem`. All business logic is fully testable with an `InMemoryFileSystem` dictionary-backed fake. Tests go in `src/Uno.UI.DevServer.Cli.Tests/Mcp/Setup/`:

| Test File | Coverage |
|-----------|----------|
| `InMemoryFileSystem.cs` | `IFileSystem` fake for tests |
| `Given_DuplicateDetector.cs` | Key/command/URL matching, negatives |
| `Given_ConfigWriter.cs` | Create/update/skip/remove, malformed JSON, preserve entries, format mapping |
| `Given_ConfigScanner.cs` | Registered/outdated/missing status per IDE |
| `Given_McpSetupOrchestrator.cs` | End-to-end status/install/uninstall with `InMemoryFileSystem` |
| `Given_DefinitionsLoader.cs` | Embedded loading, external override, malformed JSON handling, schema validation |

### Design Principles

- **Pure logic extraction**: follows the `MonitorDecisions.cs` pattern — no I/O in business logic
- **No new dependencies**: uses `System.Text.Json.Nodes` (`JsonNode`, `JsonObject`, `JsonArray`) already available in net9.0
- **Manual arg parsing**: consistent with existing `CliManager` patterns (no System.CommandLine migration)
- **JSON options**: reuses `McpJsonUtilities.DefaultOptions` for deserialization; camelCase + indented for output serialization

---

## 12. Definitions Files

IDE profiles and server definitions are stored in two separate embedded JSON files rather than in static C# registries. This makes it possible to add or modify IDE support and server definitions without recompiling the tool.

| File | Content | Override parameter |
|------|---------|--------------------|
| `ide-profiles.json` | IDE config paths, write targets, JSON format | `--ide-definitions <path>` |
| `server-definitions.json` | MCP server invocation, variants, detection patterns | `--server-definitions <path>` |

### Resolution Order

For each file independently:

1. **External override**: If the corresponding `--*-definitions <path>` parameter is passed, load from that file path. If the file does not exist or is malformed JSON, fail with exit code 2.
2. **Embedded resource**: Otherwise, load from the embedded resource compiled into the CLI assembly.

Each external override **completely replaces** its corresponding embedded file — the two are not merged. The two files are independent: overriding one does not affect the other.

### Use Cases for External Override

- **Testing**: Supply a minimal definitions file scoped to the IDE/server under test
- **QA**: Validate a new IDE profile or server definition before shipping it in the embedded file
- **Customization**: Add a non-standard IDE or server not yet supported by the tool

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

### IDE Profiles (`ide-profiles.json`)

The root is a JSON object keyed by IDE identifier (matching the `--ide` parameter values).

```json
{
  "<ide-id>": {
    "configPaths": ["<path-template>", ...],
    "writeTarget": "<path-template>",
    "jsonRootKey": "<root-key>"
  }
}
```

#### Fields

| Field | Type | Description |
|-------|------|-------------|
| `configPaths` | string[] | Ordered list of config file path templates to scan (all are checked). Order indicates priority — first entry is the most local/specific scope. |
| `writeTarget` | string | Path template where `install` writes new entries. Must be one of the `configPaths`. |
| `jsonRootKey` | string | JSON root key used by this IDE: `"servers"`, `"mcpServers"`, or `"context_servers"` |

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

#### Complete `ide-profiles.json`

```json
{
  "vscode": {
    "configPaths": [
      "{workspace}/.vscode/mcp.json",
      "{home}/.vscode/mcp.json",
      "{appdata}/Code/User/mcp.json"
    ],
    "writeTarget": "{workspace}/.vscode/mcp.json",
    "jsonRootKey": "servers"
  },
  "cursor": {
    "configPaths": [
      "{workspace}/.cursor/mcp.json",
      "{home}/.cursor/mcp.json"
    ],
    "writeTarget": "{workspace}/.cursor/mcp.json",
    "jsonRootKey": "mcpServers"
  },
  "windsurf": {
    "configPaths": [
      "{workspace}/.windsurf/mcp.json",
      "{home}/.codeium/windsurf/mcp_config.json"
    ],
    "writeTarget": "{workspace}/.windsurf/mcp.json",
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
  "trae": {
    "configPaths": [
      "{workspace}/.trae/mcp.json"
    ],
    "writeTarget": "{workspace}/.trae/mcp.json",
    "jsonRootKey": "mcpServers"
  },
  "antigravity": {
    "configPaths": [
      "{home}/.gemini/antigravity/mcp_config.json"
    ],
    "writeTarget": "{home}/.gemini/antigravity/mcp_config.json",
    "jsonRootKey": "mcpServers"
  },
  "rider": {
    "configPaths": [
      "{workspace}/.idea/mcpServers.json"
    ],
    "writeTarget": "{workspace}/.idea/mcpServers.json",
    "jsonRootKey": "mcpServers"
  },
  "claude-code": {
    "configPaths": [
      "{workspace}/.mcp.json",
      "{home}/.claude/mcp.json"
    ],
    "writeTarget": "{workspace}/.mcp.json",
    "jsonRootKey": "mcpServers"
  },
  "opencode": {
    "configPaths": [
      "{workspace}/.opencode/mcp.json"
    ],
    "writeTarget": "{workspace}/.opencode/mcp.json",
    "jsonRootKey": "mcpServers"
  },
  "aider": {
    "configPaths": [
      "{workspace}/.aider/mcp.json"
    ],
    "writeTarget": "{workspace}/.aider/mcp.json",
    "jsonRootKey": "mcpServers"
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

The root is a JSON object keyed by server name. Each entry defines the server's transport, canonical invocation variants, and patterns used to detect existing registrations across IDE config files.

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

Each variant contains the JSON properties that will be written into the IDE config file (the "inner" definition, without the server name key).

For `pinned` variants, the placeholder `{version}` is replaced with the value of `--version <ver>` at runtime.

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
public static class DefinitionsLoader
{
    /// <summary>
    /// Loads IDE profiles and server definitions from embedded resources,
    /// with optional external file overrides.
    /// </summary>
    public static Definitions Load(
        IFileSystem fs,
        string? ideDefinitionsPath = null,
        string? serverDefinitionsPath = null);
}

public record Definitions(
    IReadOnlyDictionary<string, IdeProfile> Ides,
    IReadOnlyDictionary<string, ServerDefinition> Servers);

public record IdeProfile(
    string[] ConfigPaths,
    string WriteTarget,
    string JsonRootKey);

public record ServerDefinition(
    string Transport,
    IReadOnlyDictionary<string, JsonObject> Variants,
    DetectionPatterns Detection);

public record DetectionPatterns(
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
