# Hot-Reload Handler: Invoke on Every Outcome (not success-only)

## Overview & Objectives

`HotReloadManager.ProcessSolutionChanged` only invokes `IHotReloadHandler` on the
**success** path — the cycle that produces a non-empty metadata-update (EnC delta) set.
Every other terminal outcome (`NoChanges`, `RudeEdit`, `Failed`) returns early **before**
the handler is reached. As a result, anything a handler must do that is **not** carried by
the IL/metadata delta — most importantly *side-effects derived from the
`SolutionUpdateResult`* — is silently skipped on no-delta cycles.

This spec proposes three changes to `ProcessSolutionChanged`:

1. **Invoke the handler on every terminal outcome**, passing the `HotReloadOperationResult`
   so handlers can react per-outcome — replacing the success-only `SendAsync`.
2. **Commit `CurrentSolution = result.Solution` unconditionally**, before any early return,
   so solution mutations the updater performed (e.g. rebound references) survive cycles that
   don't emit a delta.
3. **Wrap the handler invocation in `try`/`catch`** and report a handler failure through
   `hotReload.Complete(Failed, …)` rather than letting it surface as a generic
   `InternalError`.

### Key objective

Make the handler a faithful observer of the **whole** hot-reload cycle, so a custom
`ISolutionUpdater` + `IHotReloadHandler` pair can perform delta-independent work
(notably staging resolved package assemblies) on the cycle that actually produces that
state — without resorting to predictive workarounds.

---

## Motivating scenario (generic)

A downstream consumer hosts an inner application in a dedicated `AssemblyLoadContext` (ALC)
and plugs a **custom `ISolutionUpdater`** into `HotReloadManager` via the third
`CreateAsync` overload. That updater:

- re-evaluates an edited `.csproj` when a `PackageReference` is added,
- resolves the package (and its transitive graph), rebinds the resulting metadata/analyzer
  references onto the solution, and
- returns a `SolutionUpdateResult` **subtype** carrying the resolved packages (surfaced to
  the handler through `HotReloadUpdate.SolutionUpdate`).

The consumer's `IHotReloadHandler` then **stages** those resolved assemblies onto the inner
app's ALC probe directory, so that when a later edit references a type from the new package,
the ALC resolves it at runtime.

### The gap

Adding a `PackageReference` is a **references-only** change: it mutates the solution
(new metadata references) but produces **no IL/metadata delta**. In
`ProcessSolutionChanged`:

- `result.Solution != originalSolution` (the references were rebound), so the
  "no changes" check at **L167** passes.
- `EmitSolutionUpdateAsync` (**L180**) returns an **empty** `updates` set (no delta).
- The branch at **L188** (`rudeEdits.IsEmpty && updates.IsEmpty`) treats the cycle as
  `NoChanges` (or `Failed` if there are compile errors) and **returns at L201**, *before*
  the handler call at **L223**.

So the resolved packages on `SolutionUpdateResult` **never reach the handler on the cycle
that resolved them**. The handler runs only on the *next* cycle that emits a delta — by
which point the updater typically reports zero newly-resolved packages. The assembly is bound
to the compile workspace (code/XAML compiles) but is never staged onto the running app's
probe path, so instantiating a type from the new package throws `FileNotFoundException` /
`TargetInvocationException` at runtime.

Today a consumer can only work around this by **carrying resolved packages forward** to a
later delta-producing cycle. That workaround is a *prediction*: from inside `UpdateAsync`
the updater cannot observe whether the cycle it is in will actually ship a delta downstream,
so the carry/flush decision is necessarily heuristic. The proper fix is to let the handler
see the no-delta cycle and act on it directly.

---

## Current behavior (reference)

`src/Uno.HotReload/HotReloadManager.cs`, `ProcessSolutionChanged` (≈L143-226). Terminal
exits, in order:

| # | Location | Condition | Completion | Handler called? |
|---|----------|-----------|------------|-----------------|
| 1 | L161 | `result.Diagnostics` has errors | `Failed` | **no** |
| 2 | L167 | `result.Solution == originalSolution` | `NoChanges` | **no** |
| 3 | L188 | `rudeEdits.IsEmpty && updates.IsEmpty` | `NoChanges` / `Failed` | **no** |
| 4 | L204 | `!rudeEdits.IsEmpty` | `RudeEdit` | **no** |
| 5 | L217 | otherwise (delta present) | `Success` | **yes** (`SendAsync`) |

`CurrentSolution = result.Solution` is assigned at **L177** — *after* exits 1 and 2, so those
paths never commit the updated solution. The handler call (**L223**) is not guarded; a handler
exception propagates to the `ProcessFileChanges` catch (**L134**) and is reported as
`InternalError`.

---

## Proposed changes

### 1. Invoke the handler on every terminal outcome, with the result code

Replace the success-only method on `IHotReloadHandler`:

```csharp
// before
ValueTask SendAsync(HotReloadUpdate update, CancellationToken ct);

// after
ValueTask OnHotReloadAsync(HotReloadOperationResult result, HotReloadUpdate update, CancellationToken ct);
```

`ProcessSolutionChanged` builds a `HotReloadUpdate` on **every** terminal path and invokes the
handler with the computed `HotReloadOperationResult` before completing the operation:

- `Deltas` is empty on every non-success path.
- `Diagnostics` carries the rude-edit / compile diagnostics where applicable.
- `SolutionUpdate` (hence `SolutionUpdate.Packages` for a custom subtype) is populated on all
  paths reached after `UpdateAsync`.

This lets a handler:

- stage delta-independent state (e.g. resolved package assemblies) on a `NoChanges`
  (no-delta) cycle — removing the need for any carry-forward heuristic in the consumer;
- observe `RudeEdit` / `Failed` / `NoChanges` for its own state or UI (e.g. surface that an
  edit did not apply).

Handlers that only care about success guard at the top:

```csharp
public ValueTask OnHotReloadAsync(HotReloadOperationResult result, HotReloadUpdate update, CancellationToken ct)
{
    if (result is not HotReloadOperationResult.Success && update.SolutionUpdate is not /* subtype with work to do */)
    {
        return ValueTask.CompletedTask;
    }
    // …
}
```

Outcomes that invoke the handler: `Success`, `NoChanges`, `RudeEdit`, `Failed`.
`Aborted` / `InternalError` remain manager/transport-level (see §3).

#### Sketch

```csharp
// after UpdateAsync + NotifyIgnored + the unconditional CurrentSolution commit (§2):

HotReloadOperationResult outcome;
var deltas = ImmutableArray<Update>.Empty;
var diagnostics = ImmutableArray<Diagnostic>.Empty;

if (result.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
{
    outcome = HotReloadOperationResult.Failed;
    diagnostics = result.Diagnostics;
}
else if (result.Solution == originalSolution)
{
    outcome = HotReloadOperationResult.NoChanges;
}
else
{
    var (updates, hotReloadDiagnostics) = await _watchService.EmitSolutionUpdateAsync(result.Solution, ct).ConfigureAwait(false);
    var rudeEdits = hotReloadDiagnostics.RemoveAll(d => d.Severity == DiagnosticSeverity.Warning || !d.Descriptor.Id.StartsWith("ENC", StringComparison.Ordinal));

    if (!rudeEdits.IsEmpty)
    {
        outcome = HotReloadOperationResult.RudeEdit;
        diagnostics = hotReloadDiagnostics;
    }
    else if (updates.IsEmpty)
    {
        outcome = GetCompilationErrors(result.Solution, ct).IsEmpty
            ? HotReloadOperationResult.NoChanges
            : HotReloadOperationResult.Failed;
        diagnostics = hotReloadDiagnostics;
    }
    else
    {
        outcome = HotReloadOperationResult.Success;
        deltas = updates;
        diagnostics = hotReloadDiagnostics;
    }
}

var update = new HotReloadUpdate(files, changeSet, result, diagnostics, deltas);
// → §3 wraps the handler call and completion
```

### 2. Commit `CurrentSolution = result.Solution` before any early return

Hoist the assignment to immediately after `UpdateAsync` / `NotifyIgnored`, ahead of every
terminal branch:

```csharp
var result = await _solutionUpdater.UpdateAsync(originalSolution, changeSet, ct).ConfigureAwait(false);
hotReload.NotifyIgnored(result.IgnoredChanges.GetAllPaths());

// Commit unconditionally: an updater may have rebound metadata/analyzer references
// (e.g. newly resolved packages) onto result.Solution. If a cycle exits early without
// committing, those references are lost and the next cycle restarts from the stale
// originalSolution — dropping the resolved references from the workspace.
workspace.CurrentSolution = result.Solution;
```

This generalizes the intent already stated by the comment at **L175-176** ("No matter if the
build will succeed or not, we update the `_currentSolution`") to the early-exit paths.

- The `result.Solution == originalSolution` branch becomes a no-op assignment; the condition
  still compares against the **captured** `originalSolution`, so the branch is unchanged.
- **Requirement on updaters:** `result.Solution` must be coherent even when `result.Diagnostics`
  carries errors — an updater must not return a half-applied / destructive diff. (The built-in
  `SolutionUpdater` and well-behaved custom updaters already skip the diff/apply when a project
  re-evaluation errors, leaving the project at its previous references.)

### 3. Guard the handler call; report handler failure as `Failed`

The handler now performs real side-effects (staging assemblies, applying deltas — possibly
across a thread/process boundary in worker-hosted scenarios). A handler exception should
surface as a hot-reload **failure** for that operation, distinct from a manager-internal fault:

```csharp
try
{
    await _handler.OnHotReloadAsync(outcome, update, ct).ConfigureAwait(false);
}
catch (OperationCanceledException)
{
    throw; // cancellation is not a failure
}
catch (Exception e)
{
    await hotReload.Complete(HotReloadOperationResult.Failed, e, diagnostics).ConfigureAwait(false);
    return;
}

await hotReload.Complete(outcome, diagnostics: diagnostics).ConfigureAwait(false);
```

- `OperationCanceledException` is caught first and rethrown (most-specific-first ordering).
- Any other handler exception completes the operation as `Failed` carrying the exception, so
  consumers observing operation results see a real failure rather than a clean reload. A
  handler that throws while staging on a would-be `Success` cycle thus correctly yields
  `Failed` — the reload did not take effect on the consumer side.
- The outer `ProcessFileChanges` catch (**L134** → `InternalError`) is retained for genuine
  manager-internal faults (e.g. `EmitSolutionUpdateAsync` throwing).

---

## Interface & migration

- **Breaking change** to `IHotReloadHandler` (`SendAsync` → `OnHotReloadAsync`). Treat as a
  MAJOR/MINOR per the repo's SemVer policy; provide migration notes.
- Update `DelegateHotReloadHandler` to the new shape. The legacy `SendUpdatesAsync` delegate
  (`(files, updates, ct)`) should keep firing **only on `Success`** so existing
  `(files, updates)` consumers are unaffected:

  ```csharp
  public ValueTask OnHotReloadAsync(HotReloadOperationResult result, HotReloadUpdate update, CancellationToken ct)
      => result is HotReloadOperationResult.Success
          ? send(update.Files, update.Deltas, ct)
          : ValueTask.CompletedTask;
  ```

- Update the `IHotReloadHandler` / `HotReloadUpdate` doc comments (they currently state
  "success path only"). The decorator/chaining contract is unchanged.

---

## Risks & considerations

- **Per-cycle cost.** The handler is now called once per cycle (incl. `NoChanges`), and a
  `HotReloadUpdate` record is allocated on every path. A `result != Success` guard keeps
  success-only handlers cheap; the extra allocation is negligible relative to a cycle.
- **`EmitSolutionUpdateAsync` placement unchanged.** It is still only invoked once the solution
  actually changed (after the `== originalSolution` check), so the no-IL-change cost profile
  is preserved.
- **Worker-hosted handlers.** Where the handler forwards across a worker→main boundary,
  "handled" means "dispatched"; the `try`/`catch` covers dispatch failures. Downstream staging
  failures on the far side remain the consumer's responsibility to surface.
- **Outcome vs handler-failure precedence.** A handler exception completes the operation as
  `Failed` regardless of the computed `outcome`; this is intentional (the side-effect failed).

---

## Test plan

- `ProcessSolutionChanged` unit tests (driving the manager with a stub `IWatchHotReloadService`
  and a recording `IHotReloadHandler`) asserting the handler is invoked with the expected
  `HotReloadOperationResult` for each terminal path: `Success`, `NoChanges` (no solution change),
  `NoChanges` (solution changed but no delta), `RudeEdit`, `Failed` (compile errors), `Failed`
  (updater diagnostics).
- A references-only change (solution mutated, empty `updates`) invokes the handler with
  `NoChanges` and a `HotReloadUpdate` whose `SolutionUpdate` carries the updater's result.
- After an early-exit cycle, `CurrentSolution` reflects `result.Solution` (regression for §2).
- A throwing handler completes the operation as `Failed` with the exception (regression for §3);
  a handler throwing `OperationCanceledException` propagates and does not complete as `Failed`.

---

## Out of scope

- The set of `HotReloadOperationResult` values (unchanged).
- The delta-emission / rude-edit classification logic (unchanged; only its branch structure is
  reorganized to converge on a single handler call + completion).
- Removing any of the early-return *conditions* — each remains a distinct outcome; only the
  premature **handler bypass** is removed.
