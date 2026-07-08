# Hot-Reload Workspace: Property Whitelist & Runtime-Reported TFM Filtering

## Overview & Objectives

The dev-server initializes its Roslyn `MSBuildWorkspace` by replicating the *build-time*
environment of the connecting application: ~20 MSBuild properties captured at build by the
`RemoteControlGenerator` (`ProjectConfigurationAttribute`), sent through `ConfigureServer`,
and re-applied as **global properties** on the workspace. The head project's TFM travels the
most fragile path of all: `TargetFramework` (captured) → renamed to
`UnoHotReloadTargetFramework` (server, `GetWorkspaceProperties`) → promoted back to
`TargetFramework` during evaluation by `buildTransitive/Uno.WinUI.DevServer.props`
(app-side package import, `UnoIsHotReloadHost` gated).

When **any** link of that chain fails, the evaluated `TargetFramework` is empty and Roslyn's
`MSBuildWorkspace` enumerates **every** entry of `TargetFrameworks` — one project per TFM —
so the workspace contains `net10.0-android`, `net10.0-ios`, `net10.0-browserwasm` *and*
`net10.0-desktop` flavors of the head while a desktop app is being debugged. Two failure
modes follow:

1. **Blocked updates** — the non-running flavors fail to compile in the workspace context
   (e.g. Android: `error CS0433: The type 'ApplicationActivity' exists in both
   'Uno.UI.Runtime.Skia.Android' and 'Uno.UI'`), and since
   `HotReloadManager.GetCompilationErrors` scans **all** projects of the solution, every
   cycle ends with `Hot reload blocked by N compilation error(s)`.
2. **Dead init** — flavors that were never built have no output assembly
   (`CompilationOutputInfo.AssemblyPath` missing/null), which forces
   `EmitCompilationOutputAsync` over the whole solution at init; the broken flavor fails the
   emit and hot-reload never comes up.

A field regression made this chain break in practice (VS Code extension
[uno.vscode#1351](https://github.com/unoplatform/uno.vscode/pull/1351): `.csproj.user`
write/delete churn at dev-server startup interacting with the build-time property capture),
but the fragility is structural: the design couples workspace correctness to build-time
capture, IDE build orchestration, and app-side package imports — three things the server
does not control.

### Key objective

Make the hot-reload workspace initialization **deterministic and independent of build-time
property capture**:

1. Pass only a small **whitelist** of global properties to the workspace.
2. Let `MSBuildWorkspace` load the head project **naturally** (all TFM flavors).
3. Filter the resulting **solution snapshot**, keeping only the head flavor that matches the
   TFM the **running app reports at runtime** through `ConfigureServer`.
4. Retire the `UnoHotReloadTargetFramework` promotion machinery (server side).

---

## Verified facts (investigation grounding)

These were established empirically (Roslyn 4.14 `MSBuildWorkspace` + BuildHost, .NET 10 SDK)
and from source review; they constrain the design:

| # | Fact | Consequence |
|---|------|-------------|
| F1 | Roslyn enumerates per-TFM projects **iff** evaluated `TargetFramework` is empty and `TargetFrameworks` is non-empty (`ProjectFile.GetProjectFileInfosAsync`). | Empty/missing TFM ⇒ all-flavor load. Nothing in between. |
| F2 | The `UnoHotReloadTargetFramework` promotion works whenever the value reaches evaluation — including through `nuget.g.props`-style guarded imports and against conflicting `.csproj.user` content. | The server-side mechanism is not the weak link; the *value* is. Replacing capture with a runtime-reported value fixes the class. |
| F3 | `MSBuildWorkspace` is `sealed`; `CanApplyChange` does **not** support `ApplyChangesKind.RemoveProject`; no selective-removal API (verified against Roslyn 17.14 sources). | Flavor filtering must happen on **`Solution` snapshots**, not by mutating the workspace. |
| F4 | The Uno HR pipeline already operates on snapshots: `HotReloadManager` owns its `CurrentSolution`; `WatchHotReloadService.CreateAsync` captures `workspace.CurrentSolution` once; `EmitCompilationOutputAsync` has a `Solution` overload. The inner workspace is only used for services and `Dispose`. | Filtering once, right after `OpenProjectAsync`, propagates everywhere. |
| F5 | Uno source generators condition hot-reload codegen on **`Configuration`** (`_isHotReloadEnabled = _isDebug`) and the optional `UnoForceHotReloadCodeGen` override. `BuildingInsideVisualStudio` / `UnoPlatformIDE` are **telemetry-only** in generators. | `Configuration` is the only captured property that shapes compiled output. The VS-session echoes are safe to drop. |
| F6 | On the VS Code path, all VS-specific properties (`BuildingInsideVisualStudio`, `Solution*`, `LangID`, `DevEnvDir`, …) have been captured **empty** forever — that path already runs essentially property-less. | Dropping the VS echoes cannot regress anything VS Code-shaped; the exposure is VS/Rider only, and limited (see whitelist rationale). |

---

## Design

### D1 — Runtime-reported TFM in `ConfigureServer`

The running application knows its runtime; it must report it explicitly. A new field is
added to the `ConfigureServer` message, populated **at runtime** by the client — *not*
captured from MSBuild at build time.

- The `Uno.UI.RemoteControl` client ships per-flavor (`netcoremobile`, `Skia`, `Wasm`,
  `Reference`), so the **platform identifier** is compile-time knowledge of the client
  assembly (`android` / `ios` / `tvos` / `maccatalyst` / `browserwasm` / skia-desktop), and
  the **framework version** comes from the runtime (`Environment.Version`).
- The value is a *normalized runtime descriptor*, not a raw TFM string reconstruction —
  the skia client cannot (and must not) guess whether the head TFM was spelled
  `net10.0-desktop` or `net10.0`; the matching rules below treat those as equivalent.

Deriving the TFM from `HotReloadInfoPath` (which happens to embed
`obj/<Configuration>/<tfm>/…`) was considered and **rejected as a product mechanism**:
output paths are user-customizable. It remains acceptable as a *test-fixture* shortcut.

**Backward compatibility** (old client → new server): when the field is absent, fall back to
`ConfigureServer.MSBuildProperties["TargetFramework"]` (legacy capture). When that is also
missing/empty: keep **all** flavors and emit an explicit warning naming the situation and
its consequence (see D6) — never worse than today's behavior.

### D2 — Global-properties whitelist

`GetWorkspaceProperties` switches from deny-list (`IsValidProperty`) to a **whitelist**.
Properties passed to `MSBuildWorkspace.Create`:

| Property | Why it stays |
|----------|--------------|
| `UnoIsHotReloadHost` (server-set, `True`) | The one marker letting projects/packages adapt their evaluation to the HR workspace. |
| `Configuration` | The only capture that shapes compilation (F5): `DEBUG` constants, `Optimize`, generator HR gates. In practice HR is Debug-only (Release HR was attempted and abandoned; runtime support is gone), but keeping it is free insurance and keeps the workspace aligned with whatever the app was actually built as. |
| `Platform` | Companion of `Configuration`; `AnyCPU` default makes it near-inert, kept for symmetry with the app build. |
| `SolutionFileName`, `SolutionDir`, `SolutionExt`, `SolutionPath`, `SolutionName` | The workspace opens a *project*; these restore the solution context for projects whose evaluation references `$(SolutionDir)`-style anchors. Inert otherwise. |

Everything else is dropped, notably:

- `TargetFramework` / `UnoHotReloadTargetFramework` — replaced by D3. The server stops
  emitting the promotion property entirely (“drop the property”).
- `UnoHotReloadRuntimeIdentifier` — dropped. **Known-limitation note**: the Rider
  2024.3 + Android scenario where the app was built with `RuntimeIdentifier` +
  `AppendRuntimeIdentifierToOutputPath` (RID-suffixed output paths) loses the exact
  baseline-path match and falls back to baseline re-emission at init (relies on
  deterministic compilation for module identity). Revisit if field reports come back.
- `BuildingInsideVisualStudio`, `LangName`, `LangID`, `DevEnvDir`,
  `UseHostCompilerIfAvailable`, `DefineExplicitDefaults`,
  `VSIDEResolvedNonMSBuildProjectOutputs` — VS session echoes; telemetry-only in
  generators (F5); CLI-style evaluation is the more correct semantic for a headless
  design-time build.
- `UnoRemoteControlPort` — only shifts an attribute constant in the workspace's own
  generated `RemoteControl.g.cs`; EnC-neutral (deltas are computed between workspace
  snapshots, both of which carry the same generated content).
- `UnoRuntimeIdentifier` / `UnoWinRTRuntimeIdentifier` — recomputed by the Uno.Sdk during
  evaluation in single-project apps; captured value adds nothing (legacy non-single-project
  apps define them in their project files, which the workspace evaluates anyway).
- `OutputPath`, `IntermediateOutputPath`, `AppendRuntimeIdentifierToOutputPath`,
  `RuntimeIdentifier` — already excluded from globals today; they remain **read** server-side
  (file-watch filtering via `Trim`, diagnostics) from `ConfigureServer.MSBuildProperties`.
- `UnoHotReloadDiagnosticsLogPath` — consumed by the server before workspace creation;
  never needed to be a global property.
- `MSBuild*` — forbidden by Roslyn, unchanged.

**Package-props compatibility**: `buildTransitive/Uno.WinUI.DevServer.props` (the
`UnoIsHotReloadHost`-gated promotion block) ships with *applications* and must keep its
current content for now — an **older** dev-server talking to a newer app still sets
`UnoHotReloadTargetFramework` and needs the promotion. The new server simply never sets the
promoted properties, making the block inert. The props cleanup is a follow-up once servers
predating this spec are out of support.

### D3 — Post-load head-flavor filtering (solution snapshot)

After `OpenProjectAsync` succeeds:

1. **Identify head flavors**: projects whose `FilePath` equals `ConfigureServer.ProjectPath`
   (full-path normalized, `OrdinalIgnoreCase`). One project = nothing to do (fast path,
   including the current happy path for single-TFM heads).
2. **Resolve each flavor's TFM** with `Project.TryGetTargetFramework()` (see D5) and match
   it against the app-reported descriptor (D1) using the **matching rules** below.
3. **Filter the snapshot**: starting from `workspace.CurrentSolution`, remove every
   non-matching head flavor **and every project no longer reachable** from the kept head
   (transitive `ProjectReference` closure — a flushed `net10.0-android` head may have
   dragged in `lib(net10.0-android)` flavor projects that would otherwise keep polluting
   `GetCompilationErrors` and baseline emission).
4. Hand the **filtered snapshot** to the pipeline: initial emit-check
   (`EmitCompilationOutputAsync(solution)`), `WatchHotReloadService` session start, and
   `HotReloadManager.CurrentSolution` (F3/F4). The `MSBuildWorkspace` object remains solely
   the service/dispose owner.
5. `TemporaryWorkspaceAddDetector` (file-add detection re-opens a workspace through the same
   provider) applies the **same filter** to its temporary solution before diffing.

**Matching rules** (normalized comparison):

- Compare *(framework version, platform identifier)*; **ignore platform version**
  (`net10.0-ios26.0` ≡ `net10.0-ios`, `net10.0-android36.0` ≡ `net10.0-android`).
- The skia-desktop descriptor matches both `net10.0-desktop` and plain `net10.0` head
  flavors (a head defines only one of the two; `TryGetTargetFramework` disambiguates
  workspace-side via the `__DESKTOP__` define when present).
- A flavor whose `TryGetTargetFramework` returns `false` (typically: design-time load
  failed — missing workload — leaving no metadata references) is treated as non-matching.

**Failure policy**: if *no* flavor matches (or the descriptor is unavailable, D1 fallback
exhausted), keep **all** flavors and log an explicit warning describing what was reported,
what was found, and the likely consequence (blocked updates from foreign flavors). Never
fail the init on filtering itself.

### D4 — `UnoHotReloadRuntimeIdentifier` retirement

Covered in D2. Recorded here as its own decision per review: the property is flushed, the
Rider/Android RID-in-output-path scenario is documented as a known limitation (baseline
re-emission + deterministic-build assumption), and no replacement mechanism is introduced
in this spec.

### D5 — `TryGetTargetFramework` resolver

The resolver derives a project's TFM from the project's **metadata references** (ref-pack
path classification: `Microsoft.<Platform>.Ref` / base packs) with **preprocessor symbols**
(`__WASM__`, `__DESKTOP__`) as tiebreaker — deliberately never consulting output paths
(user-customizable) nor project names. An existing, field-proven implementation is ported
into the product (`Uno.HotReload/Utils/`) together with its unit tests; the DevServer test
project (linked-source convention) validates the end-to-end behavior against multi-TFM
fixtures.

Known gap to address during the port: the classifier does not recognize
`Microsoft.Windows.SDK.NET.Ref` (WinAppSDK `net10.0-windows10.x` heads resolve as plain
`net10.0`). Windows targets do not use the dev-server metadata-updates path (VS native HR),
but the mapping should either be added or the limitation asserted by a test so the filter
never silently flushes a windows head that a future scenario would rely on.

### D6 — Diagnostics

Init-time logging (all at info/debug level, one warning/error path):

- the runtime descriptor reported by the app (or the fallback used),
- the head flavors discovered (TFM each) and the kept/flushed decision,
- the applied global-properties whitelist,
- warning on keep-all fallback; explicit error when the head project itself cannot be
  found in the loaded solution.

---

## Compatibility matrix

| Client (app packages) | Server (devserver) | Behavior |
|---|---|---|
| new (sends descriptor) | new | D1–D3 nominal path. |
| old (no descriptor) | new | Fallback to captured `TargetFramework`; if absent → keep-all + warning (≈ today's broken case, but with an actionable message instead of cryptic CS0433s). |
| new | old | Old server ignores the new field; legacy promotion chain still applies (props kept, D2). No regression. |
| old | old | Unchanged. |

Other compatibility notes:

- **Release-HR runtime tests** (`UnoForceIncludeProjectConfiguration`): `Configuration`
  stays whitelisted → unchanged.
- **Init latency**: the all-flavor load costs one design-time build per TFM of the head at
  workspace init (only once; per-edit compiles operate on the filtered snapshot). If field
  feedback shows this matters on large heads, a future opt-in fast path can use the
  descriptor to pre-pin the TFM — out of scope here.
- **Memory**: the `MSBuildWorkspace` retains the unfiltered solution until dispose (lazy
  trees, mostly unrealized). Accepted.

---

## Implementation map

| Area | Change |
|---|---|
| `Uno.UI.RemoteControl/HotReload/Messages/ConfigureServer.cs` | New runtime-descriptor field (positional record addition, STJ-compatible). |
| `Uno.UI.RemoteControl/HotReload/ClientHotReloadProcessor.cs` (+ per-flavor partials) | Populate the descriptor at runtime (compile-time platform × runtime framework version). |
| `Uno.UI.RemoteControl.Server.Processors/HotReload/ServerHotReloadProcessor.MetadataUpdate.cs` | `GetWorkspaceProperties` → whitelist (D2); stop emitting `UnoHotReloadTargetFramework` / `UnoHotReloadRuntimeIdentifier`; keep reading `OutputPath`/`IntermediateOutputPath` for `Trim`. |
| `Uno.UI.RemoteControl.Server.Processors/Uno.Roslyn/MsBuild/CompilationWorkspaceProvider.cs` | Post-load filtering (D3): head-flavor detection, descriptor matching, reachability-based snapshot reduction, diagnostics (D6). |
| `Uno.HotReload/HotReloadManager.cs`, `Microsoft/WatchHotReloadService.uno.cs`, `Utils/RoslynExtensions.Emit.cs` | Accept/propagate the filtered `Solution` (workspace kept for services/dispose only). |
| `Uno.HotReload/Diffing/TemporaryWorkspaceAddDetector.cs` | Apply the same filter to temporary workspaces. |
| `Uno.HotReload/Utils/` (phase 2) | `RoslynExtensions.TryGetTargetFramework` promoted from tests (D5). |
| `Uno.UI.RemoteControl/buildTransitive/Uno.WinUI.DevServer.props` | **Untouched** (old-server compatibility); cleanup deferred. |

---

## Test plan

**Unit (DevServer tests, linked sources):**

- `TryGetTargetFramework` port: ref-pack classification (android/ios+version/maccatalyst/
  base packs), `__WASM__`/`__DESKTOP__` tiebreakers, unresolved-references → `false`,
  windows-pack gap (D5).
- Descriptor matching rules: platform-version tolerance, `desktop`/plain-net equivalence,
  case-insensitivity.
- Snapshot filtering: multi-flavor head + per-flavor lib closure → only matched-head closure
  survives; keep-all fallback when nothing matches; single-project no-op fast path.

**Integration (DevServer tests):**

- Multi-TFM head fixture (e.g. `net10.0;net9.0` to stay workload-free on CI): workspace init
  with no TFM globals keeps only the descriptor-matched flavor; edits compile only that
  flavor; foreign-flavor compile errors no longer block.
- Legacy-client simulation: `ConfigureServer` without descriptor but with captured
  `TargetFramework` → same outcome via fallback; with neither → keep-all + warning asserted.
- Roslyn-behavior canary: guard F1 (per-TFM enumeration rule) so a Roslyn upgrade changing
  the loader's contract is caught by tests, not by users.

**Manual QA:**

- The original field repro: VS Code + multi-TFM template (`android;ios;browserwasm;desktop`),
  desktop debug, XAML/C# edits — no android CS0433, HR applies.
- VS and Rider smoke on the same template.
- Rider + Android with RID-suffixed output paths: observe the documented re-emission
  behavior (D4 note).

---

## Out of scope / follow-ups

- Opt-in fast path pre-pinning the TFM at load when the descriptor is available (init
  latency optimization).
- `Uno.WinUI.DevServer.props` promotion-block removal once pre-spec servers are out of
  support.
- `Microsoft.Windows.SDK.NET.Ref` classification in `TryGetTargetFramework` (or asserted
  limitation), per D5.
- uno.vscode-side hardening of `.csproj.user` handling (tracked in uno.vscode; the present
  spec removes the server's dependency on that chain entirely).
