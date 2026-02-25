---
uid: Uno.XamarinFormsMigration.ControlMappings
---

# Control Mappings from Xamarin.Forms to Uno Platform

This guide provides a comprehensive mapping of Xamarin.Forms controls and their equivalents in Uno Platform/WinUI. Understanding these mappings is essential when migrating your Xamarin.Forms application to Uno Platform.

## Layout Controls

| Xamarin.Forms | Uno Platform / WinUI | Notes |
|---------------|---------------------|-------|
| `StackLayout` | `StackPanel` | Set `Orientation` property for horizontal/vertical layout |
| `Grid` | `Grid` | Same control name, similar API with `RowDefinitions` and `ColumnDefinitions` |
| `AbsoluteLayout` | `Canvas` | Use `Canvas.Left`, `Canvas.Top` for positioning |
| `RelativeLayout` | `RelativePanel` | Different API - uses attached properties for relationships |
| `FlexLayout` | Custom implementation or `VariableSizedWrapGrid` | No direct equivalent; consider redesigning with `Grid` or `StackPanel` |
| `ScrollView` | `ScrollViewer` | Similar functionality with additional properties |
| `ContentView` | `ContentControl` | Base class for custom controls |
| `Frame` | `Border` | Use `CornerRadius`, `BorderBrush`, `BorderThickness` |

## Text Controls

| Xamarin.Forms | Uno Platform / WinUI | Notes |
|---------------|---------------------|-------|
| `Label` | `TextBlock` | For read-only text display |
| `Entry` | `TextBox` | Single-line text input |
| `Editor` | `TextBox` with `AcceptsReturn="True"` | Multi-line text input |
| `SearchBar` | `AutoSuggestBox` | Provides search functionality with suggestions |
| `Span` (in FormattedString) | `Run` (in TextBlock) | For inline formatted text |

## Button and Input Controls

| Xamarin.Forms | Uno Platform / WinUI | Notes |
|---------------|---------------------|-------|
| `Button` | `Button` | Same control name |
| `ImageButton` | `Button` with `Image` content | Or use `AppBarButton` with icon |
| `CheckBox` | `CheckBox` | Same control name |
| `Switch` | `ToggleSwitch` | Different control with on/off states |
| `Slider` | `Slider` | Same control name |
| `Stepper` | `NumberBox` | Provides increment/decrement functionality |
| `DatePicker` | `DatePicker` or `CalendarDatePicker` | Choose based on UI requirements |
| `TimePicker` | `TimePicker` | Same control name |
| `Picker` | `ComboBox` | Dropdown selection control |
| `RadioButton` | `RadioButton` | Available in WinUI 2.5+ |

## Collection Controls

| Xamarin.Forms | Uno Platform / WinUI | Notes |
|---------------|---------------------|-------|
| `ListView` | `ListView` | Same control name with different templating |
| `CollectionView` | `ItemsRepeater` or `ListView` | `ItemsRepeater` for flexible layouts |
| `CarouselView` | `FlipView` | Swipeable item view |
| `TableView` | Custom with `ListView` or `ItemsRepeater` | No direct equivalent |
| `RefreshView` | `RefreshContainer` | Pull-to-refresh functionality |
| `SwipeView` | Custom implementation | Use `SwipeControl` on supported platforms |
| `IndicatorView` | Custom implementation | Create with `ItemsRepeater` or dots pattern |

## Visual and Media Controls

| Xamarin.Forms | Uno Platform / WinUI | Notes |
|---------------|---------------------|-------|
| `Image` | `Image` | Same control name |
| `ImageButton` | `Button` with `Image` | Or use icon fonts |
| `BoxView` | `Rectangle` or `Border` | For colored rectangles |
| `WebView` | `WebView2` | Requires WebView2 runtime on Windows |
| `MediaElement` | `MediaPlayerElement` | For audio/video playback |
| `Map` | Third-party map controls | Use Uno.Extensions or platform-specific maps |
| `ActivityIndicator` | `ProgressRing` | Indeterminate progress indicator |
| `ProgressBar` | `ProgressBar` | Same control name |

## Page and Navigation

| Xamarin.Forms | Uno Platform / WinUI | Notes |
|---------------|---------------------|-------|
| `ContentPage` | `Page` | Base page type |
| `NavigationPage` | `Frame` with navigation | See [Migrating Navigation](xref:Uno.XamarinFormsMigration.Navigation) |
| `TabbedPage` | `NavigationView` or `TabView` | Bottom tabs or top tabs |
| `CarouselPage` | `FlipView` | Swipeable pages |
| `MasterDetailPage` | `NavigationView` | Split view with menu |
| `Shell` | `NavigationView` with routing | See navigation guide |
| `FlyoutPage` | `NavigationView` | Replaces MasterDetailPage |

## Special Controls

| Xamarin.Forms | Uno Platform / WinUI | Notes |
|---------------|---------------------|-------|
| `ToolbarItem` | `AppBarButton` in `CommandBar` | Top app bar buttons |
| `MenuItem` | `MenuFlyoutItem` | Context menu items |
| `ContextActions` | `MenuFlyout` or `SwipeBehavior` | Right-click or swipe actions |
| `Shapes` (Rectangle, Ellipse, etc.) | `Windows.UI.Xaml.Shapes` namespace | Same shape controls |

## Property Mappings

### Size and Layout

| Xamarin.Forms | Uno Platform / WinUI |
|---------------|---------------------|
| `HeightRequest` | `Height` |
| `WidthRequest` | `Width` |
| `MinimumHeightRequest` | `MinHeight` |
| `MinimumWidthRequest` | `MinWidth` |
| `HorizontalOptions` | `HorizontalAlignment` |
| `VerticalOptions` | `VerticalAlignment` |
| `Margin` | `Margin` (same) |
| `Padding` | `Padding` (same) |

### Alignment Values

| Xamarin.Forms | Uno Platform / WinUI |
|---------------|---------------------|
| `Start` | `Left` |
| `End` | `Right` |
| `Center` | `Center` |
| `Fill` | `Stretch` |

### Colors and Styling

| Xamarin.Forms | Uno Platform / WinUI |
|---------------|---------------------|
| `BackgroundColor` | `Background` (use `SolidColorBrush`) |
| `TextColor` | `Foreground` (use `SolidColorBrush`) |
| `BorderColor` | `BorderBrush` (use `SolidColorBrush`) |
| `Color.Accent` | `{ThemeResource SystemAccentColor}` |
| `Color.Default` | Theme-dependent resources |

### Text and Fonts

| Xamarin.Forms | Uno Platform / WinUI |
|---------------|---------------------|
| `FontSize` | `FontSize` (same) |
| `FontAttributes="Bold"` | `FontWeight="Bold"` |
| `FontAttributes="Italic"` | `FontStyle="Italic"` |
| `FontFamily` | `FontFamily` (same) |
| `TextTransform` | Custom converter or `CharacterCasing` |

## Behaviors and Attachments

| Xamarin.Forms | Uno Platform / WinUI | Notes |
|---------------|---------------------|-------|
| `Behavior<T>` | Attached properties or `Microsoft.Xaml.Behaviors` | Use Uno.Toolkit or create custom attached properties |
| `TriggerAction` | Visual states or code-behind | See [Effects guide](xref:Uno.XamarinFormsMigration.Effects) |
| `DataTrigger` | Visual states with `StateTrigger` | Use `AdaptiveTrigger`, `StateTrigger` |
| `EventTrigger` | Event handlers or behaviors | Use code-behind or behaviors library |

## Services and Dependency Injection

| Xamarin.Forms | Uno Platform / WinUI | Notes |
|---------------|---------------------|-------|
| `DependencyService` | Built-in DI (IServiceProvider) | Use constructor injection with `IHost` |
| `MessagingCenter` | Event aggregator or messenger pattern | Use `Uno.Extensions` messaging or CommunityToolkit.Mvvm `Messenger` |

## Application Lifecycle

| Xamarin.Forms | Uno Platform / WinUI | Notes |
|---------------|---------------------|-------|
| `Application.OnStart` | `App.OnLaunched` | Application startup |
| `Application.OnSleep` | `Application.EnteredBackground` | App backgrounded |
| `Application.OnResume` | `Application.LeavingBackground` | App foregrounded |
| `Application.MainPage` | `Window.Content` or navigation setup | Set root content |

## No Direct Equivalent

Some Xamarin.Forms features don't have direct equivalents and require alternative approaches:

### CollectionView Features

- **EmptyView**: Implement with conditional visibility and custom empty state UI
- **ItemsLayout**: Use `ItemsPanel` with different panel templates in `ListView` or `ItemsRepeater`
- **RemainingItemsThreshold**: Implement using `IncrementalLoadingTrigger` or scroll position monitoring

### SwipeView

- Use platform-specific implementations with conditional compilation
- Consider `SwipeControl` for Windows 10+
- Implement custom gestures for other platforms

### Shell Navigation

- Use `Uno.Extensions.Navigation` for similar URI-based routing
- Implement with `Frame` and view models for simpler scenarios
- Use `NavigationView` for hierarchical navigation

### FlexLayout

- Redesign layouts using `Grid` with responsive triggers
- Use `VariableSizedWrapGrid` for simple wrapping scenarios
- Consider CSS Grid on WebAssembly with HTML embedding

## Migration Strategy

When migrating controls:

1. **Identify usage patterns**: Search for all instances of a Xamarin.Forms control
2. **Map to equivalent**: Use the tables above to find the Uno Platform equivalent
3. **Update properties**: Map property names according to the property mappings
4. **Test on target platforms**: Verify behavior on all platforms you're targeting
5. **Refactor if needed**: Some controls may require redesign for better Uno Platform patterns

## Platform-Specific Considerations

### Native Renderer vs Skia

- **Native renderer** on iOS/Android: Maps WinUI controls to native platform controls
- **Skia renderer**: Uses Skia for consistent pixel-perfect rendering across platforms
- Some controls behave differently between renderers - test thoroughly

### WebAssembly

- `WebView2` becomes an HTML iframe
- Media controls may have limitations
- File pickers use browser file input

### Performance

- `ListView` and `ItemsRepeater` are optimized for large lists
- Use virtualization for long scrolling lists
- Consider `ItemsStackPanel` or `ItemsWrapGrid` for layout panels

## Next Steps

- [Migrating Custom Controls](xref:Uno.XamarinFormsMigration.CustomControls)
- [Migrating Data Binding](xref:Uno.XamarinFormsMigration.DataBinding)
- [Migrating Navigation](xref:Uno.XamarinFormsMigration.Navigation)
- Return to [Overview](xref:Uno.XamarinFormsMigration.Overview)

## See Also

- [Native Views in Uno Platform](xref:Uno.Development.NativeViews)
- [Platform-Specific XAML](xref:Uno.Development.PlatformSpecificXaml)
- [Platform-Specific C#](xref:Uno.Development.PlatformSpecificCSharp)
