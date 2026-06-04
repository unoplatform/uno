# 044 — Collectible-ALC Memory Leak Fixes (XamlParseContext, AlcContentHost, ResourceDictionary)

## Problem

A downstream consumer hosts secondary applications in collectible `AssemblyLoadContext`s
(see spec 000). On every secondary-app reload, `ALC.Unload()` was called but the ALC was
never collected: each reload leaked one full copy of the previous app's object graph
(≈ +48 MB managed / +420K live objects / +150 MB RSS per reload on a representative app),
reaching OOM within ~10–15 reloads.

`dotnet-dump` SOS `gcroot` on a leaked secondary `Application` instance (~11,000 unique
root paths) identified three independent pinning mechanisms in Uno.UI, all surviving
`Application.CleanupNonDefaultAlcCaches()`.

## Root causes

### 1. `XamlParseContext` strongly captures the ALC

`XamlParseContext._assemblyLoadContext` was a strong field. Parse contexts are retained
long-term by `ResourceBinding.ParseContext` and `ThemeResourceReference.ParseContext` on
elements reachable from non-collectible statics (generated `GlobalStaticResources`
singletons of pooled assemblies, host-side singletons). Additionally, the lazy
by-`AssemblyName` resolution in the getter scans `AppDomain.CurrentDomain.GetAssemblies()`
and could permanently cache a **collectible** ALC inside a **never-dying static**
`__ParseContext_` instance. Any retained ALC reference pins the LoaderAllocator and the
entire previous app graph.

### 2. `AlcContentHost` retains projected resources after teardown

`AlcContentHost.UpdateMergedResources()` cleared and re-merged `MergedDictionaries`, but
never removed previously **copied** theme-dictionary entries and direct resources. After
the secondary app's content was cleared, its brushes/styles/templates (potentially typed
in the collectible ALC) stayed in the host control's `Resources` forever.

### 3. `ResourceDictionary` not-found cache pins collectible `Type` keys

Typed resource lookups (e.g. implicit-style probes for secondary-app element types
hosted in the host visual tree) that miss are cached in the dictionary's
`_keyNotFoundCache` as `ResourceKey`s holding the **`RuntimeType`** strongly. Element-level
dictionaries (e.g. a host page's `Resources`) live for the host's lifetime and are never
touched by `CleanupNonDefaultAlcCaches` (which only invalidates the current Application's
cache, without propagation). A single cached collectible `RuntimeType` pins
`LoaderAllocator → ALC → entire previous app graph`. gcroot showed 10,378 root paths
through one such cache entry.

## Fixes

1. **`XamlParseContext` (UI/Xaml/XamlParseContext.cs):** the ALC is now held via
   `WeakReference<AssemblyLoadContext>`, with a strong fast-path field used only for
   `AssemblyLoadContext.Default` (process-immortal, avoids the allocation). When a
   previously resolved weak target has been collected, the lazy by-name resolution re-runs,
   so a freshly loaded same-name assembly resolves to the NEW ALC — consistent with the
   hot-reload "bump to live registration" behavior in `ResourceResolver`. While a secondary
   app is active its ALC is strongly rooted by the hosting side (live windows, visual tree,
   executing threads), so weakness here only changes post-unload behavior: the getter
   returns `null` and `ResourceResolver.TryTopLevelRetrieval` falls back to the host
   (its documented priority order).

2. **`AlcContentHost` (UI/Xaml/Window/AlcContentHost.cs):** the control now tracks exactly
   which theme-dictionary keys and direct-resource keys it copied, and removes them at the
   start of every `UpdateMergedResources()` before re-copying from the (new) source app.
   Clearing `Content` therefore drops all references to the previous app's resource objects.

3. **`ResourceDictionary` (UI/Xaml/ResourceDictionary.cs):** typed keys whose
   `Type.IsCollectible` is true are never added to `_keyNotFoundCache`
   (`CanCacheKeyNotFound`). Re-probing such keys is cheap compared to leaking an entire
   secondary app; non-collectible keys keep the existing caching behavior.

## Validation

Red/fix/green (each test fails on the pre-fix code):

- `Uno.UI.Tests/Windows_UI_Xaml/Given_XamlParseContext.cs`
  - `When_Collectible_Alc_Unloaded_Then_Context_Does_Not_Pin_It` — a strongly-held parse
    context must not keep an unloaded collectible ALC alive; getter returns null after
    collection. (Red before fix 1; green after.)
  - Live-ALC and default-ALC behavioral tests (pass before and after).
- `Uno.UI.Tests/Windows_UI_Xaml/Given_ResourceDictionary_NotFoundCache.cs`
  - `When_Collectible_Type_Key_Misses_Then_Cache_Does_Not_Pin_Type` — uses a
    `RunAndCollect`-emitted type; the dictionary must not pin it after a not-found probe.
    (Red before fix 3; green after.)
  - Non-collectible keys still miss consistently (behavioral guard).
- `Uno.UI.RuntimeTests/Tests/AssemblyLoadContext/Given_AlcContentHost.cs`
  - `When_SecondaryAppContentCleared_Then_ProjectedResourcesRemovedFromHost` — direct
    resources and theme dictionaries projected from the secondary app (new fixtures in
    `AlcApp/App.xaml`) are removed when content is cleared. (Red before fix 2; green after.)
- Full unit suite: identical pass/fail set with and without the fixes (baseline-compared);
  zero regressions. Full ALC runtime suite: 33 passing; one pre-existing order-dependent
  flake (`When_TopLevelLookupFromSecondaryAlcContext_HasHostValue_Then_SecondaryWins`)
  fails identically on the unfixed baseline and passes in isolation on the fixed build.
- End-to-end in the secondary-app host: across 4 load/reload cycles with a forced-GC heap
  dump after each, the secondary `Application` instance count stays at **1** (previously
  +1 per reload — one fully leaked app graph each time).

## Notes / follow-ups

- The host process retains small (120-byte) collectible-ALC wrapper shells (+1 per reload,
  no graph behind them) — negligible; candidate follow-up.
- A separate host-side accumulation (~+40 MB/reload of host-side dependency-object and
  resource-initializer trees, not reachable from the secondary app graph) remains and is
  tracked downstream.
