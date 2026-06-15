# BC38 — Move `Background` from `FrameworkElement` to `Control`

**Epic:** [#8339](https://github.com/unoplatform/uno/issues/8339) · **Danger:** 4/5 · **Effort:** L · **Phase:** 7 (ship last, own PR)

## TL;DR

Uno declares `Background`/`BackgroundProperty` on **`FrameworkElement`** (via the internal `IFrameworkElement` interface), so *every* `FrameworkElement` inherits it. WinUI has **no `Background` on `FrameworkElement`** — it declares it individually on `Control`, `Panel`, `Border`, `ContentPresenter`, `TextBlock`, `RichTextBlock`, `Shape`, `Glyphs`, `CalendarViewBaseItem`. This item removes the FE-level `Background` and re-declares it on `Control` **and on every non-`Control` type that paints a background**.

## Current state (verified)

- `IFrameworkElement.cs:98` declares `Background`/`BackgroundProperty`; implemented per-platform on `FrameworkElement` (`FrameworkElement.Interface.skia.cs:83/91` manual `Register`; `.wasm.cs:97-104` `[GeneratedDependencyProperty]`; `.reference.cs:72-80`).
- Generated `Control.cs` emits *"Skipping already declared property Background/BackgroundProperty"* — proving `Control` has no independent declaration today; it only overrides `OnBackgroundChanged` as a no-op.
- `Border` (`:FrameworkElement`), `ContentPresenter` (`:FrameworkElement`), `Panel`, `Shape`, `TextBlock` all **reference** `BackgroundProperty` but **none declare it** (`Border.cs:334`, `ContentPresenter.cs:1149`, `Panel.cs:247`).
- **Good news:** the border-render pipeline reads `IBorderInfoProvider.Background` (`BorderLayerState.cs`), *not* `FrameworkElement.Background` — so rendering is decoupled and keeps working once each type re-declares the DP.

## What changes

1. Remove `Background`/`BackgroundProperty` from `IFrameworkElement` and the `FrameworkElement.Interface.*` partials.
2. Declare it on `Control`.
3. **Independently re-declare** it on each non-`Control` background-painter: `Panel`, `Border`, `ContentPresenter`, `TextBlock`, `RichTextBlock`, `Shape`, `Glyphs`, `CalendarViewBaseItem` (confirm the full list against WinUI before landing).

## Pros

- **WinUI parity** — matches exactly which types expose `Background`, eliminating a long-standing surface divergence.
- Removes a misleading inheritance placeholder: a bare `FrameworkElement` advertising `Background` it doesn't paint.
- Render path is already decoupled (`IBorderInfoProvider`), so the *internal* refactor is low-risk if every painter re-declares the DP.

## Cons / risks

- **Common-usage break.** `Background` is read/set through `FrameworkElement`-, `Panel`-, `Grid`-, `StackPanel`-, `TextBlock`-typed references constantly. Any such access through a type that *doesn't* re-declare it breaks at compile time. `<Setter Property="Background">` in styles targeting non-`Control` types is everywhere.
- **Silent-loss footgun:** if the change adds `Background` to `Control` but **forgets** `Panel`/`Border`/`ContentPresenter`/`TextBlock`/`Shape`, those extremely common controls *silently lose `Background` entirely*. The re-declaration list must be exhaustive.

## Open decision (needs maintainer confirmation)

Proceed with the full WinUI-exact split (move to `Control` **and** re-declare on every painter), or keep the convenient FE-level `Background` to avoid the wide source break? If proceeding, **confirm the complete list** of non-`Control` types that must re-declare it so none silently loses `Background`.

## Affected files (starting set)

`src/Uno.UI/UI/Xaml/IFrameworkElement.cs`, `…/FrameworkElement.Interface.skia.cs` `.wasm.cs` `.reference.cs`, `…/Controls/Control/Control.cs`, `…/Controls/Panel/Panel.cs`, `…/Controls/Border/Border.cs`, `…/Controls/ContentPresenter/ContentPresenter.cs`, `…/Controls/TextBlock/TextBlock.skia.cs`, `…/Shapes/Shape.cs`, `…/Controls/Border/IBorderInfoProvider.cs`, `src/Uno.UI/Generated/3.0.0.0/Microsoft.UI.Xaml.Controls/Control.cs`.

## Validation strategy

- Runtime tests asserting `Background` renders on each re-declaring type (Panel/Grid/Border/ContentPresenter/TextBlock/Shape) on Skia + WASM.
- A compile-time audit grepping for `.Background` access through non-redeclaring types in the repo itself (SamplesApp, controls) before landing.
- Visual sweep of SamplesApp backgrounds in light + dark themes.

## Sequencing

Do this **before BC14** (`UserControl → Control`) so the `Background`-on-`Control` relocation is settled before the hierarchy re-parenting. Own stabilized PR; never batch.
