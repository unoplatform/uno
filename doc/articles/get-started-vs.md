# Get Started on Visual Studio 2019 for Xamarin

## Prerequisites
* [**Visual Studio 2019 16.3 or later**](https://visualstudio.microsoft.com/), with:
    * **Universal Windows Platform** workload installed. 

      ![visual-studio-installer-uwp](Assets/quick-start/vs-install-uwp.png)

    * **All Windows SDKS 10.0.17763 or later** must be selected (on the right side of the installer)

      ![visual-studio-installer-uwp](Assets/quick-start/vs-install-uwp-sdks.png)

	* **Mobile development with .NET (Xamarin)** workload installed.

        ![visual-studio-installer-xamarin](Assets/quick-start/vs-install-xamarin.png)

        * The iOS Remote Simulator installed (for iOS development)
	    * A working Mac with Visual Studio for Mac, Xcode 8.2 or later installed (for iOS development)
	    * Google's Android x86 emulators or a physical Android device (for Android development)

    * **ASP**.**NET and web** workload installed, along with .NET 5.0 or 6.0 (for WebAssembly development)

      ![visual-studio-installer-web](Assets/quick-start/vs-install-web.png)
    * **.NET SDK**
        * [.NET 5.0 SDK](https://dotnet.microsoft.com/download/dotnet-core/5.0) (SDK 5.0.400 or later)

> [!NOTE]
> For more information about these prerequisites, see [Installing Xamarin](https://docs.microsoft.com/en-us/xamarin/get-started/installation/). For information about connecting Visual Studio to a Mac build host, see [Pair to Mac for Xamarin.iOS development](https://docs.microsoft.com/en-us/xamarin/ios/get-started/installation/windows/connecting-to-mac/).

[!include[getting-help](use-uno-check-inline.md)]

## Installing the Uno Platform Solution Templates with Visual Studio

1. Launch Visual Studio, then click `Continue without code`. Click `Extensions` -> `Manage Extensions` from the Menu Bar.

    ![](Assets/tutorial01/manage-extensions.png)

2. In the Extension Manager expand the **Online** node and search for `Uno Platform Templates`, install the <code>Uno Platform Solution Templates</code> extension or download it from the [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=nventivecorp.uno-platform-addin) extension, then restart Visual Studio.

    ![](Assets/tutorial01/uno-extensions.PNG)

## Create an application from the solution template

To easily create a multi-platform application:
* Create a new C# solution using the **Uno Platform App (Xamarin|UWP)** template, from Visual Studio's **Start Page**:
  ![new project](Assets/quick-start/vsix-new-project.png)
* Right-click on the Solution and select `Manage NuGet Packages for Solution` from the context menu.
* Update to the latest NuGet package named `Uno.UI`.
* To debug the iOS head, select the `Debug|iPhoneSimulator` configuration
* To debug the Android head, select the `Debug|Any CPU` configuration
* To debug the UWP head, select the `Debug|x86` configuration
* To run the WebAssembly (Wasm) head, select **IIS Express** and press **Ctrl+F5** or choose `Start without debugging` from the Debug menu. To enable full debugging, see these [debugging instructions](debugging-wasm.md).

You're all set! You can now head to [our tutorials](getting-started-tutorial-1.md) on how to work on your Uno Platform app.

### Make sure XAML Intellisense is enabled

[Intellisense](https://docs.microsoft.com/en-us/visualstudio/ide/using-intellisense) is supported in XAML when the UWP head is active:
![xaml-intellisense](Assets/quick-start/xaml-intellisense.png)

If XAML Intellisense isn't working on a freshly-created project, try the following steps:
1. Build the UWP head.
2. Close all XAML documents.
3. Close and reopen Visual Studio.
4. Reopen XAML documents.

### Troubleshooting Visual Studio and Uno Platform Installation Issues

You may encounter installation and/or post-installation Visual Studio issues for which workarounds exist. Please see [Common Issues](https://platform.uno/docs/articles/get-started-wizard.html) we have documented.

If you're not sure whether your environment is correctly configured for Uno Platform development, running the [`uno-check` command-line tool](external/uno.check/doc/using-uno-check.md) should be your first step.

[!include[getting-help](getting-help.md)]