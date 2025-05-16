---
uid: Uno.Features.FocusManagement
---

# Focus Management

## Programmatic focus

Support for programmatic focus is fully implemented on all Uno Platform targets and matches the logic provided by WinUI. To use programmatic focus, utilize the `Microsoft.UI.Xaml.Input.FocusManager` class and its static methods. For detailed documentation on its methods check the comments provided by IntelliSense in Visual Studio, see [FocusManager source code](https://github.com/unoplatform/uno/blob/master/src/Uno.UI/UI/Xaml/Input/FocusManager.cs), or the official [WinUI documentation](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.input.focusmanager).

## Specifying visual tree for focus operations

`FocusManager` class is `static`, but can operate in multi-window environment. To handle this correctly, some methods provide overloads allowing the user to specify a `XamlRoot` or `UIElement` the operation should be executed against. The overloads that do not have this capability will throw an exception.

- Use `GetFocusedElement(XamlRoot)` instead of `GetFocusedElement()`.
- Use `TryMoveFocus` overload with `FindNextElementOptions` instance which has `SearchRoot` set to an `UIElement` in the visual tree or `XamlRoot.Content`.
- Use `TryMoveFocusAsync` overload with `FindNextElementOptions` instance which has `SearchRoot` set to an `UIElement` in the visual tree or `XamlRoot.Content`.
- Use `FindNextElement` overload with `FindNextElementOptions` instance which has `SearchRoot` set to an `UIElement` in the visual tree or `XamlRoot.Content`.
- Use the aforementioned `FindNextElement` overload instead of `FindNextFocusableElement`.
- Always provide an element in the visual tree to `FindFirstFocusableElement` and `FindLastFocusableElement`.

> [!NOTE]
> `SearchRoot` in `FindNextElementOptions` is the root from which the next focus candidate to receive navigation focus is identified. Therefore, the methods will behave differently based on which element is used.

## Keyboard focus

Keyboard focus handling support is generally available on all targets except iOS. On iOS you can opt-in to enable experimental support by setting the related flag:

```csharp
#if __IOS__
WinRTFeatureConfiguration.Focus.EnableExperimentalKeyboardFocus = true;
#endif
```

The feature requires additional testing to verify all edge cases on Android and iOS. In a future release, we will switch the experimental support to be enabled by default.

## Disabling initial focus on Page

The focus management logic in WinUI sets initial focus on `Page` that is being loaded (for example, during navigation) if no element in the app is currently focused. This may cause an input element like `TextBox` to get focused automatically. This may not be a desirable behavior, though, as it will cause the virtual keyboard on mobile platforms to open up. To avoid this initial focus, please set `IsTabStop` of the `Page` to `false`:

```xaml
<Page
   ...
   IsTabStop="False">
   ...
</Page>
```

Alternatively, you can set focus explicitly to some other UI element by calling `element.Focus(FocusState.Programmatic)` in the code-behind - ideally in the `Loaded` event handler or in `OnNavigatedTo` override.
