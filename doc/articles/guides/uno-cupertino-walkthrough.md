# How to use Uno.Cupertino

This guide will walk you through the necessary steps to set up and use the [`Uno.Cupertino` package](https://www.nuget.org/packages/Uno.Cupertino) in an Uno Platform application.

> [!TIP]
> The complete source code that goes along with this guide is available in the [unoplatform/Uno.Samples](https://github.com/unoplatform/Uno.Samples) GitHub repository - [UnoCupertinoSample](https://github.com/unoplatform/Uno.Samples/tree/master/UI/UnoCupertinoSample)

> [!Tip]
> For a step-by-step guide to installing the prerequisites for your preferred IDE and environment, consult the [Get Started guide](../get-started.md).

## Step-by-steps
### Section 1: Setup Uno.Cupertino
1. Create a new Uno Platform application, following the instructions [here](../get-started.md).
1. Add NuGet package `Uno.Cupertino` to each of project heads by:
    > [!NOTE]
    > You may have to check the `[x] Include Prerelease` to find this package, as there are currently no stable release.

    > [!NOTE]
    > The project heads refer to the projects targeted to a specific platforms:
    > - UnoCupertinoSample.Droid
    > - UnoCupertinoSample.iOS
    > - UnoCupertinoSample.macOS
    > - UnoCupertinoSample.Skia.Gtk
    > - UnoCupertinoSample.Skia.Tizen
    > - UnoCupertinoSample.Skia.WPF
    > - UnoCupertinoSample.UWP
    > - UnoCupertinoSample.Wasm
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
        solution: Add the mentioned version of `Xamarin.AndroidX.Lifecycle.LiveData` to the `.Droid` project
1. Add the following code inside `App.xaml`:
    ```xml
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <!-- Load WinUI resources -->
                <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />

                <CupertinoColors xmlns="using:Uno.Cupertino"  />
                <CupertinoResources xmlns="using:Uno.Cupertino" />
                <!-- Application's custom styles -->
                <!-- other ResourceDictionaries -->
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
    ```

### Section 2: Using Uno.Cupertino library
1. Let's add a few controls with the Cupertino style to `MainPage.xaml`:
    ```xml
    <Page x:Class="UnoCupertinoSample.MainPage"
          xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
          xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
          xmlns:toolkit="using:Uno.UI.Toolkit">

        <Grid toolkit:VisibleBoundsPadding.PaddingMask="Top" >
            <ScrollViewer>
                <StackPanel Margin="16,0" Spacing="8">
                    <!-- controls with Cupertino styles -->
                    <TextBlock Text="Hello, Cupertino!" Style="{StaticResource CupertinoPrimaryTitle}" />
                    <TextBlock Text="text" Style="{StaticResource CupertinoBody}" />
                    <Button Content="button" Style="{StaticResource CupertinoButtonStyle}" />
                    <ComboBox ItemsSource="asd" Style="{StaticResource CupertinoComboBoxStyle}" />
                    <DatePicker Style="{StaticResource CupertinoDatePickerStyle}" />
                    <TextBox Text="input" Style="{StaticResource CupertinoTextBoxStyle}" />

                </StackPanel>
            </ScrollViewer>
        </Grid>
    <Page>
    ```

> [TIP!]
> You can find the style names using these methods:
> - "Feature" section of Uno.Themes README: https://github.com/unoplatform/Uno.Themes#features
> - Going through the source code of control styles: https://github.com/unoplatform/Uno.Themes/tree/master/src/library/Uno.Cupertino/Styles/Controls
> - Check out the [Uno.Gallery web app](https://gallery.platform.uno/) (Click on the `<>` button to view xaml source)

### Section 3: Overriding Color Palette
1. Create the nested folders `Styles\` and then `Styles\Application\` under the `.Shared` project
1. Add a new Resource Dictionary `ColorPaletteOverride.xaml` under `Styles\Application\`
1. Replace the content of that `ResourceDictionary` with the source from: https://github.com/unoplatform/Uno.Themes/blob/master/src/library/Uno.Cupertino/Styles/Application/ColorPalette.xaml
1. Make a few changes to the colors:
    > Here we are replacing the last 2 characters with 00, essentially dropping the blue-channel
    ```xml
    <!-- Light Theme -->
    <ResourceDictionary x:Key="Light">
        <Color x:Key="CupertinoBlueColor">#007B00</Color>
        <!-- ... -->
	</ResourceDictionary>

    <!-- Dark Theme -->
    <ResourceDictionary x:Key="Dark">
        <Color x:Key="CupertinoBlueColor">#007B00</Color>
        <!-- ... -->
    </ResourceDictionary>

    <!-- ... -->
    ```

1. In `App.xaml`, update the line that initializes the `CupertinoColors` to include the new palette override:    
    ```diff
    <Application.Resources>
		<ResourceDictionary>
			<ResourceDictionary.MergedDictionaries>
				<!-- ... -->

	-			<CupertinoColors xmlns="using:Uno.Cupertino" />
	+			<CupertinoColors xmlns="using:Uno.Cupertino" 
   +                            ColorPaletteOverrideSource=ms-appx:///Styles/Application/ColorPaletteOverride.xaml" />
				<CupertinoResources xmlns="using:Uno.Cupertino" />

				<!-- ... -->
			</ResourceDictionary.MergedDictionaries>
		</ResourceDictionary>
	</Application.Resources>
    ```
1. Run the app, you should now see the controls using your new color scheme.

## Get the complete code

See the completed sample on GitHub: [UnoCupertinoSample](https://github.com/unoplatform/Uno.Samples/tree/master/UI/UnoCupertinoSample)

## Additional Resources
- [Uno.Cupertino](../features/uno-cupertino.md) overview
- Uno.Cupertino library repository: https://github.com/unoplatform/Uno.Themes
- Cupertino colors: https://developer.apple.com/design/human-interface-guidelines/ios/visual-design/color/

<br>

***

## Help! I'm having trouble

> [!TIP]
> If you ran into difficulties with any part of this guide, you can:
>
> * Ask for help on our [Discord channel](https://www.platform.uno/discord) - #uno-platform
> * Ask a question on [Stack Overflow](https://stackoverflow.com/questions/tagged/uno-platform) with the 'uno-platform' tag
