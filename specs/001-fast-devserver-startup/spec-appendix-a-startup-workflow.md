# DevServer Startup Workflow Analysis

This document provides a detailed walkthrough of the current DevServer startup workflow (all modes), including mermaid diagrams, code-level timing analysis, and the proposed optimized workflow. While MCP mode (`--mcp-app`) is used as the primary example, the add-in discovery bottleneck and its solution apply to **all startup modes** (IDE, CLI `start`, MCP).

> **Related documents**: [Main Spec](spec.md) | [Discovery Roadmap](spec-appendix-f-discovery-roadmap.md)

---

## Table of Contents

1. [Current Workflow Overview](#1-current-workflow-overview)
2. [Detailed Sequence Diagram](#2-detailed-sequence-diagram)
3. [Process Chain Analysis](#3-process-chain-analysis)
4. [Add-in Discovery Deep Dive](#4-add-in-discovery-deep-dive)
5. [MCP Proxy Pipeline](#5-mcp-proxy-pipeline)
6. [Proposed Workflow: Phase 0+1b (Fast Discovery + Direct Launch)](#6-proposed-workflow-phase-01b-fast-discovery--direct-launch)
7. [Proposed Workflow: Phase 1a+2 (Instant MCP Start + R2R)](#7-proposed-workflow-phase-1a2-instant-mcp-start--r2r)
8. [Data Flow: packages.json to Add-in DLLs](#8-data-flow-packagesjson-to-add-in-dlls)

---

## 1. Current Workflow Overview

```mermaid
flowchart TD
    A[MCP Client<br/>Claude Code / Codex] -->|STDIO| B[uno-devserver CLI<br/>Process 1]
    B --> C{Parse args}
    C -->|--mcp-app| D[McpProxy.RunAsync]
    D --> E[Start STDIO MCP Server]
    D --> F[DevServerMonitor.StartMonitoring]
    F --> G{Find .sln files?}
    G -->|No| H[Wait 10s, retry]
    H --> G
    G -->|Yes| I[UnoToolsLocator.ResolveHostExecutableAsync]
    I --> J[Parse global.json]
    J --> K[Find Uno SDK in NuGet cache]
    K --> L[Read packages.json]
    L --> M[dotnet --version subprocess]
    M --> N[Resolve Host executable path]
    N --> O[DevServerMonitor.StartProcess]
    O --> P[Launch Host with --command start<br/>Process 2: Controller]
    P --> Q[AmbientRegistry check]
    Q --> R[.csproj.user generation]
    R --> S[Spawn Host server<br/>Process 3: Server]
    S --> T[AddIns.Discover<br/>2x dotnet build]
    T --> U[Assembly loading]
    U --> V[Kestrel start + /mcp mapped]
    V --> W[WaitForServerReadyAsync<br/>Poll up to 30s]
    W --> X[McpClientProxy.ConnectOrDieAsync]
    X --> Y[tools/list_changed notification]
    Y --> Z[MCP Client receives tools]

    style T fill:#ff6b6b,stroke:#333,color:#fff
    style P fill:#ffa94d,stroke:#333,color:#fff
    style S fill:#ffa94d,stroke:#333,color:#fff
    style M fill:#ffd43b,stroke:#333
```

---

## 2. Detailed Sequence Diagram

```mermaid
sequenceDiagram
    participant Client as MCP Client
    participant CLI as uno-devserver CLI
    participant Monitor as DevServerMonitor
    participant Locator as UnoToolsLocator
    participant Controller as Host (Controller)
    participant Server as Host (Server)
    participant MSBuild as dotnet build

    Note over CLI: T=0ms — Process start

    Client->>CLI: STDIO connection
    CLI->>CLI: CliManager.RunMcpProxyAsync()
    CLI->>CLI: McpProxy.RunAsync()
    CLI->>CLI: Host.CreateApplicationBuilder()
    CLI->>CLI: Start STDIO MCP server

    Note over CLI: T=~50ms — MCP server listening

    CLI->>Monitor: StartMonitoring(directory, port)
    Monitor->>Monitor: Scan for *.sln files
    Monitor->>Locator: ResolveHostExecutableAsync(workDir)

    Note over Locator: T=~100ms — SDK discovery begins

    Locator->>Locator: FindGlobalJson(startPath)
    Locator->>Locator: ParseGlobalJsonForUnoSdk()
    Locator->>Locator: EnsureNugetPackage(Uno.Sdk, version)
    Locator->>Locator: GetDevServerPackageVersion() via packages.json
    Locator->>Locator: EnsureNugetPackage(Uno.WinUI.DevServer, version)

    Note over Locator: T=~1.5s — dotnet --version subprocess

    Locator->>Locator: dotnet --version → TFM
    Locator-->>Monitor: hostPath

    Note over Monitor: T=~3.5s — Host launch

    Monitor->>Controller: Process.Start(--command start, --httpPort, --ppid)

    Note over Controller: T=~5s — Controller .NET cold start

    Controller->>Controller: AmbientRegistry check
    Controller->>Controller: CsprojUserGenerator.SetCsprojUserPort()
    Controller->>Server: Process.Start(--httpPort)

    Note over Server: T=~6.5s — Server .NET cold start

    Server->>Server: WebApplication.CreateBuilder()
    Server->>Server: ConfigureAddIns(solution)

    rect rgb(255, 107, 107)
        Note over Server,MSBuild: BOTTLENECK: 10-30s
        Server->>MSBuild: dotnet build -t:UnoDumpTargetFrameworks
        MSBuild-->>Server: target frameworks (5-15s)
        Server->>MSBuild: dotnet build -t:UnoDumpRemoteControlAddIns
        MSBuild-->>Server: add-in DLL paths (5-15s)
    end

    Note over Server: T=~30s — Assembly loading

    Server->>Server: AssemblyHelper.Load(addInDlls)
    Server->>Server: AddFromAttributes(assemblies)
    Server->>Server: Kestrel start, /mcp mapped

    Note over Server: T=~35s — Server ready

    Monitor->>Server: HTTP GET /mcp (health check poll)
    Server-->>Monitor: 200 OK

    Note over CLI: T=~36s — Upstream connection

    CLI->>Server: McpClient.CreateAsync (StreamableHttp)
    Server-->>CLI: Connected
    CLI->>Server: ListToolsAsync()
    Server-->>CLI: 12 tools

    Note over CLI: T=~37s — Tools available

    CLI->>Client: tools/list_changed notification
    Client->>CLI: tools/list
    CLI-->>Client: 12 tools available
```

---

## 3. Process Chain Analysis

```mermaid
graph LR
    subgraph "Process 1: CLI (~1.5s cold start)"
        A1[Parse args] --> A2[DI setup]
        A2 --> A3[McpProxy.RunAsync]
        A3 --> A4[STDIO MCP server]
        A3 --> A5[DevServerMonitor]
    end

    subgraph "Process 2: Controller (~1.5s cold start)"
        B1[Parse --command start] --> B2[AmbientRegistry]
        B2 --> B3[CsprojUser gen]
        B3 --> B4[Spawn server]
    end

    subgraph "Process 3: Server (~1.5s cold start)"
        C1[WebApp builder] --> C2[ConfigureAddIns]
        C2 --> C3[AddIns.Discover]
        C3 --> C4["dotnet build #1 (TFM)"]
        C4 --> C5["dotnet build #2 (AddIns)"]
        C5 --> C6[Assembly loading]
        C6 --> C7[Kestrel start]
    end

    A5 -->|"launches"| B1
    B4 -->|"launches"| C1
    C7 -->|"HTTP /mcp"| A4

    style C3 fill:#ff6b6b,stroke:#333,color:#fff
    style C4 fill:#ff6b6b,stroke:#333,color:#fff
    style C5 fill:#ff6b6b,stroke:#333,color:#fff
```

**Overhead of 3-process chain**: Each .NET process cold start adds ~1.5s. The controller process (Process 2) adds no value in MCP mode — its responsibilities (AmbientRegistry, port allocation, .csproj.user) are either unnecessary or already handled by the CLI.

---

## 4. Add-in Discovery Deep Dive

### Current Flow (MSBuild-based)

```mermaid
flowchart TD
    A[AddIns.Discover] --> B[Build targets file path]
    B --> C["dotnet build -t:UnoDumpTargetFrameworks<br/>5-15 seconds"]
    C --> D{TFMs found?}
    D -->|No| E[Retry with binlog]
    E --> F[Return Failed]
    D -->|Yes| G[For each TFM]
    G --> H["dotnet build -t:UnoDumpRemoteControlAddIns<br/>5-15 seconds per TFM"]
    H --> I{Add-ins found?}
    I -->|Yes| J[Return Success with DLL paths]
    I -->|No, more TFMs| G
    I -->|No, all tried| K[Return Empty]

    style C fill:#ff6b6b,stroke:#333,color:#fff
    style H fill:#ff6b6b,stroke:#333,color:#fff
```

### What MSBuild Actually Resolves

The MSBuild targets (`UnoDumpRemoteControlAddIns`) collect the `UnoRemoteControlAddIns` item group. In practice, this comes from NuGet package `.targets` files:

```xml
<!-- From uno.settings.devserver: buildTransitive/Uno.Settings.DevServer.targets -->
<ItemGroup>
  <UnoRemoteControlAddIns
    Include="$(MSBuildThisFileDirectory)../tools/devserver/Uno.Settings.DevServer.dll" />
</ItemGroup>

<!-- From uno.ui.app.mcp: buildTransitive/Uno.UI.App.Mcp.targets -->
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

Both follow the same convention:
- `.targets` in `buildTransitive/` folder
- `UnoRemoteControlAddIns` item pointing to `tools/devserver/{Name}.dll`
- `$(MSBuildThisFileDirectory)` = the `buildTransitive/` directory

**These paths are fully deterministic and can be resolved by parsing the XML directly.**

### Proposed Flow (Convention-Based `.targets` Parsing)

```mermaid
flowchart TD
    A[TargetsAddInResolver] --> B[Parse global.json]
    B --> C[Locate Uno SDK in NuGet cache]
    C --> D["Read packages.json<br/>(~5ms)"]
    D --> E[For each package in groups]
    E --> F["Find package in NuGet cache"]
    F --> G{"buildTransitive/*.targets<br/>exists?"}
    G -->|Yes| H["Parse .targets XML"]
    H --> I{"UnoRemoteControlAddIns<br/>item found?"}
    I -->|Yes| J["Resolve $(MSBuildThisFileDirectory)<br/>+ property references"]
    J --> K["Normalize path, verify DLL exists"]
    K --> L[Add to resolved set]
    I -->|No| M{"tools/devserver/<br/>directory exists?"}
    G -->|No| M
    M -->|Yes| N["⚠️ Log diagnostic warning<br/>(DO NOT load DLLs)"]
    N --> O
    M -->|No| O[Skip package]
    L --> P{More packages?}
    O --> P
    P -->|Yes| E
    P -->|No| Q{Any paths resolved?}
    Q -->|Yes| R["Return resolved paths<br/>(< 200ms total)"]
    Q -->|No| S["Fallback: MSBuild<br/>(10-30s, legacy)"]

    style D fill:#51cf66,stroke:#333
    style H fill:#51cf66,stroke:#333
    style J fill:#51cf66,stroke:#333
    style R fill:#51cf66,stroke:#333
    style S fill:#ffa94d,stroke:#333
```

### Known Add-in Packages

| Package | `.targets` Pattern | DLL Path | Source |
|---------|-------------------|----------|--------|
| `uno.ui.app.mcp` | Property indirection + `exists()` | `tools/devserver/Uno.UI.App.Mcp.Server.dll` | `uno.app-mcp` repo |
| `uno.settings.devserver` | Direct include | `tools/devserver/Uno.Settings.DevServer.dll` | `uno.licensing` repo |

Any future add-in package following the convention (`buildTransitive/*.targets` + `tools/devserver/`) is discovered automatically.

### `.targets` Property Resolution

The parser handles a safe subset of MSBuild properties:

| Property | Resolution | Example |
|----------|-----------|---------|
| `$(MSBuildThisFileDirectory)` | Directory of the `.targets` file (with trailing separator) | `{cache}/pkg/1.0/buildTransitive/` |
| `$(MSBuildThisFile)` | Filename of the `.targets` file | `Uno.UI.App.Mcp.targets` |
| `$({UserProperty})` | Resolved from `<PropertyGroup>` elements in the same file | `_UnoMcpServerProcessorPath` |
| `exists('...')` | Checked on disk | File system check |

Properties referencing external state (`$(Configuration)`, `$(UsingUnoSdk)`, etc.) are ignored — these are only used for local development fallback paths that don't apply to NuGet cache resolution.

---

## 5. MCP Proxy Pipeline

### Current STDIO ↔ HTTP Proxy

```mermaid
flowchart LR
    subgraph "STDIO (downstream)"
        A[MCP Client] -->|stdin| B[McpProxy]
        B -->|stdout| A
    end

    subgraph "HTTP (upstream)"
        B -->|StreamableHttp| C[Host /mcp]
        C -->|StreamableHttp| B
    end

    subgraph "McpProxy internals"
        D[WithListToolsHandler] --> E{Upstream connected?}
        E -->|Yes| F[Forward to upstream]
        E -->|No, roots fallback| G[Return cached tools + set_roots]
        E -->|No, normal| H[Return empty]

        I[WithCallToolHandler] --> J{Is set_roots?}
        J -->|Yes| K[Process roots locally]
        J -->|No| L[Forward to upstream]
    end
```

### Tool List Resolution Flow

```mermaid
sequenceDiagram
    participant Client as MCP Client
    participant Proxy as McpProxy
    participant Cache as ToolCacheFile
    participant Upstream as McpClientProxy

    Client->>Proxy: tools/list

    alt First call — roots not initialized
        Proxy->>Client: Request roots (if supported)
        Client-->>Proxy: roots response
        Proxy->>Proxy: ProcessRoots() → StartDevServerMonitor()
    end

    alt Client without list_changed support
        Note over Proxy: Block until upstream ready
        Proxy->>Upstream: await UpstreamClient
        Upstream-->>Proxy: McpClient ready
    end

    alt Upstream connected
        Proxy->>Upstream: ListToolsAsync()
        Upstream-->>Proxy: Tool[]
        Proxy->>Cache: PersistToolCacheIfNeeded(tools)
        Proxy-->>Client: tools
    else Roots fallback, no upstream
        Proxy->>Cache: GetCachedTools()
        Cache-->>Proxy: cached Tool[]
        Proxy-->>Client: [set_roots] + cached tools
    else No upstream, no cache
        Proxy-->>Client: empty tools
    end
```

### Clients Without `list_changed` Support

The proxy has special handling for clients that don't support the `tools/list_changed` notification:

```csharp
// McpProxy.cs line 41
private static readonly string[] ClientsWithoutListUpdateSupport =
    ["claude-code", "codex", "codex-mcp-client"];
```

For these clients, the first `tools/list` call **blocks** until the upstream server is ready (line 559-567). This means the full 37s startup delay is experienced synchronously by the client.

This is the primary motivation for Phase 1a (serve cached tools immediately).

---

## 6. Proposed Workflow: Phase 0+1b (Fast Discovery + Direct Launch)

> **Maps to spec phases**: Phase 0 (convention-based `.targets` parsing, `--addins` flag) + Phase 1b (controller bypass, direct Host launch). This workflow shows the optimized path after both phases ship.

```mermaid
sequenceDiagram
    participant Client as MCP Client
    participant CLI as uno-devserver CLI
    participant Monitor as DevServerMonitor
    participant Locator as UnoToolsLocator
    participant Server as Host (Server)

    Note over CLI: T=0ms — Process start

    Client->>CLI: STDIO connection
    CLI->>CLI: McpProxy.RunAsync()
    CLI->>CLI: Start STDIO MCP server

    Note over CLI: T=~50ms

    CLI->>Locator: ResolveHostExecutableAsync(workDir)
    CLI->>Locator: ResolveAddInPathsAsync(workDir)

    Note over Locator: T=~100ms — Fast add-in resolution

    Locator->>Locator: Parse global.json → SDK version
    Locator->>Locator: Find SDK in NuGet cache
    Locator->>Locator: Read packages.json (~5ms)
    Locator->>Locator: Scan NuGet cache for add-in DLLs (~5ms)
    Locator->>Locator: dotnet --version (cached, ~5ms)
    Locator-->>CLI: hostPath + addInPaths

    Note over CLI: T=~200ms — All paths resolved

    CLI->>Monitor: StartMonitoring(directory, port, addInPaths)

    Note over Monitor: Skip controller, launch directly

    Monitor->>Server: Process.Start(--httpPort, --ppid, --addins)

    Note over Server: T=~1.7s — Server cold start

    Server->>Server: Parse --addins flag
    Server->>Server: AssemblyHelper.Load(preResolvedPaths)
    Server->>Server: AddFromAttributes(assemblies)
    Server->>Server: Kestrel start, /mcp mapped

    Note over Server: T=~3.5s — Server ready (no MSBuild!)

    Monitor->>Server: HTTP health check
    Server-->>Monitor: 200 OK

    CLI->>Server: McpClient.CreateAsync (StreamableHttp)
    Server-->>CLI: Connected
    CLI->>Server: ListToolsAsync()
    Server-->>CLI: 12 tools

    Note over CLI: T=~4.5s — Tools available

    CLI->>Client: tools/list_changed
    Client->>CLI: tools/list
    CLI-->>Client: 12 tools available
```

### Timing Comparison

```mermaid
gantt
    title Startup Timeline Comparison
    dateFormat X
    axisFormat %ss

    section Current (~37s)
    CLI cold start           :a1, 0, 1500
    SDK discovery            :a2, after a1, 2000
    Launch controller        :a3, after a2, 100
    Controller cold start    :a4, after a3, 1500
    Controller work          :a5, after a4, 400
    Launch server            :a6, after a5, 100
    Server cold start        :a7, after a6, 1500
    MSBuild TFM dump         :crit, a8, after a7, 10000
    MSBuild AddIns dump      :crit, a9, after a8, 10000
    Assembly loading         :a10, after a9, 500
    Kestrel + connect        :a11, after a10, 2000

    section Phase 0+1b (~5s)
    CLI cold start           :b1, 0, 1500
    SDK + addin resolution   :b2, after b1, 200
    Launch server directly   :b3, after b2, 100
    Server cold start        :b4, after b3, 1500
    Assembly loading         :b5, after b4, 500
    Kestrel + connect        :b6, after b5, 1000

    section Phase 1a+2 (<1s)
    CLI cold start (R2R)     :c1, 0, 200
    STDIO MCP + cached tools :done, c2, after c1, 50
    Background: discovery    :active, c3, after c1, 200
    Background: launch host  :active, c4, after c3, 3000
    Background: connect      :active, c5, after c4, 1000
```

---

## 7. Proposed Workflow: Phase 1a+2 (Instant MCP Start + R2R)

> **Maps to spec phases**: Phase 1a (instant MCP start, cached tools, structured errors) + Phase 2 (ReadyToRun compilation). This workflow shows the end-state where MCP tools are served from cache in < 1s.

```mermaid
sequenceDiagram
    participant Client as MCP Client
    participant CLI as uno-devserver CLI
    participant Cache as ToolCacheFile
    participant Health as HealthResource
    participant Monitor as DevServerMonitor
    participant Server as Host (Server)

    Note over CLI: T=0ms — ReadyToRun process start (~200ms)

    Client->>CLI: STDIO connection
    CLI->>CLI: Start STDIO MCP server immediately

    Note over CLI: T=~50ms — MCP server ready

    CLI->>Health: Status = Initializing
    Client->>CLI: tools/list

    alt Cached tools available
        CLI->>Cache: GetCachedTools()
        Cache-->>CLI: 12 cached tools
        CLI-->>Client: 12 tools (instant!)
    else No cache
        CLI-->>Client: [set_roots tool only]
    end

    Note over CLI: T=~100ms — Client has tools

    par Background initialization
        CLI->>Monitor: ResolveAddInPaths + StartMonitoring
        Monitor->>Server: Launch Host with --addins
        Server->>Server: Cold start + assembly loading
        Server->>Server: Kestrel start

        Note over Server: T=~3-4s — Server ready

        CLI->>Server: McpClient.CreateAsync
        Server-->>CLI: Connected
        CLI->>Server: ListToolsAsync()
        Server-->>CLI: 12 live tools
        CLI->>Health: Status = Healthy
        CLI->>Client: tools/list_changed
    end

    Note over Client: Client can immediately start<br/>planning with tool descriptions.<br/>Actual tool calls work after ~4s.

    Client->>CLI: call_tool(uno_app_get_screenshot)
    CLI->>Server: Forward to upstream
    Server-->>CLI: Screenshot result
    CLI-->>Client: Result
```

### Always-Start Pattern State Machine

```mermaid
stateDiagram-v2
    [*] --> Initializing: Process start

    Initializing --> Discovering: STDIO MCP server started
    note right of Initializing
        Serve cached tools
        Health: Initializing
    end note

    Discovering --> LaunchingHost: SDK + add-ins resolved
    Discovering --> Degraded: Resolution failed
    note right of Discovering
        Background: parse global.json,
        resolve NuGet paths
    end note

    LaunchingHost --> Connecting: Host process started
    LaunchingHost --> Degraded: Host launch failed
    note right of LaunchingHost
        Background: start Host with --addins
    end note

    Connecting --> Healthy: Upstream MCP connected
    Connecting --> Degraded: Connection failed
    note right of Connecting
        Background: HTTP connect to /mcp
        Send tools/list_changed on success
    end note

    Healthy --> Reconnecting: Host crashed
    Healthy --> [*]: Shutdown

    Reconnecting --> LaunchingHost: Auto-restart
    Reconnecting --> Degraded: Max retries exceeded
    note right of Reconnecting
        Health: HostCrashed issue
        Tool calls return error with hint
    end note

    Degraded --> [*]: Shutdown
    note right of Degraded
        Health: Issues[] populated
        Tool calls return diagnostic error
    end note
```

---

## 8. Data Flow: packages.json to Add-in DLLs

This diagram shows how the fast path resolves add-in DLL paths by parsing `.targets` files directly from NuGet packages.

```mermaid
flowchart TD
    A["global.json<br/>{msbuild-sdks: {Uno.Sdk: '6.5.100'}}"] --> B["NuGet Cache Lookup<br/>~/.nuget/packages/uno.sdk/6.5.100/"]

    B --> C["packages.json<br/>{sdkPath}/targets/netstandard2.0/packages.json"]

    C --> D["Parse JSON array:<br/>[{version: '6.5.100', packages: ['uno.winui.devserver',<br/>'uno.ui.app.mcp', 'uno.settings.devserver', ...]}]"]

    D --> E["For each package"]

    E --> F["Find in NuGet cache"]
    F --> G{"buildTransitive/*.targets<br/>exists?"}

    G -->|Yes| H["Parse .targets XML<br/>(~1ms per file)"]
    H --> I{"UnoRemoteControlAddIns<br/>item found?"}
    I -->|Yes| J["Resolve $(MSBuildThisFileDirectory)<br/>+ property references"]
    J --> K["Verify DLL exists on disk"]
    K --> L["Add to resolved set"]

    I -->|No| M{"tools/devserver/<br/>dir exists?"}
    G -->|No| M
    M -->|Yes| N["⚠️ Log diagnostic warning<br/>(DO NOT load DLLs)"]
    N --> O
    M -->|No| O[Skip — not an add-in]

    L --> P{More packages?}
    O --> P
    P -->|Yes| E

    P -->|No| Q["Resolved Add-in Paths<br/>['.../Uno.UI.App.Mcp.Server.dll',<br/>'.../Uno.Settings.DevServer.dll']"]

    Q --> R["Pass to Host via --addins flag"]

    R --> S["AssemblyHelper.Load(paths)<br/>→ AddFromAttributes(assemblies)"]

    style A fill:#74c0fc,stroke:#333
    style C fill:#74c0fc,stroke:#333
    style H fill:#51cf66,stroke:#333
    style J fill:#51cf66,stroke:#333
    style Q fill:#51cf66,stroke:#333
    style S fill:#51cf66,stroke:#333
```

### `.targets` Parsing Example

**Simple pattern** (`uno.settings.devserver`):
```xml
<!-- {cache}/uno.settings.devserver/1.2.3/buildTransitive/Uno.Settings.DevServer.targets -->
<ItemGroup>
  <UnoRemoteControlAddIns Include="$(MSBuildThisFileDirectory)../tools/devserver/Uno.Settings.DevServer.dll" />
</ItemGroup>
```
→ `$(MSBuildThisFileDirectory)` = `{cache}/uno.settings.devserver/1.2.3/buildTransitive/`
→ Resolved: `{cache}/uno.settings.devserver/1.2.3/tools/devserver/Uno.Settings.DevServer.dll`

**Property indirection** (`uno.ui.app.mcp`):
```xml
<!-- {cache}/uno.ui.app.mcp/6.5.100/buildTransitive/Uno.UI.App.Mcp.targets -->
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
→ Resolve `_UnoMcpServerProcessorPath` property first, then substitute in Include
→ `exists()` condition verified on disk
→ Resolved: `{cache}/uno.ui.app.mcp/6.5.100/tools/devserver/Uno.UI.App.Mcp.Server.dll`

### NuGet Cache Locations Checked

The `UnoToolsLocator.EnsureNugetPackage()` method already checks these locations (in order):

| Priority | Path | Platform |
|----------|------|----------|
| 1 | `%USERPROFILE%\.nuget\packages\{id}\{version}` | Windows |
| 1 | `~/.nuget/packages/{id}/{version}` | Linux/macOS |
| 2 | `%ProgramData%\NuGet\packages\{id}\{version}` | Windows global |
| 3 | `$NUGET_PACKAGES/{id}/{version}` | Custom env var |

### Add-in Identification Strategy

The system identifies add-in entry points using **`.targets` parsing only**:

1. **`.targets` parsing** (primary, produces loadable paths): Scan `buildTransitive/*.targets` for `UnoRemoteControlAddIns` items. Resolve `$(MSBuildThisFileDirectory)` and property references. The resolved path is the **specific entry point DLL** to load.

2. **Directory presence check** (diagnostic only, NO loading): Check if `{packageDir}/tools/devserver/` exists for packages not already resolved by step 1. Log a diagnostic warning — this package might be an add-in but its entry point couldn't be determined.

> **WARNING**: `tools/devserver/` contains the entry point DLL + ALL its dependencies (dozens of DLLs). Blindly loading all `*.dll` from this directory via `Assembly.LoadFrom()` would cause load errors, DI conflicts, and non-deterministic behavior. Only the specific DLL identified by `.targets` parsing should be loaded.

Both layers are self-discovering — no hardcoded package names. A new add-in package following the convention is found automatically.

---

## Appendix: Key Code References

### CLI Entry Point

**`CliManager.RunMcpProxyAsync()`** (`src/Uno.UI.DevServer.Cli/CliManager.cs:176-242`):
- Parses MCP-specific flags: `--port`, `--mcp-wait-tools-list`, `--force-roots-fallback`, `--force-generate-tool-cache`
- Delegates to `McpProxy.RunAsync()`

### MCP Proxy

**`McpProxy.RunAsync()`** (`src/Uno.UI.DevServer.Cli/Mcp/McpProxy.cs:53-90`):
- Entry point for MCP mode
- Calls `EnsureDevServerStartedFromSolutionDirectory()` → `StartDevServerMonitor()`
- Calls `StartMcpStdIoProxyAsync()` to start the STDIO server

**`McpProxy.StartMcpStdIoProxyAsync()`** (`McpProxy.cs:377-515`):
- Creates the STDIO MCP server via `Host.CreateApplicationBuilder().AddMcpServer().WithStdioServerTransport()`
- Registers `WithCallToolHandler` (forwards to upstream) and `WithListToolsHandler` (returns tools)
- Handles `tools/list_changed` notification from upstream via `McpClientProxy`

### DevServer Monitor

**`DevServerMonitor.RunMonitor()`** (`src/Uno.UI.DevServer.Cli/Mcp/DevServerMonitor.cs:44-146`):
- Scans for `.sln`/`.slnx` files in the working directory
- Resolves host executable via `UnoToolsLocator.ResolveHostExecutableAsync()`
- Allocates TCP port via `EnsureTcpPort()`
- Launches host with `--command start` via `StartProcess()`
- Polls `WaitForServerReadyAsync()` up to 30 attempts

### Upstream MCP Client

**`McpClientProxy.ConnectOrDieAsync()`** (`src/Uno.UI.DevServer.Cli/Mcp/McpClientProxy.cs:48-105`):
- Creates `HttpClientTransport` with `StreamableHttp` mode
- Connects to upstream at `http://localhost:{port}/mcp`
- Registers `ToolListChangedNotification` handler
- Calls `ListToolsAsync()` and triggers callback if tools found

### Host Add-ins

**`AddIns.Discover()`** (`src/Uno.UI.RemoteControl.Host/Extensibility/AddIns.cs:20-126`):
- Runs `dotnet build -t:UnoDumpTargetFrameworks` to get TFMs
- Runs `dotnet build -t:UnoDumpRemoteControlAddIns` per TFM to get DLL paths
- Returns `AddInsDiscoveryResult` with list of DLL paths

**`AddInsExtensions.ConfigureAddIns()`** (`src/Uno.UI.RemoteControl.Host/Extensibility/AddInsExtensions.cs:15-31`):
- Calls `AddIns.Discover(solutionFile)` — the bottleneck
- Loads assemblies via `AssemblyHelper.Load()`
- Registers services via `AddFromAttributes()`
- Stores `AddInsStatus` singleton

### Tool Cache

**`ToolCacheFile`** (`src/Uno.UI.DevServer.Cli/Mcp/ToolCacheFile.cs`):
- Persists tool definitions to `%LocalAppData%\Uno Platform\uno.devserver\tools-cache.json`
- Validates with SHA256 checksum and version number
- Used by `--force-roots-fallback` and `--force-generate-tool-cache` modes
- Phase 1a makes this cache the primary source for instant tool list responses
