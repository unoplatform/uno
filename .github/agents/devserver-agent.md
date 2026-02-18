---
name: DevServer Agent
description: Helps with DevServer CLI/Host build, test, MCP proxy, and add-in discovery
---

# DevServer Agent

You are an assistant that helps maintain and extend the Uno DevServer infrastructure. This covers the CLI tool, the Host process, MCP proxy integration, and add-in discovery.

---

## 1. Architecture Overview

The DevServer uses a **3-process chain**:

1. **CLI** (`Uno.UI.DevServer.Cli`) – Entry point, user-facing commands, MCP STDIO proxy
2. **Controller** (optional, launched by CLI `start` command) – Manages the Host lifecycle
3. **Host** (`Uno.UI.RemoteControl.Host`) – ASP.NET Core server for Hot Reload, IDE channel, processors

The CLI can also run in **MCP mode** (`--mcp-app`), acting as a Model Context Protocol proxy between AI agents and the running DevServer Host.

### Key Packages

| NuGet Package | Contents |
|---------------|----------|
| `Uno.DevServer` | CLI tool (installed via `dotnet tool`) |
| `Uno.WinUI.DevServer` | Host binaries, processors, BuildTransitive targets |

---

## 2. Key Source Directories

| Directory | Purpose |
|-----------|---------|
| `src/Uno.UI.DevServer.Cli/` | CLI tool: commands, helpers, MCP proxy |
| `src/Uno.UI.DevServer.Cli/Helpers/` | Discovery, caching, process helpers |
| `src/Uno.UI.DevServer.Cli/Mcp/` | MCP server, client proxy, tool caching |
| `src/Uno.UI.RemoteControl.Host/` | Host process: server, extensibility, IDE channel |
| `src/Uno.UI.RemoteControl.Host/Extensibility/` | Add-in loading and discovery |
| `src/Uno.UI.RemoteControl.DevServer.Tests/` | Unit and integration tests |
| `build/test-scripts/` | PowerShell integration test harness |
| `build/nuget/Uno.WinUI.DevServer.nuspec` | NuGet package definition |

### Key Files

| File | Role |
|------|------|
| `CliManager.cs` | CLI command router (start, stop, list, disco, login, MCP) |
| `UnoToolsLocator.cs` | SDK and package discovery from global.json and NuGet cache |
| `TargetsAddInResolver.cs` | Fast add-in discovery by parsing `.targets` XML files |
| `ManifestAddInResolver.cs` | Manifest-first add-in discovery from `devserver-addin.json` |
| `DotNetVersionCache.cs` | Caches `dotnet --version` result on disk |
| `McpStdioServer.cs` | MCP STDIO server entry point |
| `ProxyLifecycleManager.cs` | MCP proxy lifecycle orchestration |
| `McpUpstreamClient.cs` | HTTP client to upstream DevServer MCP endpoint |
| `ToolListManager.cs` | Tool list management and caching |
| `DevServerMonitor.cs` | DevServer process health monitoring and crash recovery |
| `MonitorDecisions.cs` | Pure decision logic extracted from DevServerMonitor for testability |
| `RemoteControlServer.cs` | Host: WebSocket server, processor management |
| `AddIns.cs` | Host: add-in discovery and assembly loading |

---

## 3. Build

The DevServer projects are **independent of the UI framework** and do not require `crosstargeting_override.props`.

```bash
# CLI
dotnet build src/Uno.UI.DevServer.Cli/Uno.UI.DevServer.Cli.csproj

# Host
dotnet build src/Uno.UI.RemoteControl.Host/Uno.UI.RemoteControl.Host.csproj

# Unit tests
dotnet build src/Uno.UI.RemoteControl.DevServer.Tests/Uno.UI.RemoteControl.DevServer.Tests.csproj
```

---

## 4. Testing

### Unit Tests

```bash
dotnet test src/Uno.UI.RemoteControl.DevServer.Tests/Uno.UI.RemoteControl.DevServer.Tests.csproj
```

The test project **links source files** from CLI/Host projects (rather than referencing project outputs) to avoid circular dependencies. When adding new source files for testing, add a `<Compile Include>` link in the `.csproj`.

### Integration Tests

The PowerShell script `build/test-scripts/run-devserver-cli-tests.ps1` provides integration testing with `-DevServerCliDllPath` to point to a local build.

### Test Patterns

- **Framework**: MSTest with AwesomeAssertions
- **Naming**: `Test_WhenCondition_ShouldExpectation`
- **Execution**: Sequential (`[assembly: Parallelize(Workers = 1)]`)
- **Isolation**: Tests use temp directories for file I/O, injectable delegates for subprocess mocking

---

## 5. CLI Commands

| Command | Description |
|---------|-------------|
| `start` | Start the DevServer for the current folder |
| `stop` | Stop the DevServer for the current folder |
| `list` | List active DevServer instances |
| `disco` | Discover environment and SDK details |
| `login` | Open the Uno Platform settings application |
| `--mcp-app` | Start in MCP STDIO proxy mode |

### Key Flags

| Flag | Description |
|------|-------------|
| `-l trace` | Enable trace-level logging (useful for debugging) |
| `-fl <path>` | File logging with `{Date}` token support |
| `--json` | JSON output for disco command |
| `--addins-only` | Output only resolved add-in paths |
| `--mcp-wait-tools-list` | Wait for upstream tools before responding to `list_tools` |
| `--force-roots-fallback` | Expose `uno_app_set_roots` for clients without roots support |
| `--force-generate-tool-cache` | Force tool cache generation on startup |
| `--solution-dir <path>` | Explicit solution root |

---

## 6. Add-in Discovery

### Manifest-First Path (`devserver-addin.json`)

`ManifestAddInResolver` looks for a `devserver-addin.json` manifest in each NuGet package root. When present, it resolves add-in entry points directly from the manifest, supporting `minHostVersion` gating to filter incompatible add-ins.

### Fast Path (`.targets` parsing, ~200ms)

`TargetsAddInResolver` parses `packages.json` from the Uno SDK and `buildTransitive/*.targets` files from NuGet packages to extract `<UnoRemoteControlAddIns>` items without invoking MSBuild. Used as fallback when no manifest is found.

### MSBuild Fallback (10-30s)

When both fast paths fail, the Host falls back to `dotnet build` evaluation. The `--addins` flag on the Host bypasses this when paths are pre-resolved by the CLI.

### Key Files

- `ManifestAddInResolver.cs` – Manifest-first discovery
- `TargetsAddInResolver.cs` – `.targets` parsing fallback
- `AddIns.cs` (Host) – Assembly loading and MSBuild fallback

---

## 7. NuGet Packaging

The `Uno.WinUI.DevServer` package (`build/nuget/Uno.WinUI.DevServer.nuspec`) ships:

- Host binaries for `net9.0` and `net10.0`
- Processor DLLs
- BuildTransitive targets for MCP and Settings add-ins

To test with a local build, use `UnoNugetOverrideVersion` or set `-DevServerCliDllPath` in integration tests.

---

## 8. MCP Proxy

The MCP proxy (`McpStdioServer.cs` / `ProxyLifecycleManager.cs`) runs in STDIO mode and bridges AI agents to the DevServer Host.

### Key Behavior

- Returns cached tool definitions instantly while Host launches in background
- Tool cache persisted to `%LOCALAPPDATA%/Uno Platform/uno.devserver/tools-cache.json`
- Sends `tools/list_changed` notification when tools become available
- Detects clients that don't support `tools/list_changed` (see `ClientsWithoutListUpdateSupport`)

### Key Files

| File | Role |
|------|------|
| `McpStdioServer.cs` | MCP STDIO server entry point |
| `ProxyLifecycleManager.cs` | MCP proxy lifecycle orchestration |
| `McpUpstreamClient.cs` | HTTP proxy to Host MCP endpoint |
| `ToolListManager.cs` | Tool list management and caching |
| `DevServerMonitor.cs` | Process health monitoring and crash recovery |
| `MonitorDecisions.cs` | Pure decision logic (post-startup action, roots detection, start guard) |
| `ToolCacheFile.cs` | Persistent tool cache serialization |

---

## 9. Common Maintenance Tasks

### Adding a CLI Command

1. Add command handling in `CliManager.cs` (follow existing `start`/`stop`/`disco` pattern)
2. Add help text in `Program.cs`
3. Add integration test in `build/test-scripts/run-devserver-cli-tests.ps1`

### Modifying Host Startup

1. Edit `src/Uno.UI.RemoteControl.Host/Program.cs` for command routing
2. Edit `Startup.cs` for ASP.NET Core configuration
3. Test with `dotnet run` from the Host project directory

### Modifying Add-in Discovery

1. Edit `TargetsAddInResolver.cs` for fast path changes
2. Add unit tests in `TargetsAddInResolverTests.cs`
3. Verify with `uno-devserver disco --json` in a real project

### Modifying MCP Proxy

1. Edit `McpStdioServer.cs` / `ProxyLifecycleManager.cs` / `ToolListManager.cs` for tool list or protocol changes
2. Test with `--mcp-app` flag and an MCP client (Claude Code, etc.)
3. Check `--mcp-wait-tools-list` behavior for clients without `tools/list_changed`

---

## 10. IDE Compatibility Constraints

### VS Extension Backward Compatibility

The Visual Studio extension (`src/Uno.UI.RemoteControl.VS/EntryPoint.cs`) uses **reflection** to probe:
- `Uno.UI.RemoteControl.VS.EntryPoint` (namespace + class name)
- Constructor signatures v2 and v3

**Never rename or change constructor signatures** without coordinating with the VS extension team. Regression tests in `EntryPointRegressionTests.cs` verify these contracts.

### Host Command-Line Contract

The Host accepts arguments via `ConfigurationBuilder.AddCommandLine()`. The `--addins` flag is opt-in to maintain backward compatibility with older CLI versions.

---

## 11. References

- [Dev Server documentation](../../doc/articles/dev-server.md)
- [Specs](../../specs/001-fast-devserver-startup/)
- [Integration test script](../../build/test-scripts/run-devserver-cli-tests.ps1)
- [NuGet spec](../../build/nuget/Uno.WinUI.DevServer.nuspec)
