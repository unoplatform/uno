# BC58 — `DataContext` on `FrameworkElement` only (root cause) · folds in BC54

**Epic:** [#8339](https://github.com/unoplatform/uno/issues/8339) · **Issue:** [#13201](https://github.com/unoplatform/uno/issues/13201) (OPEN umbrella) · **Also resolves BC54** ([#12491](https://github.com/unoplatform/uno/issues/12491), closed as dup) · **Danger:** 4/5 · **Effort:** M · **Phase:** 7 (ship last, own PR) · **Status:** ✅ implemented

## TL;DR

Uno's `DependencyObjectGenerator` used to emit a **public `DataContext` property + `DataContextProperty` DP + `DataContextChanged` event + `OnDataContextChanged`** onto **every** `DependencyObject` (`Brush`, `Transform`, `Setter`, `DependencyObjectCollection`, `FlyoutBase`, `GradientStop`, …). WinUI exposes these on **`FrameworkElement` only**. This item restricted them to `FrameworkElement`.

**BC54** (`FlyoutBase.DataContext` should not be public) was just the most-cited *symptom* of this — `FlyoutBase : DependencyObject` got the property from the generator, not from its own files. Fixing BC58 fixed BC54; they were implemented together.

## Original state (before the change)

_As found when this spec was written. `DependencyObjectGenerator` and `DependencyObjectStore` no longer exist; see **As implemented** for what shipped._

- `DependencyObjectGenerator.WriteBinderImplementation` (~lines 619-649) **unconditionally** emitted `public object DataContext`, `public static DependencyProperty DataContextProperty`, the `DataContextChanged` event (`WriteInitializer` ~474), and `OnDataContextChanged`/`…Partial` for the **root DO implementer of every chain** — not just FE.
- The generator already *knew* many emitters were non-FE: it picked the `DataContextChanged` sender as `this` / `this as FrameworkElement` / `null` (lines ~600-617) — yet still emitted the property.
- `DependencyObjectStore` wiring assumed **every store had a `DataContextProperty`**: `__storeBackingField = new DependencyObjectStore(this, DataContextProperty)` (gen ~769), plus `FrameworkPropertyMetadataOptions.Inherits` inheritance propagation.
- `FlyoutBase` **functionally relied** on the inherited DataContext: `OnDataContextChangedPartial` forwarded to its `Popup`; `ShowAttachedFlyout` set `FlyoutBase.DataContextProperty` from the owner.

## Original plan (superseded)

_Kept for the record. Steps 1-3 did not ship as written; see **Decision** and **As implemented**._

1. In `DependencyObjectGenerator`, gate emission of `DataContext`/`DataContextProperty`/`DataContextChanged`/`OnDataContextChanged` to **FE-derived** types.
2. Rework `DependencyObjectStore` so a store **without** a `DataContextProperty` is valid (ctor + inheritance/propagation machinery must no longer assume every DO carries it).
3. Preserve **internal** DataContext flow where load-bearing (e.g. `FlyoutBase` → `Popup` forwarding) — keep an internal mechanism, hide only the public member.
4. Regenerate x:Bind / XAML **goldens**; update `build/PackageDiffIgnore.xml`; fix unit/runtime tests that assert DataContext on non-FE types.

## Pros

- **WinUI parity** — removes a large over-broad public surface (`DataContext` on brushes/transforms/setters/etc.) that WinUI never had.
- Eliminates a class of "works in Uno, not in WinUI" bugs where apps read or set `DataContext` directly on a `Brush`/`Transform`.
- Resolves BC54 and the #13201 umbrella in one stroke; smaller generated output.

## Cons / risks

- **Source/binary break** for code that reads/sets `.DataContext` on a non-FE DO, binds a `Brush`/`Transform`/`Setter` to a DataContext, subscribes to `DataContextChanged` on such a type, or references `Brush.DataContextProperty`.
- **Silent binding breakage:** XAML relying on a non-FE element inheriting the ambient DataContext (a common Uno pattern WinUI does *not* support) stops resolving — no compile error, just a binding that no longer fires. Likelihood is **moderate-to-high for libraries** doing dynamic styling/binding on brushes; lower for simple apps. _(Did not materialise as implemented — bindings still resolve through the inheritance context; see Decision.)_
- The `DependencyObjectStore` inheritance rewrite is load-bearing — getting propagation wrong breaks DataContext inheritance for the *whole* tree.

## Decision (resolved)

- **The break is API-surface only.** `DataContext` on a `Brush`/`Transform`/`Setter` was never WinUI API, so reading, setting or subscribing to it there is now a compile error. `{Binding}` on those objects keeps resolving: as in WinUI, where `BindingExpression::AttachToDataContext` falls back to the target's mentor, a non-FE object picks up the ambient `DataContext` of the `FrameworkElement` it is attached to. Multi-parent sharing is **stricter than WinUI**: Uno's `AssociateParent` (`DependencyObject.Binder.cs`) turns the inheritance context off permanently once a second owner attaches the object, but skips the exceptions in WinUI's `CMultiParentShareableDependencyObject::GetMentor` (a first `ResourceDictionary` parent, or a first `ContentControl`/`ScrollContentControl` parent with at most two parents). `Given_NonFE_DataContextBinding` guards the cases confirmed against native WinUI.
- **The DP is owned directly by `FrameworkElement`, not generated.** Rather than gate the generator's emission to FE-derived types, `DataContext`/`DataContextProperty`/`DataContextChanged`/`OnDataContextChanged` were lifted out of `DependencyObjectGenerator` entirely and hand-written once in `src/Uno.UI/UI/Xaml/FrameworkElement.DataContext.cs`. The generator no longer mentions `DataContext` at all. Inheritance still comes from `FrameworkPropertyMetadataOptions.Inherits` on that single registration.
- **`FlyoutBase` keeps working without inheriting a DataContext.** The earlier "keep an internal mechanism" plan was dropped in favour of the WinUI approach: the popup deliberately carries no `DataContext` (forwarding the owner's onto a kept-alive popup leaked it), and `ForwardTargetPropertiesToPresenter` instead copies the placement target's `DataContext` onto the presenter at show-time, clearing it in `OnClosed`. This mirrors WinUI's `FlyoutBase_partial.cpp`.

## As implemented

Landed via unoplatform/uno#23547.

1. `DataContext` emission removed from `DependencyObjectGenerator` (which BC26 later deleted outright); declared on `FrameworkElement` in the new `FrameworkElement.DataContext.cs`.
2. The planned `DependencyObjectStore` rework was overtaken by BC26 phase 2 (unoplatform/uno#23702), which dropped the separate store type altogether — the property storage now lives on `DependencyObject` itself (`DependencyObject.Store.cs` / `.Binder.cs`), and nothing there assumes a `DataContextProperty` exists.
3. `FlyoutBase` forwards target → presenter at show-time (`ForwardTargetPropertiesToPresenter`) and clears on close (`OnClosed`), replacing the old popup DataContext binding. This resolves BC54 as intended.
4. Removals covered in `build/PackageDiffIgnore.xml` by two regex entries (`.+(::|\.)DataContext\(\)`, `.+(::|\.)DataContextProperty\(\)`, reason "DataContext restricted to FrameworkElement").
5. Consumer guidance written up in `doc/articles/migrating-to-uno-7.md` → "Members restricted to their WinUI declaring types".

## Affected files (original plan)

_The generator and `DependencyObjectStore*` files below have since been removed by BC26._

`src/SourceGenerators/Uno.UI.SourceGenerators/DependencyObject/DependencyObjectGenerator.cs`, `src/Uno.UI/UI/Xaml/DependencyObjectStore.Binder.cs`, `…/DependencyObjectStore.cs`, `…/Controls/Flyout/FlyoutBase.cs`, `src/Uno.UI.UnitTests/DependencyProperty/Given_DependencyProperty.DataContext.cs`, `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml/Given_UIElement.DataContext.cs`, `src/SourceGenerators/Uno.UI.SourceGenerators.Tests/DependencyObjectGeneratorTests/Given_DependencyObjectGenerator.cs`, `build/PackageDiffIgnore.xml`.

## Validation strategy (original plan)

- Runtime tests: DataContext **inheritance** through FE trees still works; `FlyoutBase`/`MenuFlyout` binding still resolves (the canary for the internal-forwarding requirement); non-FE types no longer expose `DataContext`.
- Generator golden regeneration + diff review of a non-FE type (e.g. `Brush`) before/after.
- Existing DataContext unit/runtime suites green after the asserted-removals are adjusted.

## Sequencing

Do **BC58 first**; it gates **BC54** (same change) and de-risks **BC26** (`DependencyObject` as class) since both rewrite the generator's DO emission. Orthogonal in theory (BC26 is class-vs-interface; BC58 is which-subtypes-get-the-property) but settling generator output here first keeps BC26's diff clean. Own stabilized PR; never batch. _(This order held: #23547 merged before BC26's #23537.)_
