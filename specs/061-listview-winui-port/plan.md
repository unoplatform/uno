# ListView/GridView: full WinUI port

Replace Uno's `VirtualizingPanelLayout`-based ListView/GridView with a port of the WinUI C++ implementation
(`ModernCollectionBasePanel`, `ItemsStackPanel`, `ItemsWrapGrid`, layout strategies, `ListViewBase`, and the hosts they rely on).

- **Branch:** `dev/mazi/pending/listviewrevamp`, stacked on `dev/mazi/listviewitempresenter` (PR #25389).
- **WinUI reference:** tag `winui3/release/2.5.1`, commit `ba3a8d59e6d`. Every ported file carries
  `// MUX Reference <File>, tag winui3/release/2.5.1, commit ba3a8d59e6d`.
- **Boundaries:** `CalendarPanel` keeps its inline panel copy and only shares types. The `SemanticZoom` control and
  `ConnectedAnimationService` are out of scope: `ListViewBase` ports their code paths, inert behind `TODO Uno` markers.
  Item transitions and the garbage section are ported and use Uno's existing `Transition` classes where they work.

## Phases

| # | Scope | C++ sources (under `dxaml/xcp/dxaml/lib` unless noted) |
|---|---|---|
| 1 | Contracts, `LayoutStrategyBase`, stacking and wrapping strategies with unit tests | `components/moco`, `StackingLayoutStrategy_Partial`, `WrappingLayoutStrategy_Partial` |
| 2 | Panel core | `ModernCollectionBasePanel*`, `ItemsStackPanel_Partial`, `ItemsWrapGrid_Partial` |
| 3 | Host integration | `ItemsControl_Partial`, `Selector_Partial`, `ItemsPresenter*`, `GroupItem_Partial`, `ListViewBaseHeaderItem_Partial`, `CollectionViewManager` |
| 4 | `ListViewBase` | `ListViewBase_Partial*` |
| 5 | Cut-over: move ComboBox, FlipView, TreeView, TabView, automation peers; delete `VirtualizingPanelLayout` and friends | |
| 6 | Port WinUI tests and helpers; run the full suite | `dxaml/test/native/external/...` |

Until phase 5 the new panel compiles next to the old layouter and nothing routes to it, so the build and existing tests stay green.

## File layout

One C++ file maps to one C# file, with members in source order.

| C++ | C# |
|---|---|
| `ModernCollectionBasePanel_Partial.h` | `Primitives/ModernCollectionBasePanel/ModernCollectionBasePanel.partial.h.mux.cs` |
| `ModernCollectionBasePanel_Partial.cpp` | `.../ModernCollectionBasePanel.partial.mux.cs` |
| `ModernCollectionBasePanel_<Part>[_Partial].cpp` | `.../ModernCollectionBasePanel_<Part>.partial.mux.cs` |
| `StackingLayoutStrategyImpl.{h,cpp}` | `ItemsStackPanel/StackingLayoutStrategyImpl.{h.mux.cs,mux.cs}` |
| `WrappingLayoutStrategyImpl.{h,cpp}` | `ItemsWrapGrid/WrappingLayoutStrategyImpl.{h.mux.cs,mux.cs}` |
| `ListViewBase_Partial<Part>.cpp` | `ListViewBase/ListViewBase_<Part>.partial.mux.cs` |

Paths are relative to `src/Uno.UI/UI/Xaml/Controls/`. Internal interfaces in the C++ `DirectUI` namespace live in `src/Uno.UI/DirectUI/`, namespace `DirectUI`.

## Porting conventions

These keep independently ported files call-compatible. Follow them mechanically.

**Signatures**
- `HRESULT Foo(args, _Out_ T* pResult)` with exactly one out parameter becomes `T Foo(args)`.
- Two or more out parameters: `void Foo(args, out A a, out B b)`, same order and C++ names without the `p`/`pp` type prefix being required.
- `_Out_opt_` is a required `out`; callers discard with `out _`. `_Inout_` is `ref`.
- `HRESULT` with no out parameter becomes `void`. `IFC(...)`, `IFCPTR`, `RRETURN`, `ctl::` wrappers go away; failures throw.
- A `Cleanup:` label becomes `try`/`finally` when it holds real cleanup, otherwise it goes away.
- Method names stay verbatim (`GetElementBounds`, not `ElementBounds`), with the `Impl` suffix removed from C++ methods implementing an interface or IDL member.
- `get_X()`/`put_X()` on a C# property become property access. For Uno properties that don't exist yet, add an `internal` property with the WinUI name.

**Types**
- `wf::Rect/Size/Point` are `Windows.Foundation.Rect/Size/Point` (double fields). Keep C++ `float` locals and fields as `float`, cast at the boundary as `LayoutStrategyBase` does.
- `FLOAT`/`DOUBLE`/`INT`/`UINT`/`BOOLEAN` map to `float`/`double`/`int`/`uint`/`bool`.
- `ctl::ComPtr<T>`, `TrackerPtr<T>` become `T`. `ctl::WeakRefPtr` becomes `WeakReference<T>?`. `std::vector<T>` becomes `List<T>`. `std::deque<T>` becomes `Deque<T>` where one is needed.
- `IInspectable` is `object`. `IVector<IInspectable*>` is `IList<object>`. `xaml_controls::X` is `Microsoft.UI.Xaml.Controls.X`.
- Value semantics matter: a C++ struct copied by value stays a C# `struct`. A nested class stays a nested class.

**Behaviour**
- Never simplify or drop logic. Anything that cannot run in Uno stays in place inside `#if !HAS_UNO` with a one-line `// TODO Uno: <reason>`.
- Keep upstream comments verbatim. Don't add new narration.
- DManip paths: `ScrollViewer.IsInDirectManipulation` exists. Anything that needs a DManip secondary-content curve takes the existing "manually update" branch in C++ on Uno, always.
- `IScrollInfo`: Uno has no `ItemsPresenter` scroll info. `IPaginatedPanel` takes a `ScrollViewer` directly.
- Accessibility: `internal` for anything not in the WinUI IDL. `ModernCollectionBasePanel` is `public` only because `ItemsStackPanel` derives from it, and it stays hidden (`EditorBrowsable(Never)`, `private protected` constructor).
- Style: tabs, Allman braces, `#nullable enable` per file, file-scoped namespaces, MIT header and MUX Reference line, no `_Mux` suffix.
- Events: never `event Action`. Use `EventHandler`/`TypedEventHandler`.
- Generated stubs in `src/Uno.UI/Generated/` are only touched for `#if`/`[NotImplemented]`.

## Status

Tracked in commit history. Open items and deliberate gaps are listed here as they come up.

- `CarouselPanel` derives from `ModernCollectionBasePanel` with the stacking strategy (`CacheLength` 0.5) and does not carousel. WinUI's `OrientedVirtualizingPanel`/`CarouselPanel` need `IScrollInfo` and a working `ItemContainerGenerator`. `ComboBox` centers the selected item itself when it opens.
- `VirtualizingStackPanel` and `WrapGrid` stay unimplemented. `FlipView` keeps its `StackPanel`.
- `ItemsPresenter.OnApplyTemplate` (allowed panel types, `CarouselPanel` for `ComboBox` only) stays compiled out, so Uno does not reject a misplaced panel.
