# Hot Reload — Theme Dictionary Refresh: Briefing for Next Session

## Context

We're fixing a bug in Uno Platform's hot reload pipeline for `ResourceDictionary` instances that have **runtime-populated** `MergedDictionaries` (most prominently `Uno.Themes.BaseTheme` and its subclasses like `MaterialTheme`, `CupertinoTheme`, `FluentTheme`, `SimpleTheme`).

Repos involved:
- `X:\src\uno` — public uno repo
- `X:\src\uno-private` — private mirror; same files, same changes need to land in both
- `X:\src\uno.themes` — Uno.Themes package, contains `BaseTheme`. **Read-only for the framework fix**, but the new interface needs to be implemented on `BaseTheme`.

---

## The Problem

`BaseTheme` (`X:\src\uno.themes\src\library\Uno.Themes\BaseTheme.cs:332-348`) is a `ResourceDictionary` subclass whose constructor calls `UpdateSource()`, which **programmatically** populates its own `MergedDictionaries`:

```csharp
// BaseTheme.cs:428-435 (inside UpdateSource)
MergedDictionaries.Add(typography);      // has Source
MergedDictionaries.Add(spacing);         // NO Source — generated in code
MergedDictionaries.Add(shape);           // NO Source — generated in code
MergedDictionaries.Add(density);         // NO Source — generated in code
MergedDictionaries.Add(colors);          // has Source, with nested mergeds
MergedDictionaries.Add(converters);      // has Source

AddThemeSpecificDictionaries(MergedDictionaries);
```

It also holds **per-instance runtime state** that the user can configure: `Colors`, `DefaultCornerRadius`, `DefaultDensity`, `FontOverrideDictionary`, `_baseColorOverride`, etc. Mutating any of these calls `UpdateSource()` which rebuilds the merged dictionary list.

`BaseTheme.UpdateSource(bool fromHotReload)` (`BaseTheme.cs:374-436`) already has a documented `fromHotReload: true` code path that clears, cycles `Source`, and rebuilds — designed specifically for hot reload to call.

### What hot reload does today

`ClientHotReloadProcessor.MetadataUpdate.cs` walks `Application.Current.Resources` recursively. For each merged dictionary it calls `ResourceDictionary.RefreshMergedDictionary(merged)` (`X:\src\uno\src\Uno.UI\UI\Xaml\ResourceDictionary.cs:569-596`), which:

1. Fetches a fresh instance via `ResourceResolver.RetrieveDictionaryForSource(merged.Source)`.
2. Replaces the entry in `_mergedDictionaries` with the new instance.
3. (Current debug-state addition the previous session was unsure about) Copies `merged.MergedDictionaries` entries that aren't already in `newMerged.MergedDictionaries` (reference-equality check).

### Why this is broken

`ResourceResolver.RetrieveDictionaryForSource` (`ResourceResolver.cs:711-730`) invokes a factory from `_registeredDictionariesByUri`. For a `BaseTheme` subclass, the factory does `new MaterialTheme()` etc. — so **the constructor runs**, which calls `UpdateSource()`, which populates the new instance's `MergedDictionaries` from defaults.

Failure modes that result:

1. **Duplicates.** `newMerged.MergedDictionaries.Contains(extraMerged)` uses reference equality. The constructor-added `typography` on `newMerged` is a *different* instance than the old `typography` — so the `Contains` check returns false and we append the old one too. After one hot reload, `MergedDictionaries` has *two* of each. Grows on every subsequent reload.

2. **Lost runtime state.** All user customization on the old `BaseTheme` (`Colors.PrimarySeed`, `DefaultCornerRadius`, `FontOverrideDictionary`, `_baseColorOverride`, etc.) is gone — the new instance ran its constructor with defaults.

3. **Null-Source children crash.** The programmatic `spacing`, `shape`, `density` dictionaries have no `Source`. When the recursion walks into them and calls `RefreshMergedDictionary`, it throws `InvalidOperationException("Unable to refresh dictionary without a Source being set")` (line 574). The current code catches this in a `try/catch` and logs to Console, but it's papering over the design problem.

---

## Current State of the Code (as of this writeup)

These files have in-progress debug instrumentation (Console.WriteLine `[STEVE]` markers) that **must be cleaned up** as part of the final change.

### `X:\src\uno\src\Uno.UI.RemoteControl\HotReload\ClientHotReloadProcessor.MetadataUpdate.cs`

Caller block (lines 381-415) — note: the dead `updatedDictionaries` URI loop is still there. It's no longer used, but kept so we can re-gate on "any updated type implements `IXamlResourceDictionaryProvider`". It needs to be collapsed to a simple `Any` check.

The recursion (lines 420-466) currently:
- Snapshots `MergedDictionaries` into an array
- Calls `RefreshMergedDictionary` on each, wrapped in `try/catch` to swallow the null-Source throw
- Re-snapshots and recurses
- Uses a `HashSet<ResourceDictionary>` visited set to prevent cycles
- Contains `[STEVE]` Console.WriteLine debug lines

### `X:\src\uno\src\Uno.UI\UI\Xaml\ResourceDictionary.cs`

`RefreshMergedDictionary` (lines 569-596) currently:
- Throws on null `Source`
- Replaces the entry via the resolver
- Copies `merged.MergedDictionaries` entries to `newMerged.MergedDictionaries` if not already contained (reference equality — **this is the duplicate bug**)
- Contains `[STEVE]` Console.WriteLine debug lines

### `X:\src\uno-private\src\Uno.UI.RemoteControl\HotReload\ClientHotReloadProcessor.MetadataUpdate.cs`

Earlier version of the same changes — no `[STEVE]` debug, no try/catch, no visited set, no copy logic. Needs to be brought into parity with whatever final design we land on.

---

## Recommended Fix — Option A: Opt-in In-Place Refresh

Introduce a contract that lets a `ResourceDictionary` subclass refresh **itself** rather than being replaced by a fresh resolver instance.

### Design

```
1. ClientHotReloadProcessor walks the resource tree.
2. For each merged dictionary it encounters:
   - If it implements IHotReloadRefreshable → call RefreshForHotReload() on it.
     The instance refreshes in place. Reference identity preserved.
     The dictionary owns the rebuild; runtime state is preserved.
   - Else → call RefreshMergedDictionary, which uses the resolver to swap
     in a fresh instance (existing behavior, for plain Source-loaded dictionaries).
3. Recurse into children either way.
```

This means:
- `BaseTheme` implements `IHotReloadRefreshable` → `RefreshForHotReload()` just calls `UpdateSource(fromHotReload: true)`. All runtime state stays. All programmatic mergeds get correctly rebuilt by the existing code path.
- Plain `ResourceDictionary` instances loaded from XAML (e.g. `MyStyles.xaml` merged into App.Resources) get the existing resolver-replacement behavior.
- Null-Source intermediate wrappers are walked through but not refresh-attempted.

### Code Changes

#### 1. New interface — `X:\src\uno\src\Uno.UI\UI\Xaml\IHotReloadRefreshable.cs` (new file)

```csharp
#nullable enable

namespace Microsoft.UI.Xaml;

/// <summary>
/// Optional contract for a <see cref="ResourceDictionary"/> that knows how to refresh
/// itself in place during hot reload. The framework calls <see cref="RefreshForHotReload"/>
/// instead of swapping the instance with a fresh resolver-built copy, so runtime state
/// (configuration properties, programmatically added merged dictionaries, etc.) is preserved.
/// </summary>
internal interface IHotReloadRefreshable
{
    /// <summary>
    /// Refresh this dictionary's contents against the latest source-of-truth.
    /// Implementations should rebuild merged dictionaries / theme dictionaries / direct entries
    /// while preserving the receiver's reference identity and any configuration state set on it.
    /// </summary>
    void RefreshForHotReload();
}
```

Notes:
- Marked `internal` to start. If `Uno.Themes` cannot see it as internal, either `[InternalsVisibleTo("Uno.Themes")]` already exists (check `Uno.UI.csproj`) or promote to `public`. Verify before deciding.
- Place in `Microsoft.UI.Xaml` namespace so it sits alongside `ResourceDictionary`.
- Alternative location considered: `Uno.UI.Xaml` (Uno-internal namespace). Pick whichever matches existing conventions in the file.

#### 2. Update `RefreshMergedDictionary` — `X:\src\uno\src\Uno.UI\UI\Xaml\ResourceDictionary.cs:569-596`

Replace the entire current method with:

```csharp
/// <summary>
/// Refreshes the provided dictionary with the latest version of the dictionary (during hot reload).
/// If <paramref name="merged"/> implements <see cref="IHotReloadRefreshable"/>, refresh is delegated
/// to the instance and the reference is preserved. Otherwise the entry is replaced with a fresh
/// instance built from its <see cref="ResourceDictionary.Source"/>.
/// </summary>
/// <param name="merged">A dictionary present in the merged dictionaries</param>
internal void RefreshMergedDictionary(ResourceDictionary merged)
{
    if (merged is IHotReloadRefreshable refreshable)
    {
        refreshable.RefreshForHotReload();
        return;
    }

    if (merged.Source is null)
    {
        // Inline / programmatic dictionaries with no Source cannot be resolver-refreshed.
        // The caller is expected to recurse into them so nested Source-bearing entries still get refreshed.
        return;
    }

    var index = _mergedDictionaries.IndexOf(merged);
    if (index == -1)
    {
        throw new InvalidOperationException("The provided dictionary cannot be found in the merged list");
    }

    _mergedDictionaries[index] = ResourceResolver.RetrieveDictionaryForSource(merged.Source);
}
```

Key differences from current state:
- IHotReloadRefreshable branch added at the top.
- Null-Source case becomes a graceful no-op instead of a throw.
- **Removed** the lines-584-590 copy logic (this was the duplicate-introducing workaround).
- All Console.WriteLine `[STEVE]` lines removed.

#### 3. Update the recursion — `X:\src\uno\src\Uno.UI.RemoteControl\HotReload\ClientHotReloadProcessor.MetadataUpdate.cs`

Replace the caller block (lines 381-415) with:

```csharp
#if !(WINUI || WINAPPSDK || WINDOWS_UWP)
            // If any updated type is an IXamlResourceDictionaryProvider, refresh every merged
            // dictionary in the tree against the latest version registered with the ResourceResolver
            // (or, for IHotReloadRefreshable implementors, by delegating to the instance).
            var hasUpdatedResourceDictionary = updatedTypes
                .Any(t => t.GetInterfaces().Contains(typeof(IXamlResourceDictionaryProvider)));

            if (hasUpdatedResourceDictionary)
            {
                UpdateResourceDictionaries(Application.Current.Resources);

                // Force the app to re-evaluate global resource bindings
                Application.Current.UpdateResourceBindingsForHotReload();
            }
#endif
```

Replace the recursion (lines 420-466) with:

```csharp
#if !(WINUI || WINAPPSDK || WINDOWS_UWP)
    /// <summary>
    /// Recursively refreshes every merged ResourceDictionary in the tree.
    /// Dictionaries that implement <see cref="IHotReloadRefreshable"/> refresh themselves in place;
    /// others get replaced via <see cref="ResourceResolver"/>; null-Source intermediates are walked through.
    /// </summary>
    private static void UpdateResourceDictionaries(ResourceDictionary root)
    {
        var visited = new HashSet<ResourceDictionary>(ReferenceEqualityComparer.Instance);
        UpdateResourceDictionariesCore(root, visited);
    }

    private static void UpdateResourceDictionariesCore(ResourceDictionary root, HashSet<ResourceDictionary> visited)
    {
        if (!visited.Add(root))
        {
            return;
        }

        // Snapshot before refresh: RefreshMergedDictionary may replace entries in MergedDictionaries.
        foreach (var merged in root.MergedDictionaries.ToArray())
        {
            root.RefreshMergedDictionary(merged);
        }

        // Re-snapshot after refresh — entries may have been swapped out by the resolver path.
        foreach (var merged in root.MergedDictionaries.ToArray())
        {
            UpdateResourceDictionariesCore(merged, visited);
        }
    }
#endif
```

Key differences from current state:
- Dead `updatedDictionaries` URI-collection loop deleted entirely.
- `try/catch` around `RefreshMergedDictionary` removed — null-Source is now a graceful no-op, so there's no exception to swallow.
- Visited set switched to `ReferenceEqualityComparer.Instance` (defensive — two different dictionaries should not compare equal just because content hashes match).
- All Console.WriteLine `[STEVE]` lines removed.

#### 4. Implement on `BaseTheme` — `X:\src\uno.themes\src\library\Uno.Themes\BaseTheme.cs`

Class declaration:

```csharp
public abstract partial class BaseTheme : ResourceDictionary, IHotReloadRefreshable
```

Add at the bottom of the class:

```csharp
/// <inheritdoc />
void IHotReloadRefreshable.RefreshForHotReload() => UpdateSource(fromHotReload: true);
```

Verify `UpdateSource(fromHotReload: true)` does the right thing — looking at `BaseTheme.cs:374-389` it currently:
- Clears `ThemeDictionaries` and `MergedDictionaries`
- Sets `Source = null; Source = new Uri(DefaultStylesSource);` to force the resolver to re-load default styles
- Then runs the rebuild

This should be sufficient. **Remove the `[STEVE-THEMES]` Console.WriteLines on lines 376 and 386** while you're in there.

The `Console.WriteLine($"[STEVE] Constructing theme instance ...")` on line 334 should also go.

#### 5. Mirror everything in `uno-private`

Same changes to:
- `X:\src\uno-private\src\Uno.UI\UI\Xaml\ResourceDictionary.cs` (the same `RefreshMergedDictionary` rewrite)
- `X:\src\uno-private\src\Uno.UI.RemoteControl\HotReload\ClientHotReloadProcessor.MetadataUpdate.cs` (same caller + recursion rewrite)
- New file `X:\src\uno-private\src\Uno.UI\UI\Xaml\IHotReloadRefreshable.cs`

Verify the line numbers — `uno-private` had the same blocks at `~712-746` and `~750-768` in the last reading, but the file has diverged from `uno`. Use the same logical edits, not the same line numbers.

---

## Things to Verify Before Coding

1. **InternalsVisibleTo.** Does `Uno.UI` already expose internals to `Uno.Themes`? If not, `IHotReloadRefreshable` either needs to be `public` or the InternalsVisibleTo needs adding. Grep for `InternalsVisibleTo` in `Uno.UI.csproj` and `Directory.Build.props` to check.

2. **Where does `Uno.Themes` get its `Uno.UI` reference from?** Likely a `<PackageReference>` to a published `Uno.WinUI` (or `Uno.UI`) NuGet. For local cross-repo testing you may need to set up `<UnoNugetOverrideVersion>` or local NuGet feed — see `AGENTS.md` build properties section. The `Uno.Themes` repo's `Directory.Build.props` likely has its own override knob.

3. **`ReferenceEqualityComparer.Instance`** — available in .NET 5+. Uno targets `net10.0` and friends so this is fine.

4. **Build target file isolation.** Confirm the namespace `Microsoft.UI.Xaml` matches the rest of `ResourceDictionary.cs` for that target — check the existing namespace declaration in the file. Older WinUI3 vs UWP conditionals might branch on `Windows.UI.Xaml` vs `Microsoft.UI.Xaml`. The new interface file may need a `#if WINUI / Microsoft / Windows` namespace switch like the rest of the framework. Look at any existing tiny single-purpose file in `src/Uno.UI/UI/Xaml/` for the pattern.

5. **`UpdateResourceBindingsForHotReload` gating.** I moved this inside the `hasUpdatedResourceDictionary` branch. The previous session was OK with that, but verify it doesn't break other hot-reload scenarios that don't touch resource dictionaries. If unsure, leave it unconditional and gate only the `UpdateResourceDictionaries(...)` call.

---

## Sample App Test Plan

Create a minimal repro app to validate the fix end-to-end. Place under `src/SamplesApp/SamplesApp.Samples/Hot Reload/` or similar.

### Repro app structure

```
App.xaml:
  <Application.Resources>
    <ResourceDictionary>
      <ResourceDictionary.MergedDictionaries>
        <material:MaterialTheme>
          <material:MaterialTheme.Colors>
            <themes:ThemeColors PrimarySeed="#FF6750A4" />
          </material:MaterialTheme.Colors>
        </material:MaterialTheme>
      </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
  </Application.Resources>

MainPage.xaml:
  - A Button with Style="{StaticResource FilledButtonStyle}" (material style)
  - A TextBlock displaying Background="{ThemeResource PrimaryBrush}"
  - A second TextBlock showing the live size of Application.Current.Resources.MergedDictionaries[0].MergedDictionaries
    (use a small ViewModel that pulls the count on a timer or button-click)
```

### Test scenarios

| # | Action | Expected (with fix) | Expected (without fix, current bug) |
|---|--------|---------------------|-------------------------------------|
| 1 | Build + run | Button styled, primary color matches seed, count display = 6 (typography/spacing/shape/density/colors/converters) | Same |
| 2 | Edit a color value in a `MaterialColors.xaml` resource (in Uno.Themes if buildable locally, otherwise in a user-override `ResourceDictionary` referenced via `Colors.OverrideSource`) and save | Hot reload fires. Color updates on screen. Count stays at 6. Theme instance reference unchanged. | Count climbs to 12, then 18, etc. (duplicates). Custom seed lost — reverts to default M3 purple. |
| 3 | Edit a Style in a control template (e.g. `FilledButtonStyle.xaml` or whatever the material lib uses) and save | Button restyles. Theme runtime state intact. | Theme state lost. Possibly null-ref or stale-binding crash depending on timing. |
| 4 | Edit `MainPage.xaml` (no theme involvement) and save | Page reloads. Theme untouched. | Same — no regression expected here. |
| 5 | Repeat scenario 2 ten times | Count remains at 6. Memory stable. | Count grows linearly; memory grows. |

### How to actually run

Per `AGENTS.md` / `CLAUDE.md`:

```bash
cd src
# crosstargeting_override.props with net10.0 + UnoFastDevBuild
dotnet build src/SamplesApp/SamplesApp.Skia.Generic/SamplesApp.Skia.Generic.csproj
dotnet run --project src/SamplesApp/SamplesApp.Skia.Generic
```

Then with the app running, edit a file under `src/SamplesApp/` (or the Uno.Themes XAML if locally linked) and watch the DevServer push the update.

For automation, the `/runtime-tests` skill can run a runtime test but **hot reload itself is interactive** — you'll need a human-driven session for scenarios 2/3 unless you script a file-edit + WaitForIdle loop, which is fragile.

A unit-ish test that exercises the logic without the DevServer:

```csharp
// Uno.UI.RuntimeTests/Tests/HotReload/Given_BaseTheme_HotReload.cs
[TestMethod]
public async Task When_RefreshForHotReload_Then_Reference_Identity_Preserved()
{
    var theme = new MaterialTheme();
    theme.Colors = new ThemeColors { PrimarySeed = Colors.Red };
    var originalCount = theme.MergedDictionaries.Count;

    ((IHotReloadRefreshable)theme).RefreshForHotReload();

    Assert.AreEqual(originalCount, theme.MergedDictionaries.Count, "MergedDictionaries should not duplicate");
    Assert.AreEqual(Colors.Red, theme.Colors.PrimarySeed, "PrimarySeed runtime state must be preserved");
}

[TestMethod]
public async Task When_UpdateResourceDictionaries_Then_BaseTheme_Refreshes_In_Place()
{
    var theme = new MaterialTheme();
    Application.Current.Resources.MergedDictionaries.Add(theme);
    var beforeRef = Application.Current.Resources.MergedDictionaries[0];

    // Invoke the recursion via reflection or by exposing it test-internal
    typeof(ClientHotReloadProcessor)
        .GetMethod("UpdateResourceDictionaries", BindingFlags.NonPublic | BindingFlags.Static)!
        .Invoke(null, new object[] { Application.Current.Resources });

    Assert.AreSame(beforeRef, Application.Current.Resources.MergedDictionaries[0]);
}
```

These tests should live in `src/Uno.UI.RuntimeTests/Tests/HotReload/` if a folder exists, else create one. They run via the `/runtime-tests` skill.

---

## Validation Checklist

Before declaring done:

- [ ] `IHotReloadRefreshable` interface compiles in both `uno` and `uno-private`
- [ ] `ResourceDictionary.RefreshMergedDictionary` honors the interface, no-ops on null Source, throws only on "not found in merged list"
- [ ] All `[STEVE]` / `[STEVE-THEMES]` Console.WriteLine debug lines removed from:
  - `ClientHotReloadProcessor.MetadataUpdate.cs` (both repos)
  - `ResourceDictionary.cs` (both repos)
  - `BaseTheme.cs` (lines 334, 376, 386)
- [ ] Dead `updatedDictionaries` URI loop removed from `ClientHotReloadProcessor.MetadataUpdate.cs`
- [ ] `BaseTheme` declares `IHotReloadRefreshable` and the method delegates to `UpdateSource(fromHotReload: true)`
- [ ] `dotnet build src/Uno.UI-Skia-only.slnf` succeeds clean (no analyzer warnings introduced)
- [ ] Runtime tests pass (`/runtime-tests Given_BaseTheme_HotReload`)
- [ ] Sample app scenarios 1-5 above match the "with fix" column
- [ ] Sample app: count display stays stable across 10 hot reload cycles
- [ ] No regression in non-theme hot reload (scenario 4)

---

## Open Questions to Resolve Early

1. **Should `IHotReloadRefreshable` be public?** If users define their own `ResourceDictionary` subclasses with runtime state, they'd want to opt in too. Probably yes — `public` with appropriate XML doc.

2. **Should `UpdateSource(fromHotReload: true)` itself become the interface signature?** I.e. put `RefreshForHotReload()` on `BaseTheme` and have other resource dictionaries just override differently. No — keep the interface narrow; theme is one consumer.

3. **What about `ThemeDictionaries`?** `BaseTheme.UpdateSource` already clears `ThemeDictionaries`, but the framework recursion doesn't walk them. If a user has a `ResourceDictionary` with `ThemeDictionaries` containing further mergeds, those nested mergeds won't be refreshed by today's recursion. Out of scope for this fix but worth noting for follow-up.

4. **`uno-private` parity.** The two repos have diverged. Verify by diff that the only difference in these files is whitespace/line-count from surrounding code, not behavior, before applying edits.

---

## Files Touched (Summary)

| File | Change |
|---|---|
| `X:\src\uno\src\Uno.UI\UI\Xaml\IHotReloadRefreshable.cs` | NEW |
| `X:\src\uno\src\Uno.UI\UI\Xaml\ResourceDictionary.cs` | Rewrite `RefreshMergedDictionary` (~line 569) |
| `X:\src\uno\src\Uno.UI.RemoteControl\HotReload\ClientHotReloadProcessor.MetadataUpdate.cs` | Collapse caller, simplify recursion, drop debug, drop dead URI loop |
| `X:\src\uno.themes\src\library\Uno.Themes\BaseTheme.cs` | Implement `IHotReloadRefreshable`, remove debug lines |
| `X:\src\uno-private\src\Uno.UI\UI\Xaml\IHotReloadRefreshable.cs` | NEW (mirror) |
| `X:\src\uno-private\src\Uno.UI\UI\Xaml\ResourceDictionary.cs` | Mirror |
| `X:\src\uno-private\src\Uno.UI.RemoteControl\HotReload\ClientHotReloadProcessor.MetadataUpdate.cs` | Mirror |
| `X:\src\uno\src\Uno.UI.RuntimeTests\Tests\HotReload\Given_BaseTheme_HotReload.cs` | NEW (tests) |
| Sample app entry under `X:\src\uno\src\SamplesApp\SamplesApp.Samples\Hot Reload\` | NEW (manual repro) |

---

## What NOT to Do

- **Don't keep the copy-extras logic.** It's the source of the duplicate bug. Once `IHotReloadRefreshable` is in place, `BaseTheme` rebuilds itself, so there's nothing to copy.
- **Don't try to detect "this is a `BaseTheme`" inside `Uno.UI`.** Hard reference from framework to a higher-level package. The interface is the decoupling point.
- **Don't leave the try/catch in the recursion.** It was masking the throw from null-Source children. After the fix, null-Source is a no-op so no exception can fly.
- **Don't drop the `IXamlResourceDictionaryProvider` gate.** We still only want to walk the resource tree when an actual resource type changed — otherwise every code-only edit pays a tree-walk cost.
