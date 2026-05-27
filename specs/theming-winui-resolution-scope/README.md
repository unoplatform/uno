# Theming WinUI Alignment — Resolution Scope & Providing-Dictionary Pinning

A follow-up to [`../theming-winui-alignment`](../theming-winui-alignment/README.md). That effort aligned the
**theme input** of `{ThemeResource}` resolution (Mechanism 1 / D3: the owner's effective theme is threaded as a
parameter into the resolution leaf instead of read from a process-global ambient). It shipped per-object theme on
`DependencyObjectStore`, `EstablishThemeAtEnter`, owner-theme-threaded resolution, and deleted the global push stack.

It did **not** align the other half of WinUI's model: **how the *providing* `ResourceDictionary` is captured and
re-found.** That is the entire remaining divergence, and it is why a class of popup/reparented `{ThemeResource}`
scenarios still resolve the wrong theme even though the element's `ActualTheme` is correct.

## Why (the still-failing behavior)

A `{ThemeResource}` whose key is declared in an **ancestor's local `ThemeDictionaries`** (not in
`Application.Resources`) resolves the **wrong theme's value** when the element is hosted in a popup/flyout/tooltip
(reparented under `PopupRoot`), even though the element's `ActualTheme` is correctly inherited. Confirmed failing on
Skia Desktop (see [`tests.md`](./tests.md) for the validated run):

| Repro (runtime test) | Symptom |
|---|---|
| `Given_Flyout.When_Flyout_Opened_From_Light_Subtree_Under_Dark_App_Resolves_Light_ThemeResource` | `ActualTheme=Light`, brush resolves the **Dark** value |
| `Given_MenuFlyout.When_Flyout_Menu_Uses_Owner_Subtree_Theme_Light_Under_Dark_App` | same |
| `Given_MenuFlyout.When_MenuFlyout_Item_Uses_Owner_Subtree_Theme_Light_Under_Dark_App` | same |
| `Given_ToolTip.When_ToolTip_Label_Uses_Owner_Subtree_Theme_Light_Under_Dark_App` | same |
| `Given_ListViewBase.When_Grid_Row_Presented_After_Tab_Navigation_Light_Under_Dark_App` | same, after unload/reload |

The value tracks the **application/OS theme** (`Themes.Active`), not the owner's. The "passing" sibling
`Given_Flyout.When_Flyout_Opened_From_Inner_Light_Boundary_Resolves_Light_ThemeResource` only passes because it does
not pin the app to Dark — it matches the host OS theme by accident.

## The one defect, stated precisely

Uno builds a `ThemeResourceReference` (its `CThemeResource`) with **no pinned providing dictionary** at parse time
(`targetDictionary: null`), deferring the pin to a **load-time visual-tree walk**. For content reparented into a
popup, that walk (`DependencyObjectStore.GetResourceDictionaries`) follows `fe.Parent` and **dead-ends at the
`Popup`** — the opener's local dictionary is unreachable — so the providing dictionary is *never* pinned, and the
stale parse-time value (captured against `Themes.Active`) sticks.

WinUI never has this problem because it has **two** independent safety nets, both of which Uno currently lacks:

1. **A sticky, parse-captured providing dictionary** (`CThemeResource::m_pTargetDictionaryWeakRef`). The flyout
   content is parsed in the opener's *lexical* scope, so its `{ThemeResource}` pins the opener-local dictionary at
   parse; `RefreshValue(ownerTheme)` re-queries that same dictionary forever, regardless of reparenting. This is the
   **load-bearing** path for flyouts.
2. **A resolution walk that follows popups** (`GetParentFollowPopups`) for inline `<Popup>` and the override/alias
   walk.

## Important correction (do not "fix" this)

WinUI does **not** unify popup theme + resource scope through a single logical-parent chain to the opener, and it
does **not** parent a flyout's popup to its placement target. It **splits the two concerns**:

- **Theme** is forwarded *explicitly at open time* (`FlyoutBase::ForwardThemeToPresenter` + `CPopupRoot` pushing the
  app theme to parentless popups; `PopupRoot`'s own theme stays `None`). Uno's `FlyoutBase.ForwardThemeToPresenter`
  is faithful to this and **stays** — theme forwarding is not the bug (the failing tests have a correct `ActualTheme`).
- **Resource scope** is the parse-pinned providing dictionary (#1 above).

So the fix is **not** to re-parent popups; it is to port WinUI's providing-dictionary capture.

## The fix in one paragraph

Port WinUI's `CThemeResource` providing-dictionary model: **capture and pin the providing `ResourceDictionary` at the
point of resolution** — at parse time in `ResourceResolver.ApplyResource` (using the providing-dictionary lookup and
the lexical/ambient scope) and at deferred/template resolution (replaying the parse scope) — following WinUI's
`GetDictionaryForThemeReference` rules (local→pin local dict; global-theme→pin the theme-resources root; app→pin app
resources; system-colors→pin system). Keep the pin **sticky across reparenting**, so `ThemeResourceReference.
RefreshValue(ownerTheme)` re-queries it from anywhere (this alone turns every failing popup repro green). Then align
the rest of the resolution layer for full fidelity: add the `GetParentFollowPopups` hop to the resource and
inheritance parent walks, match `UpdateThemeReference`'s active-walk-vs-refresh decision (incl. the
`IsValueFromInitialTheme` guard), verify the `ThemeWalkResourceCache`, foreground-freeze, high-contrast composition,
and non-FE-DO paths, and keep theme-forwarding as-is.

## Documents (read in this order)

1. **[`architecture.md`](./architecture.md)** — root-cause analysis with the WinUI `CThemeResource` model
   (C++ `file:line`), the Uno-vs-WinUI discrepancy table (R1–R9), and the target design.
2. **[`plan.md`](./plan.md)** — the phased implementation (Phase 0 baseline/oracle through Phase 5 validation), each
   phase self-contained with WinUI refs, Uno `file:line`, steps, and acceptance criteria.
3. **[`tests.md`](./tests.md)** — the validated Skia failure baseline, the regression suite, the WinUI-oracle
   protocol, and the platform/parity matrix.

## Definition of done

- All five failing popup repros + the existing theming suite green on Skia Desktop **and** WASM via `/runtime-tests`.
- Every WinUI-runnable repro confirmed **green on native WinUI** via `/winui-runtime-tests` (the oracle) before it is
  used to judge Uno.
- Every `{ThemeResource}` reference carries a pinned providing dictionary after first resolution; the pin is sticky
  across unload/reload and reparenting (popup/flyout/tooltip).
- `GetParentFollowPopups` hop present in the resource-dictionary and inheritance-parent walks; inline `<Popup>`
  resolves its declaration-site resources through the live tree.
- Theme forwarding for popups left intact and verified to match WinUI's effective-theme result.
- The malformed `Given_ThemeResource` repro fixed; the OS-theme-dependent "pass-by-luck" repro made deterministic.
- No regression on the `theming-winui-alignment` invariant: resolution remains a pure function of
  (key, owner effective theme, providing dictionary); no process-global requested-theme stack reintroduced.
