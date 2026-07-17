# Finding: Icon-only Button UIA name — WinUI 3 reference behavior validated

**Date**: 2026-07-17
**Branch**: `dev/doti/findings-iconbutton-uia-name`
**How found**: Reported downstream on a customer WebAssembly (Skia) app — a `Button` whose content is a
`PathIcon` and which carries `AutomationProperties.Name` is silent in NVDA on WASM, while an adjacent
text-content button announces fine. The same app works on Skia Desktop. Per Constitution VII, the WinUI 3
reference behavior was validated first (source trace + native runtime run) before touching Uno code.

> **Evidence level**: WinUI source trace (`microsoft-ui-xaml`, dxaml) **+ runtime-validated on native
> WinUI** (WinAppSDK SamplesApp via `/winui-runtime-tests`, 4/4 new tests passing).

## WinUI 3 behavior (runtime-validated)

| Scenario | UIA Name on WinUI 3 |
|---|---|
| `Button` + `PathIcon` content + `AutomationProperties.Name="Refresh"` | **"Refresh"** |
| `Button` + `PathIcon` content, no `AutomationProperties.Name` | **empty** (unnamed button) |
| `Button` + string content `"Help Center"` | **"Help Center"** |
| `PathIcon` element itself | **no automation peer**; the icon-only button is a leaf in the UIA tree |

## Name-resolution order (source trace)

For a `Button` (peer chain `ButtonAutomationPeer` → `ButtonBaseAutomationPeer` →
`FrameworkElementAutomationPeer`), the UIA Name resolves in this exact order:

1. **`AutomationProperties.Name`** attached property —
   `dxaml/xcp/dxaml/lib/FrameworkElementAutomationPeer_partial.cpp:449`
2. **`LabeledBy`** target's name — same file, `:455-461`
3. **`FrameworkElement::GetPlainText()`** — `:469`. For a `ContentControl` this delegates to the
   `Content` object (`ContentControl_Partial.cpp:309-322` → `GetStringFromObject`,
   `FrameworkElement_partial.cpp:382-433`): a string yields its text; a `PathIcon` (any `IconElement`)
   yields `""` because it never overrides the base `FrameworkElement::GetPlainText`
   (`FrameworkElement_partial.cpp:372-380`).
4. **`ButtonBase` content-unbox fallback** — `ButtonBaseAutomationPeer_Partial.cpp:59-81`: unboxes
   `Content` as a string (`IValueBoxer::UnboxValue`); non-string content (icons) yields `NULL`.

Key structural facts:

- `PathIcon`/`IconElement`/`Path`/`Shape` **never create automation peers** — no
  `OnCreateAutomationPeer` override anywhere in that chain; the default
  `CUIElement::OnCreateAutomationPeerImpl()` returns `NULL` (`uielement.cpp:4913-4917`). The peer tree
  walk skips them (`FrameworkElementAutomationPeer_partial.cpp:259-300`), so an icon-only button has no
  UIA children.
- WinUI does **not** harvest template-descendant text for a plain `Button`. The
  `GetTextBlockText` descendant fallback exists but is only used by
  `FaceplateContentPresenterAutomationPeer` (AppBarButton) and `SelectorItem.GetPlainText`
  (ListViewItem/GridViewItem).

**Conclusion**: on WinUI, icon content is entirely irrelevant to naming — `AutomationProperties.Name`
on an icon-only button always wins and must be announced; without it, the button is legitimately
unnamed (which is why authored names are required on icon-only buttons).

## Uno parity backdrop (code review)

- Uno's peer-level chain matches WinUI's first three steps —
  `src/Uno.UI/UI/Xaml/Automation/Peers/FrameworkElementAutomationPeer.cs` `GetNameCore()`:
  `AutomationProperties.Name` → `LabeledBy` → `GetPlainText()`, then Uno-specific extras
  (`GetSimpleAccessibilityName`, Skia-only `GetAccessibilityInnerText`) that WinUI does not have. The
  extras only ever *add* names (descendant text), which explains why text-content buttons announce.
- The WinUI `ButtonBaseAutomationPeer.GetNameCore` content-unbox fallback is present but commented out
  in `src/Uno.UI/UI/Xaml/Automation/Peers/ButtonBaseAutomationPeer.cs` — benign for parity, since
  string content is already handled by `ContentControl.GetPlainText`
  (`ContentControl.mux.cs`).
- Uno icons create no peers either (no `OnCreateAutomationPeer` under
  `src/Uno.UI/UI/Xaml/Controls/Icons/`), matching WinUI.

## Implication for the WASM defect

Skia Desktop announces the name (UIA consumes the peer directly), so the peer contract is intact.
The WASM failure must therefore sit in the **semantic-DOM layer** — what
`SemanticElementFactory`/`WebAssemblyAccessibility`/`Accessibility.ts` emit (element type,
`aria-label`) for a button whose content is an icon rather than text. Next diagnostic step: DOM-level
inspection/tests of the emitted semantic element for an icon-only named button (extend
`Given_AccessibleButton` per the FR-016/FR-021 test substrate).

## Tests added (parity guards)

`src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_AutomationPeer.cs` — all four pass on
native WinUI (WinAppSDK SamplesApp, 2026-07-17); they run on all platforms and can now be executed on
Skia WASM/Desktop to expose any Uno-side gap:

- `When_Button_With_PathIcon_Content_And_AutomationName` — name = "Refresh"
- `When_Button_With_PathIcon_Content_Without_AutomationName` — name = empty
- `When_Button_With_String_Content_GetName` — name = "Help Center"
- `When_PathIcon_Has_No_AutomationPeer` — no peer for the icon; button peer has no children

## Cross-reference

Related to the WASM AOM work in [plan.md](./plan.md) (FR-016 test re-enablement, FR-021 attribute
parity). Downstream tracking of the customer report is handled privately; this document is the
source-of-truth for the expected WinUI behavior.
