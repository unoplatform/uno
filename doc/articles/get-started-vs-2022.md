# Get Started on Visual Studio 2022

> This section is covering **preview** releases of .NET 6 and Visual Studio 2022. It is a work in progress.

## Prerequisites
* [**Visual Studio 2022 Preview 3.1 or later**](https://visualstudio.microsoft.com/), with:
    * **Universal Windows Platform** workload installed.

    ![visual-studio-installer-uwp](Assets/quick-start/vs-install-uwp.png)

	* **Mobile development with .NET (Xamarin)** workload installed.

    ![visual-studio-installer-xamarin](Assets/quick-start/vs-install-xamarin.png)
    * Starting from VS 2022 Preview 4, select the **.NET Maui (Preview)** optional component (Installs the .NET 6 Android/iOS workloads)
    *
        * the iOS Remote Simulator installed (for iOS development)
	    * A working Mac with Visual Studio for Mac, XCode 13.5 Beta or later installed (for iOS development)
	    * Google's Android x86 emulators or a physical Android device (for Android development)

    * **ASP**.**NET and web** workload installed, along with .NET Core 5.0 (for WASM development)

    ![visual-studio-installer-web](Assets/quick-start/vs-install-web.png)

For more information about these prerequisites, see [Installing Xamarin](https://docs.microsoft.com/en-us/xamarin/get-started/installation/). For information about connecting Visual Studio to a Mac build host, see [Pair to Mac for Xamarin.iOS development](https://docs.microsoft.com/en-us/xamarin/ios/get-started/installation/windows/connecting-to-mac/).

## Finalize your environment setup using uno-check
* Install the uno-check tool:
   ```
   dotnet tool install -g Uno.Check --version 0.2.0-dev.327
   ```
   If a [later version is available](https://www.nuget.org/packages/Uno.Check), you can use it instead of 0.2.0-dev.327
* Run the uno-check tool:
   ```
   uno-check --preview
   ```

Follow the steps indicated by the tool.

## Installing the Uno Platform Solution Templates with Visual Studio

1. Launch Visual Studio 2022, then click `Continue without code`. Click `Extensions` -> `Manage Extensions` from the Menu Bar.

    ![](Assets/tutorial01/manage-extensions.png)

2. In the Extension Manager expand the **Online** node and search for `Uno`, install the <code>Uno Platform Solution Templates</code> extension or download it from the [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=nventivecorp.uno-platform-addin), then restart Visual Studio.

    ![](Assets/tutorial01/uno-extensions.PNG)

## Create an application from the solution template

To easily create a multi-platform application:
* Create a new C# solution using the **Multi-Platform App (Uno Platform | .NET 6 | UWP)** template, from Visual Studio's **Start Page**:
* Update to the latest NuGet package named `Uno.UI`. To get the very latest features, check the `pre-release` box.
* To debug the iOS:
    - In the "Debug toolbar" drop down, select framework `net6.0-ios`
    - Select an active device
    > Note that VS 2022 support for iOS is still a work in progress and may not deploy properly. Deploying to physical devices is not supported as of 17.0 Preview 2.
* To debug the Android platform:
    - In the "Debug toolbar" drop down, select framework `net6.0-android`
    - Select an active device in "Device" sub-menu
* To debug the UWP head:
    - Select the `Debug|x86` configuration
    - Debug the project
* To run the WebAssembly (Wasm) head:
   - Select **IIS Express** and press **Ctrl+F5** or choose 'Start without debugging' from the menu.

> Debugging the macOS and macCatalyst targets is not supported from Visual Studio on Windows.

### Make sure XAML Intellisense is enabled

[Intellisense](https://docs.microsoft.com/en-us/visualstudio/ide/using-intellisense) is supported in XAML when the UWP head is active:
![xaml-intellisense](Assets/quick-start/xaml-intellisense.png)

If XAML Intellisense isn't working on a freshly-created project, try the following steps:
1. Build the UWP head.
2. Close all XAML documents.
3. Close and reopen Visual Studio.
4. Reopen XAML documents.

### Video Tutorial
**To be defined**

### Troubleshooting Visual Studio and Uno Platform Installation Issues

You may encounter  installation and/or post-installation Visual Studio issues for which workarounds exist. Please see [Common Issues](https://platform.uno/docs/articles/get-started-wizard.html) we have documented.

If you're not sure whether your environment is correctly configured for Uno Platform development, running the [`uno-check` command-line tool](uno-check.md) should be your first step.

### Getting Help
If you continue experiencing issues with Visual Studio and Uno Platform, please visit our [Discord](https://www.platform.uno/discord) - #uno-platform channel or [StackOverflow](https://stackoverflow.com/questions/tagged/uno-platform) where our engineering team and community will be able to help you. 
