# Spec 001: Fast DevServer Startup

> **Status**: Draft v1
> **Author**: Carl de Billy
> **Date**  : 2026-02-13

### Reading Convention

This spec distinguishes between **current behavior** (what the code does today) and **target behavior** (what each phase will implement). Sections describing current behavior reference source code locations; sections describing target behavior are prefixed with the phase that delivers them (e.g., "Phase 0:", "Phase 1a:"). **The code is the source of truth for current behavior.** Where this spec describes current behavior, it must match the code; discrepancies are spec bugs.

---

## Executive Summary

### The Problem

Every DevServer startup — whether from an IDE, the CLI, or an AI agent via MCP — pays a **10-30 second tax** for MSBuild-based add-in discovery. This cost is entirely unnecessary: the add-in DLL paths are deterministic and can be computed in under 200ms from data already available on disk (NuGet package `.targets` files).

In MCP mode, this is compounded by a 3-process chain and lack of caching, resulting in **15-40+ seconds** before AI tools become available. Some agents time out and skip the DevServer entirely.

### What We're Changing

**1. Convention-based add-in discovery** (benefits all modes)
Instead of running `dotnet build` to evaluate MSBuild targets, we read the `.targets` XML files directly from the NuGet cache and extract add-in DLL paths. Same data source, same result, ~200ms instead of 10-30s. A new `--addins` flag on the Host lets any launcher pass pre-resolved paths, skipping MSBuild discovery entirely.

**2. Instant MCP startup** (MCP mode only)
The MCP STDIO server starts immediately (< 1s) and returns cached tool definitions while discovery and Host launch happen in the background. Tool calls that arrive before the Host is ready receive structured errors with remediation hints. Hot reconnection handles Host crashes without restarting the MCP client.

**3. DevServer instance management**
Today, IDE extensions and CLI launch DevServer instances independently with no awareness of each other, leading to port conflicts and duplicate instances. The CLI will use AmbientRegistry to detect existing instances and connect to them rather than spawning duplicates.

### Why This Approach

- **Zero changes to existing add-in packages**: The `.targets` convention is already shipping in NuGet packages. We read what's already there.
- **Zero changes to IDE extensions**: The `--addins` flag is additive. Absence = MSBuild discovery as before. Current IDE extension versions continue working unchanged.
- **Incremental and reversible**: Each phase is independently valuable. MSBuild fallback remains as safety net for older SDKs or unexpected package layouts.
- **Toward CLI centralization**: This work positions the CLI as the single authority for DevServer lifecycle, reducing duplicated logic across IDE extensions. IDE teams can adopt fast discovery on their own timeline.

### Expected Gains

| Metric | Current | After Phase 0+1 | After Phase 2 |
|--------|---------|-----------------|---------------|
| Add-in discovery | 10-30s (MSBuild) | < 200ms (`.targets` parsing) | < 200ms |
| MCP time to `list_tools` | 15-40s | < 1s (cached) | < 500ms |
| MCP time to functional tools | 15-40s | < 5s | < 3s |
| CLI cold start | ~1.5s | ~1.5s | ~200ms (R2R) |
| Host crash recovery | Manual restart | Automatic (hot reconnection) | Automatic |

### Scope and Phases

- **Phase 0**: `.targets` parsing + `--addins` flag (CLI modes, all platforms)
- **Phase 1**: Instant MCP start, background Host launch, health endpoint, hot reconnection
- **Phase 2**: ReadyToRun compilation for CLI cold start
- **Phase 3**: Graceful degradation, older SDK support, `devserver-addin.json` manifest
- **Long-term**: IDE extensions delegate to CLI for DevServer lifecycle

### Key Constraints

- Backward compatibility with Uno SDK 5.x, 6.x, and current
- Current IDE extension versions must work unchanged — no coordinated update required
- All existing CLI flags remain functional
- Cross-platform (Windows, macOS, Linux) with correct path and filesystem handling

---

### Document Map

| Document | Content |
|----------|---------|
| **spec.md** (this) | Problem, requirements, solution design, phases, risks, open questions |
| [Appendix A: Startup Workflow](spec-appendix-a-startup-workflow.md) | Current workflow analysis with mermaid diagrams and timing |
| [Appendix B: Add-In Discovery](spec-appendix-b-addin-discovery.md) | Add-in discovery system: algorithms, conventions, author guide, package layout |
| [Appendix C: Testing](spec-appendix-c-testing.md) | Verification plan, test strategy, compatibility matrix |
| [Appendix D: MCP Improvements](spec-appendix-d-mcp-improvements.md) | MCP protocol optimization and DI refactoring recommendations |
| [Appendix E: Reference](spec-appendix-e-reference.md) | MCP tools list, IDE extension analysis, convergence analysis |
| [Appendix F: Discovery Roadmap](spec-appendix-f-discovery-roadmap.md) | Broader discovery roadmap (absorbed from DevServerDiscovery.md) |
| [Appendix G: Compatibility Matrix](spec-appendix-g-compatibility-matrix.md) | Exhaustive backward/forward compatibility validation matrix |
| [Appendix H: Manual QA](spec-appendix-h-manual-qa.md) | Scenarios requiring human testing (IDE compat, multi-instance, MCP clients, perf) |

---

## 1. Problem Statement

The DevServer startup is slow across **all modes** — MCP, IDE extensions (VS, Rider, VS Code), and CLI `start`. The primary bottleneck is the same everywhere: **MSBuild-based add-in discovery** takes 10-30 seconds to resolve add-in DLL paths that are fully deterministic from NuGet package metadata.

In MCP mode specifically (`dnx uno.devserver --mcp-app`), this is compounded by a 3-process chain, resulting in **15-40+ seconds** before tools become available. Some AI agents (Claude Code, Codex) skip it entirely because of this delay.

### Impact (all modes)

- **Every DevServer startup** pays 10-30s for MSBuild evaluation to discover add-in DLLs.
- A broken solution state (missing packages, build errors) prevents add-in discovery entirely.
- IDE extensions (VS, Rider, VS Code) all pay this same cost on every project load.

### Additional Impact (MCP mode)

- AI agents that set a connection timeout (often 10-15s) **never** get tools.
- Users experience a 20-40s "dead zone" where tool calls silently fail.
- Error messages when things go wrong are opaque to both humans and AI models.
- A host process crash requires a full MCP client restart.

---

## 2. Bottleneck Analysis

| # | Bottleneck | Time | Root Cause |
|---|-----------|------|-----------|
| **B1** | `AddIns.Discover()` runs 2x `dotnet build` | **10-30s** | MSBuild evaluates entire solution graph to resolve `UnoRemoteControlAddIns` items |
| **B2** | 3x .NET cold start (CLI + controller + server) | **4.5s** | Controller process is unnecessary in MCP mode |
| **B3** | CLI cold start (no R2R/AOT) | **1.5s** | `dotnet tool` packaging doesn't use ReadyToRun |
| **B4** | `dotnet --version` subprocess | **0.5-1s** | Called on every startup, result is stable |
| **B5** | Health check polling | **1-30s** | HTTP polling at 1s intervals until server ready |
| **B6** | `.csproj.user` generation | **0.2s** | Controller overhead; in Phase 1b (controller bypass), must be re-implemented CLI-side — see section 1h |

### Critical Insight: Add-in Paths Are Deterministic

The 10-30s MSBuild evaluation exists to resolve `UnoRemoteControlAddIns` items. These are populated by `.targets` files in NuGet packages following a consistent convention:

```xml
<!-- From buildTransitive/Uno.UI.App.Mcp.targets -->
<UnoRemoteControlAddIns
  Include="$(MSBuildThisFileDirectory)../tools/devserver/Uno.UI.App.Mcp.Server.dll" />

<!-- From buildTransitive/Uno.Settings.DevServer.targets -->
<UnoRemoteControlAddIns
  Include="$(MSBuildThisFileDirectory)../tools/devserver/Uno.Settings.DevServer.dll" />
```

**Every known add-in** uses the same pattern: `$(MSBuildThisFileDirectory)../tools/devserver/{Name}.dll`. The `$(MSBuildThisFileDirectory)` variable is always `{NuGetCache}/{packageId}/{version}/buildTransitive/`, so the resolved path is:

```
{NuGetCache}/{packageId}/{version}/tools/devserver/{AssemblyName}.dll
```

**We can read and parse these `.targets` XML files directly** from the NuGet cache, without running MSBuild. This is the same data source MSBuild uses, just without the 10-30s evaluation overhead.

### Known Add-in Packages

| Package | `.targets` file | Add-in DLL | Source Repo |
|---------|----------------|------------|-------------|
| `uno.ui.app.mcp` | `buildTransitive/Uno.UI.App.Mcp.targets` | `tools/devserver/Uno.UI.App.Mcp.Server.dll` | `uno.app-mcp` |
| `uno.settings.devserver` | `buildTransitive/Uno.Settings.DevServer.targets` | `tools/devserver/Uno.Settings.DevServer.dll` | `uno.licensing` |

Both use the same registration pattern:
- `.targets` file in `buildTransitive/` (NuGet convention for transitive imports)
- `<UnoRemoteControlAddIns Include="..."/>` item pointing to a DLL under `tools/devserver/`
- DLL loaded by the Host via `Assembly.LoadFrom()` + `AddFromAttributes()` for DI registration

### Add-in Startup Is Lightweight

`Uno.UI.App.Mcp.Server` (from `uno.app-mcp` repo) does no MSBuild, no file scanning, no process spawning. It registers 12 tool methods via reflection and sets up async license monitoring. `Uno.Settings.DevServer` (from `uno.licensing` repo) similarly registers via `[ServiceCollectionExtension]` attribute. Both initialize in **< 100ms**.

---

## 3. Requirements

### 3.1 Functional Requirements

| ID | Requirement | Priority |
|----|------------|----------|
| **FR1** | MCP STDIO server starts and responds to `list_tools` within 1 second of process launch (with tool cache) | Must |
| **FR2** | Licensed MCP tools functional within 5 seconds of launch (warm cache). Tool count depends on license tier (Community 9, Pro 11, Business 12). | Must |
| **FR3** | If host is not ready, tool calls return structured errors with remediation hints | Must |
| **FR4** | `uno://health` resource exposes structured diagnostics | Must |
| **FR5** | Host process crash triggers automatic reconnection without MCP client restart | Must |
| **FR6** | Works with Uno SDK 5.x, 6.5, and current versions | Must |
| **FR7** | IDE integrations (VS, Rider, VS Code) remain functional | Must |
| **FR8** | Non-MCP commands (`start`, `stop`, `list`, `login`) are unaffected. `disco` is enhanced (new fields, new `--addins-only` flag) but existing output is backward-compatible. | Must |
| **FR9** | All existing CLI flags remain functional | Must |
| **FR10** | License validation does not block CLI-side MCP startup. Upstream `list_tools` may still await license resolution within the Host (see section 5.3). | Should |
| **FR11** | `list_tools` MUST return within a bounded time (max 30s) even when upstream returns 0 tools or is unreachable. Must never block indefinitely. | Must |

### 3.2 Non-Functional Requirements

| ID | Requirement |
|----|------------|
| **NFR1** | Cold start to first `list_tools` response: < 1s (with R2R + cache) |
| **NFR2** | Cold start to functional tool calls: < 5s (warm NuGet cache) |
| **NFR3** | No regression in non-MCP startup paths |
| **NFR4** | Structured error responses parseable by AI models |

---

## 4. Constraints

1. **Backward compatibility is critical**: Must work with older Uno SDK versions (5.x, 6.5). Package structures and add-in layouts may differ.
2. **IDE integrations use the same Host process**: VS, Rider, VS Code extensions launch `RemoteControl.Host` directly. Changes to Host must not break these paths.
3. **All existing CLI flags must remain functional**: `--force-roots-fallback`, `--force-generate-tool-cache`, `--solution-dir`, `--mcp-wait-tools-list`, `--port`, `--log-level`, `--file-log`.
4. **MCP protocol constraints**: Most MCP clients do not support `tools/list_changed` (only 3 of 8 do). Several don't support `roots` (Claude Desktop, Codex CLI, Windsurf). No client supports `resources/subscribe`. The `--force-roots-fallback` + `--mcp-wait-tools-list` workarounds must continue working. See [Appendix G](spec-appendix-g-compatibility-matrix.md) section 5 for the full matrix.
5. **License validation must not block startup**: The `MCPToolsObserverService` in `Uno.UI.App.Mcp.Server` already handles license-based tool filtering asynchronously. We must not introduce new blocking license checks.
6. **Code conventions**: New code should follow the project `.editorconfig` and **match the style of surrounding code** in each file. Prefer `internal` for new types unless they are part of a public API contract. Follow existing patterns in the target project (`*Extensions.cs` for extension methods, DI via `IServiceCollection`). Note: the existing codebase uses both block-scoped and file-scoped namespaces; `.editorconfig` expresses a preference, not a hard rule. Do not reformat existing files to match a different style.

---

## 4b. Backward Compatibility — Cross-Cutting Concern

> **This section is normative.** Every component, every phase, and every change in this spec MUST be evaluated against the compatibility matrix below. Backward compatibility is not a feature — it is a constraint on every decision.

### Dimensions

| Dimension | Variants | Impact |
|-----------|----------|--------|
| **Uno SDK version** | 5.x, 6.0-6.4, 6.5+ | Package layout, `packages.json` location, add-in availability |
| **IDE extension** | VS (`uno.studio`), Rider (`uno.rider`), VS Code (`uno.vscode`), none (CLI-only) | Host launch method, `--addins` adoption, controller dependency |
| **Operating system** | Windows, macOS, Linux | Path separators, NuGet cache location, filesystem case sensitivity |
| **NuGet cache** | Default (`~/.nuget/packages/`), custom (`$NUGET_PACKAGES`), multiple fallback locations | Package path resolution |
| **.NET SDK version** | 9.0, 10.0, preview | TFM resolution, Host binary availability, `dotnet tool` behavior |
| **MCP client** | Claude Code, Claude Desktop, Cursor, Codex CLI, VS Code Copilot, Windsurf | `roots` support, `tools/list_changed` support, resource support |
| **License tier** | Community, Pro, Business | Tool count, tool cache content |

### Rules (apply to ALL phases)

1. **No breaking changes to Host launch**: IDE extensions that launch `RemoteControl.Host` without `--addins` MUST continue to work exactly as before. The `--addins` flag is **additive** — its absence triggers the existing MSBuild discovery path.

2. **Current IDE extension versions MUST work unchanged**: Users may not update their IDE extensions immediately. The **currently shipped versions** of `uno.studio` (VS), `uno.rider`, and `uno.vscode` MUST continue to function correctly with the modified Host and CLI. No change may assume a coordinated IDE extension update. Future IDE versions may adopt `--addins`, but that is their choice and timeline.

3. **No breaking changes to CLI commands**: `start`, `stop`, `list`, `disco`, `login` commands MUST be unaffected. Only `--mcp-app` mode gains new behavior.

4. **SDK version graceful degradation**: If the fast path (`.targets` parsing) fails for a given SDK version, the system MUST fall back to MSBuild discovery transparently. The user sees a performance degradation, not an error.

5. **OS path handling**: All path construction MUST use `Path.Combine()` or equivalent. No hardcoded `/` or `\`. NuGet cache resolution MUST follow the existing code order (`UnoToolsLocator.cs:346`): user profile default, then common data, then `$NUGET_PACKAGES` override. Do not change the resolution order — match the current behavior.

6. **Filesystem case sensitivity**: Package name lookup in NuGet cache MUST be case-insensitive on all platforms (NuGet stores packages in lowercase, but directory listing may vary on case-sensitive filesystems).

7. **MCP client capability detection**: Never assume a client supports `roots`, `tools/list_changed`, or `resources`. Detect capabilities from the client's `initialize` request. The `ClientsWithoutListUpdateSupport` hardcoded list (`McpProxy.cs:41`) MUST be replaced with capability-based detection. **These are three independent capabilities — do not conflate them**:

   **7a. `roots` support** (can the client provide workspace roots?):
   - Detection: `ClientCapabilities.Roots != null`
   - **Current bug**: `McpProxy.cs:525` checks `Roots?.ListChanged ?? false`, which tests whether the client supports *roots list-changed notifications*, not whether it supports roots at all. A client can support `roots` (provides workspace roots on request) without supporting `Roots.ListChanged` (notifies server when roots change). The current check is too strict — it rejects clients like JetBrains Junie that support roots but not list-changed.
   - Fix: `ClientCapabilities.Roots != null` (not `Roots.ListChanged`)

   **7b. `tools/list_changed` support** (will the client re-fetch tools on notification?):
   - Detection: **Not exposed as a capability in the MCP protocol today.** The `ClientsWithoutListUpdateSupport` hardcoded list is a workaround. The reliable approach is the `--mcp-wait-tools-list` flag (block initial `list_tools` until upstream responds). Long-term, this should be replaced with `ClientCapabilities.Tools?.ListChanged == true` if the protocol adds this field, or fall back to sending the notification and letting the client ignore it.
   - **This is NOT the same as `Roots.ListChanged`** — `Roots.ListChanged` is about roots updates, not tools updates.

   **7c. `resources` support** (can the client access `uno://health`?):
   - Detection: No capability bit in the protocol. Infer from client behavior (calls `resources/read` or `resources/list`). Only 3 of 8 tested clients support this (see [Appendix G](spec-appendix-g-compatibility-matrix.md) section 5).
   - Fallback: `uno_health` tool (universally available)

8. **Add-in forward compatibility**: New add-in packages that follow the convention (`.targets` + `UnoRemoteControlAddIns`) MUST be discovered automatically without code changes. No hardcoded package name lists.

9. **Add-in backward compatibility**: Add-ins that predate the `buildTransitive/` convention MUST still work through the MSBuild fallback. The fallback is transparent and logged.

10. **Test coverage per dimension**: Each phase's test plan MUST include compatibility tests for at least: 2 SDK versions, 2 operating systems, and the "no `--addins` flag" case (IDE regression). See [testing.md](spec-appendix-c-testing.md) for the full compatibility matrix.

### Compatibility Validation Checklist (per phase)

Before any phase is considered complete:

- [ ] **Current** IDE extension versions (as shipped today) launch Host — behavior identical
- [ ] Future IDE extensions launch Host with `--addins` — add-ins loaded, no MSBuild
- [ ] CLI `start` command — works with and without `--addins`
- [ ] CLI `disco` command — outputs correct paths for current SDK
- [ ] Older SDK (5.x) — falls back to MSBuild, no crash, logged warning
- [ ] Current SDK (6.5+) — fast path resolves all add-ins
- [ ] Windows — backslash paths handled correctly
- [ ] macOS/Linux — forward slash paths, case-insensitive NuGet lookup
- [ ] Custom `$NUGET_PACKAGES` — respected in all path resolution
- [ ] MCP client without `tools/list_changed` — `--mcp-wait-tools-list` still works
- [ ] MCP client without `roots` — `--force-roots-fallback` still works

---

## 5. Solution Design

The solution is structured in two layers:
1. **General optimizations** (benefit ALL startup modes: MCP, IDE, CLI)
2. **MCP-specific optimizations** (benefit only MCP mode)

### Architecture: Before and After

**Before (current, all modes)**:
```
Any launcher (CLI/VS/Rider/VSCode)
  --> Host process
  --> AddIns.Discover() = 2x dotnet build (10-30s)
  --> Assembly loading, Kestrel start
```

**After (general optimization)**:
```
Any launcher
  --> Host process with --addins flag (pre-resolved paths)
  --> Assembly loading, Kestrel start (skip MSBuild entirely)
```

**After (MCP mode, additionally)**:
```
MCP Client --STDIO--> [CLI: MCP server starts immediately, returns cached tools]
                            |
                            +--background--> [SDK discovery, .targets parsing, host launch]
                            |
MCP Client <--STDIO-- [CLI] <--HTTP-- [Server /mcp ready after ~3-5s]
```

---

### General Optimization: Convention-Based Add-In Discovery

**This optimization benefits ALL startup modes** — MCP, IDE extensions, and CLI `start`.

#### The Problem

`AddIns.Discover()` runs two full `dotnet build` invocations to collect `UnoRemoteControlAddIns` items from `.targets` files. This takes 10-30s and requires a working build environment.

#### The Solution: Parse `.targets` Files Directly

Instead of evaluating `.targets` files through MSBuild, **read them as XML** from the NuGet cache. The `.targets` files are the same data source MSBuild uses, but we skip the entire build graph evaluation.

**Discovery algorithm** (two package sources, merged):

```
ConventionBasedAddInDiscovery(packagesJsonPath, projectAssetsPath, nugetCachePaths[]):
  1. Build merged package list from two sources:
     a. Parse packages.json -> list of (packageName, version) from Uno SDK
     b. Parse project.assets.json (obj/project.assets.json) -> list of ALL
        (packageName, version) from NuGet restore output
     c. Merge: union of both lists (deduplicate by packageName, prefer
        project.assets.json version if conflict)
  2. For each package in merged list:
     a. Find package directory in NuGet cache
     b. Scan buildTransitive/*.targets files in the package
     c. Parse each .targets XML, extract UnoRemoteControlAddIns items
     d. Resolve $(MSBuildThisFileDirectory) -> actual buildTransitive/ directory
     e. Normalize path (resolve ../), verify DLL exists on disk
     f. If DLL found, add to resolved add-in list
  3. Return resolved paths + any warnings
```

**Why two sources**: `packages.json` (Uno SDK) only lists packages bundled with the SDK. Third-party add-in packages referenced directly by the project (not via the SDK) would be missed. `project.assets.json` is generated by `dotnet restore` and lists ALL packages including transitive dependencies. It is the only complete package inventory.

**`project.assets.json` location**: `{projectDir}/obj/project.assets.json`. If the file is missing (restore not run), fall through to `packages.json`-only mode with a diagnostic warning.

**Project selection in multi-project solutions**: A typical Uno solution contains multiple projects (Mobile, Wasm, Skia, Tests). Each has its own `project.assets.json`. For add-in discovery, the specific project does not matter — all head projects reference the same Uno SDK packages and add-ins. The CLI should use the **first `.csproj` found in the `.sln` file** (parsing order, not alphabetical). If the selected project's `project.assets.json` is missing, try the next `.csproj` before falling back to `packages.json`-only mode.

> **Note**: A `--project` flag is NOT added in Phase 0. If multi-project selection proves problematic in practice, a `--project` flag can be added in a future phase. The current heuristic (first `.csproj` in `.sln`) is sufficient because all Uno head projects share the same package graph for add-in purposes.

**Performance**: Scanning all packages sounds expensive, but the filter is fast — most packages have no `buildTransitive/*.targets` files at all. Only packages with `.targets` containing `UnoRemoteControlAddIns` are actual add-ins. In practice, a typical Uno project has ~100-200 packages; checking for `.targets` file existence is ~1ms total. Parsing `project.assets.json` itself is fast even for large files (500+ packages = ~2-5 MB; `System.Text.Json` handles this in < 10ms). **Phase 0 implementation should include a benchmark** with a real-world large `project.assets.json` to validate these estimates.

**MSBuild property resolution** (limited, safe subset):

| Property | Resolution |
|----------|-----------|
| `$(MSBuildThisFileDirectory)` | Directory of the `.targets` file being parsed |
| `$(MSBuildThisFile)` | Filename of the `.targets` file (for conditions) |

This handles 100% of known add-in patterns. More complex MSBuild logic (conditions on `$(Configuration)`, etc.) is only used for local development fallback paths in `.targets` files and can be safely ignored — we only need the production package paths.

**Example: parsing `Uno.Settings.DevServer.targets`**:

```xml
<!-- Input: {cache}/uno.settings.devserver/1.2.3/buildTransitive/Uno.Settings.DevServer.targets -->
<ItemGroup>
  <UnoRemoteControlAddIns Include="$(MSBuildThisFileDirectory)../tools/devserver/Uno.Settings.DevServer.dll" />
</ItemGroup>
```

Resolution:
1. `$(MSBuildThisFileDirectory)` = `{cache}/uno.settings.devserver/1.2.3/buildTransitive/`
2. Concatenate: `{cache}/uno.settings.devserver/1.2.3/buildTransitive/../tools/devserver/Uno.Settings.DevServer.dll`
3. Normalize: `{cache}/uno.settings.devserver/1.2.3/tools/devserver/Uno.Settings.DevServer.dll`
4. Verify exists on disk -> add to result

**Example: parsing `Uno.UI.App.Mcp.targets`** (with property indirection):

```xml
<!-- Input: {cache}/uno.ui.app.mcp/6.5.100/buildTransitive/Uno.UI.App.Mcp.targets -->
<PropertyGroup>
  <_UnoMcpServerProcessorPath
    Condition="exists('$(MSBuildThisFileDirectory)../tools/devserver/Uno.UI.App.Mcp.Server.dll')">
    $(MSBuildThisFileDirectory)../tools/devserver/Uno.UI.App.Mcp.Server.dll
  </_UnoMcpServerProcessorPath>
</PropertyGroup>
<ItemGroup>
  <UnoRemoteControlAddIns Include="$(_UnoMcpServerProcessorPath)" />
</ItemGroup>
```

Resolution:
1. Resolve `_UnoMcpServerProcessorPath` property: substitute `$(MSBuildThisFileDirectory)`, check `exists()` condition
2. Resolve `$(_UnoMcpServerProcessorPath)` in the Include → DLL path
3. Normalize path, verify on disk

#### Supplementary: Directory Presence Check (NOT blind DLL scan)

> **WARNING**: The `tools/devserver/` directory contains the add-in entry point DLL **plus all its dependencies** (e.g., `Uno.UI.App.Mcp.Server.dll` + `ModelContextProtocol.dll` + dozens more). Loading ALL `*.dll` files from this directory would cause noise, load errors, and non-deterministic behavior.

The directory check is a **presence heuristic only**, not a loader:

```
For each package in NuGet cache:
  Check if {packagePath}/tools/devserver/ directory exists
  If yes → this package likely provides an add-in, but we DON'T know which DLL is the entry point
  Log a warning: "Package {pkg} has tools/devserver/ but no UnoRemoteControlAddIns in .targets"
  Do NOT blindly load all DLLs from the directory
```

The entry point DLL can only be identified by:
1. **`.targets` parsing** (primary): `UnoRemoteControlAddIns` item specifies the exact DLL
2. **`devserver-addin.json` manifest** (future): explicit path declaration
3. **MSBuild fallback** (legacy): full evaluation

If `.targets` parsing fails to identify the entry point for a package that has `tools/devserver/`, this is a **diagnostic warning** surfaced via `uno://health`, not a silent best-effort load.

#### `devserver-addin.json` Manifest (Phase 1)

For packages that want explicit control over add-in registration (e.g., multiple DLLs, conditional loading, or add-ins that don't use the `.targets` convention), the `devserver-addin.json` manifest provides a direct, unambiguous declaration.

**If present, the manifest takes priority over `.targets` parsing for that package.** If absent, `.targets` parsing is the fallback.

```jsonc
// {packageRoot}/devserver-addin.json
{
  "$schema": "https://schemas.platform.uno/devserver/addin-manifest-v1.json",
  "version": 1,
  "addins": [
    {
      "entryPoint": "tools/devserver/Uno.UI.App.Mcp.Server.dll",
      "minHostVersion": "6.0.0"
    }
  ]
}
```

**Schema**:

| Field | Type | Required | Description |
|-------|------|:--------:|-------------|
| `version` | `int` | Yes | Manifest schema version (currently `1`) |
| `addins` | `array` | Yes | List of add-in entries |
| `addins[].entryPoint` | `string` | Yes | Relative path from package root to the add-in entry point DLL |
| `addins[].minHostVersion` | `string` | No | Minimum DevServer Host version required (semver). If the running Host is older, skip with warning. |

**Rules**:
- `entryPoint` is relative to the package root directory (e.g., `tools/devserver/MyAddin.dll`)
- Path separators use `/` (forward slash), normalized to OS convention at runtime
- The DLL must exist on disk; missing DLL → warning, not fatal
- Unknown fields are ignored (forward compatibility)
- `version > 1` → skip manifest with warning, fall through to `.targets` parsing

**Location in NuGet package**: `{packageRoot}/devserver-addin.json` (at package root, alongside `buildTransitive/`, `tools/`, etc.)

See [Appendix B](spec-appendix-b-addin-discovery.md) section 6 for the full discovery priority chain with manifest support.

#### MSBuild Legacy Fallback

If convention-based discovery finds no add-ins AND the project uses an older SDK version:

```
Fallback chain:
  1. Convention-based: parse .targets + directory scan (<200ms)
  2. Last-known-good cache: reuse paths from last successful discovery (instant)
  3. MSBuild: full dotnet build -t:UnoDumpRemoteControlAddIns (10-30s, legacy)
```

The MSBuild fallback can be:
- Triggered automatically when the fast path finds zero add-ins for an SDK version that should have them
- Triggered explicitly via `--force-msbuild-discovery` flag
- **Goal**: eliminate the MSBuild fallback entirely once all maintained SDK versions ship packages with the standard `.targets` convention

---

### MCP-Specific: Phase 1 — Instant MCP Start + Background Host

**Target**: ~17-40s --> < 1s to first `list_tools` response, < 5s to functional tools.

#### 1a. Start MCP Server Immediately *(Phase 1a target)*

> **Current state**: The CLI does start its STDIO MCP server before the Host is healthy: `EnsureDevServerStartedFromSolutionDirectory()` (`McpProxy.cs:72`) launches the `DevServerMonitor` in a background `Task.Run` (`DevServerMonitor.cs:41`), then `StartMcpStdIoProxyAsync` (`:76`) starts the STDIO server immediately. However, the `list_tools` handler blocks inside `EnsureRootsInitialized` → `await tcs.Task` (`:566`) until the upstream Host responds with tools. So the MCP server is technically running, but `list_tools` hangs until the Host is ready — there is no cached response, no timeout, and no structured error.

**Phase 1a target**: The CLI starts its STDIO MCP server within the first 100ms (after R2R), **before** any discovery or host launch:

```
Program.Main()
  --> Parse args (<10ms)
  --> McpProxy.RunAsync()
      --> Start STDIO MCP server immediately
      --> Register handlers:
          - list_tools: return cached tools from tools-cache.json
          - call_tool: if upstream not ready, return structured error
          - resource (uno://health): return current diagnostics
      --> Background: start discovery + host launch
```

On first `list_tools`:
- Return cached tools from `tools-cache.json` immediately
- These give the AI model tool *descriptions* and *schemas* to plan with
- Tool *calls* that arrive before the host is ready get a structured error (not a hang)

**Current state**: `ToolCacheFile` (`src/Uno.UI.DevServer.Cli/Mcp/ToolCacheFile.cs`) already exists and handles reading, writing, SHA256 validation, and legacy format migration. However, it is **only active** when `IsToolCacheEnabled` (`McpProxy.cs:38`) returns `true`, which requires `--force-roots-fallback` or `--force-generate-tool-cache`. Phase 1a must enable it unconditionally in `--mcp-app` mode.

#### 1b. Background Discovery + Direct Server Launch

Discovery and host launch run in parallel with the MCP server:

```
Background task:
  1. global.json -> Uno SDK package + version
  2. packages.json + project.assets.json -> merged package list with versions
  3. Parse .targets from NuGet packages -> add-in DLL paths (<200ms)
  4. dotnet --version -> TFM (cached to disk)
  5. Launch Host directly in server mode (skip controller)
     Host.dll --httpPort {port} --ppid {pid} --solution {sln} --addins {dll1;dll2}
  6. Wait for /mcp endpoint ready
  7. Connect McpClientProxy
  8. Send tools/list_changed notification
```

**Key change**: Compute add-in DLL paths by parsing `.targets` files from NuGet packages instead of running `dotnet build`.

#### 1c. AI-Friendly Error Model *(Phase 1a target)*

> **Current state**: Tool calls to an unready upstream throw `InvalidOperationException` ("The tool {name} is unknown") at `McpProxy.cs:397` or await indefinitely on `_mcpClientProxy.UpstreamClient` at `:393`. No structured error, no remediation hint.

**Phase 1a target**: Structured error model for AI-friendly diagnostics:

```csharp
internal sealed record ValidationIssue
{
    public required IssueCode Code { get; init; }
    public required ValidationSeverity Severity { get; init; }
    public required string Message { get; init; }
    public string? Remediation { get; init; }
}

internal enum ValidationSeverity { Fatal, Warning }

internal enum IssueCode
{
    // Workspace issues
    GlobalJsonNotFound,
    UnoSdkNotInGlobalJson,
    SdkNotInCache,
    PackagesJsonNotFound,

    // DevServer issues
    DevServerPackageNotCached,
    HostBinaryNotFound,
    HostNotStarted,
    HostCrashed,
    HostUnreachable,

    // Runtime issues
    DotNetNotFound,
    DotNetVersionUnsupported,

    // Add-in issues
    AddInPackageNotCached,
    AddInBinaryNotFound,
    AddInLoadFailed,
    AddInDiscoveryFallback,

    // Generic upstream issue (add-ins report their own details)
    UpstreamError,
}
```

> **SOLID principle**: The `IssueCode` enum covers only DevServer infrastructure concerns (workspace, host, runtime, add-in loading). **License validation, MCP protocol details, and other add-in-specific concerns are external** — they are reported by add-ins (e.g., `Uno.UI.App.Mcp.Server`) through their own mechanisms and surfaced to the user via the upstream MCP connection, not via DevServer's `ValidationIssue` model.
>
> If an upstream add-in returns an error (e.g., license expired, tool unavailable), the CLI proxy forwards it as-is. DevServer uses `UpstreamError` only when the upstream connection itself fails, not to interpret add-in business logic.

**Tool call errors when host not ready**:
```json
{
  "content": [{
    "type": "text",
    "text": "DevServer is starting up. The host process is not yet ready. Read the uno://health resource for detailed diagnostics, or wait a few seconds and retry."
  }],
  "isError": true
}
```

**Never expose raw exceptions to the AI model.**

#### 1d. Health Endpoint — Resource AND Tool *(Phase 1a target — not implemented today)*

> **Current state**: The CLI MCP proxy exposes only `tools/call` and `tools/list` handlers (`McpProxy.cs:383`, `:414`). There is no health resource, no health tool, and no structured error model. Tool calls to an unready upstream return generic exceptions or hang.

**Phase 1a target**: MCP Resources are supported by only 3 of 8 tested clients: Cursor, VS Code Copilot, and Windsurf (see [Appendix G](spec-appendix-g-compatibility-matrix.md) section 5 for the full matrix). Claude Code, Claude Desktop, Codex CLI, Antigravity, and JetBrains Junie do **not** support `resources`. To maximize compatibility, expose health as **both**:

1. **`uno://health` resource** — for clients that support MCP Resources (read via `resources/read`)
2. **`uno_health` tool** — for all clients (always available, zero-argument tool call)

Both return the same `HealthReport` JSON payload:

```csharp
internal sealed record HealthReport
{
    public required HealthStatus Status { get; init; } // Healthy | Degraded | Unhealthy
    public string? UnoSdkVersion { get; init; }
    public string? DevServerVersion { get; init; }
    public int? HostProcessId { get; init; }
    public string? HostEndpoint { get; init; }
    public bool UpstreamConnected { get; init; }
    public int ToolCount { get; init; }
    public long DiscoveryDurationMs { get; init; }
    public required IReadOnlyList<ValidationIssue> Issues { get; init; }
}

internal enum HealthStatus { Healthy, Degraded, Unhealthy }
```

The `uno_health` tool is always registered in `tools-cache.json` and available even before upstream connection. It is the **first tool the AI model should call** when encountering errors.

**Resource subscription**: As of February 2026, **no MCP client supports `resources/subscribe`** (see [Appendix G](spec-appendix-g-compatibility-matrix.md) section 5). Health updates therefore rely on: (1) `tools/list_changed` notification for clients that support it (Cursor, VS Code Copilot, Windsurf), or (2) the `uno_health` tool called on demand. The subscription capability is declared for future-proofing but should not be relied upon.

#### 1e. License-Aware Tool List (FR2, FR10, FR11)

##### Current behavior (upstream, in `MCPToolsObserverService`)

The Host's MCP tool list is **filtered by license tier**:

| License Tier | Tool Count | Notable Exclusions |
|-------------|:-----:|---|
| Community | 9 | `uno_app_element_peer_action`, `uno_app_get_element_datacontext`, `uno_app_get_memory_counters` |
| Pro | 11 | `uno_app_get_memory_counters` |
| Business | 12 | None |

> **Verification note**: These counts are derived from the enumeration in `uno.app-mcp/README.md` (9 Community tools listed individually). The counts depend on the `[LicenseFeatures]` attribute mapping in `MCPToolsObserverService`, which is not directly enumerable from a static count in the code. **Recommendation**: Add a snapshot test in `uno.app-mcp` that asserts tool-per-tier counts. If the count changes (tool added/removed), the spec's tool cache and these counts become stale. Public docs (`doc/articles/features/using-the-uno-mcps.md`) may list fewer tools (e.g., 8) if they predate the addition of `uno_app_start`.

`MCPToolsObserverService.GetListToolsResult()` awaits an internal `_toolsUpdatedTcs` which completes after license checking finishes. This means the **upstream `list_tools` response blocks until license resolution completes** — typically fast (< 1s) but depends on network for token validation.

##### Blocking `list_tools` — the indefinite hang bug (Issue #3)

**Current bug**: For clients in `ClientsWithoutListUpdateSupport` (Claude Code, Codex), `McpProxy` blocks on a `TaskCompletionSource` (`tcs.Task` at `McpProxy.cs:566`) that is only resolved when:
- `_toolListChanged` callback fires (from `McpClientProxy.cs:97`), which requires `tools?.Count > 0`
- If upstream returns **0 tools** (no license, no MCP add-in), the TCS is **never completed** → infinite hang

**Required fix**:
1. The TCS MUST have a **bounded timeout** controlled by a named constant:
   ```csharp
   /// <summary>Maximum time to wait for upstream list_tools before returning cached/empty result.</summary>
   internal const int ListToolsTimeoutMs = 30_000; // 30s — adjustable, must match WaitForServerReadyAsync
   ```
2. On timeout, return **cached tools** (if available) or **empty list with error tool**
3. When upstream returns 0 tools, **still complete the TCS** (0 is a valid answer)
4. On upstream connection failure, complete TCS with error state

```
list_tools handler (for blocking clients):
  1. If cached tools available → return cached tools immediately
  2. Start background: await upstream ready (with ListToolsTimeoutMs timeout)
  3. If upstream responds with tools → return them, update cache
  4. If upstream responds with 0 tools → return empty list (or error tool), complete TCS
  5. If timeout → return cached tools (if any) + health warning, complete TCS
  6. NEVER block indefinitely
```

##### License strategy for tool visibility

Based on MCP ecosystem research (Stripe, GitHub MCP Server, Anthropic context engineering guidance):

**Recommended: show licensed tools + error on unlicensed calls** (current upstream behavior is fine):
- `MCPToolsObserverService` already filters tools by license tier — only licensed tools appear in `list_tools`
- This minimizes context token usage (fewer tools = better model accuracy, per Anthropic research)
- `tools/list_changed` notification sent when license state changes (e.g., user signs in mid-session)
- Unlicensed tools are invisible, not error-producing

**The spec does NOT mandate changing this behavior.** FR2's tool count is license-dependent, and the tool cache should reflect the last-known license state.

**Tool cache implication**: `tools-cache.json` reflects the license tier active at cache time. A Community user's cache will have 9 tools. This is correct behavior — the cache is refreshed when upstream responds with a different tool list.

**Tool cache invalidation** (currently not formalized — must be addressed):
The current cache is a single global file (`McpProxy.cs:110`) with non-atomic writes (`McpProxy.cs:696`). The cache must be keyed or invalidated by:
- **Workspace** (solution path): Different projects may have different add-ins
- **Uno SDK version**: Different SDK versions expose different tools
- **License tier**: Community/Pro/Business see different tool counts

Proposed: Include metadata in the cache file (workspace hash, SDK version, timestamp). On cache read, validate metadata matches current context. On mismatch, treat as cache miss (serve stale tools while refreshing in background).

**Atomic cache writes** (applies to `tools-cache.json`, `dotnet-version-cache.json`, and any other disk caches):
- Write to a temporary file in the same directory (e.g., `tools-cache.json.tmp`)
- Rename (move) the temp file to the target path — `File.Move(temp, target, overwrite: true)`
- This prevents partial reads if the process crashes mid-write
- **NOT** NTFS transactions (`TxF`) — `TxF` is deprecated by Microsoft and not cross-platform
- On Linux/macOS, `rename()` is atomic within the same filesystem. On Windows, `MoveFileEx` with `MOVEFILE_REPLACE_EXISTING` is not strictly atomic but is the best available approach and prevents partial reads

**AmbientRegistry writes**: The AmbientRegistry (`AmbientRegistry.cs:66`) uses `File.WriteAllText` (non-atomic). However, this is a **low risk**: the read side (`GetActiveDevServers()` at `:117-150`) already has per-file try/catch with automatic cleanup of corrupted files. A partial read causes a `JsonException`, which is caught, and the file is deleted. The next read will miss the entry (stale cleanup) — acceptable behavior since the process will re-register. Atomic writes are NOT required for the AmbientRegistry.

#### 1f. Hot Reconnection *(Phase 1c target)*

**Scope note**: This requires a significant refactoring of the current single-shot architecture. The reconnection design requires a proper **state machine**, not a patch on the existing code.

**Prerequisite bugs to fix** (without these fixes, hot reconnection is impossible):

| Bug | Location | Problem | Fix Required |
|-----|----------|---------|-------------|
| **One-shot TCS** | `McpClientProxy.cs:20` | `_clientCompletionSource` is a `TaskCompletionSource<McpClient>` created once, never recreated. A second `ConnectOrDieAsync` call silently fails (`TrySetResult` returns false). All code awaiting `UpstreamClient` gets the old (disposed) client. | Replace with dispose + recreate pattern: new TCS per connection attempt. |
| **Monitor exits after first success** | `DevServerMonitor.cs:135` | After `ServerStarted?.Invoke()`, the `while` loop does `break` — monitor thread exits completely. No continuous process watch. If Host crashes, nobody detects it. | `DevServerMonitor` must become a persistent process watcher: after `ServerStarted`, continue monitoring the Host process (poll `Process.HasExited` or hook `Process.Exited` event). |
| **Wrong notification type** | `McpClientProxy.cs:74` | `ToolListChangedNotification` is deserialized as `ResourceUpdatedNotificationParams` (wrong type). The deserialized value is unused (dead code), but if the types have incompatible schemas, `JsonSerializer.Deserialize` throws `JsonException`, preventing `_toolListChanged?.Invoke()` from firing. | Fix the type to match the notification, or remove the unused deserialization. |
| **Fire-and-forget connection** | `McpClientProxy.cs:35` | `OnServerStarted` wraps `ConnectOrDieAsync` in `Task.Run`. The `catch` block logs the error, but **does not complete the TCS on failure** — anything awaiting `UpstreamClient` hangs forever. **Affects Phase 1a** (DisposeAsync hang, since TCS never completes) **AND Phase 1c** (reconnection impossible without TCS reset). | **Phase 1a**: Complete TCS with error state in the `catch` block (`TrySetException` or `TrySetCanceled`). **Phase 1c**: Add retry logic with bounded attempts + dispose/recreate pattern. |

**Regression risk for monitor fix**: The `break` at line 135 is intentional in the current design — after the controller-launched Host is ready, the monitor's job is done (the controller manages the process). Changing to persistent monitoring must ensure:
- No double-start if the Host process is still running
- `ServerStarted` fires exactly once per successful Host launch, even across restarts
- Cancellation token is respected during watch loops
- Test: monitor detects Host crash → fires event → DevServerMonitor relaunches → `ServerStarted` fires again → `McpClientProxy` reconnects with new TCS

**State machine for MCP proxy lifecycle**:

```
States:
  Initializing    → STDIO server started, serving cached tools
  Discovering     → Resolving SDK, add-ins, host path
  Launching       → Host process started, waiting for ready
  Connecting      → HTTP connection to /mcp
  Connected       → Upstream MCP client active, proxying tools
  Reconnecting    → Host crashed, auto-restarting (max 3 attempts)
  Degraded        → Unrecoverable error, serving cached tools + health warning
  Shutdown        → Clean exit

Transitions:
  Initializing → Discovering       on: start background task
  Discovering → Launching          on: all paths resolved
  Discovering → Degraded           on: resolution failed
  Launching → Connecting           on: health check passes
  Launching → Degraded             on: host failed to start (3 attempts)
  Connecting → Connected           on: McpClient.CreateAsync succeeds
  Connecting → Reconnecting        on: connection failed
  Connected → Reconnecting         on: host process exited / health check failed
  Reconnecting → Launching         on: restart attempt
  Reconnecting → Degraded          on: max retries exceeded
  Any → Shutdown                   on: CancellationToken
```

**Key changes from current design**:
- `DevServerMonitor` becomes a persistent process watcher, not a one-shot detector. Must hook `Process.Exited` or poll `HasExited` to detect Host crash.
- `McpClientProxy` supports dispose + recreate pattern (new TCS per connection attempt). The old `McpClient` must be disposed before creating a new one.
- Tools-changed TCS is reset on each reconnection cycle
- Upstream `McpClient` is disposable and recreatable
- The notification deserialization bug (`McpClientProxy.cs:74`) must be fixed before reconnection logic is added

#### 1g. Skip Controller Process *(Phase 1b target)*

In MCP mode, the 3-process chain becomes a 2-process chain:

**Before**: CLI --> Controller (`--command start`) --> Server (no `--command`)
**After**: CLI --> Server directly (`--httpPort` + `--solution` + `--addins`)

> **Correction**: IDE extensions (VS, Rider, VS Code) **already** launch the Host directly — they do not use `--command start`. VS loads an intermediate assembly via `DevServerLauncher`, Rider and VS Code call `dotnet Host.dll --httpPort ...` directly. The controller path (`Program.Command`) is used only by the CLI `start` command. Therefore, bypassing the controller for MCP is consistent with what IDEs already do.

> **Critical: Controller does NOT forward `--addins`**. In Phase 0, MCP mode and CLI `start` still use the controller path. The controller's `StartCommandAsync` (`Program.Command.cs:18`) accepts only typed parameters (`httpPort`, `parentPID`, `solution`, `workingDir`, `timeoutMs`) and builds child process arguments explicitly (`Program.Command.cs:82-99`) — only `--httpPort`, `--ppid`, and `--solution` are passed to the child server. **Unknown arguments like `--addins` are silently discarded.**
>
> **Phase 0 therefore requires one of**:
> 1. **Modify the controller** to accept and forward `--addins` (add parameter to `StartCommandAsync`, add to child process argument list) — smallest change, keeps existing architecture
> 2. **Bypass the controller** earlier (move controller bypass from Phase 1b to Phase 0 for MCP mode) — larger change, more risk
>
> **Recommended**: Option 1 for Phase 0 (minimal, backward-compatible). The controller modification is a ~10-line change. Option 2 remains the Phase 1b plan.

The controller currently provides **all** of the following responsibilities (for CLI `start` only). These must be reimplemented CLI-side when bypassing the controller in Phase 1b:

| Responsibility | Controller Location | Notes |
|---|---|---|
| **AmbientRegistry duplicate check** | `Program.Command.cs:37-49` | Prevents multiple DevServers for same solution. IDEs do NOT have this. |
| **Port conflict detection** | `Program.Command.cs:50-58` | Checks if requested port is already in use. |
| **Port allocation** | `Program.Command.cs:60-64` | `EnsureTcpPort()` allocates a free TCP port when `httpPort == 0`. |
| **Solution discovery** | `Program.Command.cs:27-31` | Scans for `.sln`/`.slnx` files if no `--solution` provided. |
| **`.csproj.user` generation** | `Program.Command.cs:101` | Writes DevServer port to all project `.csproj.user` files. |
| **Child process spawn** | `Program.Command.cs:67-112` | Handles `.dll` vs `.exe` detection, argument construction, output redirect. |
| **Health polling (TCP loopback)** | `Program.Command.cs:323-347` | TCP check to `IPAddress.Loopback` (127.0.0.1 only). Does **NOT** check IPv6 `[::1]`. Note: `DevServerMonitor` has a separate, more thorough HTTP health check that polls `localhost`, `127.0.0.1`, AND `[::1]` (`DevServerMonitor.cs:151-156`). |
| **Timeout with process cleanup** | `Program.Command.cs:130-145` | Kills child process and drains output on timeout (`--timeoutMs`, default 30s). |
| **Output capture** | `Program.Command.cs:163-196` | Captures child stdout/stderr for diagnostic display. |

**Note**: `DevServerMonitor` already handles port allocation (reimplements `EnsureTcpPort`). Health polling is also partially reimplemented. The remaining gaps are: solution discovery, `.csproj.user`, timeout handling, and output capture.

**CLI-side duplicate protection** (required before controller bypass):
```
Before launching Host:
  1. Check AmbientRegistry for existing DevServer on this solution path
  2. If found, log warning with PID and port info
  3. Either reuse existing instance or fail explicitly
  4. Only then launch new Host
```

New flag on Host: `--addins {dll1;dll2;...}` to bypass `AddIns.Discover()`.

#### 1g-bis. DevServer Instance Management (Critical)

> **This is an existing problem**, not introduced by this spec. But this spec makes it worse (adding MCP as another launcher), so it must be addressed.

**Current situation**: IDE extensions, CLI `start`, and MCP mode can all launch Host instances independently. **None of the IDE extensions check AmbientRegistry** before launching — the duplicate check exists only in the controller path (`Program.Command.cs:37`), which IDEs don't use.

**Scenarios that cause problems today**:

| Scenario | Result |
|----------|--------|
| IDE running + MCP agent starts | Two DevServer instances, port conflict or wrong Hot Reload target |
| IDE running + CLI `start` | Controller checks AmbientRegistry → detects duplicate → returns exit 0 |
| MCP running + IDE starts | IDE launches its own Host, ignoring the MCP-launched one |
| Two IDEs on same solution | Two instances, no protection |
| **CLI `start` via `DevServerMonitor` + existing instance** | **Port mismatch race**: Monitor allocates port A, controller finds existing on port B, returns success. Monitor health-checks port A (nothing there) → `ServerFailed`. The existing instance's actual port is in the controller's stdout but never parsed by the monitor. |

**Required solution**: The CLI (for both `start` and MCP modes) MUST:

1. **Check AmbientRegistry** before launching a new Host
2. If an instance already exists for this solution:
   - **MCP mode**: Connect to the **existing** Host's `/mcp` endpoint instead of launching a new one. This is the ideal scenario — the MCP proxy becomes a client of the IDE-launched DevServer.
   - **CLI `start` mode**: Current behavior (block with message)
3. **Register in AmbientRegistry** after launching a new Host
4. **Unregister on shutdown** (or let the stale-process cleanup handle it)

**`.csproj.user` generation**: This is **controller-only** (`Program.Command.cs:101` calls `CsprojUserGenerator.SetCsprojUserPort()`). The Host server-mode process does NOT write `.csproj.user` — it only registers in AmbientRegistry (`Program.cs:221`).

IDE extensions handle `.csproj.user` independently:
- **Rider**: `DevServerService.cs:79` via its own `CsprojUserGenerator`
- **VS Code**: `unoDebugConfigurationProvider.ts:226`
- **VS**: `DevServerLauncher.cs:187` *(line reference from agent analysis — verify manually in `uno.studio` repo, as exact line may differ)*

**Impact for Phase 1b** (controller bypass): When MCP mode launches the Host directly, `.csproj.user` generation is lost. This MUST be re-implemented either:
1. **CLI-side** (recommended): CLI calls `CsprojUserGenerator` before launching Host
2. **Host-side**: Add `.csproj.user` generation to the server-mode startup path

Without this, the running app won't know the DevServer port for Hot Reload.

> **Note — encoding is NOT an issue**: `CsprojUserGenerator` uses `XDocument.Save()` (`CsprojUserGenerator.cs:137`) which defaults to **UTF-8** in modern .NET. The previous UTF-16 concern was based on stale information and is no longer applicable.

#### 1h. Skip Unnecessary Work in MCP Mode

| Operation | Current | MCP Mode (Phase 1b) |
|-----------|---------|----------|
| `.csproj.user` generation | Controller only (`Program.Command.cs:101`). IDE extensions write their own independently (Rider: `DevServerService.cs:79`, VS Code: `unoDebugConfigurationProvider.ts:226`, VS: `DevServerLauncher.cs:187`). Host server-mode does NOT write it. | **Must re-implement** CLI-side (call `CsprojUserGenerator` before launching Host). Required for app ↔ DevServer port sync. |
| AmbientRegistry check | Controller only | CLI handles it (see 1g-bis) |
| AmbientRegistry registration | Host `Program.cs:221` | Keep (already in Host) |
| `dotnet --version` | Every startup | Cache to `%LOCALAPPDATA%/Uno Platform/uno.devserver/dotnet-version-cache.json` |
| Solution file scan retry (10s) | Always | Immediate fail with structured error |

---

### Phase 2: ReadyToRun Compilation

**Target**: CLI cold start from ~1.5s to ~200ms.

Add ReadyToRun compilation to `Uno.UI.DevServer.Cli.csproj`:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <PublishSelfContained>true</PublishSelfContained>
    <PublishSingleFile>true</PublishSingleFile>
    <PublishReadyToRun>true</PublishReadyToRun>
    <InvariantGlobalization>true</InvariantGlobalization>
    <PublishTrimmed>false</PublishTrimmed>  <!-- Keep reflection working for MCP SDK -->
</PropertyGroup>
```

**No packaging conflict**: .NET 10 introduces RID-specific `dotnet tool` packaging. Self-contained tools with R2R/SingleFile are natively supported while remaining distributable via `dotnet tool install`. No change from `PackAsTool=true` required.

**Considerations**:
- `InvariantGlobalization` is safe — the CLI doesn't use culture-specific formatting for user output.
- `PublishTrimmed=false` is required because `ModelContextProtocol` uses reflection for JSON serialization.
- The `.nupkg` will be larger (self-contained ~70MB vs framework-dependent ~5MB) but startup gains justify this for an always-running background tool.

---

### Phase 3: Graceful Degradation + Startup Without Workspace

#### 3a. No global.json? Start Anyway

Current behavior: discovery fails, MCP server never starts.

New behavior:
1. MCP STDIO server starts immediately
2. `list_tools` returns cached tools (if available) or `uno_app_set_roots` only
3. `uno://health` shows `GlobalJsonNotFound` issue with remediation
4. When workspace is provided (via `uno_app_set_roots` or MCP roots protocol), discovery runs

#### 3b. Backward Compatibility with Older Uno Versions

| SDK Version | `packages.json` Location | Add-in Package | Notes |
|-------------|--------------------------|---------------|-------|
| Current (6.5+) | `targets/netstandard2.0/packages.json` | `uno.ui.app.mcp` | Standard path |
| 6.x | Same | `uno.ui.app.mcp` | Same layout |
| 5.x | May differ or not exist | May not have MCP add-in | Fallback needed |

**Fast-path resolution must handle**:
- Missing `packages.json` (pre-6.x SDKs)
- Different add-in package names
- Different package internal layouts

#### 3c. Fallback Chain for Add-In Resolution

```
1. Fast path: Compute add-in DLL paths from packages.json + project.assets.json + NuGet cache
   --> < 200ms, works for 95%+ of cases (covers SDK and third-party add-ins)

2. Cached path: Use last-known-good add-in paths from disk cache
   --> Immediate, stale but functional for common case
   --> Warn via health resource

3. Last resort: Full MSBuild AddIns.Discover()
   --> 10-30s, guaranteed correct for any SDK version
   --> Triggered by --force-msbuild-discovery or automatic when fast path fails
```

---

### Phase 4 (Future): Project Initialization

- `uno_app_init` tool to scaffold a new Uno project
- Works even without existing workspace
- Out of scope for this specification

---

## 6. Detailed Changes

### Files to Modify

| File | Change Summary | Phase |
|------|---------------|:-----:|
| `src/Uno.UI.RemoteControl.Host/Program.cs` | Accept `--addins` flag, skip `AddIns.Discover()` when provided | 0 |
| `src/Uno.UI.RemoteControl.Host/Program.Command.cs` | Add `--addins` parameter to `StartCommandAsync`, forward to child process argument list | 0 |
| `src/Uno.UI.RemoteControl.Host/Extensibility/AddInsExtensions.cs` | Support pre-resolved add-in paths via new overload | 0 |
| `src/Uno.UI.DevServer.Cli/Helpers/UnoToolsLocator.cs` | Wire in `TargetsAddInResolver`, dotnet version disk caching | 0 |
| `src/Uno.UI.DevServer.Cli/CliManager.cs` | Pass resolved add-in paths when launching Host for `start` command; add `--addins-only` flag to `disco` command | 0 |
| `src/Uno.UI.DevServer.Cli/Helpers/DiscoveryInfo.cs` | Add `AddIns`, `AddInsDiscoveryMethod`, `AddInsDiscoveryDurationMs` fields; add `ResolvedAddIn` record | 0 |
| `src/Uno.UI.DevServer.Cli/Helpers/DiscoveryOutputFormatter.cs` | Display `globalJsonPath` in plain text; display add-in paths; support `--addins-only` output | 0 |
| `src/Uno.UI.DevServer.Cli/Mcp/McpProxy.cs` | Immediate MCP start, background discovery, health resource, structured errors | 1a |
| `src/Uno.UI.DevServer.Cli/Mcp/DevServerMonitor.cs` | Direct server launch (skip controller), hot restart, add-in path passing | 1b |
| `src/Uno.UI.DevServer.Cli/Mcp/McpClientProxy.cs` | Reconnection support (new upstream URL), dispose + recreate pattern | 1c |
| `src/Uno.UI.DevServer.Cli/Program.cs` | DI registration for new services (HealthService, etc.) | 1a |
| `src/Uno.UI.DevServer.Cli/Uno.UI.DevServer.Cli.csproj` | R2R compilation settings | 2 |

### Files to Create

| File | Purpose | Phase |
|------|---------|:-----:|
| `src/Uno.UI.DevServer.Cli/Helpers/TargetsAddInResolver.cs` | Convention-based `.targets` parsing for add-in discovery | 0 |
| `src/Uno.UI.DevServer.Cli/Helpers/DotNetVersionCache.cs` | Disk-cached `dotnet --version` | 0 |
| `src/Uno.UI.DevServer.Cli/Models/ValidationIssue.cs` | AI-friendly error model | 1a |
| `src/Uno.UI.DevServer.Cli/Models/HealthReport.cs` | Health resource payload | 1a |
| `src/Uno.UI.DevServer.Cli/Services/HealthService.cs` | Composite health check | 1a |
| `src/Uno.UI.DevServer.Cli/Services/HealthChecker.cs` | Reusable IPv4+IPv6 health polling (extract from `DevServerMonitor`) | 1b |

---

## 7. Add-In Discovery System Design

> **Full detail**: [addin-discovery.md](spec-appendix-b-addin-discovery.md) — algorithms, convention rules, author guide, package layout examples.

The discovery system is **forward-compatible** (new add-ins work without code changes) and **backward-compatible** (older SDK versions still work).

**Priority chain** (per package):
1. `devserver-addin.json` manifest (highest priority if present)
2. `buildTransitive/*.targets` parsing (primary, convention-based)
3. `tools/devserver/` directory check (diagnostic only — **never** loads DLLs blindly)
4. MSBuild `dotnet build` (legacy fallback, 10-30s)

Levels 1-2 produce loadable DLL paths. Level 3 is diagnostic only. Level 4 is triggered only when 1-2 find zero add-ins for an SDK version that should have them.

**Add-in author guide**: Checklist and convention rules documented in [addin-discovery.md](spec-appendix-b-addin-discovery.md) section 5, to be published as `src/Uno.UI.RemoteControl.Host/DEVSERVER-ADDINS.md`.

---

## 8. Host `--addins` Flag Design

New command-line flag for `RemoteControl.Host`:

```
--addins <semicolon-separated-dll-paths>
```

#### Contract

**Parsing**:
- Paths separated by `;` (semicolon)
- Each path must be an absolute path to a `.dll` file
- Whitespace around paths is trimmed
- Empty entries (e.g., trailing `;`) are ignored

**Validation**:
- Each path is checked for existence on disk before loading
- Missing DLL → warning logged + included in `AddInsStatus` (not fatal)
- Invalid path format → warning logged, skipped

**Precedence rules**:
- `--addins` present → skip `AddIns.Discover()` entirely, use provided paths
- `--addins` absent, `--solution` present → current behavior: `AddIns.Discover(solution)` via MSBuild
- `--addins` present AND `--solution` present → `--addins` wins for add-in loading; `--solution` used for other Host features (Hot Reload, etc.)
- `--addins ""` (empty string) → no add-ins loaded, skip discovery (valid for testing)

**Loading**:
- `AssemblyHelper.Load(paths)` — same loader as current MSBuild path
- `AddFromAttributes(assemblies)` — same DI registration
- `AddInsStatus` populated with load results (same diagnostics as current flow)

When absent:
- Current behavior unchanged (MSBuild discovery via `AddIns.Discover()`)

**Process argument forwarding**: The `--addins` value contains semicolons and may contain paths with spaces. When forwarding through `ProcessStartInfo`, use `ArgumentList.Add()` (not `Arguments` string concatenation) — .NET handles OS-level escaping automatically. The existing `DevServerProcessHelper.CreateDotnetProcessStartInfo` (`DevServerProcessHelper.cs:35`) already uses `ArgumentList.Add()` per argument, so the forwarded `--addins` value must be passed as a **single argument** (the key and value as two entries: `"--addins"` then `"path1;path2"`), not split on semicolons.

This flag benefits **all launchers**, not just MCP:
- **CLI MCP mode**: CLI resolves add-in paths, passes them via `--addins`
- **CLI `start` mode**: Same fast resolution, same `--addins` flag
- **IDE extensions (future)**: VS/Rider/VSCode can adopt the same fast resolution and pass `--addins`
- **IDE extensions (current)**: Continue working without `--addins` — MSBuild discovery unchanged

### IDE Integration Path

IDE extensions currently launch the Host directly and rely on MSBuild discovery. They can adopt fast discovery incrementally:

1. **Phase 1**: CLI resolves paths, passes `--addins` to Host (CLI-only benefit)
2. **Phase 2**: Expose the `.targets` parsing logic as a shared library or CLI command (`disco --addins-only`)
3. **Phase 3**: IDE extensions call `disco --addins-only --json` and pass result as `--addins` to Host
4. **Phase 4**: IDE extensions embed the resolution logic directly (optional)

---

## 8a. `disco` Command Enhancement (Phase 0)

The `disco` command is the CLI's public interface for discovery information. It must serve as the **single source of truth** for external systems (scripts, IDE extensions, CI pipelines) that need discovery data.

### Current State

`disco --json` already outputs a `DiscoveryInfo` JSON object with 16 fields including:
- `globalJsonPath`, `unoSdkPackage`, `unoSdkVersion`, `unoSdkPath` — global.json data
- `packagesJsonPath` — SDK packages manifest
- `devServerPackageVersion`, `devServerPackagePath`, `hostPath` — Host resolution
- `settingsPackageVersion`, `settingsPackagePath`, `settingsPath` — Settings add-in
- `dotNetVersion`, `dotNetTfm` — .NET version info
- `warnings`, `errors` — diagnostics

The plain text format (`disco` without `--json`) displays all fields except `globalJsonPath` (omitted from the Spectre table).

### Phase 0 Additions

Add resolved add-in paths to `DiscoveryInfo`:

```csharp
// New fields in DiscoveryInfo
public IReadOnlyList<ResolvedAddIn> AddIns { get; init; } = [];
public string? AddInsDiscoveryMethod { get; init; }  // "targets", "manifest", "msbuild", "cached"
public long AddInsDiscoveryDurationMs { get; init; }

public sealed record ResolvedAddIn
{
    public required string PackageName { get; init; }      // e.g., "uno.ui.app.mcp"
    public required string PackageVersion { get; init; }   // e.g., "6.5.100"
    public required string EntryPointDll { get; init; }    // absolute path to DLL
    public required string DiscoverySource { get; init; }  // "targets", "manifest", "msbuild"
}
```

**New output flags**:

| Flag | Description | Output |
|------|-------------|--------|
| `disco --json` | Full discovery info including add-ins (enhanced) | JSON `DiscoveryInfo` with `addIns` array |
| `disco --addins-only --json` | Only resolved add-in DLL paths | JSON array of absolute DLL paths (for `--addins` flag) |
| `disco --addins-only` | Only resolved add-in DLL paths (text) | Semicolon-separated paths (pipe-friendly) |

**`--addins-only` flag output** (designed for piping to `--addins`):
```bash
# JSON: array of paths
dnx uno.devserver disco --addins-only --json
# Output: ["/path/to/Uno.UI.App.Mcp.Server.dll", "/path/to/Uno.Settings.DevServer.dll"]

# Text: semicolon-separated (for direct use as --addins value)
dnx uno.devserver disco --addins-only
# Output: /path/to/Uno.UI.App.Mcp.Server.dll;/path/to/Uno.Settings.DevServer.dll
```

**Use cases for external consumers**:
- **IDE extensions**: `disco --addins-only --json` → parse → pass as `--addins` to Host
- **CI scripts**: `disco --json` → validate environment, detect missing packages
- **Diagnostic tools**: `disco --json` → full environment report for support tickets
- **global.json info**: `disco --json` exposes `unoSdkPackage` and `unoSdkVersion` — external tools can determine which SDK is active and its exact version without parsing global.json themselves

### Plain Text Fix

Add `globalJsonPath` to the plain text table in the "Uno SDK" section of `DiscoveryOutputFormatter.WritePlainText()`. This is a minor fix — the data is already in `DiscoveryInfo`, just not displayed.

---

## 8b. MCP Protocol & Architecture Improvements

> **Full detail**: [mcp-improvements.md](spec-appendix-d-mcp-improvements.md)

**Protocol gaps**: The CLI MCP server declares no capabilities, no tool annotations, no structured logging.

**Confirmed bugs** (validated by code review):

| Bug | Location | Severity | Description |
|-----|----------|----------|-------------|
| **Wrong notification type** | `McpClientProxy.cs:74` | Medium | `ToolListChangedNotification` deserialized as `ResourceUpdatedNotificationParams` (wrong type). Value is unused (dead code), but `JsonSerializer.Deserialize` may throw `JsonException` if schemas are incompatible, preventing `_toolListChanged?.Invoke()` from firing. Fix: use correct type or remove dead deserialization. |
| **Hardcoded client names** | `McpProxy.cs:41` | Medium | `ClientsWithoutListUpdateSupport` is a hardcoded set of client names. Already stale (missing Junie, Windsurf). Must be replaced with capability detection from `initialize` request. |
| **Missing `ServerInfo`** | `McpProxy.cs` | Low | MCP server does not declare `ServerInfo` (name, version) in `initialize` response. |
| **One-shot TCS** | `McpClientProxy.cs:20` | **High** | `_clientCompletionSource` created once, never recreated. Blocks hot reconnection entirely. See section 1f for details. |
| **Fire-and-forget connection** | `McpClientProxy.cs:35` | **High** | `OnServerStarted` wraps `ConnectOrDieAsync` in `Task.Run`. Errors are logged but the **TCS is not completed on failure** → indefinite hang for anything awaiting `UpstreamClient`. |
| **Monitor exits after first success** | `DevServerMonitor.cs:135` | **High** | `while` loop does `break` after `ServerStarted?.Invoke()` — no crash detection. See section 1f. |

**Architecture**: `McpProxy.cs` is 700+ lines (SRP violation), no interfaces for `DevServerMonitor`/`McpClientProxy` (DIP). Service locator anti-pattern exists in `DevServerMonitor.cs:67` (`_services.GetRequiredService<UnoToolsLocator>()` instead of constructor injection). Note: `McpProxy` itself correctly receives `DevServerMonitor` and `McpClientProxy` via constructor injection (`McpProxy.cs:43`), but the overall lack of interfaces prevents testing. Recommended split into 5 focused services with explicit DI:

| Service | Responsibility | DI Dependencies |
|---------|---------------|-----------------|
| `McpStdioServer` | STDIO transport, handler registration | `IToolListManager`, `IHealthService` |
| `McpUpstreamClient` | HTTP connection to Host `/mcp`, reconnection | `IDevServerMonitor` |
| `ToolListManager` | Cache, timeout, TCS lifecycle, `tools/list_changed` | `IUpstreamClient` |
| `HealthService` | Report aggregation, issue collection | `IDiscoveryService`, `IDevServerMonitor` |
| `ProxyLifecycleManager` | State machine (8 states), orchestration | All above |

This refactoring is a **prerequisite** for the state machine (Phase 1c). All services must use **explicit constructor injection** — no `new` in business logic, no service locator patterns.

---

## 9. Verification Plan

> **Full detail**: [Appendix C](spec-appendix-c-testing.md) — 48 automated test scenarios, unit/integration/compatibility test strategy, cross-platform tests, performance measurement methodology.
> **Manual QA**: [Appendix H](spec-appendix-h-manual-qa.md) — 25+ scenarios requiring human testers (IDE extension compat, multi-instance, MCP client behavior, license transitions, crash recovery, real-world performance).

**Performance targets**:

| Metric | Current | Target (Phase 1) | Target (Phase 2) |
|--------|---------|-------------------|-------------------|
| Time to first `list_tools` | 15-40s | < 1s (cached) | < 500ms |
| Time to functional tools | 15-40s | < 5s (warm cache) | < 3s |
| CLI cold start | ~1.5s | ~1.5s | ~200ms |

**Testing is a first-class deliverable.** Each phase MUST ship with its tests before being considered complete. Tests cover: unit (12 components), compatibility (8 SDK version scenarios), cross-platform (Windows/macOS/Linux), integration (8 end-to-end scenarios), and non-regression baselines.

---

## 10. Risks and Mitigations

| Risk | Impact | Likelihood | Mitigation |
|------|--------|:----------:|-----------|
| `.targets` parsing misses complex MSBuild logic | Add-in not discovered | Low | Convention rules are simple; diagnostic check catches gaps; MSBuild fallback as last resort |
| Older SDKs with different packages.json layout | Fast path finds no packages | Medium | Version-aware parsing + MSBuild fallback |
| Add-in uses `build/` instead of `buildTransitive/` | Not found by convention scan | Low | Scan `build/` as fallback after `buildTransitive/`; document convention. Algorithm in [Appendix B](spec-appendix-b-addin-discovery.md) must include `build/` in the scan list. |
| Add-in has runtime conditions (config-dependent) | Wrong DLL loaded | Very Low | Production path is always the `exists()` branch; dev fallback paths are ignored |
| R2R breaks `dotnet tool` packaging | CLI can't be installed | Low | Keep R2R behind Release condition, test distribution |
| IDE extensions depend on controller output | IDE integration breaks | Low | Controller mode unchanged, only MCP mode bypasses it initially |
| `--addins` flag alters Host behavior | IDE path affected | Very Low | Flag is opt-in; IDE extensions only adopt it when ready |
| Framework version resolution is too simplistic | Wrong host TFM selected, startup fails | **Medium-High** | See section 13 — TFM is on critical path, not optional |
| Multiple DevServer instances for same solution | Port conflicts, conflicting Hot Reload, code gen connecting to wrong instance | **High (existing bug)** | IDE extensions already lack duplicate protection. CLI MCP mode adds another source. Must implement instance management via AmbientRegistry (see 1g-bis). |
| `list_tools` blocks indefinitely (0 tools or no upstream) | MCP client hangs forever. **Two causes**: (1) `await tcs.Task` at `McpProxy.cs:566` has no timeout; (2) the TCS is completed via `_toolListChanged` callback (`McpProxy.cs:480`), which is invoked from two code paths: the initial `ListToolsAsync` response only when `tools.Count > 0` (`McpClientProxy.cs:94`), and the `tools/list_changed` notification handler (`McpClientProxy.cs:76`). If upstream returns 0 tools on the initial call AND never sends a `tools/list_changed` notification (e.g., license failure, add-in load failure, no tools registered), neither path fires and `tcs.Task` never completes. | **High (current bug)** | Phase 1a: bounded timeout (30s) on `tcs.Task` + invoke callback even when `Count == 0` + cached tools fallback (see 1e / FR11). **Neither mitigation is implemented today.** |
| Upstream `MCPToolsObserverService` TCS has no timeout | If license check throws or hangs **before** `TrySetResult()` at `:161`, upstream `list_tools` blocks forever (`MCPToolsObserverService.cs:194`, TCS at `:37`). Note: `TrySetResult()` IS called in the normal 0-tool path; the gap is exception/hang before that line. | **High (upstream bug)** | Upstream fix recommended — see `uno.app-mcp/README.md` alongside this spec. CLI-side 30s timeout (Phase 1a) mitigates for MCP mode once implemented. |
| `.targets` diagnostic finds `tools/devserver/` but entry point unknown | Silently degraded state | Medium | Warning in health resource; do NOT load DLLs blindly |
| Upstream `list_tools` blocks on license resolution | Slower than expected "functional tools" time | Medium | FR10 acknowledges this; CLI-side cache serves tools while upstream resolves |
| **Tool cache only active in forced modes** | `IsToolCacheEnabled` (`McpProxy.cs:38`) returns `true` only when `--force-roots-fallback` or `--force-generate-tool-cache` is set. In normal MCP mode (no flags), `tools-cache.json` is never read or written. The "instant start with cached tools" strategy (Phase 1a) requires the cache to be active **unconditionally** in MCP mode. `File.WriteAllText` at `:696` is also non-atomic (crash mid-write → corrupt cache). | **Medium** | Phase 1a: enable cache unconditionally in `--mcp-app` mode. Use atomic write (write to temp file, then `File.Move` with overwrite). Consider per-solution cache path to avoid context mixing across projects. |
| **Controller bypass reimplementation scope** (Phase 1b) | Missing controller responsibilities break MCP mode | **Medium-High** | Controller has 9 responsibilities (see 1g table). Each must be reimplemented or explicitly delegated. High test coverage required — each responsibility needs a dedicated test. |
| **VS extension launcher reflection fragility** | VS extension (`uno.studio`) uses reflection to load `Uno.UI.RemoteControl.VS.dll` and probe **two type names**: `Uno.UI.DevServer.VS.EntryPoint` then `Uno.UI.RemoteControl.VS.EntryPoint` (`DevServerLauncher.cs:302-303`), with v3/v2/v1 constructor probing (`DevServerLauncher.cs:313-326`). Changes to either type name or any constructor signature break the VS extension. | Medium | Lock both type names and all three constructor signatures (v1/v2/v3) with regression tests. See `uno.studio/README.md` alongside this spec for full signature details. |
| **Rider auto-restart race condition** | Rider extension auto-restarts Host immediately on process exit. If CLI MCP mode kills and relaunches Host, Rider may race to restart its own copy → two instances. | **Medium** | AmbientRegistry pre-check exists **only in the controller path** (`Program.Command.cs:37-49`), NOT in the server-mode startup (`Program.cs` only registers at line 221, no pre-check). Rider launches Host directly (no controller). **Mitigation requires adding AmbientRegistry pre-check to the server-mode path** OR ensuring Rider uses the controller path. Test: MCP restarts Host while Rider is connected → verify only one instance survives. |
| **Controller does NOT forward `--addins` to child process** | `StartCommandAsync` (`Program.Command.cs:18`) accepts only typed parameters. Child process args are built explicitly (`Program.Command.cs:82-99`): only `--httpPort`, `--ppid`, `--solution`. Unknown args like `--addins` are silently discarded. | **High (blocking for Phase 0)** | Phase 0 must modify the controller to accept and forward `--addins`. See section 1g for the two options. |
| **Multi-instance port mismatch** | `DevServerMonitor` allocates port A (`DevServerMonitor.cs:83-85`), launches controller with port A. Controller finds existing instance on port B via AmbientRegistry, returns exit code 0 (success). Monitor health-checks port A (no server listening) → health check fails → `ServerFailed` fires. The existing instance's port is in the controller's stdout but **never parsed by the monitor**. | **Medium-High** | **Phase 0**: Log a warning when the controller returns exit code 0 but health check fails on the allocated port — this signals an existing instance on a different port. Do NOT parse stdout (fragile). **Phase 1b**: Eliminates this entirely by checking AmbientRegistry CLI-side before launching, making the controller bypass and port allocation aware of existing instances. |
| **User docs reference wrong notification name** | `doc/articles/dev-server.md:44` mentions `tool_list_changed` but the MCP protocol notification is `notifications/tools/list_changed` (sent via `NotificationMethods.ToolListChangedNotification` at `McpProxy.cs:483`). Agents reading the docs may misinterpret the capability name. | **Low** | **Deliverable**: Update `doc/articles/dev-server.md` to use the correct MCP protocol name `tools/list_changed` when implementation ships. |
| **`$NUGET_PACKAGES` set to empty string** | If `NUGET_PACKAGES=""` (set but empty), `Path.Combine("", packageId, version)` produces a **relative path** (`UnoToolsLocator.cs:350`). `Directory.Exists()` then checks relative to CWD, which could match an unrelated directory. | **Medium** | **Phase 0 deliverable**: Guard against empty/whitespace values: skip the env var path when `string.IsNullOrWhiteSpace(envVar)`. Add unit test case (see Appendix C, NuGet cache path guard). This is a pre-existing bug that Phase 0's path resolution must not inherit. |
| **Controller bypass drops `--metadata-updates` flag** | Phase 1b bypasses the controller and launches Host directly. The `--metadata-updates` flag (used by VS Code for Roslyn-based hot reload, see section 15) must be forwarded. The controller currently forwards nothing beyond typed params, so direct launch must explicitly pass through all Host-relevant flags. | **Medium** | Phase 1b implementation must collect and forward all recognized Host flags (see section 15 table). Test: launch Host directly with `--metadata-updates true` → verify `ServerHotReloadProcessor` activates. |
| **Wrong deserialization type for `tools/list_changed` notification** | `McpClientProxy.cs:74` deserializes the `tools/list_changed` notification as `ResourceUpdatedNotificationParams` instead of the correct `ToolListChangedNotificationParams` (or ignoring params entirely, since the notification has no payload). The callback still fires (line 76) so the functional impact is low today, but incorrect deserialization may throw on future SDK versions that validate param types. | **Medium** | Phase 1a: fix deserialization to use correct type or simply ignore params (the notification is a signal, not a data carrier). |
| **`McpClientProxy` dispose blocks forever if connection never established** | `DisposeAsync()` (`McpClientProxy.cs:117`) does `await (await UpstreamClient).DisposeAsync()`. `UpstreamClient` is `_clientCompletionSource.Task` (`:20-23`). If `OnServerStarted` never fires (Host never starts), the TCS is never completed and dispose awaits indefinitely. This blocks process shutdown (Ctrl+C or MCP client disconnect). | **High** | **Phase 1a (required)**: Cancel the TCS in `DisposeAsync()` before awaiting: `_clientCompletionSource.TrySetCanceled()`. Phase 1c (resettable proxy) addresses this structurally, but the immediate fix cannot wait — process shutdown must not hang. |

---

## 11. Implementation Phases

### Phase 0: Convention-Based Add-In Discovery
- Implement `.targets` parsing logic (`TargetsAddInResolver`)
- Implement directory presence diagnostic (NOT blind DLL loading)
- Implement `--addins` flag on Host server-mode with full contract (section 8)
- **Modify controller** to accept and forward `--addins` to child server process (add parameter to `StartCommandAsync`, add to argument list in `Program.Command.cs:82-99`)
- Wire into CLI `start` command (passes `--addins` to controller → Host)
- Enhance `disco` command: add resolved add-in paths to output, add `--addins-only` flag (section 8a)
- Fix `DiscoveryOutputFormatter`: display `globalJsonPath` in plain text format
- `DotNetVersionCache`: cache `dotnet --version` to disk (on critical path, see section 13)
- Add tests: known add-ins discovered, unknown packages skipped, missing packages warned, blind DLL scan prevented
- Add regression test: `EntryPoint` type names (`Uno.UI.DevServer.VS.EntryPoint` + `Uno.UI.RemoteControl.VS.EntryPoint`) and v1/v2/v3 constructor signatures (lock reflection targets)
- Capture performance baselines BEFORE changes (see measurement methodology)
- **Fix `$NUGET_PACKAGES` empty string bug** in `UnoToolsLocator.cs:350`: guard against `string.IsNullOrWhiteSpace()` (pre-existing bug, Phase 0 must not inherit it in new path resolution code)
- **Fix 3 documentation defects** (see section 12, item 8): `using-the-uno-mcps.md`, `dev-server.md`, `get-started-ai-google-antigravity.md`

**Gain per mode in Phase 0**:
| Mode | Current Path | Phase 0 Path | Gain |
|------|-------------|-------------|------|
| CLI `start` | Controller → Host → MSBuild (10-30s) | Controller → Host `--addins` (skip MSBuild) | **10-30s saved** |
| MCP (`--mcp-app`) | Monitor → Controller → Host → MSBuild | Monitor → Controller → Host `--addins` | **10-30s saved** (still goes through controller) |
| IDE extensions | Direct → Host → MSBuild | Unchanged (no `--addins` yet) | None |

> **Note**: MCP mode in Phase 0 still uses the controller path via `DevServerMonitor` (`DevServerMonitor.cs:205`). The controller bypass (direct launch) is Phase 1b. Phase 0's gain for MCP comes from `--addins` skipping MSBuild inside the Host, not from eliminating the controller.

### Phase 1a: Immediate MCP Start (MCP-only)
- Restructure `McpProxy` to start STDIO server immediately
- Return cached tools on first `list_tools` (enable `ToolCacheFile` unconditionally in `--mcp-app` mode)
- Structured error responses for premature tool calls
- **Fix `list_tools` indefinite blocking** (FR11): bounded timeout, handle 0-tool case
- **Fix `McpClientProxy.DisposeAsync` hang**: `TrySetCanceled()` on TCS before awaiting (process shutdown must not block)
- Health resource (`uno://health`) + `uno_health` tool

### Phase 1b: Background Discovery + Direct Server Launch (MCP-only)
- Background discovery chain using Phase 0's fast resolution
- Skip controller process in MCP mode (direct Host launch)
- **Re-implement CLI-side**: AmbientRegistry check (1g-bis) + `.csproj.user` generation (currently controller-only, see `Program.Command.cs:101`)
- Pass add-in paths via `--addins`

### Phase 1c: Hot Reconnection (MCP-only)

**Prerequisite bug fixes** (must be completed BEFORE state machine work — see section 1f for details):
1. Fix one-shot TCS in `McpClientProxy.cs:20` → dispose + recreate pattern
2. Fix monitor `break` in `DevServerMonitor.cs:135` → persistent process watcher
3. Fix wrong notification type in `McpClientProxy.cs:74` → correct type or remove dead code
4. Fix TCS-not-completed-on-error in `McpClientProxy.cs:35` → complete TCS in `catch` block + bounded retry

**State machine refactoring** (see 1f): Initializing → Discovering → Launching → Connecting → Connected → Reconnecting → Degraded
- `DevServerMonitor` becomes persistent process watcher (not one-shot detector)
- `McpClientProxy` supports dispose + recreate pattern (resettable TCS)
- `tools/list_changed` notification on reconnect
- Max 3 restart attempts before entering Degraded state

### Phase 2: ReadyToRun Compilation
- R2R + SingleFile for CLI
- Performance measurement and validation against Phase 0 baselines

### Phase 3: Graceful Degradation + Documentation
- Start without workspace
- Backward compatibility testing with older SDKs
- `devserver-addin.json` manifest support (convergence with discovery roadmap)
- DevServer add-in author guide: `src/Uno.UI.RemoteControl.Host/DEVSERVER-ADDINS.md`
- IDE integration guide for fast discovery adoption

### Cross-Phase Deliverable: AGENTS.md + DevServer Agent

Create `.github/agents/devserver-agent.md` and reference it from `AGENTS.md` (agent table, Key Source Directories, Specialized Agents). This file provides AI agents and maintainers with build/test/architecture guidance specific to the DevServer subsystem. Currently `AGENTS.md` does not mention the DevServer at all.

**Required content** (following the format of existing agents like `runtime-tests-agent.md`):

| Section | Content |
|---------|---------|
| Architecture | 3-process chain (CLI → Controller → Host), relationship between `Uno.DevServer` and `Uno.WinUI.DevServer` packages |
| Key source directories | `src/Uno.UI.DevServer.Cli/`, `src/Uno.UI.RemoteControl.Host/`, `build/test-scripts/`, `build/nuget/` |
| Build | Commands for CLI and Host (independent of main UI framework — no `crosstargeting_override.props` needed) |
| Testing | Unit test project, integration test script with `-DevServerCliDllPath` for local execution |
| CLI commands | Command table (`start`, `stop`, `list`, `cleanup`, `disco`, `--mcp-app`, `login`) + useful dev flags (`-l trace`) |
| Add-in discovery | Fast path (`.targets` parsing) vs MSBuild fallback, key file (`UnoToolsLocator.cs`) |
| Packaging | Two packages, Host multi-TFM, NuGet override mechanism |
| MCP proxy | Key files (`McpProxy.cs`, `McpClientProxy.cs`, `DevServerMonitor.cs`), tool caching, hot reconnection |
| Common maintenance tasks | How to add a CLI command, modify Host startup, modify discovery, modify MCP proxy |
| IDE compatibility | Backward compat constraints (VS reflection probing, `--addins` opt-in, `ConfigurationBuilder.AddCommandLine()`) |

**AGENTS.md changes** (3 additions):
1. Agent table: `| DevServer CLI | .github/agents/devserver-agent.md | DevServer CLI/Host build, test, MCP proxy |`
2. Key Source Directories: add `src/Uno.UI.DevServer.Cli/` and `src/Uno.UI.RemoteControl.Host/`
3. Specialized Agents: add `- .github/agents/devserver-agent.md - DevServer CLI/Host maintenance`

Keep the agent file in sync with code changes across all phases.

---

## 11b. CI, Packaging, and Deployment Impact

This section documents the impact of each phase on the build pipeline, NuGet packages, and CI tests. **All functional and packaging checks are automated in the integration test script** (`build/test-scripts/run-devserver-cli-tests.ps1`). The implementer runs this script locally before pushing; CI runs the same script on all 3 OS. Only IDE regression and NuGet override mechanism testing remain manual.

### Affected Packages

| Package | Package ID | Type | Affected By | Build Source |
|---------|-----------|------|-------------|-------------|
| **DevServer CLI** | `Uno.DevServer` | `dotnet tool` (`PackAsTool=true`) | Phase 0, 1, 2 | `src/Uno.UI.DevServer.Cli/Uno.UI.DevServer.Cli.csproj` |
| **DevServer Host** | *(embedded)* | ASP.NET Core app inside parent package | Phase 0 | `src/Uno.UI.RemoteControl.Host/Uno.UI.RemoteControl.Host.csproj` |
| **DevServer umbrella** | `Uno.WinUI.DevServer` | NuGet package | Phase 0 (Host changes) | `build/nuget/Uno.WinUI.DevServer.nuspec` |

**Version coordination constraint**: The CLI (`Uno.DevServer`) and Host (`Uno.WinUI.DevServer`) are separate packages but `--addins` must be present in both. **Phase 0 must ship both packages in the same release.** The CLI generates `--addins` arguments; the Host consumes them. A version mismatch (new CLI + old Host) is safe (Host ignores unknown flags via `ConfigurationBuilder.AddCommandLine()`), but the reverse (old CLI + new Host) provides no benefit.

### Host Multi-TFM

The Host targets `$(NetPrevious);$(NetCurrent)` (currently `net9.0;net10.0`). Changes to `Program.cs` (accepting `--addins`) compile for both TFMs. The nuspec packages both:

```xml
<!-- build/nuget/Uno.WinUI.DevServer.nuspec:175-176 -->
<file src="..\..\src\Uno.UI.RemoteControl.Host\bin\Release\net9.0\*.*" target="tools\rc\host\net9.0" />
<file src="..\..\src\Uno.UI.RemoteControl.Host\bin\Release\net10.0\*.*" target="tools\rc\host\net10.0" />
```

### CI Pipeline Impact

**Existing pipeline**: Azure DevOps, defined in `build/ci/tests/.azure-devops-tests-templates.yml:245-319`.

| Component | File | Current State | Required Changes |
|-----------|------|--------------|-----------------|
| DevServer CLI test job | `.azure-devops-tests-templates.yml:245` | `Dotnet_Tests_Validate_DevServerCli` — runs on Windows, Linux, macOS (25min timeout) | May need increased timeout for new tests |
| Integration test script | `build/test-scripts/run-devserver-cli-tests.ps1` | 5 tests: start/stop, solution-dir, MCP mode, roots fallback, Codex | **Add 6 new test functions** (see below) |
| Unit test project | `src/Uno.UI.RemoteControl.DevServer.Tests/` | Tests Host internals (BannerHelper, AppLaunch, AssemblyInfoReader) | **Add unit tests for `TargetsAddInResolver`, `--addins` parser, NuGet cache guard** |
| Package build | `build/ci/build/.azure-devops-build-managed.yml` | Builds all managed packages via `BuildCIPackages` target | No pipeline changes needed — existing build includes both projects |

**Important**: There is NO existing `Uno.UI.DevServer.Cli.Tests` project. The spec's test fixtures (`src/Uno.UI.DevServer.Cli.Tests/Fixtures/`) require **creating this project**. Alternatively, add CLI-specific unit tests to the existing `Uno.UI.RemoteControl.DevServer.Tests` project. Decision left to implementer.

### New Integration Test Functions (CI-Automated)

Add the following test functions to `build/test-scripts/run-devserver-cli-tests.ps1`, following the existing pattern (`Test-DevserverStartStop`, etc.). Each function is called from the main `try` block alongside the existing 5 tests.

#### `Test-DiscoJsonOutput` (Phase 0)

Validates `disco --json` output structure and backward compatibility.

```powershell
function Test-DiscoJsonOutput {
    param([string]$SlnDir)

    Write-Log "Validating disco --json output"

    $output = Invoke-DevserverCli -Arguments @('disco', '--json', '--solution-dir', $SlnDir) 2>&1
    if ($LASTEXITCODE -ne 0) { throw "disco --json exited with code $LASTEXITCODE" }

    $json = ($output | Out-String) | ConvertFrom-Json -ErrorAction Stop

    # Backward compat: existing fields must still be present
    $requiredFields = @('globalJsonPath', 'unoSdkPackage')
    foreach ($field in $requiredFields) {
        if (-not $json.PSObject.Properties[$field]) {
            throw "disco --json missing required field: $field"
        }
    }

    # New field: addIns array
    if (-not $json.PSObject.Properties['addIns']) {
        throw "disco --json missing new 'addIns' field"
    }

    if ($json.addIns.Count -eq 0) {
        Write-Log "WARNING: disco --json returned 0 add-ins (may be expected if no Uno project)"
    }

    foreach ($addIn in $json.addIns) {
        if (-not $addIn.entryPointDll -or -not (Test-Path $addIn.entryPointDll)) {
            throw "disco --json add-in entryPointDll does not exist: $($addIn.entryPointDll)"
        }
        if (-not $addIn.discoverySource) {
            throw "disco --json add-in missing discoverySource field"
        }
        Write-Log " Add-in: $($addIn.entryPointDll) (source: $($addIn.discoverySource))"
    }

    Write-Log "disco --json output validated successfully"
}
```

#### `Test-DiscoAddInsOnly` (Phase 0)

Validates `disco --addins-only` output format and round-trip with `--addins`.

```powershell
function Test-DiscoAddInsOnly {
    param([string]$SlnDir)

    Write-Log "Validating disco --addins-only output"

    # Text mode: semicolon-separated paths
    $textOutput = (Invoke-DevserverCli -Arguments @('disco', '--addins-only', '--solution-dir', $SlnDir) 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) { throw "disco --addins-only exited with code $LASTEXITCODE" }

    # Verify parseable as semicolon-separated paths
    $paths = $textOutput -split ';' | Where-Object { $_ -ne '' }
    foreach ($p in $paths) {
        if (-not (Test-Path $p)) { throw "disco --addins-only path not found: $p" }
    }

    # JSON mode: JSON array of paths
    $jsonOutput = (Invoke-DevserverCli -Arguments @('disco', '--addins-only', '--json', '--solution-dir', $SlnDir) 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) { throw "disco --addins-only --json exited with code $LASTEXITCODE" }

    $jsonPaths = $jsonOutput | ConvertFrom-Json -ErrorAction Stop
    if ($jsonPaths.Count -ne $paths.Count) {
        throw "disco --addins-only text ($($paths.Count)) and JSON ($($jsonPaths.Count)) path counts differ"
    }

    Write-Log "disco --addins-only validated: $($paths.Count) paths"
}
```

#### `Test-CleanupCommand` (Phase 0)

Validates that `cleanup` removes stale registrations and exits cleanly.

```powershell
function Test-CleanupCommand {
    param([string]$SlnDir)

    Write-Log "Validating cleanup command"

    Invoke-DevserverCli -Arguments @('cleanup', '-l', 'trace')
    if ($LASTEXITCODE -ne 0) {
        throw "cleanup command exited with code $LASTEXITCODE"
    }

    Write-Log "cleanup command completed successfully (exit code 0)"
}
```

#### `Test-HostAddInsFlag` (Phase 0)

Validates Host `--addins` flag acceptance — both with and without the flag (regression test).

```powershell
function Test-HostAddInsFlag {
    param([string]$SlnDir)

    Write-Log "Validating Host --addins flag"

    # Step 1: Get add-in paths from disco
    $addInsOutput = (Invoke-DevserverCli -Arguments @('disco', '--addins-only', '--solution-dir', $SlnDir) 2>&1 | Out-String).Trim()
    if ([string]::IsNullOrWhiteSpace($addInsOutput)) {
        Write-Log "No add-ins discovered — skipping --addins flag test"
        return
    }

    # Step 2: Resolve Host DLL path from Uno SDK
    $discoJson = (Invoke-DevserverCli -Arguments @('disco', '--json', '--solution-dir', $SlnDir) 2>&1 | Out-String) | ConvertFrom-Json
    $hostDll = $discoJson.hostPath
    if (-not $hostDll -or -not (Test-Path $hostDll)) {
        Write-Log "Host DLL not found at '$hostDll' — skipping --addins flag test"
        return
    }

    # Step 3: Launch Host with --addins, verify it starts (port 0 = auto-assign)
    $hostProcess = Start-Process -FilePath "dotnet" -ArgumentList @(
        $hostDll, '--httpPort', '0', '--addins', $addInsOutput
    ) -PassThru -NoNewWindow -RedirectStandardError ([System.IO.Path]::GetTempFileName())

    Start-Sleep -Seconds 5

    if ($hostProcess.HasExited -and $hostProcess.ExitCode -ne 0) {
        throw "Host with --addins exited with error code $($hostProcess.ExitCode)"
    }

    if (-not $hostProcess.HasExited) {
        Stop-Process -Id $hostProcess.Id -Force -ErrorAction SilentlyContinue
    }

    Write-Log "Host --addins flag accepted successfully"
}
```

#### `Test-PackageContent` (Phase 0)

Validates NuGet package contents after `dotnet pack`. This test requires the `.nupkg` artifacts to be available (already the case in CI — the build step produces them before the test step runs).

```powershell
function Test-PackageContent {
    param([string]$PackagesDir)  # CI artifact directory containing .nupkg files

    Write-Log "Validating package contents"

    # Find Uno.WinUI.DevServer nupkg
    $devServerPkg = Get-ChildItem -Path $PackagesDir -Filter "Uno.WinUI.DevServer.*.nupkg" | Select-Object -First 1
    if (-not $devServerPkg) {
        Write-Log "Uno.WinUI.DevServer .nupkg not found in $PackagesDir — skipping package content test"
        return
    }

    $extractDir = Join-Path ([System.IO.Path]::GetTempPath()) "devserver-pkg-verify"
    if (Test-Path $extractDir) { Remove-Item $extractDir -Recurse -Force }
    Expand-Archive -Path $devServerPkg.FullName -DestinationPath $extractDir

    # Verify both TFM Host binaries are present
    foreach ($tfm in @('net9.0', 'net10.0')) {
        $hostDll = Join-Path $extractDir "tools/rc/host/$tfm/Uno.UI.RemoteControl.Host.dll"
        if (-not (Test-Path $hostDll)) {
            throw "Package missing Host binary for $tfm at tools/rc/host/$tfm/"
        }
        Write-Log " Host binary present: tools/rc/host/$tfm/ ($(((Get-Item $hostDll).Length / 1KB).ToString('N0')) KB)"
    }

    Remove-Item $extractDir -Recurse -Force -ErrorAction SilentlyContinue
    Write-Log "Package content validated successfully"
}
```

#### `Test-PackageSize` (Phase 2 only)

Validates `.nupkg` size stays within NuGet.org limits after R2R packaging.

```powershell
function Test-PackageSize {
    param([string]$PackagesDir)

    Write-Log "Validating package sizes"

    $cliPkg = Get-ChildItem -Path $PackagesDir -Filter "Uno.DevServer.*.nupkg" | Select-Object -First 1
    if ($cliPkg) {
        $sizeMB = $cliPkg.Length / 1MB
        Write-Log " Uno.DevServer: $($sizeMB.ToString('N1')) MB"
        if ($sizeMB -gt 200) {
            throw "Uno.DevServer package exceeds 200MB ($($sizeMB.ToString('N1')) MB) — may fail NuGet.org upload (250MB limit)"
        }
    }
}
```

#### Main Block Integration

Add the new test calls to the main `try` block in `run-devserver-cli-tests.ps1`:

```powershell
# ... existing tests ...
Test-McpModeWithRootsFallback -SlnDir $slnDir -DefaultPort $defaultPort -MaxAllocatedPort $maxAllocatedPort
Test-CodexIntegration -SlnDir $slnDir

# --- Phase 0 new tests ---
Test-DiscoJsonOutput -SlnDir $slnDir
Test-DiscoAddInsOnly -SlnDir $slnDir
Test-CleanupCommand -SlnDir $slnDir
Test-HostAddInsFlag -SlnDir $slnDir
# Test-PackageContent requires $env:PACKAGES_DIR — only in CI
if ($env:PACKAGES_DIR) { Test-PackageContent -PackagesDir $env:PACKAGES_DIR }

# --- Phase 2 (when applicable) ---
# if ($env:PACKAGES_DIR) { Test-PackageSize -PackagesDir $env:PACKAGES_DIR }
```

### Phase 0 Validation Summary

| # | Check | Local | CI | Manual Only |
|---|-------|:-:|:-:|:-:|
| 1 | `dotnet build` both projects | **Yes** | **Yes** | |
| 2 | `dotnet pack` CLI → `.nupkg` created | **Yes** | **Yes** | |
| 3 | `dotnet tool install` from `.nupkg` | via `-DevServerCliDllPath` | **Yes** (line 943-944) | |
| 4 | `disco --json` backward compat + new fields | **Yes** | **Yes** | |
| 5 | `disco --addins-only` text + JSON output | **Yes** | **Yes** | |
| 6 | Package contains both TFM Host binaries | **Yes** (if `$env:PACKAGES_DIR` set) | **Yes** | |
| 7 | Host `--addins` flag accepted | **Yes** | **Yes** | |
| 8 | Host without `--addins` → MSBuild discovery | **Yes** (existing `Test-DevserverStartStop`) | **Yes** | |
| 9 | Existing 5 integration tests pass | **Yes** | **Yes** | |
| 10 | `cleanup` command exits cleanly | **Yes** | **Yes** | |
| 11 | Cross-platform (Linux + macOS) | | **Yes** (3 OS) | |
| 12 | Codex MCP integration | | **Yes** (API key) | |
| 13 | NuGet override mechanism | | | **Yes** |
| 14 | Real IDE (VS/Rider) regression | | | **Yes** |

### Phase 2 Validation Summary (R2R)

| # | Check | Local | CI | Manual Only |
|---|-------|:-:|:-:|:-:|
| 1 | Self-contained `.nupkg` created | **Yes** | **Yes** | |
| 2 | Package size < 200MB | **Yes** (if `$env:PACKAGES_DIR` set) | **Yes** | |
| 3 | `dotnet tool install` on all 3 OS | | **Yes** | |
| 4 | Startup time < 200ms cold start | | | **Yes** |
| 5 | CI artifact size within limits | | **Yes** | |

### Local Development Workflow (Required Before Push)

The implementer MUST run the integration tests locally before pushing to CI. The same script and test functions are used in both environments — CI is not a first-pass validation tool.

**Step 1: Build and pack locally**

```powershell
# Build both projects
cd src
dotnet build Uno.UI.DevServer.Cli/Uno.UI.DevServer.Cli.csproj -c Release
dotnet build Uno.UI.RemoteControl.Host/Uno.UI.RemoteControl.Host.csproj -c Release

# Pack the CLI tool (for Test-PackageContent)
dotnet pack Uno.UI.DevServer.Cli/Uno.UI.DevServer.Cli.csproj -c Release -o ./nupkg
```

**Step 2: Run integration tests against local build**

The script accepts a `-DevServerCliDllPath` parameter (line 5 of `run-devserver-cli-tests.ps1`) that bypasses `dotnet tool install` and runs directly against a local DLL:

```powershell
# From repo root — uses local build, no tool install needed
./build/test-scripts/run-devserver-cli-tests.ps1 `
    -DevServerCliDllPath src/Uno.UI.DevServer.Cli/bin/Release/net9.0/Uno.UI.DevServer.Cli.dll
```

This runs **all** tests (existing 5 + new 6) against the local build. The script auto-detects the DLL path and invokes via `dotnet <dll>` instead of the global tool.

To also run package content validation locally, set the environment variable:

```powershell
$env:PACKAGES_DIR = "src/nupkg"
./build/test-scripts/run-devserver-cli-tests.ps1 `
    -DevServerCliDllPath src/Uno.UI.DevServer.Cli/bin/Release/net9.0/Uno.UI.DevServer.Cli.dll
```

**Step 3: Run unit tests**

```powershell
dotnet test src/Uno.UI.RemoteControl.DevServer.Tests/Uno.UI.RemoteControl.DevServer.Tests.csproj
# And/or, if the new CLI test project exists:
dotnet test src/Uno.UI.DevServer.Cli.Tests/Uno.UI.DevServer.Cli.Tests.csproj
```

**Step 4: Manual-only validations**

These two scenarios cannot be automated and require manual verification:

1. **NuGet Override Mechanism** — Used by IDE extension developers to test local Host changes:

```powershell
cd src/Uno.UI.RemoteControl.Host
dotnet build -c Release /p:UnoNugetOverrideVersion=99.0.0-local
# Verify: ~/.nuget/packages/uno.winui.devserver/99.0.0-local/tools/rc/host/net9.0/ updated
```

2. **IDE Regression** — Open a real Uno project in VS/Rider, verify DevServer still launches via the IDE's own mechanism (no `--addins`). This validates backward compatibility with IDE extensions that bypass the CLI entirely.

### Workflow Summary

```
Implementer                          CI (Azure DevOps)
    │                                      │
    ├─ Step 1: dotnet build + pack         │
    ├─ Step 2: run-devserver-cli-tests.ps1 │
    │          -DevServerCliDllPath ...     │
    ├─ Step 3: dotnet test (unit tests)    │
    ├─ Step 4: Manual IDE + NuGet override │
    ├─ git push ──────────────────────────>│
    │                                      ├─ Build + pack (BuildCIPackages)
    │                                      ├─ dotnet tool install (from .nupkg)
    │                                      ├─ run-devserver-cli-tests.ps1
    │                                      │  (same tests, all 3 OS)
    │                                      ├─ Test-PackageContent (PACKAGES_DIR set)
    │                                      └─ Test-CodexIntegration (CODEX_API_KEY set)
```

The local and CI paths run the **same test functions**. CI additionally exercises:
- Cross-platform coverage (Windows + Linux + macOS)
- Package content verification (`.nupkg` available from build step)
- Codex MCP integration (requires API key, not available locally)

---

## 12. Open Questions

1. ~~**`dotnet tool` vs self-contained**~~: **Resolved.** .NET 10 introduces RID-specific `dotnet tool` packaging, allowing self-contained tools with R2R/SingleFile while remaining distributable via `dotnet tool install`. The CLI can target .NET 10 with `<PublishSelfContained>true</PublishSelfContained>` + `<PublishReadyToRun>true</PublishReadyToRun>` as a dotnet tool. No packaging change required — this is natively supported.
2. ~~**AmbientRegistry in MCP mode**~~: **Resolved — required.** The AmbientRegistry prevents duplicate DevServer instances. The duplicate check exists only in the controller path (`Program.Command.cs:37-58`), which is used only by CLI `start`. IDE extensions already bypass the controller (they launch Host directly) and have no duplicate protection today. MCP mode will also launch Host directly. **The CLI MUST register in AmbientRegistry AND check for existing instances** before launching. This is part of the broader instance management problem (see section 1g-bis).
3. ~~**Health resource protocol**~~: **Resolved.** Expose as **both** `uno://health` resource AND `uno_health` tool. See section 1d for rationale.
4. ~~**`devserver-addin.json` manifest format**~~: **Resolved.** Format defined in [Appendix B](spec-appendix-b-addin-discovery.md) section 6. Manifest takes priority over `.targets` if present; `.targets` is the fallback.
5. **Add-in author documentation**: Location confirmed: `src/Uno.UI.RemoteControl.Host/DEVSERVER-ADDINS.md` (maintainer docs live in source, not on public docs site).
6. ~~**`uno.hotdesign` processor pattern**~~: **Resolved — out of scope.** HotDesign is a **processor**, not an add-in. It is loaded on-demand from the client through the websocket, using `[assembly: ServerProcessorAttribute]`. It does not use the `buildTransitive/*.targets` + `UnoRemoteControlAddIns` convention and is entirely unaffected by the add-in discovery system. No action needed.
7. ~~**Convergence with `DevServerDiscovery.md`**~~: **Resolved.** `DevServerDiscovery.md` is absorbed into this spec directory as a companion document. The trajectory is formalized: `.targets` parsing (Phase 0) → `devserver-addin.json` manifest (Phase 1) → deprecate MSBuild discovery. See [Appendix B](spec-appendix-b-addin-discovery.md) section 6 and Appendix D.

8. **Public docs/spec alignment** (**Phase 0 deliverable** — fix alongside implementation):
   - `doc/articles/features/using-the-uno-mcps.md:59` does not list `uno_app_start` or Business-tier tools — must be updated to match actual server code
   - `doc/articles/dev-server.md:44` uses `tool_list_changed` — should be `tools/list_changed` (MCP protocol notation)
   - `doc/articles/get-started-ai-google-antigravity.md:44` does not recommend `--mcp-wait-tools-list` despite Antigravity lacking `tools/list_changed` support; also Antigravity is absent from the `ClientsWithoutListUpdateSupport` hardcoded list (`McpProxy.cs:41`)
   - These are **documentation defects independent of this spec** but are low-effort fixes that should ship with Phase 0 to avoid user confusion when the new features land. Do not defer to a separate follow-up.

---

## 13. Target Framework Resolution (Critical Path)

> **Note**: TFM resolution is on the **critical startup path** — a wrong TFM means the Host binary is not found and startup fails. This is not a "nice to have" improvement.

The current TFM resolution in `UnoToolsLocator` (`UnoToolsLocator.cs:520,559`) is simplistic:

```csharp
// Current: parse dotnet --version -> "9.0.100" -> "net9.0"
var dotnetVersion = await TryGetDotNetVersionInfo();
var tfm = $"net{dotnetVersion.Major}.{dotnetVersion.Minor}";
```

**Problems on the critical path**:
- `dotnet --version` spawns a subprocess on **every startup** (`UnoToolsLocator.cs:520`) — 0.5-1s
- Multiple .NET SDKs installed → `dotnet --version` may not match the SDK pinned in `global.json`
- Host package may not have a binary for the exact TFM (e.g., only `net9.0` available but SDK reports `net10.0`)
- Preview SDKs may report unexpected version strings

### Phase 0 Fix: Cache `dotnet --version`

**Minimum**: Cache `dotnet --version` result to disk. Invalidate when:
- `global.json` has a different `sdk.version` than cached
- Cache file is older than 24h
- `--force` flag is passed

### Phase 3 Fix: Robust TFM Selection

1. **Respect `global.json` SDK version**: Use the pinned SDK version, not `dotnet --version`
2. **Scan available TFMs in host package**: List `{hostPackage}/tools/rc/host/net*/` directories and pick the best match
3. **Fallback chain**: Exact match → nearest lower → nearest higher → error with clear diagnostics
4. **Cache TFM selection**: The result is stable per workspace; cache to disk alongside `dotnet --version`
5. **Structured error on failure**: `HostNotFound` issue with available TFMs listed in remediation

---

## 14. Strategic Direction: CLI as DevServer Lifecycle Owner

> **This section describes the long-term trajectory.** Not all of this is in scope for the current spec, but the current work must be designed with this direction in mind to avoid painting ourselves into a corner.

### Current State: Fragmented Ownership

Today, DevServer lifecycle management is duplicated across 4 codebases:

| Launcher | Discovery | Host Launch | Port Management | Instance Tracking | Add-in Resolution |
|----------|-----------|-------------|-----------------|-------------------|-------------------|
| VS (`uno.studio`) | Package inspection | Direct | `.csproj.user` | None | MSBuild (in Host) |
| Rider (`uno.rider`) | Package inspection + MSBuild fallback | Direct | `.csproj.user` | None | MSBuild (in Host) |
| VS Code (`uno.vscode`) | MSBuild only | Direct | `.csproj.user` | None | MSBuild (in Host) |
| CLI (`uno.devserver`) | `UnoToolsLocator` | Via controller | `.csproj.user` | AmbientRegistry (controller only) | MSBuild (in Host) |

**Problems**: Duplicated logic, inconsistent behavior, no cross-launcher instance awareness, IDE-specific bugs require IDE-specific fixes.

### Target State: CLI-Centralized Management

The CLI becomes the **single authority** for DevServer lifecycle — all launchers delegate to it:

```
IDE / MCP Agent / CLI user
    |
    v
DevServer CLI (uno.devserver)
    ├── SDK discovery
    ├── Add-in resolution (.targets / manifest)
    ├── Host launch + instance tracking (AmbientRegistry)
    ├── Port management
    └── Health monitoring
    |
    v
RemoteControl.Host (server mode only)
```

**Benefits**:
- **Fix once, fix everywhere**: Bug fixes and optimizations in the CLI benefit all launchers
- **Consistent instance management**: One AmbientRegistry, one duplicate check, one port allocation strategy
- **IDE extensions simplified**: IDE calls CLI command, CLI handles the rest
- **Agent-friendly**: MCP, CLI, and IDE all use the same codepath

### How This Spec Enables the Direction

| This Spec Deliverable | Enables |
|----------------------|---------|
| `TargetsAddInResolver` in CLI | CLI can resolve add-ins without MSBuild → IDE can delegate |
| `--addins` flag on Host | Decouples add-in resolution from Host → any launcher can resolve |
| `disco --json` command | IDE extensions can call CLI for discovery instead of implementing their own |
| AmbientRegistry check in CLI (1g-bis) | CLI becomes instance-aware → can detect and reuse IDE-launched instances |
| Direct server launch (1g) | CLI can launch Host the same way IDEs do → consistent path |

### What This Spec Does NOT Do (Future Work)

- IDE extensions are NOT modified to delegate to CLI (their timeline, their decision)
- No new CLI command to "own" the Host lifecycle for IDE use (e.g., `uno.devserver daemon`)
- No protocol for IDE ↔ CLI communication beyond AmbientRegistry

### Migration Path for IDE Extensions

Adoption paths for each IDE extension are documented in their respective subdirectories alongside this spec (`uno.studio/`, `uno.rider/`, `uno.vscode/`). These files are intended to be moved to their respective repos when adoption work begins. IDE adoption is at each team's pace — this spec does not dictate their timeline.

---

## 15. Global.json Parsing and SDK Identity

### 15.1 Duplication Risk

The global.json parsing logic is currently duplicated in two locations:

| Location | File | SDK Handling | Data Extracted |
|----------|------|-------------|----------------|
| **CLI** | `UnoToolsLocator.cs:267-307` | Both `Uno.Sdk` and `Uno.Sdk.Private` | Tuple: (path, sdkPackage, sdkVersion) |
| **VS extension** | `GlobalJsonObserver.cs:162-207` | Both `Uno.Sdk` and `Uno.Sdk.Private` | Version string only |

Both implementations walk directories upward to find `global.json`, parse `msbuild-sdks`, and handle both SDK variants. The VS extension's implementation is out of scope (deployed, cannot change), but the CLI implementation MUST be the **canonical** implementation for all new code.

**Rule**: All new discovery code (Phase 0's `TargetsAddInResolver`, `DotNetVersionCache`, etc.) MUST call `UnoToolsLocator.ParseGlobalJsonForUnoSdk()` — no additional global.json parsing implementations.

### 15.2 Uno.Sdk vs Uno.Sdk.Private — Version Identity

**Architecture**:

- **`Uno.Sdk.Private`** is the SDK package built from this repository (`Uno.Sdk.csproj`, `PackageId=Uno.Sdk.Private`). Version is generated by Nerdbank.GitVersioning (NBGV) from `version.json`.
- **`Uno.Sdk`** (without `.Private`) is the public SDK package. It is **not built from this repository** — it has its own versioning and release cycle.
- Both packages share the same `packages.json` structure and serve the same purpose (MSBuild SDK for Uno projects). They are functionally equivalent but versioned independently.

**Critical version identity rule**: Both packages can have **stable, production versions** and their version numbers **can overlap**:

```
# These are TWO DIFFERENT packages at TWO DIFFERENT NuGet cache paths:
~/.nuget/packages/uno.sdk/6.0.0/           ← Uno.Sdk @ 6.0.0
~/.nuget/packages/uno.sdk.private/6.0.0/   ← Uno.Sdk.Private @ 6.0.0
```

A `global.json` references exactly one of them:
```json
// Option A: public SDK
{ "msbuild-sdks": { "Uno.Sdk": "6.0.0" } }

// Option B: private SDK (development/internal)
{ "msbuild-sdks": { "Uno.Sdk.Private": "6.0.0" } }
```

**The version string alone is NOT sufficient to identify the package.** The package name + version together form the identity. Any code that resolves NuGet cache paths MUST use both.

**Build-time version substitution**: The source `packages.json` uses placeholder tokens (`DefaultUnoVersion`, `DefaultUnoSdkVersion`) that are replaced during NuGet package build via `ReplaceFileText` (`Uno.Sdk.csproj:71-107`). Published NuGet packages always contain concrete version strings. Discovery code never encounters these placeholders.

**Lookup priority in `UnoToolsLocator.ParseGlobalJsonForUnoSdk()`** (`UnoToolsLocator.cs:287-298`):
1. Check for `Uno.Sdk` first (public package)
2. Fall back to `Uno.Sdk.Private` (internal package)
3. Return tuple: `(globalJsonPath, sdkPackageName, sdkVersion)`

**Rules**:
1. **Never hardcode a package name.** Always use the package name returned by `ParseGlobalJsonForUnoSdk()`. The code at `UnoToolsLocator.cs:287-298` already handles this correctly.
2. **Never assume version format.** Both packages can have stable (`6.0.0`) or prerelease (`6.0.0-dev.146`) versions. Do not use the version string to infer which package it is.
3. **Package name + version = cache path.** The NuGet cache directory is `{cache}/{packageName.ToLower()}/{version}/`. A version `6.0.0` under `Uno.Sdk` is a **completely different directory** than `6.0.0` under `Uno.Sdk.Private`. Never mix them.
4. Phase 0's add-in resolution must propagate the correct SDK package name throughout the entire discovery chain — from `global.json` through `packages.json` through NuGet cache lookup through add-in path resolution.

### 15.3 `packages.json` Version Handling

The `packages.json` `versionOverride` field allows TFM-specific version overrides:

```json
{
  "group": "WasmBootstrap",
  "version": "9.0.23",
  "packages": ["Uno.Wasm.Bootstrap", "Uno.Wasm.Bootstrap.DevServer"],
  "versionOverride": { "net10.0": "10.0.15" }
}
```

**Current state**: `UnoToolsLocator.GetUnoPackageVersionFromManifest()` (line 467-512) does NOT handle `versionOverride` — it always returns the base `version` field.

**Impact on Phase 0**: The two DevServer add-in packages (`uno.ui.app.mcp` and `uno.settings.devserver`) do NOT have `versionOverride` entries today. `Uno.Wasm.Bootstrap.DevServer` has one but is NOT a DevServer add-in (it's a different kind of dev server). Therefore, the current parsing is correct for Phase 0.

**Forward compatibility**: If a future add-in package uses `versionOverride`, the fast path would resolve the wrong version. **Mitigation**: When the resolved version's NuGet cache directory does not exist, check `versionOverride` entries for the current TFM before falling back to MSBuild. Log a warning if `versionOverride` is detected and the override version differs from the base version.

**Edge case**: If both the base version AND the override version exist in the NuGet cache, the current code would use the base version (wrong). The fallback only triggers when the base version's directory is missing. **Future test** (Phase 3): Create a fixture where both versions exist; verify the resolver uses the override version for the matching TFM. This is not a Phase 0 concern since no current add-in uses `versionOverride`.

### 15.4 Known Host Flags (Compatibility Reference)

The Host (`RemoteControl.Host`) accepts these command-line flags via `ConfigurationBuilder.AddCommandLine()` (`Program.cs:42-56`):

| Flag | Type | Used By | Purpose |
|------|------|---------|---------|
| `--command` (or `-c`) | string | CLI `start`/`stop`/`list`/`cleanup` | Controller mode verb |
| `--httpPort` | int | All launchers | Kestrel listen port |
| `--ppid` | int | All launchers | Parent process monitoring |
| `--solution` | string | All launchers | Solution file path for discovery |
| `--workingDir` | string | CLI | Working directory |
| `--timeoutMs` | int | CLI controller | Startup timeout (default 30000) |
| `--ideChannel` | string | IDE extensions | Named pipe GUID for IDE ↔ Host communication |
| `--metadata-updates` | bool | VS Code extension | Enables Roslyn-based hot reload with metadata delta generation. When `true`, the `ServerHotReloadProcessor` watches project files for changes and generates compilation deltas. The app can also enable this via the `ConfigureServer.EnableMetadataUpdates` message. |
| `--addins` | string | **NEW (Phase 0)** | Semicolon-separated add-in DLL paths |
| `--force-msbuild-discovery` | flag | **NEW (Phase 0)** | Forces MSBuild fallback for add-in discovery even when `--addins` is present or fast path succeeds. Intended for diagnostics and troubleshooting. |

> **Note on `--metadata-updates`**: This flag is critical for VS Code's hot reload functionality. The Host parses it via `IConfiguration["metadata-updates"]` in `ServerHotReloadProcessor.MetadataUpdate.cs:47`. When bypassing the controller in Phase 1b, this flag (and any other unknown flags) must be forwarded to the Host process. The `ConfigurationBuilder.AddCommandLine()` approach accepts arbitrary flags, so new flags don't require parsing changes.

---

## 16. NuGet Package Availability (Graceful Degradation)

The CLI tool is installed via `dotnet tool`, but the Uno SDK and add-in packages may not be in the NuGet cache (user hasn't run `dotnet restore`, network unavailable, custom `$NUGET_PACKAGES` path misconfigured).

### Current Error Handling

`UnoToolsLocator.DiscoverAsync()` accumulates errors without fail-fast:

| Missing Package | Severity | Current Message | Current Remediation |
|----------------|----------|----------------|-------------------|
| Uno SDK | Error | "Uno SDK package not found in NuGet cache" | None |
| `packages.json` | Error | "packages.json not found at {path}" | None |
| DevServer Host | Error | "Uno.WinUI.DevServer not found in NuGet cache" | None |
| Settings add-in | Warning | "uno.settings.devserver version not found" | None |
| `dotnet` CLI | Error | "Unable to determine dotnet --version" | None |

**Gaps identified**:
1. **Directory-level checks only**: `EnsureNugetPackage()` checks if the package directory exists, but NOT if the DLL files within it exist. A partial NuGet restore (metadata present, binaries missing) passes discovery but fails at Host assembly load time.
2. **No remediation hints**: Error messages don't suggest `dotnet restore` or list which cache locations were checked.
3. **No checked-paths reporting**: When a package isn't found, the user doesn't know which locations were searched (`~/.nuget/packages/`, `$NUGET_PACKAGES`, common data).

### Phase 0 Requirements

The fast-path `.targets` parsing must handle missing packages at **DLL-level**, not just directory-level:

```
For each resolved add-in path:
  1. Verify the DLL file exists on disk (not just the package directory)
  2. If missing → structured warning with:
     - Package name and version
     - Expected DLL path
     - Checked NuGet cache locations (all of them)
     - Remediation: "Run 'dotnet restore' to download missing packages"
  3. Add to health report as AddInBinaryNotFound issue
```

**Validation chain** (from coarsest to finest):

| Level | Check | Error If Missing |
|-------|-------|-----------------|
| 1 | NuGet cache directory exists | `SdkNotInCache` — "Run `dotnet restore`" |
| 2 | Package directory exists in cache | `AddInPackageNotCached` — "Run `dotnet restore`" |
| 3 | `buildTransitive/*.targets` file exists | Skip package (not an add-in) |
| 4 | `UnoRemoteControlAddIns` item found | Skip package (not an add-in) |
| 5 | **DLL file exists on disk** | `AddInBinaryNotFound` — "Package may be partially restored. Run `dotnet restore --force`" |
| 6 | **DLL file is non-empty** (optional, Phase 3) | `AddInBinaryCorrupted` — "DLL is 0 bytes. Run `dotnet restore --force`" |

> **Note on corrupted DLLs**: If the DLL file exists but is corrupted (0 bytes, wrong architecture, truncated), the Host will crash at `Assembly.LoadFrom()`. **MSBuild fallback would NOT help** — it discovers the same DLL path. The only fix is `dotnet restore --force`. Phase 0 checks existence only (level 5). Phase 3 may add a non-empty check (level 6). Full assembly validation (e.g., PE header check) is not recommended — too expensive and fragile.

**`disco --json` must report checked locations** in a new field:

```csharp
public IReadOnlyList<string> NuGetCacheLocations { get; init; } = [];
// e.g., ["C:\\Users\\user\\.nuget\\packages", "C:\\ProgramData\\NuGet\\packages"]
```

This helps CI scripts and support engineers diagnose cache-related issues.

---

## 17. IPv6 Health Check Support

### Requirement

Health polling in MCP mode (Phase 1b) MUST check all three loopback addresses:
- `http://localhost:{port}/mcp`
- `http://127.0.0.1:{port}/mcp`
- `http://[::1]:{port}/mcp`

This is **not optional** — some environments have IPv4 disabled:
- Docker containers with IPv6-only networking
- WSL2 with specific network configurations
- Corporate networks with IPv6 migration

### Current State

| Component | Check Type | IPv4 | IPv6 | Addresses |
|-----------|-----------|:----:|:----:|-----------|
| **Controller** (`Program.Command.cs:323`) | TCP socket | Yes | **No** | `IPAddress.Loopback` (127.0.0.1 only) |
| **DevServerMonitor** (`DevServerMonitor.cs:151`) | HTTP GET | Yes | **Yes** | `localhost`, `127.0.0.1`, `[::1]` |

### Design

Phase 1b bypasses the controller and uses direct server launch. The health polling logic must use the `DevServerMonitor` approach (already correct), not the controller's limited approach.

**Implementation**: Extract `DevServerMonitor.WaitForServerReadyAsync()` into a reusable `HealthChecker` service that polls all three endpoints in parallel with configurable timeout. This service is used by:
1. `DevServerMonitor` (existing, for MCP mode)
2. Phase 1b direct launch path
3. `uno_health` tool (for on-demand checks)

```csharp
internal sealed class HealthChecker(ILogger<HealthChecker> logger)
{
    private static readonly string[] LoopbackEndpoints = ["localhost", "127.0.0.1", "[::1]"];

    public async Task<bool> WaitForReadyAsync(int port, string path, TimeSpan timeout, CancellationToken ct)
    {
        // Poll all 3 endpoints in parallel, return true on first success
    }
}
```

---

## Appendices

See the Document Map at the top of this file for links to all appendices (A through H).

[Appendix E: Reference](spec-appendix-e-reference.md) contains:
- **E.1**: MCP tools by license tier (Community 9, Pro 11, Business 12)
- **E.2**: Related specifications and discovery roadmap
- **E.3**: Architectural convergence trajectory (`.targets` → manifest → deprecate MSBuild)
- **E.4**: IDE extension analysis (VS, Rider, VS Code adoption path)

