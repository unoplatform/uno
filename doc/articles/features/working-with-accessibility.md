# Accessibility

Windows uses the UI Automation framework to provide accessibility information to screen readers.

> Microsoft UI Automation is an accessibility framework for Windows. It provides programmatic access to most UI elements on the desktop. It enables assistive technology products, such as screen readers, to provide information about the UI to end users and to manipulate the UI by means other than standard input. UI Automation also allows automated test scripts to interact with the UI. <br/> [UI Automation Overview](https://docs.microsoft.com/en-us/windows/desktop/WinAuto/uiauto-uiautomationoverview)

Uno.UI implements a subset of UWP's UI Automation APIs, to make your applications work with each platform's built-in screen reader:

| Windows  | Android  | iOS       |
|----------|----------|-----------|
| Narrator | TalkBack | VoiceOver |

Read [this guide](https://docs.microsoft.com/en-us/windows/uwp/design/accessibility/basic-accessibility-information) to learn how to use the `AutomationProperties` supported by Uno.UI:

- `AutomationProperties.AutomationId`
- `AutomationProperties.Name`
- `AutomationProperties.LabeledBy`
- `AutomationProperties.AccessibilityView`

## SimpleAccessibility mode

While we were trying to replicate UWP's behavior on iOS and Android, we realized that iOS doesn't allow nested accessible elements to be focused. 
For example, if you select a list item, the screen reader will automatically read the accessible name of all inner elements one after the other, but won't let you focus them individually (unlike Windows and Android).

While this behavior comes with its own set of limitations (e.g., you can't nest buttons), it greatly simplifies the implementation of accessibility. 
By comparison, on UWP, the user would need to navigate through every inner element of every list item, unless the developer manually disables focus for each inner element and aggregate their accessible names into a single string to use as the accessible name of the list item.

Instead of trying to replicate UWP's behavior on iOS (which *might* be doable using the `UIAccessibilityContainer` interface, although we haven't tried it yet), we decided to go along with the iOS behavior and bring it to Android as well. We called this mode SimpleAccessibility.

Here's how to enable it:

```csharp
// App's constructor (App.xaml.cs)
#if __IOS__ || __ANDROID__
FeatureConfiguration.AutomationPeer.UseSimpleAccessibility = true;
#endif
```

We highly recommend using this mode, as iOS still won't let you focus nested accessible elements even if you don't (see known issues).

##Disabling accessibility text scaling

You have the option to disable accessibility text scaling of iOS and Android devices but Apple [recommends to keep text sizes dynamic](https://developer.apple.com/videos/play/wwdc2017/245) 

Here's how to disable it
```csharp
// App's constructor (App.xaml.cs) 
Uno.UI.FeatureConfiguration.Font.IgnoreTextScaleFactor= true; 
```

## AutomationId
The `AutomationProperties.AutomationId` attached property can be used by UI Testing frameworks to find visual elements.

To avoid performance issues, this property only has an effect when the `IsUiAutomationMappingEnabled` msbuild property is set, or `Uno.UI.FrameworkElementHelper.IsUiAutomationMappingEnabled` is set to true.

Setting this property does the following:
- On iOS, it sets the [`UIView.AccessibilityIdentifier`](https://developer.apple.com/documentation/uikit/uiaccessibilityidentification/1623132-accessibilityidentifier) property
- On Android, it sets the [`View.ContentDescription`](https://developer.android.com/reference/android/view/View.html#setContentDescription(java.lang.CharSequence)) property
- On Android, it sets the [`View.ContentDescription`](https://developer.android.com/reference/android/view/View.html#setContentDescription(java.lang.CharSequence)) property
- On WebAssembly, it sets the `xamlautomationid` property on the HTML element.

This property is generally used alongside [Uno.UITest](https://github.com/unoplatform/Uno.UITest) to create UI Tests, and is particularly useful to select items using databound identifiers.

## Known issues

- `Hyperlink` in `TextBlock` is not supported.
- `TextBox` and `PasswordBox` don't use `Header` and `PlaceholderText`.
- `ItemsControl` doesn't use `AutomationProperties.Name` on its `DataTemplate`'s root.
- There are XAML code generation conflicts between `x:Name` and `AutomationProperties.Name`.
- A child with the same accessible name as its parent is accessibility focusable.
- `Control` doesn't receive focus when accessibility focused.
- `TabIndex` is not supported.
- [iOS] Nested accessible elements are not accessibility focusable.
- [Android] Both `ToggleSwitch` and its native `Switch` can be accessibility focused.
- [Android] Both `TextBox` and its native `EditText` can be accessibility focused.
- [Android] `Control` without a non-empty accessible name is not accessibility focusable.
- [Android] The accessible name of the native back button can't be customized.
- [Android] `TextBox` and `PasswordBox` don't hint "double tap to edit".
- [Android] You can tap to focus an accessible child of an accessible element that has never been focused even if `UseSimpleAccessibility` is enabled.
- `AutomationPeer.GetLocalizedControlType()` is not localized.
- `FlipViewItem` reads "double tap to activate" even if it's not interactive.
- Navigating through accessible elements can slow down when the UI is complex.
- You can't cycle through `FlipView` items using accessibility focus.
- There is no way to control or predict accessibility focus following a navigation (page or modal).
- `Control`s don't announce state changes.
- Only the following elements support accessibility:
  - `Button`
  - `CheckBox`
  - `FlipViewItem`
  - `ListViewItem`
  - `HyperlinkButton`
  - `Image`
  - `PasswordBox`
  - `RadioButton`
  - `TextBlock`
  - `TextBox`
  - `ToggleButton`
  - `ToggleSwitch`
- Only the following `AutomationProperties` are supported:
  - `AccessibilityView`
  - `LabeledBy`
  - `Name`

## Tips

- Always set and localize `AppBarButton.Label` (even if it's not displayed on Android and iOS). It is used by the screen reader for accessibility.
- Always localize `AutomationProperties.Name`. The name of the resource should look like this: `MyButton.[using:Windows.UI.Xaml.Automation]AutomationProperties.Name`.
- Avoid using `Opacity="0"` and `IsHitTestVisible="False"` when you can use `Visibility="Collapsed"`. The screen reader can still focus the former, but not the latter.
- Avoid stacking `TextBlock`s inside  a `Panel` when you can use `Inlines` inside a `TextBlock` (using `LineBreak` if necessary). This allows the screen reader to read all the text at once, instead of having the user select every part manually.
- Use a converter to trim long text. While a `TextBlock` might ellipsize long text, the screen reader will read the entire text provided.
- Avoid creating custom controls when you can use built-in ones. If you must, make sure to implement and provide an appropriate `AutomationPeer`.
- You can disable accessibility focus of native elements using `android:ImportantForAccessibility="No"` and `ios:IsAccessibilityElement="False"`.

## Enabling the screen reader

### Narrator (Windows)

1. Press **Windows key** and **Enter** at the same time.

### VoiceOver (iOS)

1. Launch the **Settings** app from your Home screen.
2. Tap on **General**.
3. Tap on **Accessibility**.
4. Tap on **VoiceOver** under the Vision category at the top.
5. Tap the **VoiceOver switch** to enable it.

### TalkBack (Android)

1. Launch the **Settings** app from your launch screen.
2. Tap on **Accessibility**.
3. Tap on **TalkBack**.
4. Tap the **switch** to enable it.
5. Tap the **OK** button to close the dialog.
