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
| 12 | Non-MCP commands | `start`, `stop`, `list`, `disco`, `login` unchanged |
| 13 | VS extension launches Host | No `--addins` flag -> MSBuild discovery works |
| 14 | Tool call before host ready | Structured error, not hang or crash |
| 15 | `dotnet --version` cache | Correct TFM even after .NET SDK update |
| 16 | **Upstream returns 0 tools** (no license) | `list_tools` returns within 30s (not indefinitely), empty list or cached tools + health warning |
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

---

## 3. Performance Measurement Methodology

Performance targets must be measured reproducibly:

1. **Environment**: Document OS, .NET SDK version, machine specs (CPU, disk type), NuGet cache state
2. **Cold start**: Kill all `dotnet` processes, clear OS file cache (`sync && echo 3 > /proc/sys/vm/drop_caches` on Linux; reboot on Windows), measure from process start to first `list_tools` response
3. **Warm start**: Normal restart with NuGet cache warm and OS file cache populated
4. **Repetitions**: Minimum 5 runs, report median and p95
5. **Instrumentation**: Use existing telemetry points (`addin-discovery-start`/`complete` in `AddIns.cs:24,128`) + new CLI-side timing for `.targets` parsing, host launch, upstream connection
6. **Baseline**: Capture current timings BEFORE changes, as non-regression reference
7. **Automated**: Add a benchmark script to `specs/001-fast-devserver-startup/` that can be run in CI

---

## 4. Test Strategy (Critical)

Testing is a first-class deliverable for this spec, not an afterthought. **Each phase MUST ship with its tests before being considered complete.**

### Unit Tests (per component)

| Component | Test Focus | Phase |
|-----------|-----------|:-----:|
| `TargetsAddInResolver` | Parse known `.targets` patterns, property resolution, `exists()` conditions, malformed XML, missing files | 0 |
| `ManifestAddInResolver` | Parse `devserver-addin.json`, schema version handling, missing manifest, invalid JSON | 1 |
| `DotNetVersionCache` | Cache hit, cache miss, invalidation on global.json change, stale cache (>24h) | 0 |
| `ToolListManager` | Timeout handling, 0-tool case, cache refresh, TCS lifecycle | 1a |
| `HealthService` | Report aggregation, status transitions, issue collection | 1a |
| `ProxyLifecycleManager` | State machine transitions, all 8 states, invalid transitions rejected | 1c |
| `--addins` flag parser | Semicolon splitting, empty entries, whitespace, missing DLLs, empty string | 0 |

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
| **Package with `tools/devserver/` but no `.targets` or manifest** | Unknown add-in | Warning only, no DLL loading |

**Test fixtures**: Create synthetic NuGet package layouts (directory structures) for each scenario. Store in `src/Uno.UI.DevServer.Cli.Tests/Fixtures/`. Each fixture contains the minimum files needed: `buildTransitive/*.targets`, `tools/devserver/*.dll` (empty marker files), `devserver-addin.json` (where applicable).

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
| CLI MCP mode | Direct server launch | Yes (Phase 0) | Fast path, skip MSBuild |
| CLI `start` command | Via controller | Yes (Phase 0) | Fast path, skip MSBuild |
| VS extension | Direct launch via `DevServerLauncher` | No (future) | MSBuild discovery (unchanged) |
| VS Code extension | `dotnet Host.dll --httpPort ...` | No (future) | MSBuild discovery (unchanged) |
| Rider extension | `dotnet Host.dll --httpPort ...` | No (future) | MSBuild discovery (unchanged) |

**Forward compatibility**: New add-in packages that follow the convention (see add-in discovery doc) are discovered automatically — no code changes required in the discovery system.

**Backward compatibility**: Older SDK versions (5.x) that predate the `buildTransitive/` convention fall through to the MSBuild fallback. The fallback is transparent and logged.
