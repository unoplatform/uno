# Appendix G: Backward & Forward Compatibility Matrix

> **Parent**: [Main Spec](spec.md) — Section 4b
> **Related**: [Testing](spec-appendix-c-testing.md)

This document is the **authoritative reference** for compatibility validation. Every phase must be validated against this matrix before being considered complete.

---

## 1. Launcher × Host Compatibility

How each launcher interacts with the Host, and what changes when `--addins` is introduced.

| Launcher | Launches Host Via | Uses `--command start` | Passes `--addins` | AmbientRegistry Check | `.csproj.user` Write | Status |
|----------|-------------------|:----------------------:|:------------------:|:---------------------:|:--------------------:|--------|
| CLI `start` | Controller → Server | Yes | **Phase 0** | Yes (controller) | Yes (controller) | Changing |
| CLI MCP (`--mcp-app`) | Direct (Phase 1b) | No | **Phase 0** | **Must add** (1g-bis) | Yes (Host handles) | Changing |
| VS (`uno.studio`) | `DevServerLauncher` → Direct | No | No (future) | **None today** | Yes (Host handles) | Unchanged |
| Rider (`uno.rider`) | `DevServerService` → Direct | No | No (future) | **None today** | Yes (Host handles) | Unchanged |
| VS Code (`uno.vscode`) | `extension.ts` → Direct | No | No (future) | **None today** | Yes (Host handles) | Unchanged |

### Risks

| Risk | Scenario | Impact | Mitigation |
|------|----------|--------|------------|
| IDE + MCP simultaneous | User has VS open, starts MCP agent | Two DevServers, port conflict, wrong Hot Reload target | CLI checks AmbientRegistry, connects to existing instance (1g-bis) |
| IDE + CLI `start` | User has Rider open, runs `uno.devserver start` | Controller blocks (duplicate detected) | Current behavior, correct |
| MCP + IDE sequential | MCP starts DevServer, user then opens IDE | IDE launches its own Host, ignores existing | IDE has no awareness of CLI-launched instances. Long-term fix: IDE delegates to CLI (section 14) |
| Old IDE + new Host | User has old VS extension, updated Uno SDK with new Host | Host must accept launch without `--addins` | `--addins` is additive; absence = MSBuild discovery |

### Validation

- [ ] Host launched **without** `--addins` → MSBuild discovery runs as before
- [ ] Host launched **with** `--addins` → MSBuild discovery skipped, provided paths used
- [ ] Host launched **with** `--addins ""` (empty) → no add-ins loaded, no discovery
- [ ] Host launched with both `--addins` and `--solution` → `--addins` for add-ins, `--solution` for Hot Reload

---

## 2. Uno SDK Version Compatibility

How different SDK versions affect add-in discovery.

| SDK Version | `packages.json` Location | Known Add-in Packages | `.targets` Convention | Fast Path Works | Fallback |
|-------------|--------------------------|----------------------|:---------------------:|:---------------:|----------|
| 6.5+ (current) | `targets/netstandard2.0/packages.json` | `uno.ui.app.mcp`, `uno.settings.devserver` | Yes | **Yes** | N/A |
| 6.0-6.4 | Same location | `uno.ui.app.mcp` (varies), `uno.settings.devserver` | Yes | **Likely** | MSBuild |
| 5.x | May differ or not exist | May not have MCP add-in | Unknown | **Unlikely** | MSBuild |
| Future (7+) | Same or with `devserver-addin.json` | Same + new add-ins | Yes + manifest | **Yes** | N/A |

### Risks

| Risk | Scenario | Impact | Mitigation |
|------|----------|--------|------------|
| `packages.json` not found | SDK 5.x, different file layout | Fast path finds nothing | Fall back to MSBuild, log warning |
| `packages.json` format changed | Future SDK restructures | Fast path parser fails | Version-aware parsing, MSBuild fallback |
| Add-in package renamed | Future SDK changes package name | Not found in NuGet cache | Convention-based scan finds any package with `UnoRemoteControlAddIns` |
| Partial resolution | 1 of 2 add-ins resolved | Degraded state (missing tools) | Diagnostic warning in health, NOT silent |

### Assumptions

1. `packages.json` continues to exist at `targets/netstandard2.0/packages.json` in 6.x+ SDKs
2. Add-in packages continue to use `buildTransitive/*.targets` with `UnoRemoteControlAddIns`
3. NuGet cache structure (`{cache}/{pkgid}/{version}/`) is stable across .NET SDK versions

### Validation

- [ ] SDK 6.5+ → both add-ins resolved via `.targets`, no MSBuild
- [ ] SDK 6.0 → test with available packages, verify resolution or graceful fallback
- [ ] SDK 5.x → fast path finds nothing, MSBuild fallback triggered, add-ins loaded
- [ ] Future SDK with `devserver-addin.json` → manifest takes priority over `.targets`
- [ ] Unknown new add-in package → discovered automatically if follows convention

---

## 3. Operating System Compatibility

| Concern | Windows | macOS | Linux |
|---------|---------|-------|-------|
| Path separator | `\` (backslash) | `/` (forward) | `/` (forward) |
| NuGet cache default | `%USERPROFILE%\.nuget\packages\` | `~/.nuget/packages/` | `~/.nuget/packages/` |
| Filesystem case | Case-insensitive (NTFS) | Case-insensitive (default APFS) | **Case-sensitive** |
| Long paths | May need registry key | No issue | No issue |
| `$NUGET_PACKAGES` env var | `%NUGET_PACKAGES%` | `$NUGET_PACKAGES` | `$NUGET_PACKAGES` |
| AmbientRegistry location | `%LOCALAPPDATA%\Uno Platform\DevServers\` | `~/.local/share/Uno Platform/DevServers/` | Same as macOS |

### Risks

| Risk | Scenario | Impact | Mitigation |
|------|----------|--------|------------|
| Case mismatch in NuGet cache | Package `Uno.UI.App.Mcp` vs directory `uno.ui.app.mcp` | Not found on Linux | Case-insensitive directory lookup |
| Backslash in resolved paths | `.targets` file contains `\` on Windows | Path broken on macOS/Linux | Normalize all paths via `Path.Combine()` |
| Long path on Windows | Deep NuGet cache + long package name | `PathTooLongException` | Use `\\?\` prefix or ensure long path support |
| Custom NuGet cache | `$NUGET_PACKAGES` set to non-default location | Packages not found | Check env var in resolution chain |

### Validation

- [ ] Windows — `.targets` parsed, paths with `\` normalized correctly
- [ ] macOS — case-insensitive lookup works
- [ ] Linux — case-insensitive lookup works on ext4 (case-sensitive)
- [ ] Custom `$NUGET_PACKAGES` → respected on all platforms
- [ ] AmbientRegistry file written/read correctly on all platforms

---

## 4. .NET SDK Version Compatibility

| .NET SDK | TFM | Host Binary Location | `dotnet tool` R2R Support | Notes |
|----------|-----|---------------------|:-------------------------:|-------|
| 9.0 | `net9.0` | `tools/rc/host/net9.0/` | No | Current |
| 10.0 | `net10.0` | `tools/rc/host/net10.0/` | **Yes** (RID-specific tools) | Target for Phase 2 |
| Preview | `net{x}.0` | May not match installed SDKs | Varies | Edge case |
| Multiple installed | Depends on `global.json` | Must match pinned SDK | N/A | See section 13 |

### Risks

| Risk | Scenario | Impact | Mitigation |
|------|----------|--------|------------|
| TFM mismatch | `dotnet --version` reports 10.0 but Host only has `net9.0` binary | Host not found | Scan available TFMs, pick best match (section 13 Phase 3) |
| `global.json` pins older SDK | Project uses 9.0 but system default is 10.0 | Wrong TFM selected | Respect `global.json` SDK version, not `dotnet --version` |
| Preview SDK version string | `10.0.100-preview.1` | Parser fails | Robust version parsing, ignore preview suffix for TFM |

### Validation

- [ ] .NET 9.0 installed → `net9.0` TFM, Host found
- [ ] .NET 10.0 installed → `net10.0` TFM, Host found
- [ ] `global.json` pins 9.0 with 10.0 installed → `net9.0` TFM used
- [ ] Host package only has `net9.0` but SDK is 10.0 → fallback to `net9.0` with warning
- [ ] Cache invalidated when `global.json` changes

---

## 5. MCP Client Compatibility

> **Source**: [Apify MCP Client Capabilities Registry](https://github.com/apify/mcp-client-capabilities), client documentation, and empirical testing. Versions as of February 2026. Capabilities may change with updates.

| Client | Ref. Version | `roots` | `tools/list_changed` | `resources` | `resources/subscribe` | Transport | Workaround Needed |
|--------|:------------:|:-:|:-:|:-:|:-:|:-:|---|
| Antigravity | ~1.15 | Yes | No | No | No | stdio | `--mcp-wait-tools-list` |
| Claude Code | ~2.1 | Yes | No | No | No | stdio | `--mcp-wait-tools-list` |
| Claude Desktop | ~1.1 | No | No | No | No | stdio | `--force-roots-fallback` + `--mcp-wait-tools-list` |
| Cursor | ~2.4 | Yes | Yes | Yes | No | stdio | None |
| Codex CLI | ~0.98 | No | No | No | No | stdio, HTTP | `--force-roots-fallback` + `--mcp-wait-tools-list` + `uno_health` tool |
| JetBrains Junie | 2025.2+ | Yes | No | No | No | stdio only | `--mcp-wait-tools-list` |
| VS Code Copilot | ~1.109 | Yes | Yes | Yes | No | stdio | None |
| Windsurf | ~1.95 | No | Yes | Yes | No | stdio, HTTP | `--force-roots-fallback` |

**Key observations**:
- **`resources/subscribe` is unsupported across ALL clients** — the `uno://health` resource subscription feature (section 1d) will not work for any current client. Polling or `tools/list_changed` are the only notification mechanisms.
- **`tools/list_changed` is supported by only 3 clients** (Cursor, VS Code Copilot, Windsurf). All others need `--mcp-wait-tools-list` to block the initial `list_tools` until upstream responds.
- **`roots` is supported by 5 of 8 clients**. The 3 without (Claude Desktop, Codex CLI, Windsurf) need `--force-roots-fallback`.
- **Capability detection from `initialize` is the only reliable approach** — hardcoding client names is fragile and already stale.

### Risks

| Risk | Scenario | Impact | Mitigation |
|------|----------|--------|------------|
| Client doesn't support `tools/list_changed` | Tool list updates after initial response | Client never sees updated tools | `--mcp-wait-tools-list` blocks until upstream responds |
| Client doesn't support `resources` | `uno://health` not accessible | No diagnostics | `uno_health` tool as universal fallback |
| Client doesn't support `roots` | Workspace not discovered | Discovery fails | `--force-roots-fallback` uses `--solution-dir` |
| No client supports `resources/subscribe` | Health push notifications never received | Clients must poll or rely on `tools/list_changed` | Design for pull-based health (tool call), not push-based (subscription) |
| Hardcoded client list becomes stale | New client not in `ClientsWithoutListUpdateSupport` | Wrong behavior for new client | Replace with capability detection from `initialize` request |
| Junie stdio-only | Server uses HTTP transport | Junie can't connect | Ensure stdio transport always available |

### Validation

- [ ] Client with `roots` support → workspace discovered automatically
- [ ] Client without `roots` + `--force-roots-fallback` → workspace from `--solution-dir`
- [ ] Client with `tools/list_changed` → sees updated tool list after host connects
- [ ] Client without `tools/list_changed` + `--mcp-wait-tools-list` → initial response waits for upstream
- [ ] Client without `resources` → `uno_health` tool returns same data as `uno://health`

---

## 6. Multi-Instance Scenarios (Critical)

These scenarios test the interaction between different DevServer launchers running simultaneously.

| # | Scenario | Current Behavior | Expected Behavior (Post-Spec) |
|---|----------|-----------------|-------------------------------|
| 1 | IDE running, MCP starts | MCP launches new Host → **two instances, conflict** | MCP detects existing instance via AmbientRegistry, connects to it |
| 2 | MCP running, IDE starts | IDE launches new Host → **two instances** | No change (IDE not modified). Long-term: IDE delegates to CLI |
| 3 | CLI `start` running, MCP starts | MCP detects via AmbientRegistry → connects or warns | MCP connects to existing instance |
| 4 | MCP running, CLI `start` | Controller detects via AmbientRegistry → blocks | Existing behavior, correct |
| 5 | Two IDEs on same solution | Two instances, no protection | No change (out of scope, IDE responsibility) |
| 6 | MCP crashes, IDE still running | IDE's Host unaffected | Correct — MCP was only a client |
| 7 | IDE closes, MCP still running | MCP loses upstream if connected to IDE's Host | MCP detects disconnect, relaunches own Host (hot reconnection) |

### Validation

- [ ] Scenario 1: MCP connects to IDE-launched DevServer instead of launching new one
- [ ] Scenario 3: MCP connects to CLI-launched DevServer
- [ ] Scenario 7: MCP recovers when IDE-launched Host exits

---

## 7. License Tier Compatibility

| Tier | Tool Count | Tool Cache Content | Upgrade Mid-Session |
|------|:----------:|-------------------|:-------------------:|
| Community | ~9 | 9 tools cached | `tools/list_changed` sent |
| Pro | ~11 | 11 tools cached | `tools/list_changed` sent |
| Business | ~12 | 12 tools cached | `tools/list_changed` sent |
| Expired/None | 0 | Last-known cached | Warning in health |

### Validation

- [ ] Community license → `list_tools` returns ~9 tools, cache reflects this
- [ ] Pro upgrade mid-session → `tools/list_changed` notification, updated tool list
- [ ] No license / expired → `list_tools` returns within 30s (not infinite hang), health warning
- [ ] Cache from Pro session used by Community session → stale tools served, refreshed when upstream responds
