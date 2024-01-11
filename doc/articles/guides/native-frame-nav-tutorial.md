---
uid: Uno.Tutorials.UseNativeFrameNav
---

# How to use native Frame navigation

## Example

The complete sample code can be found here: [NativeFrameNav](https://github.com/unoplatform/Uno.Samples/tree/master/UI/NativeFrameNav)

## Step-by-step instructions

1. Create a new Uno Platform application, following the instructions [here](../get-started.md).
2. Add two more pages (`BlankPage1` and `BlankPage2`) to the `Your_Project_Name` project

    Right-click on the `Your_Project_Name` project -> `Add` -> `New Item...` -> `Page (Uno Platform Windows App SDK)`
    Repeat once
3. Modify the content of each page to:
   - `MainPage.xaml`, `BlankPage1.xaml`, `BlankPage2.xaml`:
        > [!NOTE]
        > Add `xmlns:toolkit="using:Uno.UI.Toolkit"` to the `<Page>` element.

        ```xml
        <Grid toolkit:VisibleBoundsPadding.PaddingMask="Top">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="*" />
            </Grid.RowDefinitions>

            <CommandBar Grid.Row="0" Content="CHANGE_THE_TEXT_HERE" />
            <StackPanel Grid.Row="1">
                <TextBlock Text="CHANGE_THE_TEXT_HERE" />
            </StackPanel>
        </Grid>
        ```

        > Put a different text for each page: "MainPage", "Content 1", "Content 2"
4. Add a button for forward navigation in the first two pages:
    - `MainPage.xaml`, `BlankPage1,xaml`:

        ```xml
        <StackPanel Grid.Row="1">
            <TextBlock Text="Main Content" />
            <Button Content="Next Page" Click="GotoNextPage" />
        </StackPanel>
        ```

    - `MainPage.xaml.cs`, `BlankPage1.xaml.cs`:

        ```csharp
        private void GotoNextPage(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(BlankPage1)); // in MainPage
        private void GotoNextPage(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(BlankPage2)); // in BlankPage1
        ```

5. (Optionally) Add an `AppBarButton` to the `CommandBar` for back navigation in the last two pages for Skia heads:
    - `BlankPage1.xaml`, `BlankPage2.xaml`:
        > [!NOTE]
        > Here a platform conditional is used to show the `AppBarButton` on the Skia platforms only. For more information on this feature, check out: [platform specific XAML](../platform-specific-xaml.md)

        ```xml
        <Page ...
              xmlns:skia="http://uno.ui/skia"
              mc:Ignorable="d skia"
              ...>
        <!-- ...  -->
        <CommandBar Grid.Row="0" Content="BlankPage1">
   <CommandBar.PrimaryCommands>
    <skia:AppBarButton Content="Back" Click="NavigateBack" />
   </CommandBar.PrimaryCommands>
  </CommandBar>
    - `BlankPage1.xaml.cs`, `BlankPage2.xaml.cs`:

        ```csharp
        private void NavigateBack(object sender, RoutedEventArgs e) => Frame.GoBack(); // in both pages
        ```

6. Enable native frame navigation in `App.cs` or `App.xaml.cs`:

    ```csharp
    public App()
    {
        InitializeLogging();

    #if __IOS__ || __ANDROID__
        Uno.UI.FeatureConfiguration.Style.ConfigureNativeFrameNavigation();
    #endif

        this.InitializeComponent();
        this.Suspending += OnSuspending;
    }
    ```

7. Add some extra setups for navigation:

    ```csharp
    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        /* existing code... */

        ConfigureNavigation();
    }

    private void ConfigureNavigation()
    {
        #if __ANDROID__ || __WASM__
        var frame = (Frame)_window.Content;
        var manager = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();

        // Toggle the visibility of back button based on if the frame can navigate back.
        // Setting it to visible has the follow effect on the platform:
        // - uwp: show a `<-` back button on the title bar
        // - wasm: add a dummy entry in the browser back stack
        frame.Navigated += (s, e) => manager.AppViewBackButtonVisibility = frame.CanGoBack
       ? Windows.UI.Core.AppViewBackButtonVisibility.Visible
       : Windows.UI.Core.AppViewBackButtonVisibility.Collapsed;

  // On some platforms, the back navigation request needs to be hooked up to the back navigation of the Frame.
  // These requests can come from:
  // - uwp: title bar back button
  // - droid: CommandBar back button, os back button/gesture
  // - wasm: browser back button
  manager.BackRequested += (s, e) =>
  {
   if (frame.CanGoBack)
   {
    frame.GoBack();

    e.Handled = true;
   }
  };
        #endif
    }

## Additional Resources

- [Uno-specific documentation](../controls/CommandBar.md) on `CommandBar` and `AppBarButton`
- [Native Frame navigation](../features/native-frame-nav.md)
