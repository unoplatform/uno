# Verification Plan & Test Strategy

> **Parent**: [Main Spec](spec.md) — Section 9
> **Related**: [Add-In Discovery](spec-appendix-b-addin-discovery.md) | [Startup Workflow](spec-appendix-a-startup-workflow.md)

---

## 1. Performance Targets

| Metric | Current | Target (Phase 1) | Target (Phase 2) |
|--------|---------|-------------------|-------------------|
| Time to first `list_tools` | 15-40s | < 1s (cached) | < 500ms |
| Time to functional tools | 15-40s | < 5s (warm cache) | < 3s |
| CLI cold start | ~1.5s | ~1.5s | ~200ms |

---

## 2. Test Scenarios

| # | Scenario | Validation |
|---|----------|-----------|
| 1 | Normal startup (warm cache) | `list_tools` < 1s, licensed tools functional < 5s |
| 2 | First-ever startup (no cache) | `list_tools` returns cached tools, functional tools < 10s |
| 3 | Missing global.json | MCP starts, `uno://health` shows `GlobalJsonNotFound`, remediation hint |
| 4 | Broken solution | Fast-path add-in resolution succeeds (no MSBuild needed) |
| 5 | Uno SDK 5.x project | Fallback chain resolves add-ins correctly |
| 6 | Uno SDK 6.5 project | Fast-path resolution works |
| 7 | Host crash mid-session | Auto-restart, `tools/list_changed`, tool calls recover |
| 8 | Host crash 3x | Give up, `uno://health` shows `HostCrashed`, remediation |
| 9 | Missing NuGet cache | Structured error with `dotnet restore` remediation |
| 10 | `--force-roots-fallback` | `uno_app_set_roots` workflow unchanged |
| 11 | `--force-generate-tool-cache` | Cache primed and process exits |
| 12 | Non-MCP commands | `start`, `stop`, `list`, `cleanup`, `disco`, `login` unchanged |
| 13 | VS extension launches Host | No `--addins` flag -> MSBuild discovery works |
| 14 | Tool call before host ready | Structured error, not hang or crash |
| 15 | `dotnet --version` cache | Correct TFM even after .NET SDK update |
| 16 | **Upstream returns 0 tools** (no license, add-in load failure, or registration error — not a valid license tier) | `list_tools` returns within 30s (not indefinitely), empty list or cached tools + health warning |
| 17 | **Upstream connection fails** | `list_tools` returns within 30s with cached tools or error tool |
| 18 | **Community license** | `list_tools` returns ~9 tools (not 12), cache reflects license tier |
| 19 | **`--addins` with `--solution`** | `--addins` wins for add-in loading, `--solution` used for Hot Reload |
| 20 | **`--addins` with missing DLL** | Warning logged, remaining add-ins still loaded |
| 21 | **Duplicate DevServer for same solution** | CLI detects via AmbientRegistry, reuses or fails explicitly |
| 22 | **Package has `tools/devserver/` but no `.targets`** | Diagnostic warning in health, DLLs NOT loaded blindly |
| 23 | **`.targets` has complex conditions** (e.g., `$(Configuration)`) | Ignored gracefully, production path still resolved |
| 24 | **Windows path separators** | `\` handled correctly in `.targets` resolution on Windows |
| 25 | **Linux/macOS NuGet cache location** | `~/.nuget/packages/` resolved correctly |
| 26 | **Custom `$NUGET_PACKAGES` env var** | Custom NuGet cache location used |
| 27 | **`devserver-addin.json` with version > 1** | Warning logged, falls through to `.targets` |
| 28 | **Mixed discovery: manifest + .targets** | Each package resolved by its best available method |
| 29 | **VS extension launcher reflection compatibility** | `Uno.UI.RemoteControl.VS.EntryPoint` class (loaded via `Assembly.LoadFrom` in `DevServerLauncher.cs:301`) must have v3 constructor `(DTE2,string,AsyncPackage,string)`. VS probes v3 → v2 → v1 via `Activator.CreateInstance` (`DevServerLauncher.cs:313-331`). Test: verify constructor signatures are present. |
| 30 | **Rider auto-restart race condition** | When MCP mode relaunches Host (hot reconnection), Rider's auto-restart (immediate on process exit) should not create a competing instance. Test: kill Host while both Rider and MCP are connected → verify AmbientRegistry prevents duplicate, only one Host survives. |
| 31 | **Health check IPv6 loopback** | MCP mode health polling must check `[::1]` (IPv6) in addition to `localhost` and `127.0.0.1`. Test: Host bound to IPv6 only → health check succeeds. |
| 32 | **Controller `--addins` forwarding** | Controller accepts `--addins` parameter and includes it in child server process argument list. Test: run `Host.dll --command start --addins "p1;p2" --httpPort 0 --solution s.sln` → verify child process receives `--addins "p1;p2"`. |
| 33 | **`disco --json` includes add-in paths** | `disco --json` output contains `addIns` array with resolved add-in DLL paths, discovery method, and duration |
| 34 | **`disco --addins-only --json`** | Returns JSON array of absolute DLL paths only. Output is parseable and paths exist on disk. |
| 35 | **`disco --addins-only` (text)** | Returns semicolon-separated DLL paths. Output can be piped as `--addins` value. |
| 36 | **`disco --json` backward compat** | Existing fields (`globalJsonPath`, `unoSdkPackage`, etc.) still present and correct. New fields are additive. |
| 37 | **NuGet cache unavailable** | Uno SDK directory missing from all cache locations → structured error with remediation hint ("Run `dotnet restore`") and list of checked paths. |
| 38 | **Partial NuGet restore** | Package directory exists but DLL file missing → `AddInBinaryNotFound` error at DLL-level validation (not silently passed). |
| 39 | **Custom `$NUGET_PACKAGES` missing** | Env var points to non-existent path → error includes the custom path in checked locations. |
| 40 | **Health check IPv6-only environment** | Host bound to `[::1]` only → health check succeeds via IPv6 loopback endpoint. |
| 41 | **`build/` fallback for `.targets`** | Package has `.targets` in `build/` but NOT in `buildTransitive/` → `.targets` resolved from `build/` directory, add-in paths extracted correctly. |
| 42 | **`$NUGET_PACKAGES` set to empty string** | `NUGET_PACKAGES=""` → env var path skipped (not treated as relative path). Only UserProfile and CommonApplicationData paths checked. |
| 43 | **`--metadata-updates` forwarding in direct launch** | Phase 1b direct Host launch with `--metadata-updates true` → `ServerHotReloadProcessor` activates metadata delta generation. |
| 44 | **`cleanup` command non-regression** | `--command cleanup` → stale DevServer registrations removed, active ones preserved. Exit code 0. |
| 45 | **MCP capability detection from `initialize`** | Client sends `initialize` with `capabilities.roots` → server detects roots support via `Roots != null` (not `Roots.ListChanged`). Client without `roots` capability → server falls back to `--force-roots-fallback` behavior. |
| 46 | **`tools/list_changed` notification deserialization** | Upstream sends `tools/list_changed` → `McpClientProxy` processes it without deserialization error. Callback fires, `list_tools` refresh triggered. |
| 47 | **`McpClientProxy` dispose without connection** | Host never starts → `McpClientProxy.DisposeAsync()` completes within 5s (does not block indefinitely). |
| 48 | **`list_tools` timeout for waiting clients** | Client without `tools/list_changed` capability (detected from `initialize`) or `--mcp-wait-tools-list` flag → `list_tools` returns within 30s even if upstream never responds (not indefinitely). Returns cached tools or empty list. No hardcoded client name list used. |

---

## 3. Performance Measurement Methodology

Performance targets must be measured reproducibly:

1. **Environment**: Document OS, .NET SDK version, machine specs (CPU, disk type), NuGet cache state
2. **Cold start**: Kill all `dotnet` processes, clear OS file cache (`sync && echo 3 > /proc/sys/vm/drop_caches` on Linux; reboot on Windows), measure from process start to first `list_tools` response
3. **Warm start**: Normal restart with NuGet cache warm and OS file cache populated
4. **Repetitions**: Minimum 5 runs, report median and p95
5. **Instrumentation**: Use existing telemetry points (`addin-discovery-start`/`complete` in `AddIns.cs:24,144`) + new CLI-side timing for `.targets` parsing, host launch, upstream connection
6. **Baseline**: Capture current timings BEFORE changes, as non-regression reference
7. **Automated**: Add a benchmark script to `specs/001-fast-devserver-startup/` that can be run in CI

---

## 4. Test Strategy (Critical)

Testing is a first-class deliverable for this spec, not an afterthought. **Each phase MUST ship with its tests before being considered complete.**

### Unit Tests (per component)

| Component | Test Focus | Phase |
|-----------|-----------|:-----:|
| `TargetsAddInResolver` | Parse known `.targets` patterns, property resolution, `exists()` conditions, malformed XML, missing files, **`build/` fallback when `buildTransitive/` empty** | 0 |
| `ManifestAddInResolver` | Parse `devserver-addin.json`, schema version handling, missing manifest, invalid JSON | 1 |
| `DotNetVersionCache` | Cache hit, cache miss, invalidation on global.json change, stale cache (>24h) | 0 |
| `ToolListManager` | Timeout handling, 0-tool case, cache refresh, TCS lifecycle | 1a |
| `HealthService` | Report aggregation, status transitions, issue collection | 1a |
| `ProxyLifecycleManager` | State machine transitions, all 8 states, invalid transitions rejected | 1c |
| `--addins` flag parser | Semicolon splitting, empty entries, whitespace, missing DLLs, empty string | 0 |
| `EntryPoint` launcher API surface (VS compat) | Verify both type names exist: `Uno.UI.DevServer.VS.EntryPoint` (current) and `Uno.UI.RemoteControl.VS.EntryPoint` (legacy). Verify v1/v2/v3 constructor signatures matching VS extension's reflection targets (`DevServerLauncher.cs:302-326`): v3 `(DTE2,string,AsyncPackage,string)`, v2 `(DTE2,string,AsyncPackage,Action<Func<Task<Dictionary<string,string>>>>,Func<Task>)`, v1 `(DTE2,string,AsyncPackage,Action<Func<Task<Dictionary<string,string>>>>)`. Lock with snapshot test. | 0 |
| Controller `--addins` forwarding | Verify controller accepts `--addins` parameter and passes it to child server process argument list. Test: launch controller with `--addins "path1;path2"` → child process receives `--addins "path1;path2"`. | 0 |
| NuGet cache path guard | `$NUGET_PACKAGES=""` (empty string) → path skipped, not treated as relative. `$NUGET_PACKAGES` unset → `null` coalesced to `""` → also skipped. Only non-empty paths checked. | 0 |
| MCP capability parser | Parse `initialize` request `ClientCapabilities`: `Roots != null` → roots supported. `Roots == null` → roots unsupported. `Roots.ListChanged == true` → roots list-changed supported (separate from roots support). | 1a |
| Host flag forwarding (Phase 1b) | Direct Host launch passes through `--metadata-updates`, `--ideChannel`, `--solution`, `--httpPort`, `--ppid`. Unknown flags silently accepted by `ConfigurationBuilder.AddCommandLine()`. | 1b |

### Compatibility Tests (Forward + Backward)

These test the discovery system against **real package layouts** from different Uno SDK versions:

| Test | Description | Expected |
|------|-------------|----------|
| **Current SDK (6.5+)** | packages.json + both add-in packages in NuGet cache | Both add-ins resolved via `.targets` |
| **SDK 6.x** | Same layout, different versions | Same result |
| **SDK 5.x** | No packages.json or different layout | Fallback to MSBuild or cached paths |
| **Future SDK** | `devserver-addin.json` manifest present | Manifest takes priority over `.targets` |
| **Mixed** | One package with manifest, one with `.targets` only | Each resolved by its best method |
| **New unknown add-in** | Package with `.targets` + `UnoRemoteControlAddIns` never seen before | Discovered automatically |
| **Third-party add-in (not in Uno SDK)** | Add-in package referenced directly by project, not listed in Uno SDK `packages.json`. Present in `project.assets.json` only. | Discovered via `project.assets.json` scan, resolved via `.targets` parsing |
| **Third-party add-in without restore** | Same as above but `project.assets.json` missing (restore not run) | Warning "restore required for third-party add-in discovery". SDK add-ins still resolved via `packages.json`. |
| **Package with `tools/devserver/` but no `.targets` or manifest** | Unknown add-in | Warning only, no DLL loading |
| **`build/` fallback** | Package has `.targets` in `build/` but NOT in `buildTransitive/` | `.targets` resolved from `build/` directory (see appendix B, section 2 step 2b) |

**Test fixtures**: Create synthetic NuGet package layouts (directory structures) for each scenario. Each fixture contains the minimum files needed: `buildTransitive/*.targets`, `tools/devserver/*.dll` (empty marker files), `devserver-addin.json` (where applicable).

**Test project**: Unit tests can go in the existing `src/Uno.UI.RemoteControl.DevServer.Tests/` project or in a new `src/Uno.UI.DevServer.Cli.Tests/` project (implementer's choice — see spec section 11b). Integration tests should be added to the existing CI script `build/test-scripts/run-devserver-cli-tests.ps1` which already runs on Windows, Linux, and macOS.

### Cross-Platform Tests

| OS | Concern | Test |
|----|---------|------|
| Windows | Backslash path separators, long paths, `%USERPROFILE%` | `.targets` resolution produces valid Windows paths |
| macOS | Case-sensitive filesystem option | Package name case matching in NuGet cache |
| Linux | Case-sensitive filesystem (always) | Package name case matching, `~/.nuget/packages/` resolution |
| All | `$NUGET_PACKAGES` env var | Custom cache location respected |
| All | Multiple NuGet cache fallback locations | All locations checked in order |

### Integration Tests

| Test | Description |
|------|-------------|
| CLI MCP startup (warm cache) | Measure time from process start to `list_tools` response |
| CLI MCP startup (cold, no cache) | Verify fallback behavior, timing |
| CLI `start` with `--addins` | Host receives pre-resolved paths, skips MSBuild |
| CLI `disco --json` | JSON output contains correct paths |
| Host `--addins` flag | Verify add-ins loaded, `AddInsStatus` correct |
| Host without `--addins` | MSBuild discovery unchanged (regression test) |
| Hot reconnection | Kill host, verify auto-restart, tool recovery |
| Hot reconnection + external restart race | Kill host while external launcher (simulated Rider) also restarts → verify AmbientRegistry prevents duplicate instances |

### Non-Regression Tests

Every phase captures a **non-regression baseline**:
1. Run the full test suite BEFORE the phase's changes
2. Run the full test suite AFTER
3. No existing tests may break (unless explicitly obsoleted)
4. Performance baselines captured before Phase 0 (see measurement methodology)

---

## 5. Backward Compatibility Matrix

| Consumer | Launch Method | `--addins` | Expected Behavior |
|----------|-------------|:-:|---|
| CLI MCP mode | Via controller (Phase 0); direct launch (Phase 1b) | Yes (Phase 0) | Fast path, skip MSBuild |
| CLI `start` command | Via controller | Yes (Phase 0) | Fast path, skip MSBuild |
| VS extension | Direct launch via `DevServerLauncher` | No (future) | MSBuild discovery (unchanged) |
| VS Code extension | `dotnet Host.dll --httpPort ...` | No (future) | MSBuild discovery (unchanged) |
| Rider extension | `dotnet Host.dll --httpPort ...` | No (future) | MSBuild discovery (unchanged) |

**Forward compatibility**: New add-in packages that follow the convention (see add-in discovery doc) are discovered automatically — no code changes required in the discovery system.

**Backward compatibility**: Older SDK versions (5.x) that predate the `buildTransitive/` convention fall through to the MSBuild fallback. The fallback is transparent and logged.
