# Hot-Reload: Gate `UpdateFile` Requests on Workspace Initialization

**Repo**: `uno` (Uno.HotReload)
**Created**: 2026-07-16
**Status**: Implemented
**Issue**: [unoplatform/uno#23787](https://github.com/unoplatform/uno/issues/23787)
**Related**: [spec 047](../047-hotreload-workspace-single-tfm/spec.md) (baseline TFM filtering)

**Implementation** — `WorkspaceGatedFileUpdater` + `IFileUpdater` in
`src/Uno.HotReload/IO/`, wired in `ServerHotReloadProcessor`; unit tests in
`src/Uno.HotReload.Tests/IO/Given_WorkspaceGatedFileUpdater.cs`. See
*Implementation notes* at the end for the deltas vs. this design.

## Overview & Objectives

The dev-server processes hot-reload file-update requests (`UpdateFileRequest` /
legacy `UpdateSingleFileRequest`) the moment they arrive, with **no dependency on
the Roslyn workspace initialization**. The workspace captures its baseline by
reading the solution **from disk**, asynchronously, after `ConfigureServer`.
Any update request that lands before that read completes writes its edit to disk
*first* — and the baseline is then loaded from the already-mutated file.

Two distinct failure windows exist, both producing *"request acknowledged,
change never applied"*:

- **Window A — edit lands before the baseline disk read.** The baseline
  silently absorbs the edit. Roslyn diffs every subsequent change against a
  solution that already contains it → the edit is **never emitted as a delta**,
  and the running application permanently diverges from the delta engine's
  baseline (until full rebuild).
- **Window B — edit lands after the baseline read but before the
  `FileSystemObserver` exists.** The observer is only constructed once the
  workspace has fully loaded, so the change is **never observed** and no
  hot-reload is triggered for it (it self-heals only if the same file is edited
  again later — but the original request was already reported successful).

This is most easily hit by design-time tooling that starts issuing update
requests at application startup, before the server has notified
`HotReloadEvent.Ready` — a scenario that has moved from theoretical/QA to
real with tooling that connects immediately at app launch.

### Key objective

`UpdateFile` requests received while the workspace **will exist but is not
ready yet** must be **queued** and applied only once the baseline has been
captured. Requests must **never be written to disk ahead of the initial
baseline read**. When the workspace will never be available (failed init,
disposed connection), or a queued request outlives a hard time limit, the
server must **fail the request with an explicit error** instead of applying it
late or silently.

The Visual Studio path (no dev-server workspace at all — VS's own hot-reload
engine owns delta generation) must remain strictly pass-through: no gate, no
queue, no behavior change.

---

## Verified facts (investigation grounding)

Established from source review of `Uno.UI.RemoteControl.Server.Processors`,
`Uno.UI.RemoteControl` (client), `Uno.UI.RemoteControl.ServerCore` and
`Uno.HotReload`:

| # | Fact | Consequence |
|---|------|-------------|
| F1 | `ServerHotReloadProcessor.ProcessUpdateFile` (both overloads) calls `_fileUpdater.UpdateAsync(request, _ct.Token)` immediately; nothing awaits or checks `_workspace`. `_workspace.GetAsync` is in fact **never consumed** outside `InitializeAsync` itself. | Any request racing the init writes to disk unguarded. |
| F2 | The workspace is created asynchronously: `ServerHotReloadProcessor.InitializeInner` stores `(InitializeAsync(ct), ct)` into `_workspace` and returns; the baseline is read **from disk** inside `LoadSolutionFromDisk` → `CompilationWorkspaceProvider.CreateWorkspaceAsync` (`ServerHotReloadProcessor.MetadataUpdate.cs`). | Window A. |
| F3 | The `FileSystemObserver` is constructed only **after** the manager is fully created and `Ready` notified (`ServerHotReloadProcessor.InitializeAsync`, `ServerHotReloadProcessor.MetadataUpdate.cs`). | Window B. |
| F4 | `_fileUpdater` is initialized **in the `ServerHotReloadProcessor` constructor** with the on-disk editor, before any `ConfigureServer`. | A request arriving before *any* configuration still writes to disk — the gate must also cover the not-yet-configured state. |
| F5 | Visual Studio mode: the client computes `devServerEnabled = false` when `BuildingInsideVisualStudio` (`ClientHotReloadProcessor.Agent.cs`) → `ConfigureServer.EnableMetadataUpdates = false` → server-side `InitializeMetadataUpdater` returns `false` → `Notify(Ready)` immediately, **no workspace is ever created** (`ServerHotReloadProcessor.InitializeMetadataUpdater`). File edits are routed to the `IDEFileEditor` (VS applies them, VS's HR pipeline emits deltas). | HR is active as soon as the app starts; the gate must NOT apply — pass-through preserved. |
| F6 | `ConfigureServer` can be received **multiple times** on one connection (explicit warning in `ServerHotReloadProcessor.InitializeProcessor` — e.g. once by the app, once by design-time tooling). `InitializeInner` guards with `_workspace is not null` → warn + return: the workspace is initialized **once per connection**. `_useRoslynHotReload` is a sticky OR across calls (`ServerHotReloadProcessor.MetadataUpdate.cs`), so a *later* `ConfigureServer` can be the one that enables and triggers the init. | The gate must key on the workspace lifecycle, not on "first ConfigureServer processed"; mode can transition no-workspace → workspace-initializing on a subsequent `ConfigureServer`. |
| F7 | `RemoteControlServer` is a **per-connection instance** ("Creates a per-connection server instance", `RemoteControlServer`); processors are registered per connection. A client reconnection (e.g. an app/worker restart against a long-lived dev-server) gets a **fresh `ServerHotReloadProcessor`** with `_workspace == null`. | Workspace "re-init" happens naturally via a new connection — no in-place re-init path exists (a commented-out `ServerHotReloadProcessor.ProcessPackWorkspaceAsync` reload sketch exists but is dead code). |
| F8 | Within one connection there is no recovery: if `InitializeAsync` faults, `_workspace` stays non-null holding a **faulted task** forever; `ServerHotReloadProcessor.Dispose` cancels `_workspace.Ct` (kills manager + observer) with no reset. | "Workspace unloaded / failed and will never come back" is a real terminal state on a live connection — queued and future requests must fail fast with an explicit error. |
| F9 | There is **no server-side time bound** on update processing: the transport is WebSocket frames (not HTTP request/response). The client's `UpdateRequest.ServerUpdateTimeout` (default 10 s, extendable ×10/×30 via `WithExtendedTimeouts`) is purely client-side — the client stops *waiting*, but the server would still apply the edit later, mutating disk with nobody listening. | A hard server-side limit is required; applying an edit after the requester gave up is strictly worse than failing it. |

---

## Design

### Workspace lifecycle states (server-side, per connection)

The gate is driven by an explicit lifecycle around `_workspace`:

```
                 ┌──────────────────────────────┐
                 │  NotConfigured               │  (no ConfigureServer yet — F4)
                 └──────┬───────────────────────┘
        ConfigureServer │
            ┌───────────┴─────────────┐
            ▼                         ▼
   ┌────────────────┐        ┌──────────────────┐
   │ NoWorkspace    │        │ Initializing     │  (metadata updates enabled — F2)
   │ (VS/IDE-driven)│        └───┬──────────┬───┘
   └───────┬────────┘   success  │          │ failure
     pass-through│               ▼          ▼
                 │        ┌───────────┐  ┌───────────┐
                 │        │  Ready    │  │  Failed   │  (terminal — F8)
                 │        └───────────┘  └───────────┘
                 └─ subsequent ConfigureServer (enables metadata updates — F6) ─▶ Initializing
                              any state ──Dispose──▶ Disposed (terminal)
```

- `NoWorkspace → Initializing` remains possible on a **subsequent**
  `ConfigureServer` that enables metadata updates (F6).
- `Failed` and `Disposed` are terminal for the connection; recovery is a new
  connection with a fresh processor (F7).

### Request handling per state

| State | Behavior for `UpdateFileRequest` / `UpdateSingleFileRequest` |
|---|---|
| `NotConfigured` | **Queue.** Mode is not yet known; do not touch the disk. |
| `NoWorkspace` (VS/IDE-driven) | **Pass-through immediately** — current behavior, unchanged (F5). Requests queued while `NotConfigured` are flushed through the same path. |
| `Initializing` | **Queue.** Flush in arrival order on transition to `Ready`. |
| `Ready` | Process immediately (current behavior). |
| `Failed` / `Disposed` | **Fail fast**: respond with an explicit error (see below), **no disk write**, including any still-queued requests at the moment of the transition. |

### Gate placement — a decorator over the file updater

The state handling must **not** live as inline conditionals in the processor's
message paths. The gate is a **decorator** over an updater abstraction:

- Extract the updater contract consumed by the processor into an interface
  (e.g. `IFileUpdater` with `Task<IUpdateFileResponse> UpdateAsync(IUpdateFileRequest, CancellationToken)` —
  the exact shape `FileUpdater` already exposes).
- Implement the whole state machine (queue, FIFO flush, TTL, terminal-state
  rejection) in a `WorkspaceGatedFileUpdater` decorator wrapping the real
  `FileUpdater`. Terminal-state and TTL rejections are just
  `IUpdateFileResponse` instances carrying the global error — the processor
  sends the response frame exactly as it does today.
- The decorator is fed workspace lifecycle transitions
  (`NotConfigured / NoWorkspace / Initializing / Ready / Failed / Disposed`)
  by the processor, which remains the only party that *knows* the lifecycle —
  but contains no gating logic: `ProcessUpdateFile` keeps its current
  one-liner shape, only the construction/composition changes.
- Consumers that have no Roslyn workspace (ad-hoc processors) keep composing
  the bare `FileUpdater` — behavior unchanged.

This keeps the state machine unit-testable in isolation (no processor, no
transport) and the processor free of state spaghetti.

Both message shapes funnel through the same updater call (a single-file
request is a multi-edit with one edit — the legacy response is likewise
reconstructed from the multi result), so the gate covers them uniformly by
construction.

### Requirements

- **R1 — Queue before ready.** While `NotConfigured` or `Initializing`, incoming
  update requests (both message shapes) are enqueued; the disk is not touched
  and the IDE is not solicited. FIFO order is preserved on flush.
- **R2 — Flush on Ready, after baseline.** The queue is flushed only once the
  baseline solution has been captured **and** the `FileSystemObserver` is
  active — i.e. after *both* Window A and Window B have closed. Note this is
  **not** where `HotReloadEvent.Ready` is notified today: today's `Notify(Ready)`
  fires *before* the `FileSystemObserver` is constructed, so the implementation
  reports the gate's `Ready` transition from a later point (after the observer
  exists) rather than reusing the existing notification (see *Implementation
  notes → `Ready` placement*). Flushed requests follow the normal
  `FileUpdater.UpdateAsync` path (including the existing `BufferGate` batching).
- **R3 — VS pass-through.** In `NoWorkspace` mode nothing is gated. Requests
  received while `NotConfigured` and resolved to `NoWorkspace` by the first
  `ConfigureServer` are flushed immediately at that point.
- **R4 — Fail terminal states.** In `Failed`/`Disposed`, respond
  `UpdateFileResponse` / `UpdateSingleFileResponse` with a global error clearly
  stating the workspace is unavailable and will not recover on this connection
  (e.g. `"Hot-reload workspace failed to initialize; reconnect to retry"`).
  Existing `FileUpdateResult` codes are reused (≥ `Failed`); no wire-format
  change.
- **R5 — Hard queue TTL.** Every queued request carries a deadline. TTL is
  enforced **proactively (timer-based)**: each queued request arms its own timer
  (`WatchTimeoutAsync` awaiting `Task.Delay(QueueTimeout, ct)`), so on expiry the
  server responds with the explicit timeout error **within ~TTL regardless of
  how long initialization takes** — the error is *not* deferred until the queue
  next flushes or reaches a terminal state. The expired request is **never
  applied afterwards** (F9). Default: **30 s** (decided — covers typical
  workspace loads while staying within the same order of magnitude as the
  client's default patience; the client's debugger-extended timeouts go far
  beyond, so the server limit is the effective bound). Configurable via a server
  configuration key (same mechanism as `metadata-updates`, e.g.
  `update-file-queue-timeout`). A request whose deadline expires is removed
  without touching the disk.
- **R6 — Re-entrant `ConfigureServer`.** The gate keys on the workspace
  lifecycle, not on configuration calls: repeated `ConfigureServer` messages
  must not re-create, reset, or double-flush the queue; a later call enabling
  metadata updates transitions `NoWorkspace → Initializing` and gating resumes
  (F6). Requests already passed through in `NoWorkspace` mode are not replayed.
- **R7 — Scope of the gate.** The gate is the `WorkspaceGatedFileUpdater`
  decorator (see *Gate placement* above): all state handling lives there, the
  processor only composes it and reports lifecycle transitions. Out-of-tree
  ad-hoc consumers of the bare `FileUpdater` (no Roslyn workspace) keep
  current semantics untouched.
- **R8 — Diagnosability.** Queue transitions are observable: log on enqueue
  (with current state), on flush (count + max wait), on TTL expiry and on
  terminal-state rejection; telemetry events mirroring the existing
  `notify-*` pattern (`update-queued`, `update-flushed`, `update-expired`,
  `update-rejected`) with queue length / wait-duration measurements.
- **R9 — Cancellation.** Queued requests are dropped (with error responses
  where the transport still allows it) when the processor is disposed;
  no orphaned `TaskCompletionSource`, no unobserved task exceptions.

### Client-side impact

None required: the client already surfaces `UpdateFileResponse.GlobalError` as
an `InvalidOperationException` through `TryUpdateFilesAsync`, and already
handles server timeouts. Callers automatically benefit: an early request now
either succeeds (after the workspace is ready) or fails with an actionable
error instead of silently corrupting the baseline.

Callers issuing startup-time updates should account for queueing latency in
`ServerUpdateTimeout` (the existing `WithExtendedTimeouts` covers this).

### Validation task — reconnection lifecycle (Studio Live-style hosts)

The per-connection assumption (F7) is established from `RemoteControlServer`
source, but must be validated end-to-end for hosts that keep the dev-server
alive across app sessions:

1. Confirm that an app/worker restart produces a **new transport connection**
   → new scoped `RemoteControlServer` → new `ServerHotReloadProcessor` (fresh
   `_workspace`), and that the previous processor is disposed.
2. Confirm no host reuses a single connection across a workspace unload; if
   such a flow exists, it lands in the `Failed`/`Disposed` terminal handling
   (R4) by design — the error message must make the "reconnect to retry"
   remediation explicit.

---

## Non-goals

- **In-place workspace re-initialization / reload** on a live connection (the
  dead `ProcessPackWorkspaceAsync` sketch). Recovery remains connection-scoped.
- Changing `FileUpdater`'s own behavior or the ad-hoc processor path — the
  interface extraction (R7) is contract-neutral; the bare updater keeps
  current semantics.
- Wire-format changes to the update messages or responses.
- Gating of the file-system-watcher pipeline itself (the `BufferGate` batching
  behavior is unchanged; the observer keeps starting with the workspace).
- Proactive push of the `Failed` state to late-joining tooling beyond the
  existing `Disabled` broadcast (`HotReloadStatusMessage`), which is deemed
  sufficient for now.

## Test plan

DevServer integration tests (`Uno.UI.RemoteControl.DevServer.Tests`) plus
focused unit tests on the `WorkspaceGatedFileUpdater` decorator in isolation
(fake inner updater + scripted lifecycle transitions — no processor, no
transport):

1. **Race regression (Window A):** send `UpdateFileRequest` immediately after
   `ConfigureServer` (before `HotReloadWorkspaceLoadResult`); assert the edit
   is applied only after `Ready`, a delta is produced for it, and the response
   is successful.
2. **Pre-configuration request (F4):** send an update before any
   `ConfigureServer`; assert it is queued, not written to disk, then flushed
   per the resolved mode.
3. **VS mode pass-through (R3):** `EnableMetadataUpdates=false` +
   `BuildingInsideVisualStudio=true`; assert no queueing/delay and the
   IDE-editor path is used.
4. **Init failure (R4):** force workspace init failure (e.g. invalid project
   path); assert queued and subsequent requests get the explicit terminal
   error, and nothing touches the disk.
5. **TTL expiry (R5):** stall init beyond the (test-shortened) TTL; assert the
   timeout error response and that a late `Ready` does **not** apply the
   expired edit.
6. **Double `ConfigureServer` (R6):** app + tooling configuration sequence;
   assert single init, single flush, no duplicate application of queued edits.
7. **Order preservation (R1):** multiple queued edits on distinct files flush
   FIFO and batch through the `BufferGate`.
8. **Dispose during queue (R9):** close the connection with a non-empty queue;
   assert clean teardown (no unobserved exceptions).
9. **`NoWorkspace → Initializing` mode change (F6/R6):** first `ConfigureServer`
   resolves to `NoWorkspace` (`EnableMetadataUpdates=false`) and two update
   requests pass through (no queuing); a second `ConfigureServer` with
   `EnableMetadataUpdates=true` transitions to `Initializing`; a third request
   is now queued. Assert the third request is applied **exactly once** after
   `Ready`, and the first two are **not** replayed (R6).

## Resolved decisions

1. **TTL default (R5): 30 s.** Fixed default, server-side configuration key
   for overrides; no derivation from a client-declared timeout (not on the
   wire today).
2. **Legacy single-file message: gated identically.** A single-file request is
   converted to a multi-edit with one edit and flows through the same updater
   call, so the gate covers it by construction — no special-casing.
3. **`Failed` state: no proactive push.** The existing `Disabled` broadcast
   through `HotReloadStatusMessage` is sufficient; revisit only if tooling
   demonstrates a need.

## Implementation notes (deltas & concretizations vs. the design above)

Implemented as designed; the following points are where the implementation
concretized or slightly adjusted the text:

- **Rejection result code:** rejections (terminal states, TTL expiry,
  cancellation) use `FileUpdateResult.NotAvailable` (503) on every edit, with
  the same message duplicated as `GlobalError`. Per-edit results are always
  populated because `ClientHotReloadProcessor.TryUpdateFilesAsync` treats an
  empty `Results` array as a "no changes" **success**
  (`Results.All(NoChanges)` is vacuously true) — a global-error-only response
  would be silently swallowed.
- **Winner-takes-the-entry semantics:** each queued entry is claimed
  atomically (`TryTake`) by exactly one of flush / TTL expiry / cancellation /
  terminal rejection. A claimed entry is guaranteed to be skipped by the flush
  (which re-checks `TryTake` before applying) — this is what guarantees R5's
  "never applied afterwards" without cross-cancelling timers. To avoid the queue
  retaining already-claimed entries when the workspace never reaches a flushing
  or terminal state (stuck `Initializing`), the timeout/cancellation path
  compacts them out of the queue (`CompactQueue`), preserving FIFO order of the
  survivors, so a stalled workspace cannot accumulate dead entries (nor inflate
  the queue-length telemetry).
- **Strict FIFO across the flush boundary:** in `Ready`/`NoWorkspace`, a
  request arriving while the queue is non-empty (or a drain is in progress)
  is queued behind it rather than passed through, so a flush can never be
  overtaken by a newer request. Pass-through only happens on an empty, idle
  queue.
- **Cancellation (R9):** a queued entry watches its own `CancellationToken`
  (the processor passes its connection-level token); cancellation completes
  the request with an explicit "cancelled while waiting" error response
  rather than a fault, and the entry is never applied.
- **Inner swap:** `WorkspaceGatedFileUpdater.Inner` is a settable property;
  `InitializeProcessor` replaces only the decorated `FileUpdater` (editor
  refinement on `ConfigureServer`) while the gate — lifecycle state and queued
  requests — survives, per R6. A flush that races the swap uses the inner
  present at entry-execution time.
- **Diagnostics surface (R8):** the decorator raises a `WorkspaceGateEvent`
  (`queued` / `flushed` / `expired` / `rejected`, queue length, wait duration)
  through an optional callback; the processor relays them as telemetry events
  `update-<kind>` with `QueueLength` / `WaitDurationMs` measurements, and logs
  through the existing `IReporter`.
- **Config key:** `update-file-queue-timeout` (integer, seconds), read once at
  processor construction via `GetServerConfiguration` — same mechanism as
  `metadata-updates`.
- **`Ready` placement:** reported from `InitializeAsync` **after** the
  `FileSystemObserver` is constructed (not at the `HotReloadEvent.Ready`
  notification a few lines earlier), closing Window B as required by R2.
- **Test coverage:** the decorator unit tests (13 tests, MSTest +
  AwesomeAssertions) cover test-plan items 1–8 at the state-machine level,
  including two cases the plan didn't list explicitly: an expired entry must
  not block later live entries from flushing, and an `Inner` swap while
  queued must flush through the new inner. The **end-to-end DevServer
  integration tests** (real `ConfigureServer` → `UpdateFile` race against an
  in-process server) are NOT included in the initial implementation and
  remain as follow-up; the full existing DevServer suite passes unchanged
  (481 ✔).
