# BC58 — `DataContext` on `FrameworkElement` only (root cause) · folds in BC54

**Epic:** [#8339](https://github.com/unoplatform/uno/issues/8339) · **Issue:** [#13201](https://github.com/unoplatform/uno/issues/13201) (OPEN umbrella) · **Also resolves BC54** ([#12491](https://github.com/unoplatform/uno/issues/12491), closed as dup) · **Danger:** 4/5 · **Effort:** M · **Phase:** 7 (ship last, own PR)

## TL;DR

Uno's `DependencyObjectGenerator` emits a **public `DataContext` property + `DataContextProperty` DP + `DataContextChanged` event + `OnDataContextChanged`** onto **every** `DependencyObject` (`Brush`, `Transform`, `Setter`, `DependencyObjectCollection`, `FlyoutBase`, `GradientStop`, …). WinUI exposes these on **`FrameworkElement` only**. This item restricts the generated emission to `FrameworkElement`-derived types.

**BC54** (`FlyoutBase.DataContext` should not be public) is just the most-cited *symptom* of this — `FlyoutBase : DependencyObject` gets the property from the generator, not from its own files. Fixing BC58 fixes BC54; implement them together.

## Current state (verified)

- `DependencyObjectGenerator.WriteBinderImplementation` (~lines 619-649) **unconditionally** emits `public object DataContext`, `public static DependencyProperty DataContextProperty`, the `DataContextChanged` event (`WriteInitializer` ~474), and `OnDataContextChanged`/`…Partial` for the **root DO implementer of every chain** — not just FE.
- The generator already *knows* many emitters are non-FE: it picks the `DataContextChanged` sender as `this` / `this as FrameworkElement` / `null` (lines ~600-617) — yet still emits the property.
- `DependencyObjectStore` wiring assumes **every store has a `DataContextProperty`**: `__storeBackingField = new DependencyObjectStore(this, DataContextProperty)` (gen ~769), plus `FrameworkPropertyMetadataOptions.Inherits` inheritance propagation.
- `FlyoutBase` **functionally relies** on the inherited DataContext: `OnDataContextChangedPartial` forwards to its `Popup`; `ShowAttachedFlyout` sets `FlyoutBase.DataContextProperty` from the owner. Any fix must keep this working **internally** while only hiding the public surface.

## What changes

1. In `DependencyObjectGenerator`, gate emission of `DataContext`/`DataContextProperty`/`DataContextChanged`/`OnDataContextChanged` to **FE-derived** types.
2. Rework `DependencyObjectStore` so a store **without** a `DataContextProperty` is valid (ctor + inheritance/propagation machinery must no longer assume every DO carries it).
3. Preserve **internal** DataContext flow where load-bearing (e.g. `FlyoutBase` → `Popup` forwarding) — keep an internal mechanism, hide only the public member.
4. Regenerate x:Bind / XAML **goldens**; update `build/PackageDiffIgnore.xml`; fix unit/runtime tests that assert DataContext on non-FE types.

## Pros

- **WinUI parity** — removes a large over-broad public surface (`DataContext` on brushes/transforms/setters/etc.) that WinUI never had.
- Eliminates a class of "works in Uno, not in WinUI" bugs where apps bind a `Brush`/`Transform` to the ambient `DataContext`.
- Resolves BC54 and the #13201 umbrella in one stroke; smaller generated output.

## Cons / risks

- **Source/binary break** for code that reads/sets `.DataContext` on a non-FE DO, binds a `Brush`/`Transform`/`Setter` to a DataContext, subscribes to `DataContextChanged` on such a type, or references `Brush.DataContextProperty`.
- **Silent binding breakage:** XAML relying on a non-FE element inheriting the ambient DataContext (a common Uno pattern WinUI does *not* support) stops resolving — no compile error, just a binding that no longer fires. Likelihood is **moderate-to-high for libraries** doing dynamic styling/binding on brushes; lower for simple apps.
- The `DependencyObjectStore` inheritance rewrite is load-bearing — getting propagation wrong breaks DataContext inheritance for the *whole* tree.

## Open decision (needs maintainer confirmation)

- Confirm we accept the break to Uno's de-facto "bind a `Brush` to `DataContext`" pattern.
- **Where does the DataContext DP get registered/owned** — directly on `FrameworkElement`, or still generated but gated to FE-derived types?
- How is inheritance/propagation re-wired in `DependencyObjectStore`, which today assumes every DO has a `DataContextProperty`?

## Affected files (starting set)

`src/SourceGenerators/Uno.UI.SourceGenerators/DependencyObject/DependencyObjectGenerator.cs`, `src/Uno.UI/UI/Xaml/DependencyObjectStore.Binder.cs`, `…/DependencyObjectStore.cs`, `…/Controls/Flyout/FlyoutBase.cs`, `src/Uno.UI.UnitTests/DependencyProperty/Given_DependencyProperty.DataContext.cs`, `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml/Given_UIElement.DataContext.cs`, `src/SourceGenerators/Uno.UI.SourceGenerators.Tests/DependencyObjectGeneratorTests/Given_DependencyObjectGenerator.cs`, `build/PackageDiffIgnore.xml`.

## Validation strategy

- Runtime tests: DataContext **inheritance** through FE trees still works; `FlyoutBase`/`MenuFlyout` binding still resolves (the canary for the internal-forwarding requirement); non-FE types no longer expose `DataContext`.
- Generator golden regeneration + diff review of a non-FE type (e.g. `Brush`) before/after.
- Existing DataContext unit/runtime suites green after the asserted-removals are adjusted.

## Sequencing

Do **BC58 first**; it gates **BC54** (same change) and de-risks **BC26** (`DependencyObject` as class) since both rewrite the generator's DO emission. Orthogonal in theory (BC26 is class-vs-interface; BC58 is which-subtypes-get-the-property) but settling generator output here first keeps BC26's diff clean. Own stabilized PR; never batch.
