# Hot-Reload Workspace: Align Analyzer Flavor Selection and Roslyn Version with the SDK Compiler

**Repo**: `uno` (Uno.UI.RemoteControl.Server.Processors / Uno.HotReload)
**Created**: 2026-07-23
**Status**: Approved — ready for implementation
**Related**: [spec 054](../054-hotreload-audit-changeset-scope/spec.md) (audit scoping — the path that *surfaced* this bug), [spec 055](../055-hotreload-noop-pass-dedup/spec.md)

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

In `CompilationWorkspaceProvider.CreateWorkspaceAsync`
(`src/Uno.UI.RemoteControl.Server.Processors/Uno.Roslyn/MsBuild/CompilationWorkspaceProvider.cs`),
add to the `globalProperties` dictionary:

```csharp
// The workspace compiles with the embedded Microsoft.CodeAnalysis, not with the SDK's
// csc: force the analyzer multi-targeting (analyzers/dotnet/roslyn{X.Y} folders) to
// select flavors loadable by the embedded Roslyn. Without this, an SDK newer than the
// embedded Roslyn selects flavors that fail to type-load, and
// AnalyzerFileReference.GetGenerators() silently returns zero generators (missing
// generated code in every compile of the affected projects).
["CompilerApiVersion"] = $"roslyn{typeof(Compilation).Assembly.GetName().Version!.Major}.{typeof(Compilation).Assembly.GetName().Version!.Minor}",
```

- Computed at **runtime** from `typeof(Microsoft.CodeAnalysis.Compilation).Assembly` —
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
  `grep -rn "Microsoft.CodeAnalysis" src/*/[!o]*.csproj` — keep every flavor-pair
  consistent (a mixed 4.x/5.x graph in one host flavor must not ship).

Compatibility notes for the implementing agent:

- `Uno.HotReload/Microsoft/WatchHotReloadService.cs` accesses
  `Microsoft.CodeAnalysis.ExternalAccess.Watch.Api.WatchHotReloadService` **via
  reflection** (method lookup + `ITuple` result decomposition) — resilient to signature
  drift, but **verify against 5.6**: the `Update` field names read by reflection
  (`ModuleId`, `ILDelta`, `MetadataDelta`, `PdbDelta`, `UpdatedTypes`) and the
  constructor arity of the service. If 5.6 renamed/extended them, extend the shim (keep
  reflection, do not take a compile-time dependency).
- `Microsoft.CodeAnalysis.Workspaces.MSBuild` 5.x loads projects through the
  out-of-process BuildHost; smoke-test that `CompilationEnvironment`'s assembly
  resolver registration still applies (it hooks the ALC of
  `CompilationWorkspaceProvider`, unchanged).
- Do NOT bump the net9 flavor to 5.x even if it loads: the alignment target is the
  SDK-9 compiler line, and 4.14 keeps that flavor skew-free.

### R3 — per-project analyzer load-failure logging

Requirement (verbatim from review): *the log must state which project is impacted and
make it clear hot reload will not work on it* — a few lines, not a 2,500-line error dump.

Implementation sketch, in the workspace initialization path (after
`OpenProjectAsync` completes, e.g. at the end of `CreateWorkspaceAsync` or in the
`LoadSolutionFromDisk` continuation in `ServerHotReloadProcessor.MetadataUpdate.cs`):

```csharp
foreach (var project in workspace.CurrentSolution.Projects)
{
    foreach (var reference in project.AnalyzerReferences.OfType<AnalyzerFileReference>())
    {
        // Subscribe BEFORE forcing materialization: the event carries the real
        // load/type-load exception that GetGenerators() otherwise swallows.
        reference.AnalyzerLoadFailed += (_, e) => failures.Add((project, reference, e));
        _ = reference.GetGenerators(LanguageNames.CSharp); // force load once, cached by Roslyn
    }
}
```

- Aggregate and emit **one `reporter.Warn` line per (project, analyzer reference)**,
  de-duplicated across projects sharing the same reference instance, e.g.:

  `Analyzer 'CommunityToolkit.Mvvm.SourceGenerators' (analyzers/dotnet/roslyn5.0) failed to load in the hot-reload workspace (Roslyn 4.14): its generated code will be MISSING — hot reload will NOT work for project 'Contoso.ViewModels' (and any project consuming its generated members).`

  Include: analyzer simple name, the `roslyn{X.Y}` path segment when present in
  `FullPath`, the embedded Roslyn version, and the project name.
- Emit at workspace load (so the session log carries the warning **before** any
  failure), and keep the eager `GetGenerators` call — it is one-time, cached by
  Roslyn, and turns a lazy mid-session failure into a startup signal.
- With R1+R2 in place this log should never fire for the packages above; it exists for
  the next skew (SDK 11 / Roslyn 6, or a package shipping flavors newer than the
  embedded line before we bump).

## Non-goals

- No change to delta emission or EnC semantics: the workspace's Roslyn still emits the
  deltas exactly as today (the baseline is the on-disk assembly metadata; EnC diffs
  source-vs-source against the committed baseline solution — compiler-version skew
  between the app build and the delta emit is supported by design and already the
  shipping configuration).
- No attempt to run analyzers/diagnostics beyond generator loading (R3 loads, it does
  not execute).
- Scoping the full-solution error audit is spec 054, not this one.

## Test plan

1. **Unit — pin format**: assert the computed `CompilerApiVersion` matches
   `roslyn{Major}.{Minor}` of `typeof(Compilation).Assembly` and is non-empty.
2. **Integration (DevServer.Tests)**: a test project graph where a class library uses
   `CommunityToolkit.Mvvm` 8.4.2 `[ObservableProperty]` + `[RelayCommand]` and the head
   references it. Load the workspace via `CompilationWorkspaceProvider` (SDK 10 CI
   image), then `GetCompilationAsync` on the library:
   - without R1 (guard test, optional): expect the CS0759/CS0103 signature;
   - with R1: expect **0 errors** and generated documents from
     `ObservablePropertyGenerator`/`RelayCommandGenerator`
     (`Project.GetSourceGeneratedDocumentsAsync`, group by generated-doc path).
3. **R3 logging**: with a deliberately unloadable analyzer reference (e.g. reference a
   `roslyn5.0` flavor path directly while running the 4.14-line host build, or a
   corrupt dll), assert exactly one warn line per (project, reference) and that it
   names the project.
4. **Manual validation protocol**: on an SDK-10 machine, `dotnet run` a head app whose
   library uses the MVVM Toolkit; edit a `.cs` in the library → the update must apply
   (previously: blocked with the phantom-error wall as soon as any pass compiled the
   library, cf. spec 054).

## Resolved decisions

- Pin computed at runtime (not hardcoded), so R2-style bumps never desynchronize it.
- net9 stays on the 4.x line (alignment with SDK 9's compiler), even though 5.x might
  load: symmetry of the "embed the served SDK's Roslyn line" rule.
- Logging is warn-level, aggregated, startup-time — explicitly not the raw error dump.
