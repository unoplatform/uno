# Feature Specification: ALC-Scoped Hot-Reload Handlers + Multi-Handler Support

**Repo**: `uno-private` (Uno.UI.RemoteControl)
**Created**: 2026-03-23
**Status**: Draft
**Input**: Hot-reload handler discovery breaks in multi-ALC scenarios because agents scan assemblies from all ALCs and only support one handler per element type.

---

## Problem Statement

### Current Architecture

`ElementUpdateAgent` and `HotReloadAgent` (per-ALC singletons in `ClientHotReloadProcessor`) discover hot-reload handlers by scanning `AppDomain.CurrentDomain.GetAssemblies()`.

Each ALC loads its own copy of `Uno.UI.RemoteControl`, so `ClientHotReloadProcessor` and its agents are per-ALC singletons. However, `AppDomain.CurrentDomain.GetAssemblies()` returns assemblies from **all** ALCs.

### Bug 1 — Cross-ALC assembly scanning

When an agent in ALC-A scans all assemblies, it encounters assemblies from ALC-B. For handlers in ALC-B assemblies:

1. `GetHandlerMethod(handlerType, "CaptureState", ..., parameterTypes)` **fails** because `parameterTypes` contains `typeof(FrameworkElement)` from ALC-A, but the handler method's parameters expect `FrameworkElement` from ALC-B. These are different `Type` instances.
2. The handler entry is created with all method delegates remaining null (empty handler).
3. This empty handler **overwrites** the previously-good handler for the same element type.

### Bug 2 — Single handler per element type

`_elementHandlerActions[elementType] = updateActions` overwrites any previous handler for the same element type. Even within a single ALC, two assemblies registering handlers for the same element type (e.g. `FrameworkElement`) silently clobber each other.

### Bug 3 — Unsoped tree walk

`ReloadWithUpdatedTypes` is called once per ALC (since each ALC has its own `ClientHotReloadProcessor`). Without scoping, each call traverses the entire visual tree including elements owned by other ALCs, leading to duplicate processing.

### Consequence

Libraries loaded in a secondary ALC cannot define hot-reload handlers without colliding with the host's handlers. The host must implement workaround handlers using cross-ALC reflection, which is fragile and creates coupling.

---

## Goals

1. Each agent only scans assemblies from its own ALC (`_alc.Assemblies`).
2. Multiple handlers can coexist for the same element type (additive, not overwrite).
3. Visual tree traversal is scoped per ALC — each agent processes only its own subtree.
4. `AlcContentHost.LoadContext` is auto-populated from content type's ALC.
5. No breaking change to existing handler registration for non-ALC scenarios.

## Non-Goals

- Modifying ALC sharing rules (host-specific concern).
- Changing the `ElementMetadataUpdateHandlerAttribute` API.
- Cross-ALC method resolution (not needed when each agent only sees its own ALC).

---

## Design

### Part 1 — ALC-scoped assembly enumeration

Store the owning ALC in each agent:

```csharp
private readonly AssemblyLoadContext _alc;

// In constructor:
_alc = AssemblyLoadContext.GetLoadContext(typeof(ElementUpdateAgent).Assembly)
    ?? AssemblyLoadContext.Default;
```

Replace all `AppDomain.CurrentDomain.GetAssemblies()` with `_alc.Assemblies.ToArray()`:

```csharp
var sortedAssemblies = TopologicalSort(_alc.Assemblies.ToArray());
```

Filter `AssemblyLoad` events by ALC:

```csharp
private void OnAssemblyLoad(object? _, AssemblyLoadEventArgs eventArgs)
{
    if (AssemblyLoadContext.GetLoadContext(eventArgs.LoadedAssembly) == _alc)
    {
        LoadElementUpdateHandlerActions();
    }
}
```

**Rationale**: Since each ALC has its own agent instance, each agent only needs to see its own ALC's assemblies. This eliminates cross-ALC type mismatches entirely — no FullName fallback, no DynamicMethod bridges, no split storage needed.

### Part 2 — Multi-handler per element type

Change the storage from single handler to a list:

```csharp
// BEFORE:
private readonly ConcurrentDictionary<Type, ElementUpdateHandlerActions> _elementHandlerActions = new();

// AFTER:
private readonly ConcurrentDictionary<Type, ImmutableList<ElementUpdateHandlerActions>> _elementHandlerActions = new();
```

In `GetElementHandlerActions`, append instead of overwrite:

```csharp
_elementHandlerActions.AddOrUpdate(
    elementType,
    _ => ImmutableList.Create(updateActions),
    (_, existing) => existing.Add(updateActions));
```

Expose as:

```csharp
public ImmutableDictionary<Type, ImmutableList<ElementUpdateHandlerActions>> ElementHandlerActions
    => _elementHandlerActions.ToImmutableDictionary();
```

### Part 3 — Scoped visual tree walk

In `EnumerateHotReloadInstances`, when encountering an `AlcContentHost`, check if its `LoadContext` matches the current agent's ALC. If the `LoadContext` is non-null and does NOT match the current ALC, skip its children — they belong to a different ALC's agent.

This ensures:
- The host ALC's agent processes the host visual tree and stops at `AlcContentHost` boundaries.
- The inner ALC's agent finds the `AlcContentHost` whose `LoadContext` matches, and processes only that subtree.

### Part 4 — Auto-populate `AlcContentHost.LoadContext`

Add the `LoadContext` property and auto-populate in `OnContentChanged`:

```csharp
public System.Runtime.Loader.AssemblyLoadContext? LoadContext { get; set; }

protected override void OnContentChanged(object oldContent, object newContent)
{
    base.OnContentChanged(oldContent, newContent);
    _contentApplication = Application.GetForInstance(newContent);
    LoadContext = newContent is not null
        ? System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(newContent.GetType().Assembly)
        : null;
    UpdateMergedResources();
}
```

---

## Affected Files

| File | Change |
|------|--------|
| `Uno.UI.RemoteControl/HotReload/MetadataUpdater/ElementUpdaterAgent.cs` | ALC-scoped assembly scan; multi-handler storage (`ImmutableList`); filter `OnAssemblyLoad` by ALC |
| `Uno.UI.RemoteControl/HotReload/MetadataUpdater/HotReloadAgent.cs` | ALC-scoped assembly scan in `GetMetadataUpdateHandlerActions`, `ApplyDeltas`, `GetMetadataUpdateTypes`; filter `OnAssemblyLoad` by ALC |
| `Uno.UI.RemoteControl/HotReload/ClientHotReloadProcessor.MetadataUpdate.cs` | Consume `ImmutableList<ElementUpdateHandlerActions>` (SelectMany in LINQ, foreach in Before/After/ReloadCompleted) |
| `Uno.UI.RemoteControl/HotReload/ClientHotReloadProcessor.Common.cs` | Scope `EnumerateHotReloadInstances` by ALC — skip `AlcContentHost` children when `LoadContext` doesn't match |
| `Uno.UI/UI/Xaml/Window/AlcContentHost.cs` | Add `LoadContext` property; auto-populate in `OnContentChanged` |

---

## Implementation Checklist

- [ ] **ALC-scoped assembly scan** in `ElementUpdateAgent` — `_alc.Assemblies.ToArray()` replaces `AppDomain.CurrentDomain.GetAssemblies()`.
- [ ] **Multi-handler storage** — `ConcurrentDictionary<Type, ImmutableList<ElementUpdateHandlerActions>>` with `AddOrUpdate` append.
- [ ] **ALC-scoped assembly scan** in `HotReloadAgent` — same pattern.
- [ ] **Filter `OnAssemblyLoad`** — both agents only reload when the loaded assembly is in their own ALC.
- [ ] **Multi-handler consumption** — `ClientHotReloadProcessor` iterates lists of handlers.
- [ ] **Scoped tree walk** — `EnumerateHotReloadInstances` skips `AlcContentHost` children from other ALCs.
- [ ] **Auto-populate `LoadContext`** on `AlcContentHost`.
- [ ] **Unit tests** in `Uno.UI.RemoteControl.DevServer.Tests`.

---

## Testing

### Test project

`Uno.UI.RemoteControl.DevServer.Tests` — MSTest, already links source files from `Uno.UI.RemoteControl`.

### Unit tests

File: `Uno.UI.RemoteControl.DevServer.Tests/HotReload/ElementUpdateAgentCrossAlcTests.cs`

| Test | Description |
|------|-------------|
| `When_DefaultAlc_Then_HandlersDiscovered` | Agent finds handlers from own ALC. Regression guard. |
| `When_MultipleHandlersForSameType_Then_AllRegistered` | Two handlers registered for same element type → both present in the list. |
| `When_AssemblyLoadInOtherAlc_Then_NoReload` | `AssemblyLoad` event from a different ALC does not trigger re-scan. |
| `When_AlcScopedAgent_Then_OnlyOwnAssemblies` | Agent only sees assemblies from its own ALC. |

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| `_alc.Assemblies` may not include all needed assemblies at agent init time | Missing handlers | `OnAssemblyLoad` reloads handlers when new assemblies load in the same ALC |
| Multiple handlers for same element type cause ordering issues | Unexpected behavior | Handlers are appended in topological sort order; consumers iterate all handlers |
| `EnumerateHotReloadInstances` misses elements | Incomplete hot-reload | Only skips `AlcContentHost` children when `LoadContext` is non-null and differs from current ALC |

---

## Design Decisions

1. **ALC-scoping over cross-ALC bridging.** Since each ALC has its own agent, scanning only own assemblies is simpler and eliminates all cross-ALC type identity issues. No FullName fallback, no DynamicMethod bridges, no ConditionalWeakTable needed.

2. **`ImmutableList<ElementUpdateHandlerActions>` per type.** Additive handler registration is more composable and avoids silent overwrites. Two libraries can register handlers for `FrameworkElement` without collision.

3. **`AlcContentHost.LoadContext` for tree walk scoping.** The property is auto-populated from the content's assembly ALC. The tree walker checks this property to decide whether to traverse children or leave them to the other ALC's agent.
