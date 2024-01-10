---
uid: Uno.Controls.ListViewBase
---

# ListViewBase in Uno Platform

Uno Platform's implementation of ListViewBase supports shared styling and template use with UWP apps, whilst internally delegating to the native list view on Android and iOS for high performance. This document explains how Uno's implementation differs in some details from Windows.

For contributors, [in-depth documentation on the internals of ListView is available](../uno-development/listviewbase-internals.md).

## Style reuse

This is a stripped-down view of the default style for ListView in Uno:

```xml
<!-- Default style for Windows.UI.Xaml.Controls.ListView -->
<not_win:Style TargetType="ListView">
  <Setter Property="ItemsPanel">
    <Setter.Value>
      <ItemsPanelTemplate>
        <ItemsStackPanel Orientation="Vertical" />
      </ItemsPanelTemplate>
    </Setter.Value>
  </Setter>
  <Setter Property="Template">
    <Setter.Value>
      <ControlTemplate TargetType="ListView">
        <Border>
          <ScrollViewer
              x:Name="ScrollViewer"
              not_win:Style="{StaticResource ListViewBaseScrollViewerStyle}">
            <ItemsPresenter/>
          </ScrollViewer>
        </Border>
      </ControlTemplate>
    </Setter.Value>
  </Setter>
</not_win:Style>
```

As on Windows, the ItemsPanelTemplate can be set; ItemsStackPanel and ItemsWrapGrid are the supported panels, and each of these supports most of the same properties as on Windows.

In fact there is only one difference from the Windows style, which is a custom Style on the ScrollViewer element. Below is the custom ScrollViewer style in its entirety:

```xml
<!-- This is an Uno-only Style which removes the ScrollContentPresenter, in order for ListViewBase to use the default Windows style (nearly)
     while delegating to a native implementation for performance. -->
<not_win:Style TargetType="ScrollViewer" x:Key="ListViewBaseScrollViewerStyle">
  <Setter Property="Template">
    <Setter.Value>
      <ControlTemplate TargetType="ScrollViewer">
        <ListViewBaseScrollContentPresenter
            x:Name="ScrollContentPresenter"
            Content="{TemplateBinding Content}"
            ContentTemplate="{TemplateBinding ContentTemplate}"
            ContentTemplateSelector="{TemplateBinding ContentTemplateSelector}"/>
      </ControlTemplate>
    </Setter.Value>
  </Setter>
</not_win:Style>
```

This style replaces the internal `ScrollPresenter` with a `ListViewBaseScrollContentPresenter`, for reasons explained below. Custom 
ListView/GridView styles should modify the ScrollViewer template part's style to the one shown above.

## Performance tips

### Observable collections

If you use a collection type like an array or a `List<T>` as an `ItemsSource`, then whenever you change the `ItemsSource`, all the item 
views will be removed and recreated. Often it's preferable to support incremental changes to your source collection.

`ListViewBase` implicitly supports incremental collection changes whenever the ItemsSource is a collection which implements the 
`INotifyCollectionChanged` interface. The `ObservableCollection<T>` class in the standard library is one such collection. Whenever an 
item is added, removed, or replaced in the collection, the corresponding `CollectionChanged` event is raised. `ListViewBase` listens to 
this event and only visually modifies the items that have actually changed, using platform-specific animations. The list also maintains 
the scroll position if possible and maintains the current selected item (unless it's removed).

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

 Note that using an observable collection adds complexity and there are some cases when it can even be anti-performant. For example, 
 consider a list of items that can be filtered by a search term. Modifying the search term by one character (i.e., when the user types in a 
 `TextBox`) might remove or add hundreds of items from the filtered source. Particularly on iOS, and to a lesser extent on Android, the 
 list tries to do preprocessing on each change to determine what animations it needs, etc. The result is a noticeable lag, where changing 
 the `ItemsSource` completely would have been nearly instantaneous.

 So consider using a non-observable collection, or use the Uno-only `RefreshOnCollectionChanged` flag (which causes the native list to 
 refresh without animations when any `CollectionChanged` event is raised), if your scenario matches the following:

 1. Large numbers of items may change at once, and
 2. Changes are not particularly 'meaningful' to the user.

## Internal implementation

Internally Uno's implementation differs from UWP's. On UWP the ScrollViewer handles scrolling, while the ItemsStackPanel or ItemsWrapGrid 
handles virtualization (reuse of item views). On Uno, both scrolling and virtualization are handled by NativeListViewBase, an internal 
panel which inherits from the native list class on each platform. ItemsStackPanel/ItemsWrapGrid exist only as logical elements of ListView/
GridView, they aren't in the visual tree. Their properties are redirected to ItemsStackPanelLayout/ItemsWrapGridLayout, non-visual 
classes which instruct NativeListViewBase on how to lay out its views.

Since NativeListViewBase class handles scrolling, the ScrollViewer contains a ListViewBaseScrollContentPresenter which is essentially a 
placeholder with no functionality.

Note that this only applies when ItemsStackPanel/ItemsWrapGrid are used. When any other panel is used, ListViewBase will use an ordinary 
ScrollViewer + ScrollContentPresenter.

### Difference in the visual tree

```
 UWP                                                      Uno
+--ListView------------------------------------------+   +--ListView------------------------------------------+
|                                                    |   |                                                    |
| +--ScrollViewer----------------------------------+ |   | +--ScrollViewer----------------------------------+ |
| |                                                | |   | |                                                | |
| | +--ScrollContentPresenter--------------------+ | |   | | +--ListViewBaseScrollContentPresenter--------+ | |
| | |                                            | | |   | | |                                            | | |
| | | +--ItemsPresenter------------------------+ | | |   | | | +--ItemsPresenter------------------------+ | | |
| | | |                                        | | | |   | | | |                                        | | | |
| | | | +--Header----------------------------+ | | | |   | | | | +--NativeListViewBase----------------+ | | | |
| | | | |                                    | | | | |   | | | | |                                    | | | | |
| | | | +------------------------------------+ | | | |   | | | | | +--Header------------------------+ | | | | |
| | | |                                        | | | |   | | | | | |                                | | | | | |
| | | | +--ItemsStackPanel-------------------+ | | | |   | | | | | +--------------------------------+ | | | | |
| | | | |                                    | | | | |   | | | | |                                    | | | | |
| | | | | +--ListViewItem------------------+ | | | | |   | | | | | +--ListViewItem------------------+ | | | | |
| | | | | |                                | | | | | |   | | | | | |                                | | | | | |
| | | | | +--------------------------------+ | | | | |   | | | | | +--------------------------------+ | | | | |
| | | | |                                    | | | | |   | | | | |                                    | | | | |
| | | | | +--ListViewItem------------------+ | | | | |   | | | | | +--ListViewItem------------------+ | | | | |
| | | | | |                                | | | | | |   | | | | | |                                | | | | | |
| | | | | +--------------------------------+ | | | | |   | | | | | +--------------------------------+ | | | | |
| | | | |                                    | | | | |   | | | | |                                    | | | | |
| | | | |                                    | | | | |   | | | | | +--Footer------------------------+ | | | | |
| | | | |                                    | | | | |   | | | | | |                                | | | | | |
| | | | |                                    | | | | |   | | | | | +--------------------------------+ | | | | |
| | | | +------------------------------------+ | | | |   | | | | |                                    | | | | |
| | | |                                        | | | |   | | | | |                                    | | | | |
| | | | +--Footer----------------------------+ | | | |   | | | | |                                    | | | | |
| | | | |                                    | | | | |   | | | | |                                    | | | | |
| | | | +------------------------------------+ | | | |   | | | | +------------------------------------+ | | | |
| | | |                                        | | | |   | | | |                                        | | | |
| | | +----------------------------------------+ | | |   | | | +----------------------------------------+ | | |
| | |                                            | | |   | | |                                            | | |
| | +--------------------------------------------+ | |   | | +--------------------------------------------+ | |
| |                                                | |   | |                                                | |
| +------------------------------------------------+ |   | +------------------------------------------------+ |
|                                                    |   |                                                    |
+----------------------------------------------------+   +----------------------------------------------------+
```

### Other differences from UWP
 
 * The ListView doesn't use XAML animations (AddDeleteThemeTransition, etc) for collection modifications, instead it uses the animations provided by the native collection class. On iOS, these can be disabled by setting the `ListViewBase.UseCollectionAnimations` flag to `false`.
