# Hot-Reload: Scope the Blocked-Compilation Audit to the Pass's Change-Set

**Repo**: `uno` (Uno.HotReload)
**Created**: 2026-07-23
**Status**: Approved — ready for implementation
**Related**: [spec 053](../053-hotreload-workspace-analyzer-flavor-skew/spec.md) (the
generator-loading skew this audit surfaced), [spec 055](../055-hotreload-noop-pass-dedup/spec.md)
(the no-op pass that lands in this audit), [spec 050](../050-hotreload-updatefile-workspace-gate/spec.md)

## Overview & Objectives

`HotReloadManager.ProcessSolutionChanged` (`src/Uno.HotReload/HotReloadManager.cs`)
ends a pass that produced **no rude edits and no metadata updates** with a
last-chance audit:

```csharp
case (true, true) when GetCompilationErrors(result.Solution, ct) is { IsEmpty: false } compilationErrors:
    _tracker.Output($"Hot reload blocked by {compilationErrors.Length} compilation error(s).");
    outcome = HotReloadOperationResult.Failed;
```

`GetCompilationErrors` iterates **every project of the solution** and reads
`project.TryGetCompilation(...)` — i.e. whatever compilation state happens to be
cached for each project at that instant. Two structural problems:

1. **It judges projects unrelated to the pass.** A pass whose change-set is a single
   file in project A can complete `Failed` because project B — untouched by the pass,
   possibly never compiled by the engine before — reports errors. Observed live: a
   no-op pass over the per-request hot-reload info file completed `Failed` with ~2,400
   errors from seven *unrelated* library projects (root cause of those errors: spec
   053), while the actual user change had already applied successfully. Because the
   info file correlates the pass to the originating `UpdateFile` request (R6 in spec
   050), **the request reported failure for a change that was applied** — the
   user-facing "failed" toast for a successful operation.
2. **`TryGetCompilation` is not a truth source.** It returns cached, possibly partial
   compilation states and never forces a real compile; auditing arbitrary projects on
   those states reports transient artifacts as errors.

Objective: the audit keeps its purpose — *"your edited file does not compile, that is
why there is no delta"* — but only ever judges **the projects owning the pass's
changed files**.

## Requirements

### R1 — audit only the change-set's projects

In the `(rudeEdits.IsEmpty, updates.IsEmpty) == (true, true)` branch, replace the
whole-solution `GetCompilationErrors(result.Solution, ct)` with an overload scoped to
the pass:

```csharp
GetCompilationErrors(result.Solution, files, ct)
```

where `files` is the pass's change-set (the `ImmutableHashSet<string>` already flowing
through `ProcessSolutionChanged`). Project resolution, per file:

- `solution.GetDocumentIdsWithFilePath(file)` → `documentId.ProjectId`;
- plus projects where the file is an **AdditionalDocument**
  (`project.AdditionalDocuments.Any(d => PathComparer.PathEquals(d.FilePath, file))`) —
  XAML and other generator inputs are additional documents, and an edit to one can
  block compilation exactly like a source edit;
- union over all files, distinct.

Rules:

- A file resolving to **no** project contributes nothing (it was already reported via
  `NotifyIgnored`).
- If the resolved set is **empty** (e.g. every file of the pass is outside the
  solution), **skip the audit entirely** and fall through to the `NoChanges` outcome —
  never fail a pass on foreign projects.
- Inside the scoped projects, keep the existing behavior byte-for-byte: same
  `TryGetCompilation` + `GetDiagnostics` walk, same error formatting/colors, same
  WASM cap (`MaxCompilationErrorsPerCycle`), same `Failed` outcome when errors exist.

### R2 — keep the reason visible

The `"Hot reload blocked by N compilation error(s)."` output line must additionally
name the audited projects, e.g.:

`Hot reload blocked by 3 compilation error(s) in MyApp (edited: MainPage.xaml.cs).`

(project names joined with `, `; file names via `Path.GetFileName`, capped to the
first 3 with `+N more` beyond.)

### R3 — do not touch the other outcomes

`Success`, `RudeEdit`, `NoChanges` (reference-equal solution) branches are unchanged.
The pre-existing `FIXME` above the audit (diagnostics not populated into the
operation's `diagnostics`) is explicitly **out of scope** — do not attempt it in this
PR (it needs the dedup discussed in the FIXME).

## Non-goals

- Fixing *why* unrelated projects can carry phantom errors (spec 053).
- Preventing the no-op pass that most often lands in this branch (spec 055).
- Making the audit force real compilations (`GetCompilationAsync`): scoping makes the
  cheap cached read acceptable; forcing compiles here would reintroduce the
  design-time-build starvation documented in spec 052's readiness work.

## Test plan

Unit tests next to the existing manager/updater tests (`src/Uno.HotReload.Tests/`),
using `AdhocWorkspace` (the manager takes a solution provider — follow the pattern of
existing `HotReloadManager`/`WorkspaceGatedFileUpdater` tests):

1. **Foreign errors do not fail the pass**: solution with project A (healthy) and
   project B (a source with a deliberate `CS0103`, compilation realized via
   `GetCompilationAsync` beforehand so `TryGetCompilation` serves it). Process a
   change-set touching only A whose emit yields no updates (identical-content write) →
   outcome must be `NoChanges`, not `Failed`.
2. **Own errors still block**: change-set file belongs to project B (the broken one),
   0 updates → outcome `Failed`, output line names B and the edited file.
3. **AdditionalDocument resolution**: change-set file is an AdditionalDocument of A →
   A is audited (arrange an error in A to observe `Failed`).
4. **Out-of-solution files**: change-set = one unknown path → audit skipped, outcome
   `NoChanges`.

## Resolved decisions

- Skip (rather than whole-solution fallback) when the change-set resolves to no
  project: a pass that touched nothing known can never be "blocked by" anything.
- Scoping is by project, not by document: an edit can break *other* documents of the
  same project, so project-level diagnostics inside the scope are the right grain.
