# DevServer Discovery Roadmap

> **Origin**: This document was originally `src/Uno.UI.DevServer.Cli/DevServerDiscovery.md`. It is absorbed into this spec directory as the broader vision document.
> **Relationship to `spec.md`**: The main spec implements Phases 3-4 of this roadmap using `.targets` parsing as the initial mechanism and `devserver-addin.json` manifest as the target.

---

# Executive summary
The current DevServer discovery model is fragmented: each IDE independently locates the DevServer, rebuilds MSBuild targets to find the host and add-ins, and manages its own port selection through .user files. This makes startup slow, brittle, and inconsistent across environments.

Uno Discovery replaces this with a unified, manifest-based pipeline. DevServer hosts and add-ins are now described by manifests embedded in their NuGet packages, removing the need for MSBuild-driven discovery. A shared CLI (disco, start, diag) becomes the single entrypoint for VS, VSCode, Rider, and the command line, ensuring identical logic everywhere.

Discovery becomes fast, build-independent, and resilient even when the solution is in a broken state. IDE extensions stop duplicating logic, and the system becomes easier to test, evolve, and reason about. Port ownership remains unchanged for now and will be handled separately in a later phase.

- Current: 
  - each IDE discovers/starts DevServer on its own
  - uses MSBuild for host/add-ins
  - picks/persists ports in `.user`
  - startup is slow/fragile and ports can clash across IDEs
- Target:
  - manifest-based discovery (host + add-ins)
  - single CLI entrypoint (`disco/start/diag`)
  - local add-ins injectable via explicit manifest/dir
  - no MSBuild for discovery
  - no duplicate discovery logic in IDE extensions
- Risks (test-driven mitigations):
  - manifest rollout across IDEs
  - every phase will be covered by tests

---

# DevServer discovery and startup (draft)

## Purpose
Document how the Uno DevServer is located and started across supported environments, the discovery steps, and the main failure points to fix. Living draft.

## User contexts
- Visual Studio (VS) - VSIX orchestrates; uses `Uno.UI.RemoteControl.VS`.
- Visual Studio Code (VSCode) - JavaScript extension; similar responsibilities.
- Rider - Kotlin plugin hosting .NET code; similar flow.
- CLI - `dnx uno.devserver start`; used when no IDE.

## Lifecycle (common phases)
1) Extension/plugin activation - idle until a solution/project is opened.
2) Project load trigger  
   - VS: VSIX finds the DevServer package (`Uno.UI.DevServer`/`Uno.WinUI.DevServer`, fallback RemoteControl), sets `_toolsPath = <InstallPath>/tools/rc`, loads `17.0/Uno.UI.DevServer.VS.dll` (or RemoteControl) and instantiates `EntryPoint` (prefers API v3/RPC). Builds `host/net{dotnetMajor}.0/Uno.UI.RemoteControl.Host.dll` and runs it via `dotnet`. Layout: `.nuget\uno.winui.devserver\<ver>\tools\rc\host\<sdk>\Uno.UI.RemoteControl.Host`.
   - VSCode: runs `dotnet build /t:GetRemoteControlHostPath "<startup project>" [-f <tfm>]`, reads `RemoteControlHostPath`, launches it. Marker `.uno.vscode.remote-control` triggers external-host debug (fixed port/GUID).
   - Rider: Kotlin + C# `DevServerService`. Host resolution: (1) inspect refs into `uno.winui.devserver/.../tools/rc/host`, derive `host/net{major.minor}/...` from `dotnet --version`; (2) fallback `dotnet build /t:GetRemoteControlHostPath "<project>" -f <tfm>`. First success wins.
   - CLI: payload already known; no IDE resolution.
3) DevServer process launch  
   - VS: `dotnet "<_toolsPath>/host/net{major}.0/Uno.UI.RemoteControl.Host.dll" --httpPort {port} --ppid {IDE pid} --ideChannel {pipeGuid} --solution "<solution.sln>"` (hidden, stdout/err captured).
   - Port handling:
     * VS: reads `UnoRemoteControlPort` from project `.user`; reuses if free else grabs a free port; writes back to startup projects. Racy.
     * VSCode: uses `unoplatform.rc.host.port` if valid else `findAvailablePort`; GUID for `--ideChannel` (fixed with marker). Persists `<UnoRemoteControlPort>` to project `.csproj.user`. Launches `dotnet {RemoteControlHostPath} ...`.
     * Rider: port from `portHelperService.GetTcpPort()`, propagated to all Uno `.csproj.user` via `CsprojUserGenerator` (`<UnoRemoteControlPort>`). Launches host with GUID; auto-restart on exit.
   - CLI: same launch semantics; no port persistence.
   - Port persistence today: VS `.user`; VSCode `.csproj.user`; Rider `.csproj.user`; CLI none.
4) Capability discovery (current)
   - Step A: `dotnet build "<solution>" -t:UnoDumpTargetFrameworks ...`
   - Step B: per TFM `dotnet build "<solution>" -t:UnoDumpRemoteControlAddIns ... --framework "<tfm>" -nowarn:MSB4057`
   - Add-ins injected via `.targets` (`<UnoRemoteControlAddIns Include="...dll" />`). Stub targets when `UsingUnoSdk != true`; real target expected from Uno.Sdk/RC packages.
5) Add-in loading
   - `Assembly.LoadFrom` in default ALC (not collectible).
   - DI via `Services.AddFromAttributes(assemblies)`; no explicit add-in ID.
   - Shared AppDomain – potential version conflicts; restart to unload.

## Known issues / fragilities
- Port selection weak/racy; per-IDE ownership; collisions possible.
- Build-heavy discovery; add-ins require builds.
- Broken solutions break add-in discovery; little self-healing.
- VS early-port helper races with server start.
- Path resolution differs per IDE; inconsistent when packages/targets missing.
- Port source of truth per IDE; multiple IDEs can clobber `.user`.

## Proposed changes (by theme)
### [A] Host discovery without MSBuild
- DevServer package carries a manifest listing host binaries per TFM; MSBuild targets removed for host lookup once manifest is deployed.

### [B] Add-in discovery without MSBuild builds
- Add-in NuGets carry a manifest; DevServer scans restored packages (no builds).
- Local dev add-ins: allow extra manifest or folder (`--addin-manifest`, `--addin-dir`) to inject add-ins.

### [C] Centralized bootstrap via CLI
- Commands: `disco` (host + TFMs), `start`, `diag`.
- Context resolution (dotnet-like): cwd or explicit path -> single sln/slnx else error if multiple/`.slnf`; else single csproj; else error.

### [D] Port ownership (out of scope)
- Optional future handshake where DevServer picks/broadcasts port. Not in scope now.

### [E] Command-line UDEI status/diagnostics (out of scope)
- `dnx uno.devserver status` for CLI view of UDEI. Out of scope.

### [F] Live log capture (out of scope)
- `dnx uno.devserver watch` to stream logs of active DevServer; `status/diag` should report active instance and a second `start` should fail explicitly. Out of scope.

## Acceptance criteria (per change)
- Host manifest: host resolved without MSBuild in <1s; works on VS/VSCode/Rider/CLI.
- Add-in manifest: add-ins discoverable without builds; supports local injection via flag/file; telemetry notes source.
- CLI bootstrap: `disco/start/diag` callable; machine- and human-readable output; backward compatible with direct start.
- Port ownership (optional): opt-in handshake, no break for current IDEs.
- Local add-ins: extra manifest/dir args accepted; errors surfaced via IdeChannel/diag.
- Ambiguity tests before IDE integration: `disco` fails clearly on multiple sln/slnx or multiple csproj; VS integration only after these pass.
- Status/diag/report indicate if a DevServer is already active for the resolved project; a second `start` fails explicitly.

## Other known problems (not addressed)
- Multiple Uno heads in one solution: port/version coordination unclear; assume single head; warn.
- SDK version in csproj instead of `global.json`: warn (unsupported path).
- Same solution opened in multiple IDEs/instances: state/port clobber risk; warn if detected.
- Port persistence divergence until a server-owned handshake exists.
- For VSCode/Rider, manifest rollout may need a compatibility plan (not covered here).

# Implementation roadmap
## Phase 0: Baseline tests
  - Integration tests launching current CLI, capturing output, asserting current behavior (ports, add-ins). Non-regression baseline.
## Phase 1: CLI refactor
  - Command parsing; ProjectContextResolver (dotnet-like); interfaces (IHostLocator, IAddInLocator). `start` unchanged.
## Phase 2: `disco` + ambiguity handling
  - Implement `disco` with resolver; JSON + human output; explicit errors on multiple sln/slnx or multiple csproj; none found -> error. Must pass before IDE changes.
## Phase 3: Host manifest (no MSBuild)
  - Read host manifest in package; MSBuild no longer used for host location. Tests: manifest present vs missing (error surfaced).
## Phase 4: Add-in manifest + local injection
  - Consume add-in manifest; no MSBuild fallback. Support `--addin-manifest` / `--addin-dir` for local add-ins. Tests: manifest-only + local.
## Phase 5: Diagnostics (`diag`)
  - Markdown + JSON report with host/TFM/add-ins and warnings (multi-heads, SDK in csproj, multi-IDE). Reuse resolver and manifest-first discovery.
## Phase 5b: Shared live registry (hub IPC)
  - Replace file-based AmbientRegistry with a local IPC hub (pipe/UDS) tracking active DevServers (pid, solution, port) via heartbeats.
  - “First-to-bind” hub election; peers re-announce on hub restart; reject a second `start` on the same solution via hub (not via port collision).
  - `list/status/diag` use the hub; legacy file registry only as temporary fallback if needed.
## Phase 6: IDE integration (start with VS)
  - VS consumes CLI `disco/start` (or manifest) under feature flag. Validate port/host/add-ins via new pipeline.
## Phase 7: IDE integration (VSCode & Rider)
  - VSCode and Rider adopt the same CLI/manifest-first flow behind flags. Ensure `.csproj.user` port persistence still works; plan compatibility if older extensions linger.
## Phase 8: Cleanup/compat
  - Document manifest formats and CLI contracts; plan future port-handshake work separately.
