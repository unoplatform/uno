# Appendix H: Manual QA Plan

> **Parent**: [Main Spec](spec.md) — Section 9
> **Related**: [Testing](spec-appendix-c-testing.md) | [Compatibility Matrix](spec-appendix-g-compatibility-matrix.md)

These scenarios **cannot be reliably automated** and require human testers with access to real IDE installations, MCP clients, license accounts, and multi-platform hardware. Each scenario includes acceptance criteria and the reason it requires manual validation.

---

## 1. IDE Extension Compatibility (Current Versions)

> **Why manual**: Requires real IDE installations with current extension versions, valid licenses, and interactive UI verification.

### Prerequisites
- Windows machine with Visual Studio + `uno.studio` extension (current shipped version)
- Machine with Rider + `uno.rider` plugin (current shipped version)
- Machine with VS Code + `uno.vscode` extension (current shipped version)
- A working Uno project that builds and runs with Hot Reload

### Scenarios

| # | Scenario | Steps | Accept Criteria |
|---|----------|-------|-----------------|
| H1.1 | VS with modified Host (no `--addins`) | 1. Update Uno SDK (includes modified Host). 2. Open project in VS. 3. Run app with Hot Reload. | App launches, Hot Reload works, DevServer tools panel functional. No behavior change from before. |
| H1.2 | Rider with modified Host (no `--addins`) | Same as H1.1 but with Rider. | Same criteria. |
| H1.3 | VS Code with modified Host (no `--addins`) | Same as H1.1 but with VS Code. | Same criteria. |
| H1.4 | VS with older Uno SDK (5.x) | 1. Open a project targeting Uno 5.x. 2. Run with Hot Reload. | MSBuild discovery runs (slow but works). No crash, no regression. |
| H1.5 | IDE after `--addins` Host update, extension NOT updated | 1. Update only the Uno SDK (not the IDE extension). 2. Open project. 3. Run. | Extension still works. Host accepts launch without `--addins`. MSBuild discovery path unchanged. |

### What to watch for
- `.csproj.user` file is generated with correct port
- App connects to DevServer on the correct port
- Hot Reload edits propagate to the running app
- No duplicate DevServer processes in Task Manager
- No new error messages in IDE output window

---

## 2. Multi-Instance Scenarios (IDE + MCP Coexistence)

> **Why manual**: Requires orchestrating an IDE with a CLI/MCP agent on the same project, observing cross-process behavior that is very difficult to script.

### Prerequisites
- IDE (any) with DevServer running on a project
- CLI with MCP mode available
- Ability to monitor processes (Task Manager / `ps`)

### Scenarios

| # | Scenario | Steps | Accept Criteria |
|---|----------|-------|-----------------|
| H2.1 | IDE first, then MCP | 1. Open project in IDE, verify DevServer running. 2. Start `dnx uno.devserver --mcp-app` for same project. 3. Use MCP tools. | MCP connects to existing DevServer instance (via AmbientRegistry). Only ONE Host process. MCP tools functional. |
| H2.2 | MCP first, then IDE | 1. Start MCP for a project. 2. Open same project in IDE. | IDE launches its own Host (expected — IDE not modified). Two instances exist. Verify: app connects to correct one, no port crash. Document observed behavior. |
| H2.3 | MCP running, IDE closes | 1. Both running (scenario H2.2). 2. Close IDE. | If MCP was connected to IDE's Host: hot reconnection triggers, MCP relaunches its own Host. If MCP had its own Host: no impact. |
| H2.4 | IDE running, MCP crashes | 1. IDE + MCP running. 2. Kill MCP CLI process. | IDE's DevServer completely unaffected. No port issues. |
| H2.5 | Two MCP agents on same project | 1. Start first MCP agent. 2. Start second MCP agent for same project. | Second agent detects existing instance via AmbientRegistry, connects to same Host. Only ONE Host process. |

### What to watch for
- Number of `Uno.UI.RemoteControl.Host` processes (should be 1 in ideal cases)
- Port conflicts (bind errors in logs)
- `.csproj.user` port value matches the running Host
- Hot Reload still works after MCP connects/disconnects

---

## 3. MCP Client Behavior

> **Why manual**: Each MCP client has its own transport quirks, timeout behavior, and UI for displaying tool results. Automated testing can verify protocol compliance but not the end-user experience.

### Prerequisites
- Access to at least 3 MCP clients (e.g., Claude Code, Cursor, VS Code Copilot)
- A working Uno project

### Scenarios

| # | Scenario | Client | Steps | Accept Criteria |
|---|----------|--------|-------|-----------------|
| H3.1 | Normal startup (warm cache) | Claude Code | 1. Start MCP. 2. Ask agent to list tools. | Tools appear within ~5s. `uno_app_start` available. |
| H3.2 | Normal startup (warm cache) | Cursor | Same. | Same. Verify `tools/list_changed` updates tool list in Cursor's MCP panel. |
| H3.3 | Startup with `--mcp-wait-tools-list` | Codex CLI | 1. Start MCP with flag. 2. Call a tool. | Initial `list_tools` blocks until upstream responds (not cached). Tools functional. |
| H3.4 | Tool call before host ready | Any client | 1. Start MCP. 2. Immediately call `uno_app_start`. | Structured error returned (not hang, not crash). Error mentions `uno_health` or `uno://health`. |
| H3.5 | `uno_health` tool | Any client | 1. Start MCP. 2. Call `uno_health`. | Returns structured JSON with status, SDK version, issues list. Works even before upstream connected. |
| H3.6 | `uno://health` resource | Client with resources (Cursor, VS Code) | 1. Start MCP. 2. Read `uno://health` resource. | Same data as `uno_health` tool. |
| H3.7 | Timeout (30s) with no upstream | Any client | 1. Start MCP with broken project (no solution). 2. Wait. | `list_tools` returns within 30s (cached or empty + health warning). Does NOT hang. |

### What to watch for
- Response times (measure with stopwatch for real-world feel)
- Error messages are understandable by AI model (not stack traces)
- Client UI doesn't freeze or show cryptic errors
- `--force-roots-fallback` works correctly for clients without `roots`

---

## 4. License Tier Transitions

> **Why manual**: Requires real license accounts at different tiers and the ability to change tier mid-session.

### Prerequisites
- Community license account
- Pro or Business license account
- Ability to sign in/out during a session

### Scenarios

| # | Scenario | Steps | Accept Criteria |
|---|----------|-------|-----------------|
| H4.1 | Community startup | 1. Start MCP with Community account. 2. List tools. | ~9 tools visible. Pro/Business tools absent. |
| H4.2 | Pro startup | 1. Start MCP with Pro account. 2. List tools. | ~11 tools visible. |
| H4.3 | Upgrade mid-session | 1. Start as Community. 2. Sign in with Pro account. | `tools/list_changed` sent (for clients that support it). New tools appear. |
| H4.4 | No license / expired | 1. Start MCP with expired license. 2. List tools. | Response within 30s (not hang). Cached tools or empty list. Health shows license issue. |
| H4.5 | Tool cache reflects tier | 1. Start as Pro, stop. 2. Start as Community. | Cache serves Pro tools initially (stale), refreshes to Community count when upstream responds. |

### What to watch for
- `list_tools` never hangs (30s max, even with license issues)
- Tool count matches expected tier
- Cache doesn't permanently serve wrong tier's tools

---

## 5. Cross-Platform Verification

> **Why manual**: Edge cases around filesystem behavior, path handling, and platform-specific NuGet cache locations require real hardware or carefully configured VMs.

### Prerequisites
- Windows 11 machine
- macOS machine (preferably with case-sensitive APFS volume for testing)
- Linux machine (Ubuntu or similar)

### Scenarios

| # | Scenario | Platform | Steps | Accept Criteria |
|---|----------|----------|-------|-----------------|
| H5.1 | Default NuGet cache | All 3 | 1. Run `disco --json`. | Correct add-in paths resolved using platform-default NuGet cache. |
| H5.2 | Custom `$NUGET_PACKAGES` | All 3 | 1. Set env var to custom path. 2. Restore packages there. 3. Run `disco --json`. | Packages found in custom location. |
| H5.3 | Case-sensitive filesystem | Linux | 1. Verify NuGet cache has lowercase dirs. 2. Run discovery. | Packages found despite case differences in `.targets` references. |
| H5.4 | Long path | Windows | 1. Place project in deep directory (> 260 chars total path). 2. Run discovery. | No `PathTooLongException`. |
| H5.5 | Path with spaces | All 3 | 1. Place project in path with spaces (e.g., `My Projects/`). 2. Run MCP. | All paths resolved correctly. No broken arguments. |

---

## 6. Performance Under Real Conditions

> **Why manual**: CI benchmarks use warm caches and fast disks. Real-world performance depends on cold OS cache, HDD vs SSD, antivirus, etc.

### Prerequisites
- A machine representative of developer hardware
- A real Uno solution (not a minimal test project)
- Stopwatch or timing script

### Protocol
1. **Baseline** (before changes): Measure current startup times. Record 5 runs, report median.
2. **After Phase 0**: Same measurement. Compare.
3. **After Phase 1**: Same measurement. Compare.

### Metrics to measure

| Metric | How | Target |
|--------|-----|--------|
| CLI cold start → first `list_tools` | Time from process start to first response | < 1s (Phase 1, cached) |
| CLI cold start → functional tool call | Time from process start to `uno_app_start` succeeding | < 5s (Phase 1) |
| `disco --json` | Time from invocation to output | < 500ms |
| Hot reconnection | Kill Host, measure time to next successful tool call | < 10s |

### Conditions to test
- **Cold start**: Reboot machine, clear OS file cache, first run
- **Warm start**: Normal developer workflow, NuGet cache populated
- **Large solution**: 50+ projects, multiple add-in packages
- **Slow disk**: HDD or network drive (if available)

---

## 7. Host Crash Recovery (Hot Reconnection)

> **Why manual / semi-automatable**: The kill can be scripted, but verifying the MCP client's reaction (no disconnect, tool recovery, state machine transitions) requires observation.

### Scenarios

| # | Scenario | Steps | Accept Criteria |
|---|----------|-------|-----------------|
| H7.1 | Single crash | 1. MCP running, tools functional. 2. Kill Host process. 3. Call a tool. | MCP auto-restarts Host. Tool call succeeds after brief delay (< 10s). `tools/list_changed` sent if client supports it. |
| H7.2 | Triple crash | 1. Kill Host 3 times in succession. | After 3rd crash, MCP enters Degraded state. `uno_health` reports `HostCrashed`. Cached tools still returned. |
| H7.3 | Crash during tool call | 1. Call a long-running tool. 2. Kill Host mid-execution. | Tool call returns error (not hang). MCP begins reconnection. Next tool call works. |
| H7.4 | Crash + IDE running | 1. IDE + MCP both connected to same Host (scenario H2.1). 2. Kill Host. | Both IDE and MCP lose connection. MCP auto-reconnects (launches new Host). IDE may or may not reconnect (IDE's responsibility). |

---

## 8. QA Execution Tracking

For each phase, track manual QA completion:

### Phase 0
- [ ] H1.1-H1.5 (IDE compatibility)
- [ ] H5.1-H5.5 (Cross-platform)
- [ ] H6 baseline measurements

### Phase 1
- [ ] H2.1-H2.5 (Multi-instance)
- [ ] H3.1-H3.7 (MCP clients)
- [ ] H6 post-Phase 1 measurements
- [ ] H7.1-H7.4 (Crash recovery)

### Phase 2
- [ ] H6 post-Phase 2 measurements (cold start target: ~200ms)

### Phase 3
- [ ] H4.1-H4.5 (License tiers)
- [ ] H1.4 (Older SDK)
