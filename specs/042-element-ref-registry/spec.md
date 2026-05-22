# Spec 042: Element Ref Handle Registry

> **Status**: Under Review - 2026-05-20
> **Author**: Carl de Billy
> 2026-05-19 - Draft
> 2026-05-20 - Under Review

---

## Executive Summary

### The Problem

Two Uno tooling systems — the Uno App MCP server (`uno.app-mcp`) and Hot Design
(`Uno.HotDesign.Client`) — both need to **identify and address individual visual-tree
elements across multiple tool calls within the same session**.

Today, `uno.app-mcp` ships its own private implementation (`ElementRefRegistry`)
inside `Uno.UI.App.Mcp.Client`. Hot Design has its own `ElementId` (Guid) system
for source-level identity. When MCP tooling and Hot Design operate together — which
is the primary use-case — there is no shared handle: a ref obtained via one system
cannot be used by the other, and each system must independently look up or describe
the same on-screen element.

The consequences:

- **No interoperability**: an element selected in Hot Design cannot be handed to an
  MCP tool by reference; either system must re-discover it independently.
- **Duplication**: `uno.app-mcp` and any future tooling must each re-implement
  weak-ref registries with no shared contract.
- **No canonical contract**: consumers have no stable API to depend on; formats are
  implementation details hidden behind private classes.

### What We're Shipping

A minimal, stable API surface in `Uno.UI` that allows any Uno tooling component to:

1. Obtain a short, opaque string handle for a live `DependencyObject` on demand.
2. Resolve such a handle back to a live `DependencyObject`.

```csharp
// Obtain or retrieve the handle for a live object (creates on first call)
string handle = ElementRefHandle.GetOrCreate(element);

// Resolve back to a live object (returns false if GC'd or unknown)
bool found = ElementRefHandle.TryResolve(handle, out DependencyObject? obj);
```

The handle is a **short, opaque, token-efficient string** (base-36 encoding of a
monotonic integer, typically 1–7 characters). The format is an implementation
detail; callers must treat it as opaque. Handles are compared **ordinal,
case-insensitive**.

### Why This Approach

- **Minimal surface**: one static class + one interface, no lifecycle hooks, no events.
- **Token-efficient**: short handles are suitable for AI agent contexts.
- **Ephemeral by design**: handles are valid for the lifetime of the object in the
  current session. They do not survive app restarts or devserver resets.
- **No object rooting**: handles are backed entirely by weak references. The registry
  never prevents an object from being garbage-collected.
- **Interoperable**: once a handle is obtained through any Uno tooling path, any
  other tooling component can resolve it. Note: the bridge between Hot Design's
  `ElementId` and `ElementRefHandle` is implemented in `Uno.HotDesign.Client` (out
  of scope for this spec).
- **Mockable**: `IElementRefHandleRegistry` allows consumers to inject or replace
  the registry in tests.

### Scope

- Public API: `Uno.UI.Diagnostics.ElementRefHandle` (static facade) and
  `Uno.UI.Diagnostics.IElementRefHandleRegistry` (interface).
- Internal implementation: `Uno.UI.Diagnostics.ElementRefHandleRegistry` in `Uno.UI`.
- Handle format: base-36 encoded `int` (implementation detail, not part of the contract).
- Consumers: `Uno.UI.App.Mcp.Client` (replaces its private `ElementRefRegistry`) and
  `Uno.HotDesign.Client` (interoperability with its existing `ElementId` system).
- Not in scope: XAML/XML serialization of the visual tree, element bounds or metadata,
  persistence across sessions, Hot Design `ElementId` ↔ handle bridge.

---

## 1. Public API

Namespace: `Uno.UI.Diagnostics`
Assembly: `Uno.UI`

```csharp
namespace Uno.UI.Diagnostics;

/// <summary>
/// Provides short opaque handles to live <see cref="DependencyObject"/> instances
/// for use by diagnostic and tooling components within a session.
/// </summary>
/// <remarks>
/// Handles are ephemeral: they are valid only as long as the object is alive in
/// the current session. They do not survive application restarts or devserver resets.
/// <para>
/// The handle format is an implementation detail; callers must treat handles as
/// opaque strings. Handle comparison is ordinal, case-insensitive.
/// </para>
/// <para>
/// Both methods must be called from the UI thread. A violation throws
/// <see cref="InvalidOperationException"/> unless
/// <see cref="FeatureConfiguration.ElementRefHandle.DisableThreadingCheck"/> is set.
/// </para>
/// </remarks>
public static class ElementRefHandle
{
    /// <summary>
    /// Gets the active registry instance. Defaults to
    /// <see cref="ElementRefHandleRegistry"/>.
    /// </summary>
    /// <remarks>
    /// Read-only in production. Replaced internally for testing scenarios via
    /// <c>ElementRefHandle.SetForTesting(IElementRefHandleRegistry)</c>.
    /// </remarks>
    public static IElementRefHandleRegistry Default { get; }

    /// <summary>
    /// Returns the opaque handle for <paramref name="element"/>,
    /// creating one if this is the first call for this object.
    /// </summary>
    /// <param name="element">The object to identify. Must not be null.</param>
    /// <returns>A short opaque string that can be passed to <see cref="TryResolve"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when not called from the UI thread (unless
    /// <see cref="FeatureConfiguration.ElementRefHandle.DisableThreadingCheck"/> is set).
    /// </exception>
    public static string GetOrCreate(DependencyObject element)
        => Default.GetOrCreate(element);

    /// <summary>
    /// Attempts to resolve a previously obtained handle back to its object.
    /// </summary>
    /// <param name="handle">The opaque handle string. <see langword="null"/> or empty returns <see langword="false"/>.</param>
    /// <param name="element">
    /// When this method returns <see langword="true"/>, the live object;
    /// otherwise <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the handle is known and the object is still alive;
    /// <see langword="false"/> if the handle is <see langword="null"/>, empty,
    /// unrecognized, or the object has been garbage-collected.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when not called from the UI thread (unless
    /// <see cref="FeatureConfiguration.ElementRefHandle.DisableThreadingCheck"/> is set).
    /// </exception>
    public static bool TryResolve(string? handle, [NotNullWhen(true)] out DependencyObject? element)
        => Default.TryResolve(handle, out element);
}

/// <summary>
/// Defines the contract for a session-scoped registry of opaque handles
/// to live <see cref="DependencyObject"/> instances.
/// </summary>
/// <remarks>
/// This interface is public to allow injection in consumer tests
/// (e.g. <c>Uno.UI.App.Mcp.Client</c>, <c>Uno.HotDesign.Client</c>).
/// Future additions will be made as default interface methods to preserve
/// binary compatibility.
/// </remarks>
public interface IElementRefHandleRegistry
{
    /// <inheritdoc cref="ElementRefHandle.GetOrCreate"/>
    string GetOrCreate(DependencyObject element);

    /// <inheritdoc cref="ElementRefHandle.TryResolve"/>
    bool TryResolve(string? handle, [NotNullWhen(true)] out DependencyObject? element);
}
```

### Thread safety

Both `GetOrCreate` and `TryResolve` must be called from the **UI thread**. This
constraint is **enforced at runtime**: a violation throws `InvalidOperationException`.
The check follows the same pattern as `DependencyProperty.GetProperty`
(`src/Uno.UI/UI/Xaml/DependencyProperty.cs`) and can be disabled via
`FeatureConfiguration.ElementRefHandle.DisableThreadingCheck`.

The `RefEntry` finalizer (reverse-map cleanup) is **exempt** from this check: it
runs on the finalizer thread and uses only `ConcurrentDictionary.TryRemove`, which
is thread-safe by design.

### Null handling

`GetOrCreate` throws `ArgumentNullException` on a null element. `TryResolve` with
a null or empty handle returns `false` without throwing.

---

## 2. Handle Format

The handle is the base-36 encoding of a monotonic `int` counter. Examples: `"1"`,
`"z4"`, `"3w5e"`. The maximum length for a positive 32-bit integer is 7 characters.

The format is documented here for transparency, but is **not a contract**. Callers
must never parse, inspect, or generate handles. Future implementations may change
the encoding without notice as long as existing handles from the current session
remain resolvable.

**Handle type**: the handle is a plain `string`. This is intentional: handles are
frequently embedded in JSON payloads or AI agent prompts where a native string
representation is preferable to a custom struct.

**Comparison rule**: handles are compared **ordinal, case-insensitive**. This
protects against involuntary case-folding by intermediate transports — particularly
AI agents (small models may "normalise" short tokens that look like words: `"cat"` →
`"Cat"`, `"fox"` → `"FOX"`) and text serialisation paths that uppercase or
title-case identifiers.

**Counter lifetime**: the counter resets each process start. The first handle issued
in a session has an unspecified value (implementation detail). Handles never recycle
within a session.

**Counter limit**: `int.MaxValue` (~2.1 billion). Reaching it would require
allocating billions of `DependencyObject` instances in a single session, which is
not a realistic scenario for tooling workloads. If a future use case approaches this
limit, the handle-generation algorithm will be revised; the public contract is
unaffected because the format is opaque.

**Security model — capability-by-knowledge**: handles are monotonic and predictable
by design. An adversary with the ability to call `TryResolve` arbitrarily could
enumerate the keyspace to address any registered element. This is acceptable because
the registry is only consumed by trusted tooling (MCP server, Hot Design) within a
developer's own session; there is no production attack surface. If a future scenario
introduces an untrusted caller, a per-session nonce can be added without changing
the public contract (the format is opaque).

Rationale for base-36 (vs. UUID/GUID): substantially shorter, URL-safe without
encoding, and well within token budgets for AI agent contexts.

---

## 3. Lifetime and Invalidation

- A handle is **valid** from its creation until its `DependencyObject` is
  garbage-collected or the application session ends.
- The registry holds **only weak references** to objects. No object is rooted by
  the registry.
- `TryResolve` performs lazy cleanup: when it finds that the object for a given
  handle has been collected, it removes the reverse-map entry and returns `false`.
- A `RefEntry` finalizer performs **eager cleanup** of the forward map entry (via
  `ConditionalWeakTable`) and the reverse map entry (via `ConcurrentDictionary`)
  when the object is collected.
- **Hot reload**: handles are not guaranteed to survive a hot-reload cycle that
  re-materializes objects. Callers that require stability across hot-reload must
  re-acquire handles after a reload event. This limitation is tracked as a future
  improvement (see §8 Open Items).

---

## 4. Internal Implementation

### `ElementRefHandleRegistry` (internal)

Located at `src/Uno.UI/Diagnostics/ElementRefHandleRegistry.cs`, namespace
`Uno.UI.Diagnostics`. Implements `IElementRefHandleRegistry`.

```
┌─────────────────────────────────────────────────────────────────┐
│  ElementRefHandleRegistry  (internal sealed)                    │
│                                                                 │
│  _table   : ConditionalWeakTable<DependencyObject, RefEntry>    │
│              ──► forward map; GC collects entry when key dies   │
│                                                                 │
│  _reverse : ConcurrentDictionary<int, WeakReference<T>>         │
│              ──► reverse map; cleaned lazily + by finalizer     │
│                                                                 │
│  _nextId  : int   (++, UI thread only)                          │
└─────────────────────────────────────────────────────────────────┘
```

**`RefEntry`** is a private sealed class holding the numeric id. Its finalizer calls
`OnObjectCollected(id)` to remove the reverse-map entry once the
`ConditionalWeakTable` releases it.

**Thread enforcement**: `GetOrCreate` and `TryResolve` start with the standard Uno
UI-thread guard, mirroring `DependencyProperty.GetProperty`:

```csharp
if (!FeatureConfiguration.ElementRefHandle.DisableThreadingCheck
    && !NativeDispatcher.Main.HasThreadAccess)
{
    throw new InvalidOperationException(
        "ElementRefHandle should not be accessed from a non-UI thread.");
}
```

The finalizer path is exempt (runs on finalizer thread; uses only
`ConcurrentDictionary.TryRemove`).

**Weak references**: `GetOrCreate` stores a plain `WeakReference<DependencyObject>`
per element. The reference is created inside the `ConditionalWeakTable.GetValue`
factory (invoked at most once per element on the UI thread) so that the reverse-map
insertion is always paired with the forward-map entry.

**Logging**: traces emitted only at `Trace` level, conditioned on
`Logger.IsEnabled(LogLevel.Trace)` to avoid cost in production. Log format uses
`{HandleString}` and `{ObjectTypeName}` only — never `obj.ToString()` (PII risk for
TextBox/PasswordBox content).

### `ElementRefHandle` (public facade)

Located at `src/Uno.UI/Diagnostics/ElementRefHandle.cs`. Thin static wrapper that
delegates to `ElementRefHandle.Default`, which is initialized to an instance of
`ElementRefHandleRegistry`. The `Default` property is get-only; it is replaced for
testing via `internal static void SetForTesting(IElementRefHandleRegistry)`,
accessible from test assemblies via `[InternalsVisibleTo]`.

### File layout

```
src/Uno.UI/Diagnostics/
  IElementRefHandleRegistry.cs    ← public interface
  ElementRefHandle.cs             ← public static facade
  ElementRefHandleRegistry.cs     ← internal sealed class (IElementRefHandleRegistry)
```

---

## 5. Relationship with Hot Design's `ElementId`

Hot Design (`Uno.HotDesign.Client`) has its own element-identity system based on
`ElementId` (a `Guid`), issued by the devserver, stored as an attached property, and
designed for XAML-source stability across reparses.

These two systems serve **different purposes** and are **complementary**:

| | `ElementRefHandle` (this spec) | Hot Design `ElementId` |
|---|---|---|
| **Primary use** | Session-scoped tooling (MCP, diagnostics) | XAML designer, property editor |
| **ID format** | Short base-36 string (token-efficient) | Guid ("D" format) |
| **Issuing authority** | App-side, on-demand | Devserver-issued, eagerly at parse time |
| **Storage** | In-memory CWT (no object mutation) | Attached property on the `DependencyObject` |
| **Hot-reload stability** | Not guaranteed (see §8) | Maintained via element-id matcher |
| **GC semantics** | Weak-ref only, no rooting | Attached DP keeps a string value on the element |

**Interoperability**: Hot Design tooling that needs to bridge between the two systems
will use `ElementRefHandle.GetOrCreate` to obtain a short handle for an element whose
`ElementId` is already known, and will emit that handle alongside the `ElementId` in
its responses. This enables MCP tool calls to refer to an element selected in Hot
Design using a single, token-efficient string. The bridge code lives in
`Uno.HotDesign.Client` and is out of scope for this spec.

---

## 6. Tests

**Location**: `src/Uno.UI.Tests/Diagnostics/Given_ElementRefHandleRegistry.cs`

| Test | Description |
|------|-------------|
| `GetOrCreate_SameElement_ReturnsSameHandle` | Idempotence: multiple calls for the same object return the same string. Ensures token-budget stability when the same element is referenced repeatedly. |
| `GetOrCreate_DifferentElements_ReturnsDifferentHandles` | Two distinct objects get distinct handles. |
| `TryResolve_ValidHandle_ReturnsTrueAndElement` | Round-trip: resolve the handle back to the object. Drops the intermediate strong reference before resolving to prove the registry does not implicitly root the object. |
| `TryResolve_UnknownHandle_ReturnsFalse` | An unknown handle string returns false. |
| `TryResolve_NullOrEmpty_ReturnsFalse` | Null and empty string return false without throwing. |
| `TryResolve_AfterGC_ReturnsFalse` | After forcing GC on the object, `TryResolve` returns false and the reverse map is cleaned (covers both lazy cleanup and finalizer path). |
| `TryResolve_HandleComparison_IsCaseInsensitive` | The same handle in upper and lower case resolves to the same object. |
| `GetOrCreate_FromBackgroundThread_Throws` | Calling `GetOrCreate` off the UI thread throws `InvalidOperationException`. |
| `TryResolve_FromBackgroundThread_Throws` | Calling `TryResolve` off the UI thread throws `InvalidOperationException`. |
| `GetOrCreate_WithDisableThreadingCheck_DoesNotThrow` | Setting `FeatureConfiguration.ElementRefHandle.DisableThreadingCheck` suppresses the thread check. |
| `Finalizer_RemovesReverseMapEntry` | After GC, the finalizer eagerly removes the reverse-map entry before `TryResolve` is ever called. Distinguishes eager cleanup from lazy. |
| `Handles_DoNotRecycle_AfterGC_WithinSession` | After an object is collected, a new registration for a fresh object receives a different handle — no id recycling within a session. |
| `SetForTesting_RestoresDefaultOnDispose` | `SetForTesting` returns an `IDisposable`; on dispose, `Default` is restored to the original registry. |

---

## 7. Migration: `Uno.UI.App.Mcp.Client`

`uno.app-mcp` currently ships a private `ElementRefRegistry` in
`Uno.UI.App.Mcp.Client`. Once this spec is implemented and a new Uno NuGet is
available, the following changes are made to `uno.app-mcp` in a single PR:

1. Bump the Uno SDK/package version to the one containing `ElementRefHandle`.
2. Replace all call sites of `ElementRefRegistry.GetOrCreateId(...)` with
   `ElementRefHandle.GetOrCreate(...)`.
3. Replace all call sites of `ElementRefRegistry.TryGetElement(...)` with
   `ElementRefHandle.TryResolve(...)`.
4. Delete `src/Uno.UI.App.Mcp.Client/Helpers/ElementRefRegistry.cs`.

Handles are opaque and their format is not part of the contract. Any handles
produced by the old private registry are not expected to be resolvable by the new
public one; the migration removes the old registry in the same PR so no parallel
operation occurs.

---

## 8. Open Items

| # | Item | Priority |
|---|------|----------|
| 1 | **Hot-reload stability**: handles are not guaranteed to survive a hot-reload cycle that re-materializes objects. Define a notification mechanism (e.g., `IHotReloadHandler`) that allows the registry to re-associate or invalidate handles when objects are replaced. Note: the reference implementation in `uno.app-mcp` does not handle this scenario either; to be addressed when a concrete need arises. | P2 |
| 2 | **Cross-process protection**: handles are session-scoped and in-process. A future hardening step could reject handles that were not issued by the current session (e.g., via a per-session nonce prefix), to guard against handles being accidentally forwarded across process boundaries. | P3 |
| 3 | **Registration hook**: expose a registration event or callback for tooling that wants to observe when objects receive or lose handles (e.g., to update a visual overlay). | P3 |
| 4 | **Finalizer-free cleanup**: evaluate whether the `RefEntry` finalizer is necessary or whether lazy cleanup in `TryResolve` plus a periodic sweep suffices, to reduce pressure on the finalization queue. | P3 |
| 5 | **Metrics**: expose a `Meter` `Uno.UI.Diagnostics.ElementRefHandle` (Counter `elementref.handle.created` / `elementref.handle.miss`, Gauge `elementref.handle.table_size`) for runtime observability. | P3 |
