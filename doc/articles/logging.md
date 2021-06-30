# Logging

Virtually every production level application should utilize some form of logging. During development, especially when developing WASM applications where using a debugger can be more challenging, diagnostic logs can be invaluable for identifying issues. Once in production, capturing information about critical failures can be invaluable.

## Logging in Uno

The Uno platform makes use of the Microsoft logging NuGet packages to provide comprehensive logging support.

> [!IMPORTANT]
> Due to current limitations regarding logging in WASM, Uno currently supports version 1.1.1 of the **Microsoft.Extensions.Logging** package and associated packages.

### Configuring logging

The standard Uno template configures logging in the **Shared** project **App.xaml.cs** file.

1. In the **Shared** project and open the **App.xaml.cs** file.

1. Locate the **App** constructor and note that logging is configured first:

    ```csharp
    public App()
    {
        ConfigureFilters(global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory);
    ```

    Notice that Uno provides a logger factory implementation.

1. Locate the **ConfigureFilters** method and review the code:

    ```csharp
    /// <summary>
    /// Configures global logging
    /// </summary>
    /// <param name="factory"></param>
    static void ConfigureFilters(ILoggerFactory factory)
    {
        factory
            .WithFilter(new FilterLoggerSettings
                {
                    { "Uno", LogLevel.Warning },
                    { "Windows", LogLevel.Warning },

                    // Debug JS interop
                    // { "Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug },

                    // Generic XAML events
                    // { "Windows.UI.Xaml", LogLevel.Debug },
                    // { "Windows.UI.Xaml.VisualStateGroup", LogLevel.Debug },
                    // { "Windows.UI.Xaml.StateTriggerBase", LogLevel.Debug },
                    // { "Windows.UI.Xaml.UIElement", LogLevel.Debug },

                    // Layouter specific messages
                    // { "Windows.UI.Xaml.Controls", LogLevel.Debug },
                    // { "Windows.UI.Xaml.Controls.Layouter", LogLevel.Debug },
                    // { "Windows.UI.Xaml.Controls.Panel", LogLevel.Debug },
                    // { "Windows.Storage", LogLevel.Debug },

                    // Binding related messages
                    // { "Windows.UI.Xaml.Data", LogLevel.Debug },

                    // DependencyObject memory references tracking
                    // { "ReferenceHolder", LogLevel.Debug },

                    // ListView-related messages
                    // { "Windows.UI.Xaml.Controls.ListViewBase", LogLevel.Debug },
                    // { "Windows.UI.Xaml.Controls.ListView", LogLevel.Debug },
                    // { "Windows.UI.Xaml.Controls.GridView", LogLevel.Debug },
                    // { "Windows.UI.Xaml.Controls.VirtualizingPanelLayout", LogLevel.Debug },
                    // { "Windows.UI.Xaml.Controls.NativeListViewBase", LogLevel.Debug },
                    // { "Windows.UI.Xaml.Controls.ListViewBaseSource", LogLevel.Debug }, //iOS
                    // { "Windows.UI.Xaml.Controls.ListViewBaseInternalContainer", LogLevel.Debug }, //iOS
                    // { "Windows.UI.Xaml.Controls.NativeListViewBaseAdapter", LogLevel.Debug }, //Android
                    // { "Windows.UI.Xaml.Controls.BufferViewCache", LogLevel.Debug }, //Android
                    // { "Windows.UI.Xaml.Controls.VirtualizingPanelGenerator", LogLevel.Debug }, //WASM
                }
            )
            .AddConsole(LogLevel.Information);
    }
    ```

    Notice that the logging levels of various categories can be added and configured.

    > [!NOTE]
    > Notice that console logging is configured by default- `.AddConsole(LogLevel.Information);`. This **does not** log output to the Visual Studio console when running a UWP app. The next task details how to add UWP logging.

## Adding UWP logging

In order to support logging to the debug output view in Visual Studio, complete the following steps

1. To install the, **IdentityModel** NuGet package, right-click the solution, and select **Manage NuGet packages for solution...**

1. In the **Manage Packages for Solution** UI, select the **Browse** tab, search for **Microsoft.Extensions.Logging.Debug** and select it in the search results.

    > [!IMPORTANT]
    > Only version 1.1.1 of the nuget package is supported by Uno at present.

1. On the right-side of the **Manage Packages for Solution** UI, ensure the UWP and WASM projects are selected, and then click **Install**.

1. In order to add UWP logging, configure `AddDebug` instead of (or in addition to) `AddConsole`. Review the code below for an example.

    Configuring logging **App.xaml.cs**

    ```csharp
    /// <summary>
    /// Configures global logging
    /// </summary>
    /// <param name="factory"></param>
    static void ConfigureFilters(ILoggerFactory factory)
    {
        factory
            .WithFilter(new FilterLoggerSettings
                {
                    { "Uno", LogLevel.Warning },
                    { "Windows", LogLevel.Warning },

                    // Debug JS interop
                    // { "Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug },

                    // Generic XAML events
                    // { "Windows.UI.Xaml", LogLevel.Debug },
                    // { "Windows.UI.Xaml.VisualStateGroup", LogLevel.Debug },
                    // { "Windows.UI.Xaml.StateTriggerBase", LogLevel.Debug },
                    // { "Windows.UI.Xaml.UIElement", LogLevel.Debug },

                    // Layouter specific messages
                    // { "Windows.UI.Xaml.Controls", LogLevel.Debug },
                    // { "Windows.UI.Xaml.Controls.Layouter", LogLevel.Debug },
                    // { "Windows.UI.Xaml.Controls.Panel", LogLevel.Debug },
                    // { "Windows.Storage", LogLevel.Debug },

                    // Binding related messages
                    // { "Windows.UI.Xaml.Data", LogLevel.Debug },

                    // DependencyObject memory references tracking
                    // { "ReferenceHolder", LogLevel.Debug },

                    // ListView-related messages
                    // { "Windows.UI.Xaml.Controls.ListViewBase", LogLevel.Debug },
                    // { "Windows.UI.Xaml.Controls.ListView", LogLevel.Debug },
                    // { "Windows.UI.Xaml.Controls.GridView", LogLevel.Debug },
                    // { "Windows.UI.Xaml.Controls.VirtualizingPanelLayout", LogLevel.Debug },
                    // { "Windows.UI.Xaml.Controls.NativeListViewBase", LogLevel.Debug },
                    // { "Windows.UI.Xaml.Controls.ListViewBaseSource", LogLevel.Debug }, //iOS
                    // { "Windows.UI.Xaml.Controls.ListViewBaseInternalContainer", LogLevel.Debug }, //iOS
                    // { "Windows.UI.Xaml.Controls.NativeListViewBaseAdapter", LogLevel.Debug }, //Android
                    // { "Windows.UI.Xaml.Controls.BufferViewCache", LogLevel.Debug }, //Android
                    // { "Windows.UI.Xaml.Controls.VirtualizingPanelGenerator", LogLevel.Debug }, //WASM
                }
            )
            //.AddConsole(LogLevel.Information);
    #if DEBUG
            .AddConsole(LogLevel.Debug)
            .AddDebug(LogLevel.Debug);

    #else
            .AddConsole(LogLevel.Information);
    #endif
    }
    ```

    Notice the use of the pre-compiler directives to change the logging level when debugging.

### Uno logging extensions

The Uno platform also provides an extension that simplifies the use of logging by making it simple to retrieve a reference to the logger.

1. In order to add logging to a class, add the following using statements namespaces:

    ```csharp
    using Microsoft.Extensions.Logging;
    using Uno.Extensions;
    ```

1. In order to write output to a log, the **Log** extension method is available on an object instance, which then exposes the operations available from **Microsoft.Extensions.Logging.LoggerExtensions**. Here is an example on how to write error and information level messages:

    ```csharp
    if (discoveryClient.IsError)
    {
        this.Log().LogError(discoveryClient.Error);
        throw new Exception(discoveryClient.Error);
    }
    this.Log().LogInformation($"UserInfoEndpoint: {discoveryClient.UserInfoEndpoint}");
    ```

    > [!TIP]
    > To learn more about the logging capabilities, review the Microsoft logging reference materials here:
    >
    > * [Logging in .NET](https://docs.microsoft.com/dotnet/core/extensions/logging)

## Log output for not implemented member usage

By default, when a member is invoked at runtime that's not implemented by Uno (ie, marked with the `[NotImplemented]` attribute), an error message is logged. 

> [!IMPORTANT]
> This feature flag must be set before the `base.InitializeComponent()` call within the `App.xaml.cs` constructor.

The logging behavior can be configured using feature flags:

 * By default, a message is only logged on the first usage of a given member. To log every time the member is invoked:
    ```csharp
    Uno.UI.FeatureConfiguration.ApiInformation.AlwaysLogNotImplementedMessages = true;
    ```

 * By default the message is logged as an error. To change the logging level:
    ```csharp
    Uno.UI.FeatureConfiguration.ApiInformation.NotImplementedLogLevel = LogLevel.Debug; // Raise not implemented usages as Debug messages
    ```
    This can be used to suppress the not implemented output, if it's not useful.
