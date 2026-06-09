# Hot-reload resource refresh for apps hosted in a secondary ALC

**Status:** Implemented. Diagnostic instrumentation has been removed; only the behavioral change remains.

---

## Context

A host process can load a consumer application into a per-app collectible `AssemblyLoadContext` (ALC) and render it in-process (Skia desktop and WASM). The consumer app's `Application` instance is a **secondary** ALC application — it is registered in `Application`'s ALC registry (`Application.EnumerateSecondaryApplications()`), and is **never** `Application.Current` (which always returns the default-ALC/host application).

## Problem

`ClientHotReloadProcessor.UpdateGlobalResources` (in `HotReload/ClientHotReloadProcessor.MetadataUpdate.cs`) only ever:

- walked `Application.Current.Resources` to find/refresh source-backed merged dictionaries, and
- called `Application.Current.UpdateResourceBindingsForHotReload()`.

When the edited resource dictionary belongs to a **secondary-ALC** app, its source-backed dictionaries live in *that* app's `Application.Resources`, not in `Application.Current` (the host). So a hot-reload edit to a consumer app's source-backed `ResourceDictionary` (e.g. a themed colors/fonts/resources file) was **never refreshed**, and the inner app's visual tree bindings were never re-evaluated.

The host (`Application.Current`) merged-dictionary tree contained zero matches for an edited dictionary, while the inner ALC app contained all the matching source-backed entries.

## Fix

`UpdateGlobalResources` now refreshes the host **and every secondary-ALC application**:

- New `RefreshResourcesForApp(Application? app, HashSet<string> updatedSources)`:
  - pins the resolution context to that app's own ALC via `ResourceResolver.SetResolutionContext(alc)` so `RefreshMergedDictionary` resolves each source against the correct ALC-scoped registry, not the global one;
  - walks the app's merged-dictionary graph and refreshes only source-backed dictionaries whose normalized source is in `updatedSources`;
  - calls `app.UpdateResourceBindingsForHotReload()` only when at least one dictionary was refreshed.
- `UpdateResourceDictionaries` now **returns the count** of refreshed dictionaries (so the binding re-eval is gated).
- The call site iterates `Application.Current` then `Application.EnumerateSecondaryApplications()`.

These APIs are `internal` in Uno.UI and visible to Uno.UI.RemoteControl via `InternalsVisibleTo`.

Theme libraries that register an `[assembly: MetadataUpdateHandler]` (e.g. a base theme's `UpdateApplication` that re-runs the theme's own `UpdateSource`) are discovered and invoked in the secondary ALC by `HotReloadAgent.GetMetadataUpdateHandlerActions()`, which scans both the current ALC and the default ALC, deduping by assembly name.

## Scope note

This fix correctly propagates hot-reload edits of *source-backed* dictionaries (including ones a theme library does not manage, e.g. a merged "theme resources" file) and re-evaluates the inner app's bindings. It is a self-contained RemoteControl change and stands on its own merits. Any remaining theme-library display behavior (e.g. color-only overrides not reaching `StaticResource`-bound brushes) is a separate concern tracked downstream and is independent of this fix.

## Files changed

- `src/Uno.UI.RemoteControl/HotReload/ClientHotReloadProcessor.MetadataUpdate.cs` — the secondary-ALC refresh fix (`RefreshResourcesForApp`, `UpdateResourceDictionaries` returns count, call-site loop over the host and every secondary app).
- `src/Uno.UI.RemoteControl/HotReload/MetadataUpdater/HotReloadAgent.cs` — no behavior change (diagnostic logging only, since removed).
- `src/Uno.UI.RuntimeTests/Tests/AssemblyLoadContext/Given_AlcContentHost.cs` — runtime tests for the secondary-ALC refresh (see below).

## Build

Build the **Reference** variant (this is what the host and inner ALC load from `lib/net10.0`), not Skia:

```
cd src && dotnet build Uno.UI.RemoteControl/Uno.UI.RemoteControl.Reference.csproj -c Debug
```

## Follow-up: secondary-app binding refresh must use the owning app's theme

The per-secondary-app `UpdateResourceBindingsForHotReload()` added here surfaced a latent defect in `Application.OnResourcesChanged` (`src/Uno.UI/UI/Xaml/Application.cs`). It is an instance method that walks the **process-global** content-root list (`WinUICoreServices.Instance.ContentRootCoordinator.ContentRoots`) and applied `this.InternalRequestedTheme` to *every* root via `NotifyThemeChanged`. When a host (default ALC) renders a consumer app in a secondary ALC and the two have different themes, the consumer app's refresh re-themed the host's content root (and the host's refresh would re-theme the consumer's). After a hot reload the host's visual tree adopted the consumer app's theme.

**Fix:** derive each content root's theme from the application that **owns** it rather than from `this`. `Application.GetOwningApplication(ContentRoot)` (in `Application.Alc.cs`, guarded by `#if UNO_HAS_ENHANCED_LIFECYCLE` to match its only call site) resolves the owner from `XamlRoot.Content`'s `AssemblyLoadContext` via `GetForAssemblyLoadContext` (default-ALC content → `Current`; secondary-ALC content → its registered app). The lookup is gated on `HasSecondaryApps`, so the single-app (and single-app multi-window) path is byte-for-byte unchanged. The global calls (`DefaultBrushes.ResetDefaultThemeBrushes`, `ResourceResolver.UpdateSystemThemeBindings`) stay global and `PropagateResourcesChanged` is unchanged, so a consumer app's content nested in the host's tree still re-binds.

**Regression test:** `Given_AlcContentHost.When_SecondaryAlcApp_HotReloadsResources_Then_HostThemeUnchanged` — host = Dark, secondary ALC app = Light; after the secondary app's `UpdateResourceBindingsForHotReload()`, a non-themed host probe's `ActualTheme` must stay Dark. RED before the fix, GREEN after.

### Files changed (follow-up)

- `src/Uno.UI/UI/Xaml/Application.cs` — `OnResourcesChanged` derives the per-root theme from the owning app.
- `src/Uno.UI/UI/Xaml/Application.Alc.cs` — new `GetOwningApplication(ContentRoot)` helper.
- `src/Uno.UI.RuntimeTests/Tests/AssemblyLoadContext/Given_AlcContentHost.cs` — regression test.
