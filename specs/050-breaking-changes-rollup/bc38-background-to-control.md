# BC38 — Move `Background` from `FrameworkElement` to `Control`

**Epic:** [#8339](https://github.com/unoplatform/uno/issues/8339) · **Danger:** 4/5 · **Effort:** L · **Phase:** 7 (ship last, own PR) · **Status:** ✅ implemented (true WinUI parity)

## TL;DR

Uno declared `Background`/`BackgroundProperty` on **`FrameworkElement`** (via the internal `IFrameworkElement` interface), so *every* `FrameworkElement` inherited it. WinUI has **no `Background` on `FrameworkElement`** — it declares it individually on the painter types. This item removes the FE-level `Background` and re-declares it exactly where WinUI does.

## Verified WinUI list (authoritative — direct `.idl` sweep of `microsoft-ui-xaml`)

`Brush Background` is declared on exactly **8** types: `Control`, `Panel`, `Border`, `ContentPresenter`, `ItemsRepeater`, `ScrollPresenter` (all `: FrameworkElement`), plus `SwipeItem` and `TextHighlighter` (both `: DependencyObject`).

> ⚠️ **Correction to the original list.** This spec previously listed `TextBlock`, `RichTextBlock`, `Shape`, `Glyphs`, `CalendarViewBaseItem` as declarers. They are **not** — WinUI's `TextBlock`/`RichTextBlock` have no background, `Shape`/`Glyphs` use `Fill`, and `CalendarViewBaseItem : Control` (inherits it). The list also **omitted `ItemsRepeater`**, which WinUI *does* declare. The implementation follows the verified list (the "confirm against WinUI before landing" caveat below resolved this way).

## Current state (verified)

- `IFrameworkElement.cs` declared `Background`/`BackgroundProperty`; implemented on `FrameworkElement` via manual `Register` in `FrameworkElement.Interface.skia.cs` and `.reference.cs`. (On this branch native rendering is dropped — `Uno.UI` builds only Skia + Reference — so there is no `.wasm.cs`/`.Android.cs`/`.UIKit.cs` FE partial; those were the only declaration sites.)
- Generated `Control.cs` emits *"Skipping already declared property Background/BackgroundProperty"* — `Control` had no independent declaration; it only overrode `OnBackgroundChanged` as a no-op.
- `Border`/`ContentPresenter`/`Panel` (all `: FrameworkElement`) **referenced** `BackgroundProperty` in their `OnBackgroundChanged` overrides but **declared none**.
- **Good news:** the border-render pipeline reads `IBorderInfoProvider.Background` (`BorderLayerState.cs`), *not* `FrameworkElement.Background` — so rendering is decoupled and keeps working once each type re-declares the DP.

## What changed (implemented)

1. Removed `Background`/`BackgroundProperty` (and the FE-level `protected virtual OnBackgroundChanged`) from `IFrameworkElement` and the `FrameworkElement.Interface.skia.cs` / `.reference.cs` partials. (The `.wasm.cs` partial referenced in the original draft no longer exists on this branch — native rendering was dropped, so `Uno.UI` builds only Skia + Reference.)
2. Declared `Background` independently on each WinUI declarer that is a `FrameworkElement` subclass:
   - `Control` (with `protected virtual OnBackgroundChanged`, a placeholder no-op; `Page`/`CalendarViewBaseItem` override it).
   - `Panel`, `Border`, `ContentPresenter` (each keeps its painting `OnBackgroundChanged`; `override` → `virtual`).
   - `ItemsRepeater` (un-commented its previously-stubbed DP).
   - `ScrollPresenter` already re-declared it with `new`; dropped the now-pointless `new` and the `base.Background = …` init (the long-standing TODO).
3. `CalendarViewBaseItem : Control` and `Page : … : Control` keep `Background` **by inheritance from `Control`** (matches WinUI).
4. Types that lose `Background` to match WinUI: `TextBlock`, `RichTextBlock`, `Shape` (+ `Rectangle`/`Ellipse`/`Line`/`Path`/`Polygon`/`Polyline`), `Glyphs`, `Image`, `AnimatedVisualPlayer`, and every other bare `FrameworkElement` subclass. `Shape`'s no-op `OnBackgroundChanged` override was removed.

### Consumers reconciled

- `FrameworkElement.IsViewHit()` legacy branch: `Background != null` → `this is IBorderInfoProvider { Background: { } }` (the painters already override `IsViewHit` via `Border.IsViewHitImpl`).
- `Slider.mux.cs`: `_tpSliderContainer.Background` (FE-typed; the template's `SliderContainer` is a `Grid`) → gated on `is Panel`.
- `Uno.UI.Lottie` `GetBackgroundColor()` read `AnimatedVisualPlayer.Background` (never set anywhere) → canvas clears to `Transparent`.
- `Win32NativeWebView`: `FrameworkElement.BackgroundProperty` → `ContentPresenter.BackgroundProperty` (the `_presenter` field type).
- Runtime tests (`Given_TreeView`, `Given_ItemsRepeater`, `Given_VisualTreeHelper`) re-pointed at the concrete declaring type.
- Framework XAML was clean except one native-only Button template (`Generic.Native.xaml`) and one source-generator fixture (`ThemeResourcesTest.xaml`).

## Pros

- **WinUI parity** — matches exactly which types expose `Background`, eliminating a long-standing surface divergence.
- Removes a misleading inheritance placeholder: a bare `FrameworkElement` advertising `Background` it doesn't paint.
- Render path is already decoupled (`IBorderInfoProvider`), so the *internal* refactor is low-risk if every painter re-declares the DP.

## Cons / risks

- **Common-usage break.** `Background` is read/set through `FrameworkElement`-, `Panel`-, `Grid`-, `StackPanel`-, `TextBlock`-typed references constantly. Any such access through a type that *doesn't* re-declare it breaks at compile time. `<Setter Property="Background">` in styles targeting non-`Control` types is everywhere.
- **Silent-loss footgun:** if the change adds `Background` to `Control` but **forgets** `Panel`/`Border`/`ContentPresenter`/`TextBlock`/`Shape`, those extremely common controls *silently lose `Background` entirely*. The re-declaration list must be exhaustive.

## Decision (resolved)

Proceeded with the **full WinUI-exact split** (true parity). `Background` now exists only where WinUI declares it; bare `FrameworkElement` subclasses such as `TextBlock`/`Shape`/`Image` no longer expose it. The convenient FE-level placeholder was *not* kept.

## Affected files (as implemented)

- Removed from: `IFrameworkElement.cs`, `FrameworkElement.Interface.skia.cs`, `FrameworkElement.Interface.reference.cs`.
- Declared on: `Controls/Control/Control.cs`, `Controls/Panel/Panel.cs`, `Controls/Border/Border.cs`, `Controls/ContentPresenter/ContentPresenter.cs`, `Controls/Repeater/ItemsRepeater.Properties.cs`; `new` dropped in `Controls/ScrollPresenter/ScrollPresenterPrimitives.idl.cs` (+ `ScrollPresenter.cs`).
- Consumers reconciled: `FrameworkElement.cs` (`IsViewHit`), `Controls/Slider/Slider.mux.cs`, `Shapes/Shape.cs`, `AddIns/Uno.UI.Lottie/LottieVisualSource.Skottie.cs`, `Uno.UI.Runtime.Skia.Win32/.../Win32NativeWebView.cs`, `Style/Generic/Generic.Native.xaml`.
- Tests: new `Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml/Given_Background.cs`; fixed `Given_TreeView.cs`, `Given_ItemsRepeater.cs`, `Given_VisualTreeHelper.cs`; fixed source-gen fixture `SourceGenerators/XamlGenerationTests/ThemeResourcesTest.xaml`.

## Validation performed

- **Compile (Skia):** `Uno.UI.Skia`, `Uno.UI.Reference`, `Uno.UI.RuntimeTests.Skia` (transitively `Uno.UI.Lottie` + Skia runtime), `Uno.UI.Runtime.Skia.Win32`, and `SamplesApp.Skia.Generic` build with 0 errors. The compiler is the exhaustive checker for `.Background` access on now-unqualified types.
- **Audit:** repo-wide sweep for `.Background`/`BackgroundProperty` through non-declaring types and for XAML `Background`/`<Setter>` on losing types — framework XAML clean (one native-only template fixed); SamplesApp XAML/C# clean.
- **Runtime tests (Skia):** `Given_Background` asserts `Background` renders on `Border`, `Panel` (`Grid`), `ContentPresenter` and that each painter owns an independent DP.
- **WASM:** shares the same `Uno.UI.Skia` assembly (native rendering dropped); the same `Given_Background` tests run via the Skia-WASM head (`SamplesApp.Skia.WebAssembly.Browser`).

## Sequencing

Do this **before BC14** (`UserControl → Control`) so the `Background`-on-`Control` relocation is settled before the hierarchy re-parenting. Own stabilized PR; never batch.
