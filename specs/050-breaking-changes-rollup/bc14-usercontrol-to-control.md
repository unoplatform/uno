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

## Open decision (needs maintainer confirmation)

Commit to WinUI parity (accept that `ContentTemplate`/`ContentTemplateSelector`/`ContentTransitions` vanish from `UserControl`/`Page`, and `is ContentControl` flips), **or** keep the extra `ContentControl` for back-compat? This is the single highest-risk break in the epic — get explicit sign-off before starting.

## Affected files (starting set)

`src/Uno.UI/UI/Xaml/Controls/UserControl/UserControl.cs`, `…/Page/Page.cs`, `…/ContentControl/ContentControl.cs`, `src/Uno.UI/Generated/3.0.0.0/Microsoft.UI.Xaml.Controls/UserControl.cs`.

## Validation strategy

- Heavy `Uno.UI.RuntimeTests`: **Page navigation** (Frame back/forward, lifecycle), **`ContentPresenter`-less rendering**, **XAML content assignment**, focus & automation peers on `UserControl`/`Page`.
- WinUI parity run via the `/winui-runtime-tests` skill for the same suites.
- SamplesApp regression sweep (nearly every sample is a `UserControl`).

## Sequencing

Do **BC38 (Background → Control)** first so the `Background` relocation onto `Control` is not redone during this re-parenting. Independent of the DataContext / DependencyObject items. Own stabilized PR; never batch.
