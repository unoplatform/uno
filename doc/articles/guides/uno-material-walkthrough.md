# How to use Uno.Material

This guide will walk you through the necessary steps to setup and to use the [`Uno.Material` package](https://www.nuget.org/packages/Uno.Material) in an Uno Platform application.

> [!TIP]
> The complete source code that goes along with this guide is available in the [unoplatform/Uno.Samples](https://github.com/unoplatform/Uno.Samples) GitHub repository - [UnoMaterialSample](https://github.com/unoplatform/Uno.Samples/tree/master/UI/UnoMaterialSample)

## Prerequisites

# [Visual Studio for Windows](#tab/tabid-vswin)

* [Visual Studio 2019 16.3 or later](http://www.visualstudio.com/downloads/)
  * **Universal Windows Platform** workload installed
  * **Mobile Development with .NET (Xamarin)** workload installed
  * **ASP**.**NET and web** workload installed
  * [Uno Platform Extension](https://marketplace.visualstudio.com/items?itemName=nventivecorp.uno-platform-addin) installed

# [VS Code](#tab/tabid-vscode)

* [**Visual Studio Code**](https://code.visualstudio.com/)

* [**Mono**](https://www.mono-project.com/download/stable/)

* **.NET Core SDK**
    * [.NET Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1) (**version 3.1.8 (SDK 3.1.402)** or later)
    * [.NET Core 5.0 SDK](https://dotnet.microsoft.com/download/dotnet-core/5.0) (**version 5.0 (SDK 5.0.100)** or later)

    > Use `dotnet --version` from the terminal to get the version installed.

# [Visual Studio for Mac](#tab/tabid-vsmac)

* [**Visual Studio for Mac 8.8**](https://visualstudio.microsoft.com/vs/mac/)
* [**Xcode**](https://apps.apple.com/us/app/xcode/id497799835?mt=12) 10.0 or higher
* An [**Apple ID**](https://support.apple.com/en-us/HT204316)
* **.NET Core SDK**
    * [.NET Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1) (**version 3.1.8 (SDK 3.1.402)** or later)
    * [.NET Core 5.0 SDK](https://dotnet.microsoft.com/download/dotnet-core/5.0) (**version 5.0 (SDK 5.0.100)** or later)
* [**GTK+3**](https://formulae.brew.sh/formula/gtk+3) for running the Skia/GTK projects

# [JetBrains Rider](#tab/tabid-rider)

* [**Rider Version 2020.2+**](https://www.jetbrains.com/rider/download/)
* [**Rider Xamarin Android Support Plugin**](https://plugins.jetbrains.com/plugin/12056-rider-xamarin-android-support/) (you may install it directly from Rider)

***

<br>

> [!Tip]
> For a step-by-step guide to installing the prerequisites for your preferred IDE and environment, consult the [Get Started guide](../get-started.md).

## Step-by-steps
### Section 1: Setup Uno.Material
1. Create a new Uno Platform application, following the instructions [here](../get-started.md).
1. Add NuGet package `Uno.Material` to each of project heads by:
    > [!NOTE]
    > You may have to check the `[x] Include Prerelease` to find this package, as there are currently no stable release.

    > [!NOTE]
    > The project heads refer to the projects targeted to a specific platforms:
    > - UnoMaterialSample.Droid
    > - UnoMaterialSample.iOS
    > - UnoMaterialSample.macOS
    > - UnoMaterialSample.Skia.Gtk
    > - UnoMaterialSample.Skia.Tizen
    > - UnoMaterialSample.Skia.WPF
    > - UnoMaterialSample.UWP
    > - UnoMaterialSample.Wasm
    >
    > > The shared project is not part of this, and the `.Skia.WPF.Host` project is another exception.

    Here are some issues that you are likely to run into after complete the above step:
    - > NU1605: Detected package downgrade: Uno.UI from 3.6.0-dev.275 to 3.5.1. Reference the package directly from the project to select a different version.

        The app may not compile, crash at runtime, or behave strangely as a result of this.
        solution: You need to update the version of `Uno.UI` packages for all project heads that you are using to the higher version.
        > note: By `Uno.UI` packages, it includes:
        > - Uno.UI
        > - Uno.UI.RemoteControl
        > - Uno.UI.WebAssembly
        > - Uno.UI.Skia.Gtk
        > - Uno.UI.Skia.Tizen
        > - Uno.UI.Skia.Wpf

    - When building the `.Droid` project, the project failed to build with:
        ```
        error : Could not find 1 Android X assemblies, make sure to install the following NuGet packages:
            - Xamarin.AndroidX.Lifecycle.LiveData
        You can also copy-and-paste the following snippet into your .csproj file:
            <PackageReference Include="Xamarin.AndroidX.Lifecycle.LiveData" Version="2.1.0" />
        ```
        solution: Simply add the specific version of `Xamarin.AndroidX.Lifecycle.LiveData` to the `.Droid` project
1. Add the following code inside `App.xaml`:
    ```xml
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <!-- Load WinUI resources -->
                <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />

                <!-- Application's custom styles -->
                <!-- other ResourceDictionaries -->
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
    ```

1. Initialize the material library in `App.xaml.cs`:
    ```cs
    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
         Uno.Material.Resources.Init(this, null);

        // [existing code...]
    }
    ```

### Section 2: Using Uno.Material library
1. Let's add a few controls with the Material style to `MainPage.xaml`:
    ```xml
    <Page x:Class="UnoMaterialSample.MainPage"
          xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
          xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
          xmlns:toolkit="using:Uno.UI.Toolkit">

        <Grid toolkit:VisibleBoundsPadding.PaddingMask="Top" >
            <ScrollViewer>
                <StackPanel Margin="16,0" Spacing="8">
                    <!-- controls with material styles -->
                    <TextBlock Text="Hello, Material!" Style="{StaticResource MaterialHeadline2}" />
                    <TextBlock Text="text" Style="{StaticResource MaterialBody1}" />
                    <Button Content="button" Style="{StaticResource MaterialContainedButtonStyle}" />
                    <ComboBox ItemsSource="asd" Style="{StaticResource MaterialComboBoxStyle}" />
                    <DatePicker Style="{StaticResource MaterialDatePickerStyle}" />
                    <TextBox Text="input" Style="{StaticResource MaterialFilledTextBoxStyle}" />

                </StackPanel>
            </ScrollViewer>
        </Grid>
    <Page>
    ```
1. Now we'll add a few new controls that are defined in the Material package - `Card`, `ChipGroup`, `Chip`, and `Divider`:
    ```xml
    <Page ...
          xmlns:material="using:Uno.Material.Controls">

        <Grid toolkit:VisibleBoundsPadding.PaddingMask="Top" >
            <ScrollViewer>
                <StackPanel Margin="16,0" Spacing="8">
                    <!-- controls with material styles -->
                    <!-- ... -->

                    <!-- material controls -->
                    <material:Divider SubHeader="Uno.Material Controls:" Style="{StaticResource MaterialDividerStyle}" />
                    <material:Card HeaderContent="Material Design"
                            SupportingContent="Material is a design system created by Google to help teams build high-quality digital experiences for Android, iOS, Flutter, and the web."
                            Style="{StaticResource MaterialOutlinedCardStyle}">
                        <material:Card.HeaderContentTemplate>
                            <DataTemplate>
                                <TextBlock Text="{Binding}" Margin="16,14,16,0" Style="{ThemeResource MaterialHeadline6}" />
                            </DataTemplate>
                        </material:Card.HeaderContentTemplate>
                        <material:Card.SupportingContentTemplate>
                            <DataTemplate>
                                <TextBlock Text="{Binding}" Margin="16,0,16,14" Style="{ThemeResource MaterialBody2}" />
                            </DataTemplate>
                        </material:Card.SupportingContentTemplate>
                    </material:Card>
                    <material:ChipGroup SelectionMode="Multiple" Style="{StaticResource MaterialChipGroupStyle}">
                        <material:Chip Content="Uno" Style="{StaticResource MaterialChipStyle}" />
                        <material:Chip Content="Material" Style="{StaticResource MaterialChipStyle}" />
                        <material:Chip Content="Controls" Style="{StaticResource MaterialChipStyle}" />
                    </material:ChipGroup>
                </StackPanel>
            </ScrollViewer>
        </Grid>
    <Page>
    ```

> [TIP!]
> You can find the style names using these methods:
> - "Feature" section of Uno.Themes README: https://github.com/unoplatform/Uno.Themes#features
> - Going through the source code of control styles: https://github.com/unoplatform/Uno.Themes/tree/master/src/library/Uno.Material/Styles/Controls
> - Check out the [Uno.Gallery web app](https://gallery.platform.uno/) (Click on the `<>` button to view xaml source)

### Section 3: Overriding Color Palette
1. Create the nested folders `Styles\` and then `Styles\Application\` under the `.Shared` project
1. Add a new Resource Dictionary `ColorPaletteOverride.xaml` under `Styles\Application\`
1. Replace the content of that res-dict with the source from: https://github.com/unoplatform/Uno.Themes/blob/master/src/library/Uno.Material/Styles/Application/ColorPalette.xaml
1. Make a few changes to the color:
    > Here we are replacing the last 2 characters with 00, essentially dropping the blue-channel
    ```
    <!-- Light Theme -->
    <ResourceDictionary x:Key="Light">
        <Color x:Key="MaterialPrimaryColor">#5B4C00</Color>
        <Color x:Key="MaterialPrimaryVariantDarkColor">#353F00</Color>
        <Color x:Key="MaterialPrimaryVariantLightColor">#B6A800</Color>
        <Color x:Key="MaterialSecondaryColor">#67E500</Color>
        <Color x:Key="MaterialSecondaryVariantDarkColor">#2BB200</Color>
        <Color x:Key="MaterialSecondaryVariantLightColor">#9CFF00</Color>
        <Color x:Key="MaterialBackgroundColor">#FFFFFF</Color>
        <Color x:Key="MaterialSurfaceColor">#FFFFFF</Color>
        <Color x:Key="MaterialErrorColor">#F85900</Color>
        <Color x:Key="MaterialOnPrimaryColor">#FFFF00</Color>
        <Color x:Key="MaterialOnSecondaryColor">#000000</Color>
        <Color x:Key="MaterialOnBackgroundColor">#000000</Color>
        <Color x:Key="MaterialOnSurfaceColor">#000000</Color>
        <Color x:Key="MaterialOnErrorColor">#000000</Color>
        <Color x:Key="MaterialOverlayColor">#51000000</Color>

        <!-- ... --->
    </ResourceDictionary>

    <!-- Dark Theme -->
    <ResourceDictionary x:Key="Dark">
        <Color x:Key="MaterialPrimaryColor">#B6A800</Color>
        <Color x:Key="MaterialPrimaryVariantDarkColor">#353F00</Color>
        <Color x:Key="MaterialPrimaryVariantLightColor">#D4CB00</Color>
        <Color x:Key="MaterialSecondaryColor">#67E500</Color>
        <Color x:Key="MaterialSecondaryVariantDarkColor">#2BB200</Color>
        <Color x:Key="MaterialSecondaryVariantLightColor">#9CFF00</Color>
        <Color x:Key="MaterialBackgroundColor">#121212</Color>
        <Color x:Key="MaterialSurfaceColor">#121212</Color>
        <Color x:Key="MaterialErrorColor">#CF6600</Color>
        <Color x:Key="MaterialOnPrimaryColor">#0000FF</Color>
        <Color x:Key="MaterialOnSecondaryColor">#000000</Color>
        <Color x:Key="MaterialOnBackgroundColor">#FFFFFF</Color>
        <Color x:Key="MaterialOnSurfaceColor">#DEFFFFFF</Color>
        <Color x:Key="MaterialOnErrorColor">#000000</Color>
        <Color x:Key="MaterialOverlayColor">#51FFFFFF</Color>

        <!-- ... --->
    </ResourceDictionary>

    <!-- ... --->
    ```
    > You may also use this for picking colors: https://material.io/design/color/the-color-system.html#tools-for-picking-colors
1. In `App.xaml.cs`, update the line that initializes the material library to include the new palette:
    ```cs
    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
         Uno.Material.Resources.Init(this, new ResourceDictionary { Source = new Uri("ms-appx:///Styles/Application/ColorPaletteOverride.xaml") });

        // [existing code...]
    }
    ```
1. Run the app, you should now see the controls using your new color scheme.

## Note
- Certain controls may require additional setup to setup and/or overriding color pallette. For details, see: [Uno.Material controls extra setup](../features/uno-material-controls-extra-setup.md)

## Get the complete code

See the completed sample on GitHub: [UnoMaterialSample](https://github.com/unoplatform/Uno.Samples/tree/master/UI/UnoMaterialSample)

## Additional Resources
- [Uno.Material](../features/uno-material.md) overview
- Uno.Material library repository: https://github.com/unoplatform/Uno.Themes
- Tools for picking colors: https://material.io/design/color/the-color-system.html#tools-for-picking-colors

<br>

***

## Help! I'm having trouble

> [!TIP]
> If you ran into difficulties with any part of this guide, you can:
>
> * Ask for help on our [Discord channel](https://www.platform.uno/discord) - #uno-platform
> * Ask a question on [Stack Overflow](https://stackoverflow.com/questions/tagged/uno-platform) with the 'uno-platform' tag
