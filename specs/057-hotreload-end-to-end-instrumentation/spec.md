# Hot-Reload: End-to-End Instrumentation — Make the Edit-to-Frame Cycle Measurable

**Repo**: `uno` (Uno.HotReload, Uno.UI.RemoteControl, Uno.UI.RemoteControl.Server.Processors)
**Created**: 2026-08-27
**Status**: Proposed
**Related**: [spec 050](../050-hotreload-updatefile-workspace-gate/spec.md) (R6 — the per-request
info file that already carries the correlation id to the app),
[spec 051](../051-hotreload-baseline-identity-handshake/spec.md) (baseline MVID mismatch — a
silent drop this instrumentation would make visible),
[spec 055](../055-hotreload-noop-pass-dedup/spec.md) (the `0.007 s` no-op emit measurement),
[spec 044](../044-alc-memory-leak-fixes/spec.md) (per-reload memory accumulation),
[spec 001](../001-fast-devserver-startup/) (`TIMELINE|` staged startup measurement — the model
this spec follows for the edit cycle)

> **Note**: this spec has no associated GitHub issue yet. One should be filed and linked here
> before any PR, per the repository's contribution rules.

## Overview & Objectives

Uno's hot-reload pipeline **cannot measure itself end to end**, and the gap is structural
rather than a matter of missing log lines.

The pipeline already emits three durations. None of them is, or can become, an edit-to-frame
figure:

- `ServerHotReloadProcessor.cs:221-222` computes
  `DurationMs = Current.CompletionTime - Current.StartTime` — a **server-side** operation
  window.
- `ServerHotReloadProcessor.cs:77` records `WaitDurationMs`, the workspace-gate queue time.
- `RemoteControlClient.Status` records `_roundTrip`, a WebSocket ping RTT.

The 250 ms watcher debounce (`FileSystemObserver.cs:242`,
`bufferTimer.Change(250, Timeout.Infinite); // Wait for 250 ms without any file change`) is
**inside** the server's window, not before it: `HotReloadManager.ProcessFileChanges` awaits
`StartOrContinueHotReload()` *before* awaiting the file set, and says so in a comment —
*"Notify the start of the hot-reload processing as soon as possible, even before the buffering
of file change is completed"* (`HotReloadManager.cs:141-143`). `DurationMs` therefore already
contains the debounce, which is why it cannot be compared across edits of different shapes.

What `DurationMs` does exclude is everything after the server completes: the 100 ms
`await Task.Delay(100, ct); // […] this is just for safety.`
(`ClientHotReloadProcessor.ClientApi.cs:392`, reached on the fully-successful path only — six
early returns bypass it) and **both visual-tree walks** land **entirely after**
`CompletionTime`. So `DurationMs` covers a window that both starts too early to be a
per-edit cost and ends too soon to be a developer-perceived latency, and no amount of careful
reading turns it into one.

**The blocking defect is narrower than "add instrumentation", but it is not a single field.**
One field carries the id; a server-side frame handler is also required, because
`ServerHotReloadProcessor.ProcessFrame` has no case for `HotReloadClientOperationEvent` and no
`default`, so the client's outcome event is sent and silently dropped today. A
correlation id already spans the server and reaches the app:
`UpdateFileResponse.HotReloadCorrelationId` is the `HotReloadServerOperation.Id`
(`HotReloadInfoHelper.cs:76` documents exactly this), and the client blocks on it in
`WaitForServerHotReloadAsync` (`ClientApi.cs:350`). But when the client reports its own
outcome it sends:

```csharp
// ClientHotReloadProcessor.Common.Status.cs:769-777
_ = client.SendMessage(new Messages.HotReloadClientOperationEvent
{
    OperationSequenceId = Id,   // <- client-LOCAL sequential id, not the server correlation id
    StartTime = StartTime,
    …
});
```

`Id` is the client's own counter. **The client half of every hot-reload operation therefore
cannot be joined to the server half.** Two accurate timelines exist and there is no key
between them.

### Key objective

Make one hot-reload cycle observable as a single correlated record spanning file-change
observation to rendered frame, so that (a) a regression is detectable, (b) the stage that
dominates is identifiable, and (c) Uno can state a latency figure at all.

This is deliberately scoped as **measurement only**. No stage is optimised here; several
obvious targets (the 250 ms trailing debounce, the 100 ms success-path delay) are left
untouched precisely so that their cost is measured before it is argued about.

### Why now

Two independent motivations converge on the same plumbing.

1. **Comparability.** Competing implementations publish to-frame numbers; Flutter measures
   `hotReloadMillisecondsToFrame` at three edit tiers in CI, which is how a regression from
   ~400 ms to ~670 ms was caught and escalated. Uno cannot currently detect an equivalent
   regression in any stage.
2. **Diagnosability.** The terminal verdict today is computed from what the *server* did.
   Making `Success` conditional on a client acknowledgement that the delta reached the loaded
   module requires exactly the correlation key R1 introduces. R1 is therefore a prerequisite
   for the diagnosability fix as well as for measurement, and should be sequenced first.

## Verified facts (investigation grounding)

All confirmed by reading the source at the referenced lines on `master` (2026-08-27).

| Fact | Location |
|---|---|
| Watcher debounce is a fixed 250 ms trailing quiet period | `Uno.HotReload/FileSystemObserver.cs:242` |
| One `Stopwatch` covers solution-update **and** emit together | `Uno.HotReload/HotReloadManager.cs:178` (start) → `:309` (`Found {n} metadata updates after {sw.Elapsed}`) |
| Server duration is StartTime→CompletionTime only | `Server.Processors/HotReload/ServerHotReloadProcessor.cs:219-222` |
| Workspace-gate queue time is already measured | `ServerHotReloadProcessor.cs:77` (`WaitDurationMs`) |
| Correlation id exists server→client and is documented as the server operation id | `Uno.UI.RemoteControl/HotReload/HotReloadInfoHelper.cs:76`; `IO/IUpdateFileResponse.cs:16` |
| Client waits on that correlation id | `ClientHotReloadProcessor.ClientApi.cs:342-350` |
| **Client outcome event is keyed by a client-local counter, not the correlation id** | `ClientHotReloadProcessor.Common.Status.cs:770` |
| `EndTime` means "UI update completed", not "frame presented" | `Messages/HotReloadClientOperationEvent.cs:48-49` |
| 100 ms trailing delay on the successful `UpdateFile` path (six early returns bypass it) | `ClientApi.cs:392` |
| Client timeout budgets 10 s / 10 s / 5 s, ×10 or ×30 | `ClientApi.cs:81, 90, 96, 108-116` |
| `TypeMappings` holds two static collections, cleared only by `ClearMappings()` | `Uno.UI/Helpers/TypeMappings.cs:32, 38, 119-122` |
| `HotReloadAgent._deltas` accumulates per module | `HotReload/MetadataUpdater/HotReloadAgent.cs:28, 238` |
| Telemetry can already be redirected to JSONL | `Uno.UI.RemoteControl.Server/Helpers/ServiceCollectionExtensions.cs:183` (`UNO_PLATFORM_TELEMETRY_FILE`) |
| A no-op emit measured 0.007 s | spec 055 |

## Design

### The correlated record

One hot-reload cycle becomes one record, keyed by the **existing**
`HotReloadCorrelationId` (`HotReloadServerOperation.Id`). No new identifier is introduced;
the id is propagated to the two ends that currently drop it.

```
 t0  first file-system event observed        ─┐ pre-server window
 t1  debounce elapsed, batch closed           │ (today: unattributed)
 t2  server operation created (StartTime)    ─┘
 t3  workspace update complete                  ← split from t4 by R3
 t4  EnC emit complete                          (today: t2→t4 is one stopwatch)
 t5  delta serialized, payload size known       ← R4
 t6  frame sent on the wire
 t7  client received
 t8  MetadataUpdater.ApplyUpdate returned
 t9  UI update completed (today: EndTime)
 t10 first frame presented containing the change ← R5
```

`t0→t10` is the figure a developer perceives. `t2→t9` is the most the pipeline can express
today, and only if the two halves are joined.

### Transport

Reuse the existing telemetry channel rather than adding one. Stage timings ride the current
`update-*` measurement events and the `HotReloadClientOperationEvent`, so
`UNO_PLATFORM_TELEMETRY_FILE` continues to be the single collection point and the
DevServer-test harness in `Uno.UI.RemoteControl.DevServer.Tests/Telemetry/` keeps working
unchanged.

## Requirements

### R1 — join the client and server halves *(prerequisite for everything else)*

`HotReloadClientOperationEvent` gains a nullable `ServerCorrelationId` (`long?`) carrying the
`HotReloadCorrelationId` the client already received in `UpdateFileResponse` and already
awaited in `WaitForServerHotReloadAsync`. `OperationSequenceId` is **retained unchanged** —
it is a real client-local ordering key and existing consumers depend on it.

Null is legal and meaningful: a client operation with no server correlation is one the client
originated locally (a drain after a UI pause, an IDE-driven delta that never went through
`UpdateFile`). Those must remain reportable.

**Acceptance**: for a `UpdateFile`-originated edit, a consumer reading the telemetry stream can
join the server's `update-*` events and the client's operation event on a single key without
heuristics, timestamps, or file-path matching.

### R2 — attribute the pre-server window

`FileSystemObserver` stamps the timestamp of the **first** file-system event that opened the
current buffer, and that stamp travels with the batch into `HotReloadManager.ProcessFileChanges`
and onto the created server operation as `ObservedAtUtc`.

The server then reports `DebounceDurationMs` = `BufferCompletedAtUtc - ObservedAtUtc`, which for
a single quiet edit should approximate the 250 ms constant and for a burst should exceed it.

Note the subtraction cannot use `StartTime`: the operation is started *before* the buffer
completes (`HotReloadManager.cs:141-143`), so `StartTime - ObservedAtUtc` is approximately zero
and would measure nothing. The batch therefore has to carry the moment the buffer closed —
the completion of `filesAsync` — as a second stamp alongside `ObservedAtUtc`.

**Rationale**: this window is currently invisible, is a fixed floor on every edit, and is one of
the two cheapest optimisation targets in the pipeline. It must be measured before it is changed.

### R3 — split the generator run from the EnC emit

The single `Stopwatch` at `HotReloadManager.cs:178` is retained (its log line is load-bearing for
existing diagnosis) and supplemented with two lap measurements reported as
`SolutionUpdateMs` (through `SolutionUpdater.UpdateAsync`, which is where source generators
re-run) and `EmitMs` (`WatchHotReloadService.EmitSolutionUpdateAsync`).

**Rationale**: XAML-generator cost and Roslyn cost are currently indistinguishable from outside.
Because Uno compiles XAML to C#, the generator share is the term most likely to differentiate a
markup edit from a code edit, and it is the term a markup fast path would remove. Without this
split, any argument about a markup fast path is unfalsifiable.

### R4 — record delta payload size

`AssemblyDeltaReload` records the byte length of the serialized payload, reported as
`DeltaBytes`, along with `DeltaCount`.

**Rationale**: deltas are base64 inside a JSON frame — roughly 1.33× inflation (4 characters per
3 bytes), uncompressed — and
nothing currently records how large they get. On WASM and on physical devices over Wi-Fi the
transport term is plausibly significant, and it is currently pure speculation in both
directions.

### R5 — measure to the frame, not to the callback

`HotReloadClientOperationEvent` gains a nullable `RenderedAtUtc`, stamped after the first
rendering pass that follows the visual-tree update completing — not when `UpdateApplication`
returns.

`EndTime` keeps its current meaning; `RenderedAtUtc` is additive so no existing consumer breaks.
Where a target cannot cheaply observe frame presentation, the field stays null rather than being
approximated — a null is honest and a fabricated stamp would poison the only number this whole
spec exists to produce.

**Acceptance**: on Skia desktop, `RenderedAtUtc - ObservedAtUtc` is a defensible edit-to-frame
figure for a single-file XAML edit.

### R6 — memory counters on the known accumulators

Report, per operation: `TypeMappingCount` (the two `TypeMapCollection` instances in
`TypeMappings`), `RetainedDeltaCount` (`HotReloadAgent._deltas`, summed across modules), and
managed heap size after the update settles.

**Rationale**: spec 044 measured roughly +48 MB managed and +150 MB RSS per reload under ALC
hosting, reaching OOM in 10–15 cycles. All three accumulators ship with no counter, so the
condition is invisible until the process dies. These are cheap counters, not a profiler.

### R7 — a staged CI benchmark

A benchmark task in the DevServer test suite performs a scripted edit at three tiers and asserts
the correlated record is complete, following the `TIMELINE|` precedent from spec 001:

| Tier | Edit | Purpose |
|---|---|---|
| small | one attribute on one element in a leaf page | best case; dominated by fixed constants |
| medium | a property on a `UserControl` instantiated in several places | exercises partial tree reload |
| large | an app-level `ResourceDictionary` entry | exercises the every-content-root resource pass |

The benchmark asserts **completeness and non-regression of the record**, not absolute wall-clock
thresholds — the harness runs on shared CI hardware and absolute timings there would be noise.
Absolute figures come from running the same task on controlled hardware.

**Non-negotiable**: the benchmark must fail if any stage timestamp is missing. A silently
incomplete record is precisely the failure this spec exists to eliminate.

## Non-goals

- **Optimising any stage.** The 250 ms debounce and the 100 ms trailing delay are both
  measured here and both left in place. Removing them is a separate change that this spec makes
  arguable with evidence.
- **Changing the terminal verdict semantics.** Making `Success` conditional on client
  acknowledgement is the natural follow-up and depends on R1, but it is a protocol change with
  its own compatibility surface and belongs in its own spec.
- **A cross-implementation benchmark.** Comparing Uno against other hot-reload stacks requires
  one harness measuring all of them on the same metric and hardware; it cannot start until Uno
  can measure itself.
- **Instrumenting Hot Design's own write path.** It has its own debounce and coalescing
  constants and deserves separate treatment.
- **New transport or a new telemetry sink.**

## Test plan

1. **R1 join** — DevServer integration test: perform an `UpdateFile`, capture the JSONL
   telemetry, assert exactly one server operation and one client event share a
   `ServerCorrelationId`, and assert a locally-originated client operation still reports with a
   null one.
2. **R2 debounce** — assert `DebounceDurationMs` is present and ≥ 250 ms for a single edit;
   assert a burst of edits inside the window produces **one** operation whose
   `ObservedAtUtc` is that of the *first* event.
3. **R3 split** — assert `SolutionUpdateMs + EmitMs` ≤ the existing stopwatch total, and that a
   no-op pass reports a near-zero `EmitMs` (spec 055 measured 0.007 s).
4. **R4 size** — assert `DeltaBytes > 0` for a real edit and that a no-op pass reports zero
   deltas.
5. **R5 frame** — Skia-desktop runtime test: assert `RenderedAtUtc` is set and ordered after
   `EndTime`; assert the field is null rather than absent where unsupported.
6. **R6 counters** — assert `TypeMappingCount` is monotonic across successive reloads of the
   same type, which is the observable signature of the spec 044 leak.
7. **R7 benchmark** — the three tiers run and produce complete records; the task fails on any
   missing stage.

Existing coverage that must not regress: `Given_HotReloadWorkspace`, `Given_HotReloadInfo`
(pins the correlation id reaching the app, spec 050 R6), and the telemetry tests under
`Uno.UI.RemoteControl.DevServer.Tests/Telemetry/`.

## Resolved decisions

- **Reuse `HotReloadCorrelationId`; do not mint a new trace id.** It already exists, is already
  documented as the server operation id, and already reaches the app through the generated info
  file. The defect is that one message drops it, not that the concept is missing.
- **Keep `OperationSequenceId`.** It is a legitimate client-local ordering key with existing
  consumers; `ServerCorrelationId` is additive.
- **Null over approximation for `RenderedAtUtc`.** A fabricated frame stamp would corrupt the
  one number this work exists to produce.
- **Assert record completeness in CI, not wall-clock thresholds.** Shared CI hardware cannot
  support absolute latency assertions; completeness is both checkable and the actual failure
  mode being guarded against.
- **Measure before optimising.** No stage cost is changed by this spec, so any later argument
  about the debounce or the safety delay starts from evidence.

## Residual risks / follow-ups

- **Frame-presentation observability is uneven across targets.** Skia desktop is
  straightforward; WASM, native Android and iOS may not offer a cheap present callback. R5
  permits null, so the risk is reduced coverage rather than wrong data — but it does mean the
  headline figure will initially exist only on Skia desktop, which is also the only target with
  end-to-end hot-reload test coverage today.
- **Telemetry volume.** Per-operation counters on every reload will increase the JSONL stream.
  If this proves material, gate R6's heap measurement behind an opt-in switch rather than
  dropping the counters.
- **Observer effect.** Reading managed heap size after each update is not free. R6 should
  measure the settled value only, and the benchmark should record whether enabling instrumentation
  shifts the measured cycle.
- **`DurationMs` becomes ambiguous.** Once richer stage timings exist, the existing server
  `DurationMs` is a partial window that reads like a total. It should be documented as such at
  its emission site, or renamed in a follow-up.
