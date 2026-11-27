<!-- winui-sync-run target=winui3/release/2.6.5 build=passed skipped=0 -->
**Sync to WinUI 2.6.5** (`65718e2813` → `c29edd64ae`)

## Upstream changes
- TabView.cpp: removed `velocity.h` include.
- TabView.cpp: `RequestCloseTab` uses `FocusManager::GetFocusedElement(XamlRoot())`.
- TabView.cpp: `RequestCloseTab` losing-focus handler returns early if the new focused element equals `args.NewFocusedElement()`.
- TabView.cpp: `MoveFocus` uses `GetFocusedElement(XamlRoot())`, and returns the real `Focus()` result instead of asserting inside `MUX_ASSERT`.
- TabView.xaml: removed `TabContainer.Padding` setters from the three selected visual states; `CloseIconStates` now sets padding with `TabViewItemHeaderPaddingWithCloseButton` / `TabViewItemHeaderPaddingWithoutCloseButton`.
- TabView_themeresources.xaml: added the two padding resources; `TabViewSelectedItemHeaderMargin` changed to `-1,0,-1,1`.
- TabView_perf2026.xaml and TabView.vcxitems: new perf2026 variant, plus its project registration.

## Ported
- TabView.cpp (GetFocusedElement with XamlRoot in RequestCloseTab) -> TabView.cs; already present.
- TabView.cpp (same-element no-op in OnLosingFocus) -> TabView.cs; already present.
- TabView.cpp (MoveFocus GetFocusedElement and focusResult) -> TabView.cs; already present.
- TabView.xaml (padding setters and CloseIconStates) -> Uno.UI.FluentTheme TabView.xaml; already present.
- TabView_themeresources.xaml (new padding resources, margin change) -> Uno.UI.FluentTheme TabView_themeresources.xaml; already present.

## Not applicable
- `velocity.h` include removal: C++ include, no equivalent in Uno.
- TabView_perf2026.xaml: perf2026 variant that Uno does not use.
- TabView.vcxitems: Windows project file.
- No test changes upstream in this range.

## Skipped
None
