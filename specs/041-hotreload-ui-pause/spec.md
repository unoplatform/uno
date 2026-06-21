# Spec 041: Hot Reload UI Pause — Transactional, Per-Caller, Phase-Scoped

> **Status**: Draft
> **Date**  : 2026-04-30
> **Owner** : David (platform.uno)

---

## Executive Summary

### The Problem

The hot-reload client currently relies on `Uno.UI.Helpers.TypeMappings.Pause()` /
`Resume()` as a global, ambient pause flag to coordinate UI updates between the
hot-reload pipeline and Hot Design (HD).

The flow is inverted and brittle:

1. HD acquires a long-lived pause via `TypeMappings.Pause()` for the lifetime
   of a design session.
2. Every incoming HR delta is routed to `UpdateApplicationCore` →
   `UpdateVisualTree`. `UpdateVisualTree` consults `TypeMappings.IsPaused`
   and skips the visual-tree update when paused.
3. HD periodically drains its own `HotReloadUpdateHandler.UpdatedTypes`
   accumulator and calls `XamlUpdateService.RunUIUpdate`, which
   **temporarily resumes** the global pause, calls
   `ClientHotReloadProcessor.ReloadWithUpdatedTypes` via reflection
   (`ReflectionExtensions.ClientHotReloadProcessorExtensions.ReloadUI`), then
   **re-pauses**.

Concrete consequences:

- **Studio Live races HD startup**: any `client.UpdateFile(...)` issued before
  HD has installed its long-lived pause applies to the visual tree
  immediately; any update after the pause is installed but before HD starts
  draining is silently deferred and may never visibly apply.
- **Cross-cutting global state**: an unrelated caller cannot tell whether
  `TypeMappings` is paused, by whom, or for which phase of the update —
  so it cannot reason about whether its update will actually produce a
  visual effect.
- **Reflection coupling**: HD reaches into `Uno.UI.RemoteControl` private
  surface (`ReloadWithUpdatedTypes`) by name. Any rename or signature
  change silently breaks HD.
- **Mappings store divergence**: `TypeMappings.RegisterMapping` maintains
  two parallel dictionaries (`AllMappedTypeToOriginalTypeMappings` vs.
  `MappedTypeToOriginalTypeMappings`) and only updates the "active" pair
  when not paused. This logic is buggy in practice and adds no value once
  the new pause mechanism owns deferral.

### What We're Changing

Replace the ambient `TypeMappings.Pause/Resume` flag with a **transactional,
per-caller, phase-scoped** pause mechanism wholly owned by the hot-reload
client:

- New public API in `Uno.UI.RemoteControl.dll`, namespace
  `Uno.HotReload.Client`:
  - `static class UIUpdate` exposing `UIUpdate.Pause(HotReloadUIPhases phases)`.
  - `sealed class HotReloadUIPauseHandle : IDisposable` with
    `Drop(params Type[] types)`.
  - `[Flags] enum HotReloadUIPhases { None = 0, ResourceDictionaries = 1, VisualTree = 2, All = 3 }`.
- The API is **public**: any caller may acquire a pause when it has a
  legitimate reason to defer the visual-tree apply for a window of work
  it owns end-to-end (it is responsible for managing the handle, dropping
  the right types, and disposing the handle).
- In this iteration the **only in-tree caller is `UpdateFile`**, which
  acquires its own short-lived pause for the duration of the call. HD
  delegates pause management to `UpdateFile` rather than holding pauses
  directly — `UpdateFile` already owns the HR-op correlation needed to
  compute the `Drop` set, so doing it inside `UpdateFile` is strictly
  less work than re-implementing the correlation in HD. **There is no
  long-lived pause** anywhere in shipped code paths.
- HD (or any future consumer) is free to acquire pauses directly if a
  use case arises that `UpdateFile` cannot serve (e.g. a window of work
  spanning multiple unrelated `UpdateFile` calls). Such a use case is
  out of scope for this spec; if it lands, it must come with its own
  correlation strategy and explicit `Drop` discipline.
- Pause accumulates pending types **per phase** in two dedicated lists
  (`_pendingResourceDictionariesUpdates`, `_pendingVisualTreeUpdates`).
- Releasing the **last handle that pauses a given phase** drains that
  phase's pending list and triggers the corresponding application
  (`UpdateGlobalResources` for ResourceDictionaries, `DoUpdateVisualTreeCore`
  for VisualTree).
- `UpdateFile` correlates the local `HotReloadClientOperation` ids it
  observes with the HR id it was waiting for, and explicitly `Drop`s
  the corresponding types from pending so that the auto-drain on its
  own handle release does not re-apply types it considers handled.

In parallel:

- `TypeMappings.Pause/Resume/IsPaused/WaitForResume` become inert
  (`[Obsolete]` no-ops with a one-time warning log on first call).
- `TypeMappings` is simplified to a single mapping store (the
  `_mappingsPaused` divergence is removed entirely).
- `ClientHotReloadProcessor.ReloadWithUpdatedTypes` becomes an inert
  no-op + warning log (its only caller is HD's reflection trampoline, kept
  for backward compatibility with already-shipped HD versions).
- HD removes its long-lived pause, `RunUIUpdate`, the `UIUpdatesEnabled`
  toggle, and `HotReloadUpdateHandler.UpdatedTypes` accumulator. With no
  pause held outside `UpdateFile`, HR deltas apply naturally to the
  visual tree as they arrive.

### Why This Approach

- **Inverts the responsibility correctly**: the producer of a file update
  decides whether the update should suppress the visual-tree apply, not
  some ambient global flag set asynchronously by another component.
- **Pause is scoped, not ambient**: each handle has an owner, a phase
  mask, and (for diagnostics) a `[CallerMemberName]` / `[CallerLineNumber]`
  capture of where it was acquired. Multi-source races become traceable.
- **Removes reflection coupling**: HD no longer reaches into private
  hot-reload internals to drive the visual tree update. The visual tree
  update path is the standard `UpdateApplicationCore` → `UpdateVisualTree`
  flow.
- **Drains by phase**: HD wanted to suppress visual-tree updates while
  still letting `App.xaml`-level resource edits flow through. With phase
  masks, this becomes a first-class capability: a caller can pause
  `VisualTree` only and resource updates continue.
- **Multi-threaded by construction**: the new module uses concurrent
  primitives (atomic counters, `ImmutableInterlocked` for pending lists)
  so `Pause`, `Drop`, and `Dispose` are safe from any thread, matching
  the reality that HR deltas arrive on background threads while
  `UpdateFile` runs on the dispatcher.

### Scope

- New public surface: `Uno.HotReload.Client` namespace under
  `src/Uno.UI.RemoteControl/HotReload/UIUpdate/`.
- Modifications to `ClientHotReloadProcessor.ClientApi.cs` (UpdateFile
  pause acquisition + Drop), `ClientHotReloadProcessor.MetadataUpdate.cs`
  (phase-aware pause check, drain dispatch), `ReloadWithUpdatedTypes`
  (no-op + warning).
- New properties on `UpdateRequest`, `UpdateFileRequest`,
  `UpdateSingleFileRequest`: phase mask + caller info.
- Cleanup of `Uno.UI.Helpers.TypeMappings`.
- HD changes in
  `src/Uno.UI.HotDesign.Client/Logic/XamlUpdates/XamlUpdateService.cs`,
  `src/Uno.UI.HotDesign.Client/HotReloadUpdateHandler.cs`,
  `src/Uno.UI.HotDesign.Client/AppUpdater.Messaging.cs`.

### Out of Scope

- Server-side hot-reload pipeline (`Uno.UI.RemoteControl.Server.Processors`).
- ALC-aware HR handlers (covered by spec 039).
- Wire format changes to `UpdateFileResponse` or `HotReloadStatusMessage`.

### Key Constraints

| Constraint | Impact |
|------------|--------|
| HD versions already shipped reach `ReloadWithUpdatedTypes` via reflection | Keep the method present, signature-compatible, but inert. Log a one-time warning at first invocation. |
| External consumers may call `TypeMappings.Pause/Resume` | Keep the methods present, mark `[Obsolete]`, no-op them, log a one-time warning. |
| `UpdateFileRequest` and `UpdateSingleFileRequest` are wire types | New phase / caller-info fields must be additive and tolerate older peers (default values when absent). The phase enum belongs to client behavior; the wire format does not need to carry it (see §3.4). |
| `Uno.UI.RemoteControl` already ships an STJ source-gen context (spec 040) | New types added to wire requests must be registered with the STJ context. |
| Multi-threading | HR deltas arrive on the runtime's `MetadataUpdateHandler` callback (background), `UpdateFile` runs on a dispatcher path, and HD callers hit the API from arbitrary threads. The new module must be thread-safe by construction. |

---

## 1. Current Architecture (What Exists Today)

### 1.1 Pause coordination (`Uno.UI.Helpers.TypeMappings`)

`src/Uno.UI/Helpers/TypeMappings.cs:140-181` — a `TaskCompletionSource`
field `_mappingsPaused` is the entire pause state. `Pause()` installs a
new TCS via `Interlocked.CompareExchange`; `Resume()` swaps it back to
null and completes the prior TCS.

`RegisterMapping` (line 111) writes to `AllMapped*` always, but only
copies into the active `Mapped*` collections when `_mappingsPaused is null`
— a divergence we will remove (the user has confirmed the divergence is
buggy in practice and adds no value once pause moves out of `TypeMappings`).

### 1.2 Visual tree update path

`src/Uno.UI.RemoteControl/HotReload/ClientHotReloadProcessor.MetadataUpdate.cs`:

- `UpdateApplication(Type[])` (line 650) — entry point invoked by .NET's
  `MetadataUpdateHandler`. Calls `UpdateApplicationCore`.
- `UpdateApplicationCore(Type[])` (line 664) — registers
  `MetadataUpdateOriginalTypeAttribute`, dispatches `UpdateVisualTree`
  on the UI thread.
- `UpdateVisualTree` (line 119) — checks `ShouldReload()` (line 59),
  which today is a thin wrapper around `TypeMappings.IsPaused`. When
  paused, calls `UpdateGlobalResources` (resource-only delta) and then
  either `ReportCompleted` (no FrameworkElement subtypes) or
  `ReportIgnored` (FE present, defer to HD).
- `DoUpdateVisualTreeCore` (line 168) — the actual visual tree walk and
  swap. Calls `UpdateGlobalResources` first.
- `UpdateGlobalResources` (line 440) — invokes `Initialize` /
  `RegisterResourceDictionariesBySource` on `*GlobalStaticResources`
  types, then refreshes merged dictionaries. Does **not** touch the
  visual tree, so it is safe to run while HD pauses VisualTree only.
- `ReloadWithUpdatedTypes(HotReloadClientOperation?, Window, Type[])`
  (line 102) — private static, called by HD via reflection
  (`ClientHotReloadProcessorExtensions.ReloadUI`). Calls
  `DoUpdateVisualTreeCore` directly, bypassing `ShouldReload()`.

### 1.3 UpdateFile flow

`src/Uno.UI.RemoteControl/HotReload/ClientHotReloadProcessor.ClientApi.cs`:

- `TryUpdateFilesAsync` (line 145) constructs `UpdateFileRequest`,
  records the local HR id (`currentLocalHrId`, line 170) before sending,
  then awaits server HR (`WaitForServerHotReloadAsync`) and local HR
  (`WaitForLocalHotReloadAsync`, line 245). Local HR completion is
  detected when any `HotReloadClientOperation` with `Id >= currentLocalHrId + 1`
  reaches a non-null `Result`.
- The `HotReloadClientOperation.Types` of those local ops are the
  authoritative correlation between an `UpdateFile` call and the HR
  deltas it triggered.

### 1.4 HD pause/resume

`src/Uno.UI.HotDesign.Client/Logic/XamlUpdates/XamlUpdateService.cs`:

- `UIUpdatesEnabled` (lines 90-105) toggles `TypeMappings.Pause/Resume`.
- `RunUIUpdate(IReadOnlyCollection<Type>)` (line 128) on the dispatcher:
  saves current pause state, resumes, calls `ReloadUIAfterHotReload`
  (which dispatches to the private `ReloadWithUpdatedTypes`), restores
  prior pause state.
- `Dispose` re-enables UI updates, ensuring no orphaned pause survives
  HD teardown.

`src/Uno.UI.HotDesign.Client/HotReloadUpdateHandler.cs`:

- `UpdatedTypes` (line 49) — `HashSet<Type>` accumulator filled by
  `UpdateApplication` (line 51, the `MetadataUpdateHandler` registered
  via `[assembly: MetadataUpdateHandler]`).
- Filtered to `IsSubclassOf(typeof(FrameworkElement))` only (line 63),
  so resource-only deltas never reach `UpdatedTypes`. (This is why the
  existing `UpdateVisualTree` paused path hoists `UpdateGlobalResources`
  out of the skip path.)

`src/Uno.UI.HotDesign.Client/AppUpdater.Messaging.cs:233-255` —
`UpdateToLatestUI` snapshots `UpdatedTypes`, clears it, and calls
`_xamlUpdateService.RunUIUpdate(typesToUpdate)`.

---

## 2. Target Architecture

### 2.1 New module layout

```
src/Uno.UI.RemoteControl/HotReload/UIUpdate/
├── HotReloadUIPhases.cs        // [Flags] enum
├── HotReloadUIPauseHandle.cs   // IDisposable + Drop
├── UIUpdate.cs                 // static entry point: UIUpdate.Pause(...)
└── PendingUIUpdates.cs         // internal: state machine
```

All four files use `namespace Uno.HotReload.Client`. The assembly
remains `Uno.UI.RemoteControl.dll` — namespace ≠ assembly is intentional
(the user has confirmed this).

### 2.2 Public surface

```csharp
namespace Uno.HotReload.Client;

[Flags]
public enum HotReloadUIPhases
{
    None                  = 0,
    ResourceDictionaries  = 1 << 0,
    VisualTree            = 1 << 1,
    All                   = ResourceDictionaries | VisualTree,
}

public static class UIUpdate
{
    public static HotReloadUIPauseHandle Pause(
        HotReloadUIPhases phases = HotReloadUIPhases.All,
        [CallerMemberName] string? caller   = null,
        [CallerFilePath]   string? filePath = null,
        [CallerLineNumber] int     line     = 0);
}

public sealed class HotReloadUIPauseHandle : IDisposable
{
    public HotReloadUIPhases Phases { get; }

    /// <summary>
    /// Removes <paramref name="types"/> from the pending lists for any
    /// phase this handle pauses. Idempotent. Safe from any thread.
    /// </summary>
    public void Drop(params Type[] types);
    public void Drop(Type[] types,
        [CallerMemberName] string? caller = null,
        [CallerLineNumber] int     line   = 0);

    /// <summary>
    /// Releases the pause for the phases owned by this handle. If this is
    /// the last handle pausing a given phase, that phase's pending list
    /// is drained and applied (resource initializers re-run, visual tree
    /// updated).
    /// </summary>
    public void Dispose();
}
```

The `[Caller*]` attributes on `Pause` capture **acquisition** site;
`Drop` captures the **drop** site. Both are recorded and surface in
the diagnostic logs (§5).

### 2.3 Internal state machine (`PendingUIUpdates`)

```csharp
internal static class PendingUIUpdates
{
    // One reference counter per phase (atomic).
    private static int _resourceDictionariesPauseCount;
    private static int _visualTreePauseCount;

    // One pending list per phase, manipulated via ImmutableInterlocked.
    private static ImmutableHashSet<Type> _pendingResourceDictionariesUpdates
        = ImmutableHashSet<Type>.Empty;
    private static ImmutableHashSet<Type> _pendingVisualTreeUpdates
        = ImmutableHashSet<Type>.Empty;

    // Drain dispatch is owned by ClientHotReloadProcessor — see §2.5.
    internal static event Action<HotReloadUIPhases, Type[]>? Drain;

    public static bool IsPaused(HotReloadUIPhases phase) => /* ... */;
    public static void Enqueue(HotReloadUIPhases phase, Type[] types) => /* ... */;

    // Called by HotReloadUIPauseHandle:
    internal static void Acquire(HotReloadUIPhases phases) => /* ... */;
    internal static void Drop(HotReloadUIPhases phases, Type[] types, /*caller info*/) => /* ... */;
    internal static void Release(HotReloadUIPhases phases) => /* ... */;
}
```

Invariants:

- `IsPaused(phase)` returns true iff the corresponding counter is `> 0`.
- `Enqueue` is a no-op for any phase that is not currently paused
  (the caller — `UpdateApplicationCore` / `UpdateVisualTree` — applies
  the update immediately when the phase is unpaused).
- `Acquire(phases)` increments every counter named by `phases`.
- `Release(phases)` decrements every counter named by `phases`. For each
  phase that transitions from `1 → 0`, snapshot+clear the pending list
  for that phase and raise `Drain(phase, snapshot)`.
- `Drop(phases, types, ...)` removes `types` from the pending list for
  every phase named by `phases` and logs the operation (§5).

### 2.4 Acquisition contract in `UpdateFile`

Inside `TryUpdateFilesAsync` (`ClientHotReloadProcessor.ClientApi.cs`):

```csharp
public async Task<UpdateResult> TryUpdateFilesAsync(UpdateRequest req, CancellationToken ct)
{
    // ... existing validation ...

    var currentLocalHrId = GetCurrentLocalHotReloadId();

    HotReloadUIPauseHandle? pauseHandle = null;
    if (req.PauseUIPhases != HotReloadUIPhases.None)
    {
        pauseHandle = UIUpdate.Pause(
            req.PauseUIPhases,
            caller:   req.CallerMemberName,
            filePath: req.CallerFilePath,
            line:     req.CallerLineNumber);
    }

    try
    {
        var request = new UpdateFileRequest { /* existing init */ };
        var response = await UpdateFileCoreAsync(request, req.ServerUpdateTimeout, ct);

        // ... existing error handling ...

        if (response.HotReloadCorrelationId is null) { /* no HR awaited */ return result; }

        var serverHr = await WaitForServerHotReloadAsync(response.HotReloadCorrelationId.Value, req.ServerHotReloadTimeout, ct);
        // ...

        var localHr = await WaitForLocalHotReloadAsync(currentLocalHrId + 1, req.LocalHotReloadTimeout, ct);

        if (pauseHandle is not null)
        {
            // Correlate: every local op id strictly greater than the
            // pre-call snapshot up to and including the awaited id (`localHr.Id`)
            // is a candidate. Drop the union of their `Types`.
            // Edge case (multiple ops in [snapshot+1 .. localHr.Id]): drop
            // them all. Logged with caller info (§5).
            var typesToDrop = CurrentStatus.Local.Operations
                .Where(op => op.Id > currentLocalHrId && op.Id <= localHr.Id)
                .SelectMany(op => op.Types)
                .Distinct()
                .ToArray();

            pauseHandle.Drop(typesToDrop);
        }

        return result;
    }
    catch (...) { /* unchanged */ }
    finally
    {
        pauseHandle?.Dispose();
    }
}
```

Notes:

- `Pause(...)` is called **before** the server `SendMessage`, as the
  user specified ("Avant de faire une demande de mise à jour sur le
  serveur, on va aquérir un pause handle").
- `Dispose` runs in `finally` — exception/cancel-safe. If the handle
  is disposed before `Drop` ran, the auto-drain fires for everything
  in pending (acceptable per §2.6).
- The "drop ops 2 and 3" edge case the user described (awaiting id 3,
  observing 2 and 3) maps to `op.Id > currentLocalHrId && op.Id <= localHr.Id`.
  The same edge case can interfere with a concurrent `UpdateFile`'s
  legitimately-paused types — accepted, logged with `caller`/`line` so
  the diagnostic is traceable.

### 2.5 Drain dispatch into the existing visual tree update path

`ClientHotReloadProcessor` subscribes to `PendingUIUpdates.Drain` once
per process (in the static initializer or constructor):

```csharp
PendingUIUpdates.Drain += OnPendingDrain;

static void OnPendingDrain(HotReloadUIPhases phase, Type[] types)
{
    if (types.Length == 0) return;
    if (Instance is not { } instance || CurrentWindow is not { } window) return;

    switch (phase)
    {
        case HotReloadUIPhases.ResourceDictionaries:
            // UpdateGlobalResources is static, ALC-safe, doesn't need dispatcher.
            UpdateGlobalResources(types);
            break;

        case HotReloadUIPhases.VisualTree:
            // Re-enter the standard dispatch path so the existing
            // _uiUpdateGate, hrOp lifecycle, and ALC content host
            // routing all kick in unchanged.
            var hr = instance._status.ContinueOrStartLocal(HotReloadSource.Manual, types);
#if HAS_UNO_WINUI || WINDOWS_WINUI
            window.DispatcherQueue.TryEnqueue(async () =>
                await instance.DoUpdateVisualTreeCore(hr, window, types));
#else
            _ = window.Dispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Normal,
                async () => await instance.DoUpdateVisualTreeCore(hr, window, types));
#endif
            break;
    }
}
```

The drain re-enters `DoUpdateVisualTreeCore` (not `UpdateVisualTree`) —
the pause check has already been satisfied by virtue of being on the
drain path, so we skip `ShouldReload()`.

### 2.6 `UpdateVisualTree` after the change

```csharp
private async Task UpdateVisualTree(HotReloadClientOperation? hrOp, Window window, Type[] updatedTypes)
{
    using var sequentialUiUpdateLock = await _uiUpdateGate.LockAsync(default);
    var op = hrOp ?? HotReloadClientOperation.Empty;

    var rdPaused = PendingUIUpdates.IsPaused(HotReloadUIPhases.ResourceDictionaries);
    var vtPaused = PendingUIUpdates.IsPaused(HotReloadUIPhases.VisualTree);

    if (rdPaused)
    {
        PendingUIUpdates.Enqueue(HotReloadUIPhases.ResourceDictionaries, updatedTypes);
    }
    else
    {
        UpdateGlobalResources(updatedTypes);
    }

    if (vtPaused)
    {
        PendingUIUpdates.Enqueue(HotReloadUIPhases.VisualTree, updatedTypes);

        // Mirror today's behavior: report op as Ignored if there's a
        // FrameworkElement type, completed otherwise. The drain on
        // handle release (or HD's design surface) will resolve it.
        var needsUiUpdate = updatedTypes.Any(t => t.IsSubclassOf(typeof(FrameworkElement)));
        if (!needsUiUpdate) { op.ReportCompleted(); }
        else                { op.ReportIgnored("UI update paused by UpdateFile"); }
        return;
    }

    await DoUpdateVisualTreeCore(op, window, updatedTypes);
}
```

`ShouldReload()` is deleted (the user has confirmed). Both phases are
checked independently.

### 2.7 Backward-compatibility shims

#### `Uno.UI.Helpers.TypeMappings`

Final shape after cleanup:

- Single mapping store (`_mappedToOriginal`, `_originalToMapped`).
- `RegisterMapping` always writes both (no `_mappingsPaused` test).
- `Pause`, `Resume`, `IsPaused`, `WaitForResume` are kept, marked
  `[Obsolete("Use Uno.HotReload.Client.UIUpdate.Pause instead.", error: false)]`,
  and become inert:
  - `Pause()`: log a `Warning` once via the standard `Logger`, return.
  - `Resume()`: same.
  - `IsPaused`: always returns `false`.
  - `WaitForResume()`: returns `Task.CompletedTask`.
- `_mappingsPaused` field, `TaskCompletionSource` machinery, and the
  `AllMapped*` / `Mapped*` divergence are deleted.

#### `ClientHotReloadProcessor.ReloadWithUpdatedTypes`

Kept (HD ships a reflection trampoline against the symbol name) but
inert:

```csharp
#pragma warning disable IDE0051
private static Task ReloadWithUpdatedTypes(HotReloadClientOperation? hrOp, Window window, Type[] updatedTypes)
{
    if (_log.IsEnabled(LogLevel.Warning))
    {
        _log.Warn(
            $"[HotReload] ReloadWithUpdatedTypes is obsolete and now a no-op. " +
            $"Caller: {(updatedTypes.Length > 0 ? updatedTypes[0].Assembly.GetName().Name : "(unknown)")} " +
            $"with {updatedTypes.Length} type(s). " +
            $"Hot Design must remove its long-lived TypeMappings pause and rely on " +
            $"Uno.HotReload.Client.UIUpdate.Pause inside UpdateFile only.");
    }
    return Task.CompletedTask;
}
#pragma warning restore IDE0051
```

The warning fires only when reached, i.e., only when an old HD build
runs against this Uno. The first call sets a static `_warned` flag so
the rest stay silent.

### 2.8 Hot Design changes

The `Uno.HotReload.Client.UIUpdate` API is public, so HD is free to
acquire pauses directly if a future use case demands it. In this
iteration HD does not — it delegates pause management to `UpdateFile`
because `UpdateFile` already owns the local-HR-op correlation needed
to compute the `Drop` set. Reproducing that correlation in HD would be
strictly more code for no observed benefit.

`src/Uno.UI.HotDesign.Client/Logic/XamlUpdates/XamlUpdateService.cs`:

- Delete `UIUpdatesEnabled` getter/setter.
- Delete the body of `RunUIUpdate` — replace with a no-op returning
  `Task.FromResult(true)` and a comment pointing at this spec. (HD's
  `AppUpdater.UpdateToLatestUI` still calls it; we let the call survive
  for now to minimize churn but it does nothing.)
- Delete the `Dispose` re-enable logic (no pause to release).
- The `UpdateFileAsync` call into `_processor.TryUpdateFileAsync` does
  **not** request a pause by default: `req.PauseUIPhases = HotReloadUIPhases.None`.
  HR deltas apply naturally to the visual tree as they arrive. (HD
  could opt in to `HotReloadUIPhases.VisualTree` if it wanted to take
  ownership of the drain timing for its own xaml edits — out of scope
  here, mentioned only to highlight the design surface remains open.)

`src/Uno.UI.HotDesign.Client/HotReloadUpdateHandler.cs`:

- Remove the `UpdatedTypes` accumulator and the `IsSubclassOf(FrameworkElement)`
  filter on `UpdateApplication`. The MetadataUpdateHandler hook stays
  (HD still needs the hook to clear cached type information via
  `AppUpdater.ClearCachedTypeInformation`), but no accumulator is
  populated.

`src/Uno.UI.HotDesign.Client/AppUpdater.Messaging.cs`:

- `UpdateToLatestUI` collapses to: clear xaml caches, send
  `ReloadShadowDomMessage`, run `ApplyDesignTimeValues`. The
  `_xamlUpdateService.RunUIUpdate(typesToUpdate)` call can either be
  removed or left as a no-op — leaving it minimizes diff churn during
  the migration.

`src/Uno.UI.HotDesign.Client.Core/Extensions/ReflectionExtensions.cs`:

- `ClientHotReloadProcessorExtensions.ReloadUI` static field is no
  longer needed by HD logic; mark `[Obsolete]` and document that the
  symbol still resolves but is inert. Eligible for removal in a
  follow-up after Studio Live picks up the new HD build.

---

## 3. Detailed Design

### 3.1 `UpdateRequest` additions

```csharp
public record struct UpdateRequest(ImmutableArray<FileEdit> Edits, bool WaitForHotReload = true)
{
    // ... existing fields ...

    /// <summary>
    /// Phases of UI update to suspend for the duration of this UpdateFile call.
    /// Defaults to <see cref="HotReloadUIPhases.None"/> — no pause.
    /// </summary>
    public HotReloadUIPhases PauseUIPhases { get; init; } = HotReloadUIPhases.None;

    // Diagnostics — captured at construction time via [CallerXxx],
    // never sent over the wire. Surfaces in the pause-handle log entry.
    public string? CallerMemberName  { get; init; }
    public string? CallerFilePath    { get; init; }
    public int     CallerLineNumber  { get; init; }
}
```

The four overloads of `UpdateRequest`'s primary/explicit constructor
take `[CallerMemberName] string? callerName = null`,
`[CallerFilePath] string? callerFile = null`,
`[CallerLineNumber] int callerLine = 0` and forward them into the init
properties. The `record struct` `with` syntax preserves them across
mutations as long as the caller doesn't override them explicitly.

### 3.2 Wire-format-bearing requests

`UpdateFileRequest` and `UpdateSingleFileRequest` are wire types
(serialized via STJ per spec 040). They do **not** need to carry the
phase enum — the pause is purely a client-side decision and the server
has no role. We add the diagnostic caller fields on the client-only
`UpdateRequest` record only; the request types sent on the wire are
unchanged.

(If a future use case wants the server to know about the pause, that's
a separate spec — out of scope here.)

### 3.3 Multi-threading guarantees

| Method | Threads | Mechanism |
|--------|---------|-----------|
| `UIUpdate.Pause` | Any | `Interlocked.Increment` on per-phase counters. |
| `HotReloadUIPauseHandle.Drop` | Any | `ImmutableInterlocked.Update` on per-phase pending sets. |
| `HotReloadUIPauseHandle.Dispose` | Any | `Interlocked.Decrement`; on `1 → 0` transition for a phase, a snapshot+clear via `ImmutableInterlocked.Update` followed by raising `Drain`. The drain handler must dispatch to the UI thread itself (it does, see §2.5). |
| `PendingUIUpdates.Enqueue` | UI thread (called from `UpdateVisualTree`) | `ImmutableInterlocked.Update`. |
| `Drop`-vs-`Dispose` race | The whole point of the design | `Drop` is a strict subtraction; if it runs after `Dispose` cleared the pending list, it's a no-op. If it runs before `Dispose`, the snapshot at decrement time observes the post-drop state. |

Idempotency: calling `Dispose` twice is safe — the second call is a
guard-checked no-op (a `_disposed` flag set under `Interlocked.Exchange`).

### 3.4 Drain ordering

When a single `Dispose` releases multiple phases simultaneously
(handle held both), `Release` processes phases in a fixed order:
**ResourceDictionaries first, then VisualTree**. This matches the
order inside `DoUpdateVisualTreeCore` (resources before tree walk),
so a release of a `Phases.All` handle behaves identically to two
sequential releases.

### 3.5 HR-correlation correctness

The current `WaitForLocalHotReloadAsync` resolves on **any** local op
with `Id >= currentLocalHrId + 1` and a non-null `Result`. If multiple
ops were created (e.g., a manual edit and a Studio Live `UpdateFile`
fire near-simultaneously), the first to complete resolves the await.

Our drop window — `op.Id > currentLocalHrId && op.Id <= localHr.Id` —
deliberately captures every op in the open interval, including ops
from a concurrent `UpdateFile`. Per the user's directive: this is the
accepted edge case. The diagnostic log entry on `Drop` includes the
`(caller, line)` from the dropping `UpdateFile`'s `UpdateRequest` and
the type list, so post-mortem debugging can identify the cross-talk.

A concurrent `UpdateFile` that is also paused will, in turn, drop
its own correlated types and observe a partial list when it computes
its own drop set. Both calls converge to a fully-drained pending list
on the last handle release.

---

## 4. Telemetry & Logging

All log entries below go through the existing `Logger` infrastructure
(`Uno.Foundation.Logging`), with the source category
`Uno.HotReload.Client.UIUpdate`.

| Event | Level | Fields |
|-------|-------|--------|
| Pause acquired | Trace | `phases`, `caller`, `filePath`, `line`, `currentResourceDictionariesCount`, `currentVisualTreeCount` |
| Drop | Information | `phases`, `dropped types[]`, `caller`, `line`, `pendingResourceDictionariesCountBefore/After`, `pendingVisualTreeCountBefore/After` |
| Release (no drain) | Trace | `phases`, `remainingResourceDictionariesCount`, `remainingVisualTreeCount` |
| Drain triggered | Information | `phase`, `drained types[]` |
| TypeMappings.Pause/Resume hit | Warning (once) | call site (best-effort, walk `StackTrace(skipFrames: 2)` for the first non-Uno frame) |
| `ReloadWithUpdatedTypes` hit | Warning (once) | type count, first-type assembly name |

Volume: pause/release sit at `Trace` because they're acquired at every
`UpdateFile` call. Drops and drains are at `Information` because they
are the diagnostic-relevant transitions.

---

## 5. Testing Strategy

### 5.1 Unit tests

Location: `src/Uno.UI.RuntimeTests/Tests/HotReload/Given_HotReloadUIPause.cs`
(new file). Tests target the `Uno.HotReload.Client` API directly, no
HR pipeline involvement:

| Test | Asserts |
|------|---------|
| `When_PauseDispose_Then_PendingDrains` | `Pause(All)` → `Enqueue` two types → `Dispose` → `Drain` event fires once per phase with the two types. |
| `When_PausePhaseSubset_Then_OtherPhasesUnaffected` | `Pause(VisualTree)` → `Enqueue(ResourceDictionaries, ...)` is a no-op (event raised inline by caller, here observed by test stub). |
| `When_NestedPauses_Then_OnlyOuterDispose Drains` | Two `Pause(VisualTree)` handles, dispose inner first, no drain; dispose outer, drain. |
| `When_DropAllTypes_Then_DispatchSkipsDrain` | Pause → Enqueue → Drop all → Dispose → no `Drain` event raised. |
| `When_DropSubset_Then_DrainsRemainder` | Pause → Enqueue {A,B,C} → Drop {A} → Dispose → drain receives {B,C}. |
| `When_PauseFromMultipleThreads_Then_CountsAreConsistent` | 100 threads each `Pause`+`Dispose`, final counters are 0 and pending lists empty. |
| `When_DropConcurrentWithDispose_Then_NoCrash` | Stress test, validates `Drop`-after-`Dispose` is benign. |

### 5.2 Runtime tests

Location: `src/Uno.UI.RuntimeTests/Tests/HotReload/Given_UpdateFile_With_UIPause.cs`.
These exercise the full pipeline end-to-end on Skia Desktop and WASM.

- `When_UpdateFile_Without_Pause_Then_VisualTreeUpdates`: baseline.
- `When_UpdateFile_With_VisualTree_Pause_Then_VisualTreeStable`: send
  an XAML edit, verify the visual tree did not swap during the
  `UpdateFile` call.
- `When_UpdateFile_With_Pause_Then_DropsCorrelatedTypes`: assert via
  the public `Status` API that the local op for the awaited HR id was
  reported as `Ignored` (because pause was held), and that the visual
  tree state matches the pre-edit DOM.
- `When_TypeMappings_Pause_Logs_Warning`: install a test logger,
  call `TypeMappings.Pause()`, assert one `Warning` entry.
- `When_ReloadWithUpdatedTypes_Logs_Warning`: invoke the private method
  via reflection (mimicking HD), assert one `Warning` entry and zero
  visual-tree mutation.

### 5.3 HD integration

Tracked separately in HD's repo (`uno.hotdesign`). The HD-side spec
should cover:

- HD smoke tests (a session does its initial load and one xaml edit
  cycle without invoking `TypeMappings.Pause` or
  `ReloadWithUpdatedTypes`).
- HD legacy fallback (ensure that an HD build pinned to the **old**
  behavior still works against the new Uno bits — this validates the
  no-op shims).

### 5.4 Studio Live regression test

Re-run the original repro: spawn Studio Live, push an `UpdateFile`
before HD has finished initializing. With this spec, the update applies
to the visual tree immediately because no long-lived pause is held.
Confirm the regression that motivated this spec is gone.

---

## 6. Migration Plan

### Phase 1 — Land new module (uno-private)

1. Add `src/Uno.UI.RemoteControl/HotReload/UIUpdate/` files (new types,
   no behavior change yet).
2. Wire `UpdateVisualTree` to consult `PendingUIUpdates.IsPaused` **in
   addition to** `TypeMappings.IsPaused` (logical OR), and to call
   `Enqueue` when the new pause is held. This keeps both mechanisms
   live and lets HD continue working unchanged.
3. Wire `UpdateFile` to acquire/drop/dispose pauses based on
   `req.PauseUIPhases`.
4. Add the unit tests from §5.1.

### Phase 2 — Cut over (uno-private)

1. Replace `ShouldReload()` with the phase-aware checks; delete
   `ShouldReload`.
2. No-op `TypeMappings.Pause/Resume`, simplify the mapping store.
3. No-op `ReloadWithUpdatedTypes`.
4. Add the runtime tests from §5.2.
5. Verify spec-039 ALC-aware HR handlers still pass.

### Phase 3 — HD adoption (uno.hotdesign)

1. Delete `UIUpdatesEnabled`, `RunUIUpdate` body, `UpdatedTypes`
   accumulator, `ReflectionExtensions.ClientHotReloadProcessorExtensions.ReloadUI`.
2. Verify HD's interactive design loop still updates the visual tree
   on edit (now via the natural HR flow rather than the manual reflection
   trampoline).

### Phase 4 — Studio Live validation

1. Run the original race scenario (push `UpdateFile` before HD load).
2. Confirm green.
3. Land the spec status as "Implemented".

---

## 7. Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Old HD shipped against new Uno hits the no-op `ReloadWithUpdatedTypes`, design surface stops updating. | Phase-1 keeps `TypeMappings.Pause` honored alongside the new mechanism, so old HD continues to work until HD ships the matching build. The no-op shim is enabled in Phase 2 only after HD adoption is in flight. |
| Cross-`UpdateFile` Drop interference (the "ops 2 and 3" edge case). | Accepted by the user. Logged at `Information` with caller info on every Drop so the cross-talk is identifiable post-mortem. |
| Drain re-enters a disposed dispatcher (window torn down). | `OnPendingDrain` early-returns when `Instance is null` or `CurrentWindow is null` (already the pattern used in `UpdateApplicationCore`). |
| Pause held across an exception in `UpdateFile` causes a leak. | `pauseHandle?.Dispose()` is in `finally` — exception-safe. |
| Static `_pendingResourceDictionariesUpdates` / `_pendingVisualTreeUpdates` root types from collectible ALCs (cross-cutting with spec 040 ALC concerns). | Pending types are drained on every handle release; pause windows are short-lived (single `UpdateFile` call). The two static lists are normally empty. ALC unload ordering: the unloaded ALC's HR state is torn down before the pause module sees a residual reference. (Cross-ref: spec 039.) |
| `ReportIgnored("UI update paused by UpdateFile")` confuses downstream `Status` consumers used to seeing "TypeMappings paused". | Reason string is the only thing that changes; the lifecycle (`Ignored → Completed` after drain) is identical. |

---

## 8. Open Questions

None at the time of writing — the four ambiguities raised during plan
review (phase enum shape, pending-per-phase, HD's update mechanism
post-no-op, long-lived-pause location) have all been resolved and the
resolutions are reflected throughout the spec body.

If implementation surfaces new questions, append them here with
`Q-N:` prefixes and route the resolution back through the spec
before adjusting code.

---

## 9. References

- Existing implementation:
  - `src/Uno.UI/Helpers/TypeMappings.cs`
  - `src/Uno.UI.RemoteControl/HotReload/ClientHotReloadProcessor.ClientApi.cs`
  - `src/Uno.UI.RemoteControl/HotReload/ClientHotReloadProcessor.MetadataUpdate.cs`
  - `src/Uno.UI.RemoteControl/HotReload/ClientHotReloadProcessor.Common.Status.cs`
  - HD: `src/Uno.UI.HotDesign.Client/Logic/XamlUpdates/XamlUpdateService.cs`
  - HD: `src/Uno.UI.HotDesign.Client/HotReloadUpdateHandler.cs`
  - HD: `src/Uno.UI.HotDesign.Client.Core/Extensions/ReflectionExtensions.cs`
- Related specs:
  - `specs/039-alc-aware-hotreload-handlers/spec.md`
  - `specs/040-remotecontrol-stj-migration/spec.md`
