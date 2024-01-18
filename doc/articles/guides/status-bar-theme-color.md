---
uid: Uno.Tutorials.StatusBarThemeColor
---

# How to update StatusBar color based on dark/light theme

The [`UISettings.ColorValuesChanged` event](https://learn.microsoft.com/uwp/api/windows.ui.viewmanagement.uisettings.colorvalueschanged) can be used to listen for notifications when dark mode is enabled or disabled at the system level.

## Example

The complete sample code can be found here: [StatusBarThemeColor](https://github.com/unoplatform/Uno.Samples/tree/master/UI/StatusBarThemeColor)

## Step-by-step instructions

1. Create a new Uno Platform application, following the instructions [here](../get-started.md).
2. In `MainPage.xaml`, add a `<CommandBar>`:
    > On iOS, the status bar color cannot be set directly, so it is done via a `CommandBar` placed in the page. You could also use any XAML element like `<Grid>` or `<Border>` to achieve a similar effect, if your application doesn't use navigation or doesn't use native navigation. This is because the page content can go under the status bar. In fact, you usually have to add padding to avoid that (see next step).

    ```xml
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <CommandBar Grid.Row="0"
                    x:Name="MyCommandBar"
                    Content="MainPage" />

        <StackPanel Grid.Row="1">
            <TextBlock Text="Hello, world!"
                        Margin="20"
                        FontSize="30" />
        </StackPanel>
    </Grid>
    ```

3. Add a [`VisibleBoundsPadding.PaddingMask`](../features/VisibleBoundsPadding.md) to the root `<Grid>` to prevent the content being partially covered by the status and command bars:

    ```xml
    <Page ...
          xmlns:toolkit="using:Uno.UI.Toolkit"
          ...>

        <Grid toolkit:VisibleBoundsPadding.PaddingMask="Top">
    ```

4. In `MainPage.xaml.cs`, expose the `MyCommandBar` which will be referenced in a later step:

    ```csharp
    public static MainPage Instance { get; private set; }

    public MainPage()
    {
        Instance = this;

        this.InitializeComponent();
    }

    public CommandBar GetCommandBar() => MyCommandBar;
    ```

5. In `App.cs`, enable the native frame navigation to use the native style for `CommandBar`, instead of the UWP style. Make sure that the `#if ... #endif` block is before `this.InitializeComponent()`:

    ```csharp
    public App()
    {
        /***/

    #if __IOS__ || __ANDROID__
        // Enable native frame navigation.
        Uno.UI.FeatureConfiguration.Style.ConfigureNativeFrameNavigation();
    #endif

        this.InitializeComponent();
        this.Suspending += OnSuspending;
    }
    ```

6. Subscribe to the `UISettings.ColorValuesChanged` event from `App.cs`:
    > Note that the instance of `UISettings` is kept to prevent it from being disposed when going out of scope, which would otherwise dispose the event subscription.

    ```csharp
    public sealed partial class App : Application
    {
        private UISettings _uiSettings;
        private bool? _wasDarkMode = null;

        /* ... */

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            /* existing code... */

            // add this line at the end of OnLaunched:
            ConfigureStatusBar();
        }

        /* ... */

        private void ConfigureStatusBar()
        {
            // Listen for the system theme changes.
            _uiSettings = new UISettings();
            _uiSettings.ColorValuesChanged += (s, e) =>
            {
    #if __ANDROID__
                var backgroundColor = _uiSettings.GetColorValue(UIColorType.Background);
                var isDarkMode = backgroundColor == Windows.UI.Colors.Black;

                // Prevent deadlock as setting StatusBar.ForegroundColor will also trigger this event.
                if (_wasDarkMode == isDarkMode) return;
                _wasDarkMode = isDarkMode;
    #endif

                UpdateStatusBar();
            };

    #if __IOS__
            // Force an update when the app is launched.
            UpdateStatusBar();
    #endif
        }
    }
    ```

7. Implement the `UpdateStatusBar()` method:

    ```csharp
    private void UpdateStatusBar()
    {
        // === 1. Determine the current theme from the background value,
        // which is calculated from the theme and can only be black or white.
        var backgroundColor = _uiSettings.GetColorValue(UIColorType.Background);
        var isDarkMode = backgroundColor == Windows.UI.Colors.Black;

    #if __IOS__ || __ANDROID__
        // === 2. Set the foreground color.
        // note: The foreground color can only be set to a "dark/light" value. See uno remarks on StatusBar.ForegroundColor.
        // note: For ios in dark mode, setting this value will have no effect.
        var foreground = isDarkMode ? Windows.UI.Colors.White : Windows.UI.Colors.Black;
        Windows.UI.ViewManagement.StatusBar.GetForCurrentView().ForegroundColor = foreground;

        // === 3. Set the background color.
        var background = isDarkMode ? Windows.UI.Colors.MidnightBlue : Windows.UI.Colors.SkyBlue;
    #if __ANDROID__
        // On Android, this is done by calling Window.SetStatusBarColor.
        if (Uno.UI.ContextHelper.Current is Android.App.Activity activity)
        {
            activity.Window.SetStatusBarColor(background);
        }
    #endif
        // On iOS, this is done via the native CommandBar which goes under the status bar.
        // For android, we will also update the CommandBar just for consistency.
        if (MainPage.Instance.GetCommandBar() is CommandBar commandBar)
        {
            commandBar.Foreground = new SolidColorBrush(foreground); // controls the color for the "MainPage" page title
            commandBar.Background = new SolidColorBrush(background);
        }
    #endif
    }

8. Change the theme of your device to validate the result.

## Concluding remarks

This is just one way to show how it could be done. Alternatively, depending on the application, the following options can also be considered:

- [Attached Property](https://learn.microsoft.com/windows/uwp/xaml-platform/attached-properties-overview) on the `CommandBar` (when multiple pages are used)
- Xaml Grid/Border/Image that goes under the StatusBar (with `WindowManagerFlags.TranslucentStatus` on Android, no additional change on iOS)
