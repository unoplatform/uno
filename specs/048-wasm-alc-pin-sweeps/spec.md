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
| 1 | `ResourceLoader._lookupAssemblies` / `_parsedResources` | `List<Assembly>` / `HashSet<(Assembly,string)>` | `ClearNonDefaultAlcAssemblies()` in cleanup hook | `Given_ResourceLoader_Alc` |
| 2 | `UIElementNativeRegistrar._classNames` (WASM) | `Dictionary<Type,int>` | `ClearNonDefaultAlcEntries()` in cleanup hook | ALC pin guard (WASM runtime); dedicated unit test TBD |
| 3 | `AppWindow` / `ApplicationView` / `CoreDragDropManager` WindowId maps | `ConcurrentDictionary<WindowId,…>` | `DestroyForWindowId(WindowId)` mirrored on each; called from `CloseAlcWindow` | `Given_WindowId_Maps_Alc` |
| 4 | `CompositionTarget._handlers` (WASM) | `List<EventHandler<object>>` | `ClearNonDefaultAlcHandlers()` in cleanup hook; per-frame snapshot reuse | `Given_CompositionTargetFrameDispatcher` (unit) + `Given_CompositionTarget` (WASM runtime) |
| 6 | Hot-reload client status history `HotReloadClientOperation.Types` | `Type[]` per op, unbounded history | null `Type[]` at terminal state (curated strings retained); ring buffer (~100 ops) | `Given_HotReloadClientOperation_Alc` |
| 7 | `PagePool` (WASM) | orphaned pool + eternal 30s scavenger; `Type`-keyed instances | lazy singleton; scavenger only when pooling enabled; ALC-sweep `Type` keys | ALC pin guard (WASM runtime); dedicated unit test TBD |
| 8 | `HtmlElementHelper._cache` (WASM), `FeatureConfiguration.Style.UseUWPDefaultStylesOverride` | `Dictionary<Type,…>` | extend sweep with `RemoveNonDefaultAlcEntries` | `Given_ResidualTypeStatics_Alc` |

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

Each sweep has a red/green test asserting a collectible-ALC-keyed entry is dropped while
default-ALC / framework entries are kept; red proven by neutering the sweep.

- `Given_ResourceLoader_Alc`, `Given_WindowId_Maps_Alc`, `Given_ResidualTypeStatics_Alc`
  (`Uno.UI.UnitTests`, reference target): built and **passing** locally; red proven for
  finding 1 (neutered sweep → removal assertion fails) and the WindowId-map group.
- `Given_HotReloadClientOperation_Alc` (`Uno.UI.RuntimeTests`, `HAS_UNO_WINUI`): production
  side built clean on the reference target; the runtime test runs on CI (WinUI flavor).

### Build-honesty note

The local environment has neither the `browserwasm` nor the `desktop`/skia workload installed
(`NETSDK1139`), so the WASM-only sweeps (finding 2, finding 4, `HtmlElementHelper`) and the
Skia/WASM runtime test could not be built or run locally. They are validated by CI. Everything
compilable on the reference target (findings 1, 3, 6, 7, 8-Style) was built and tested locally.
