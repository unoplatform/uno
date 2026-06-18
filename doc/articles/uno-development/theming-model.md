# The theming model (WinUI-aligned)

This note describes the invariants of Uno Platform's theming implementation on the enhanced-lifecycle targets (Skia, WebAssembly). The implementation is a structural 1:1 port of WinUI's theming machinery (sources cited per file with `MUX Reference …, commit fc2f82117` headers); this page exists so future control work doesn't reintroduce the per-call-site workarounds the port removed.

## The invariants

1. **Every `DependencyObject` carries a resolved theme.** The per-object theme (`CDependencyObject::m_theme` in WinUI) lives on `DependencyObjectStore` and is established when the object enters the live tree: the `Enter` walk (`DependencyObjectStore.mux.cs`, ported from `depends.cpp`) recursively enters DependencyObject-valued property values and finishes with the theme block — inherit from the (logical) inheritance parent, or refresh theme references. Popups and flyout content inherit through their *logical* parent, which is why they are themed correctly on first open.

2. **The theme is never cleared on Leave.** Re-entering the tree re-establishes it from the new parent (recycled containers keep working).

3. **`{ThemeResource}` resolution is a pure function of (key, ambient theme).** The resolution leaf (`ResourceDictionary.EnsureActiveThemeDictionary`, ported from `Resources.cpp:687-819`) selects the theme sub-dictionary from one core-level ambient: `CoreServices.RequestedThemeForSubTree`, falling back to `FrameworkTheming.GetBaseTheme()`. There is **no resolution-time ancestor walk** and no per-element theme parameter threading.

4. **The ambient slot has exactly four writers**, all scoped save/set/restore, all derived from a per-object theme (matching WinUI):
   - `DependencyObjectStore.NotifyThemeChanged` — the theme walk (`Theming.cpp:137-149`);
   - `DependencyObjectStore.UpdateThemeReference` — re-binding a theme reference outside a walk (`SetThemeResourceBinding`, `Theming.cpp:364-379`);
   - `FrameworkElement.NotifyThemeChangedForInheritedProperties` — the inherited-foreground lookup (`framework.cpp:3441-3487`);
   - `CoreServices.ScopeRequestedThemeForSubTree` — the keyed-lookup helper (`CCoreServices::LookupThemeResource`, `xcpcore.cpp:2371-2394`) used by setter application, focus-visual defaults, hyperlink state brushes and lazy materialization.

   **Do not add new ambient writers for a new materialization path.** If a path resolves the wrong theme, the root cause is an `Enter` coverage gap (the owner's theme wasn't established) — fix that.

5. **The theme walk is a `DependencyObject` mechanism.** `NotifyThemeChanged`/`NotifyThemeChangedCore`/`NotifyThemeChangedCoreImpl` live on `DependencyObjectStore` (ported from `components/DependencyObject/Theming.cpp`); `UIElement` recurses into visual children, `FrameworkElement` adds the inherited-property freeze and raises `ActualThemeChanged`, `Popup`/`PopupRoot` add the open-popup propagation — the same virtual chain as the C++ class hierarchy. Property values are notified per-child (skipping only UIElements active in the visual tree); detached values (e.g. unopened flyout items) are walked and persist their own theme.

6. **`ActualThemeChanged` is delivered asynchronously** (posted), after the new theme is persisted — handlers observe the new `ActualTheme`, like WinUI's EventManager-raised events. Elements outside the live tree with subscribers are raised through the core theme-changed-listener registry.

7. **App/system theme state lives in `FrameworkTheming`** (`CoreServices.Theming`): effective theme = `(requested ?: system) | highContrast`. An explicitly-set `Application.RequestedTheme` suppresses OS following by construction (the OS flip doesn't change the composed theme, so no walk starts). App-level changes walk all roots through `CoreServices.NotifyThemeChange` (`xcpcore.cpp:8006-8118`): system resources, app resources, the popup root, the visual root (with the foreground freeze), and island roots.

8. **Theme keys are strictly `Light`/`Dark`** (plus the high-contrast keys). The custom-theme axis (`ApplicationHelper.RequestedCustomTheme`) is retired — custom palettes use merged dictionaries overriding specific keys.

## Native targets

Native Android/iOS support **OS + application theme only**; the per-object theme, `Enter` establishment, and element-level theming are intentionally not compiled there (`UNO_HAS_ENHANCED_LIFECYCLE`). Don't extend element theming to native without an explicit decision.

## Known follow-ups

- OS high-contrast detection on Skia (the HC machinery is structurally complete but driven by `WinRTFeatureConfiguration.Accessibility.HighContrast`; the White/Black/Custom variant classification is preserved as comments at the selection sites).
- Namescope (`RegisterName`) portions of the `Enter`/`Leave` port are `TODO`-documented in `DependencyObjectStore.mux.cs`.
