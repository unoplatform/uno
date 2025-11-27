# DevServer discovery and startup (draft)

## Purpose
Document how the Uno DevServer is located and started across supported environments, the build-based discovery steps, and the main failure points we need to harden. This is a living draft; mark items that need validation against source.

## User contexts
- Visual Studio (**VS**) (VS extension orchestrates; calls `Uno.UI.RemoteControl.VS` in this repo). Responsible for: starting DevServer, running uno-check, surfacing UDEI panel.
- Visual Studio Code (**VSCode**) (JavaScript extension in separate repo). Similar responsibilities: start DevServer, run uno-check, VS Code-flavored UDEI.
- Rider (**Rider**) (Kotlin plugin hosting .NET code). Mirrors VS Code flow, implemented in Rider plugin stack. Responsible for uno-check and Rider-specific UDEI.
- **CLI** (`uno.devserver` dotnet tool, invoked via `dnx uno.devserver start`). Used by LLM scenarios (or power-users without an IDE?)

## Lifecycle (common phases)
1. Extension/plugin activation  
   Each IDE loads its extension independently of any solution. They remain idle until a project/solution is opened.

2. Project load trigger  
   - VS: VSIX enumerates solution NuGet packages (`DevServerLauncher` in uno.studio) looking for `Uno.UI.DevServer` / `Uno.WinUI.DevServer` (fallback `Uno.UI.RemoteControl`/`Uno.WinUI.RemoteControl`). Sets `_toolsPath = <InstallPath>/tools/rc`, loads `17.0/Uno.UI.DevServer.VS.dll` or `17.0/Uno.UI.RemoteControl.VS.dll`, instantiates `EntryPoint` (prefers API v3/RPC). `EntryPoint` builds `host/net{dotnetMajor}.0/Uno.UI.RemoteControl.Host.dll` and runs it via `dotnet`. Expected layout: `.nuget\uno.winui.devserver\<version>\tools\rc\host\<sdk>\Uno.UI.RemoteControl.Host`.
   - VS Code: runs `dotnet build /t:GetRemoteControlHostPath "<startup project>" [-f <tfm>]`, reads `RemoteControlHostPath`, launches it. `.uno.vscode.remote-control` marker enables external-host debug (fixed port/GUID).
   - Rider: Kotlin + C# `DevServerService`. Host path resolution: (1) inspect assembly references into `uno.winui.devserver/.../tools/rc/host`, derive `host/net{major.minor}/...` using `dotnet --version`; (2) fallback `dotnet build /t:GetRemoteControlHostPath "<project>" -f <tfm>`. Uses first success; no hardcoded layout when target supplies path.
   - CLI: payload already known; no IDE resolution.

3. DevServer process launch  
   - VS: `dotnet "<_toolsPath>/host/net{major}.0/Uno.UI.RemoteControl.Host.dll" --httpPort {port} --ppid {IDE pid} --ideChannel {pipeGuid} --solution "<solution.sln>"` (hidden, stdout/err captured).  
   - Port handling:  
     * VS: reads `UnoRemoteControlPort` from project `.user`; reuses if free else grabs a free port; writes back to all startup projects’ `.user`. Race remains.  
     * VS Code: uses setting `unoplatform.rc.host.port` if valid else ephemeral via `findAvailablePort`; GUID for `--ideChannel` (fixed when debug marker present). Persists to project `.csproj.user` (`<UnoRemoteControlPort>`). Launches `dotnet {RemoteControlHostPath} ...`.  
     * Rider: port from `portHelperService.GetTcpPort()`, consolidated to all Uno project `.csproj.user` via `CsprojUserGenerator` (`<UnoRemoteControlPort>`). Launches host with GUID; auto-restart on exit.  
   - CLI does the same internally (no persistence).

   **Port persistence today**
   - VS: project `.user` (`UnoRemoteControlPort`).
   - VS Code: project `.csproj.user` (`UnoRemoteControlPort`) + optional setting hint.
   - Rider: project `.csproj.user` (`UnoRemoteControlPort`).
   - CLI: none.

4. Capability discovery builds  
   - Step A: `dotnet build "<solution>" -t:UnoDumpTargetFrameworks "-p:UnoDumpTargetFrameworksTargetFile=<tmp>" "-p:CustomBeforeMicrosoftCSharpTargets=<DevServer.Custom.Targets>" --verbosity quiet`.  
   - Step B: per TFM `dotnet build "<solution>" -t:UnoDumpRemoteControlAddIns "-p:UnoDumpRemoteControlAddInsTargetFile=<tmp>" "-p:CustomBeforeMicrosoftCSharpTargets=<DevServer.Custom.Targets>" --verbosity quiet --framework "<tfm>" -nowarn:MSB4057`.  
   - Add-ins come from `.targets` injecting `<UnoRemoteControlAddIns Include="...dll" />`. `DevServer.Custom.Targets` imports stubs when `UsingUnoSdk != true`; real target expected from Uno.Sdk/RC packages.

5. Add-in loading  
   - Uses `Assembly.LoadFrom` in default ALC (not collectible).  
   - Services registered via `Services.AddFromAttributes(assemblies)` (DI attribute scanning).  
   - Implications: shared AppDomain, potential version conflicts; restart needed to unload.

## Known issues / fragilities
- Port selection weak/racy: per-IDE selection/persistence; collisions possible; Kestrel fails if port already bound.
- Build-heavy discovery: multiple MSBuild invocations delay startup.
- Unhealthy solutions break property evaluation → add-ins missing with little self-healing.
- VS early-port helper races with server start.
- Path resolution differs per IDE (VS package probing, VS Code build target, Rider refs/build); inconsistent behavior if packages/targets missing.
- Port source of truth lives in each IDE; multiple IDEs can clobber `.user`.

## Proposed changes (by theme)

### [A] Host discovery without MSBuild (faster startup)
- Manifest in DevServer NuGet (`tools/rc/manifest.json`) listing host binaries per TFM; MSBuild target as fallback.

### [B] Add-in discovery without MSBuild builds
- Add-in NuGets ship `uno.addins.json`; DevServer scans restored packages; MSBuild fallback.
- Local dev add-ins via `--addin-manifest` / `--addin-dir`.

### [C] Centralized bootstrap via CLI
- Commands: `disco` (host/TFMs/add-ins manifest-first, MSBuild fallback), `start`, `diag`.
- Context resolution (dotnet-like): cwd or explicit path → single sln/slnx else error if multiple/`.slnf`; else single csproj; else error.

### [D] Port ownership (out of scope)
- Optional future handshake: DevServer picks/broadcasts port. Not in scope now.

### [E] Command-line UDEI status/diagnostics (out of scope)
- `dnx uno.devserver status` for CLI view of UDEI. Out of scope.

### [F] Live log capture (out of scope)
- `dnx uno.devserver watch` to stream logs of active DevServer; status/diag should report existing instance and second start should fail explicitly. Out of scope.

## Acceptance criteria (per change)
- Host manifest: resolve host without MSBuild in <1s; fallback retained; works VS/VSCode/Rider/CLI.
- Add-in manifest: discover add-ins without build when manifest present; fallback kept; local inject via flag/file; telemetry notes source.
- CLI bootstrap: `disco/start/diag` callable; machine + human output; backward compatible with direct start.
- Port ownership (optional): opt-in handshake, no break for current IDEs.
- Local add-ins: extra manifest/dir args accepted; errors surfaced via IdeChannel/diag.
- Ambiguity tests before IDE integration: `disco` fails clearly on multiple sln/slnx or multiple csproj; VS integration only after these pass.
- Status/diag/report should indicate active DevServer for resolved project; second `start` fails explicitly.

## Other known problems (not addressed)
- Multiple Uno heads in one solution: port/version coordination unclear; assume single head; warn.
- SDK version in csproj instead of `global.json`: warn (unsupported path).
- Same solution opened in multiple IDEs/instances: state/port clobber risk; warn if detected.
- Port persistence divergence until server-owned handshake exists.

# Implementation roadmap
## Phase 0: Baseline tests
  - Add integration tests in DevServer tests to launch current CLI, capture output, assert existing behavior (ports, add-ins via MSBuild). Goal: non-regression baseline.
## Phase 1: CLI refactor
  - Command parsing, refactor to be ready to receive new commands.
  - `start` behavior unchanged; tests stay green.
## Phase 2: `disco` command + ambiguity handling
  - Implement `disco` with resolver; JSON + human text output.
  - Explicit errors for multiple sln/slnx or multiple csproj; none found → error.
  - Ambiguity tests must pass before IDE changes.
## Phase 3: Host manifest (no-MSBuild host lookup)
  - Read `tools/rc/manifest.json` if present; fallback MSBuild.
  - Tests: manifest package vs. no-manifest fallback.
## Phase 4: Add-in manifest + local injection
  - Consume `uno.addins.json`; fallback MSBuild.
  - Support `--addin-manifest` / `--addin-dir` for local dev add-ins.
  - Tests for manifest-only, fallback, local add-ins.
## Phase 5: Diagnostics (`diag`)
  - Markdown + JSON report with host/TFM/add-ins and warnings (multi-heads, SDK in csproj, multi-IDE).
  - Reuse resolver and manifest-first discovery.
## Phase 6: IDE integration (start with VS)
  - VS consumes CLI `disco/start` (or manifest) under feature flag.
  - Validate port/host/add-ins via new pipeline; keep fallback.
## Phase 7: Other IDEs integration (VS Code + Rider)
  - VS Code and Rider adopt same CLI/manifest-first flow (or manifest) behind flags.
  - Ensure `.csproj.user` port persistence still works; fallback maintained.
## Phase 8: Cleanup/compat
  - Maintain MSBuild fallback for older packages.
  - Document manifest formats and CLI contracts; plan future port-handshake work separately.
