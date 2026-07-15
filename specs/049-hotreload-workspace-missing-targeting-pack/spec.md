# Hot-Reload Workspace: Missing-Targeting-Pack Detection & Restore-Based Recovery

Issue: [unoplatform/uno#23780](https://github.com/unoplatform/uno/issues/23780)
Companion (environment detection/repair): [unoplatform/uno.check#542](https://github.com/unoplatform/uno.check/issues/542)
Builds on: [spec 047](../047-hotreload-workspace-single-tfm/spec.md) (runtime-reported TFM filtering).

## Overview & Objectives

Field case (Linux + WASM, devserver 6.6.151, `TargetFrameworks=net10.0-browserwasm;net10.0-desktop`):
hot reload is blocked at workspace init with the 047 keep-all fallback:

```
[Warn] None of the 2 target frameworks loaded for '…/testerrorfix1.csproj'
(<unresolved: testerrorfix1(net10.0-browserwasm)>, net10.0-desktop) matches the
application's 'net10.0-browserwasm'. Keeping all of them; …
```

The wasm flavor is in the solution (evaluation succeeded — Roslyn's `name(tfm)` naming),
carries documents and 168 metadata references, **but not a single .NET ref-pack reference**
(no `Microsoft.NETCore.App.Ref`). Its compilation has no BCL, `TryGetTargetFramework`
returns `false` (`<unresolved:`), the 047 filter keeps all flavors, and the broken flavor
blocks every update with compilation errors. **Zero `WorkspaceFailed` diagnostics** are
emitted anywhere on this path — the failure is silent end to end.

Root cause (established from the user's design-time binlogs): the dotnet root the Roslyn
BuildHost resolves has a **stale mono-toolchain workload manifest** pinning the wasm chain
to a `Microsoft.NETCore.App` version whose **targeting pack is not on disk**. The SDK falls
back to `PackageDownload` (`_PackageToDownload: Microsoft.NETCore.App.Ref`) — a mechanism
that only materializes during **restore**, which Roslyn's `MSBuildWorkspace` never runs.
With `DesignTimeBuild=true` the SDK's `ResolveTargetingPackAssets` deliberately does *not*
error on the missing pack; it silently yields no `Reference` items. The user's regular
`dotnet build` works because restore materializes the download (and, in the field case, the
terminal resolved a different, correctly-aligned dotnet root — `PATH` vs `DOTNET_ROOT`).

### Key objective

The workspace init must **detect** the "flavor without framework references" state,
**recover** from it when a restore can fix it, and otherwise emit a **diagnostic that names
the cause and the remediation** instead of the generic keep-all warning:

1. Detect the signature after `OpenProjectAsync` (per head flavor).
2. Recover: run `dotnet restore` on the head project in the *same SDK resolution context*
   as the BuildHost, then reload the workspace — once.
3. If still broken, warn with the SDK root in use and the `dotnet workload update`
   remediation.

---

## Verified facts (investigation grounding)

Established from the field binlogs (`MSBUILDDEBUGENGINE` capture of the BuildHost
design-time builds), local reproduction (aligned SDK: both flavors resolve fine on Linux
with Uno.Sdk 6.5 *and* 6.6 — the misalignment is the determining factor, not the OS nor the
Uno version), and Roslyn/dotnet-sdk source review:

| # | Fact | Consequence |
|---|------|-------------|
| F1 | Roslyn's `MSBuildWorkspace` never restores. Design-time builds run `Compile;CoreCompile` with `DesignTimeBuild=true`, `SkipCompilerExecution=true`, `ProvideCommandLineArgs=true`; project references come exclusively from the captured `CscCommandLineArgs` (fallback `ReferencePath`). | Any resolution mechanism that relies on restore-time materialization silently produces nothing at design time. |
| F2 | When a `KnownFrameworkReference`-selected targeting pack version is absent from `<root>/packs/`, `ProcessFrameworkReferences` emits `PackageDownload: Microsoft.NETCore.App.Ref` and `ResolveTargetingPackAssets` yields **zero `Reference` items without any error** under `DesignTimeBuild=true` (by design in dotnet/sdk). | The flavor compiles a BCL-less command line; `Csc` "succeeds" (`SkipCompilerExecution`); no `WorkspaceFailed`, no MSBuild error, nothing in any log. |
| F3 | After a successful restore, the downloaded ref pack lands in the NuGet cache and subsequent design-time builds resolve it from there (this is how regular builds compile in this configuration). | A one-shot `restore` + workspace reload is a *real* recovery, not a mitigation. |
| F4 | The signature is precisely distinguishable workspace-side: the flavor has non-empty `MetadataReferences` but no reference classified as a .NET ref pack by `TryParseRefPackPath` — versus the *design-time-build-failed* state (empty or near-empty references, and Roslyn *does* report those through `WorkspaceFailed`). | Detection can be implemented as a pure `Solution`-snapshot predicate next to the existing 047 helpers; no Roslyn diagnostics needed. |
| F5 | The BuildHost resolves its SDK via `dotnet` from the host process `PATH`/environment (`DOTNET_ROOT`-aware muxer resolution, `global.json` honored via the project directory). | Running `dotnet restore` from the devserver host process, with inherited environment and the project directory as cwd, restores against the **same SDK** the BuildHost uses. |
| F6 | Environmental root cause class: multiple dotnet roots and/or workload manifests out of sync with installed packs (field case: `DOTNET_ROOT=~/.dotnet`, SDK 10.0.203, mono-toolchain manifest 10.0.105 pinning `Microsoft.NETCore.App@10.0.5`, packs containing only `10.0.7`). | The devserver cannot *fix* the environment (that is uno-check's job, uno.check#542); it can recover the workspace (F3) and name the cause. |

---

## Design

### D1 — Missing-framework-references detection

New helper next to the 047 resolver (`Uno.HotReload/Utils/`):

- `Project.HasFrameworkReferences()` — true when at least one `PortableExecutableReference`
  path classifies as a .NET ref pack (reuses `TryParseRefPackPath`, base *or* platform
  packs).
- Detection predicate for a loaded head: a head flavor with `MetadataReferences.Any()` and
  `!HasFrameworkReferences()`. Non-head projects are not scanned (a class-library flavor
  without ref packs would be flushed by 047 filtering anyway when its head goes).

Placement: in `CompilationWorkspaceProvider.CreateWorkspaceAsync`, right after
`OpenProjectAsync` succeeds — *before* the 047 filtering that runs in the caller — because
recovery (D2) must reopen the workspace, which is this provider's responsibility. The check
applies to **all** head flavors, including single-TFM heads: a single-flavor head in this
state never reaches the 047 filter (count == 1 fast path) yet is just as broken (blocked by
BCL-less compilation errors at first update).

### D2 — Restore-based recovery (one shot)

When D1 fires on any head flavor:

1. Dispose the freshly-opened workspace.
2. Run `dotnet restore "<headProjectPath>"`:
   - executable: `dotnet` resolved exactly as the BuildHost resolves it (PATH of the host
     process, environment inherited — F5);
   - working directory: the head project directory (`global.json` honored);
   - no extra global properties (the restore must reproduce what the app's own build
     restore would do; the workspace-only `UnoIsHotReloadHost` marker is deliberately not
     passed);
   - bounded by a timeout (default 120 s) and the init `CancellationToken`;
   - stdout/stderr relayed at verbose level, one info line for start/outcome.
3. Re-run `MSBuildWorkspace.Create` + `OpenProjectAsync` (fresh workspace — Roslyn caches
   the design-time build results per workspace instance).
4. Re-evaluate D1. Recovery is attempted **at most once per init**; the flag lives in the
   `CreateWorkspaceAsync` retry loop alongside the existing
   "app build not yet completed" retry.

If the restore process fails to start (no `dotnet`, non-zero exit), log the outcome and
fall through to D3 with the first workspace re-opened (a failed restore does not abort HR
init — degraded-but-functional beats dead, same policy as 047).

### D3 — Actionable diagnostic

When D1 still fires after D2 (or D2 could not run):

- Warn (replacing nothing — this is *in addition to* the 047 keep-all warning, which stays
  as-is for its own failure mode) with:
  - the flavor(s) concerned (project name incl. Roslyn's `(tfm)` discriminator),
  - the statement that the design-time build produced **no .NET framework references**
    (missing targeting pack at design time),
  - the SDK root in use when determinable — derived from the ref-pack paths of the sibling
    flavors that *did* resolve (`<root>/packs/Microsoft.NETCore.App.Ref/…`); omitted
    otherwise,
  - the remediation: `dotnet workload update` against that SDK root, and a pointer to
    uno-check (uno.check#542).

### D4 — `WorkspaceFailed` visibility (small, related)

The investigation showed `WorkspaceFailed` diagnostics (which *do* fire for the sibling
failure mode "design-time build failed with MSBuild errors") are logged at Verbose in
`CompilationWorkspaceProvider` and never reach users. Buffer the diagnostics during
`OpenProjectAsync`; when the loaded solution ends up with any unresolved head flavor
(D1 signature *or* `TryGetTargetFramework == false`), re-emit the buffered
`WorkspaceDiagnosticKind.Failure` entries at **Warn** (bounded, e.g. first 10). Nominal
loads keep today's quiet behavior.

### Explicit non-goals

- **No "restore always before open"**: rejected for now — adds seconds to every init to
  cover a rare environmental state; D2 pays the cost only when the state is detected.
  Revisit if field reports surface adjacent stale-assets classes (tracked in issue #23780
  as option 3).
- **No filtering "by elimination"**: keeping the unresolved flavor because the resolved
  ones don't match would not help — its compilation has no BCL; without a successful
  restore there is nothing to recover to.
- **No environment mutation**: the devserver never runs `dotnet workload update` itself
  (long, mutating, may require elevation; uno-check owns remediation).

---

## Compatibility

- Server-side only; no protocol change, no client change, no `ConfigureServer` change.
- Nominal init path: one extra pass over head-flavor `MetadataReferences` (negligible);
  no restore, no behavior change.
- Degraded path: one `dotnet restore` + one workspace reload at init (seconds). Strictly
  better than the current outcome (permanently blocked HR).
- Old-client/new-server and new-client/old-server matrices are unaffected (feature is
  entirely inside workspace init).

---

## Implementation map

| Area | Change |
|---|---|
| `Uno.HotReload/Utils/RoslynExtensions.TryGetTargetFramework.cs` | Expose `HasFrameworkReferences(Project)` (reusing `TryParseRefPackPath`); no behavior change to the resolver itself. |
| `Uno.HotReload/Utils/RoslynExtensions.TargetFrameworkFiltering.cs` | D3 message enrichment: when flavors are `<unresolved:` **and** carry references without ref packs, the keep-all warning gains the missing-targeting-pack hint (kept coherent with the D3 warning emitted by the provider). |
| `Uno.UI.RemoteControl.Server.Processors/Uno.Roslyn/MsBuild/CompilationWorkspaceProvider.cs` | D1 detection after `OpenProjectAsync`; D2 one-shot restore + reopen inside the existing retry loop; D4 diagnostic buffering/re-emission; D3 warning. |
| `Uno.UI.RemoteControl.Server.Processors/…` (new) `DotnetRestoreRunner` (or equivalent helper) | Process invocation: muxer resolution mirroring the BuildHost (F5), cwd, timeout, cancellation, output relay. |
| `Uno.UI.RemoteControl.Server.Processors/HotReload/ServerHotReloadProcessor.MetadataUpdate.cs` | Unchanged flow; benefits from the recovered workspace. |

---

## Test plan

**Unit (DevServer tests, linked sources):**

- `HasFrameworkReferences`: ref-pack paths (SDK `packs/` layout and NuGet-cache layout) →
  true; NuGet `lib/`-only reference sets → false; empty references → false.
- Detection predicate: distinguishes *missing-pack* (refs without ref packs) from
  *build-failed* (no refs) states.
- D3 message composition: SDK root extraction from sibling ref-pack paths; message without
  root when no sibling resolved.

**Integration (DevServer tests):**

- **Deterministic missing-pack fixture** (no workloads involved): a head project with
  `<FrameworkReference Update="Microsoft.NETCore.App" TargetingPackVersion="X">` where `X`
  is a valid-but-not-installed patch version, and `NUGET_PACKAGES` redirected to a clean
  temp folder. Restore-less design-time load → D1 fires (reproduces the field state
  CI-deterministically). Then: end-to-end recovery — the D2 restore materializes the
  `PackageDownload` into the temp cache, reload resolves, `TryGetTargetFramework`
  succeeds, 047 filtering restricts normally.
- Restore-failure path: unreachable NuGet source → D2 fails → init completes degraded with
  the D3 warning (assert message contains the remediation and the SDK root).
- Single-TFM head with the same fixture: D1/D2 apply even though 047 filtering is a no-op.
- Canary guarding F2: assert that a restore-less design-time build of the fixture yields a
  project with references but no ref packs — if a future .NET SDK starts erroring (or
  resolving) in this state, the canary flags the behavior change.

**Manual QA:**

- Field scenario replay: misaligned root (`dotnet workload` manifests pinning a
  non-installed targeting pack), VS Code + wasm head on Linux → HR recovers at init
  (restore visible in devserver log), edits apply.
- Aligned environment: no restore triggered, no new log noise.

---

## Out of scope / follow-ups

- Environment detection/repair (multiple dotnet roots, `DOTNET_ROOT`/`PATH` divergence,
  manifest/packs alignment): uno-check, tracked in
  [uno.check#542](https://github.com/unoplatform/uno.check/issues/542).
- Restore-always-before-open option (issue #23780, option 3) — revisit on field feedback.
- Stale `project.assets.json` recovery (different signature: design-time build *fails*
  with NETSDK1004/1005 and surfaces through `WorkspaceFailed`/D4) — same D2 machinery
  could be triggered from those diagnostics later; not wired in this spec.
