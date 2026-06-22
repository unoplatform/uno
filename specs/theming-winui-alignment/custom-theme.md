# Custom-theme feature: preserve-minimal vs ditch

> Go/no-go for Uno's app-level **custom theme** feature under the WinUI-aligned theming model. Referenced by [`plan.md`](./plan.md) Phase 6 step 4. **Fill in the Decision box at the bottom before Phase 6 starts.**

## What the feature is today

`Application`/`ApplicationHelper.RequestedCustomTheme = "Foo"` makes the app's active theme key an arbitrary string `"Foo"` instead of `"Light"`/`"Dark"`:

```
src/Uno.UI/UI/Xaml/Application.cs:205-214   UpdateRequestedThemesForResources()
    Current.RequestedThemeForResources =
        (ApplicationHelper.RequestedCustomTheme, Current.RequestedTheme) switch
        {
            (var custom, _) when !custom.IsNullOrEmpty() => custom,   // ← arbitrary string
            (_, ApplicationTheme.Light) => "Light",
            (_, ApplicationTheme.Dark)  => "Dark",
            ...
        };
```

That key becomes `Themes.Active` (`Application.cs:217-225` → `ResourceDictionary.SetActiveTheme`), so every `{ThemeResource}` lookup selects `ThemeDictionaries["Foo"]` **app-wide**. Properties:

- It is an **app-global string axis**. There is **no element-level** custom theme — `FrameworkElement.RequestedTheme` is `ElementTheme` (Default/Light/Dark) only.
- WinUI has **no equivalent**. This is a pure Uno extension.
- Today, a key missing from `ThemeDictionaries["Foo"]` falls back to `Themes.Default` (`ResourceDictionary.cs:590-600`), and Fluent's `"Default"` dictionary **is the Dark theme** — so an incomplete custom theme silently yields dark values.

## Why it doesn't fit the new model cleanly

The whole point of the refactor is: resolution = `f(key, owner.GetTheme())`, and `owner.GetTheme()` returns a **`Theme` enum** (`None`/`Light`/`Dark` + HC bits). A `Theme` enum **cannot carry a custom string**. So a custom name can only ever be an app-global value layered on top of a per-object Light/Dark theme. Custom themes and per-object theming are therefore on **different axes** and can only be partially reconciled.

---

## Option A — PRESERVE-MINIMAL

**Definition (exact, three rules):**

1. `RequestedCustomTheme = "Foo"` continues to select `ThemeDictionaries["Foo"]` for any resolution whose owner is running the **app's ambient theme** — i.e. neither the element nor any ancestor set an explicit `RequestedTheme`.
2. Any element with an explicit `RequestedTheme = Light/Dark` (and its whole subtree) resolves against the **standard** `"Light"`/`"Dark"` dictionaries. **The custom palette does NOT apply inside element-theme islands.**
3. A key missing from `ThemeDictionaries["Foo"]` falls back to the app's **base** Light/Dark (per `RequestedTheme`), never the dark `"Default"` (this is the D7 fallback fix, kept regardless of A/B).

**Exact implementation cost (the "non-trivial Uno-specific code" to weigh):**

- **One bit of per-element state** — "my base theme is an explicit element override, not the inherited app theme" — set during the `NotifyThemeChanged` walk (where `GetRequestedThemeOverride` already decides whether the element overrides). This bit is Uno-specific (WinUI has no reason to track it).
- **A branch at the resolution choke point** (`UpdateThemeReference`/`ResolveOwnerTheme`): if the owner's "override" bit is false **and** a custom theme is active → use the custom key `"Foo"`; else → use the standard key from `owner.GetTheme()`.
- That is the whole cost: ~1 bit + a few lines in the walk + a few lines at the leaf. Small, but it is hot-path Uno-specific logic that every resolution touches, and it **must not break the Phase 3 zero-behavior-difference equivalence** for the non-custom (Light/Dark) case.

**Hard limitation you are accepting with Option A:** custom palettes and element-level theming **cannot fully compose**. An app that both (a) defines a custom palette that *redefines* Light/Dark brushes and (b) uses element-level `RequestedTheme` will see element-themed islands render the **standard** Light/Dark brushes, visually diverging from the custom palette. Full composition would require element-level custom-theme naming — the larger feature that was explicitly rejected. So "preserve" here means *keep it working app-wide where element theming is not used*, **not** *make it compose with element theming*.

---

## Option B — DITCH

**Definition (exact):**

- Remove the `RequestedCustomTheme → ThemeDictionaries["Foo"]` keying. `Themes.Active` becomes strictly `"Light"`/`"Dark"` (+ HC). The `RequestedCustomTheme` API is removed or hard-deprecated to a no-op.
- Apps that want a brand/custom palette use the **standard, WinUI-compatible** approach: merge a resource dictionary that **overrides specific brush/color keys** on top of the Light/Dark theme dictionaries (StaticResource-style overrides), rather than inventing a new theme *name*.
- **Zero** custom-theme-specific code in the per-object resolution path. Fully WinUI-aligned; nothing Uno-specific in the hot path.

**Breaking-change scope (small in practice):**

- Only affects apps that pass a custom name **other than** `"Light"`/`"Dark"`. An app that passes `RequestedCustomTheme = "Light"` is **unaffected**, because `"Light"` is already the standard key and resolves identically with or without the feature. The only custom-theme usage observed in practice passes `nameof(ApplicationTheme.Light)` = `"Light"` and thus falls in this **harmless bucket**.
- Apps using a genuine custom name (e.g. `"Sepia"`, `"Brand"`) must migrate to merged override dictionaries. This is a documented breaking change with a clear migration path.

---

## Recommendation

Given (a) the maintainer guidance "partially preserve, but not at the cost of non-trivial Uno-specific code / if too risky, ditch it", (b) that **even Option A cannot make custom themes compose with element theming**, (c) that Option A puts Uno-specific state on the hot path that the Phase 3 equivalence proof must carefully respect, and (d) that the only real-world consumer in evidence uses the harmless `"Light"` name — **Option B (ditch) is recommended.** It removes a feature that is half-broken under the new model anyway, keeps the hot path purely WinUI-aligned, and the migration cost lands only on apps using non-standard custom names (none observed here).

Choose **Option A** only if there is a known shipping app using a genuine custom theme name that cannot migrate to merged override dictionaries.

---

## DECISION

> **Selected option: B — DITCH** (maintainer decision, 2026-05-21).
>
> **Rationale:** the feature cannot compose with element-level theming under the new model, preserving it adds hot-path Uno-specific state, and no observed consumer relies on a genuine custom name (the one observed usage passes `"Light"`, which is in the harmless bucket).
>
> **Known consumers to protect:** none identified. The one observed usage (`RequestedCustomTheme = "Light"`) is unaffected because `"Light"` resolves identically as the standard theme with or without the feature.
>
> **Phase 6 implementation (ditch):**
> - Remove the custom-name branch in `Application.UpdateRequestedThemesForResources` (`Application.cs:205-214`); `RequestedThemeForResources`/`Themes.Active` becomes strictly `"Light"`/`"Dark"` (+ HC).
> - Hard-deprecate `ApplicationHelper.RequestedCustomTheme` to a no-op with an `[Obsolete]` message pointing to merged brush-override dictionaries (keep the property so existing code compiles; it simply no longer keys a custom `ThemeDictionaries` entry). Confirm `"Light"`/`"Dark"` custom names still resolve as the standard themes (harmless-bucket migration).
> - Document the breaking change (apps using a non-`Light`/`Dark` custom name must move that palette into merged dictionaries that override specific brush/color keys on top of the Light/Dark theme dictionaries).
> - Keep the D7 robustness fallback (a dictionary missing the active theme key falls back to the app base Light/Dark, never the dark `"Default"`).
