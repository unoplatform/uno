---
name: devserver
description: Build, test, and maintain the Uno DevServer (CLI tool, RemoteControl Host, MCP proxy, add-in discovery). Use when working under src/Uno.UI.DevServer.Cli, src/Uno.UI.RemoteControl.Host, or src/Uno.UI.RemoteControl.DevServer.Tests, or on DevServer CLI commands, the MCP proxy, IDE channel, or add-in resolution.
---

# DevServer (CLI / Host / MCP)

Maintain and extend the Uno DevServer: the CLI tool, the Host process, MCP proxy integration, and add-in discovery. The DevServer projects are **independent of the UI framework** and do **not** require `crosstargeting_override.props`.

## 1. Architecture — a 2-process chain

1. **CLI** (`Uno.UI.DevServer.Cli`) – entry point, user-facing commands, MCP STDIO proxy, start-lifecycle management.
2. **Host** (`Uno.UI.RemoteControl.Host`) – ASP.NET Core server for Hot Reload, IDE channel, processors.

The CLI `start` command handles existing-server detection (AmbientRegistry), IDE channel rebinding, port allocation, and launches the Host in **direct mode** (no `--command`), so all arguments (including `--ideChannel`) reach the Host via `IConfiguration` regardless of Host binary version. Non-start commands (`stop`, `list`, `cleanup`) use the Host's controller mode (`--command=<verb>`). The CLI can also run in **MCP mode** (`--mcp-app`) as a Model Context Protocol proxy between AI agents and the running Host.

### Packages
| NuGet Package | Contents |
|---------------|----------|
| `Uno.DevServer` | CLI tool (installed via `dotnet tool`) |
| `Uno.WinUI.DevServer` | Host binaries, processors, BuildTransitive targets |

## 2. Source map
| Directory | Purpose |
|-----------|---------|
| `src/Uno.UI.DevServer.Cli/` | CLI tool: commands, helpers, MCP proxy |
| `src/Uno.UI.DevServer.Cli/Helpers/` | Discovery, caching, process helpers |
| `src/Uno.UI.DevServer.Cli/Mcp/` | MCP server, client proxy |
| `src/Uno.UI.RemoteControl.Host/` | Host process: server, extensibility, IDE channel |
| `src/Uno.UI.RemoteControl.Host/Extensibility/` | Add-in loading and discovery |
| `src/Uno.UI.RemoteControl.DevServer.Tests/` | Unit and integration tests |
| `build/test-scripts/` | PowerShell integration test harness |
| `build/nuget/Uno.WinUI.DevServer.nuspec` | NuGet package definition |

Key files: `CliManager.cs` (command router), `UnoToolsLocator.cs` (SDK/package discovery from global.json, NuGet cache, AmbientRegistry), `TargetsAddInResolver.cs` (fast add-in discovery via `.targets` XML), `ManifestAddInResolver.cs` (manifest-first discovery from `devserver-addin.json`), `DotNetVersionCache.cs`, `McpStdioServer.cs`, `ProxyLifecycleManager.cs`, `McpUpstreamClient.cs`, `ToolListManager.cs`, `DevServerMonitor.cs` (+ `MonitorDecisions.cs`, pure decision logic for testability), `HealthService.cs`/`HealthReport.cs`, `DiscoveryIssueMapper.cs`, `ConnectionState.cs`, `SolutionFileFinder.cs`; Host: `RemoteControlServer.cs` (WebSocket server, processors), `AddIns.cs` (discovery + assembly loading).

## 3. Build & test
```bash
dotnet build src/Uno.UI.DevServer.Cli/Uno.UI.DevServer.Cli.csproj
dotnet build src/Uno.UI.RemoteControl.Host/Uno.UI.RemoteControl.Host.csproj
dotnet test  src/Uno.UI.RemoteControl.DevServer.Tests/Uno.UI.RemoteControl.DevServer.Tests.csproj
```
The test project **links source files** from CLI/Host (rather than referencing outputs) to avoid circular dependencies — when adding a new source file for testing, add a `<Compile Include>` link in the `.csproj`. Integration tests: `build/test-scripts/run-devserver-cli-tests.ps1` with `-DevServerCliDllPath`. Test conventions: MSTest + AwesomeAssertions, naming `Test_WhenCondition_ShouldExpectation`, sequential (`[assembly: Parallelize(Workers = 1)]`), temp dirs + injectable delegates for subprocess mocking.

## 4. CLI commands & flags
Commands: `start`, `stop`, `list`, `disco` (discover env/SDK/active server), `login` (open Uno settings app), `--mcp-app` (MCP STDIO proxy mode).
Flags: `-l trace`, `-fl <path>` (file logging, `{Date}` token), `--json` (disco output), `--addins-only`, `--mcp-wait-tools-list`, `--force-roots-fallback` (legacy override; auto-detected now), `--solution-dir <path>`.

## 5. Add-in discovery — three paths
1. **Manifest-first** (`devserver-addin.json`): `ManifestAddInResolver` resolves entry points directly, with `minHostVersion` gating.
2. **Fast `.targets` parse (~200ms)**: `TargetsAddInResolver` parses `packages.json` + `buildTransitive/*.targets` for `<UnoRemoteControlAddIns>` without invoking MSBuild.
3. **MSBuild fallback (10-30s)**: Host runs `dotnet build` evaluation; `--addins` (opt-in) bypasses this when the CLI pre-resolves paths.

## 6. MCP proxy
`McpStdioServer.cs` / `ProxyLifecycleManager.cs` bridge AI agents to the Host over STDIO: returns tool defs while the Host launches in the background, sends `tools/list_changed` when tools are ready, detects client `roots` support.

**MCP roots — two modes:** some clients support roots (Claude Code, VS Code Copilot, Cursor); others don't (Windsurf, JetBrains, Gemini CLI, Claude Desktop).
- **Default**: DevServer starts immediately using `--solution-dir`/cwd; requests roots if supported; auto-enables roots fallback (agent calls `uno_app_initialize`) when unresolved + no roots support.
- **`--force-roots-fallback`**: defers startup until `uno_app_initialize`. Rarely needed.
`StartOnceGuard` (`MonitorDecisions.cs`) prevents duplicate starts when roots arrive after an immediate start.

**Solution discovery** (`SolutionFileFinder`): recursive `.sln`/`.slnx` search up to **3 levels deep**, respects `.gitignore` (via `git check-ignore`), falls back to a skip list (`node_modules`, `bin`, `obj`, `.vs`, `.idea`, `packages`) when git is unavailable.

**ConnectionState lifecycle** (`Mcp/ConnectionState.cs`):
```
Initializing → Discovering → Launching → Connecting → Connected
                                                ↓ (crash)
                                          Reconnecting → Discovering (cycle)
                                                ↓ (max retries)
                                             Degraded
```
Two retry counters: DevServerMonitor (3 startup attempts) and ProxyLifecycleManager (3 crash→restart cycles).

## 7. Common maintenance tasks
- **Add a CLI command**: handle in `CliManager.cs` (follow `start`/`stop`/`disco`), add help in `Program.cs`, add an integration test in `run-devserver-cli-tests.ps1`.
- **Modify Host startup**: `Program.cs` (routing), `Startup.cs` (ASP.NET config); test via `dotnet run` from the Host dir.
- **Modify add-in discovery**: `TargetsAddInResolver.cs` (+ `TargetsAddInResolverTests.cs`); verify with `uno-devserver disco --json` in a real project.
- **Modify MCP proxy**: `McpStdioServer.cs`/`ProxyLifecycleManager.cs`/`ToolListManager.cs`; test with `--mcp-app` + an MCP client; check `--mcp-wait-tools-list` for clients without `tools/list_changed`.

## 8. IDE compatibility constraints
All launchers use **direct Host launch**; the controller path (`--command start`) is only for non-start commands. The CLI `start` (`StartCommandHandler`) manages the start lifecycle itself. Consequences:
- AmbientRegistry duplicate protection exists for CLI-launched instances, **not** IDE-launched; multiple instances per solution are possible (IDE + CLI, or IDE + MCP).
- `--addins` MUST stay opt-in: absence = MSBuild discovery unchanged.
- `disco --json` returns an `activeServer` field (null, or `processId`/`port`/`mcpEndpoint`/`parentProcessId`/`startTime`) from AmbientRegistry — the recommended way for extensions to detect a running server.

**Visual Studio** (`uno.studio`): probes `Uno.UI.RemoteControl.VS.EntryPoint` and constructor signatures v2/v3 via **reflection** — **never rename or change those constructor signatures** without coordinating with the VS extension team; `EntryPointRegressionTests.cs` guards the contract. Launch resolves `tools/rc/host/net{major}.0/Uno.UI.RemoteControl.Host.dll` and launches directly with `--httpPort/--ppid/--solution`.
**Rider** (`uno.rider`): inspects project references for `uno.winui.devserver`, derives host path from `dotnet --version`; fallback `dotnet build /t:GetRemoteControlHostPath`.
**VS Code** (`uno.vscode`): delegates to the CLI — `dotnet dnx uno.devserver start --httpPort {port} --ideChannel {guid} --solution {sln}`; supports external-host debug via `.uno.vscode.remote-control` marker.
**Host CLI contract**: args via `ConfigurationBuilder.AddCommandLine()`; `--addins` opt-in for backward compatibility.

## 9. References
- `doc/articles/dev-server.md`
- `src/Uno.UI.DevServer.Cli/health-diagnostics.md`, `src/Uno.UI.DevServer.Cli/addin-discovery.md`
- `specs/001-fast-devserver-startup/`
- `build/test-scripts/run-devserver-cli-tests.ps1`, `build/nuget/Uno.WinUI.DevServer.nuspec`
