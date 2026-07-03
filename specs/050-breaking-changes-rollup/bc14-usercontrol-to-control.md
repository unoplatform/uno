# BC14 — `UserControl` inherits `Control` (drop the extra `ContentControl`)

**Epic:** [#8339](https://github.com/unoplatform/uno/issues/8339) · **Danger:** 5/5 (highest) · **Effort:** L · **Phase:** 7 (ship last, own PR)

## TL;DR

WinUI hierarchy is **`Control` → `UserControl` → `Page`**, with `UserControl.Content` typed **`UIElement`** (not `object`) and no content-template machinery. Uno inserts an extra layer: **`Control` → `ContentControl` → `UserControl` → `Page`**. This item removes the extra `ContentControl` so `UserControl : Control` directly, matching WinUI.

## Current state (verified)

- `src/Uno.UI/UI/Xaml/Controls/UserControl/UserControl.cs:7` → `public partial class UserControl : ContentControl`. `Page : UserControl`.
- All `UserControl` content rendering currently flows through `ContentControl` (`ContentPresenter` / `OnContentChanged` / template — ~30 refs in `ContentControl.cs`).
- WinUI (local MUX IDL): `UserControl : Control` with `UIElement Content` + `ContentProperty` (`controls.idl`), `Page : UserControl` (`controls2.idl`).

## What changes

1. Re-parent `UserControl` onto `Control`, giving it its **own `UIElement`-typed `Content` / `ContentProperty`**.
2. Reimplement single-child content hosting **the WinUI way** — a direct single `UIElement` child, *without* `ContentPresenter`/`ContentTemplate` plumbing.
3. Cascade through `Page` (and the generated `UserControl.cs` stub).

## Pros

- **True WinUI parity** for the most-subclassed control in any app. `x is ContentControl` / casts behave as on WinUI.
- Removes a layer of `ContentPresenter`/template overhead from every `UserControl` and `Page` — measurably lighter visual trees and faster load.
- Aligns `Content` typing (`UIElement`) with WinUI, eliminating a long-standing semantic divergence.

## Cons / risks

- **Broadest blast radius of any item in this epic.** Essentially every app and control library subclasses `UserControl`/`Page`.
- Dropping `ContentControl` **removes `ContentTemplate`, `ContentTemplateSelector`, `ContentTransitions`, and the `object`-typed `Content` setter** from every `UserControl`/`Page`. XAML/code-behind that set any of these on a `UserControl` break.
- `x is ContentControl` checks and `ContentControl`-typed casts against a `UserControl`/`Page` **flip from `true` to `false`** — can silently change behaviour in framework and app code.
- Re-implementing content hosting without `ContentPresenter` is non-trivial: focus, automation peers, and visual-state hosting all currently lean on the `ContentControl` path.

## Decision (resolved)

**Commit to WinUI parity.** `ContentTemplate`/`ContentTemplateSelector`/`ContentTransitions` and the `object`-typed `Content` setter are removed from `UserControl`/`Page`; `Content` is typed `UIElement`; `is ContentControl` against a `UserControl`/`Page` flips to `false`. The back-compat alternative (keeping the extra `ContentControl`) was rejected.

## Affected files (starting set)

`src/Uno.UI/UI/Xaml/Controls/UserControl/UserControl.cs`, `…/Page/Page.cs`, `…/ContentControl/ContentControl.cs`, `src/Uno.UI/Generated/3.0.0.0/Microsoft.UI.Xaml.Controls/UserControl.cs`.

## Validation strategy

- Heavy `Uno.UI.RuntimeTests`: **Page navigation** (Frame back/forward, lifecycle), **`ContentPresenter`-less rendering**, **XAML content assignment**, focus & automation peers on `UserControl`/`Page`.
- WinUI parity run via the `/winui-runtime-tests` skill for the same suites.
- SamplesApp regression sweep (nearly every sample is a `UserControl`).

## Sequencing

Originally planned after **BC38 (Background → Control)**. In practice BC14 is **independent of BC38**: `Background` is declared on `FrameworkElement`, so `UserControl` keeps inheriting it through `FrameworkElement` → `Control` → `UserControl` regardless of BC38; if BC38 lands later it relocates `Background` onto `Control` without disturbing this change. Independent of the DataContext / DependencyObject items. Own stabilized PR; never batch.

## Implementation (as landed)

`UserControl : Control` with its own `UIElement`-typed `Content` (`[GeneratedDependencyProperty]`, `AffectsMeasure | ValueDoesNotInheritDataContext`). Single-child hosting is done directly via `AddChild`/`RemoveChild` in `OnContentChanged` — these primitives exist on every platform, so no per-platform partial and no `ContentPresenter`/template plumbing is needed. `Control`'s existing single-child `MeasureOverride`/`ArrangeOverride` (`FindFirstChild`/`ArrangeFirstChild`) handles layout, so `UserControl` adds none. `Page` inherits the new base unchanged (its `OnBackgroundChanged` override still works). The generated `UserControl`/`Page` stubs already matched the target shape (stub skips `Content`/`ContentProperty`), so no regeneration was required — confirmed by a clean full-samples build.

Consumer adaptation was minimal: `IFrameworkElement.FindName` gained a `UserControl.Content` branch (covers `Page`), and hot reload's `SwapViews` gained a `UserControl` parent path. The other `is ContentControl` sites (AriaMapper, FocusManager, VisualStateUtil, WASM a11y) correctly stop matching `UserControl`/`Page` and needed no code change.

Files changed: `…/Controls/UserControl/UserControl.cs`, `…/Controls/UserControl/UserControl.Properties.cs` (new), `…/UI/Xaml/IFrameworkElement.cs`, `Uno.UI.RemoteControl/HotReload/ClientHotReloadProcessor.Common.cs`, plus `Uno.UI.RuntimeTests/.../Given_UserControl.cs` (new). `ContentControl.cs` and the generated stubs were **not** touched.

## Validation status

- **Done (Skia Desktop, runtime):** `Given_UserControl` (5/5) plus a regression batch of **407/0** across `Given_Control`, `Given_ContentControl` (incl. FindName), `Given_ContentPresenter`, `Given_Frame` (navigation), `Given_VisualStateManager`, `Given_FocusManager`, `Given_FrameworkElement`. Full-samples Skia build clean (Uno.UI, RemoteControl, SamplesApp).
- **Pending (PR-stabilization):** WinUI-parity run (`/winui-runtime-tests`), SamplesApp light/dark visual sweep (layout-equivalence headline risk), WASM Skia head, HotReload app-harness tests.
