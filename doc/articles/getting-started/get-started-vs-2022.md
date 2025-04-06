---
uid: Uno.GetStarted.vs2022
---
## Get Started on Visual Studio 2022

This getting started will guide you through setting up your environment to create Uno Platform applications using C# and .NET with Visual Studio 2022.

> [!TIP]
> If you want to use another environment or IDE, see our [general getting started](get-started.md).

## Install Visual Studio with Workloads

To create Uno Platform applications you will need [**Visual Studio 2022 17.8 or later**](https://visualstudio.microsoft.com/vs/):

1. **ASP.NET and web development** workload installed (for WebAssembly development)

    ![Visual Studio Installer - ASP.NET and web development workload](Assets/quick-start/vs-install-web.png)

1. **.NET Multi-platform App UI development** workload installed (for iOS, Android, Mac Catalyst development).

    ![Visual Studio Installer - .NET Multi-platform App UI development workload](Assets/quick-start/vs-install-dotnet-mobile.png)

1. **.NET desktop development** workload installed (for Skia-based targets development)

    ![Visual Studio Installer - .NET desktop development workload](Assets/quick-start/vs-install-dotnet.png)

> [!NOTE]
> If you intend to do Linux development with WSL, make sure to select **.NET Debugging with WSL** in the **Individual components** section.
> [!IMPORTANT]
> Uno Platform 5.0 and later [does not support Xamarin projects anymore](xref:Uno.Development.MigratingToUno5). To build Xamarin-based projects in Visual Studio 2022, in Visual Studio's installer `Individual components` tab, search for Xamarin and select `Xamarin` and `Xamarin Remoted Simulator`. See [this section on migrating Xamarin projects](migrating-from-xamarin-to-net6.md) to .NET 6.

## Check your environment

[!include[use-uno-check](includes/use-uno-check-inline-windows-noheader.md)]

## Install the Uno Platform Extension

1. Launch Visual Studio 2022, then click `Continue without code`. Click `Extensions` -> `Manage Extensions` from the Menu Bar.  

    ![Visual Studio - "Extensions" drop-down selecting "Manage Extensions"](Assets/tutorial01/manage-extensions.png)  

2. In the Extension Manager expand the **Online** node and search for `Uno`, install the `Uno Platform` extension or download it from the [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=unoplatform.uno-platform-addin-2022), then restart Visual Studio.  

    ![Extension Manager - Uno Platform extension](Assets/tutorial01/uno-extensions.PNG)  

## Additional Setup for Windows Subsystem for Linux (WSL)

It is possible to build and debug Skia Desktop projects under WSL if you choose to do so.

To setup your environment for WSL:

- Install [WSL Ubuntu 22.04 or later](https://learn.microsoft.com/windows/wsl/install-win10)
- Install the prerequisites for Linux mentioned below, in your installed distribution using the Ubuntu shell

> [!NOTE]
> Running Uno Platform apps using WSL makes use of [WSLg's support for Wayland](https://github.com/microsoft/wslg#install-instructions-existing-wsl-install). In general, running `wsl --update`, then optionally rebooting the Windows machine can get you started.

## Additional Setup for Skia Desktop projects

### WSL

[!include[linux-setup](includes/additional-linux-setup-inline.md)]

---

## Next Steps

You're all set! You can create your [first Uno Platform app](xref:Uno.GettingStarted.CreateAnApp.VS2022).
