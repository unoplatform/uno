## Migrating from Previous Releases of Uno Platform

This article details the migration steps required to migrate from one version to the next.

### Uno 2.x to Uno 3.0

Migrating from Uno 2.x to Uno 3.0 requires a small set of changes in the code and configuration.

- **Android 8.0** is not supported anymore, you'll need to update to **Android 9.0** or **10.0**.
- For Android, you'll need to update the `Main.cs` file from:
    ```csharp
	: base(new App(), javaReference, transfer)
    ```
    to
    ```csharp
	: base(() => new App(), javaReference, transfer)
    ```
- For WebAssembly, in the `YourProject.Wasm.csproj`:
    - Change `<PackageReference Include="Uno.UI" Version="2.4.4" />` to `<PackageReference Include="Uno.UI.WebAssembly" Version="3.0.12" />`
    - Remove `<WasmHead>true</WasmHead>`
    - You can remove `__WASM__` in `DefineConstants`
- The symbols font has been updated, and the name needs to be updated. For more information, see [this article](uno-fluent-assets.md).

### Uno 3.6 

#### Optional upgrade for Microsoft.Extension.Logging

Uno Platform 3.6 templates provide an updated version of the loggers to allow the use of updated `Microsoft.Extension.Logging.*` logging packages. It is not required for applications to upgrade to these newer loggers, yet those provide additional features particularly for iOS and WebAssembly.

Here's how to upgrade:

- For all projects:
    - Remove references to the `Microsoft.Extensions.Logging.Filter` package
    - Add a reference to the `Microsoft.Extensions.Logging` package version **5.0.0**
    - Upgrade the `Microsoft.Extensions.Logging.Console` package to version **5.0.0**
- For UWP:
    - Change the reference from `Microsoft.Extensions.Logging.Console` to `Microsoft.Extensions.Logging.Debug`
- For WebAssembly:
    - Add the following line to the `LinkerConfig.xaml` file:
        ```xml
        <assembly fullname="Microsoft.Extensions.Options" />
        ```
    - Add a reference to `Uno.Extensions.Logging.WebAssembly.Console` version **1.0.1**
- For iOS:
    - Add a reference to `Uno.Extensions.Logging.OSLog` version **1.0.1**
- In the `App.xaml.cs` file:
    - Replace the `ConfigureFilters()` method with the following:
    ```csharp
                /// <summary>
                /// Configures global Uno Platform logging
                /// </summary>
                private static void InitializeLogging()
                {
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
                }
    ```
    - In the constructor, remove this call:
        ```csharp
        ConfigureFilters(global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory);
        ```
        and replace it with:
        ```csharp
        InitializeLogging();
        ```

Note that there are two new loggers:
- `Uno.Extensions.Logging.OSLog` which provides the ability to log the the iOS system logs
- `Uno.Extensions.Logging.WebAssembly.Console` which provides thread safe and colored logging to the browser debugger console

#### Migrating WebAssembly projects to .NET 5

If your WebAssembly project is using the `netstandard2.0` TargetFramework, migrating to `net5.0` can be done as follows:

- Change `<TargetFramework>netstandard2.0</TargetFramework>` to `<TargetFramework>net5.0</TargetFramework>`
- Upgrade `Uno.Wasm.Bootstrap` and `Uno.Wasm.Bootstrap.DevServer` to `2.0.0` or later
- Add a reference to the `Microsoft.Windows.Compatibility` package to `5.0.1`

You may also want to apply the changes from the section above (logger updates) to benefits from the update to .NET 5.
