# Release notes

### Features

- Added new `ElevatedView` in the `Uno.Toolkit` to provide elevation & rounded corners on all platforms
  (not supported on Windows yet, because Uno needs to target framework `10.0.18362.0`)
- [Android] Support for `Application.Current.Exit`
- Support for `Windows.Storage.FileProperties.BasicProperties.DateModified`
- Added CornerRadius support to more default styles to match UWP (for list of updated styles see PR [#2713])
- Support for `FontIcon` on macOS
- Support for `PhoneCallManager.ShowPhoneCallUI` on macOS
- Support for full screen mode on macOS
- Support for `ShowComposeSmsMessageAsync` on macOS
- Support for `Flyout` on macOS
- Support for `HingeAngleSensor` for Surface Duo
- Support for `Geolocator` on macOS
- Support for `Clipboard` get/set Text content on macOS
- Support for `ApplicationView.Title` on Android and macOS
- Support for `AnalyticsInfo` on macOS
- Support for `TryEnterFullScreenMode` and `ExitFullScreenMode` on WebAssembly
- Support for `MessageDialog` on macOS
- [Android] support of `KnownFolders.MusicLibrary` and `VideosLibrary`
- Add support for `StorageFile.DateCreated`
- Support for `ApplicationView.IsScreenCaptureEnabled` on Android
- Add support for `StorageFile.DeleteAsync()`
- Support for `PointerDown`, `PointerUp` `PointerEntered`, `PointerExited` and `PointerMoved` events on macOS
- Support for `Launcher` API on macOS, support for special URIs
- Support for `EmailManager.ShowComposeNewEmailAsync`
- Add support for `StorageFolder.CreateFileAsync(string path)`
- Add support for ApplicationViewTitleBar.BackgroundColor on WASM
- Add support for Automation SetDependencyPropertyValue in Uno.UITest
- Added support for using a `string` value in a `StaticResource` when using `CreateFromStringAttribute'
- [Android] Adds support for `FeatureConfiguration.ScrollViewer.AndroidScrollbarFadeDelay`
- Add support for `Grid.ColumnSpacing` and `Grid.RowSpacing`
- Add clarification in [documentation](../articles/uno-development/working-with-the-samples-apps.md) for adding automated UI tests
- Add support for `Popup.LightDismissOverlayMode`, as well as `DatePicker.LightDismissOverlayMode` and `Flyout.LightDismissOverlayMode`. To modify the background color of the Overlay see Popup.md, DatePicker.md, TimePicker.md and Flyout.md
- `TransformToVisual` now returns a real transform to convert coordinates between views (was only returning a translate transform to offset the origin of controls)
- Multiple pointers at same time on screen (a.k.a. Multi-touch) are now supported
- Add support for WinUI 2.3 [`NumberBox`](https://docs.microsoft.com/en-us/uwp/api/microsoft.ui.xaml.controls.numberbox?view=winui-2.3)
- Add support of the `UIElement.RightTapped` event (The context menu won't appear anymore on WASM, except for the `TextBox`)
- Add support of the `UIElement.Holding` event
- [MacOS] Support for `ScrollViewer`
- [MacOS] Support for `LinearGradientBrush`
- Add support for [TwoPaneView](https://docs.microsoft.com/en-us/uwp/api/microsoft.ui.xaml.controls.twopaneview?view=winui-2.3) control.
- Add support for `ApplicationView.GetSpanningRects`
- Add base support for API Extensibility through `Uno.Foundation.Extensibility.ApiExtensibility` and `ApiExtensionAttribute`
- Add support for Surface Duo through the `Uno.UI.DualScreen` package
- Add support for enums in `x:Bind` functions and `BindBack`
- Add XamlReader support for Primitive static resources
- [Android] Add support for non-native `Popup` by default. Can be enabled through `FeatureConfiguration.Popup.UseNativePopup` set to false (See #2533 for more details)
- Add template tags for the VS2019 VSIX template search experience
- [iOS] #2746 Fix border thickness when a corner radius is set
- [Android] #2762 ProgressRing wasn't displaying inside a StackPanel
- #2797 Stack overflow in ListView when changing SelectedItem to and from invalid value
- [Android] #2761 Control with AreDimensionsConstrained and Margin set not measured correctly

### Breaking changes
- `IconElement.AddIconElementView` is now `internal` so it is not accessible from outside.
- `Thumb.DragStarted.<Horizontal|Vertical>Offset` are now fullfilled (was always 0)
- `Thumb.Drag<Delta|Completed>.<Horizontal|Vertical>` are now relative to the last event (was cummulative / relative to the started)
- On iOS, the parent of the `ListViwItem` is now the `NativeListViewBase` (was the `ListView` it self) as described here https://github.com/unoplatform/uno/blob/master/doc/articles/controls/ListViewBase.md#difference-in-the-visual-tree

### Bug fixes

- [iOS] Applying a `<RenderTransform>` on an image was producing an incorrect layout result.
- Adjust `CornerRadius` for `Button` style to apply properly
- Add support for `CornerRadius` in default `ComboBox` style
- Fix for samples app compilation for macOS
- [#2465] Raising macOS Button Click event
- [#2506] `DesignMode.DesignMode2Enabled` no longer throws (is always `false` on non-UWP platforms)
- [#915] FontFamily values are now properly parsed on WebAssembly, updated docs with new info
- [#2213] Fixed `ApplicationData` on MacOS, added support for `LocalSettings`
- Made macOS Samples app skeleton runnable (temporarily removed ApplicationData check on startup, fixed reference), added xamarinmacos20 to crosstargeting_override sample
- [#2230] `DisplayInformation` leaks memory
- [WASM] Shapes now update when their Fill brush's Color changes
- [WASM] Fix bug where changing `IsEnabled` from false to true on `Control` inside another `Control` didn't work
- [Wasm] Add arbitrary delay in Safari macOS to avoid StackOverflow issues
- #2227 fixed Color & SolidColorBrush literal values generation
- [Android] Fix bug where setting Canvas.ZIndex would apply shadow effect in some cases
- #2287 Vertical `ListView` containing a horizontal `ScrollViewer`: horizontal scrolling is difficult, only works when the gesture is perfectly horizontal
- #2130 Grid - fix invalid measure when total star size is 0
- [iOS] Fix invalid image measure on constrained images with `Margin`
- [#2364] fixed missing Xaml IntelliSense on newly created project
- `ViewBox` no longer alters its child's `RenderTransform`
- [#2033] Add Missing `LostFocus` Value to `UpdateSourceTrigger` Enum
- [Android] Fix Image margin calculation on fixed size
- [Android] Native views weren't clipped correctly
- [Android] Border thickness was incorrect when CornerRadius was set
- [iOS] #2361 ListView would measure children with infinite width
- [iOS] Fix crash when using ComboBox template with native Picker and changing ItemsSource to null after SelectedItem was set
- [#2398] Fully qualify the `MethodName` value for `CreateFromStringAttribute' if it's not fully qualified it the code
- [WASM] Fix bug where changing a property could remove the required clipping on a view
- #2294 Fix TextBox text binding is updated by simply unfocusing
- [Android] Fix unconstrained Image loading issue when contained in a ContentControl template
- Enable partial `NavigationView.ItemSource` scenario (https://github.com/unoplatform/uno/issues/2477)
- [Wasm] Fail gracefully if IDBFS is not enabled in emscripten
- [#2513] Fix `TransformGroup` not working
- [#1956] Fis iOS invalid final state when switching visual state before current state's animation is completed.
- Fix `Selector` support for IsSelected (#1606)
- [Android] 164249 fixed TextBox.Text flickering when using custom IInputFilter with MaxLength set
- [MacOS] Fix exceptions when modifying UIElementCollection, layouting view with null `Layer`
- Fix invalid conversion when using ThemeResource (e.g. Color resource to Brush property)
- Fix XamlBindingHelper.Convert double to GridLength
- [Android] Adjust `TextBlock.TextDecorations` is not updating properly
- Adjust `XamlBindingHelper` for `GridLength` and `TimeSpan`
- Add missing `ListView` resources
- [WASM] Setting null to the Fill no longer fill shapes in black
- Shapes was not able to receive pointer events
- [WASM] Invisble Shapes no longer prevent sub-elements to receive the pointer events
- `Thumb.DragStarted.<Horizontal|Vertical>Offset` are now fullfilled (was always 0)
- `Thumb.Drag<Delta|Completed>.<Horizontal|Vertical>` are now relative to the last event (was cummulative / relative to the started)
- Thumb now handles the PointyerPressed event (like WinUI)
- [WASM] Inserting an element at index 0 was appending the element instead of prepending it.
- #2570 [Android/iOS] fixed ObjectDisposedException in BindingPath
- #2107 [iOS] fixed ContentDialog doesn't block touch for background elements
- #2108 [iOS/Android] fixed ContentDialog background doesn't change
- #2680 [Wasm] Fix ComboBox should not stretch to the full window width

## Release 2.0

### Features

* [#2040] Support for ms-settings:// special URIs on Android and iOS, Launcher API alignments to match UWP behavior
* [#2029](https://github.com/unoplatform/uno/pull/2029) Support for MenuFlyoutItem.Click
* support /[file]/[name] format in ResourceLoader.GetForCurrentView().GetString()
* [#2039] Added support for Xaml type conversions using `CreateFromStringAttribute`.
* [#] Support for `Windows.Devices.Lights.Lamp` on iOS, Android.
* [#1970](https://github.com/unoplatform/uno/pull/1970) Added support for `AnalyticsInfo` properties on iOS, Android and WASM
* [#1207] Implemented some `PackageId` properties
* [#1919](https://github.com/unoplatform/uno/pull/1919) Support for `PathGeometry` on WASM.
* Support for `Geolocator` on WASM, improvements for support on Android, iOS
* [#1813](https://github.com/unoplatform/uno/pull/1813) - Added polyline support for WASM and samples for all shapes
* [#1743](https://github.com/unoplatform/uno/pull/1743) - Added a change to make the `MarkupExtensionReturnType` optional
* Added Dark and HighContrast theme resources, reacts to Dark/Light theme on iOS, Android and WASM automatically during the startup of the app if `RequestedTheme` is not set in `App.xaml`
* Support for `Gyrometer` on Android, iOS and WASM
   * `ReadingChanged`
   * `ReportInterval`
* Support for `Launcher.QueryUriSupportAsync` method on Android and iOS
* [#1493](https://github.com/unoplatform/uno/pull/1493) - Implemented the `Windows.Input.PointerUpdateKind` Enum.
*  [#1428](https://github.com/unoplatform/uno/issues/1428) - Add support for horizontal progressbars to `BindableProgressBar` on Android.
* Add support for `Windows.Devices.Sensors.Magnetometer` APIs on iOS, Android and WASM
   * `ReadingChanged`
   * `ReportInterval`
* Add support for `Windows.UI.StartScreen.JumpList` APIs on Android and iOS
   * Includes `Logo`, `DisplayName` and `Arguments`
   * The activation proceeds through the `OnLaunched` method same as on UWP
* Refactored `DrawableHelper` to the `Uno` project
* Add full implementation of `Windows.UI.Xaml.Input.InputScopeNameValue` on all platforms.
* Add support for `Windows.Devices.Sensors.Accelerometer` APIs on iOS, Android and WASM
   * `ReadingChanged`
   * `Shaken`
   * `ReportInterval`
* Align `ApplicationData.Current.LocalSettings.Add` behavior with UWP for `null` and repeated adds
* Add support for `Windows.ApplicationModel.Calls.PhoneCallManager`
* Add support for `Windows.Phone.Devices.Notification.VibrationDevice` API on iOS, Android and WASM
* Basic support for `Windows.Devices.Sensors.Barometer`
* Support setting `Style` inline (e.g. `<TextBlock><TextBlock.Style><Style TargetType="TextBlock"><Setter>...`)
* [Wasm] Add support for `DisplayInformation` properties `LogicalDpi`, `ResolutionScale`, `ScreenWidthInRawPixels`, `RawPixelsPerViewPixel` , and `ScreenHeightInRawPixels`¸
* Permit `DependencyProperty` to be set reentrantly. E.g. this permits `TextBox.TextChanged` to modify the `Text` property (previously this could only be achieved using `Dispatcher.RunAsync()`).
* Add support for filtered solutions development for Uno.UI contributions.
* 132984 [Android] Notch support on Android
* Add support for Android UI Tests in PRs for improved regression testing
* Add static support for **ThemeResources**: `Application.Current.RequestedTheme` is supported
  - `Dark` and `Light` are supported.
  - **Custom Themes** are supported. This let you specify `HighContrast` or any other custom themes.
    (this is a feature not supported in UWP)
    ``` csharp
    // Put that somewhere during app initialization...
    Uno.UI.ApplicationHelper.RequestedCustomTheme = "MyCustomTheme";
    ```
  - `FrameworkElement.RequestedTheme ` is ignored for now.
  - Should be set when the application is starting (before first request to a static resource).
* Prevent possible crash with `MediaPlayerElement` (tentative)
* Add support for `ContentDialog`, including `Closing` and `Closed` events
* Permit `DependencyProperty` to be set reentrantly. E.g. this permits `TextBox.TextChanging` to modify the `Text` property (previously this could only be achieved using `Dispatcher.RunAsync()`).
* Implement `TextBox.TextChanging` and `TextBox.BeforeTextChanging`. As on UWP, this allows the text to be intercepted and modified before the UI is updated. Previously on Android using the `TextChanged` event would lead to laggy response and dropped characters when typing rapidly; this is no longer the case with `TextChanging`.
* [WASM] `ComboBox`'s dropdown list (`CarouselPanel`) is now virtualized (#1012)
* Improve Screenshot comparer tool, CI test results now contain Screenshots compare data
* Updated Xamarin.GooglePlayServices.* packages to 60.1142.1 for Target MonoAndroid80
* Updated Xamarin.GooglePlayServices.* packages to 71.1600.0 for Target MonoAndroid90
* `<ContentPresenter>` will now - as a fallback when not set - automatically bind to
  `TemplatedParent`'s `Content` when this one is a `ContentControl`.
  You can deactivate this behavior like this:
  ```
  FeatureConfiguration.ContentPresenter.UseImplicitContentFromTemplatedParent = false;
  ```
* Add support for `Selector.IsSynchronizedWithCurrentItem`
* Add support for `CoreApplication.MainView` and `CoreApplication.Views`
* Add support for resolution of merged and theme resources from `ResourceDictionary` in code
* Add non-failing StatusBar BackgroundOpacity and BackgroundColor getters
* Relax DependencyProperty owner validation for non-FrameworkElement
* `ToolTip` & `ToolTipService` are now implemented.
* [#1352](https://github.com/unoplatform/uno/issues/1352) Add support for `ThemeResource`s with different types (e.g.: mixing `SolidColorBrush` and `LinearGradientBrush`)
* Add support for BitmapSource.PixelWidth and Height
* Preliminary support for `ColumnDefinition.ActualWidth` and `RowDefinition.ActualHeight`.
* Updated VisualTree of an app with Visibility for each items.
* Add support for `CompositionTarget.Rendering` event.
* Add support for `IObservableVector<T>` in `ItemsControl`
* [#1559] [#1167] Wasm: make the IsEnabled property inheritable.
* Full support of pointer events cf. [routed events documentation](../articles/features/routed-events.md)
* Add support of manipulation events cf. [routed events documentation](../articles/features/routed-events.md)
* Update CheckBox style to 10.0.17763
* Adds the support for `AutomationProperties.AutomationId`
* [#1328](https://github.com/unoplatform/uno/issues/1328) Basic ProgressRing implementation for WASM
* Add support for `Windows.UI.Xaml.Controls.Primitives.LayoutInformation.GetAvailableSize`
* Add support for Runtime Tests that require UI integration
* Enable iOS UI Tests
* Add support for `PersonPicture`
* Add support for `VisualState` `Setter` data binding, static resources and complex objects
* Clipping to bounds of control is now more similar to UWP
* The _feature flag_ `FeatureConfiguration.UseLegacyClipping` is now deprecated and not used anymore
* XAML Hot Reload support for iOS, Android and Windows
* Add support for GitPod Workspace and prebuilds
* #880 Added added implicit conversion for double to Thickness
* Add Android support for `CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar` to programatically draw under the status bar
* [WASM] `ScrollViewer.ChangeView` is now supported
* [Wasm] Add the ability to focus a TextBox by clicking its header
* Add support for `ToggleButton.IsThreeState` and `ToggleButton.Indeterminate`
* [Wasm] Add support for `TextBox.IsReadonly`
* [iOS] [WASM] `Path` now supports `LinearGradientBrush` as `Fill`
* A feature flag has been added to change the default preferred placement mode fo the drop down of the `ComboBox` (cf. ../articles/control/ComboBox.md)

### Breaking changes
* `TextBox` no longer raises TextChanged when its template is applied, in line with UWP.
* `TextBox.TextChanged` is now called asynchronously after the UI is updated, in line with UWP. For most uses `TextChanging` should be preferred.
* [Android] `TextBox.IsSpellCheckEnabled = false` is now enforced in a way that may cause issues in certain use cases (see https://stackoverflow.com/a/5188119/1902058). The old behavior can be restored by setting `ShouldForceDisableSpellCheck = false`, per `TextBox`.
* `TextBox.Text = null` will now throw an exception, as on UWP. Pushing `null` via a binding is still valid.
* Projects targeting Android 8 must now use Xamarin.GooglePlayServices.* 60.1142.1 (60.1142.0 has been unlisted)
* Projects targeting Android 9 must now use Xamarin.GooglePlayServices.* 71.1600.0
* [iOS] UIWebView is deprecated and replaced with WKWebView (ITMS-90809: Deprecated API Usage - Apple will stop accepting submissions of apps that use UIWebView APIs . See https://developer.apple.com/documentation/uikit/uiwebview for more information.)
* [iOS] If you set the `ManipulationMode` to something else than `System` or `All`, the [DelaysContentTouches](https://developer.apple.com/documentation/uikit/uiscrollview/1619398-delayscontenttouches) is going to be disabled on all parent `ScrollViewer`
* [#1237] Static resources defined in App.xaml were not processed and registered properly
    > This change might break the compilation for projects that define duplicate resources in other globally accessible resource dictionaries. Adjustments to remove duplicate resources may be necessary.
 * [WASM] The tranform returned by `UIElement.TransformToVisual` is now including scale, rotation or any custom transformation that was declard on a parent element (transform was only including translate components)

### Bug fixes
* [#2186](https://github.com/unoplatform/uno/pull/2186) Fix Canvas Measurement to behave like UWP
* [#2093](https://github.com/unoplatform/uno/pull/2093) Fix missing measurement option for polyline and polygon
* Font size, used for ComboBoxItems, are same as in ComboBox content (not smaller)
* [#2023](https://github.com/unoplatform/uno/pull/2023) Android WebView.NavigateToString doesn't throw exception even when string is very long.
* [#2020](https://github.com/unoplatform/uno/pull/2020) `ContentControl` no longer display the datacontext type when ContentTemplate and content are empty
* [#1987](https://github.com/unoplatform/uno/pull/1987) Missing XML comment warnings are disabled on generated code
* [#1939](https://github.com/unoplatform/uno/pull/1939) Handles nullables types in XAML file generator
* [#1741](https://github.com/unoplatform/uno/issues/1741) On Android, `ApplicationData.Current.[LocalFolder|RoamingFolder]` can now be used in the ctor of App.xaml.cs
    > This change introduces a new constructor in `Windows.UI.Xaml.NativeApplication` that requests a delegate. In the Visual Studio Templates for Uno Platform, the `Main.cs` for the Android, the constructor now provides `() => new App()` instead of `new App()`, you can do the same in your existing application. See [this file](https://github.com/unoplatform/uno/blob/master/src/SolutionTemplate/UnoSolutionTemplate/Droid/Main.cs) for an example.
* [#1767] Invalid `this` keyword generated for `Storyboard.SetTarget`
* [#1781] WASM Images are no longer draggable and selectable by default to match UWP
* [#1771](https://github.com/unoplatform/uno/pull/1771) Fix ".Uno" in project names resulted in build errors.
* [#1531](https://github.com/unoplatform/uno/pull/1531) Fix an issue with VirtualizePanelAdaptater by adding a cache where the ItemSources length change and created a OutOfRangeException
* [WASM] #1518 Fix Navigation Issue Where SystemNavigationManager.enable() is called twice and clear the stack history
* [#1278](https://github.com/unoplatform/uno/pull/1278) the XAML sourcegenerator now always uses the fully qualified type name to prevent type conflicts.
* [#1392](https://github.com/unoplatform/uno/pull/1392) Resolved exceptions while changing cursor color on Android P.
* [#1383](https://github.com/unoplatform/uno/pull/1383) resolve Android compilation errors related to Assets filenames: "Invalid file name: It must contain only"
* [#1380](https://github.com/unoplatform/uno/pull/1380) iOS head generated by Uno Solution Template now specifies MinimumOSVersion, in line with XF so first compile is successful.
* #1276 retrieving non-existent setting via indexer should not throw and  `ApplicationDataContainer` allowed clearing value by calling `Add(null)` which was not consistent with UWP.
* [iOS] Area of view outside Clip rect now allows touch to pass through, this fixes NavigationView not allowing touches to children (#1018)
* `ComboBox` drop down is now placed following a logic which is closer to UWP and it no longer flickers when it appears (especilly on WASM) cf. ../articles/control/ComboBox.md
* #854 `BasedOn` on a `<Style>` in `App.Xaml` were not resolving properly
* #706 `x:Name` in `App.Xaml`'s resources were crashing the compilation.
* #846 `x:Name` on non-`DependencyObject` resources were crashing the compilation
* [Android/iOS] Fixed generated x:uid setter not globalized for Uno.UI.Helpers.MarkupHelper.SetXUid and Uno.UI.FrameworkElementHelper.SetRenderPhase
* Fix invalid XAML x:Uid parsing with resource file name and prefix (#1130, #228)
* Fixed an issue where a Two-Way binding would sometimes not update values back to source correctly
* Adjust the behavior of `DisplayInformation.LogicalDpi` to match UWP's behavior
* [Android] Ensure TextBox spell-check is properly enabled/disabled on all devices.
* Fix ComboBox disappearing items when items are views (#1078)
* [iOS] TextBox with `AcceptsReturn=True` crashes ListView
* [Android/iOS] Fixed Arc command in paths
* Changing the `DataContext` of an element to a new value were pushing the properties default
  value on data bound properties before setting the new value.
* [Android] `.Click` on a `ButtonBase` were not raising events properly
* #1350 Vertical Slider was inverting value when tapped
* TemplateReuse not called when dataContext is set
* [WASM] #1167 Apply `IsEnabled` correctly to `TextBox` (inner `TextBoxView` is now correctly disabled)
* [Android/WASM] Fix MaxLength not respected or overwriting text
* Settings collection-based properties on root node in XAML were leading to C# compilation errors
* Properties on root node in XAML were not applied when there was no content (sub-elements)
* [Android] GroupedListviewHeaders were causing scrolling lag, missing flag
* Flyout that are than anchor but fit in page were defaulting to full placement.
* [iOS]Fixed DatePickerFlyout & TimePickerFlyout not being placed at the bottom
* [Android] Animated content is cut off/glitchy when RenderTransform translation is applied (#1333)
* [#1409](https://github.com/unoplatform/uno/pull/1413) Provide a better error-message on Page-Navigation-Errors
* Fix NRE when using custom `Pivot` templates.
* Fix iOS CompositionTarget handler race condition
* [Wasm] Fix TextBoxView SelectionStart/SelectionEnd value parsing
* [Wasm] Don't fail on FrameworkElement.Dispose()
* [Android] ScrollViewer were no more clipping the scrollable area.
* `ComboBox`'s ControlTemplate was requiring a binding to _TemplatedParent_ for the `x:Name="ContentPresenter"` control. Now aligned with UWP by making this binding in the control itself.
* [#1352](https://github.com/unoplatform/uno/issues/1352) `ThemeResource` bugfixes:
  - `StaticResource` not working inside `ResourceDictionary.ThemeDictionaries`
  - Using a `ThemeResource` on the wrong property type shouldn't raise compile-time error (to align with UWP)
* Fix layout bug in Image control.
* [#1387] `ComboBox`: Fix DataContext was propagated to `<ContentPresenter>` when there was no selected item, causing strange display behavior.
* #1354 fixed Recycler.State desync issue
* #1533 [Wasm] Fix measure caching for zero sized measure
* [iOS(iPad)] `ComboBox` : the combobox wasn't fully expanding vertically on first opening.
* `Popup` & `ComboBox` (and other controls using `Popup`) were not behaving properly when `IsLightDismissable` were set to `true`.
* [Wasm] Fix unloaded UIElements are made visible if measured and arranged
* [Android] Fix java NRE handing touch events on detached view
* [Pivot] Add support for non PivotItem items
* #1557 Fix local DataContext on ContentDialog is overwritten
* [WASM] Fix display for multiple popups (e.g. ComboBox inside of ContentDialog)
* [Android] Fix invalid ImageBrush stack overflow with delayed image reuse
* CommandBar fixes (AppBarToggleButton, AppBarButton)
* Fix Symbols rendering in sample app
* Fix multiple invocations of OnLoaded when hosting a control in ItemsControl
* [Android] Fix glitchy animations inside ListView with transformed ancestor.
* Adjust `AppBar` and `CommandBar` styles.
* Adjust the Stretch mode of `BitmapIcon` content
* Fix invalid Image size constraint
* [Android] MenuFlyout was misplaced if view was in a hierarchy with a RenderTransform
* Fix color refresh of `BitmapIcon` monochrome Foreground
* [IOS] DatePickerFlyout min and max year were resetting to FallbackNullValue
* [Android] Fix bug in `ListView` when using an `ObservableCollection` as its source and using `Header` and `Footer`.
* [#1924](https://github.com/unoplatform/uno/issues/1924) Fix Android `ListView.HeaderTemplate` (and `.FooterTemplate`) binding bug when changing `Header` and `Footer`.
* 164480 [Android] fixed a text wrapping issue caused by layout height desync
* [Wasm] Fix unable to reset `Image.Source` property
* [#2014](https://github.com/unoplatform/uno/issues/2014) Fix iOS Picker for ComboBox not selecting the correct item.
* [iOS] #977 Fix exception when setting MediaPlayerElement.Stretch in XAML.
* #1708 Fix initial Flyout placement and window resize placement
* [Android] #2007 ComboBox does not take Window.VisibleBounds to position its popup
* [Wasm] Fixes the measure of a TextBoxView #2034 #2095
* [Android] [Wasm] Recent clipping improvements were incompleted. Fixed a case where a control was allowed to draw itself to use more than available place in the _arrange_ phase.
* [iOS] Fix negative result value of TimePicker. Now value range is limited from 0 to 1 day
* #2129 WebAssembly Bootstrapper update to remove the implicit .NET 4.6.2 dependency, and support for long file paths on Windows.
* #2147 Fix NRE in android-specific TextBox.ImeOptions
* #2146 [iOS] ListView doesn't take extra space when items are added to collection
* [iOS] Animation might run twice

## Release 1.45.0
### Features
* Add support for `Windows.System.Display.DisplayRequest` API on iOS and Android
* Add support for the following `Windows.System.Power.PowerManager` APIs on iOS and Android:
    - BatteryStatus
    - EnergySaverStatus
    - PowerSupplyStatus
    - RemainingChargePercent
    - PowerSupplyStatusChanged
    - EnergySaverStatusChanged
    - RemainingChargePercentChanged
    - BatteryStatusChanged
* Updated `CheckBox` glyph to match UWP style on all platforms
* Add support for the following `DisplayInformation` properties on iOS and Android:
* Add support for `CurrentInputMethodLanguageTag` and `TrySetInputMethodLanguageTag` on Android, iOS and WASM
* Add support for `ChatMessageManager.ShowComposeSmsMessageAsync` (and `ChatMessage` `Body` and `Recipients` properties) on iOS and Android
* Add support for the following `DisplayInformation` properties on iOS and Android:
    - CurrentOrientation
    - LogicalDpi
    - NativeOrientation
    - RawDpiX
    - RawDpiY
    - ResolutionScale
    - StereoEnabled
    - RawPixelsPerViewPixel
    - DiagonalSizeInInches
    - ScreenHeightInRawPixels
    - ScreenWidthInRawPixels
    - AutoRotationPreferences
* Performance improvements
	- Use `Span<T>` for Grid layout
	- Optimize Wasm text measuring
	- Performance improvements in `TSInteropMarshaller.InvokeJS`
* [Wasm] Improve TextBlock measure performance
* [Wasm] Improve PivotItem template pooling
* 150233 [Android] fixed status-bar, keyboard, nav-bar layout on android
* Add support for Brush implicit conversion (Fixes #730)
* Add `XamlReader` support for top level `ResourceDictionary` (#640)
* Add support for IDictionary objects in XAM (#729)
* Add support for Binding typed property (#731)
* Add support for `RelativeSource.Self` bindings
* 149377 Improve performance of `TimePicker` and `DatePicker` on iOS.
* 145203 [iOS] Support ScrollViewer.ChangeView() inside TextBox
* 150793 [iOS] Add ListView.UseCollectionAnimations flag to allow disabling native insert/delete animations
* 150882 [iOS] Fix visual glitch when setting new RenderTransform on a view
* [Wasm] Add support of hardware/browser back button in `SystemNavigationManager.BackRequested`
* [Wasm] Added support for custom DOM events
* WebAssembly UI tests are now integrated in the CI
* Enable support for macOS head development
* [Wasm] Add NativeXXX styles (which are aliases to the XamlXXX styles)
* [Wasm] Enable persistence for all ApplicationData folders
* [Wasm] Add Samples App UI Screenshots diffing tool with previous builds
* Add `PasswordVault` on supported platform
* [Android] Updated support libraries to 28.0.0.1 for Android 9
* Add support for `x:Load`
* [Wasm] Restore support for `x:Load` and `x:DeferLoadStrategy`
* [Wasm] Scrolling bar visibility modes are now supported on most browsers
* Fix invalid cast exception when using `x:Load` or `x:DeferLoadStrategy`
* Add `Windows.Globalization.Calendar`
* [Wasm] Support of overlay mode of the pane
* Using _State Triggers_ in `VisualStateManager` now follows correct precedence as documented by Microsoft
* Add support for `FlyoutBase.AttachedFlyout` and `FlyoutBase.ShowAttachedFlyout()`
* `x:Bind` now supports binding to fields
* `Grid` positions (`Row`, `RowSpan`, `Column` & `ColumnSpan`) are now behaving like UWP when the result overflows grid rows/columns definition
* [Wasm] Improve TextBlock measure performance
* [Wasm] Improve Html SetAttribute performance
* MenuBar
    - Import of MenuBar code, not functional yet as MenuItemFlyout (Issue #801)
    - Basic support for macOS native system menus
* Ensure FrameworkElement.LayoutUpdated is invoked on all elements being arranged
* Fix Grid.ColumnDefinitions.Clear exception (#1006)
* 155086 [Android] Fixed `AppBarButton.Label` taking precedence over `AppBarButton.Content` when used as `PrimaryCommands`.
* ComboBox
	- Remove dependency to a "Background" template part which is unnecessary and not required on UWP
	- Make sure that the `PopupPanel` hides itself if collapsed (special cases as it's at the top of the `Window`)
	- [iOS] Add support of `INotifyCollectionChanged` in the `Picker`
	- [iOS] Remove the arbitrary `null` item added at the top of the `Picker`
	- [iOS] Fix infinite layouting cycle in the iOS picker (Removed workaround which is no longer necessary as the given method is invoked properly on each measure/arrange phases)
* [Wasm] Refactored the way the text is measured in Wasm. Wasn't working well when a parent with a RenderTransform.
* `Grid` now supports `ColumnDefinition.MinWidth` and `MaxWidth` and `RowDefinition.MinHeight` and `MaxHeight` (#1032)
* Implement the `PivotPanel` measure/arrange to allow text wrapping in pivot items
* [Wasm] Add `PathIcon` support
* Add support UI Testing support through for `Uno.UI.Helpers.Automation.GetDependencyPropertyValue`
* [WASM] ListView - support item margins correctly
* [iOS] Fix items dependency property propagation in ListView items
* [Wasm] Add UI Testing support through for `Uno.UI.Helpers.Automation.GetDependencyPropertyValue`\

### Breaking Changes
* The `WebAssemblyRuntime.InvokeJSUnmarshalled` method with three parameters has been removed.
* `NavigationBarHelper` has been removed.
* Localized Text, Content etc is now applied even if the Text (etc) property isn't set in Xaml. Nested implicit content (e.g. `<Button><Border>...`) will be overridden by localized values if available.
* [Android] Unless nested under `SecondaryCommands`, the `AppBarButton.Label` property will no longer be used for the title of menu item, instead use the `AppBarButton.Content` property. For `SecondaryCommands`, keep using `AppBarButton.Label`.
* The `WordEllipsis` was removed from the `TextWrapping` as it's not a valid value for UWP (And it was actually supported only on WASM) (The right way to get ellipsis is with the `TextTrimming.WordEllipsis`)
* [Android] `Popup.Anchor` is no longer available

### Bug fixes
* DatePicker FlyoutPlacement now set to Full by default
* Semi-transparent borders no longer overlap at the corners on Android
* The `HAS_UNO` define is now not defined in `uap10.0.x` target frameworks.
* The `XamlReader` fails when a property has no getter
* `Click` and `Tapped` events were not working property for `ButtonBase` on Android and iOS.
* 146790 [Android] AndroidUseManagedLoadedUnloaded causes partial item shuffling in ListView
* 150143 [Android] Toggling `TextBox.IsReadOnly` from true to false no longer breaks the cursor
* `WasmHttpHandler` was broken because of a change in the internal Mono implementation.
* 140946 [Android] Upon modifying a list, incorrect/duplicated items appear
* 150489 [Android] PointerCanceled not called on scrolling for views with a RenderTransform set
* 150469 [iOS] Virtualized ListView items don't always trigger their multi-select VisualStates
* 1580172 ToggleSwitch wasn't working after an unload/reload: caused by routedevent's unregistration not working.
* 145203 [Android] Fix overflow on LogicalToPhysicalPixels(double.MaxValue), allowing ScrollViewer.ChangeView(double.MaxValue,...) to work
* 150679 [iOS] Fix path issue with Media Player not being able to play local files.
* Adjust support for `StaticResource.ResourceKey`
* 151081 [Android] Fix Keyboard not always dismissed when unfocusing a TextBox
* [WASM] Support `not_wasm` prefix properly. (#784)
* 151282 [iOS] Fixed Slider not responding on second navigation, fixed RemoveHandler for RoutedEvents removing all instances of handler
* 151497 [iOS/Android] Fixed Slider not responding, by ^ RemoveHandler fix for RoutedEvents
* 151674 [iOS] Add ability to replay a finished video from media player
* 151524 [Android] Cleaned up Textbox for android to remove keyboard showing/dismissal inconsistencies
* Fix invalid code generation for `x:Name` entries on `Style` in resources
* [Wasm] Fix incorrect `TextBlock` measure with constrains
* 151676 [iOS] The keyboard is closing when tap on the webview or toolbar
* 151655 [TimePicker][iOS] First time you open time picker it initializes the existing value to current time
* 151656 [TimePicker][iOS] Time picker always shows +1 minute than selected value
* 151657 [DatePicker][iOS] Date picker flyout displays 1 day earlier than selected value
* 151430 [Android] Prevent touch event being dispatched to invisible view
* Fixed overflow errors in Grid.Row/Column and Grid.RowSpan may fail in the Grid layouter.
* 151547 Fix animation not applied correctly within transformed hierarchy
* Setting the `.SelectedValue` on a `Selector` now update the selection and the index
* [WASM] Fix ListView contents not remeasuring when ItemsSource changes.
* [WASM] Dismissable popup & flyout is closing when tapping on content.
* 145374 [Android] fixed android keyboard stays open on AppBarButton click
* 152504 [Android] Pointer captures weren't informing gestures of capture, fixes Slider capture issue
* 148896 [iOS] TextBlock CarriageReturns would continue past maxlines property
* 153594 [Android] EdgeEffect not showing up on listView that contain Headers and Footers
* #881 [iOS] [Android] Support explicitly-defined ListViewItems in ListView.
* #902 [Android] Resource generation now correctly escapes names starting with numbers and names containing a '-' character
* 154390 Storyboard `Completed` callback were not properly called when there's not children.
* [iOS] Fix bug where Popup can be hidden if created during initial app launch.
* #921 Ensure localization works even if the property isn't defined in XAML
* [WASM] Using x:Load was causing _Collection was modified_ exception.
* Fix support for localized attached properties.
* Fix a potential crash during code generated from XAML, content were not properly escaped.
* #977 Fix exception when setting MediaPlayerElement.Stretch in XAML.
* [Android] Fix MediaPlayerElement.Stretch not applied
* [Android] Fix for ListView elements measuring/layouting bug
* Fix Grid.ColumnDefinitions.Clear exception (#1006)
* [Wasm] Align Window.SizeChanged and ApplicationView.VisibleBoundsChanged ordering with UWP (#1015)
* Add VS2019 Solution Filters for known developer tasks
* #154969 [iOS] MediaPlayer ApplyStretch breaking mediaplayer- fixed
* 154815 [WASM] ItemClick event could be raised for wrong item
* 155256 Fixed xaml generated enum value not being globalized
* 155161 [Android] fixed keyboard flicker when backing from a page with CommandBar
* Fix the processing of the GotFocus event FocusManager (#973)
* 116098 [iOS] The time/day pickers are missing diving lines on devices running firmware 11 and up.
* [iOS] Fix invalid DataContext propagation when estimating ListView item size (#1051)
* RadioButton was not applying Checked state correctly with non-standard visual state grouping in style
* [Android] Fix several bugs preventing AutoSuggestBox from working on Android. (#1012)
* #1062 TextBlock measure caching can wrongly hit
* 153974 [Android] fixed button flyout placement
* Fix support for ScrollBar touch events (#871)
* [iOS] Area of view outside Clip rect now allows touch to pass through, this fixes NavigationView not allowing touches to children (#1018)
* `ComboBox` drop down is now placed following a logic which is closer to UWP and it longer flickers when it appears (especilly on WASM)
* Date and Time Picker Content fix and Refactored to use PickerFlyoutBase (to resemble UWP implementation)
* `LinearGradientBrush.EndPoint` now defaults to (1,1) to match UWP
* [Android] A ListView inside another ListView no longer causes an app freeze/crash
* `Click` on `ButtonBase` was not properly raised.


## Release 1.44.0

### Features
* Add support for `ICollectionView.CopyTo`
* Add support for `ViewBox`
* Add support for `AutoSuggestBox.ItemsSource`
* Add support for `Selector.SelectedValuePath` (e.g. useful for ComboBox)
* Add support for JS unhandled exception logging for CoreDispatcher (support for Mixed mode troubleshooting)
* [WASM] Improve element arrange and transform performance
* Restore original SymbolIcon.SymbolProperty as a C# property
* Add support for `MediaPlaybackList`
* Update Uno.SourceGenerationTasks to improve build performance
    - Move to the latest Uno.SourceGenerationTasks to improve project parsing performance, and allows for the removal of unused targets caused by unoplatform/uno.SourceGeneration#2. Uno.Xaml and Uno.UI.BindingHelpers now only build the required targets.
    - Move to net461 for test projects so the the Uno.Xaml project can be referenced properly
    - Use the latest MSBuild.Sdk.Extras for actual parallel cross-targeted builds
    - Move the nuget package versions to the Directory.Build.targets file so it's easier to change all versions at once.
* Add support for NavigationView Top Navigation
* Adjust `SystemChromeMediumHighColor` to use the Light theme
* Add support for `FrameworkElement.GoToStateCore`
* Adjust `ListView` measure/arrange for dynamic content
* Add some missing default UWP styles
* The `FrameworkElement.IsLoaded` property is now public
* Improve XAML generation error messages for unknown symbols
* Added default console logging for all platforms
* Add support for `Application.OnWindowCreated`
* Added non-throwing stubs for `AutomationProperty`
* Add missing system resources
* Add support for x:Bind in StaticResources (#696)
* Add support for x:Name late binding support to adds proper support for CollectionViewSource in Resources (#696)
* `PointerRelease` events are now marked as handled by the `TextBox`
* `KeyDown` events that are changing the cursor position (left/right/top/bottom/home/end) are now marked as handled by the `TextBox`
* `RoutedEventArgs.IsGenerated` returns `false` as generating events with Uno is not yet supported
* `AutomationPeer.ListenerExists` returns `false` as we cannot generating events with Uno is not yet supported
* `KeyUp` event properly sends `KeyEventArgs` to the controls
* Add ItemsSource CollectionViewSource update support (#697)
* Add support for the `CollectionViewSource.ItemsPath` property
* Fixed support for dots in resource names (#700)
* Add support for `BindingExpression.UpdateSource()`
* Updated Android version to target Android 9.0
* The CI validates for API breaking changes
* Added samples application BenchmarkDotNet support.
* `MediaTransportControls` buttons now use Tapped event instead of Click
* Fixed Pointer capture issues on sliders on iOS

### Breaking changes
* Make `UIElement.IsPointerPressed` and `IsPointerOver` internal
* You will not be able to build projects targeting Android 8.0 locally anymore. Change your Android target to Android 9.0 or replace MonoAndroid90 by MonoAndroid80 in the TargetFrameworks of your projects files.
* 1.43.1 breaking changes rollback to 1.42.0:
    - `ObservableVector<T>` is now internal again
    - `TimePicker.Time` and `TimePicker.MinuteIncrement` are now back for `netstandard2.0`
    - `MediaPlaybackItem.Source` is back as a readonly property
    - `MediaPlaybackList.Items` is back to an `IObservableVector`

### Bug fixes
 * Transforms are now fully functional
 * [Wasm] Fixed ListView infinite loop when using custom containers
 * [Wasm] Use Uno.UI Assembly for namespace type lookup in `XamlReader`
 * [Wasm] Fixed `System.UriConverter` is being linked out
 * 145075 [Android] [Wasm] Android and Wasm don't match all specific UWP behaviors for the Image control.
 * [Wasm] Don't fail if the dispatcher queue is empty
 * 146648 [Android] fixed ListView grouped items corruption on scroll
 * [Wasm] Fix `ListView` recycling when the `XamlParent` is not available for `AutoSuggestBox`
 * 147405 Fix NRE on some MediaTransportControl controls
 * #139 Update Uno.SourceGenerationTasks to improve build performance
 * Update `Uno.UI.Toolkit` base UWP sdk to 17763
 * [Wasm] Fixes items measured after being removed from their parent appear in the visual tree, on top of every other items.
 * [Wasm] Fixes lements may not be removed form the global active DOM elements tracking map
 * [Wasm] Disable the root element scrolling (bounce) on touch devices
 * Fixed invalid iOS assets folder. `ImageAsset` nodes must not be `<Visible>false</Visible>` to be copied to the generated project.
 * Make CollectionViewSource.View a proper DependencyProperty (#697)
 * Fixed support for string support for `Path.Data` (#698)
 * 150018 Fix nullref in `Pivot` when using native style
 * 149312 [Android] Added `FeatureConfiguration.NativeListViewBase.RemoveItemAnimator` to remove the ItemAnimator that crashes when under stress
 * 150156 Fix `ComboBox` not working when using `Popover`.
 * Restore missing ButtonBase.IsPointerOver property

## Release 1.43.1

### Features
* [Wasm] Improve general performance and memory pressure by removing Javascript interop evaluations.
* Add support for Windows 10 SDK 17763 (1809)
* Improve the Uno.UI solution memory consumption for Android targets
* Add support for GridLength conversion from double
* Raise exceptions on missing styles in debug configuration
* Add working ViewBox stub
* `Path.Data` property now invalidates measure and arrange
* Wasm `Image` logs Opened and Failed events
* Add UpToDateCheckInput to avoid VS invalid incremental rebuilds
* 35178 Added recipe for copying text to clipboard
* Added ToogleSwitch documentation in Uno/Doc/Controls.
* Added new properties for ToggleSwitch Default Native Styles.
  [iOS] For BindableUISwitch : TintColorBrush property was added to be able to tint the outline of the switch when it is turned off.
  [Android] For BindableSwitchCompat : - Text property was added in order to change the ToggleSwitch label.
                                       - TextColor property was added in order to change the ToggleSwitch label color.
                                       - ThumbTint property was added in order to change the Thumb color.
                                       - TrackTint property was added in order to change the Track color.
* Samples apps now contain a Unit Tests page
* Added missing resources for NavigationViewItem
* All Nuget and VSIX artifacts are now Authenticode signed
* Resource strings are now loaded from `upri` files for faster resolution
* Add `FeatureConfiguration.Interop.ForceJavascriptInterop` to enable JS Eval fallback in Wasm mode.
* Add support for 1809 NavigationView
* Add support for runtime conversion of global static resources unknown at compile time
* Fixed fallback support for Style property set via ThemeResource
* Add support for multiple resw folders with upri resource generation
* Add support for `ThicknessHelper`
* ResourceLoader adjustments …
  * CurrentUICulture and CurrentCulture are set when setting ResourceLoader .DefaultCulture
  * upri load now ignores resources not used by the current culture
* Add BrushConverter support for Color input
* Add SplitView support for PaneOpened and PaneOpening
* Add CoreApplication.GetCurrentView() Dispatcher and TitleBar stubs support
* Add support for IsItemItsOwnContainer iOS ListView
* Add missing Android Sample App symbols font
* Add SampleControl for Samples app for easier browsing and UI Testing of samples
* Import Border samples
* Improve UIElement inner Children enumeration performance and memory usage
* Add `FeatureConfiguration.FrameworkElement.AndroidUseManagedLoadedUnloaded` to control the native or managed propagation performance of Loaded/Unloaded events through the visual tree
* Raise Application.UnhandledException event on failed navigation
* Adjusts the `Microsoft.NETCore.UniversalWindowsPlatform` version in the UWP head template to avoid assembly loading issues when using the Uno library template in the sample solution.
* [Android] Add support for ListViewItem instances provided via the ItemsSource property
* Added support to disable accessibility feature of larger text on iOS and Android by adjusting the FeatureConfiguration.Font.IgnoreTextScaleFactor flag. Please note that Apple [recommends to keep text sizes dynamic](https://developer.apple.com/videos/play/wwdc2017/245) for a variety of reasons and to allow users to adjust their text size preferences.
* [Wasm] Code for `Path.Stretch` has been moved to `Shape` and works well now for all shapes.
* Add support for `DynamicObject` data binding, to enable support for `Elmish.Uno`.
* Add support for VS2019 VSIX installation
* Improved Xaml generation speed, and incremental build performance
* [Wasm] Fix `CoreDispatcher` `StackOverflowException` when running on low stack space environments (e.g. iOS)
* Add support for `ResourceLoader.GetForViewIndependentUse(string)` and named resource files
* [Wasm] Load events are now raised directly from managed code. You can restore the previous behavior (raised from native) by setting `FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded = false`.
* Updated memory profiling documentation
* Updated default app template iOS GC settings
* Add support for WebAssembly Web Projects
* Add support for WebAssembly debugging with Chrome
* Add support for XAML `x:FieldModifier`
* Add Uno.UI linker definition files
* Adjust FlyoutPresenter default template
* Add support for Flyout anchor
* Improved XAML designer support
* Improved DependencyObject performance under AOT (JS dynCalls for overrides/delegates inside of EH blocks)
* Add support for MatrixTransform, UIElement.TransformToVisual now returns a MatrixTransform
* 140564 [Android] Added workaround for inverted ListView fling issue on Android P

### Breaking changes
* Refactored ToggleSwitch Default Native XAML Styles. (cf. 'NativeDefaultToggleSwitch' styles in Generic.Native.xaml)
  [iOS] For BindableUISwitch : Background property was changed for OnTintColorBrush and Foreground property for ThumbTintColorBrush.
  [Android] BindableSwitch was renamed BindableSwitchCompat in order to avoid confusion with the Switch control.
* Remove invalid Windows.UI.Xaml.Input.VirtualKeyModifiers
* Time picker flyout default styles has been changed to include done and cancel buttons
* DataTemplateSelector implementations are now called using the 2 parameters overload first with a fallback to the 1 parameter overload on null returned value.
  Old behavior could be restored using `FeatureConfiguration.DataTemplateSelector.UseLegacyTemplateSelectorOverload = true`.
* Using "/n" directly in the XAML for a text/content property is not supported anymore in order to match the UWP behavior.
  You can use "&#x0a;" instead in the text/content properties or a carriage return where you need it in the localized resources.
* The `ResourcesGeneration` msbuild target has been renamed to `UnoResourcesGeneration`
  If your csproj is using this target explicily, change it to the new name.

### Bug fixes
 * MediaPlayerElement [iOS] Subtitles are not disable on initial launch anymore
 * MediaPlayerElement [Android]Player status is now properly updated on media end
 * MediaPlayerElement [Android]Fix issue when video metadata reports a width or height of 0
 * #388 Slider: NRE when vertical template is not defined
 * 138117 [Android] Removing a bookmarked/downloaded lesson can duplicate the assets of a different lesson.
 * [Wasm] Fix VirtualizingPanelAdapter measure and arrange
 * 137892 [Android] Fixed FontFamily, FontSize and FontWeight are not applied anymore on the TextBox's content.
 * Don't fail on empty grid ArrangeOverride
 * Don't generate the Bindable attribute if already present
 * Adjust .NET template projects versions to 4.6.1
 * Adjust Microsoft.CodeAnalysis versions to avoid restore conflicts
 * Fix element name matching existing types fails to compile (e.g. ContentPresenter)
 * 138735 [Android] Fixed broken DatePicker
 * Multi-selection Check Boxes in ListViewItems are appearing brielfly (https://github.com/unoplatform/uno/issues/403)
 * 140721 [Android] FlipView not visible when navigating back to page
 * 138537 [iOS] App freezes after State selection causing infinite load on every subsequent launch
 * Fix invalid Border Content type for macOS
 * Don't fail iOS ListView if item Content is null
 * [Wasm] Implement naive refresh for items manipulation in the ListViewBase
 * 3326 [iOS][ItemsControl] ItemsControl in FlipView does not restore items properly
 * Fix NRE in Slider when no template is applied
 * Fix `Frame` does not unset `Page.Frame` when a page is removed
 * Add Wasm PlatformNotSupportedException for System.IO after CoreFX merge in mono
 * Border properties now invalidates measure and arrange on all platforms
 * 141907 [Android] [iOS] The toggle switch is half missing.
 * 142937 [Android] [iOS] Some Button ThemeBrushes are missing.
 * 143527 [Android] Fixed broken TimePicker Flyout on android devices.
 * 143596 [Wasm] Images stretching is incorrect
 * 143595 [Wasm] Wasm ListView Resizing is not working - Limitation: items can't change its size yet, but it's now getting measured/arranged correctly.
 * 143527 [Android] Fixed broken TimePicker Flyout on android devices.
 * 143598 [Wasm] Wasm Animation rotation center is incorrect
 * Fixes invalid parsing of custom types containing `{}` in their value (#455)
 * Add workaround for iOS stackoverflow during initialization.
 * Improve the file locking issues of Uno.UI.Tasks MSBuild task
 * Fix `VisibleBoundsPadding` memory leak
 * [ios] Time picker missing "OK" confirmation button
 * #87 / 124046 ComboBox incorrect behavior when using Items property
 * [Wasm] ComboBox wasn't working anymore since few versions
 * Fix memory leak with defining event handlers in XAML documents
 * Fix memory leak in `CommandBar`
 * Fix memory leak when using `x:Name` in XAML documents
 * 143170 [iOS] [WatermarkedDatePicker] When the Maxyear boundary is reached the first time, the calendar goes back two days instead of one
 * #491 DataTemplateSelector.SelectTemplate is not called on iOS and Android. The behavior is now closer to UWP.
 * 144268 / #493 : Resources outside of 'en' folder not working
 * Support for duplicate XAML `AutomationProperties.Name`
 * `ListViewBase.SelectedItems` is updated on selection change in Single selection mode
 * #528 ComboBoxes are empty when no datacontext
 * Ensure that Uno.UI can be used with VS15.8 and earlier (prevent the use of VS15.9 and later String APIs)
 * [Android] Listview Items stay visually in a pressed state,(can click multiple) when you click then scroll down, click another item, and scroll back up
 * 144101 fixed `ListView` group headers messed up on item update
 * #527 Fix for `Selector.SelectionChanged` is raised twice on updated selection
 * [iOS] Add fail-safe on `FrameworkElement.WillMoveToSuperview` log to `Application.Current.UnhandledException`
 * Flyout were not presented correctly on Wasm

## Release 1.42

### Features
* Add base infrastructure platform for macOS
* 136259 Add a behavior so that tap makes controls fade out
* 135985 [Android], [iOS] ListViewBase Support [MultiSelectStates](https://msdn.microsoft.com/en-us/library/windows/apps/mt299136.aspx?f=255&MSPPError=-2147217396) on ListViewItem. This allows the item container to visually adapt when multiple selection is enabled or disabled.
* #325 Add support for `NavigationView` control
* Add support for `SymbolIcon` control for WebAssembly
* Add support for `UIElement.Clip` for WebAssembly
* Add support for inner-Uno.UI strings localization
* Add stubs for RichTextBlock
* Add `BitmapIcon` support
* Add `BitmapIcon.ShowAsMonochrome` support
* Add support for `Windows.Foundation.UniversalApiContract` in `IsApiContractPresent`
* Add support for ContentProperty on UserControl
* Add `DelegateCommand<T>`
* #131258 Added support for _RoutedEvents_. See [routed-events.md documentation](../articles/routed-events.md).
* [WASM] #234 Support virtualization in ListView

### Breaking changes
* 132002 [Android] The collapsible button bar is now taken into account by visible bounds calculation. Apps which use VisibleBoundsPadding or have command bars will therefore see an adjustment to the height of their windows on Android.

### Bug fixes
 * 135258 [Android] Fixed ImageBrush flash/flickering occurs when transitioning to a new page for the first time.
 * 131768 [iOS] Fixed bug where stale ScrollIntoView() request could overwrite more recent request
 * 136092 [iOS] ScrollIntoView() throws exception for ungrouped lists
 * 136199 [Android] TextBlock.Text isn't visually updated if it changes while device is locked
 * Fix Android and iOS may fail to break on breakpoints in `.xaml.cs` if the debugging symbol type is Full in projects created from templates
 * 136210 [Android] Path is cut off by a pixel
 * 132004 [Android] Window bounds incorrect for screen with rounded corners
 * #312 [Wasm] Text display was chopped on Wasm.
 * 135839 `WebView` No longer raises NavigationFailed and NavigationCompleted events when navigation is cancelled on iOS.
 * 136188 [Android] Page elements are aligned differently upon back navigation
 * 136114 [iOS] Image inside Frame doesn't respond to orientation changes
 * Fix crash when a `VisualState` does not have a valid `Name`
 * Adjust compiled binding application ordering when loading controls
 * Ensure the SplitView templated parent is propagated properly for FindName
 * Fix infinite loop when parsing empty Attached Properties on macOS
 * 137137 [iOS] Fixed `DatePickerSelector` not propagating coerced initial value
 * 103116 [iOS] Navigating to a _second_ local html file with `WebView` doesn't work.
 * 134573 CommandBar doesn't take the proper space on iOS phones in landscape
 * Image with partial size constraint now display properly under Wasm.
 * 138297 [iOS][TextBlock] Measurement is always different since we use Math.Ceiling
 * 137204 [iOS] ListView - fix bug where item view is clipped
 * 137979 [Android] Incorrect offset when applying RotateTransform to stretched view
 * Now supports internal object in desource dictionaries
 * 134573 CommandBar doesn't take the proper space on iOS phones in landscape
 * #26 The explicit property <Style.Setters> does not initialize style setters properly
 * 104057 [Android] ListView shows overscroll effect even when it doesn't need to scroll
 * #376 iOS project compilation fails: Can't resolve the reference 'System.Void Windows.UI.Xaml.Documents.BlockCollection::Add(Windows.UI.Xaml.Documents.Block)
 * 138099, 138463 [Android] fixed `ListView` scrolls up when tapping an item at the bottom of screen
 * 140548 [iOS] fixed `CommandBar` not rendering until reloaded
 * [147530] Add a missing `global::` qualifier in the `BindableMetadataGenerator`
 * [WASM] Add workaround for mono linker issue in AOT mode in `ObservableVectorWrapper`

## Release 1.41

### Features

* [#154](https://github.com/unoplatform/uno/issues/154) Implement the MediaPlayerElement control
* 135799 Implemented MediaPlayer.Dispose()

### Bug fixes

 * 129762 - Updated Android SimpleOrientationSensor calculations based on SensorType.Gravity or based on single angle orientation when the device does not have a Gyroscope.
 * 134189 [iOS] The Time Picker flyout placement is not always respected
 * 134132 [Android] Fix loading of ItemsPresenter
 * 134104 [iOS] Fixed an issue when back swiping from a page with a collapsed CommandBar
 * 134026 [iOS] Setting a different DP from TextBox.TextChanging can cause an infinite 'ping pong' of changing Text values
 * 134415 [iOS] MenuFlyout was not loaded correctly, causing templates containing a MenuFlyout to fail
 * 133247 [iOS] Image performance improvements
 * 135192 [iOS] Fixed ImageBrush flash/flickering occurs when transitioning to a new page.
 * 135112 [Android] Fix crash in UpdateItemsPanelRoot() in the ItemsControl class.
 * 132014, 134103 [Android] Set the leading edge considering header can push groups out off the screen
 * 131998 [Android] Window bounds set too late
 * 131768 [iOS] Improve ListView.ScrollIntoView() when ItemTemplateSelector is set
 * 135202, 131884 [Android] Content occasionally fails to show because binding throws an exception
 * 135646 [Android] Binding MediaPlayerElement.Source causes video to go blank
 * 136093, 136172 [iOS] ComboBox does not display its Popup
 * 134819, 134828 [iOS] Ensures the back gesture is enabled and disabled properly when the CommandBar is visible, collapsed, visible with a navigation command and collapsed with a navigation command.
 * 137081 Xaml generator doesn't support setting a style on the root control
 * 148228 [Android] Right theme (clock or spinner) is selected for specific time increments
 * 148229 [Android] Right time is picked and rounded to nearest time increment in clock mode
 * 148241 [Android] won't open if `MinuteIncrement` is not set
 * 148582 Time picker initial time when using time increment is using initial time seconds when rounding.. it should ignore seconds..
 * 148285 [iOS] TimePicker is clipped off screen when ios:FlyoutPlacement isn't set

## Release 1.40

This release is the first non-experimental release of the Uno Platform since the initial public version in May 2018. Lot of bug fixes and features have been added since then, and lots more are coming.

A lot of those changes where included to support these libraries : [MVVMLight](https://github.com/unoplatform/uno.mvvmlight), [ReactiveUI](https://github.com/unoplatform/uno.ReactiveUI), [Prism](https://github.com/unoplatform/uno.Prism), [Rx.NET](https://github.com/unoplatform/uno.Rx.NET), [Windows Community Toolkit](https://github.com/unoplatform/uno.WindowsCommunityToolkit), [Xamarin.Forms UWP](https://github.com/unoplatform/uno.Xamarin.Forms).

Here are some highlights of this release:

- General improvement in the memory consumption of the `ListView` control
- Many Wasm rendering and support updates
    - Invalid images support
    - Text and images measuring fixes
    - Add support for AppManifest.displayName
- Support for the `Pivot` control
- Support for inline XAML event handlers in `DataTemplate`
- Support for implicit styles in the `XamlReader`
- Support for `ThreadPoolTimer`
- Add support for implicit `bool` to `Visibility` conversion
- Support for `AutoSuggestBox`
- SourceLink, Reference Assemblies and deterministic builds are enabled
- Support for `x:Name` reference in `x:Bind` markup
- Support for `WriteableBitmap` for all platforms
- Added support for `Cross-platform Library` template in vsix
- Added support for `StaticResource` as top level `ResourceDictionary` element
- Added support for `AutomationPeer`
- Android status bar height is now included in `Window.Bounds`
- Add support for `Underline` in `HyperLinkButton`
- Add support for TextBlock.TextDecorations
- TextBlock base class is now `FrameworkElement` on iOS, instead of `UILabel`
- Auto generated list of views implemented in Uno in the documentation
- Add support for string to `Type` conversion in XAML generator and binding engine
- Support for Attached Properties localization
- Added `ItemsControl.OnItemsChanged` support
- Added support for ListView GroupStyle.HeaderTemplateSelector for iOS/Android

Here's the full change log:

- Fixes for VisualTransition.Storyboard lazy bindings [#12](https://github.com/unoplatform/uno/pull/12)
- ListView fixes [#22](https://github.com/unoplatform/uno/pull/22)
    - Improve Path parser compatibility
    - Update assets generation documentation
    - Fix ItemsWrapGrid layout when ItemHeight/ItemWidth are not set
    - Adjust for invalid AnchorPoint support for iOS (#16)
    - Fix for ListView initialization order issue
- Default styles clearing fixes [#23](https://github.com/unoplatform/uno/pull/23)
- Compatibility and stability fixes [#37](https://github.com/unoplatform/uno/pull/37)
    - Wasm SplitView fixes
    - Enum fast converters
    - TextBox InputScope fixes
    - Improved ListViewBase stability
    - SimpleOrientationSensor fixes
    - PathMarkupParser: Add support for whitespace following FillRule command
    - Fix DependencyObjectStore.PopCurrentlySettingProperty
    - Raised navigation completed after setting CanGoBack/Forward
    - Fix layouting that sometimes misapplies margin
    - Selector: Coerce SelectedItem to ensure its value is always valid
    - Remove legacy panel default constructor restriction
    - Wasm image support improvements
    - Add support for forward slash in image source
    - Add support for CollectionViewSource set directly on ItemsControl.ItemSource
    - Fix Pane template binding for SplitView
    - Add support for Object as DependencyProperty owner
    - Add Wasm support for UIElement.Tapped
    - Fix iOS UnregisterDoubleTapped stack overflow
- Compatibility and stability fixes [#43](https://github.com/unoplatform/uno/pull/43)
    - Adjust WASM thickness support for children arrange
    - Fix support for inline text content using ContentProperty
    - Fix memory leaks in ScrollViewer
    - Adjust for missing styles in UWP Styles FeatureConfiguration
    - Fix for Source Generation race condition on slow build servers
- Compatibility and stability fixes [#53](https://github.com/unoplatform/uno/pull/53)
    - Adjust for WASM Support for local images [#1](https://github.com/unoplatform/uno/issues/1)
    - Fixes x:Bind support for Wasm
    - Fix invalid deserialization of ApplicationDataContainer for iOS
    - Fix error for ApplicationView.Title for WASM
    - Remove glib conversion errors in WASM
- UWP API Alignments for Wasm [#70](https://github.com/unoplatform/uno/pull/70)
    - Add support for Application.Start() to provide a proper SynchronizationContext for error management
    - Fix for ImplicitStyles support in XamlReader
    - Add support for the Pivot control using the default UWP Xaml style
    - Adjust body background color after the splash screen removal
    - Adjust the materialization of Control templates to not be lazy
- Add support for Xaml file defined event handlers [#71](https://github.com/unoplatform/uno/pull/71)
- API Compatibility Updates [#75](https://github.com/unoplatform/uno/pull/75)
    - Add support for implicit bool to Visibility conversion
    - Fix default Style constructor does not set the proper property precedence
    - Add more DependencyObjectStore logging
    - Align ItemsControl.Items behavior with UWP (#34)
    - Fix invalid uri parsing when set through BitmapImage.UriSource
- [WASM] Fix text measure when not connected to DOM [#76](https://github.com/unoplatform/uno/pull/76)
- Pivot, AutoSuggestBox, TextBox, XamlReader updates [#77](https://github.com/unoplatform/uno/pull/77)
    - Added missing TransformGroup ContentProperty
    - Fixed invalid namespace attribution of attached properties in XamlReader
    - Fixed BitmapImage.UriSource updates not being applied on Wasm
    - Add basic implementation of AutoSuggestBox
    - Fixed focus stealing issues with inactive PivotItem content
    - Add ThreadPoolTimer support
    - Fix for iOS popup not appearing
    - Fix for Wasm textbox not properly updating while not loaded
- [WASM] Add support for TextBlock.Padding property [#88](https://github.com/unoplatform/uno/pull/88)
- [WASM] Fixed measuring support with Polyfill for Node.isConnected [#89](https://github.com/unoplatform/uno/pull/88), [#91](https://github.com/unoplatform/uno/pull/91)
- Misc fixes [#93](https://github.com/unoplatform/uno/pull/93)
    - Fixed iOS `SimpleOrientationSensor` default queue management
    - Fixed multiple memory leaks in `ListView`, `ScrollViewer`
    - Implemented `CacheLength` for Android `ListViewBase`
    - Fixed for `DependencyObject` properties inheritance race condition
    - Fix for empty Path reporting an infinite size
    - Fix Title  not appearing in CommandBar
- Add support for WebAssembly AppManifest.displayName [#94](https://github.com/unoplatform/uno/pull/94)
- Enable SourceLink, Reference Assemblies, Deterministic build [#100](https://github.com/unoplatform/uno/pull/100)
- Binding Engine Alignments [#113](https://github.com/unoplatform/uno/pull/113)
    - Use Portable symbols for Xamarin debugging stability
    - Enable x:Name reference in x:Bind markup. This requires for a failed BindableMetadata lookup to fall through reflection lookup.
    - Assume ".Value" binding path on a primitive is equivalent to self, to enable nullable bindings.
    - Adjust unit tests logging
    - Enables auto "LogicalChild" treatment to allow for DependencyObjectCollection members to be databound
    - Enable parent reset for "LogicalChild" assignations
- Implement the CoreWindow.Dispatcher property [#117](https://github.com/unoplatform/uno/pull/117)
- Misc Fixes [#120](https://github.com/unoplatform/uno/pull/120)
    - Fix for CommandBar back button icon
    - Improve HyperLinks hit-testing for iOS
    - Fixed android PaintDrawable opacity
    - Adjust Unloaded event for ToggleButton
    - Adjust for brightness support
    - Adjust touch support for rotated elements
    - Adjust MinWidth/MinHeight support in Grid
    - Adjust PasswordBox custom font for during password reveal
    - ListView, ContentControl memory improvements
    - Style behavior adjustments
- Update for android animation reliability [#123](https://github.com/unoplatform/uno/pull/123)
- Add support for WriteableBitmap [#125](https://github.com/unoplatform/uno/pull/125)
- Updated vsix structure [#128](https://github.com/unoplatform/uno/pull/128)
- Multiple enhancements for WCT 4.0 [#131](https://github.com/unoplatform/uno/pull/131)
    - Adds support for `IconElement` fast conversion
    - Adds stubs for `ToggleSwitchTemplateSettings`, `PackageId`, `UISettings`
    - Adjust `XamlObjectBuilder` logging
    - Add implicit conversion for `KeyTime` and `Duration`
    - Add support for top level `StaticResource` resource dictionary elements
    - Implement FindFirstParent for net46/netstd2.0
    - Adds ElementNotAvailableException and ElementNotEnabledException
    - Fix invalid measure for empty wasm images
    - Add size/rect checks for measure/arrange wasm
    - Improve XamlReader error reporting
- Add support for Cross-platform library template in VSIX [#132](https://github.com/unoplatform/uno/pull/132)
- Add support for AutomationPeer [#141](https://github.com/unoplatform/uno/pull/141)
- Improved support for UWP resources [#149](https://github.com/unoplatform/uno/pull/149)
    - Projects no longer need to define `XamlCodeGenerationFiles` (fixes #144)
    - Projects no longer need to define `ResourcesDirectory` (fixes #106)
    - Projects no longer need to initialize `ResourceHelper.ResourcesService` (fixes #142)
    - `ResourceLoader.GetString` is now supported (fixes #142)
- Updates rollup [#151](https://github.com/unoplatform/uno/pull/151)
    - Fixed `VisualState` not updated when `TextBox` is focused
    - Improve `ListView` and `Selector` memory footprint
    - Adjust GenericStyles application sequence for Android
    - Add diagnostics methods for `BinderReferenceHolder`
    - Include android status bar height in `Window.Bounds`
    - Fixed `Grid` items size when `MinHeight` and `MinHeight` are used
    - Fixed android race condition during visual tree cleanup
    - Add support for underline in `HyperLinkButton`
    - Fixed `ScrollContentPresenter` margin issue
    - Adjust `MessageDialog` behavior for android
    - `ContentControl` Data Context is now properly unset
    - Add `EmailNameOrAddress` InputScope for `TextBox`
    - Fixed duplicated resw entry support
    - Fixed `ComboBox` popup touch issue
    - Add support for TextBlock.TextDecorations
    - TextBlock base class from UILabel to FrameworkElement
- Auto-generate list of views implemented in Uno [#152](https://github.com/unoplatform/uno/pull/152)
- Add support for string to `Type` conversion in Xaml generator and Binding engine. [#159](https://github.com/unoplatform/uno/pull/159)
- Add support for attached properties localization [#156](https://github.com/unoplatform/uno/pull/156)
- Added `ItemsControl.OnItemsChanged` support [#175](https://github.com/unoplatform/uno/pull/175)
- Added support for XAML inline collections declaration [#184](https://github.com/unoplatform/uno/pull/184)
- Adjust the sequence of control template materialization [#192](https://github.com/unoplatform/uno/pull/192)
- Support for ListView.ScrollIntoView with leading alignment
- Added support for ListView GroupStyle.HeaderTemplateSelector
- [IOS-ANDROID] Added support for time picker minute increment
