---
uid: Uno.Controls.ListViewBase
---

# ListViewBase in Uno Platform

Uno Platform's `ListViewBase` is a port of the WinUI control, so `ListView` and `GridView` share their styles, templates and behavior with WinUI apps. This document covers what's still useful to know when working with them, and where Uno Platform differs from Windows.

For contributors, see in-depth documentation on [the internals of ListView](../uno-development/listviewbase-internals.md).

## Style reuse

`ListView` and `GridView` use WinUI's default styles unchanged, so custom styles and templates written for WinUI work as-is. Their default items panels are `ItemsStackPanel` and `ItemsWrapGrid`, as on Windows.

## Performance tips

### Observable collections

If you use a collection type like an array or a `List<T>` as an `ItemsSource`, then whenever you change the `ItemsSource`, all the item views will be removed and recreated. Often it's preferable to support incremental changes to your source collection.

`ListViewBase` implicitly supports incremental collection changes whenever the ItemsSource is a collection which implements the `INotifyCollectionChanged` interface. The `ObservableCollection<T>` class in the standard library is one such collection. Whenever an item is added, removed, or replaced in the collection, the corresponding `CollectionChanged` event is raised. `ListViewBase` listens to this event and only visually modifies the items that have actually changed, animating them with the transitions of the items panel. The list also maintains the scroll position if possible and maintains the current selected item (unless it's removed).

Using `ObservableCollection` (or another `INotifyCollectionChanged` implementation) has performance benefits in certain situations:

1. When only modifying one or two items.
2. When modifying items out of view.
3. When fetching 'new' data that hasn't actually changed (e.g. auto refresh).

It also has UX benefits in certain situations:

1. When only adding/removing a couple of items (esp. in response to user action).
2. When modifying the items source shouldn't affect the scroll position.
3. When modifying the items source shouldn't change the user's selection.
4. When item changes should be visually highlighted.

A good use case might be, for instance, a list of reminders that the user can remove by swiping them out of view.

Note that using an observable collection adds complexity and there are some cases when it can even be anti-performant. For example, consider a list of items that can be filtered by a search term. Modifying the search term by one character (i.e., when the user types in a `TextBox`) might remove or add hundreds of items from the filtered source, each of them processed as a separate change. Changing the `ItemsSource` completely can be much cheaper.

So consider using a non-observable collection, or use the Uno-only `RefreshOnCollectionChanged` flag (which causes the list to refresh when any `CollectionChanged` event is raised), if your scenario matches the following:

1. Large numbers of items may change at once, and
2. Changes are not particularly 'meaningful' to the user.

## Differences from WinUI

The visual tree of a `ListView` is the same as on WinUI: `ScrollViewer`, `ScrollContentPresenter`, `ItemsPresenter`, then the header, the items panel with its containers, and the footer.

* **`CarouselPanel`** derives from the same panel as `ItemsStackPanel`, and doesn't support carouseling.
* **Item transitions** run on the element itself rather than on a separate layout transition element, so they can't draw outside the bounds of the panel, and grid items changing column get a straight move instead of a crossfade.
* **`ScrollViewer.ViewChanging`** isn't raised.
