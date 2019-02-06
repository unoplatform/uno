# Release notes

## Next version

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
* Add support for `ICollectionView.CopyTo`
* Add support for MatrixTransform, UIElement.TransformToVisual now returns a MatrixTransform
* Add support for `ViewBox`
* Add support for `AutoSuggestBox.ItemsSource`
* Add support for `Selector.SelectedValuePath` (e.g. useful for ComboBox)

### Breaking changes
* Refactored ToggleSwitch Default Native XAML Styles. (cf. 'NativeDefaultToggleSwitch' styles in Generic.Native.xaml)
  [iOS] For BindableUISwitch : Background property was changed for OnTintColorBrush and Foreground property for ThumbTintColorBrush.
  [Android] BindableSwitch was renamed BindableSwitchCompat in order to avoid confusion with the Switch control.
* Remove invalid Windows.UI.Xaml.Input.VirtualKeyModifiers
* Time picker flyout default styles has been changed to include done and cancel buttons
* DataTemplateSelector implementations are now called using the 2 parameters overload first with a fallback to the 1 parameter overload on null returned value.
  Old behavior could be restored using `FeatureConfiguration.DataTemplateSelector.UseLegacyTemplateSelectorOverload = true`.

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
 * Multi-selection Check Boxes in ListViewItems are appearing brielfly (https://github.com/nventive/Uno/issues/403)
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
 * Transforms are now fully functionnal
 * #527 Fix for `Selector.SelectionChanged` is raised twice on updated selection
 * [Wasm] Fixed ListView infinite loop when using custom containers

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
* Add DelegateCommand<T>
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
 * #26 The explicit property <Style.Setters> does not intialize style setters properly
 * 104057 [Android] ListView shows overscroll effect even when it doesn't need to scroll
 * #376 iOS project compilation fails: Can't resolve the reference 'System.Void Windows.UI.Xaml.Documents.BlockCollection::Add(Windows.UI.Xaml.Documents.Block)
 * 138099, 138463 [Android] fixed `ListView` scrolls up when tapping an item at the bottom of screen
 * 140548 [iOS] fixed `CommandBar` not rendering until reloaded

## Release 1.41

### Features

* [#154](https://github.com/nventive/Uno/issues/154) Implement the MediaPlayerElement control
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
