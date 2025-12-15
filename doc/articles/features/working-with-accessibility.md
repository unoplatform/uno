---
uid: Uno.Features.Accessibility
---

# Accessibility (a11y)

Windows uses the UI Automation framework to provide accessibility information to screen readers.

> Microsoft UI Automation is an accessibility framework for Windows. It provides programmatic access to most UI elements on the desktop. It enables assistive technology products, such as screen readers, to provide information about the UI to end users and to manipulate the UI by means other than standard input. UI Automation also allows automated test scripts to interact with the UI.\
[UI Automation Overview](https://learn.microsoft.com/windows/desktop/WinAuto/uiauto-uiautomationoverview)

Uno.UI implements a subset of WinUI's UI Automation APIs to make your applications work with each platform's built-in screen reader or accessibility support:

| Windows  | Android  | iOS       | macOS     | WebAssembly                  |
|----------|----------|-----------|-----------|------------------------------|
| Narrator | TalkBack | VoiceOver | VoiceOver | OS or Web Browser Integrated |

Read [Expose basic accessibility information](https://learn.microsoft.com/windows/uwp/design/accessibility/basic-accessibility-information) to learn how to use the `AutomationProperties` supported by Uno.UI:

- `AutomationProperties.AutomationId`
- `AutomationProperties.Name`
- `AutomationProperties.LabeledBy`
- `AutomationProperties.AccessibilityView`

## SimpleAccessibility mode

While we were trying to replicate WinUI's behavior on iOS and Android, we realized that iOS doesn't allow nested accessible elements to be focused.
For example, if you select a list item, the screen reader will automatically read the accessible name of all inner elements one after the other, but won't let you focus them individually (unlike Windows and Android).

While this behavior comes with its own set of limitations (e.g., you can't nest buttons), it greatly simplifies the implementation of accessibility.
By comparison, on WinUI, the user would need to navigate through every inner element of every list item, unless the developer manually disables focus for each inner element and aggregates their accessible names into a single string to use as the accessible name of the list item.

Instead of trying to replicate WinUI's behavior on iOS (which *might* be doable using the `UIAccessibilityContainer` interface, although we haven't tried it yet), we decided to go along with the iOS behavior and bring it to Android as well. We called this mode SimpleAccessibility.

Here's how to enable it:

```csharp
// App's constructor (`App.cs` or `App.xaml.cs`)
#if __IOS__ || __TVOS__ || __ANDROID__ || __WASM__
FeatureConfiguration.AutomationPeer.UseSimpleAccessibility = true;
#endif
```

We highly recommend using this mode, as iOS still won't let you focus nested accessible elements even if you don't (see known issues).

## Disabling accessibility text scaling

You have the option to disable accessibility text scaling of iOS and Android devices but Apple [recommends to keep text sizes dynamic](https://developer.apple.com/videos/play/wwdc2017/245)

Here's how to disable it

```csharp
// App's constructor (`App.cs` or `App.xaml.cs`)
Uno.UI.FeatureConfiguration.Font.IgnoreTextScaleFactor = true;
```

## AutomationId

The `AutomationProperties.AutomationId` attached property can be used by UI Testing frameworks to find visual elements.

To avoid performance issues, this property only has an effect when either the `IsUiAutomationMappingEnabled` msbuild property, or `Uno.UI.FrameworkElementHelper.IsUiAutomationMappingEnabled` is set to true.

Setting this property does the following:

- On **Android**, it sets the [`View.ContentDescription`](https://developer.android.com/reference/android/view/View.html#setContentDescription(java.lang.CharSequence)) property
- On **iOS**, it sets the [`UIView.AccessibilityIdentifier`](https://developer.apple.com/documentation/uikit/uiaccessibilityidentification/1623132-accessibilityidentifier) property
- On **macOS**, it sets the [`NSView.AccessibilityIdentifier`](https://developer.apple.com/documentation/appkit/nsaccessibility/1535023-accessibilityidentifier) property
- On **WebAssembly**, it sets [`aria-label`](https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/Attributes/aria-label) and the `xamlautomationid` property on the HTML element. The [`role`](https://www.w3.org/WAI/PF/HTML/wiki/RoleAttribute) HTML Attribute is also set based on the XAML view type whose `AutomationProperties.AutomationId` was set.

This property is generally used alongside [Uno.UITest](https://github.com/unoplatform/Uno.UITest) to create UI Tests, and is particularly useful to select items using data-bound identifiers.

## Windows.UI.ViewManagement.AccessibilitySettings

Some external libraries or UI toolkits may depend on the `AccessibilitySettings` class to check for high contrast settings. As Uno Platform targets cannot check for this via accessible APIs, the properties only return predefined defaults, unless you override them manually via `WinRTFeatureConfiguration.Accessibility`:

```csharp
var accessibilitySettings = new AccessibilitySettings();
accessibilitySettings.HighContrast; // default - false
accessibilitySettings.HighContrastScheme; // default - High Contrast Black

// Override the defaults
WinRTFeatureConfiguration.Accessibility.HighContrast = true;
WinRTFeatureConfiguration.Accessibility.HighContrastScheme = "High Contrast White";

accessibilitySettings.HighContrast; // true
accessibilitySettings.HighContrastScheme; // High Contrast White
```

When the value of `WinRTFeatureConfiguration.Accessibility.HighContrast` is changed, the `AccessibilitySettings.HighContrastChanged` event is raised.

## Known issues

- `Hyperlink` in `TextBlock` is not supported.
- `TextBox` and `PasswordBox` don't use `Header` and `PlaceholderText`.
- `ItemsControl` doesn't use `AutomationProperties.Name` on its `DataTemplate`'s root.
- There are XAML code generation conflicts between `x:Name` and `AutomationProperties.Name`.
- A child with the same accessible name as its parent is accessibility focusable.
- `Control` doesn't receive focus when accessibility focused.
- `TabIndex` is not supported.
- On iOS, nested accessible elements are not accessibility focusable.
- On Android, both `ToggleSwitch` and its native `Switch` can be accessibility focused.
- On Android, both `TextBox` and its native `EditText` can be accessibility focused.
- On Android, `Control` without a non-empty accessible name is not accessibility focusable.
- On Android, the accessible name of the native back button can't be customized.
- On Android, `TextBox` and `PasswordBox` don't hint at "double tap to edit".
- On Android, you can tap to focus an accessible child of an accessible element that has never been focused even if `UseSimpleAccessibility` is enabled.
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

- On Wasm, only the following elements support accessibility:
  - `Button`
  - `CheckBox`
  - `Image`
  - `RadioButton`
  - `TextBlock`
  - `TextBox`
  - `Slider`

- On Wasm, `PasswordBox` is not currently supported due to external limitations.

## Tips

- Interactive controls (like `TabBarItem`, `Button`, cards) must have an accessibility label. Use `AutomationProperties.Name` to provide this label (e.g. `AutomationProperties.Name="Home tab"`).
- Always set and localize `AppBarButton.Label` (even if it's not displayed on Android and iOS). It is used by the screen reader for accessibility.
- Always use `x:Uid` to localize the `AutomationProperties.Name` attached property.
- Avoid using `Opacity="0"` and `IsHitTestVisible="False"` when you can use `Visibility="Collapsed"`. The screen reader can still focus the former, but not the latter.
- Avoid stacking `TextBlock`s inside  a `Panel` when you can use `Inlines` inside a `TextBlock` (using `LineBreak` if necessary). This allows the screen reader to read all the text at once, instead of having the user select every part manually.
- Use a converter to trim long text. While a `TextBlock` might ellipsize long text, the screen reader will read the entire text provided.
- Avoid creating custom controls when you can use built-in ones. If you must, make sure to implement and provide an appropriate `AutomationPeer`.
- You can disable accessibility focus of native elements using `android:ImportantForAccessibility="No"` and `ios:IsAccessibilityElement="False"`.
- `ContentControl` based controls (`Button`, `CheckBox`, ...) automatically use the string representation of their `Content` property. In order for the `AutomationProperties.AutomationId` property to be selectable, add `AutomationProperties.AccessibilityView="Raw"` to the control as well.

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

### VoiceOver (macOS)

1. Launch the **System Preferences** from the macOS logo.
2. Tap on **Accessibility**.
3. Tap on **VoiceOver** under the Vision category at the top.
4. Tap the **Enable VoiceOver switch** to enable it.
