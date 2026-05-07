# Feature Specification: Contextual-ALC Type Resolution for XAML / DependencyProperty Lookups

**Repo**: `uno-private` (Uno.UI)
**Created**: 2026-05-08
**Status**: Implemented
**Input**: Type-by-name lookups in Uno that fall back to scanning `AppDomain.CurrentDomain.GetAssemblies()` pick up stale assemblies from previous-generation per-app `AssemblyLoadContext`s, breaking XAML type resolution when an `EnterContextualReflection` scope is active on a non-default ALC.

---

## Problem Statement

### Symptom

In Studio Live, opening a preview from a freshly-regenerated nested app intermittently throws `ArrayTypeMismatchException` from inside `Microsoft.UI.Xaml.Markup.XamlReader.Load`, or renders the wrong control entirely. The exception originates inside `Uno.UI.DependencyPropertyDetailsCollection` while binding a property — `IsAssignableFrom(previewType)` against the resolved type returns `false` because the runtime returned a `MainPage` (or similar named type) from a **previous generation** of the nested app, not the current one.

### Why

Studio Live regenerates user apps into per-app `AssemblyLoadContext`s. Each generation creates a new collectible ALC. The previous generation's ALC is `Unload()`ed and its caches scrubbed, but a single managed reference (a static handler, a captured closure, a Type-keyed cache outside the ones `BclAlcCacheCleaner` already scrubs) is enough to keep it alive. While alive, **its assemblies remain visible to `AppDomain.CurrentDomain.GetAssemblies()`** even after `Unload()`.

When `XamlReader.Load` runs inside `AssemblyLoadContext.EnterContextualReflection(previewType.Assembly)` (the contextual scope set by Hot Design's preview opener so type resolution targets the current generation's ALC), it eventually calls `XamlTypeResolver.SourceFindType`. That method's last-resort resolver is:

```csharp
() => AppDomain.CurrentDomain
    .GetAssemblies()
    .Select(a => (name != null ? a.GetType(name) : null) ?? a.GetType(originalName))
    .Trim()
    .FirstOrDefault()
```

The scan iterates every loaded assembly. The contextual reflection scope **does not influence** which assemblies `AppDomain.GetAssemblies` returns — it only affects assembly **resolution** for unresolved references. The first hit wins; if a stale generation's `MainPage` is iterated before the current one's, the resolver returns the stale type, and downstream type-identity comparisons fail.

The same pattern appears in two other call sites that resolve a type by name across all loaded assemblies: `DependencyPropertyDescriptor.SearchTypeInLoadedAssemblies` and `HtmlElementHelper.wasm.GetUnoUIRuntimeWebAssembly`.

### Approaches considered and rejected

1. **Force-GC the stale ALC inside the consumer (Hot Design)** — implemented as a `try-once / DeepCollect / try-once-more` retry around `OpenPreview`. `GC.Collect` cannot reclaim an ALC that is still pinned by a managed reference, so the retry kept failing the same way. Adds latency on the failure path without addressing the root cause.
2. **`EnterContextualReflection` alone** — necessary so the contextual scope is set, but insufficient because the resolver explicitly enumerates `AppDomain.GetAssemblies` and the contextual scope has no effect on that enumeration.
3. **Threading an `Assembly?` parameter through every XAML API down to `XamlReader.Load`** — too invasive for one bug; would change Hot Design's `IAppUpdater` and several internal Uno call signatures.

### Approach taken

A small helper that returns the right enumeration source: when an `EnterContextualReflection` scope is active on a non-default ALC, return `currentAlc.Assemblies` followed by `AssemblyLoadContext.Default.Assemblies`. Otherwise return `AppDomain.GetAssemblies` (existing behaviour, no consumer-visible change). Apply at the three sites that scan loaded assemblies for a name. Stale ALCs are simply not in either ALC's `Assemblies` collection — pinning becomes irrelevant.

---

## Goals

1. Type-by-name lookups inside an `EnterContextualReflection` scope on a non-default ALC consult only that ALC's assemblies and the default ALC's, never sibling or stale per-app ALCs.
2. Callers without an active contextual scope see exactly the existing behaviour (`AppDomain.GetAssemblies`).
3. No new public API surface, no interface changes, no DI plumbing.
4. Single helper, applied uniformly at the three known type-by-name scan sites in `Uno.UI`.

## Non-Goals

- Modifying any non-`Uno.UI` consumer of `AppDomain.GetAssemblies` (`Uno.UI.RemoteControl.Host`, `Uno.UI.XamlHost`) — out of scope for this fix; can be revisited if needed.
- Changing `Application.Alc.DeepScanForAlcReferences` — that diagnostic walks every loaded assembly **by design** to find cross-ALC static-field leaks.
- Eliminating the underlying ALC-pinning issue in Studio Live's binary loader — orthogonal, separate effort.
- Adding a new resolver order or merging this with the four existing resolver lambdas.

---

## Design

### Helper

`Uno.UI.Helpers.ContextualAssemblyResolver.GetRelevantAssemblies()` — single static method.

```csharp
public static IEnumerable<Assembly> GetRelevantAssemblies()
{
    var contextual = AssemblyLoadContext.CurrentContextualReflectionContext;
    if (contextual is not null && contextual != AssemblyLoadContext.Default)
    {
        return contextual.Assemblies.Concat(AssemblyLoadContext.Default.Assemblies);
    }

    return AppDomain.CurrentDomain.GetAssemblies();
}
```

`internal` visibility, in a new file `src/Uno.UI/Helpers/ContextualAssemblyResolver.cs`. No state, no caching — `AssemblyLoadContext.Assemblies` is already enumerable cheaply and `CurrentContextualReflectionContext` is an `AsyncLocal` read.

### Site applications

Three call sites in `Uno.UI` get the swap:

#### 1. `XamlTypeResolver.SourceFindType`

The fourth (last-resort) resolver only:

```diff
-               // Look for the type in all loaded assemblies
-               () => AppDomain.CurrentDomain
-                   .GetAssemblies()
+               // Look for the type in all relevant loaded assemblies. When EnterContextualReflection
+               // is active for a non-default ALC, this enumerates only that ALC's assemblies +
+               // the default ALC's — avoiding stale per-app ALCs that AppDomain.GetAssemblies
+               // would otherwise still expose. Falls back to AppDomain.GetAssemblies otherwise.
+               () => ContextualAssemblyResolver.GetRelevantAssemblies()
                    .Select(
                        [UnconditionalSuppressMessage("Trimming","IL2026", Justification = "...")]
                        (a) =>
                            (name != null ? a.GetType(name) : null) ??
                            a.GetType(originalName)
                    )
                    .Trim()
                    .FirstOrDefault(),
```

The first three resolvers (`Type.GetType(name)`, `Type.GetType(originalName)`, `Type.GetType(unqualified)`) and the default-namespace search loop above the resolver chain are untouched.

#### 2. `DependencyPropertyDescriptor.SearchTypeInLoadedAssemblies`

The fallback `foreach`:

```diff
            if (type == null)
            {
+               // Use the contextual ALC's assemblies when one is set so per-app
+               // types resolve against the right ALC and stale per-app ALCs
+               // (visible to AppDomain.GetAssemblies but unloaded) are skipped.
-               foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
+               foreach (var asm in ContextualAssemblyResolver.GetRelevantAssemblies())
                {
                    type = asm.GetType(qualifiedTypeName);
                    if (type != null)
                    {
                        break;
                    }
                }
            }
```

#### 3. `HtmlElementHelper.wasm.GetUnoUIRuntimeWebAssembly`

```diff
        // .NET Core fails to load assemblies property because of ALC issues: https://github.com/dotnet/runtime/issues/44269
-       return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == UnoUIRuntimeWebAssemblyName)
+       return ContextualAssemblyResolver.GetRelevantAssemblies().FirstOrDefault(a => a.GetName().Name == UnoUIRuntimeWebAssemblyName)
            ?? throw new InvalidOperationException($"Unable to find {UnoUIRuntimeWebAssemblyName} in the loaded assemblies");
```

This call runs at static-cctor time, when no contextual scope is active, so it falls through to `AppDomain.GetAssemblies` — behaviour unchanged. Swap is for consistency.

### Behavioural matrix

| Caller state | Pre-fix | Post-fix | Net change |
|---|---|---|---|
| No `EnterContextualReflection` scope | `AppDomain.GetAssemblies` | `AppDomain.GetAssemblies` | None |
| Scope set on `AssemblyLoadContext.Default` | `AppDomain.GetAssemblies` | `AppDomain.GetAssemblies` (`contextual == Default`) | None |
| Scope set on a non-default ALC | `AppDomain.GetAssemblies` (returns stale ALCs too) | `contextual.Assemblies + Default.Assemblies` (sibling/stale ALCs excluded) | Fix only |

---

## Affected Files

| File | Change |
|------|--------|
| `Uno.UI/Helpers/ContextualAssemblyResolver.cs` (new) | Helper class with `GetRelevantAssemblies()` static method. |
| `Uno.UI/UI/Xaml/Markup/Reader/XamlTypeResolver.cs` | Add `using Uno.UI.Helpers;`; swap last resolver lambda's `AppDomain.CurrentDomain.GetAssemblies()` to `ContextualAssemblyResolver.GetRelevantAssemblies()`. |
| `Uno.UI/UI/Xaml/DependencyPropertyDescriptor.cs` | Add `using Uno.UI.Helpers;`; swap the fallback `foreach` enumeration source. |
| `Uno.UI/UI/Xaml/HtmlElementHelper.wasm.cs` | Add `using Uno.UI.Helpers;`; swap the `FirstOrDefault` enumeration source. |
| `Uno.UI.Tests/Helpers/Given_ContextualAssemblyResolver.cs` (new) | Four MSTest tests covering the four behavioural cases. |

Total diff vs `feature/alc`: +13/-5 in three files plus two new files.

---

## Implementation Checklist

- [x] `ContextualAssemblyResolver.GetRelevantAssemblies()` helper in `Uno.UI/Helpers/`.
- [x] `XamlTypeResolver.SourceFindType` last resolver swapped.
- [x] `DependencyPropertyDescriptor.SearchTypeInLoadedAssemblies` fallback `foreach` swapped.
- [x] `HtmlElementHelper.wasm.GetUnoUIRuntimeWebAssembly` swap (consistency).
- [x] Unit tests in `Uno.UI.Tests` covering all four behavioural cases.
- [x] End-to-end verification in Studio Live: regenerate nested app twice, open preview from latest generation — preview renders correctly without `ArrayTypeMismatchException`.
- [ ] Apply at out-of-scope sites (`Uno.UI.RemoteControl.Host`, `Uno.UI.XamlHost`) — deferred; revisit if a real consumer hits the same pattern there.

---

## Testing

### Test project

`Uno.UI.Tests` — MSTest. Already builds as `AssemblyName: Uno.UI`, so internal helper visible without `[InternalsVisibleTo]`.

### Unit tests

File: `Uno.UI.Tests/Helpers/Given_ContextualAssemblyResolver.cs`

| Test | Description |
|------|-------------|
| `When_NoContextualScope_ReturnsAppDomainAssemblies` | Sanity baseline: no `EnterContextualReflection` scope ⇒ result equivalent to `AppDomain.GetAssemblies`. Regression guard for callers without a scope. |
| `When_ContextualScopeOnDefaultAlc_ReturnsAppDomainAssemblies` | Scope on `AssemblyLoadContext.Default` ⇒ same fallback as no-scope (`contextual == Default`). |
| `When_ContextualScopeOnNonDefaultAlc_ReturnsContextualPlusDefault` | Custom collectible ALC, load an assembly into it, enter scope ⇒ result contains the loaded assembly **and** every default-ALC assembly (so XAML / DependencyProperty resolvers still see system types). |
| `When_ContextualScopeOnNonDefaultAlc_ExcludesSiblingAlcs` | Two custom collectible ALCs each loading a copy of the same DLL; entering one ALC's scope must not surface the sibling's copy. **The core regression guard** for the stale/sibling ALC pollution. |

Each test cleans up its custom ALC via `Unload()` in a `finally` block.

### End-to-end verification

The failing scenario is reproducible in Studio Live's WASM build:

1. Apply Uno + Hot Design overrides via `crosstargeting_override.props` / `tfm-override.props`.
2. Build `Uno.UI.Skia.csproj` with `--no-dependencies` (override active) and `Uno.UI.HotDesign.Client.csproj` with `--no-dependencies`.
3. Clean Studio Live's `bin`/`obj` and rebuild.
4. In a browser at `http://localhost:52512/`: regenerate the nested app twice in a row, then open a preview from the second generation.
5. Pre-fix: throws `ArrayTypeMismatchException` or renders wrong type. Post-fix: preview renders correctly; no exception.

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Helper called from a hot path adds overhead | Allocation / iteration cost | `Concat` is lazy; `Assemblies` is an `IEnumerable<Assembly>` already; no per-call allocation in the no-scope branch. |
| `CurrentContextualReflectionContext` returns `null` in some unexpected edge case | Falls through to `AppDomain.GetAssemblies` | That is the documented fallback; behaviour identical to today for callers in that state. |
| A consumer relies on the old behaviour of seeing all-ALC assemblies under a non-default contextual scope | Different result returned | The previous behaviour returned **stale** assemblies that the consumer had no way to filter; any code relying on that was already broken in multi-ALC scenarios. The fix is the documented intent of `EnterContextualReflection`. |
| New file location (`src/Uno.UI/Helpers/`) conflicts with WASM linker config | None | Helpers folder already exists with sibling files; no linker descriptor change needed. |

---

## Design Decisions

1. **Helper, not threading.** `EnterContextualReflection` is the public CLR API for this use case; it carries the ALC implicitly across every awaited operation. Reading it back at each call site keeps the change local — the alternative (adding an `Assembly?` parameter to every `XamlReader.Load` call site) is intrusive and changes Hot Design's `IAppUpdater` interface.

2. **`internal` not `public`.** No external Uno consumer asks for this — they get it implicitly through the three site applications. Keeps the public API surface unchanged and leaves room to revisit the placement later if other Uno projects need it.

3. **Three sites only.** Other `AppDomain.CurrentDomain.GetAssemblies` callers in `Uno.UI` (`Application.Alc.DeepScanForAlcReferences`) and in sibling projects (`Uno.UI.RemoteControl.Host.ServiceCollectionServiceExtensions`, `Uno.UI.XamlHost.MetadataProviderDiscovery`) are intentionally untouched: the diagnostic's all-assemblies semantics are correct as-is, and the sibling projects aren't in the failing path. If any of them surfaces the same problem later, adopt the helper there too (or duplicate it if the project doesn't reference `Uno.UI`).

4. **No interaction with the existing `Type.GetType` resolvers.** The four-step resolver chain in `SourceFindType` keeps the same order. Only the last (least specific) resolver changed, and only its enumeration source — the resolution logic per assembly is unchanged. This minimises the risk of a behaviour change for callers whose types resolve via earlier steps.

5. **Tests use a custom collectible ALC plus `LoadFromAssemblyPath` on the test assembly's own DLL.** Self-contained — no fixture assemblies to ship, no `[DeploymentItem]`, no platform-specific paths. Validates exactly the property the helper guarantees: that sibling/stale custom ALCs are excluded from the result.
