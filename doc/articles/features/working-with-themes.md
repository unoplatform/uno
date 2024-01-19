---
uid: Uno.Features.Themes
---

# Supporting Dark and High Contrast themes

Uno Platform offers fine-grained customization of typography, corner radius, and styling of UI elements to convey a brand identity in your application. However, platforms like iOS, Android, and web browsers also offer a system-wide color mode that users can change. For instance, Windows uses **Light mode** by default, but many users prefer **Dark mode** which is intended to be easier on the eyes in low-light conditions. Many platforms also offer a **High Contrast mode** to make things easier to see.

These themes affect both the `Background` and `Foreground` colors to accommodate user preferences. All of the color modes mentioned above are available for use in your app. This guide will detail how to change the system theme setting, make your app receive change notifications for it, and react to those changes at runtime.

## Enable dark mode

As in WinUI, the possible values `Light`, `Dark`, and `HighContrast` correspond to a value users can select in the settings of their respective platforms.

High Contrast mode is often available to enable separately, from a dedicated _Accessibility_ page. For the purpose of this documentation, we will assume the user wants to use Dark mode.

### [**Windows**](#tab/windows)

Windows PCs can enable Dark mode from Windows Settings. See [Change colors in Windows](https://support.microsoft.com/windows/change-colors-in-windows-d26ef4d6-819a-581c-1581-493cfcc005fe) for more information.

### [**iOS**](#tab/ios)

Devices running iOS or iPadOS can enable Dark mode from Control Center or Settings. See [Use Dark Mode on your iPhone and iPad](https://support.apple.com/HT210332) for more information.

### [**Android**](#tab/android)

Android devices can enable Dark mode from Settings. See [Change to dark or color mode on your Android device](https://support.google.com/android/answer/9730472) for more information.

### [**Browser**](#tab/browser)

The steps to enable Dark mode in a browser vary by browser. See the following guides for more information:

- [Browse in dark mode or Dark theme (Chrome)](https://support.google.com/chrome/answer/9275525)
- [Use the dark theme in Microsoft Edge](https://support.microsoft.com/microsoft-edge/use-the-dark-theme-in-microsoft-edge-9b74617b-f542-77ed-033b-1a5cfb17a2df)
- [Use themes to change the look of Firefox](http://mzl.la/1BAQGDX)

---

## React to OS theme changes

When you change the theme mode on your device, the system will send a notification to your app. The colors of UI elements in your app will automatically switch over as long as you do _not_ specify a theme in any of the following places:

- `App` constructor
- `App.xaml`
- `AppResources.xaml`
- The `RequestedTheme` property of a parent `FrameworkElement`

However, your app can still react manually to changes in the theme mode. To do so, you can use the [Uno.CommunityToolkit.WinUI.UI](https://www.nuget.org/packages/Uno.CommunityToolkit.WinUI.UI) NuGet package. This package is not required to change the app color theme, but it contains a `ThemeListener` class that can be used to listen for OS theme changes. To actually change the app color theme at runtime, you need to install the [Uno.Toolkit.WinUI](https://www.nuget.org/packages/Uno.Toolkit.WinUI) NuGet package which contains a `SystemThemeHelper` class.

Below is an example of how to use these classes to change the app theme at runtime based on a raised event:

```csharp
using CommunityToolkit.WinUI.UI.Helpers;
using Uno.Toolkit.UI;

namespace MyApp.UI.Views;

public class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();

        ThemeListener.Current.ThemeChanged += OnThemeChanged;
    }

    private void OnThemeChanged(ThemeListener sender)
    {
        bool isDarkMode = sender.CurrentTheme == ApplicationTheme.Dark;

        SystemThemeHelper.SetApplicationTheme(darkMode: isDarkMode);
    }
}
```

## Change the app theme at runtime

The [`SystemThemeHelper.SetApplicationTheme`](https://github.com/unoplatform/uno.toolkit.ui/blob/main/src/Uno.Toolkit.UI/Helpers/SystemThemeHelper.cs) method from `Uno.Toolkit.UI` can also be used to support an in-app toggle for dark mode. For example, you could add a toggle button to your app's settings page that allows the user to switch between light and dark modes. The following code snippet shows how to implement this:

```csharp
using Uno.Toolkit.UI;

namespace MyApp.UI.Views;

public class SettingsPage : Page
{
    public SettingsPage()
    {
        this.InitializeComponent();

        this.Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        this.DarkModeToggle.IsOn = SystemThemeHelper.IsDarkModeEnabled;
    }

    private void OnDarkModeToggleToggled(object sender, RoutedEventArgs e)
    {
        SystemThemeHelper.SetApplicationTheme(darkMode: this.DarkModeToggle.IsOn);
    }
}
```

## Change the app theme at startup

Another method to change the app theme is to adjust it immediately upon startup. The following must only be called in the `App` class constructor.

```csharp
public App()
{
    this.InitializeComponent();

#if HAS_UNO
    Uno.UI.ApplicationHelper.RequestedCustomTheme = nameof(ApplicationTheme.Dark);
#else
    this.RequestedTheme = ApplicationTheme.Dark;
#endif
}
```

## See also

- [Uno Toolkit](https://www.github.com/unoplatform/uno.toolkit.ui)
- [Theme Listener](https://learn.microsoft.com/windows/communitytoolkit/helpers/themelistener)
- [High Contrast Themes](https://learn.microsoft.com/windows/apps/design/accessibility/high-contrast-themes)
