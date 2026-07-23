# Hot-Reload: No-Op Watcher Passes Must Not Complete (or Fail) Operations

**Repo**: `uno` (Uno.HotReload)
**Created**: 2026-07-23
**Status**: Approved — ready for implementation
**Related**: [spec 054](../054-hotreload-audit-changeset-scope/spec.md) (the audit the
no-op pass lands in), [spec 053](../053-hotreload-workspace-analyzer-flavor-skew/spec.md),
[spec 050](../050-hotreload-updatefile-workspace-gate/spec.md) (R6 — the per-request
info file)

## Overview & Objectives

`FileUpdater.UpdateAsync` (`src/Uno.HotReload/IO/FileUpdater.cs`) writes the request's
edits **and** the per-request hot-reload info file while holding the `BufferGate`, so
the `FileSystemObserver`'s 250 ms buffer flushes them as **one** batch — one pass, one
solution update, one emit. That is the design and it works in the nominal case.

But the file-system is not transactional: **trailing events** for the same files
(e.g. a close-write notification delivered after the buffer already closed, observed
on Linux/inotify during a long first pass) open a **new** watcher batch whose files'
on-disk content is *already* in the solution. That parasite pass then:

1. forks the solution anyway (`WithDocumentText` with byte-identical text still
   produces a new snapshot — reference inequality), so it does **not** take the
   `result.Solution == originalSolution` → `NoChanges` shortcut;
2. emits (`Found 0 metadata updates after 0.007s` — measured) and falls into the
   `(no rude edit, 0 update)` audit branch (spec 054);
3. worst of all, if it arrived while the originating request's operation was still
   open, `TryMerge` folds it into **that** operation — and since *"operations are
   completed by the pass that processes them"* (`HotReloadManager.ProcessFileChanges`,
   merge-under-gate comment), the parasite pass's outcome **overwrites the real one**.
   Observed live: batch pass applies the request's edits successfully, parasite pass
   completes the merged operation as `Failed` (via the audit), and the request — which
   correlates to that operation id through the info file (spec 050 R6) — reports
   failure for a change that was applied.

Objective: a batch whose content brings nothing new must be a **silent no-op** — it
must not fork the solution, must not emit, must not run the audit, and must never
degrade the outcome of an operation it merged into.

## Requirements

### R1 — content-level no-op detection in the solution update

Where the changed files' on-disk content is applied to the solution (the
`_solutionUpdater.UpdateAsync(originalSolution, changeSet, ct)` step of
`HotReloadManager.ProcessSolutionChanged`, and the updater implementation(s) it
dispatches to): before replacing a document's text, compare the new content with the
document's current text and **skip identical writes**.

- Comparison: `SourceText.ContentEquals` (or checksum comparison via
  `SourceText.GetChecksum()` when both sides expose one) — not string comparison on
  decoded text, so encodings/BOM handled by the existing load path stay authoritative.
- Applies to regular documents **and** additional documents (a XAML additional
  document re-observed with identical bytes is the same no-op).
- A batch where **every** file was skipped must return the *original* solution
  instance (reference-equal), so the existing
  `result.Solution == originalSolution` → `NoChanges` branch in
  `ProcessSolutionChanged` short-circuits everything downstream (no emit, no audit).
- Log one debug/verbose line: `No-op batch: {N} file(s) already up to date ({names})`.

### R2 — a no-op continuation must not overwrite a real outcome

Guard the operation lifecycle around merged batches (`HotReloadManager.ProcessFileChanges`
merge block + `HotReloadOperation.Complete`):

- If a pass resolves to *no effective change* (R1) **and** it merged into an operation
  that another pass is completing (or has completed) with a substantive outcome
  (`Success`, `RudeEdit`, `Failed` with diagnostics), the no-op pass must not replace
  that outcome — `NoChanges` from a no-op continuation is a *silent* completion at
  most, never a downgrade of `Success` nor an upgrade to `Failed`.
- The existing rule *"a batch must not merge into an operation that completes before
  the batch's own pass runs"* stays; this requirement covers the complementary case —
  the batch merged in time but turned out to be vacuous.
- Keep the `NoChanges` auto-retry semantics intact for *real* no-change passes on
  requests that armed `EnableAutoRetryIfNoChanges` (`ForceHotReloadAttempts/Delay`):
  R1's skip must not swallow the retry when the request's own batch legitimately
  produced no delta (retry decision happens in `HotReloadOperation.Complete` —
  unchanged).

### R3 — the info file stays a first-class change

No special-casing of the hot-reload info file path: its content **does** change on
every request (new operation id) and that change must keep flowing exactly as today —
within the request's batch. The dedup is strictly content-based and file-agnostic;
only byte-identical re-observations are suppressed.

## Non-goals

- Redesigning the watcher/debounce (the 250 ms `To2StepsObservable` buffer and the
  `BufferGate` batching are untouched).
- The audit itself (spec 054) and the generator-loading skew (spec 053).
- De-duplicating *semantic* no-ops (identical AST after reformat, etc.) — bytes only.

## Test plan

Unit tests in `src/Uno.HotReload.Tests/`:

1. **Updater skip**: document with text `T`; process a change-set claiming the file
   changed while disk still contains `T` → returned solution is reference-equal to
   the input; with a genuinely different `T'` → forked solution, document text `T'`.
2. **AdditionalDocument skip**: same as (1) for an additional document.
3. **Mixed batch**: two files, one identical + one changed → solution forked, only the
   changed document differs.
4. **Manager short-circuit**: full-reduced batch → outcome `NoChanges`, no emit
   invoked (assert via the watch-service test double), no audit output line.
5. **Merged-operation protection (R2)**: operation completed `Success` by pass A;
   pass B (vacuous continuation of the same operation) processes → operation result
   stays `Success`; symmetric test with pass A `Failed` (real diagnostics) — B must
   not clear it.

## Resolved decisions

- Suppression happens at solution-update level (content compare), not at watcher
  level: the watcher cannot know whether bytes changed without reading files, and the
  updater already reads them.
- `NoChanges` from a vacuous continuation is silent: no retry consumption, no
  completion overwrite — the operation belongs to the pass that did real work.
