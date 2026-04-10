---
uid: Uno.Development.WPFWinUIEquivalents
---

# WPF to WinUI XAML Equivalents Reference

This reference provides a comprehensive mapping of WPF namespaces, controls, XAML syntax, events, and patterns to their WinUI 3 / Uno Platform equivalents. Use it as a lookup when migrating WPF applications.

> [!TIP]
> For architectural migration guidance (MVVM, navigation, data access), see [Migrating WPF Apps to Web](wpf-migration.md).

## Namespace Mapping

| WPF Namespace | WinUI 3 Namespace | Notes |
|---|---|---|
| `System.Windows` | `Microsoft.UI.Xaml` | Root namespace |
| `System.Windows.Controls` | `Microsoft.UI.Xaml.Controls` | Core controls |
| `System.Windows.Controls.Primitives` | `Microsoft.UI.Xaml.Controls.Primitives` | Low-level primitives |
| `System.Windows.Media` | `Microsoft.UI.Xaml.Media` | Brushes, transforms |
| `System.Windows.Media.Animation` | `Microsoft.UI.Xaml.Media.Animation` | Storyboard, animations |
| `System.Windows.Media.Imaging` | `Microsoft.UI.Xaml.Media.Imaging` | BitmapImage, WriteableBitmap |
| `System.Windows.Media.Media3D` | No equivalent | Use Win2D or Composition APIs |
| `System.Windows.Shapes` | `Microsoft.UI.Xaml.Shapes` | Rectangle, Ellipse, Path |
| `System.Windows.Data` | `Microsoft.UI.Xaml.Data` | Binding, IValueConverter |
| `System.Windows.Input` | `Microsoft.UI.Xaml.Input` | Pointer, keyboard, focus |
| `System.Windows.Navigation` | No direct equivalent | Use `Frame.Navigate()` |
| `System.Windows.Documents` | Limited | `RichTextBlock` + `Paragraph` |
| `System.Windows.Markup` | `Microsoft.UI.Xaml.Markup` | XAML parsing, markup extensions |
| `System.Windows.Automation` | `Microsoft.UI.Xaml.Automation` | Accessibility / UI Automation |
| `System.Windows.Interop` | `Microsoft.UI.Xaml.Hosting` | Interop / XAML Islands |
| `System.Windows.Threading` | `Microsoft.UI.Dispatching` | `Dispatcher` becomes `DispatcherQueue` |

### XAML Declaration Syntax

| WPF | WinUI |
|---|---|
| `xmlns:local="clr-namespace:MyApp"` | `xmlns:local="using:MyApp"` |
| `xmlns:local="clr-namespace:MyApp;assembly=MyLib"` | `xmlns:local="using:MyApp"` (assembly inferred) |

## Control Mappings

### Controls That Transfer Directly

These controls exist in both WPF and WinUI and typically require only a namespace change:

Button, TextBox, TextBlock, CheckBox, RadioButton, ComboBox, ListBox, ListView, Grid, StackPanel, Border, ScrollViewer, Canvas, Image, Slider, ProgressBar, ToolTip/ToolTipService, Expander, TreeView.

### WinUI-Only Additions You May Want to Adopt

The following controls do not have direct equivalents in WPF but are often valuable to introduce when modernizing your UI with WinUI / Uno Platform:

NavigationView, NumberBox, InfoBar, ProgressRing, ToggleSwitch, HyperlinkButton, GridView.
### Controls Requiring Replacement

| WPF Control | WinUI / Uno Replacement | Notes |
|---|---|---|
| `DataGrid` | Community Toolkit `DataGrid` | Similar API, not identical. Requires `CommunityToolkit.WinUI.UI.Controls`. |
| `Ribbon` | `CommandBar` or `NavigationView` | No Ribbon in WinUI |
| `Menu` / `MenuItem` | `MenuBar` / `MenuBarItem` / `MenuFlyoutItem` | `MenuBar` for top-level menus; `MenuFlyout` for context menus |
| `ContextMenu` | `MenuFlyout` via `ContextFlyout` property | Assign to `ContextFlyout` on any `UIElement` |
| `ToolBar` / `ToolBarTray` | `CommandBar` + `AppBarButton` | |
| `StatusBar` | Custom `Grid` / `StackPanel` or `InfoBar` | No StatusBar control in WinUI |
| `TabControl` | `TabView` or `NavigationView` (Top mode) | `TabView` supports closeable tabs |
| `DocumentViewer` | `WebView2` | Render PDFs/XPS inside WebView2 |
| `FlowDocument` | `RichTextBlock` | Partial replacement only |
| `RichTextBox` | `RichEditBox` | Rich text editing |
| `WrapPanel` | `WrapPanel` (built-in on Uno Platform) or Community Toolkit `WrapPanel` | Built-in on Uno Platform; in WinUI 3 (Windows App SDK only), use `CommunityToolkit.WinUI.UI.Controls` |
| `UniformGrid` | Community Toolkit `UniformGrid` | Not in WinUI by default |
| `DockPanel` | Community Toolkit `DockPanel` | Not in WinUI by default |
| `GroupBox` | `Expander` or custom `HeaderedContentControl` | No GroupBox in WinUI |
| `Label` | `TextBlock` | Use `TextBlock` + `AccessKey` property |
| `MediaElement` | `MediaPlayerElement` | Different API surface |
| `Window` (standalone) | `Window` | Window host; content is typically a `Page`/root `UIElement`. Use `ContentDialog` for modal windows. |
| `GridSplitter` | Community Toolkit `GridSplitter` | Requires `CommunityToolkit.WinUI.UI.Controls` |
| `Calendar` | `CalendarView` | Similar functionality with updated API |
| `ListBox` | `ListView` | `ListView` is the WinUI equivalent |

### Useful NuGet Packages

| Package | Purpose |
|---|---|
| `CommunityToolkit.WinUI.UI.Controls` | DataGrid, WrapPanel, DockPanel, UniformGrid. Note: `DataGrid` and some related controls are available in Windows Community Toolkit 7.x; they were removed from 8.x. |
| `CommunityToolkit.Mvvm` | RelayCommand, ObservableObject, source generators |
| `Uno.Themes.WinUI` | Material, Cupertino, or Fluent theme support |
| `Uno.Toolkit.WinUI` | Additional cross-platform controls and helpers |

## Property and Value Mappings

| WPF Property / Value | WinUI Equivalent | Context |
|---|---|---|
| `Visibility.Hidden` | Not available | Use `Opacity="0"` together with `IsHitTestVisible="False"` (and, for focusable controls, `IsTabStop="False"`) to be invisible, non-interactive, but still layout-occupying. |
| `TextWrapping.WrapWithOverflow` | `TextWrapping.Wrap` | WinUI does not distinguish |
| `Focusable` | `IsTabStop` and, when preventing interaction focus, `AllowFocusOnInteraction` | `IsTabStop` controls keyboard tab navigation; to also prevent focus via pointer interaction in WinUI/Uno, typically use `AllowFocusOnInteraction="False"` as well (and `IsTabStop="False"` where applicable). |

## Event Mappings

### Pointer Events (Replacing Mouse Events)

| WPF Event | WinUI Event | Args Type |
|---|---|---|
| `MouseLeftButtonDown` | `PointerPressed` | `PointerRoutedEventArgs` |
| `MouseLeftButtonUp` | `PointerReleased` | `PointerRoutedEventArgs` |
| `MouseMove` | `PointerMoved` | `PointerRoutedEventArgs` |
| `MouseEnter` | `PointerEntered` | `PointerRoutedEventArgs` |
| `MouseLeave` | `PointerExited` | `PointerRoutedEventArgs` |
| `MouseWheel` | `PointerWheelChanged` | `PointerRoutedEventArgs` |
| `MouseDoubleClick` | `DoubleTapped` | `DoubleTappedRoutedEventArgs` |
| `PreviewMouseDown` | `PointerPressed` | No direct WPF-style tunneling/preview pointer event in WinUI |

> [!NOTE]
> WinUI does not provide WPF-style `PreviewMouse*` tunneling events for pointer/mouse input. If you need to listen for a bubbling event even after a child marks it handled, use `AddHandler` with `handledEventsToo: true`. This does not reproduce WPF-style preview ordering (parent before child).

### Keyboard Events

| WPF Event | WinUI Event | Args Type |
|---|---|---|
| `KeyDown` | `KeyDown` | `KeyRoutedEventArgs` |
| `KeyUp` | `KeyUp` | `KeyRoutedEventArgs` |
| `PreviewKeyDown` | `PreviewKeyDown` | `KeyRoutedEventArgs` |
| `PreviewKeyUp` | `PreviewKeyUp` | `KeyRoutedEventArgs` |

> [!NOTE]
> Unlike pointer/mouse input, WinUI/Uno supports preview keyboard events via `PreviewKeyDown` and `PreviewKeyUp` on `UIElement`. If you are migrating from WPF, use those events for keyboard tunneling scenarios. Platform-specific behavior can still vary, so validate event ordering on your target platforms when the distinction matters.

### Mouse Capture

| WPF | WinUI |
|---|---|
| `element.CaptureMouse()` | `element.CapturePointer(e.Pointer)` |
| `element.ReleaseMouseCapture()` | `element.ReleasePointerCapture(e.Pointer)` |
| `Mouse.GetPosition(element)` | `e.GetCurrentPoint(element).Position` |

## XAML Syntax Translations

### Resource and Binding Markup

| WPF | WinUI | Notes |
|---|---|---|
| `{StaticResource Key}` | `{StaticResource Key}` | Identical - resolved once at load |
| `{DynamicResource Key}` | `{ThemeResource Key}` | Re-evaluated on theme changes (Light/Dark) |
| `{x:Static ns:Type.Member}` | `{x:Bind ns:Type.Member}` | Common option for static member references; enums/values can often use direct `ns:Type.Member` or literals |
| `{Binding Path=X}` | `{x:Bind ViewModel.X, Mode=OneWay}` | See [binding comparison](#binding-technology-comparison) below |
| `{Binding RelativeSource={RelativeSource AncestorType=...}}` | Uno Toolkit `AncestorSource` | See [Ancestor/ItemsControl binding](https://github.com/unoplatform/uno.toolkit.ui/blob/main/doc/helpers/ancestor-itemscontrol-binding.md) |

### Binding Technology Comparison

| Feature | `{Binding}` | `{x:Bind}` |
|---|---|---|
| Compile-time validation | No | Yes |
| Default mode | OneWay | **OneTime** |
| Default source | DataContext | Page / UserControl code-behind |
| Function binding | No | Yes |
| Performance | Reflection-based | Compiled, no reflection |
| MultiBinding | Not in WinUI | Use function binding |

> [!IMPORTANT]
> `{x:Bind}` defaults to **OneTime**, not OneWay. Always specify `Mode=OneWay` for properties that should update.

## Patterns Without Direct WinUI Equivalents

### Style.Triggers and DataTriggers

WinUI does not support `Style.Triggers`, `DataTrigger`, `EventTrigger`, or `MultiDataTrigger`. Replace with `VisualStateManager`:

**WPF:**

```xml
<Style TargetType="Border">
  <Style.Triggers>
    <DataTrigger Binding="{Binding IsActive}" Value="True">
      <Setter Property="Background" Value="Green" />
    </DataTrigger>
  </Style.Triggers>
</Style>
```

**WinUI equivalent:**

```xml
<Border x:Name="MyBorder">
  <VisualStateManager.VisualStateGroups>
    <VisualStateGroup>
      <VisualState x:Name="Active">
        <VisualState.StateTriggers>
          <StateTrigger IsActive="{x:Bind ViewModel.IsActive, Mode=OneWay}" />
        </VisualState.StateTriggers>
        <VisualState.Setters>
          <Setter Target="MyBorder.Background" Value="Green" />
        </VisualState.Setters>
      </VisualState>
    </VisualStateGroup>
  </VisualStateManager.VisualStateGroups>
</Border>
```

### MultiBinding

WinUI does not support `MultiBinding`. Use `x:Bind` with function binding:

```xml
<TextBlock Text="{x:Bind local:Converters.FormatFullName(ViewModel.FirstName, ViewModel.LastName), Mode=OneWay}" />
```

```csharp
public static class Converters
{
    public static string FormatFullName(string first, string last)
        => $"{first} {last}";
}
```

### RoutedUICommand

Replace `RoutedUICommand` and `CommandBinding` with `ICommand` implementations. Using CommunityToolkit.Mvvm:

```csharp
[RelayCommand(CanExecute = nameof(CanSave))]
private void Save() { /* save logic */ }

private bool CanSave() => IsDirty;
```

WinUI 3 also offers `StandardUICommand` and `XamlUICommand` for predefined operations (Cut, Copy, Paste) with built-in icons and keyboard accelerators.

### Adorners

WinUI does not have an Adorner layer. Use these alternatives:

| Adorner Use Case | WinUI Replacement |
|---|---|
| Validation indicators | `TeachingTip`, `InfoBar`, or input validation templates |
| Resize handles | `Popup` positioned relative to target |
| Drag preview | `DragItemsStarting` event with custom DragUI |
| Overlay decorations | Canvas overlay or `Popup` layer |
| Watermark / Placeholder | `TextBox.PlaceholderText` (built-in) |

## Resource Dictionaries

```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <!-- Required: default Fluent styles -->
      <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
      <!-- Your custom resources -->
      <ResourceDictionary Source="ms-appx:///Styles/Colors.xaml" />
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

Key differences from WPF:

- `XamlControlsResources` must be the **first** merged dictionary to provide default styles. Omitting it leaves controls with no visual appearance.
- Resource paths use `ms-appx:///` protocol instead of relative paths.
- `Window.Resources` does not exist in WinUI. Place window-level resources on root layout containers or `Page`.

### Implicit Styles and BasedOn

Always use `BasedOn` when overriding default control styles. Without it, your style **replaces** the entire default style rather than extending it:

```xml
<Style TargetType="Button" BasedOn="{StaticResource DefaultButtonStyle}">
  <Setter Property="Background" Value="Red" />
</Style>
```

## Common API Replacements

| WPF Code | WinUI Replacement | Notes |
|---|---|---|
| `Application.Current.Dispatcher.Invoke(...)` | `App.MainWindow.DispatcherQueue.TryEnqueue(...)` | Fire-and-forget; no synchronous `Invoke`. For awaitable dispatch, wrap with a `TaskCompletionSource`: `var tcs = new TaskCompletionSource(); App.MainWindow.DispatcherQueue.TryEnqueue(() => { /* work */ tcs.SetResult(); }); await tcs.Task;` |
| `Window.Current` | `App.MainWindow` (captured at startup) | Not supported in Windows App SDK |
| `Clipboard` (System.Windows) | `Windows.ApplicationModel.DataTransfer.Clipboard` | Different API surface |
| `MessageBox.Show()` | `ContentDialog` with `XamlRoot` | No MessageBox in WinUI |

## Find-and-Replace Quick Reference

### XAML Attribute Replacements

| Find | Replace With |
|---|---|
| `ContextMenu=` | `ContextFlyout=` |
| `{DynamicResource` | `{ThemeResource` |
| `{x:Static` | `{x:Bind` |
| `Visibility="Hidden"` (layout-preserving) | `Opacity="0"` with `IsHitTestVisible="False"` | Keeps layout space; combine with `IsTabStop="False"` for focusable controls |
| `Visibility="Hidden"` (remove from layout) | `Visibility="Collapsed"` | Collapses space like WPF `Visibility="Collapsed"` |
| `MouseLeftButtonDown` | `PointerPressed` |
| `MouseLeftButtonUp` | `PointerReleased` |
| `MouseEnter` | `PointerEntered` |
| `MouseLeave` | `PointerExited` |
| `MouseMove` | `PointerMoved` |
| `MouseWheel` | `PointerWheelChanged` |
| `Focusable="True"` | `IsTabStop="True"` |
| `TextWrapping="WrapWithOverflow"` | `TextWrapping="Wrap"` |
| `MediaElement` | `MediaPlayerElement` |
| `InputBindings` | `KeyboardAccelerators` |
| `KeyBinding` | `KeyboardAccelerator` |
### Code-Behind Replacements

| Find | Replace With |
|---|---|
| `using System.Windows;` | `using Microsoft.UI.Xaml;` |
| `using System.Windows.Controls;` | `using Microsoft.UI.Xaml.Controls;` |
| `using System.Windows.Media;` | `using Microsoft.UI.Xaml.Media;` |
| `using System.Windows.Data;` | `using Microsoft.UI.Xaml.Data;` |
| `using System.Windows.Input;` | `using Microsoft.UI.Xaml.Input;` |
| `Dispatcher.Invoke(` | `DispatcherQueue.TryEnqueue(` |
| `MouseEventArgs` | `PointerRoutedEventArgs` |
| `KeyEventArgs` | `KeyRoutedEventArgs` |
| `RoutedUICommand` | `RelayCommand` (CommunityToolkit.Mvvm) |
| `CommandBinding` | Remove; bind `ICommand` directly |

## AI Prompt Template

Use this prompt with an AI coding assistant to automate the mechanical translation of WPF XAML files:

````text
You are a WPF-to-WinUI XAML migration assistant. Translate the following WPF XAML
file to WinUI 3 XAML compatible with Uno Platform.

Apply ALL of the following rules:

NAMESPACE RULES:
- Keep the default xmlns as-is
- Replace clr-namespace references with "using:" syntax
- Replace System.Windows.* with Microsoft.UI.Xaml.*

RESOURCE RULES:
- Replace {DynamicResource X} with {ThemeResource X}
- Replace {x:Static X} with {x:Bind X}
- Keep {StaticResource X} as-is

CONTROL REPLACEMENTS:
- Menu/MenuItem -> MenuBar/MenuBarItem/MenuFlyoutItem
- ContextMenu -> ContextFlyout with MenuFlyout
- ToolBar -> CommandBar with AppBarButton
- StatusBar -> Grid at bottom of layout
- TabControl -> TabView or NavigationView (Top mode)
- DataGrid -> CommunityToolkit DataGrid
- Label -> TextBlock
- MediaElement -> MediaPlayerElement

PROPERTY REPLACEMENTS:
- Visibility="Hidden" -> use `Opacity="0"` with `Visibility="Visible"` when layout must be preserved; use `Visibility="Collapsed"` only when removing the element from layout is acceptable
- TextWrapping="WrapWithOverflow" -> TextWrapping="Wrap"
- Focusable -> IsTabStop

EVENT REPLACEMENTS:
- Mouse* events -> Pointer* equivalents
- Remove Preview* tunneling events

TRIGGER REPLACEMENTS:
- Remove Style.Triggers, DataTrigger, EventTrigger
- Create VisualStateManager.VisualStateGroups with StateTrigger

BINDING UPGRADES:
- Convert {Binding Path=X} to {x:Bind ViewModel.X, Mode=OneWay}
- Replace MultiBinding with x:Bind function binding

OUTPUT: Complete translated XAML with a list of manual follow-up items.

WPF XAML to translate:
````

## FAQ

**Do I need to rewrite all my XAML from scratch?**

No. The majority of WPF XAML transfers with namespace changes and minor property fixes. The heavy lifting is in triggers, MultiBinding, and controls that don't exist in WinUI.

**Can I keep using {Binding} or must I switch to {x:Bind}?**

`{Binding}` still works and is not mandatory to replace. However, `{x:Bind}` provides compile-time validation, better performance, and function binding (which replaces MultiBinding). For new or migrated code, `{x:Bind}` is recommended.

**What replaces Visibility.Hidden?**

WinUI only has `Visible` and `Collapsed`. For invisible-but-layout-occupying behavior, set `Opacity="0"` while keeping `Visibility="Visible"`.

**How do I handle preview/tunneling events?**

WinUI/Uno supports some preview/tunneling events for keyboard input, including `PreviewKeyDown` and `PreviewKeyUp`, so you do not need to replace those with `KeyDown`/`KeyUp`. However, WPF-style preview mouse/pointer events such as `PreviewMouseDown` do not generally have direct equivalents. In those cases, use the corresponding bubbling event (for example, `PointerPressed`) and, if you need to observe events that were already marked handled, register with `AddHandler(..., handledEventsToo: true)`.

**How do I migrate the Dispatcher pattern?**

Replace `Application.Current.Dispatcher.Invoke(...)` with `App.MainWindow.DispatcherQueue.TryEnqueue(...)`. The `DispatcherQueue` API is asynchronous by default and has no synchronous `Invoke` method.

**Does Uno Platform add controls beyond WinUI?**

Yes. The [Uno Toolkit](xref:Toolkit.GettingStarted) includes NavigationBar, TabBar, DrawerControl, SafeArea, and more. The [Community Toolkit](uno-community-toolkit.md) controls (DataGrid, WrapPanel, DockPanel) also work across all Uno Platform targets.
