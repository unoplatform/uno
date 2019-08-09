# Uno Support for Windows.UI.StartScreen

## `JumpList` &amp; `JumpListItem`

The APIs are implemented on iOS and Android. Other platforms return `false` when calling the `JumpList.IsSupported()` method.

`JumpListItem` supports `DisplayName`, `Description`, `Arguments` and `Logo` properties on both Android and iOS.

`Logo` property can be initialized only with `ms-appx:`-based images. This behavior matches UWP and actually matches native support on both platforms as well.

To handle `JumpListItem` activation, check the `LaunchActivatedEventArgs.Arguments` in `App.OnLaunched` method. Note, that when the application is running, the method will still be called again (this behavior matches UWP).

The API supports interoperability with native "app shortcuts". This means items you add using native API instead of `JumpList` API will not be overwritten by `JumpList` and will not be shown in `JumpList.Items`. To identify Uno-specific app shortcuts, a `UnoShortcut` key is used and set in `ShortcutInfo.Extras` on Android and `UIApplicationShortcutItem.UserInfo` on iOS.