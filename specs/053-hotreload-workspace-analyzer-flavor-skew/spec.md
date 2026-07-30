# Hot-Reload Workspace: Align Analyzer Flavor Selection and Roslyn Version with the SDK Compiler

**Repo**: `uno` (Uno.UI.RemoteControl.Server.Processors / Uno.HotReload)
**Created**: 2026-07-23
**Status**: Implemented in this PR ([#23863](https://github.com/unoplatform/uno/pull/23863))
**Related**: spec 054 — audit scoping, the path that *surfaced* this bug ([PR #23864](https://github.com/unoplatform/uno/pull/23864)) · spec 055 — no-op watcher passes ([PR #23865](https://github.com/unoplatform/uno/pull/23865)). Each spec lands in its own PR: the spec files live on those branches, not on master yet, so they are referenced by PR link rather than by relative path.

## Overview & Objectives

The hot-reload workspace loads projects through `MSBuildWorkspace` with the machine's
SDK, but compiles them with the **NuGet-pinned Microsoft.CodeAnalysis embedded in the
dev-server** (4.13 on the net9 host flavor, 4.14 on net10). Modern analyzer packages
multi-target Roslyn (`analyzers/dotnet/roslyn{X.Y}/…`), and the standard SDK selection
logic picks the flavor matching **the SDK's compiler** (`$(CompilerApiVersion)` — the
.NET 10 SDK's `csc` is Roslyn 5.6 → `roslyn5.6`), not the Roslyn that will actually
*run* the generators.

Verified end-to-end failure (reproduced on a scratch `MSBuildWorkspace` using the exact
global properties of `CompilationWorkspaceProvider`, .NET SDK 10.0.302):

- `CommunityToolkit.Mvvm` **8.4.2** ships `analyzers/dotnet/{roslyn4.0, roslyn4.3, roslyn4.12, roslyn5.0}`.
  Under an SDK-10 design-time build, the selected path is
  `…/analyzers/dotnet/roslyn5.0/cs/CommunityToolkit.Mvvm.SourceGenerators.dll`.
- That assembly cannot type-load against the embedded CodeAnalysis 4.14
  (`ReflectionTypeLoadException`, 48 loader exceptions).
- `AnalyzerFileReference.GetGenerators("C#")` **swallows the failure and returns 0
  generators** — no exception, no diagnostic, no CS8784/CS8785 in the compilation.
- Every workspace compile of a project using `[ObservableProperty]`/`[RelayCommand]`
  is then missing all generated members: measured 229 errors on a single library
  (CS0759 orphaned partial methods, CS0103/CS1061 missing generated properties,
  CS0169/CS0414 "unused" backing fields).
- Generators shipping only ≤ roslyn4.x flavors (Uno.UI.SourceGenerators, PolySharp,
  Microsoft.Extensions.*) load fine — which made the failure look
  generator-specific and hard to diagnose.

Scope of impact: **every dev-server session on a machine with the .NET 10+ SDK, for any
solution using an analyzer package that ships a `roslyn5.x` flavor** (CommunityToolkit.Mvvm
8.4+ today; more packages over time). The failure is invisible until something compiles
the affected projects in the workspace (see spec 054), because the emit pipeline only
validates changed projects and the startup captures EnC baselines from the on-disk
assemblies without compiling anything.

Three deliverables (one PR):

1. **R1 — pin analyzer flavor selection to the embedded Roslyn** (the invariant).
2. **R2 — bump the embedded Roslyn per host flavor**: latest 4.x for net9, latest 5.x
   for net10 (the capability: C# 14 parsing, native `roslyn5.0` flavor loading).
3. **R3 — log analyzer load failures per project** (the observability: the silence is
   what cost the diagnosis).

## Requirements

### R1 — `CompilerApiVersion` pinned to the embedded Roslyn

**As implemented**: the computation lives in `Uno.HotReload/Roslyn/EmbeddedRoslyn.cs`
(internal, `InternalsVisibleTo` the processors and the unit tests — the only reachable
placement for a unit test, since no test project compiles against `Server.Processors`).
`Compilation` below is `Microsoft.CodeAnalysis.Compilation`; the assembly identity is read
once and cached, and a hypothetical unversioned assembly throws a diagnosable error instead
of a `NullReferenceException`:

```csharp
internal static Version Version { get; } = typeof(Compilation).Assembly.GetName().Version
	?? throw new InvalidOperationException("The embedded Microsoft.CodeAnalysis assembly has no version.");

internal static string CompilerApiVersion { get; } = $"roslyn{Version.Major}.{Version.Minor}";
```

`CompilationWorkspaceProvider.CreateWorkspaceAsync`
(`src/Uno.UI.RemoteControl.Server.Processors/Uno.Roslyn/MsBuild/CompilationWorkspaceProvider.cs`)
forwards it in the `globalProperties` dictionary:

```csharp
// The workspace compiles with the embedded Microsoft.CodeAnalysis, not with the SDK's
// csc: force the analyzer multi-targeting (analyzers/dotnet/roslyn{X.Y} folders) to
// select flavors loadable by the embedded Roslyn.
["CompilerApiVersion"] = EmbeddedRoslyn.CompilerApiVersion,
```

- Computed at **runtime** from the loaded `Microsoft.CodeAnalysis` assembly —
  never hardcode the version: it must follow R2's bumps (and any future ones) for free.
- MSBuild semantics making this reliable: a **global** property is immutable for the
  evaluation; the unconditional `<CompilerApiVersion>roslyn5.6</CompilerApiVersion>`
  assignment in the SDK's `Microsoft.Managed.Core.CurrentVersions.targets` is ignored
  when the property comes in as a global. Empirically verified: with the pin, the
  selected CommunityToolkit path flips to `…/roslyn4.12/cs/…`, `GetGenerators` returns
  7 generators, and the 229-error library compiles **clean** (0 errors).
- Note: `AssemblyName.Version` of Microsoft.CodeAnalysis 4.14 is `4.14.x` (assembly
  version tracks the package minor on this line). Add a unit test asserting the
  computed string matches `^roslyn\d+\.\d+$` and equals the package line the project
  references (guards against an assembly-versioning scheme change on a future bump).

### R2 — bump the embedded Roslyn per host flavor

Alignment rule: **each host flavor embeds the latest Roslyn line of the SDK it serves**
(the host flavor is already selected per SDK by `GetRemoteControlHostPath` /
`BundledNETCoreAppTargetFrameworkVersion`):

- net9 flavor (SDK 9, csc = Roslyn 4.x line): bump `4.13.0` → **`4.14.0`** (latest 4.x).
- net10 flavor (SDK 10, csc = Roslyn 5.x line): bump `4.14.0` → **`5.6.0`** (latest 5.x
  stable at the time of writing — check nuget.org for a newer 5.x when implementing).

Files with per-TFM conditional `PackageReference`s to update (all
`Microsoft.CodeAnalysis.*` pins in each):

- `src/Uno.UI.RemoteControl.Server.Processors/Uno.UI.RemoteControl.Server.Processors.csproj`
  (net9.0 group → 4.14.0, net10.0 group → 5.6.0)
- `src/Uno.HotReload/Uno.HotReload.csproj` (same)
- Sweep for other `Microsoft.CodeAnalysis` pins in the dev-server component set
  (`Uno.UI.RemoteControl.Host`, `Uno.UI.RemoteControl.Messaging`, DevServer tests):
  `grep -rn --include='*.csproj' "Microsoft.CodeAnalysis" src/` — keep every flavor-pair
  consistent (a mixed 4.x/5.x graph in one host flavor must not ship). Sweep result: the
  two csproj above are the only dev-server projects pinning `Microsoft.CodeAnalysis.*`
  (other hits — `Uno.Analyzers`, `Uno.WinAppSDKSyncGenerator`, analyzer test projects —
  are outside the dev-server component set and intentionally untouched).

Compatibility notes for the implementing agent:

- `Uno.HotReload/Microsoft/WatchHotReloadService.cs` accesses
  `Microsoft.CodeAnalysis.ExternalAccess.Watch.Api.WatchHotReloadService` **via
  reflection** (method lookup + `ITuple` result decomposition) — resilient to signature
  drift, but **verify against 5.6**: the `Update` field names read by reflection
  (`ModuleId`, `ILDelta`, `MetadataDelta`, `PdbDelta`, `UpdatedTypes`) and the
  constructor arity of the service. If 5.6 renamed/extended them, extend the shim (keep
  reflection, do not take a compile-time dependency).
  **RESOLVED during implementation**: Roslyn *removed* `ExternalAccess.Watch` from
  `Microsoft.CodeAnalysis.Features` between 5.0 and 5.3. The shim now targets its twin,
  `Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api.UnitTestingHotReloadService`,
  whose shape is byte-for-byte identical from 4.14 to 5.6 (verified by reflection on
  4.14.0 / 5.0.0 / 5.3.0 / 5.6.0): the only deltas vs Watch are the capabilities moving
  from the constructor to `StartSessionAsync` and an explicit `commitUpdates` flag on
  emit — `true` reproduces Watch's implicit commit-on-ready (confirmed against the
  Roslyn source). Empirically validated on both 4.14.0 and 5.6.0: session start + a
  **real EnC delta emission** (on-disk baseline dll+pdb) + the five `Update` fields.
- Companion compile-time pins required by `Workspaces.MSBuild` 5.6.0 in the net10.0
  groups (all `ExcludeAssets="runtime"` where already the case): `Microsoft.Build*`
  17.7.2/17.8.43 → **18.0.2**, `Microsoft.Extensions.Logging*` 9.0.0 → **10.0.1**.
- `Workspace.WorkspaceFailed` (event) is `[Obsolete]` (error under warnaserror) in 5.x:
  use `RegisterWorkspaceFailedHandler` behind `#if NET10_0_OR_GREATER` — the API does
  not exist on the 4.x line.
- `Microsoft.CodeAnalysis.Workspaces.MSBuild` 5.x loads projects through the
  out-of-process BuildHost; smoke-test that `CompilationEnvironment`'s assembly
  resolver registration still applies (it hooks the ALC of
  `CompilationWorkspaceProvider`, unchanged).
- Do NOT bump the net9 flavor to 5.x even if it loads: the alignment target is the
  SDK-9 compiler line, and 4.14 keeps that flavor skew-free.

### R3 — per-project analyzer load-failure logging

Requirement (verbatim from review): *the log must state which project is impacted and
make it clear hot reload will not work on it* — a few lines, not a 2,500-line error dump.

**As implemented** — `EmbeddedRoslyn.WarnOnAnalyzerLoadFailures(Solution, IReporter)`
(`Uno.HotReload/Roslyn/EmbeddedRoslyn.cs`), called on the solution snapshot the hot-reload
manager is initialized with (`ServerHotReloadProcessor.MetadataUpdate`), after the R4
loader rewiring — running it on the workspace's own solution would force-load through the
default loaders R4 exists to replace. Two passes:

1. Over the **distinct** `AnalyzerFileReference`s of the solution
   (`AnalyzerFileReference` equality is path+loader based, so a reference shared by N
   projects is forced **once**): subscribe a named handler to `AnalyzerLoadFailed`
   *before* forcing `GetGenerators(LanguageNames.CSharp)` (the event carries the real
   load/type-load exception that `GetGenerators()` otherwise swallows; the forced load is
   one-time, cached by Roslyn), unsubscribe in a `finally` (no handler accumulation across
   reloads), and keep the **first** failure per reference (`Dictionary<AnalyzerFileReference,
   AnalyzerLoadFailureEventArgs>.TryAdd`).
2. Over the projects: emit exactly **one `reporter.Warn` per (project, failed reference)**
   pair — per-project granularity is deliberate (the requirement is naming every impacted
   project); the *load work and failure capture* are what get deduplicated, e.g.:

  `Analyzer 'CommunityToolkit.Mvvm.SourceGenerators' (analyzers/dotnet/roslyn5.0) failed to load in the hot-reload workspace (Roslyn 4.14): its generated code will be MISSING — hot reload will NOT work for project 'Contoso.ViewModels' (and any project consuming its generated members).`

  Include: analyzer simple name, the `roslyn{X.Y}` path segment when present in
  `FullPath`, the embedded Roslyn version, the project name, and the captured failure
  (`AnalyzerLoadFailureEventArgs` error code, message and exception — the exception is
  what made R4's root cause diagnosable from a session log).
- Emit at workspace load (so the session log carries the warning **before** any
  failure), and keep the eager `GetGenerators` call — it is one-time, cached by
  Roslyn, and turns a lazy mid-session failure into a startup signal.
- With R1+R2 in place this log should never fire for the packages above; it exists for
  the next skew (SDK 11 / Roslyn 6, or a package shipping flavors newer than the
  embedded line before we bump).

### R4 — analyzers must load in COLLECTIBLE contexts

Requirement (from runtime validation): the `When_HotReloadScenario` runtime test hung the
`Tests - Desktop Skia Windows` CI job at its 60-minute limit — every hot-reload compile of
the secondary app failed with the missing-generated-code wall (~90 errors, no
`InitializeComponent`), i.e. the exact disease R1 targets, still present at runtime.

Root cause — a Roslyn 4.x→5.x behavior change colliding with the dev-server's isolation
model:

- Roslyn **4.x** created its per-directory analyzer load contexts with
  `isCollectible: true`; Roslyn **5.x** creates them **non-collectible**
  (`AnalyzerAssemblyLoader.DirectoryLoadContext`, verified by decompilation of 4.14.0 vs
  5.6.0).
- The dev-server hosts its hot-reload processors — embedded Roslyn included — in a
  **collectible per-application `AssemblyLoadContext`**
  (`DefaultRemoteControlProcessorFactory`, `isCollectible: true` so a disconnected app's
  processors can unload).
- The runtime forbids a non-collectible assembly from referencing a collectible one: the
  moment an analyzer's `Microsoft.CodeAnalysis` reference is bound (type materialization
  in `GetAnalyzersForTypeNames`), the load dies with `NotSupportedException`
  (*"A non-collectible assembly may not reference a collectible assembly"*), surfaced as
  `[UnableToCreateAnalyzer] Could not load … 'Microsoft.CodeAnalysis, Version=4.x' —
  Operation is not supported (0x80131515)`.
- Net effect on the 5.6 embed: **every** analyzer fails to load — the in-box SDK
  generators and `Uno.UI.SourceGenerators` alike — so every workspace compile misses all
  generated code. R1's flavor pin is correct but moot when nothing can load at all.

**As implemented** — `CollectibleAnalyzerAssemblyLoader`
(`Uno.HotReload/Roslyn/CollectibleAnalyzerAssemblyLoader.cs`), an
`IAnalyzerAssemblyLoader` restoring the 4.x semantics under any embedded Roslyn:

- one **collectible** `AssemblyLoadContext` per analyzer directory (a collectible assembly
  may reference both collectible and non-collectible ones);
- assemblies already loaded in the embedded Roslyn's own context are **unified to that
  exact instance** (an analyzer built against `Microsoft.CodeAnalysis` 4.x binds to the
  loaded 5.x, mirroring Roslyn's own compiler-context redirect), everything else resolves
  from the analyzer's directory, its registered dependency locations, then the default
  context;
- analyzer files are **shadow-copied** (per path+timestamp, under
  `%TMP%/uno-devserver/analyzers`) so the originals never get locked while the IDE or a
  build rebuilds them — parity with Roslyn's shadow-copying loader.

Wired as a pure snapshot transform, `Solution.WithCollectibleAnalyzerReferences()`,
applied with the other snapshot transforms when the hot-reload manager (re)loads the
solution — both the initial load and the temporary added-file discovery workspaces go
through the same delegate. `Workspace.TryApplyChanges` was deliberately NOT used: applying
analyzer-reference changes through `MSBuildWorkspace` writes them into the user's
`.csproj`.

### R5 — restore Watch's implicit `AddExplicitInterfaceImplementation` capability grant

With R4 in place the generators load and ordinary hot reloads work end-to-end, but every
ResourceDictionary/DataTemplate scenario still failed on the 5.x embed — the update was
rejected as a rude edit (`ENC0106: Updating a reloadable type … requires restarting …`,
`CS9346: Update requires emitting explicit interface implementation …`) and every
subsequent update of the session then timed out (the server commits its EnC baseline while
the application never applied the update, and the skew cascades through the whole run).

Root cause — NOT a Roslyn 4.x→5.x analysis change: the entire gate chain
(`GrantNewTypeDefinition`, the capabilities grantor and parser, `IsReloadable`,
`HasExplicitlyImplementedInterfaceMember`) is byte-identical between the shipped 4.14 and
5.6 binaries. The chain of facts:

- Since Roslyn 4.10/4.11 (dotnet/roslyn#73265, 2024) the EnC analyzer refuses to Replace a
  reloadable type that has an explicitly-implemented interface member unless the
  `AddExplicitInterfaceImplementation` capability is granted. The gate protects .NET
  Framework (adding an InterfaceImpl row there can crash with an access violation); the
  capability is reported by **no** runtime — net10 CoreCLR grants
  `Baseline … NewTypeDefinition … AddFieldRva`, nothing more.
- The **same PR** added the compensation for the runtimes that do support the operation:
  `WatchHotReloadService.AddImplicitDotNetCapabilities()` grants the capability on top of
  whatever the application reports ("available by default on runtimes supported by
  dotnet-watch: .NET and Mono"). Every Watch-based host — dotnet-watch itself, and this
  server's vendored shim — therefore kept replacing those types successfully on 4.x. The
  scenario was never broken, and never runtime-unsupported, on .NET/Mono.
- Every XAML ResourceDictionary singleton the Uno generator emits is exactly the gated
  shape: `[CreateNewOnMetadataUpdate] internal sealed class … :
  IXamlResourceDictionaryProvider` with an explicit
  `IXamlResourceDictionaryProvider.GetResourceDictionary()` implementation
  (`XamlFileGenerator`, singleton emission).
- Roslyn removed `WatchHotReloadService` from `Microsoft.CodeAnalysis.Features` between
  5.0 and 5.3 (it moved to the `ExternalAccess.HotReload` assembly compiled into
  dotnet-watch, and the implicit grant moved with it into dotnet/sdk's `HotReloadClient`,
  where it still lives today). The R2 bump therefore re-targeted the shim to
  `UnitTestingHotReloadService` — which forwards the capabilities **verbatim**. The
  implicit grant silently disappeared in the move, and the 2024 gate started firing.

Fix — the shim now makes the grant itself: `WatchHotReloadService.AddImplicitCapabilities()`
appends `AddExplicitInterfaceImplementation` to the application-reported capabilities
before `StartSessionAsync`, restoring the 4.x Watch behavior and matching what dotnet-watch
does on 5.x (permalinks to both reference implementations — Roslyn 4.x
`AddImplicitDotNetCapabilities` and dotnet/sdk `HotReloadClient` — are in the shim's doc).
CS9346, the 5.x emit-layer twin of the gate, reads the same session capabilities and is
lifted by the same grant.

Validation (local Skia desktop runtime tests, R5 in place): the rude edits are gone from
full-class runs — 0×ENC0106 / 0×CS9346 (the pre-R5 red runs showed 20–60 ENC0106) — and
reloadable-type updates now emit and apply: the DataTemplate scenario's update+undo and
the first AppResources update all apply end-to-end. `Uno.HotReload.Tests`: 116/116,
including the new grant test.

### Known issue (follow-up) — CoreCLR rejects the 4th generation of a mixed replace sequence

One failure mode remains, now precisely scoped and NOT capability-related: in a session
that replaces the DataTemplate page twice (update+undo) and then AppResources twice, the
**4th** delta (the AppResources undo) is rejected by the runtime —
`MetadataUpdater.ApplyUpdate` throws "The assembly update failed" (generic
`COR_E_INVALIDOPERATION`; the native HRESULT is not surfaced). The same AppResources
update+undo pair applied **without** the DataTemplate generations before it succeeds
end-to-end.

Forensic evidence (all artifacts preserved, see `hr-enc-repro` bundle):

- The rejected generation-4 delta is **structurally clean**: SRM-level diff of its
  EncLog/EncMap against the accepted generation-2 delta of the isolated run shows
  identical operation sequences and uniform aggregate renumbering (every table shifted by
  exactly the rows the extra generations added; every `AddParameter` parent is a method
  the delta itself adds). Its relationship to its predecessor is byte-shape-identical to
  the accepted pair's.
- The rejection reproduces **standalone** — a 30-line console harness
  (`Assembly.LoadFrom` + `MetadataUpdater.ApplyUpdate`, no dev-server, no Uno hot-reload
  machinery) replaying the four dumped deltas against the baseline assembly: generations
  1–3 apply, generation 4 fails. This is the escalation package for dotnet/runtime (the
  suspected defect is in CoreCLR's EnC bookkeeping for later generations of interleaved
  type replacements; a checked runtime names the exact failing validation).
- 4.x never hit this because the whole path was different only in volume: the Watch-based
  4.14 embed produced the same logical sequence and CoreCLR applied it — narrowing the
  trigger to the 5.6-emitted delta *content* interacting with runtime state is exactly
  what the standalone repro enables.

Secondary effect (ours, to harden as a follow-up): the server commits its EnC baseline at
emit time regardless of whether the application applied the update
(`UnitTestingHotReloadService.EmitSolutionUpdateAsync(commitUpdates: true)`, same net
behavior as the historical Watch service). After the first runtime rejection the server
and the application permanently disagree on the baseline, so every subsequent delta of the
session is mis-based and rejected — one runtime rejection cascades into a fully poisoned
session (the "105 rejections" pattern of full-suite runs). Follow-up: tie the baseline
commit to the client's apply acknowledgment (or resync/restart the session on apply
failure) so a single rejected update degrades one reload instead of the whole session.

## Non-goals

- No change to delta emission or EnC semantics: the workspace's Roslyn still emits the
  deltas exactly as today (the baseline is the on-disk assembly metadata; EnC diffs
  source-vs-source against the committed baseline solution — compiler-version skew
  between the app build and the delta emit is supported by design and already the
  shipping configuration).
- No attempt to run analyzers/diagnostics beyond generator loading (R3 loads, it does
  not execute).
- Scoping the full-solution error audit is spec 054, not this one.

## Test plan — status as implemented

1. **Unit — pin format**: **done** — `Uno.HotReload.Tests/Roslyn/Given_EmbeddedRoslyn.cs`,
   2 tests: shape (`^roslyn\d+\.\d+$`) + equality with the loaded assembly's
   `major.minor`, and a **package-line guard** comparing against the leading
   `AssemblyInformationalVersion` (the flavor folders are named after the *package*
   version — this test fails if a future Roslyn changes its assembly-versioning scheme,
   the regression the review called out as required red/green coverage at the unit level).
2. **Integration (workspace-level red/green)**: **not automated in this PR** — no test
   project compiles against `Server.Processors` (`DevServer.Tests` references it with
   `ReferenceOutputAssembly="false"` and validates through a spawned host), so hosting
   `CompilationWorkspaceProvider` in a test needs a new SDK-pinned fixture graph +
   harness; tracked as a follow-up. The red/green evidence for R1 is the reproducible
   standalone `MSBuildWorkspace` probe used for the diagnosis (same global properties as
   the provider, SDK 10.0.302, real MVVM-Toolkit project graph): **red** without the pin
   (roslyn5.0 flavor selected, 0 generators, 229 errors), **green** with the pin on the
   4.14 embed (7 generators, 0 errors), **green** again on the 5.6.0 embed (generators
   produce their documents, 0 errors / 0 CS8784-CS8785).
3. **R3 logging**: **done** — 2 tests in `Given_EmbeddedRoslyn`: a corrupt analyzer under
   a `roslyn9.9` flavor-style folder shared by **two** projects yields exactly one
   warning per project (naming the project, the analyzer, the flavor segment, the
   embedded Roslyn version and the no-hot-reload consequence); a loadable reference
   yields none.
4. **Manual validation protocol**: on an SDK-10 machine, `dotnet run` a head app whose
   library uses the MVVM Toolkit; edit a `.cs` in the library → the update must apply
   (previously: blocked with the phantom-error wall as soon as any pass compiled the
   library, cf. spec 054).

5. **R4 loader**: **done** — `Uno.HotReload.Tests/Roslyn/Given_CollectibleAnalyzerAssemblyLoader.cs`,
   4 tests: contexts are collectible and shared per directory; the original file stays
   unlocked after load (shadow copy: rewrite + delete succeed); compiler assemblies unify
   to the compiler context's exact instance **including when that context is collectible**
   (the dev-server scenario, simulated with a collectible ALC hosting
   `Microsoft.CodeAnalysis`); `WithCollectibleAnalyzerReferences()` swaps the loader while
   preserving `FullPath` and the swapped reference force-loads warning-free.
6. **R4 runtime red/green**: reproduced the CI hang locally (Skia desktop runtime tests,
   `Given_HotReloadWorkspace`): **red** without the loader — every analyzer fails with
   `[UnableToCreateAnalyzer] … Operation is not supported`, every hot-reload compile shows
   the missing-generated-code wall; **green** with it — zero analyzer load failures, the
   generators emit, metadata updates flow.
7. **R5 grant**: **done** — `Uno.HotReload.Tests/Microsoft/Given_WatchHotReloadService.cs`
   asserts the .NET 10 CoreCLR capability string gets `AddExplicitInterfaceImplementation`
   appended (order preserved). Runtime red/green: pre-R5 full-class runs show 20–60
   ENC0106 + CS9346 and every ResourceDictionary/DataTemplate scenario fails; with R5,
   0×ENC0106 / 0×CS9346 and reloadable-type updates apply (DataTemplate update+undo,
   AppResources update).
8. **Known-issue evidence (4th-generation rejection)**: the remaining failure is isolated
   to a standalone `MetadataUpdater.ApplyUpdate` replay (baseline + 4 dumped deltas,
   generations 1–3 apply, 4 fails; the same AppResources pair without the DataTemplate
   generations applies) — the dev-server is out of the loop, the delta is SRM-clean, and
   the repro bundle is ready for a dotnet/runtime escalation.

Additional coverage from the implementation pass: the retargeted EnC shim was validated
by emitting a **real delta** (on-disk baseline, IL/metadata/PDB + updated types) against
both 4.14.0 and 5.6.0, and the full existing `Uno.HotReload.Tests` suite (116 tests,
including the R5 capability-grant test) runs green on the net10/Roslyn 5.6 flavor.

## Resolved decisions

- Pin computed at runtime (not hardcoded), so R2-style bumps never desynchronize it.
- net9 stays on the 4.x line (alignment with SDK 9's compiler), even though 5.x might
  load: symmetry of the "embed the served SDK's Roslyn line" rule.
- Logging is warn-level, aggregated, startup-time — explicitly not the raw error dump.
