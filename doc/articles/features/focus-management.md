# Focus Management

## Programmatic focus

Support for programmatic focus is fully implemented on all Uno Platform targets and matches the logic provided by WinUI. To use programmatic focus, utilize the `Windows.UI.Xaml.Input.FocusManager` class and its static methods. For detailed documentation on its methods check the comments provided by IntelliSense in Visual Studio, the source code [here](https://github.com/unoplatform/uno/blob/master/src/Uno.UI/UI/Xaml/Input/FocusManager.cs), or the official [WinUI documentation](https://docs.microsoft.com/en-us/uwp/api/Windows.UI.Xaml.Input.FocusManager).

## Keyboard focus

Keyboard focus handling support is generally available on all targets except iOS and Android. On iOS and Android you can opt-in to enable experimental support by setting the related flag:

```
#if __IOS__ || __ANDROID__
WinRTFeatureConfiguration.Focus.EnableExperimentalKeyboardFocus = true;
#endif
```

The feature requires additional testing to verify all edge cases on Android and iOS. In a future release, we will switch the experimental support to be enabled by default.
