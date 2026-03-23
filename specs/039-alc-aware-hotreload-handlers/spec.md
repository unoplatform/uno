# Feature Specification: ALC-Aware Hot-Reload Element Handlers

**Repo**: `uno-private` (Uno.UI.RemoteControl)
**Created**: 2026-03-23
**Status**: Draft
**Input**: `ElementUpdateAgent` handler discovery fails to resolve methods on handler types loaded in a secondary `AssemblyLoadContext`, preventing extension libraries from defining their own hot-reload handlers.

---

## Problem Statement

### Current Architecture

`ElementUpdateAgent` (singleton in `ClientHotReloadProcessor`) discovers `ElementMetadataUpdateHandlerAttribute` handlers by scanning `AppDomain.CurrentDomain.GetAssemblies()` and resolving handler methods via reflection.

When a host application loads a secondary app inside an `AlcContentHost`, that app's assemblies are loaded in a **collectible inner ALC**. Some assemblies are shared with the host (e.g. `Uno.UI`, `System.*`), but others are loaded exclusively from the inner ALC (e.g. extension libraries).

### The Bug

If a handler is defined in an inner-ALC assembly, the following happens:

1. `AppDomain.CurrentDomain.GetAssemblies()` **does** include the inner-ALC assembly.
2. `assembly.GetCustomAttributesData()` **does** find the `ElementMetadataUpdateHandlerAttribute`.
3. The `elementType` (e.g. `UIElement`) resolves correctly because `Uno.UI` is a **shared** assembly — same `Type` object across ALCs.
4. The `handlerType` resolves to the inner ALC's copy of the type.
5. `GetHandlerMethod(handlerType, "CaptureState", ...)` **fails** to match the method signature.

The method resolution fails because `GetHandlerMethod` uses `handlerType.GetMethod(name, bindingFlags, binder: null, parameterTypes, modifiers: null)` where `parameterTypes` contains `typeof(FrameworkElement)` from the **host's compilation context**. Even though `Uno.UI` is shared, the reflection call on a type from the inner ALC does not reliably match parameter types from the host context. The result: the handler entry exists (`ElementType=UIElement`) but all method delegates remain null (`CaptureState=False RestoreState=False`).

### Consequence

Libraries loaded in a secondary ALC cannot define their own hot-reload handlers. The host must implement workaround handlers that use cross-ALC reflection to access the library's types (scanning `AssemblyLoadContext.All`, resolving `DependencyProperty` fields by name). This is fragile and creates coupling between the host and inner-app libraries.

---

## Goals

1. Allow inner-ALC assemblies to register `ElementMetadataUpdateHandlerAttribute` handlers that work correctly.
2. When refreshing the handler list, **keep handlers from shared assemblies** and **refresh handlers from assemblies loaded in both the host and the inner ALC**.
3. Inner-ALC handlers must be **considered** during hot-reload (they are currently silently ignored).
4. No breaking change to existing handler registration for non-ALC scenarios.
5. **No memory leaks**: per-ALC handler caches must not prevent collectible ALCs from being garbage collected.

## Non-Goals

- Modifying ALC sharing rules (host-specific concern).
- Changing the `ElementMetadataUpdateHandlerAttribute` API.

---

## Design

### Core Invariants

When the handler list is refreshed (`LoadElementUpdateHandlerActions`):

1. **MUST keep** handlers from shared assemblies (default ALC) — these are stable across ALC reloads.
2. **MUST refresh** handlers from assemblies that exist in both the host and the inner ALC — the inner ALC's copy may have different handler registrations.
3. **MUST include** handlers from inner-ALC-only assemblies — these are currently silently dropped because method resolution fails.

### Part 1 — Fix cross-ALC method resolution

In `ElementUpdateAgent.GetHandlerMethod()`, when `handlerType.GetMethod(name, ..., parameterTypes, ...)` returns null, add a **FullName-based fallback**:

```csharp
// Existing strict match
var method = handlerType.GetMethod(name, bindingFlags, null, parameterTypes, null);
if (method is not null && /* return type check */)
    return method;

// Fallback: match by name + parameter FullName (cross-ALC safe)
foreach (var candidate in handlerType.GetMethods(bindingFlags))
{
    if (candidate.Name != name) continue;
    var candidateParams = candidate.GetParameters();
    if (candidateParams.Length != parameterTypes.Length) continue;
    if (returnType is not null && candidate.ReturnType.FullName != returnType.FullName) continue;
    if (returnType is null && candidate.ReturnType != typeof(void)) continue;

    bool match = true;
    for (int i = 0; i < parameterTypes.Length; i++)
    {
        if (candidateParams[i].ParameterType.FullName != parameterTypes[i].FullName)
        {
            match = false;
            break;
        }
    }
    if (match) return candidate;
}
```

This resolves the immediate bug: handler methods using shared types (`FrameworkElement`, `IDictionary<string, object>`, `Type[]`) are matched by name even when `Type` identity doesn't survive the cross-ALC boundary.

### Part 2 — Per-ALC handler cache

Extend `ElementUpdateAgent` to partition handlers by ALC origin:

```
_sharedHandlerActions    → handlers from the default ALC (stable, never cleared on ALC reload)
_alcHandlerActions       → handlers from non-default ALCs (cleared when the owning ALC is unloaded)
```

During `LoadElementUpdateHandlerActions()`:

```
for each assembly in AppDomain.CurrentDomain.GetAssemblies():
    alc = AssemblyLoadContext.GetLoadContext(assembly)
    if alc is default ALC:
        register handler in _sharedHandlerActions
    else:
        register handler in _alcHandlerActions[alc]
```

Handler lookup merges both dictionaries. For the same `elementType` key, inner-ALC handler takes precedence (it has direct type access).

### Part 3 — Memory leak prevention (critical)

Collectible ALCs are heavy. Storing strong references to inner-ALC types or delegates **prevents the ALC from being garbage collected**, leaking all assemblies, types, and static state in that ALC.

**Leak vectors to address:**

| Reference | Where | Mitigation |
|-----------|-------|------------|
| `Dictionary<AssemblyLoadContext, ...>` key | `_alcHandlerActions` | Use `ConditionalWeakTable<AssemblyLoadContext, AlcHandlerSet>` — entries are automatically removed when the ALC is collected |
| `ElementUpdateHandlerActions` delegates | Point to methods on inner-ALC types | Delegates root the `MethodInfo` → `Type` → `Assembly` → `ALC`. Must be cleared when the ALC unloads |
| `Type` keys in handler dictionary | `_elementHandlerActions[elementType]` | `elementType` is from shared `Uno.UI` (not inner ALC) — safe. But `handlerType` references must not be stored |

**Strategy:**

1. **`ConditionalWeakTable<AssemblyLoadContext, AlcHandlerSet>`** for the per-ALC cache. When the ALC becomes unreachable, the entry is automatically removed — no explicit cleanup needed, no preventing GC.

2. **Subscribe to `AssemblyLoadContext.Unloading`** as a belt-and-suspenders cleanup: when an ALC starts unloading, proactively clear its handler delegates. This ensures delegates don't prevent finalization of the ALC's types during the unload window.

3. **Never store `handlerType` references long-term.** The `ElementUpdateHandlerActions` stores `Action<...>` / `Func<...>` delegates, not `Type` references. The delegates DO root the handler type's assembly. This is acceptable while the ALC is alive (we need the delegates), but cleanup on unload (point 2) releases them.

4. **`OnAssemblyLoad` re-scan**: The existing `AppDomain.CurrentDomain.AssemblyLoad` handler calls `LoadElementUpdateHandlerActions()`. On re-scan, shared handlers are preserved; stale inner-ALC entries are naturally gone (ConditionalWeakTable purged them or the ALC's assemblies are no longer in `GetAssemblies()`).

```csharp
// Pseudocode for the cache structure
private readonly ConcurrentDictionary<Type, ElementUpdateHandlerActions> _sharedHandlerActions = new();
private readonly ConditionalWeakTable<AssemblyLoadContext, AlcHandlerSet> _alcHandlerActions = new();

private sealed class AlcHandlerSet
{
    public ConcurrentDictionary<Type, ElementUpdateHandlerActions> Handlers { get; } = new();
}

// On ALC unloading — proactive cleanup to release delegates sooner
private void OnAlcUnloading(AssemblyLoadContext alc)
{
    if (_alcHandlerActions.TryGetValue(alc, out var set))
    {
        set.Handlers.Clear(); // Release delegates → unroot handler types
    }
    // ConditionalWeakTable entry will be removed automatically when ALC is GC'd
}
```

### Handler lookup at hot-reload time

When selecting handlers for an element during `ReloadWithUpdatedTypes`:

1. Start with `_sharedHandlerActions` (always applicable).
2. Determine if the element is inside an `AlcContentHost` subtree:
   - Check `AssemblyLoadContext.GetLoadContext(element.GetType().Assembly)`.
   - If non-default ALC → look up `_alcHandlerActions[alc]`.
3. Merge: inner-ALC handler **wins** for the same `elementType` key.
4. Apply `GetSubClassDepth` ordering as before.

For elements NOT in an inner ALC (host-level UI), only `_sharedHandlerActions` applies.

---

## Affected Files

| File | Change |
|------|--------|
| `Uno.UI.RemoteControl/HotReload/MetadataUpdater/ElementUpdaterAgent.cs` | FullName fallback in `GetHandlerMethod`; split `_elementHandlerActions` into shared + per-ALC with `ConditionalWeakTable`; subscribe to `ALC.Unloading`; merged handler lookup |
| `Uno.UI.RemoteControl/HotReload/ClientHotReloadProcessor.MetadataUpdate.cs` | Pass ALC context during tree enumeration; use merged handler lookup when inside AlcContentHost subtree |
| `Uno.UI.RemoteControl/HotReload/ClientHotReloadProcessor.Common.cs` | Detect `AlcContentHost` boundary during `EnumerateHotReloadInstances`; propagate ALC context |
| `Uno.UI/UI/Xaml/Window/AlcContentHost.cs` | Possibly expose the inner ALC reference or add a marker interface for detection |

---

## Implementation Checklist

- [ ] **FullName fallback** in `GetHandlerMethod` — cross-ALC method resolution works.
- [ ] **Split handler storage** — `_sharedHandlerActions` (default ALC) vs `_alcHandlerActions` (ConditionalWeakTable keyed by ALC).
- [ ] **ALC classification** during `LoadElementUpdateHandlerActions` — use `AssemblyLoadContext.GetLoadContext(assembly)` to route handlers to the correct dictionary.
- [ ] **ALC.Unloading subscription** — proactively clear delegates to release inner-ALC types before GC.
- [ ] **Merged handler lookup** — `ReloadWithUpdatedTypes` merges shared + inner-ALC handlers, inner-ALC wins on conflict.
- [ ] **AlcContentHost detection** — during tree walk, identify when entering inner-ALC content to activate the correct handler set.
- [ ] **Unit tests** in `Uno.UI.RemoteControl.DevServer.Tests` (see Testing section).
- [ ] **Memory leak test** — verify ALC can be collected after unload when handlers were registered.

---

## Testing

### Test project

`Uno.UI.RemoteControl.DevServer.Tests` — MSTest, already links source files from `Uno.UI.RemoteControl`.

### Unit tests to add

New file: `Uno.UI.RemoteControl.DevServer.Tests/HotReload/ElementUpdateAgentCrossAlcTests.cs`

| Test | Description |
|------|-------------|
| `When_HandlerInDefaultAlc_Then_MethodsResolved` | Baseline: handler in the default ALC has CaptureState/RestoreState correctly resolved. Regression guard. |
| `When_HandlerInSecondaryAlc_Then_MethodsResolved` | Core fix: load a test assembly in a collectible ALC, register a handler, verify CaptureState/RestoreState are found. |
| `When_HandlerInSecondaryAlc_Then_DelegatesInvocable` | Resolved delegates can be invoked — CaptureState/RestoreState round-trip state correctly. |
| `When_SecondaryAlcUnloaded_Then_HandlersCleanedUp` | After unloading the ALC, handler entries are removed on next scan. No stale delegates. |
| `When_SecondaryAlcUnloaded_Then_AlcIsCollected` | **Memory leak test**: after unloading and clearing handlers, the ALC's `WeakReference.IsAlive` returns false after GC. Proves no reference leak. |
| `When_SharedAndAlcHandlerForSameType_Then_AlcWins` | Both host and inner ALC register for the same element type. Inner-ALC handler takes precedence in merged lookup. |
| `When_SharedAndAlcHandlers_Then_SharedPreservedOnAlcReload` | Unload inner ALC and load a new one. Shared handlers remain intact; only ALC handlers change. |

### Test assembly approach

Pre-compile a minimal test DLL (or generate via Roslyn at test time) containing:

```csharp
[assembly: ElementMetadataUpdateHandlerAttribute(typeof(UIElement), typeof(TestAlcHandler))]

internal static class TestAlcHandler
{
    public static void CaptureState(FrameworkElement element, IDictionary<string, object> dict, Type[] types)
        => dict["test"] = "captured";

    public static Task RestoreState(FrameworkElement element, IDictionary<string, object> dict, Type[] types)
    {
        // no-op for test
        return Task.CompletedTask;
    }
}
```

Load in a `new AssemblyLoadContext("TestAlc", isCollectible: true)`, trigger `LoadElementUpdateHandlerActions()`, assert.

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| FullName fallback matches wrong overload | Wrong method invoked | Only fall back when strict match fails; verify parameter count + return type + name |
| Inner-ALC handler invoked for host-side elements | Unexpected behavior | Handler lookup is scoped by ALC detection on the element; host elements only get shared handlers |
| Delegate prevents ALC collection | Memory leak | `ConditionalWeakTable` + `ALC.Unloading` proactive cleanup + unit test proving collection |
| ConditionalWeakTable entry not removed fast enough | Stale handlers invoked briefly | `ALC.Unloading` callback clears delegates immediately; CWT removal is secondary safety net |
| Performance of FullName comparison in GetHandlerMethod | Slower resolution | Only on fallback path; handler resolution happens once at assembly load, not per hot-reload |

---

## Design Decisions

1. **`AlcContentHost` exposes its ALC.** Add an `AssemblyLoadContext? LoadContext` property to `AlcContentHost`. Cleaner than inferring it from `element.GetType().Assembly` at every tree walk step.

2. **Single active inner ALC for now, but `ConditionalWeakTable` for safety.** Only one sub-ALC is supported today. Using `ConditionalWeakTable` is forward-compatible and avoids having to rework if multiple ALCs become supported later.

3. **`CreateDelegate` on cross-ALC `MethodInfo` — must be validated.** Known issue: delegates have shown erratic behavior with HR + ALC on WASM specifically. The happy path (shared parameter types, host delegate type, inner-ALC `MethodInfo`) should work, but **a dedicated unit test must cover this** (`When_HandlerInSecondaryAlc_Then_DelegatesInvocable`). If `CreateDelegate` fails on any platform, fall back to `MethodInfo.Invoke` with a thin wrapper.
