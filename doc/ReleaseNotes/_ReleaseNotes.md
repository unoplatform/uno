# Release notes

## Next version

### Features
* 136259 Add a behavior so that tap makes controls fade out
* 135985 [Android], [iOS] ListViewBase: Support [MultiSelectStates](https://msdn.microsoft.com/en-us/library/windows/apps/mt299136.aspx?f=255&MSPPError=-2147217396) on ListViewItem. This allows the item container to visually adapt when multiple selection is enabled or disabled.

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






