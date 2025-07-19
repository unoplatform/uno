---
uid: Uno.Contributing.ListViewBase
---

# ListViewBase internals for contributors

This document describes the internal operations of Uno's `ListViewBase` implementation(s) in detail, aimed at contributors.

Before reading it, you should first read the documentation of [ListViewBase aimed at Uno app developers](xref:Uno.Controls.ListViewBase), which covers the high-level differences between Uno's implementation and UWP's implementation.

## Introduction

`ListViewBase` is the base class of `ListView` and `GridView`. The remainder of the article will refer to 'ListView', the more commonly used of the two derived controls, for ease of reading, but most of the information is applicable to `GridView` as well since a large part of the implementation is shared.

`ListView` is a specialized type of [`ItemsControl`](https://learn.microsoft.com/uwp/api/windows.ui.xaml.controls.itemscontrol) designed for showing large numbers of items. `ListView` is by default *virtualized*, meaning that it only materializes view containers for those items which are visible or about to be visible within the scroll viewport. When items disappear from view, their containers are *recycled* and reused for newly appearing views. Correctly-functioning virtualization is the key to good scroll performance.

Other important features of `ListView`:

- selection, including multiple selection
- support for 'observable' collections, allowing items to be inserted and deleted without completely resetting the state of the list, and (on some platforms) with an animation of the item being added or removed
- item groups (with optional 'sticky' group headers)
- drag and drop to reorder items in the list

## Platform-specific implementations of `ListView`

On Android and iOS, `ListView` uses a platform-specific implementation that maps the XAML API to an inner instance of the native list control on each platform, being [RecyclerView](https://developer.android.com/reference/androidx/recyclerview/widget/RecyclerView) and [UICollectionView](https://developer.apple.com/documentation/uikit/uicollectionview) respectively. Using the native list control brings the advantage of getting advanced features like item animations 'for free', along with the disadvantage of added maintenance burden, the possibility of platform-specific differences in behavior, and the additional complexity of having non-`FrameworkElement` views in the visual tree.

Other platforms (WebAssembly, Skia, and macOS at the time of writing) use a purely managed implementation of `ListView`. This implementation is closer to WinUI in its comportment, for example, it actually uses the items panel (`ItemsStackPanel`) to host items. However, it's not a direct port of the WinUI control.

The managed ListView implementation is newer and lacks some features that are supported by the Android and iOS ListViews (and of course WinUI); the feature gap is tracked by [this issue](https://github.com/unoplatform/uno/issues/234).

### Jargon

`ListView` can scroll either vertically or horizontally, and the layouting logic is written as much as possible to reuse the same code for both orientations. Accordingly, certain terms are used throughout the code to avoid using orientation-specific terms like 'width' and 'height'. (These usages are probably unique to the Uno codebase.) The main terms are the following:

- Extent: Size along the dimension parallel to scrolling. The equivalent of 'Height' if scrolling is vertical, or 'Width' otherwise.
- Breadth: Size along the dimension orthogonal to scrolling. The equivalent of 'Width' if scrolling is vertical, or 'Height' otherwise.
- Start: The edge of the element nearest to the top of the content panel, ie 'Top' or 'Left' depending whether scrolling is vertical or horizontal.
- End: The edge of the element nearest to the bottom of the content panel, ie 'Bottom' or 'Right' depending whether scrolling is vertical or horizontal.
- Leading: When scrolling, the edge that is coming into view. ie, if the scrolling forward in a vertical orientation, the bottom edge.
- Trailing: When scrolling, the edge that is disappearing from view.

### Android and iOS ListViews in detail

Although the Android and iOS implementations are quite different, they share some high-level similarities. Both platforms expose a `NativeListViewBase` class, which inherits from the native list view for the platform.

Architecturally, the Android and iOS implementations share a similar high-level 'division of labor', reflecting the underlying platform API. Aside from the view type itself, both implementations implement a class, `VirtualizingPanelLayout`, whose responsibility is to determine what items are visible, what size they should take, and how they should be positioned within the list. Additionally, both Android and iOS implement an 'adapter' or 'source' class with the responsibility of materializing item containers for a given list index and binding them to the appropriate item from the items source.

(As an aside, this division of labor has no equivalent in `ListView`, but a somewhat similar approach is taken by WinUI's newer [`ItemsRepeater`](https://learn.microsoft.com/windows/winui/api/microsoft.ui.xaml.controls.itemsrepeater) control, also available in Uno.)

[This diagram](xref:Uno.Controls.ListViewBase#difference-in-the-visual-tree) shows how the `NativeListViewBase` view is incorporated into the visual tree, and the resulting difference from UWP. The key differences are:

- the scrolling container is the `NativeListViewBase` itself, not the `ScrollViewer`. Thus, the `ItemsPresenter` is **outside** the scrollable region. Additionally, there's no ScrollContentPresenter; instead, there's a ListViewBaseScrollContentPresenter. (It was implemented this way back when ScrollContentPresenter inherited directly from the native scroll container.)
- the `ItemsStackPanel` (or `ItemsWrapGrid`) is not actually present in the visual tree. These items' panels are created, and their configured values (eg, `Orientation`) are used to set the behavior of the list, but they are not actually loaded into the visual hierarchy or measured and arranged. They just act as a facade for the native layouter.
- the Header and Footer, if present, are managed by the native list on Android and iOS, whereas on WinUI, they're outside the ItemsStackPanel/ItemsWrapGrid.

Much of the time, these are implementation details that are invisible to the end user. In certain cases, they can have a visible impact. They're useful to be aware of when working on `ListView` bugs.

#### Android

`NativeListViewBase` implements Android's [`RecyclerView`](https://developer.android.com/reference/androidx/recyclerview/widget/RecyclerView). The available customization points of `RecyclerView` are heavily utilized to support all the features exposed by the XAML `ListView` contract.

The most important class is `VirtualizingPanelLayout`, which inherits from `RecyclerView.LayoutManager`, and is responsible for determining which views to create for a given scroll position, and where they should go.

The 'life cycle' of view creation and positioning of the `ListView` largely takes place during the measure and arrange phases, and when scrolling. A summary follows:

##### Layouting 'life cycle' summary

1. List is measured.
    1. Android framework calls `VirtualizingPanelLayout.OnMeasure()`, which calls `VirtualizingPanelLayout.UpdateLayout()`.
    1. `UpdateLayout()` => `ScrapLayout()`. `ScrapLayout()` performs a 'lightweight detach operation' on all views and adds them to the `Recycler's` 'scrap'. This allows the size and position of views to be recalculated if need be, but is very cheap (compared to removing and re-adding views in the normal Android way, which kills performance if done frequently).
    1. `UpdateLayout()` => `FillLayout()`. `FillLayout()` takes a direction, either Forward or Backward, and fills in unmaterialized items in that direction. The next item to add is always determined *relative to existing materialized items*, and it is added *if there is available viewport space in the designated direction*. To take a concrete example: the viewport is 320 pixels high; individual item containers are 80 pixels high; the list is currently scrolled to an offset of 100 pixels. Item containers for positions 0, 1, 2, 3 are currently materialized; their bounds in y are (-100, -20), (-20, 60), (60, 140), and (140, 220), relative to the viewport. `FillLayout()` would determine that the next unmaterialized item is 4. It would also see that `220 < 320`, and therefore space is available to add the item - this occurs within `TryCreateLine()`. Item 4 will be added at (220, 300). The list would then try to add item 5 at (300, 380), and succeed because `300 < 320`. It will then try to add item 6, and fail, because `380 > 320` (that is, item 6 is still entirely out of the viewport), at which point the loop terminates.
    1. `UpdateLayout()` => `UnfillLayout()`. `UnfillLayout()` takes a direction, and trims materialized item containers that are not visible starting from the **opposite** direction. So to take the example above: `UnfillLayout()` would start with item 0, and see that it lies entirely outside the viewport (`-20 < 0`), so it would dematerialize it, returning it to the recycler to be reused. It would then consider item 1, see that it is partially visible (`60 > 0`), and terminate at that point. `UnfillLayout()` is particularly important during scrolling (see below).
2. List is arranged.
    1. Android framework => `VirtualizingPanelLayout.OnLayoutChildren()` => `VirtualizingPanelLayout.UpdateLayout()`.
    2. `UpdateLayout()` does *not* call `ScrapLayout()` from within the arrange pass. This is because item dimensions should not have changed since the measure. It does however call `FillLayout()` and `UnfillLayout()` again. This is because the dimensions available to the list itself *might* be different from the ones it was measured with.
3. List is scrolled.
    1. Android framework => `VirtualizingPanelLayout.ScrollVerticallyBy()` (or `ScrollHorizontallyBy()`).
    1. `ScrollVerticallyBy()` => `ScrollBy()`.
    1. `ScrollBy()` => `ScrollByInner()`. `ScrollByInner()` essentially moves the viewport 'window' according to the supplied offset, and calls `FillLayout()` and `UnfillLayout()` to add the views visible within that window and remove the ones not visible within it. For large scrolls, `ScrollBy()` will `ScrollByInner()` multiple times with increasingly larger offsets: the purpose of this is to have `FillLayout()` not add multiple views in a single call, because that uses the pool of recycled views inefficiently and will cause new views to be created unnecessarily, which in turn degrades performance.

        At some point the end of the items will be reached, meaning for large requested scrolls, it may not actually be possible to scroll as far as requested. Consequently, `ScrollBy()` returns the actual offset that was possible to scroll.
    1. `ScrollBy()` => `OffsetChildrenVertical()` (or `OffsetChildrenHorizontal()`). The base native method is what actually adjusts the offset of the children relative to the `NativeListViewBase`, which produces the impression that they're being scrolled. Uno-side state which depends on the scroll offset is also updated here.

#### iOS

On iOS, `NativeListViewBase` inherits from the `UICollectionView` native control. `UICollectionView` is special in that it expects to be given dimensions and positions for the item containers in the list **before** those containers have been materialized. This poses a challenge, because if the individual containers have not been materialized, then they have not been data-bound, and depending on the specific item template, the bound data may change the measured size. (Eg, for data-bound text wrapped to multiple lines, panel elements `Visibility={Binding SomeVMProperty}` ... etc.)

The measuring logic for iOS' `ListView` makes an initial guess for the size of each item based on the dimensions of the unbound `ItemTemplate`. Then, once the container for the item is actually materialized and bound, `UICollectionView` offers a chance to update the measured dimensions for the item, via the method `UICollectionViewCell.PreferredLayoutAttributesFittingAttributes()`. This method is implemented by the `ListViewBaseInternalContainer` derived type. This approach to supporting dynamic item container sizes works for the most part, but can be brittle and throws up a number of edge cases.

##### Layouting 'life cycle' summary

1. Upon measure, `VirtualizingPanelLayout.SizeThatFits()` is called, and in turn calls `PrepareLayoutIfNeeded()`. (`SizeThatFits()` is called from Uno's layouting code. `SizeThatFits()` is an overridden virtual method that's defined in UIKit, but it's mostly not used by UIKit itself.) `PrepareLayoutIfNeeded()` determines the `DirtyState` of the list. A `DirtyState` of `None` indicates that the existing internal layout state is still valid. `NeedsRelayout` indicates that the layout should be completely rebuilt, which may be the case if the available size changed, or if `InvalidateLayout()` was called. Other `DirtyState` values indicate more specific reasons for updating the layout state.
2. If `PrepareLayoutIfNeeded()` determines that a layout rebuild is required, it calls `PrepareLayoutInternal()`. `PrepareLayoutInternal()` calculates the sizes and positions of every item in the list, as well as non-item elements like header, footer, and group headers. Note that during measure, these sizes and values are not actually persisted (governed by the `createLayoutInfo` parameter), just used to calculate the total dimensions of the list contents.
3. On arrange, `UICollectionView` calls the `VirtualizingPanelLayout.PrepareLayout()` method, which also calls `PrepareLayoutIfNeeded()`=>`PrepareLayoutInternal()`. This time, `PrepareLayoutInternal()` will persist the calculated sizes and offsets of the items, as `UICollectionViewLayoutAttributes` objects.
4. `UICollectionView` calls `VirtualizingPanelLayout.LayoutAttributesForElementsInRect()` with the current viewport bounds, to determine what elements should be shown. `LayoutAttributesForElementsInRect()` checks the cached layout attributes, and returns all those that intersect with the passed viewport bounds.
5. For each element returned by `LayoutAttributesForElementsInRect()`, `UICollectionView` materializes a container by calling `ListViewBaseSource.GetCell()`. `GetCell()` returns a `ListViewBaseInternalContainer`, which has a `ListViewItem` (or a `ContentControl` in the case of a header, footer, or group header element) in its visual subtree.
6. For each container, `UICollectionView` calls `ListViewBaseInternalContainer.PreferredLayoutAttributesFittingAttributes()`. Here we are able to actually measure the data-bound item, and determine the final size it needs. If the size differs from the un-data-bound size, we:
    1. Return updated layout attributes from the method;
    2. Update the cached layout attributes for that item, for future use;
    3. Update the positions of subsequent items in the cached layout info, since if an item is larger/smaller than initially estimated, the offsets of the remaining items must be modified accordingly.
7. Whenever the list is scrolled, `UICollectionView` will call `LayoutAttributesForElementsInRect()` with the new visible viewport. Steps 5. and 6. will occur for each newly-visible item.

### Android and iOS internal classes

| Uno class | Android base class | iOS base class | Description |
| --- | --- | --- | --- |
| NativeListViewBase | AndroidX.RecyclerView.Widget.RecyclerView | UIKit.UICollectionView | Native list view, parent of item views. |
| VirtualizingPanelLayout† | RecyclerView.LayoutManager | UIKit.UICollectionViewLayout | Tells NativeListViewBase how to lay out its items. Bridge for ItemsStackPanel/ItemsWrapGrid. |
| NativeListViewBaseAdapter(Android), ListViewBaseSource(iOS) | RecyclerView.Adapter | UIKit.UICollectionViewSource | Handles creation and binding of item views.
| ListViewBaseInternalContainer | - | UICollectionViewCell | Implements the native container type - one is created for every `ListViewItem`/`GridViewItem` |
| ScrollingViewCache | RecyclerView.ViewCacheExtension | - | Additional virtualization handling on Android which optimizes scroll performance. |

† `VirtualizingPanelLayout` is the base class of `ItemsStackPanelLayout` and `ItemsWrapGridLayout`. The derived classes implement the item positioning logic for the corresponding items panel.

### Managed ListView in detail

On WASM, Skia, and macOS, `ListViewBase` uses a shared implementation that's dubbed 'managed' because it doesn't rely upon an external native control. The visible implementation details of the managed `ListView` are much closer to WinUI. Specifically:

- the items panel is a 'real' panel which hosts the ListViewItems as its children. The size of the panel reflects the estimated total size based on the number of items, as determined by the list.
- the `ScrollViewer` in the ListView's control template is a 'real' `ScrollViewer`, ie, it is in fact responsible for scrolling.

The internals of the managed `ListView` were originally implemented independently of the WinUI source, but have been gradually converging on the internals of WinUI.

Consistent with the other platforms, the managed `ListView` delegates layouting to a class called `VirtualizingPanelLayout`. Item container management is delegated to `VirtualizingPanelGenerator` (similar in responsibility to `NativeListViewBaseAdapter` (Android) and `ListViewBaseSource` (iOS)).

#### Layouting 'life cycle' summary

1. On measure, `ItemsStackPanel.MeasureOverride()` directly calls `VirtualizingPanelLayout.MeasureOverride()`.
2. `VirtualizingPanelLayout.MeasureOverride()` 'scraps' the existing layout. The 'scrap' concept is borrowed from Android: existing containers are returned to the item generator, but marked as able to be reused without rebinding during the current pass, if they are still needed (which will often be the case).
3. `VirtualizingPanelLayout.MeasureOverride()` => `UpdateLayout()`. `UpdateLayout()` calls `UnfillLayout()` and `FillLayout()`, which respectively dematerialize containers that are no longer visible within the current viewport and materialize containers for newly visible items.
4. `FillLayout()` calls `CreateLine()` for every missing 'line', which in turn calls `AddView()` for the view(s) in the line. `AddView()` measures the view, adds it to the panel, and stores its planned `Bounds` (size and offset) in `VirtualizationInformation` attached to the view.
5. `MeasureOverride()` returns an estimate of the panel size, based on the current extent of materialized items, the number of unmaterialized items, and the best guess of their size. (This estimate is potentially inaccurate, in the case that the unmaterialized items have a different data-bound size or use a template with a different size; this is a fundamental limitation of the `ListView` that's also present on WinUI.)
6. `ArrangeOverride()` calls `ArrangeElements()` which arranges each child view according to its stored Bounds, adjusted for the actual arranged size of the panel itself and also the parent `ScrollViewer`.
7. The panel listens to the `ViewChanged` event of its parent `ScrollViewer`. `VirtualizingPanelLayout.OnScrollChanged()` calls `UpdateLayout()`, in small increments to ensure that vanished views are recycled at the same rate as appearing views are materialized. `OnScrollChanged()` then calls `ArrangeElements()` to ensure newly-added views are arranged.
