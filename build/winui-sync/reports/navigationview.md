<!-- winui-sync-run target=winui3/release/2.6.5 build=passed skipped=0 -->
**Sync to WinUI 2.6.5** (`bac7a9c33f` → `c29edd64ae`)

## Upstream changes
- NavigationView.cpp: servicing fix 62995866 in UpdatePaneLayout (use `menuItemsActualHeight` in the half-height check; the "couldn't determine menuItems" branch only runs if `totalAvailableHeight >= footerItemsRepeater.ActualHeight`, avoids negative MaxHeight).
- NavigationView.cpp: local `c_infSize` static replacing `LayoutUtils::c_infSize`; `for (const auto&` -> `for (auto` in OnRepeaterGettingFocus; Containment.h include; BOM added.
- NavigationViewHelper.h: `inline constexpr` -> `static constexpr` for the visual-state name constants.
- NavigationViewItem.cpp: servicing fix 63007686 — the queued deferred flyout show re-validates IsExpanded / ShouldRepeaterShowInFlyout / root grid / attached flyout before showing (plus telemetry when skipped).
- NavigationView_ApiTests/NavigationViewTests.cs: VerifyVisualTree split into `_OptimizedStyles` and `_OldStyles` variants with file-name suffixes.
- NavigationView_InteractionTests (CommonTests, EventTests, FocusBehaviorTests, NavigationViewTestsBase, PaneBehaviorTests, TopModeTests): test-infrastructure refactors.

## Ported
- NavigationView.cpp (UpdatePaneLayout fix 62995866) -> NavigationView.cs (the change is ported as enabled, since Uno has no Containment switch)
- NavigationViewItem.cpp (deferred flyout re-validation, 63007686) -> NavigationViewItem.cs (Skia path that queues the callback; the non-Skia synchronous path is unchanged)
- MUX Reference headers updated to `tag winui3/release/2.6.5` in NavigationView.cs, NavigationViewItem.cs, NavigationViewHelper.cs

## Not applicable
- `LayoutUtils::c_infSize` -> local `c_infSize` and `const auto&` -> `auto` in the loop (NavigationView.cpp): C++ build/lifetime refactors; Uno's `LayoutUtils.c_infSize` has the same value.
- Containment.h include, WINAPPSDK_CHANGEID defines and the telemetry TraceLogging block: Windows-only servicing/telemetry plumbing.
- NavigationViewHelper.h `inline` -> `static` constexpr: C++ linkage only; C# constants unchanged.
- NavigationView_ApiTests VerifyVisualTree split: Uno's port has this test commented out (no VisualTreeVerifier infrastructure).
- NavigationView_InteractionTests/*: UI automation (TAEF) tests with no Uno counterpart; the existing Uno tests don't port them.

## Skipped
None
