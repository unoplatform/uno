<!-- winui-sync-run target=winui3/release/2.6.5 build=passed skipped=0 -->
**Sync to WinUI 2.6.5** (`2a60e27c59` → `c29edd64ae`)

## Upstream changes
- `BreadcrumbBar.cpp`: removed unused `itemsSourceView` local in `ReIndexVisibleElementsForAccessibility`.
- `BreadcrumbBar.cpp`: `MoveFocus` now calls `FocusManager::GetFocusedElement(XamlRoot())`.
- `BreadcrumbBar.xaml`: item Button gets `Foreground` TemplateBinding and reordered `CornerRadius`; inner template Grid background is `{TemplateBinding Background}`; `Normal` visual state emptied; PART_ContentPresenter gets `Foreground` TemplateBinding; PART_LastItemContentPresenter gets `Background` TemplateBinding.
- `BreadcrumbBar_perf2026.xaml` and `Breadcrumb.vcxitems`: new perf2026 style/theme resources.
- `InteractionTests/BreadcrumbBarTests.cs`: `[TestCleanup]` attribute, `AutomaticCleanup = false` options with `AllowCleanup()`, whitespace fixes.

## Ported
- `BreadcrumbBar.cpp` (itemsSourceView removal) -> `BreadcrumbBar.cs`: already present (no unused local).
- `BreadcrumbBar.cpp` (`GetFocusedElement(XamlRoot)`) -> `BreadcrumbBar.cs`: already present (uses `FocusManager.GetFocusedElement(xamlRoot)`).
- `BreadcrumbBar.xaml` -> `src/Uno.UI.FluentTheme/Resources/PriorityDefault/BreadcrumbBar.xaml`: all changes already present, verified by inspection.

## Not applicable
- `BreadcrumbBar_perf2026.xaml` and the `.vcxitems` perf2026 entries: Uno does not use `*_perf2026*` variants; the vcxitems is a project file.
- `InteractionTests/BreadcrumbBarTests.cs`: Windows UI-automation interaction tests; Uno has no equivalent test infrastructure and no Uno tests exist for this area.

## Skipped
None
