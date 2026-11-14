<!-- winui-sync-run target=winui3/release/2.6.5 build=passed skipped=0 -->
**Sync to WinUI 2.6.5** (`b91b3ce6f2` → `c29edd64ae`)

## Upstream changes
- `SelectorBar_themeresources.xaml`: in the HighContrast dictionary, `SelectorBarItemForeground` and `SelectorBarItemForegroundSelected` now use `SystemControlForegroundBaseHighBrush`. Before, they used `SystemColorButtonTextColor` and `SystemColorHighlightTextColor`.
- `SelectorBar_perf2026.xaml`: new file, a perf2026 style variant (207 lines).
- `SelectorBar.vcxitems`: registers the new `SelectorBar_perf2026.xaml` page, and registers the theme resources a second time as `ThemeResourcesPerf2026`.

## Ported
- `SelectorBar_themeresources.xaml` -> `src/Uno.UI.FluentTheme/Resources/PriorityDefault/SelectorBar_themeresources.xaml`. The HighContrast dictionary (lines 46 and 48) already uses `SystemControlForegroundBaseHighBrush`, so no edit was needed. No C# files changed, so no MUX Reference headers were updated. No build was run because nothing was edited. The `build: passed` line above is therefore not a build result.

## Not applicable
- `SelectorBar_perf2026.xaml`: a perf2026 variant that Uno does not use.
- `SelectorBar.vcxitems`: a Windows C++ project file. It only registers the new perf2026 page and the second theme-resources entry.

## Skipped
None
