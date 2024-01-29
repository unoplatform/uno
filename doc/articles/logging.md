---
uid: Uno.Development.Logging
---

# Logging

Virtually every production level application should utilize some form of logging. During development, especially when developing WASM applications where using a debugger can be more challenging, diagnostic logs can be invaluable for identifying issues. Once in production, capturing information about critical failures can be invaluable.

## Logging in Uno

The Uno platform makes use of the Microsoft logging NuGet packages to provide comprehensive logging support.

### Configuring logging

The standard Uno template configures logging in the **App.xaml.cs** file.

1. Add the [`Uno.UI.Adapter.Microsoft.Extensions.Logging`](https://www.nuget.org/packages/Uno.UI.Adapter.Microsoft.Extensions.Logging/) NuGet package to your platform projects.
1. In the iOS project, add the [`Uno.Extensions.Logging.OSLog`](https://www.nuget.org/packages/Uno.Extensions.Logging.OSLog/) NuGet package to your platform projects.
1. In the WebAssembly project, add the [`Uno.Extensions.Logging.WebAssembly.Console`](https://www.nuget.org/packages/Uno.Extensions.Logging.WebAssembly.Console/) NuGet package to your platform projects.

1. Open the **App.xaml.cs** file.

1. Locate the **App** constructor and note that logging is configured first:

    ```csharp
    public App()
    {
        InitializeLogging();
    ```

    Notice that Uno provides a logger factory implementation.

1. Locate the **ConfigureFilters** method and review the code:

    ```csharp

    private static void InitializeLogging()
    {
    #if DEBUG

    // Logging is disabled by default for release builds, as it incurs a significant
    // initialization cost from Microsoft.Extensions.Logging setup. If startup performance
    // is a concern for your application, keep this disabled. If you're running on web or
    // desktop targets, you can use url or command line parameters to enable it yourself.
    //
    // For more performance documentation: https://platform.uno/docs/articles/Uno-UI-Performance.html

        var factory = LoggerFactory.Create(builder =>
        {
    #if __WASM__
            builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
    #elif __IOS__
            builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
    #elif NETFX_CORE
            builder.AddDebug();
    #else
            builder.AddConsole();
    #endif

            // Exclude logs below this level
            builder.SetMinimumLevel(LogLevel.Information);

            // Default filters for Uno Platform namespaces
            builder.AddFilter("Uno", LogLevel.Warning);
            builder.AddFilter("Windows", LogLevel.Warning);
            builder.AddFilter("Microsoft", LogLevel.Warning);

            // Generic Xaml events
            // builder.AddFilter("Windows.UI.Xaml", LogLevel.Debug );
            // builder.AddFilter("Windows.UI.Xaml.VisualStateGroup", LogLevel.Debug );
            // builder.AddFilter("Windows.UI.Xaml.StateTriggerBase", LogLevel.Debug );
            // builder.AddFilter("Windows.UI.Xaml.UIElement", LogLevel.Debug );
            // builder.AddFilter("Windows.UI.Xaml.FrameworkElement", LogLevel.Trace );

            // Layouter specific messages
            // builder.AddFilter("Windows.UI.Xaml.Controls", LogLevel.Debug );
            // builder.AddFilter("Windows.UI.Xaml.Controls.Layouter", LogLevel.Debug );
            // builder.AddFilter("Windows.UI.Xaml.Controls.Panel", LogLevel.Debug );

            // builder.AddFilter("Windows.Storage", LogLevel.Debug );

            // Binding related messages
            // builder.AddFilter("Windows.UI.Xaml.Data", LogLevel.Debug );
            // builder.AddFilter("Windows.UI.Xaml.Data", LogLevel.Debug );

            // Binder memory references tracking
            // builder.AddFilter("Uno.UI.DataBinding.BinderReferenceHolder", LogLevel.Debug );

            // RemoteControl and HotReload related
            // builder.AddFilter("Uno.UI.RemoteControl", LogLevel.Information);

            // Debug JS interop
            // builder.AddFilter("Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug );
        });

        global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

    #if HAS_UNO
        global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
    #endif

    #endif // DEBUG
    }
    ```

    Notice that the logging levels of various categories can be added and configured.

    > [!NOTE]
    > Notice that console logging is configured by default- `.AddConsole();`. This **does not** log output to the Visual Studio console when running a UWP app. The next task details how to add UWP logging.

## Adding UWP logging

In order to support logging to the debug output view in Visual Studio, complete the following steps

1. To install the, **Microsoft.Extensions.Logging.Debug** NuGet package, right-click the solution, and select **Manage NuGet packages for solution...**

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
    > * [Logging in .NET](https://learn.microsoft.com/dotnet/core/extensions/logging)

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

## IOS Unhandled Exception

```Unhandled Exception:
System.InvalidOperationException: A suitable constructor for type 'Microsoft.Extensions.Options.OptionsMonitor`1[Microsoft.Extensions.Logging.LoggerFilterOptions]' could not be located. Ensure the type is concrete and services are registered for all parameters of a public constructor.
```

If you are getting the above exception when running on iOS and the Linker Behavior is set to "Link All" it is likely that the IL linker is removing some logging classes.
See [Linking Xamarin.iOS Apps](https://learn.microsoft.com/xamarin/ios/deploy-test/linker?tabs=macos)

One option is to use `linkskip` file to exclude the assemblies causing issues.
Add the following to your `mtouch` arguments:

```console
--linkskip=Uno.Extensions.Logging.OSLog 
--linkskip=Microsoft.Extensions.Options
```

The other option is to add a [custom linker definition file](https://learn.microsoft.com/xamarin/cross-platform/deploy-test/linker)

```xml
<linker> 
  <assembly fullname="YOUR PROJECT ASSEMBLIES" />
  
  <assembly fullname="Microsoft.Extensions.Options" />

  <assembly fullname="System.Net.Http" />

  <assembly fullname="System.Core">
    <!-- This is required by JSon.NET and any expression.Compile caller -->
    <type fullname="System.Linq.Expressions*" />
  </assembly>
</linker>
```
