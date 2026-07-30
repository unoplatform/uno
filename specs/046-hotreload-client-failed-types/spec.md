# Hot-Reload Client Event: Report Failed Type Identities (not just counts)

**Repo**: `uno` (Uno.UI.RemoteControl)
**Created**: 2026-06-18
**Status**: Proposed
**Input**: The `HotReloadClientOperationEvent` reports only **counts** (`FailedElementCount` /
`TotalElementCount`) of per-element apply outcomes. A server tracking *which* views are in a failed
UI-apply state cannot attribute a failure to a specific type, nor observe that same type recover —
especially for a multi-type delta with a partial failure. This spec enriches the event with the
identities of the failed (and succeeded) types.

---

## Overview & Objectives

When the client applies a hot-reload delta and re-instantiates affected views, it reports the
outcome to the server via `HotReloadClientOperationEvent`. Today that event carries only **counts**
of the per-element outcome:

```csharp
public int FailedElementCount { get; init; }
public int TotalElementCount { get; init; }
```

A server that tracks *which* views are currently in a failed UI-apply state cannot do so from this
event: it knows *how many* elements failed, but not *which types*. This spec proposes adding the
**identities** of the failed (and, optionally, succeeded) types to the event, so the server can
attribute a failure to a specific view and later observe that same view recover.

### Motivating scenario (generic)

A server consumer gates a corrective action (e.g. a forced rebuild) on whether any view is still
in a failed UI-apply state. It must:

1. mark a view as failed when its delta fails to re-instantiate, and
2. **clear** that view's failure when a subsequent delta for the **same** view applies cleanly —
   without clearing it when a *different* view succeeds (a different view succeeding says nothing
   about the still-broken one).

Step 2 requires per-type identity. With only `FailedElementCount` / `TotalElementCount`, the
consumer can approximate identity from server-side inputs (the changed-file set of the cycle), but
that approximation is imprecise for a **multi-type delta with a partial failure**: when a single
cycle updates types `A` and `B` and the client reports "1 of 2 failed", the server cannot tell
*which* failed. Reporting the failed type identities closes that gap exactly.

## Current behavior (reference)

`src/Uno.UI.RemoteControl/HotReload/Messages/HotReloadClientOperationEvent.cs`:

- `OperationSequenceId` — correlation id.
- `StartTime` / `IgnoreTime` / `EndTime` — lifecycle timestamps (drive `Kind`).
- `ErrorMessage` — free-text error details (not structured per type).
- `FailedElementCount` / `TotalElementCount` — **counts only**.
- `Kind` — derived (`Started` / `Ignored` / `Succeeded` / `Failed`).

The client produces this event in the hot-reload client operation pipeline, where per-element
(per-type) apply outcomes are known before being collapsed into the counts.

## Proposed change

Add structured per-type outcome identities to `HotReloadClientOperationEvent`, populated from the
same per-element results that already produce the counts. Counts are retained (backwards
compatible; they remain the cheap summary).

```csharp
/// <summary>Identities of the types whose UI update failed to apply (per-element isolation).</summary>
public ImmutableArray<HotReloadTypeOutcome> FailedTypes { get; init; } = ImmutableArray<HotReloadTypeOutcome>.Empty;

/// <summary>Identities of the types whose UI update applied successfully.</summary>
public ImmutableArray<HotReloadTypeOutcome> UpdatedTypes { get; init; } = ImmutableArray<HotReloadTypeOutcome>.Empty;
```

where a minimal, transport-friendly identity is:

```csharp
public sealed record HotReloadTypeOutcome
{
    /// <summary>Metadata token of the updated type (stable within a delta module); null when unavailable.</summary>
    public int? MetadataToken { get; init; }

    /// <summary>Fully-qualified type name (e.g. "MyApp.Views.MainPage"), when resolvable.</summary>
    public string? FullName { get; init; }

    /// <summary>Per-type error detail; null on the success list.</summary>
    public string? ErrorMessage { get; init; }
}
```

Notes:

- **At least one of** `MetadataToken` / `FullName` must be set. `FullName` is the friendlier key for
  a server correlating against source/file identity; `MetadataToken` correlates against the delta's
  `UpdatedTypes` tokens.
- `FailedElementCount` should equal `FailedTypes.Length` (and `TotalElementCount` the sum
  `FailedTypes.Length + UpdatedTypes.Length`, since the two lists are disjoint) so existing
  count-only consumers are unaffected — the lists are an additive enrichment.
- Serialization must go through the existing System.Text.Json source-generated context for the
  RemoteControl messages (AOT/trimming safe); add the new types to that context.
- Keep the payload bounded: the per-type list is naturally small (the types in one delta), but a
  cap is advisable for pathological deltas, mirroring existing diagnostic caps in the manager.
  Truncation must be represented **structurally** rather than as an informal string marker, so
  consumers can detect it deterministically without parsing — e.g. dedicated
  `OmittedFailedTypeCount` / `OmittedUpdatedTypeCount` fields (number of identities dropped past the
  cap, `0` when none). The counts (`FailedElementCount` / `TotalElementCount`) remain the true
  totals; the omitted-count fields let a consumer reconcile `FailedTypes.Length + OmittedFailedTypeCount
  == FailedElementCount`.

## Compatibility & migration

- **Additive** to the message contract: older servers ignore the new fields and keep using the
  counts; older clients send empty lists and servers fall back to count-only behavior. No
  coordinated version bump required, though a capability/version note in release notes is
  appropriate.
- The new array fields **must** be initialized to `ImmutableArray<HotReloadTypeOutcome>.Empty` (as
  in the sketch above), not left as the `default(ImmutableArray<T>)` struct value. A default
  `ImmutableArray<T>` is an *uninitialized* state (`IsDefault == true`), distinct from `Empty`, and
  may serialize/deserialize differently (`null` vs `[]`) than an empty array. Explicitly defaulting
  to `.Empty` guarantees stable, non-null list semantics, which is what the "older clients send
  empty lists" guarantee relies on for a payload that omits the fields.
- No behavioral change for consumers that only read the counts.

## Risks & considerations

- **Payload size.** Bounded by the number of types in a delta; cap defensively.
- **Token vs. name stability.** Metadata tokens are stable within a delta's module but not across
  rebuilds; full names are stable but may be unavailable for some generated/anonymous types. Hence
  "at least one of" rather than requiring both.
- **Cost on the client hot path.** The per-element outcomes already exist where the counts are
  computed; materializing identities is a projection of data already in hand, not new work.

## Test plan

- Client-side: a delta updating two types where one fails reports `FailedTypes` of length 1 with the
  failing type's identity and `UpdatedTypes` of length 1 for the other; counts agree
  (`FailedElementCount == 1`, `TotalElementCount == 2`).
- Round-trip serialization through the source-generated context preserves both lists and the counts.
- A fully-successful apply reports an empty `FailedTypes` and a populated `UpdatedTypes`.
- A capability-absent (older) client path yields empty lists and unchanged count behavior.
- Deserializing a payload that **omits** `FailedTypes` / `UpdatedTypes` entirely (simulating an older
  client) yields empty arrays — not a thrown exception or a `default` `ImmutableArray<T>` — on a
  newer server.

## Out of scope

- The lifecycle/`Kind` derivation and the timestamp fields (unchanged).
- Any server-side consumption logic (lives downstream); this spec only enriches the message so such
  logic *can* attribute failures per type.
