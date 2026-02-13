# Appendix E: Reference Tables & Analysis

> **Parent**: [Main Spec](spec.md)

---

## E.1 MCP Tools (from uno.app-mcp)

> **Note**: The tool count visible to the AI model depends on the user's license tier. `MCPToolsObserverService` filters tools via `[LicenseFeatures]` attributes. The tool cache (`tools-cache.json`) reflects the license tier active at cache time.

| Tool Name | Description | Min License |
|-----------|-------------|:----------:|
| `uno_app_start` | Start app with Hot Reload | Community |
| `uno_app_close` | Graceful or forced termination | Community |
| `uno_app_get_runtime_info` | PID, window title, uptime | Community |
| `uno_app_get_screenshot` | Take PNG/JPEG screenshot | Community |
| `uno_app_pointer_click` | Pointer interaction | Community |
| `uno_app_key_press` | Keyboard input | Community |
| `uno_app_type_text` | Text input | Community |
| `uno_app_visualtree_snapshot` | XML visual tree dump | Community |
| `uno_app_element_peer_default_action` | Default automation action | Community |
| `uno_app_element_peer_action` | Advanced automation action | Pro |
| `uno_app_get_element_datacontext` | Element data context | Pro |
| `uno_app_get_memory_counters` | Memory diagnostics | Business |

**Visible tools by tier**: Community ~9, Pro ~11, Business ~12. Exact counts may change as tools are added.

---

## E.2 Related Specifications

- [`discovery-roadmap.md`](spec-appendix-f-discovery-roadmap.md) — Broader discovery and startup redesign roadmap (host manifest, add-in manifest, CLI bootstrap, port ownership).
- This spec covers both the general add-in discovery optimization (all modes) and MCP-specific fast-startup improvements. It implements Phases 3-4 of the discovery roadmap using `.targets` parsing as the first step, with `devserver-addin.json` manifest as the target.

---

## E.3 Architectural Convergence with Discovery Roadmap

[`discovery-roadmap.md`](spec-appendix-f-discovery-roadmap.md) defines a broader roadmap for manifest-based discovery. This spec is an intermediate step on that trajectory:

```
Current state          This spec (Phase 0-1)           Discovery Roadmap target
────────────────       ─────────────────────           ──────────────────────────────
MSBuild targets        .targets XML parsing            devserver-addin.json manifest
dotnet build (10-30s)  Direct XML read (<200ms)        Direct JSON read (<10ms)
UnoRemoteControlAddIns UnoRemoteControlAddIns          Manifest-declared entry points
Per-IDE discovery      CLI-centralized                 CLI-centralized
```

**The `.targets` parsing approach is explicitly transitional:**
1. It works today with zero changes to existing add-in packages
2. It proves the fast-path concept and enables immediate startup gains
3. It will be superseded by `devserver-addin.json` manifests when packages are updated
4. The MSBuild fallback will be deprecated when all maintained SDKs support either `.targets` parsing or manifests

**Not a competing direction** — this spec implements Phase 3 ("Host manifest") and Phase 4 ("Add-in manifest") of the discovery roadmap using `.targets` parsing as the first implementation, with `devserver-addin.json` manifest as the target.

---

## E.4 IDE Extension Analysis

Analysis of how each IDE extension currently discovers and launches the DevServer, and how they benefit from the `--addins` flag.

### Visual Studio (`uno.studio`)

**Current flow** (`DevServerLauncher.cs:269,302`, `EntryPoint.cs:488`):
1. Finds `uno.winui.devserver` (or `uno.ui.devserver`, fallback `remotecontrol`) package in project references
2. Resolves `tools/rc/host/net{major}.0/Uno.UI.RemoteControl.Host.dll` from package install path
3. Loads an intermediate assembly via `DevServerLauncher`, then launches Host **directly** (not via `--command start`) with `--httpPort {port} --ppid {pid} --solution {sln}`
4. Host performs MSBuild add-in discovery internally (10-30s)
5. Uses `EntryPoint` API v3/RPC for IDE channel communication
6. **No AmbientRegistry duplicate check** — VS manages its own instance lifecycle

**Impact of `--addins`**: VS can resolve add-in paths via the same fast discovery logic, then pass `--addins` to the Host. This would save 10-30s on every project load. Adoption path: VS calls `disco --addins-only --json` and feeds result to `--addins`.

### Rider (`uno.rider`)

**Current flow** (`DevServerService.cs:138`, `HostFolderPathResolver.cs`):
1. **Fast path**: Inspect project references, find `uno.winui.devserver` NuGet package, derive `host/net{major}.{minor}/...` from `dotnet --version`
2. **Fallback**: `dotnet build /t:GetRemoteControlHostPath "{project}" -f {tfm}` (slow, same as current)
3. Launches Host **directly** with `--httpPort {port} --ppid {pid} --solution {sln}` (no `--command start`)
4. Port managed via `CsprojUserGenerator` writing to `.csproj.user`
5. Auto-restart on host process exit
6. **No AmbientRegistry duplicate check**

**Impact of `--addins`**: Rider already has a fast host resolution path. Adding `--addins` support is straightforward — the `.targets` parsing logic can be exposed via `disco --addins-only --json` and Rider can pass the result. Rider's auto-restart mechanism aligns well with the hot reconnection design.

### VS Code (`uno.vscode`)

**Current flow** (`extension.ts:603,607`):
1. Always uses MSBuild: `dotnet build /t:GetRemoteControlHostPath "{startup project}" [-f {tfm}]`
2. Reads `RemoteControlHostPath` from build output
3. Launches Host **directly** with `--httpPort {port}` (no `--command start`), persists port to `.csproj.user`
4. External-host debug mode via `.uno.vscode.remote-control` marker file
5. **No AmbientRegistry duplicate check**

**Impact of `--addins`**: VS Code currently has no fast path at all — it always pays the MSBuild cost for host resolution AND add-in discovery. This extension benefits the most from `disco --json` which can replace the entire MSBuild call. The extension can call `dnx uno.devserver disco --json` to get host path AND add-in paths in < 200ms.

### Common Pattern

**All three IDE extensions launch the Host directly** — none use `--command start` (the controller path). The controller is only used by CLI `start`. This means:
- AmbientRegistry duplicate protection **does not exist** for IDE-launched instances
- Each IDE manages its own DevServer lifecycle independently
- **Multiple instances for the same solution** are possible today (IDE + CLI, or IDE + MCP)

### Adoption Summary

| IDE | Current Host Discovery | Current Add-In Discovery | Fast Path Available | Adoption Effort |
|-----|----------------------|------------------------|:-------------------:|:---:|
| VS | Package inspection | MSBuild (in Host) | Needs `disco` | Medium |
| Rider | Package inspection + MSBuild fallback | MSBuild (in Host) | Needs `disco` | Low |
| VS Code | MSBuild only | MSBuild (in Host) | Needs `disco` | Low |
| CLI | `UnoToolsLocator` | MSBuild (in Host) | Phase 0 | Done |

**Key insight**: All three IDE extensions can benefit from a single CLI command (`disco --addins-only --json`) that returns pre-resolved add-in paths. This is exposed in Phase 0 of the implementation and can be adopted incrementally by each IDE team.
