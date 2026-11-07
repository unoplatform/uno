---
uid: Uno.Contributing.ListViewBase
---

# ListViewBase internals for contributors

This document describes the internal operations of Uno Platform's `ListViewBase` implementation, aimed at contributors.

Before reading it, you should first read the documentation of [ListViewBase aimed at Uno Platform app developers](../controls/ListViewBase.md).

## Introduction

`ListViewBase` is the base class of `ListView` and `GridView`. The remainder of the article will refer to 'ListView', the more commonly used of the two derived controls, for ease of reading, but most of the information is applicable to `GridView` as well since a large part of the implementation is shared.

`ListView` is a specialized type of [`ItemsControl`](https://learn.microsoft.com/uwp/api/windows.ui.xaml.controls.itemscontrol) designed for showing large numbers of items. `ListView` is by default *virtualized*, meaning that it only materializes view containers for those items which are visible or about to be visible within the scroll viewport. When items disappear from view, their containers are *recycled* and reused for newly appearing views. Correctly-functioning virtualization is the key to good scroll performance.

Other important features of `ListView`:

- selection, including multiple selection
- support for 'observable' collections, allowing items to be inserted and deleted without completely resetting the state of the list, with an animation of the item being added or removed
- item groups (with optional 'sticky' group headers)
- drag and drop to reorder items in the list

## A port of the WinUI control

`ListViewBase` and its panels are a direct port of the WinUI C++ sources, with the same file structure and method names where possible. The ported files use the `.mux.cs` suffix and carry a `// MUX Reference` header. Treat WinUI as the specification: when fixing a bug, compare against the C++ first. Behavior which Uno Platform has to do differently is marked with a `// TODO Uno` comment, and code which can't run yet is parked under `#if !HAS_UNO`.

The visual tree matches WinUI. The `ScrollViewer` in the control template does the scrolling, and the items panel is a real panel hosting the containers as its children.

## Classes

| Class | Role |
| --- | --- |
| `ListViewBase` | Selection, interaction, drag and drop, container phasing and incremental loading. |
| `Selector` | Selection state and focus tracking, shared with `ComboBox` and others. |
| `ItemsControl` | Implements `IGeneratorHost`, which the panel uses to create, prepare and clear containers. |
| `ModernCollectionBasePanel` | The virtualizing panel, referred to as MCBP in the code. Owns the realization window, the containers and the recycling. |
| `ItemsStackPanel`, `ItemsWrapGrid` | MCBP with a layout strategy. |
| `CarouselPanel` | MCBP with the stacking strategy, for `ComboBox`. Unlike WinUI's, it doesn't derive from `OrientedVirtualizingPanel`. |
| `StackingLayoutStrategy`, `WrappingLayoutStrategy` | Decide where an element goes and estimate what isn't realized. |
| `BuildTreeService` | Runs time-sliced work once layout is done, such as filling the cache and clearing recycled containers. |

The parts of MCBP live in `Primitives/ModernCollectionBasePanel/`, one file per concern:

- `WindowManagement`: the visible and realization windows, and the commands that move them, such as `ScrollItemIntoView`.
- `ViewportManagement`: keeps the viewport on a tracked element when the collection or the estimates change.
- `ContainerMapper`: the `ContainerManager`, which maps indices to the realized elements.
- `IICG2`: generating and recycling containers, and reacting to collection changes.
- `CacheManager`: cached item and group counts of the collection.
- `StickyHeaders`: sticky group headers.

## Layout life cycle

1. On measure, MCBP computes the window it wants to realize from the viewport. Items in that window get a container, which is taken from the recycle queue when possible, prepared through `IGeneratorHost` and measured.
2. Containers leaving the window go to the recycle queue. They stay children of the panel, arranged out of view, so don't count `Panel.Children` to know what's realized: check `VirtualizationInformation.IsRealized`.
3. `ListViewBase` defers the clear of a recycled container, because a prepare usually follows right away and then the clear isn't needed.
4. `ListViewBase` runs on the `BuildTreeService`: it sets the content of the containers in phases and raises `ContainerContentChanging`. MCBP then extends the realized range over the cache buffers, a few containers per pass. With the default `CacheLength` of 4, the window is 5 viewports in total.
5. The panel returns an estimate of its size from the realized elements and the layout strategy. As on WinUI, it changes while items get realized if they vary in size.

## Elements which stay realized

`ContainerFromIndex` can return a container far from the viewport:

- The stacking strategy always keeps item 0 pinned, along with the first group header when grouped, to measure them for the estimates (`MeasureSpecialElements`).
- A focused container isn't recycled.
- The cache buffers extend beyond the viewport, and when the list is at one end, the whole cache moves to the other side.

## Differences forced by the platform

These are the main places where Uno Platform has to differ from the C++, so check them first if something around them misbehaves:

- **Scrolling:** Uno Platform has no DirectManipulation. The `ScrollContentPresenter` drives inertia and animated scrolling, and `ScrollViewer.IsInManipulation` covers those motions.
- **Offsets:** the `ScrollViewer` publishes its offsets ahead of the layout that realizes the new window, while WinUI publishes them after it. Offset-driven checks like incremental loading therefore run again once layout is done, and the panel sets offsets with `ChangeViewForLayoutAdjustment` so that they survive the extent update.
- **Transitions:** there are no layout transition elements. Storyboards animate the element's own `TransitionTarget`, and `UIElement.GetTransform` takes running layout transitions back out, so that coordinates report the final layout position.
- **Sticky headers:** the panel doesn't set a clip. Containers crossing the bottom of a sticky header get a `Visual.TransitionClip`, and the headers are raised with `Canvas.ZIndex`.
