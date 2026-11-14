<!-- winui-sync-run target=winui3/release/2.6.5 build=passed skipped=0 -->
**Sync to WinUI 2.6.5** (`7198c4e690` → `c29edd64ae`)

## Upstream changes
- RadioButton_Partial.cpp/.h: `OnGroupNamePropertyChanged` now calls `UpdateRadioButtonGroup()`.
- RadioButton_Partial.cpp/.h: new `EnterImpl` override that calls `UpdateRadioButtonGroup()` when entering live.
- RadioButton_Partial.cpp: `UpdateRadioButtonGroup` returns early if the button is unchecked or has no group parent. It also computes the parent once up front and makes sure the parent's DXaml peer exists.
- RadioButton_Partial.cpp/.h: `GetParentForGroup` now walks the core visual tree and returns a `CDependencyObject`. `FocusNextElementInGroup` was adapted to match.
- RadioButton_Partial.cpp: `SetFocusedElement` result is checked with `IFC_RETURN`.
- Annotation-only changes (`_In_` on `EnterImpl`/`LeaveImpl` params, `MeasureOverride` `_In_` removal, `final` -> `override` on `ButtonBase::EnterImpl`, trailing newline): Button.cpp, Button.h, ButtonBase_Partial.h, HyperLinkButton_Partial.cpp/.h, RadioButton `UnregisterFromGroup`.
- No changes: ButtonBaseKeyProcess.h, Button_Partial.*, all other AutomationPeer files, ButtonBase_Partial.cpp, CheckBox*, RepeatButton*, ToggleButton*.

## Ported
- RadioButton_Partial.cpp (`OnGroupNamePropertyChanged`) -> `RadioButton.mux.cs`: added `UpdateRadioButtonGroup()` call.
- RadioButton_Partial.cpp/.h (`EnterImpl`) -> `RadioButton.mux.cs`: added a `private protected override void EnterImpl(bool live)` that calls `UpdateRadioButtonGroup()` when live.
- RadioButton_Partial.cpp (`UpdateRadioButtonGroup`) -> `RadioButton.mux.cs`: early return when unchecked, early return when there is no parent, parent computed once up front.
- `RadioButton.mux.cs` had no MUX header, so I added one with tag winui3/release/2.6.5.

## Not applicable
- Button.cpp / Button.h `_In_` SAL annotations: C++ annotations only.
- ButtonBase_Partial.h `final` -> `override`: C++ virtual specifier, nothing to port.
- HyperLinkButton `MeasureOverride` `_In_` removal and trailing newline: C++ annotation/whitespace.
- `UnregisterFromGroup` `_In_` annotation: C++ annotation.
- `GetParentForGroup` core-side visual-relative walk: the Uno version already uses `Parent` / `VisualTree.GetRootForElement`, which has no peer-resurrection problem.
- DXaml peer creation block in `UpdateRadioButtonGroup`: Uno has no separate peer for the core object.
- `IFC_RETURN(result.GetHResult())` after `SetFocusedElement`: HRESULT plumbing.

## Skipped
- None
