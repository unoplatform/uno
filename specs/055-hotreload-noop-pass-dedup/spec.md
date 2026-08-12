# Hot-Reload: A Re-Observed Up-To-Date File Must Be a Silent No-Op

**Repo**: `uno` (Uno.HotReload)
**Created**: 2026-07-23
**Status**: Resolved — implemented on this branch
**Related**: [#23861](https://github.com/unoplatform/uno/issues/23861) (the blocked-compilation
audit the parasite pass lands in), [spec 050](../050-hotreload-updatefile-workspace-gate/spec.md)
(R6 — the per-request info file)

## Overview & Objectives

`FileUpdater.UpdateAsync` (`src/Uno.HotReload/IO/FileUpdater.cs`) writes a request's edits
**and** its per-request hot-reload info file (`HotReloadInfo.g.cs`) while holding the
`BufferGate`, so the `FileSystemObserver`'s 250 ms buffer flushes them as **one** batch —
one pass, one solution update, one emit. That is the design and it works in the nominal
case; the info file's new content (the operation id) rides that batch and reaches the
application (spec 050 R6, pinned by the `Given_HotReloadInfo` runtime test).

The problem: the file-system is **not transactional**. A single `File.WriteAllTextAsync`
emits several inotify notifications (`IN_MODIFY`, `IN_CLOSE_WRITE`); the pipeline's own
`WaitForFileUpdated` only synchronises on the **first** one. A **trailing** event —
typically the close-write for a file the pipeline *just wrote itself*, delivered after the
250 ms buffer already closed (observed on Linux/inotify during a long first pass) — opens a
**new** watcher batch for a file whose on-disk content is *already* in the solution. That
parasite pass then:

1. forks the solution anyway (`WithDocumentText` with byte-identical text still produces a
   new snapshot — reference inequality), so it does **not** take the
   `result.Solution == originalSolution` → `NoChanges` shortcut;
2. emits (`Found 0 metadata updates after 0.007s` — measured) and falls into the
   `(no rude edit, 0 update)` audit branch ([#23861](https://github.com/unoplatform/uno/issues/23861))
   — observed completing as `Failed`;
3. if it merged into the originating request's **still-open** operation, its outcome degrades
   the real one. The normal path is already guarded: post-`8076b444aa` the batch↔operation
   merge decision is under the solution-update gate, so an operation completed by its own pass
   routes the parasite to a *fresh* operation (no overwrite). What remains exposed is the
   window where the origin operation is still open when the parasite runs under the gate — the
   `ProcessFileChanges` exception path (which completes the operation *after* the gate is
   released) and the auto-retry / deferred-completion window. Observed live: the request's
   edits applied successfully, yet it reported `Failed`.

Objective: a batch whose content brings nothing new must resolve to a **silent no-op** — it
must not fork the solution, must not emit, and must not run the audit. The fix is
**content-based and file-agnostic**; it lives in the solution update, where files are
already read.

## Requirements

### R1 — content-level no-op detection in the solution update

In `SolutionUpdater.UpdateAsync`: before replacing a document's text, compare the new
on-disk content with the document's current text and **skip identical writes**.

- Comparison: `SourceText.ContentEquals` — regular documents **and** additional documents
  (a XAML additional document re-observed with identical bytes is the same no-op).
  `ContentEquals` (rather than `GetChecksum`) is deliberate for this access pattern: the
  compared disk text is read fresh every cycle (`SourceText.From(stream)`), so it never
  carries a cached checksum — a checksum path would pay a full hash over the fresh text on
  every cycle, whereas `ContentEquals` short-circuits on length and first difference (the
  common *actually-changed* case bails immediately), and each document is compared at most
  once per pass, so there is no repeated-comparison amortisation for a cached hash to exploit.
- Only *realized* texts are compared (`TryGetText`): an unrealized text means the document
  was never read into this snapshot, so the batch cannot be a re-observation of it — the
  edit is applied unconditionally (never swallow a first edit).
- A batch where **every** entry was skipped returns the *original* solution instance
  (reference-equal), so the existing `result.Solution == originalSolution` → `NoChanges`
  branch in `ProcessSolutionChanged` short-circuits everything downstream — **before the
  emit**, so no metadata-update roundtrip and no blocked-compilation audit
  ([#23861](https://github.com/unoplatform/uno/issues/23861)).
- Skipped entries are surfaced on the result as `SolutionUpdateResult.UpToDateChanges`
  (a `ChangeSet`) — they were **consumed** (their content is in the solution), *not*
  ignored. The manager reports them with a single verbose line.

### R2 — the info file stays a first-class change

No special-casing of the hot-reload info file path: its content **does** change on every
request (new operation id) and that change must keep flowing exactly as today — within the
request's batch, applied and emitted so the application observes the new `VersionId`. The
dedup is strictly content-based; only byte-identical re-observations are suppressed. On the
request's own batch the info file's content differs from the solution, so it is applied
normally.

## Non-goals

- **No watcher-level trigger exclusion.** An earlier draft made the info file "unable to
  open a batch" in the observer (a `canTriggerBatch` predicate). Rejected: making a specific
  file unable to open the update gate is conceptually wrong (the observer should not carry
  per-file lifecycle policy), it does not generalise (any self-written or trailing
  re-observation of *any* file has the same hazard), and the file system is not
  transactional — you cannot know when the last trailing event has arrived, so this can only
  ever be probabilistic. Content-level dedup addresses the whole class at the layer that
  already reads the files.
- Redesigning the watcher/debounce (the 250 ms buffer and the `BufferGate` batching are
  untouched).
- Touching the operation lifecycle: `TryMerge` and the merge-under-gate decision
  (`8076b444aa`) are untouched.
- The audit itself ([#23861](https://github.com/unoplatform/uno/issues/23861)) and the
  analyzer/generator flavor-loading skew.
- De-duplicating *semantic* no-ops (identical AST after reformat, etc.) — bytes only.

## Known residual (accepted)

A trailing event still *opens* a batch and runs a pass; R1 only guarantees that pass is a
**silent `NoChanges`** (reference-equal solution, no emit, no audit) rather than a spurious
`Failed`. So an intermittent trailing event can still surface an extra `NoChanges` operation
in the tracker/history. This is cosmetic and strictly better than the observed failure; the
request itself correlates to its own operation id (returned in the response), which the real
pass completes with the substantive outcome before any trailing pass runs (the merge
decision is under the gate — `8076b444aa`).

## Test plan

Unit tests in `src/Uno.HotReload.Tests/`:

1. **Updater skip** (`Given_SolutionUpdater`): document with realized text `T`; process a
   change-set claiming the file changed while disk still contains `T` → returned solution is
   reference-equal to the input and the document appears in `UpToDateChanges`; with a
   genuinely different `T'` → forked solution, document text `T'`, empty `UpToDateChanges`.
2. **AdditionalDocument skip**: same as (1) for an additional document.
3. **Mixed batch**: two files, one identical + one changed → solution forked, only the
   changed document differs, the identical one in `UpToDateChanges`.
4. **Unrealized-text guard**: a document whose text was never realized is applied even when
   disk content is identical (no false skip).
5. **Manager short-circuit** (`Given_HotReloadManager`): a fully-reduced batch (updater
   returns the original solution + non-empty `UpToDateChanges`) → outcome `NoChanges`, the
   emitter is never invoked, no blocked-compilation audit line, and the up-to-date entries
   are reported.

## Resolved decisions

- Suppression happens at solution-update level (content compare), not at watcher level: the
  watcher cannot know whether bytes changed without reading files, and the updater already
  reads them.
- Up-to-date entries are reported via `UpToDateChanges` (not `IgnoredChanges`): they were
  consumed — their content is in the solution.

## Discussion & decision log

The path to the decision above, recorded so the rejected branches are not re-explored:

1. **Root cause is a file we wrote ourselves.** `FileUpdater` writes `HotReloadInfo.g.cs`
   on every request; a trailing inotify event for *that* file re-triggers hot reload for
   our own bookkeeping write. This was surfaced (not caused) by `8076b444aa`
   (*decide batch merge under the update gate*): before it, the trailing batch was
   silently absorbed into the originating operation; after it, the originating operation
   has already completed by the time the trailing batch runs under the gate, so `TryMerge`
   refuses and the batch spawns a **fresh, user-visible** parasite operation.

2. **First draft — rejected.** Two-part fix: (R1) a watcher-level `canTriggerBatch`
   predicate so the info file could *join* a batch but never *open* one, plus (R2) a
   `HotReloadManager` lifecycle guard so a "vacuous continuation" could not overwrite a
   real outcome. Rejected because:
   - making a specific file "unable to open the update gate" is conceptually wrong — the
     observer would carry per-file lifecycle policy;
   - it does not generalise — any self-written or trailing re-observation of *any* file has
     the same hazard, not just the info file;
   - it can only ever be probabilistic (see 4).

3. **Why the info file cannot simply be excluded from the watcher.** `HasInterest` already
   has an *exception that keeps* the info file (it lives under `obj/`, otherwise filtered).
   It is kept on purpose: `ChangesDetector.DiscoverChangesAsync` only classifies files that
   are **in the batch** (it does not diff the whole solution against disk), and
   `HotReloadInfo.VersionId` is a *compiled constant* — for the app to observe the bump, the
   info-file change must be applied to the solution and emitted. Fully excluding it from the
   watcher would break the `VersionId` bump (spec 050 R6, `Given_HotReloadInfo` runtime
   test). So the file genuinely must be observed to *join* the request's batch.

4. **"Do we release the `BufferGate` too early?" — not fixable by timing.** `FileUpdater`
   holds the gate across all its writes, and `WaitForFileUpdated` synchronises on the
   **first** inotify event of each write — so in the nominal path the info file *is* in the
   request's batch. The parasite is a **trailing** event (a later `IN_CLOSE_WRITE` for the
   same write). You cannot know how many trailing events the kernel will still emit, nor
   when, so holding the gate longer is a probabilistic band-aid, not a fix. The file system
   is not transactional — any fix that lives in watcher/gate timing is chasing a race it
   cannot win.

5. **Options weighed.**
   - **(A)** Keep the observe-but-don't-trigger asymmetry, but inline the check instead of
     threading a delegate. Still watcher-level lifecycle policy.
   - **(B)** Stop observing the info file entirely and have `FileUpdater` inject its change
     into the request's processing directly. Cleanest conceptually, but couples `FileUpdater`
     to the update path — a coupling deliberately avoided (the two are decoupled via the file
     system).
   - **(C)** Chosen: **content-level no-op only.** Let any file (info file included) trigger
     if the OS says so; the updater skips byte-identical re-writes, so a re-observation of
     content already in the solution collapses to a reference-equal solution →
     `NoChanges` before the emit. Treats the whole class at the layer that already reads the
     files, adds no watcher policy, no `FileUpdater`↔manager coupling.

6. **Accepted trade-off.** (C) does not stop the parasite pass from *running*; it guarantees
   the pass is a silent `NoChanges` rather than a spurious `Failed`. The intermittent
   `NoChanges` operation that may appear in history (see *Known residual*) is cosmetic and
   strictly better than the observed failure — an acceptable price for the simplest, safest
   fix.
