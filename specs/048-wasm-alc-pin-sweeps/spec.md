# 048 — WASM Collectible-ALC Static-Cache Pin Sweeps (Batch 1)

Backing issue: unoplatform/uno#23706

## Problem

A downstream host loads previewed apps into their own **collectible**
`AssemblyLoadContext`s and unloads them on every reload. Each reload calls
`AssemblyLoadContext.Unload()`, but a set of process-lifetime static caches in Uno keep
strong references to the unloaded context's `Assembly` / `Type` / delegate objects, so the
ALC's `LoaderAllocator` is never collected and the whole previous-app object graph leaks.

On WebAssembly the problem is sharper: `AssemblyLoadContext.Unloading` is **never raised**,
so a cache cannot self-clean on a per-context callback. The only teardown seam is the
host-driven cleanup hook `Application.CleanupNonDefaultAlcCaches()` (called from
`Window.CloseAlcWindow`). Every static cache that outlives an app and holds (or transitively
references) that app's `Assembly`/`Type`/delegate must therefore be swept from that hook, or
expose its own ALC-scoped removal that a reachable teardown path can call.

This spec covers Batch 1 of additional pins found by rooting a disposed ALC's
`LoaderAllocator` and following genuine managed roots (strong handle / static / stack) into
it — distinct from the benign runtime dependent-handle residual.

## Existing pattern (matched)

- `Style.ClearCachesForNonDefaultAlc` / `DependencyProperty…TypeNullableDictionary.RemoveNonDefaultAlcEntries`:
  a `Type`-keyed dictionary drops keys whose `Type.IsCollectible` is true, falling back to a
  load-context lookup. Wired into `CleanupNonDefaultAlcCaches`.
- `SystemThemeHelper.ClearNonDefaultAlcHandlers`: an event's invocation list drops entries
  whose target/method assembly resolves to a non-default ALC.
- `DisplayInformation.DestroyForWindowId`: a WindowId-keyed static map removes the closed
  window's entry; called from `Window.CloseAlcWindow`.

## Findings and fixes

| # | Cache | Kind | Fix | Test |
|---|-------|------|-----|------|
| 1 | `ResourceLoader._lookupAssemblies` / `_parsedResources` | `List<Assembly>` / `HashSet<(Assembly,string)>` | `ClearAlcAssemblies(alc)` / `ClearNonDefaultAlcAssemblies()` in cleanup hook (dying-ALC-scoped marker removal, no loader rebuild) | `Given_ResourceLoader_Alc` |
| 2 | `UIElementNativeRegistrar._classNames` (WASM) | `Dictionary<Type,int>` | `ClearNonDefaultAlcEntries()` in cleanup hook (shared `AlcCacheSweep` loop) | **No unit test** — the code is WASM-only and unreachable from the unit-test target; covered by the downstream WASM runtime ALC pin guard (CI) only |
| 3 | `AppWindow` / `ApplicationView` / `CoreDragDropManager` WindowId maps | `ConcurrentDictionary<WindowId,…>` | `DestroyForWindowId(WindowId)` mirrored on each; called from `CloseAlcWindow` | `Given_WindowId_Maps_Alc` (incl. instance-collectibility assertion) |
| 4 | `CompositionTarget._handlers` (WASM) | `List<EventHandler<object>>` | `ClearAlcHandlers(alc)` / `ClearNonDefaultAlcHandlers()` in cleanup hook; ownership predicate extracted to platform-neutral `CompositionTargetHandlerSweep`; per-frame snapshot reuse | `Given_CompositionTargetHandlerSweep` (unit: scoped + all-non-default paths, collectible-method-behind-default-target, static null-target) + `Given_CompositionTargetFrameDispatcher` (unit: buffer lifetime, throw resilience, reentrancy) — `Given_CompositionTarget` (WASM runtime) covers the frame loop, not the ALC sweep |
| 6 | Hot-reload client status history `HotReloadClientOperation.Types` | `Type[]` per op, unbounded history | null `Type[]` at terminal state (curated strings retained); ring buffer (~100 ops) | `Given_HotReloadClientOperation_Alc` |
| 7 | `PagePool` | orphaned pool + eternal 30s scavenger; `Type`-keyed instances | per-`Frame` pools registered in a process-wide WEAK registry (a pool dies with its Frame); shared scavenger only while pooling enabled; ALC-sweep `Type` keys across live pools | `Given_PagePool` (unit: TTL on dequeue, drop-on-disable/re-enable, per-pool isolation, sweep keeps default-ALC keys); collectible-key removal itself: WASM runtime ALC pin guard (CI) |
| 8 | `HtmlElementHelper._cache` (WASM), `FeatureConfiguration.Style.UseUWPDefaultStylesOverride` | `Dictionary<Type,…>` | `HtmlElementHelper`: shared `AlcCacheSweep` loop (WASM-only, CI-covered). `UseUWPDefaultStylesOverride` is USER CONFIGURATION, not a cache: swept only for the dying ALC via `Style.RemoveAlcScopedUserStyleOverrides`, never all-non-default | `Given_ResidualTypeStatics_Alc` (dying swept, sibling + default kept; cache-clear group does not touch user config) |

### Skipped (verified not an ALC pin)

- **Finding 5 — `ImageBrush._naturalSizeCache`**: keyed by data-URI/URL **strings**, not
  `Type`/`Assembly`. Strings carry no ALC identity, so this cache does not pin a collectible
  context — it is an unbounded-growth (memory-bloat) concern, out of scope for ALC-pin
  sweeps. Noted as a follow-up (hash/LRU the data-URI keys).
- **Finding 2 (JS side)** — `WindowManager.uiElementRegistrations`: the JS map stores only
  strings (`typeName`, `classNames`) and holds no managed reference, so it does not pin an
  ALC. Its registration id is a `length`-derived counter shared with still-live
  registrations; deleting JS entries would risk id collisions with live elements. The
  managed `_classNames` sweep alone releases the pin; the JS map is left intact (a few
  duplicate string entries on re-registration are harmless).
- **Finding 8 — `UnicodeText.ICU._lookupCache`**: keyed by `typeof(T)` where `T` is a native
  delegate type **declared inside `ICU`** (framework, default ALC). App types are never keys,
  so it cannot pin a collectible ALC. Left unchanged.

## Out of scope (follow-ups)

- Hot-reload delta pipeline retention.
- Image Blob URL lifetime.
- `FontFamilyLoader` caches.

## Verification

Coverage is NOT uniform across the table — see the per-row Test column for what is actually
asserted where. Rows 1, 3, 4, 6, 7 and 8 have unit tests asserting a collectible/dying-ALC-keyed
entry is dropped while default-ALC / framework / sibling entries are kept. Row 2
(`UIElementNativeRegistrar`) and the collectible-key removal half of row 7 (`PagePool`) have
**no unit test** — that code is WASM-only or requires loading a `Page` type into a collectible
ALC, which the unit-test host cannot do; they are covered only by the downstream WASM runtime
ALC pin guard running in CI.

- `Given_ResourceLoader_Alc`, `Given_WindowId_Maps_Alc`, `Given_ResidualTypeStatics_Alc`,
  `Given_CompositionTargetHandlerSweep`, `Given_CompositionTargetFrameDispatcher`,
  `Given_PagePool` (`Uno.UI.UnitTests`, Skia target): built and **passing** locally.
- `Given_HotReloadClientOperation_Alc` (`Uno.UI.RuntimeTests`, `HAS_UNO_WINUI`): production
  side built clean locally; the runtime test runs on CI (WinUI flavor).
- A green WASM CI run is a **merge precondition**, not a formality: the `#if __WASM__` cleanup
  block (UIElement registrar, HtmlElementHelper, CompositionTarget handler sweep call sites) is
  first type-checked and executed there.

### Build-honesty note

`Uno.UI.Skia` (net10.0), `Uno.UI.Wasm` (net10.0), `Uno.UI.RemoteControl.Skia` (net10.0) and
`Uno.UI.UnitTests` build clean locally, so the WASM-only sweeps (finding 2, finding 4,
`HtmlElementHelper`) are at least compile-verified here. Their runtime BEHAVIOR on WASM (and the
collectible-key removal paths that require a real collectible-ALC-loaded app) is still validated
only by CI — see Verification above.
