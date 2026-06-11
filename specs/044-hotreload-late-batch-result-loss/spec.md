# Fix Specification: Hot-Reload Operation Loses the Result of Late-Merged Batches

**Repo**: `uno` (Uno.HotReload)
**Created**: 2026-06-10
**Status**: Implemented
**Input**: A file-change batch merged into an in-flight `HotReloadOperation` can have its compile
result (including `Failed` + error diagnostics) silently discarded when the operation was already
completed by an earlier pass. Status consumers then see `Success` / no diagnostics while the
workspace no longer compiles.

---

## Problem Statement

### Symptom

Observed by a downstream consumer (agent-driven workspace tooling hosting Roslyn in a WASM
worker): a `.xaml` file was written twice in quick succession — an initial write, then an
automated post-write correction of the same file ~0.5 s later. The hot-reload status for the
covering operation reported `Success`, "no diagnostics". A full build of the same workspace
minutes later failed on that exact file (`UXAML0001` "Unable to find class name for toplevel
control" + `CS1061` for the missing `InitializeComponent`). The failure of the second hot-reload
pass had been computed — and thrown away.

### Why (current code)

Three cooperating behaviors in `src/Uno.HotReload`:

1. **`HotReloadManager.ProcessFileChanges`** (`HotReloadManager.cs`) merges an incoming batch
   into the current operation when the path sets intersect and the operation has not completed:

   ```csharp
   var hotReload = await _tracker.StartOrContinueHotReload().ConfigureAwait(false);
   var files = await filesAsync.ConfigureAwait(false);
   if (!hotReload.TryMerge(files))
   {
       hotReload = await _tracker.StartHotReload(files).ConfigureAwait(false);
   }
   ```

   `TryMerge` checks completion (`_result is -1`) **at merge time**. The pass itself then queues
   on `_solutionUpdateGate`, so it can execute **after** the operation has been completed by an
   earlier pass. Nothing re-validates the operation between the merge and the pass.

2. **`ProcessSolutionChanged`** runs the merged pass against the (now-completed) operation and
   reports its outcome through `hotReload.Complete(...)`.

3. **`HotReloadOperation.Complete`** (`Tracking/HotReloadOperation.cs`) drops any completion after
   the first, including its diagnostics, with no trace:

   ```csharp
   if (Interlocked.CompareExchange(ref _result, (int)result, -1) is not -1)
   {
       return; // Already completed
   }
   ```

Net effect: first pass completes the operation (`Success` at time T); the later pass updates
`CurrentSolution` with the newer file content (the manager sets `workspace.CurrentSolution`
*before* emitting), compiles it, fails — and the `Failed` + diagnostics vanish. The tracker's
`Current`/`Last` still expose the `Success` operation. The workspace is left containing source
that no longer compiles, with no surviving signal.

### Timing window

The window is `[merge accepted, operation completed]`. Any producer that writes the same file
twice within one debounce-plus-compile interval can hit it; the faster the first pass, the wider
the practical exposure.

---

## Fix Design

### Root-cause fix (decide the merge under the gate)

The merge decision is currently made **outside** `_solutionUpdateGate`, so it can be invalidated
while the pass waits for the gate. Moving the `TryMerge`/`StartHotReload` decision **inside** the
gate removes the window entirely:

```csharp
var hotReload = await _tracker.StartOrContinueHotReload().ConfigureAwait(false); // early Processing notification — unchanged
var files = await filesAsync.ConfigureAwait(false);   // outside the gate: don't hold it during debounce
using var _ = await _solutionUpdateGate.LockAsync(ct).ConfigureAwait(false);
if (!hotReload.TryMerge(files))
{
    hotReload = await _tracker.StartHotReload(files).ConfigureAwait(false);
}
await ProcessSolutionChanged(hotReload, files, ct).ConfigureAwait(false);
```

Why this is sufficient: passes are serialized by the gate, and an operation processed by a pass
is completed *before* that pass releases the gate. A `TryMerge` evaluated under the gate can
therefore no longer be invalidated by a concurrent pass completion — if the operation completed,
`TryMerge` fails (`_result` set) and the batch gets its own tracked operation N+1, exactly as if
it had arrived after completion (which, observably, it did).

Side effects, all acceptable or better:
- The early `StartOrContinueHotReload()` call stays where it is, so the immediate `Processing`
  state notification is unchanged.
- The op's abort timer is no longer re-armed at merge time but at gate acquisition; an op kept
  waiting >30 s behind a long pass aborts and the batch is routed to a fresh op — more correct.
- `ConsideredFilePaths` reflects the batch only once it is actually about to be processed.

Residual window: an **out-of-band** completion (30 s abort timer, explicit abort) landing *during*
the pass still loses that pass's result — rare, and made observable by the hardening below.

### Defensive hardening (labeled as such, not the fix)

`HotReloadOperation.Complete`: when a completion is discarded because the operation already
completed, report it instead of returning silently — `_owner` reporter warning including the
dropped result and diagnostic count. No behavioral change beyond observability.

### Testability seam

`HotReloadManager` takes the concrete reflection wrapper `WatchHotReloadService`. Extract a
minimal interface (`IWatchHotReloadService`: `StartSessionAsync`, `EmitSolutionUpdateAsync`,
`EndSession`) implemented by the existing wrapper, so the manager can be unit-tested with a stub
emitter. No public-API change for consumers (type is internal to the library's wiring).

---

## Test Plan (red first — commits between steps)

New project `src/Uno.HotReload.Tests` (none exists for this library).

Red tests (must fail on current code):

1. **`ProcessFileChanges_BatchMergedIntoCompletedOp_TracksItsOwnResult`** (manager level)
   - Stub `IWatchHotReloadService`: first emit returns success/updates, second emit returns an
     error diagnostic.
   - Drive two `ProcessFileChanges` calls whose batches intersect; hold the second batch's
     `filesAsync` until the first pass completed the operation (deterministic — no sleeps:
     the test controls the `Task<ImmutableHashSet<string>>` and the stub emitter's completion).
   - Assert the tracker's last operation reflects the second pass: `Result == Failed` (or
     `RudeEdit`) and its diagnostics are present.
2. **`Complete_AfterCompleted_ReportsDroppedResult`** (operation level)
   - `Complete(Success)` then `Complete(Failed, diagnostics)`; assert the reporter received a
     warning mentioning the dropped result (and the first result is unchanged).
3. Non-regression: a batch merged and processed **while** the operation is still pending keeps
   today's behavior (single operation, its result wins).

Then:

- Fix per design above.
- All new tests green; existing solution-filter build unaffected.

## Progress

- [x] `src/Uno.HotReload.Tests` project created and wired to `Uno.UI.slnx` + `Uno.UI-UnitTests-only.slnf`
- [x] Seam: `IWatchHotReloadService` extracted (wrapper implements it; manager consumes it; internal ctor + `InternalsVisibleTo`)
- [x] Red test 1 (`When_BatchMergedIntoCompletedOperation_Then_ItsResultIsTracked`) — confirmed failing: expected `RudeEdit`, observed `Success` (the exact production defect)
- [x] Red test 2 (`When_CompletingAnAlreadyCompletedOperation_Then_DroppedResultIsReported`) — confirmed failing: no warning emitted
- [x] Non-regression tests (`When_SingleBatch…`, `When_BatchMergedIntoPendingOperation…`, `When_PreviousOperationAbortedByNext…`) — passing pre-fix
- [x] Commit (tests + seam, red)
- [x] Fix: `TryMerge`/`StartHotReload` decision moved under `_solutionUpdateGate` in `ProcessFileChanges` (root-cause fix)
- [x] Hardening: `Complete` reports dropped completions via the tracker reporter, except chain-aborts (`isFromNext`) (defensive hardening)
- [x] All 5 tests green post-fix
- [x] Commit (fix, green)

## Validation Evidence (per repo protocol)

- **Code review assessment**: merge decision is now made while holding the same gate under which
  operations are completed, so a completed operation can no longer absorb a pending batch; the
  late batch becomes operation N+1 whose result and diagnostics surface through the tracker.
- **Compile validation**: `src/Uno.HotReload/Uno.HotReload.csproj` (Debug, net9.0 + net10.0) — 0 errors;
  `src/Uno.UI.RemoteControl.Server.Processors/Uno.UI.RemoteControl.Server.Processors.csproj` (direct consumer) — 0 warnings / 0 errors.
- **Runtime validation**: `dotnet test src/Uno.HotReload.Tests` — 5/5 passed (deterministic
  interleaving via controlled `filesAsync` tasks and a gated stub emitter; no sleeps). Downstream
  integration validated by the consumer via a local package override.
