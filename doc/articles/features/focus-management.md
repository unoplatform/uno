---
uid: Uno.Features.FocusManagement
---

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

### Android keyboard focus highlighting

To disable the native Android highlighting of focused elements when keyboard navigation is used, the `styles.xml` file needs to be updated to make the highlight transparent:

```xml
<item name="android:colorControlHighlight">@android:color/transparent</item>
```


## Disabling initial focus on Page

The focus management logic in UWP/WinUI sets initial focus on `Page` that is being loaded (for example during navigation) if no element in the app is currently focused. This may cause an input element like `TextBox` to get focused automatically. This may not be a desirable behavior though, as it will cause the virtual keyboard on mobile platforms to open up. To avoid this initial focus, please set `IsTabStop` of the `Page` to `false`:

```xaml
<Page
   ...
   IsTabStop="False">
   ...
</Page>
```

Alternatively you can set focus explicitly to some other UI element by calling `element.Focus(FocusState.Programmatic)` in the code-behind - ideally in the `Loaded` event handler or in `OnNavigatedTo` override.
