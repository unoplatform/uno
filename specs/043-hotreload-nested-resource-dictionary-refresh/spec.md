# Feature Specification: Hot-Reload Refresh of Nested Source-Backed Resource Dictionaries

**Repo**: `uno-private` (Uno.UI.RemoteControl)
**Created**: 2026-05-29
**Status**: Implemented
**Input**: Editing a source-backed `ResourceDictionary` XAML file does not propagate to the running visual tree on hot reload when the live merged entry for that file is **nested** (not a direct entry of `Application.Current.Resources.MergedDictionaries`) — for example when a typed `ResourceDictionary` subclass adds the source-backed dictionary to its own `MergedDictionaries`. The pre-fix logic only walked the top level of the app's merged dictionaries and matched on exact `Source` URI equality.

---

## Problem Statement

### Symptom

Editing a `ResourceDictionary` XAML file that is referenced by source (`ms-appx:///Foo.xaml`) does not visibly update the running application on hot reload. The connection is healthy, the metadata update is received and the file's generated dictionary type is rebuilt, but the resources resolved from that file (colors, brushes, values consumed via `{ThemeResource ...}` / `{StaticResource ...}`) keep their previous values until a full app restart. No error is logged.

### Why (pre-fix code)

`ClientHotReloadProcessor.UpdateGlobalResources` runs when a `*GlobalStaticResources` type is rebuilt. Its resource-dictionary-refresh block did two things:

1. Scanned `updatedTypes` for any implementing `IXamlResourceDictionaryProvider`, read each provider's `Instance.GetResourceDictionary().Source`, and collected those URIs into a `List<Uri> updatedDictionaries`.
2. Walked **only the top level** of `Application.Current.Resources.MergedDictionaries` and called `RefreshMergedDictionary` on entries whose `Source` was **reference-equal** to one of the collected URIs.

Two independent defects prevented the user-facing dictionary from ever being refreshed:

**Defect 1 — the walk was not recursive.** A source-backed dictionary is frequently not a direct child of `Application.Current.Resources.MergedDictionaries`. It is common for a typed `ResourceDictionary` subclass to construct a plain `ResourceDictionary { Source = ... }` and append it to its own `MergedDictionaries`. The live entry for the edited file therefore sits one (or more) levels deep in the merged-dictionary graph. The top-level-only walk never reached it.

**Defect 2 — the source URIs did not match.** The rebuilt dictionary's `IXamlResourceDictionaryProvider` advertises its source in **local-resource form** — `ms-resource:///Files/Foo.xaml` (this is `XamlFilePathHelper.LocalResourcePrefix + path`). The live merged entry created from `ms-appx:///Foo.xaml` carries the **app-package form** of the *same file*. The two `Uri` values are not equal, so the `Where(merged => updatedDictionaries.Any(d => d == merged.Source))` predicate would not have matched even if the entry had been at the top level.

Both were confirmed at runtime. For a single edited file the collected set was, e.g.:

```
updatedDictionaries:
  ms-resource:///Files/Foo.xaml
```

while the live, source-backed entry in the graph was:

```
- [<a ResourceDictionary subclass>] Source=ms-appx:///Themes/...mergedpages.xaml
    ...
  - [ResourceDictionary] Source=ms-appx:///Foo.xaml      <-- the entry that needed refreshing
```

### Approaches considered and rejected

1. **Refresh every source-backed dictionary in the graph unconditionally (drop URI targeting).** This "fixes" Defect 2 by never comparing URIs, but it over-refreshes: it reloads dictionaries the update did not touch, and it requires re-introducing a guard to avoid replacing typed subclasses (whose own constructor-managed state would be lost by a plain reload from `Source`). It also forces swallowing per-entry exceptions, because the framework merges compile-time-embedded `ms-resource:///` entries that the runtime resolver cannot re-fetch. Rejected — the over-refresh, the type-identity guard, and the exception-swallowing are all symptoms of having discarded the information about *which* dictionary actually changed.
2. **Match on exact `Source` equality but recurse.** Fixes Defect 1 only. Still never matches because of Defect 2 (scheme/prefix difference between the advertised and the live source).
3. **Recurse + match on a normalized, scheme-independent source key.** Adopted — fixes both defects while preserving targeting. See below.

---

## Approach taken

Two coordinated changes in `src/Uno.UI.RemoteControl/HotReload/ClientHotReloadProcessor.MetadataUpdate.cs`.

### 1. Collect updated sources as normalized keys; gate the phase on having any

The provider's advertised `Source` and the live entry's `Source` are reduced to the same scheme-independent key before comparison. The recursive refresh (and the subsequent `UpdateResourceBindingsForHotReload`) only runs when at least one resource dictionary was actually rebuilt by the update.

```csharp
var updatedSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

foreach (var updatedType in updatedTypes)
{
    if (updatedType.GetInterfaces().Contains(typeof(IXamlResourceDictionaryProvider)))
    {
        var staticDictionaryProperty = updatedType
            .GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);

        if (staticDictionaryProperty?.GetMethod is { } getMethod
            && getMethod.Invoke(null, null) is IXamlResourceDictionaryProvider provider
            && provider.GetResourceDictionary() is { Source: not null } dictionary)
        {
            updatedSources.Add(NormalizeResourceDictionarySource(dictionary.Source));
        }
    }
}

if (updatedSources.Count > 0)
{
    UpdateResourceDictionaries(updatedSources, Application.Current.Resources);
    Application.Current.UpdateResourceBindingsForHotReload();
}
```

### 2. Recursive, normalized-targeted refresh

`UpdateResourceDictionaries` now recurses through the entire merged-dictionary graph. It refreshes only entries whose normalized source is in the updated set, but traverses **every** entry (including typed subclasses) so nested matches are reached.

```csharp
private static void UpdateResourceDictionaries(HashSet<string> updatedSources, ResourceDictionary root)
{
    // Snapshot first: RefreshMergedDictionary mutates MergedDictionaries (it replaces the entry at its index).
    foreach (var merged in root.MergedDictionaries.ToArray())
    {
        if (merged.Source is { } source
            && updatedSources.Contains(NormalizeResourceDictionarySource(source)))
        {
            root.RefreshMergedDictionary(merged);
        }
    }

    foreach (var merged in root.MergedDictionaries)
    {
        UpdateResourceDictionaries(updatedSources, merged);
    }
}

private static string NormalizeResourceDictionarySource(Uri source)
{
    var value = source.OriginalString;

    if (value.StartsWith(Uno.UI.Xaml.XamlFilePathHelper.LocalResourcePrefix, StringComparison.OrdinalIgnoreCase))
    {
        return value.Substring(Uno.UI.Xaml.XamlFilePathHelper.LocalResourcePrefix.Length);
    }

    if (value.StartsWith(Uno.UI.Xaml.XamlFilePathHelper.AppXIdentifier, StringComparison.OrdinalIgnoreCase))
    {
        return value.Substring(Uno.UI.Xaml.XamlFilePathHelper.AppXIdentifier.Length);
    }

    return value;
}
```

`NormalizeResourceDictionarySource` strips either `ms-resource:///Files/` (`XamlFilePathHelper.LocalResourcePrefix`) or `ms-appx:///` (`XamlFilePathHelper.AppXIdentifier`), leaving the file path. Both URI forms for the same file therefore reduce to the same key (`Foo.xaml`), and the comparison is path-aware (a different relative path produces a different key).

---

## Why this works

- **Defect 1 (nesting) is resolved by the recursion.** The second `foreach` descends into every entry, so a source-backed dictionary nested inside another dictionary — including inside a typed `ResourceDictionary` subclass — is reached and, if its source matches, refreshed via its parent's `RefreshMergedDictionary`.

- **Defect 2 (URI form) is resolved by normalization.** The advertised `ms-resource:///Files/Foo.xaml` and the live `ms-appx:///Foo.xaml` reduce to the same key, so the entry that actually changed is identified.

- **Refreshing the live entry by its own `Source` is sound.** `RefreshMergedDictionary` re-resolves the entry from its `Source` via the `ResourceResolver` registry. The same metadata update re-invokes the generated `RegisterResourceDictionariesBySource(Local)` initializers (already done earlier in `UpdateGlobalResources`), so the registry holds the rebuilt content before the refresh runs; reloading the live entry from its source yields the edited values. The entry resolved successfully from that source at construction time, so the registry can resolve it again.

- **Targeting is preserved, which removes the need for the rejected approach's extra machinery:**
  - *No over-refresh.* Only dictionaries whose source was rebuilt are reloaded.
  - *No type-identity guard.* A typed `ResourceDictionary` subclass is never replaced, because its own `Source` is not in the updated set unless that subclass's source file was itself edited. It is still **traversed** so its nested children are reached. Its constructor-managed state is therefore preserved without a special case.
  - *No exception swallowing.* `RefreshMergedDictionary` is only called on entries known to have been rebuilt (and therefore resolvable). Compile-time-embedded `ms-resource:///` entries that the runtime resolver cannot re-fetch are never in the updated set, so they are never passed to `RefreshMergedDictionary` and cannot throw `Cannot locate resource from '...'`.

---

## File-Level Change List

- `src/Uno.UI.RemoteControl/HotReload/ClientHotReloadProcessor.MetadataUpdate.cs`
  - `UpdateGlobalResources` (`#if !(WINUI || WINAPPSDK || WINDOWS_UWP)` block): replaced the `List<Uri> updatedDictionaries` collection with a normalized `HashSet<string> updatedSources`, and gated the recursive refresh + `UpdateResourceBindingsForHotReload` on `updatedSources.Count > 0`.
  - `UpdateResourceDictionaries(List<Uri>, ResourceDictionary)` → `UpdateResourceDictionaries(HashSet<string>, ResourceDictionary)`: now recursive, refreshes only normalized-source matches, traverses all entries (snapshotting `MergedDictionaries` before refreshing since `RefreshMergedDictionary` mutates it in place).
  - Added `NormalizeResourceDictionarySource(Uri)` using `XamlFilePathHelper.LocalResourcePrefix` / `XamlFilePathHelper.AppXIdentifier`.

No other files changed. No public API surface changed.

---

## Out of scope (does not propagate on hot reload)

- Compile-time-embedded resources referenced by `ms-resource:///` URIs (framework/library-embedded XAML baked into assemblies). Refreshing these would require rebuilding and reloading the owning assembly.
- Inputs that are only applied through a dictionary subclass's own change-callbacks (e.g. dependency-property-driven regeneration). A hot-reload edit that changes a value in a source-backed file refreshes that file's key/value content; it does not re-fire unrelated DP change-callbacks on a containing subclass.

---

## Verification

### Compile

```
dotnet build src/Uno.UI.RemoteControl/Uno.UI.RemoteControl.Skia.csproj --no-dependencies -c Debug
```

Build succeeded, 0 warnings, 0 errors. The build overrides the local runtime package cache so the patched client DLL is consumed by the app under test.

### Runtime

1. Reproduced the failure on the pre-fix build: with hot reload connected, editing a source-backed dictionary nested inside a typed `ResourceDictionary` subclass produced no visible change (objective before/after dominant-color comparison: unchanged).
2. On the patched build, the same edit propagated to the running visual tree without restart. Verified on the first edit after launch and across multiple consecutive edits into the same running instance (objective comparison: changed each time).

---

## Related Specs

- [041-hotreload-ui-pause](../041-hotreload-ui-pause/spec.md) — coordinates *when* hot-reload UI updates apply; this spec coordinates *what* gets reloaded inside the resource-dictionary phase.
- [039-alc-aware-hotreload-handlers](../039-alc-aware-hotreload-handlers/spec.md) — `RefreshMergedDictionary` resolves through the ALC-aware `ResourceResolver` registry this refresh relies on.
