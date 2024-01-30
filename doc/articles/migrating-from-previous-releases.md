---
uid: Uno.Development.MigratingFromPreviousReleases
---

# Migrating from Previous Releases of Uno Platform

This article details the migration steps required to migrate from one version to the next when breaking changes are being introduced.

## Uno Platform 5.1

Uno Platform 5.1 does not contain breaking changes that require attention when upgrading.

This version however introduces the MSBuild Uno.SDK, which provides support for smaller project files and better Visual Studio integration. Using the Uno.Sdk is entirely optional and previous projects templates are fully supported. If you want to migrate to Uno.Sdk based projects, you can follow the [Migrating Projects to Uno.Sdk](xref:Uno.Development.MigratingToUnoSdk) guide.

## Uno Platform 5.0

Uno Platform 5.0 contains binary-breaking changes in order to further align our API surface with the Windows App SDK. Most of these changes are binary-breaking changes but do not introduce behavior changes.

Additionally, this version:

- Adds support for .NET 8 for iOS, Android, Mac Catalyst, and macOS. [Follow this guide](xref:Uno.Development.MigratingFromNet7ToNet8) to upgrade from .NET 7 to .NET 8.
- Removes the support for Xamarin.iOS, Xamarin.Android, Xamarin.Mac, and netstandard2.0 for WebAssembly.
- .NET 7.0 support for iOS, Android, Mac Catalyst, and macOS remains unchanged.
- Updates the base Windows SDK version from 18362 to 19041.

Uno Platform 5.0 continues to support both UWP and WinUI API sets.

Read about additional information about the [migration to Uno Platform 5.0](xref:Uno.Development.MigratingToUno5).

## Uno Platform 4.10

This release does not require upgrade steps.

## Uno Platform 4.9

This release does not require upgrade steps.

## Uno Platform 4.8

This release does not require upgrade steps.

## Uno Platform 4.7

### Symbol Fonts

Uno Platform 4.7 now brings the Uno Fluent Symbols font implicitly. You can remove:

- The `uno-fluentui-assets.ttf` file from all your project heads,
- Any reference to `uno-fluentui-assets.ttf` in the `UIAppFonts` sections of the iOS `Info.plist`

### Breaking change with ms-appx:/// resolution

Uno Platform 4.7 brings a behavior-breaking change where the library Assets feature introduced in Uno Platform 4.6 required assembly names to be lowercase.

If you had an asset in a nuget package or project named "MyProject", you previously had to write `ms-appx:///myproject/MyAsset.txt`. With 4.7, you'll need to write `ms-appx:///MyProject/MyAsset.txt`.

## Uno Platform 4.6

### Breaking change with Android 13

The introduction of Android 13 led to a breaking change, as some Android members exposed to .NET were removed.

You'll need to migrate from `BaseActivity.PrepareOptionsPanel` to `BaseActivity.PreparePanel` instead.

## Uno Platform 4.5

### ElevatedView

The built-in `ElevatedView` control has undergone a visual unification, which means existing apps may experience slightly different shadow visuals, especially on Android, which now supports the full range of colors, including opacity. If you encounter visual discrepancies, please tweak the `Elevation` and `ShadowColor` properties to fit your needs.

## Uno Platform 4.1

### Android 12 support

Uno 4.1 removes the support for the Android SDK 10 and adds support for Android 12. Note that Android 10  versions and below are still supported at runtime, but you'll need Android 11 SDK or later to build an Uno Platform App. You can upgrade to Android 11 or 12 using the `Compile using Android version: (Target Framework)` option in Visual Studio Android project properties.

Additionally, here are some specific hints about the migration to Android 12:

- If you are building with Android 12 on Azure Devops Hosted Agents (macOS or Windows), you'll need two updates:
  - Use the JDK 11, using the following step:

    ```yml
    - pwsh: |
        echo "##vso[task.setvariable variable=JAVA_HOME]$(JAVA_HOME_11_X64)"
        echo "##vso[task.setvariable variable=JavaSdkDirectory]$(JAVA_HOME_11_X64)"
    displayName: Select JDK 11
    ```

  - You may need to [add the following property](https://github.com/tdevere/AppCenterSupportDocs/blob/main/Build/Could_not_determine_API_level_for_$TargetFrameworkVersion_of_v12.0.md) to your Android csproj:

    ```xml
    <PropertyGroup>
        <AndroidUseLatestPlatformSdk>true</AndroidUseLatestPlatformSdk>
    </PropertyGroup>
    ```

- The AndroidX libraries need to be at specific versions to avoid [an upstream android issue](https://learn.microsoft.com/answers/questions/650236/error-androidattrlstar-not-found-after-upgrading-n.html). The Uno Platform NuGet packages are using those versions automatically, but if you override those packages, make sure to avoid direct or indirect dependencies on `Xamarin.AndroidX.Core(>=1.7.0.1)`. For reference, [view this page](https://github.com/unoplatform/uno/blob/533c5316cbe7537bb2f4a542b46a52b96c75004a/build/Uno.WinUI.nuspec#L66-L69) to get the package versions used by Uno Platform.

## Uno Platform 4.0

Uno 4.0 introduces a set of binary and source-breaking changes required to align with the Windows App SDK 1.0.

To migrate your application to Uno 4.0:

- Update all `Uno.UI.*` nuget packages to 4.0
- Add a package reference to `Uno.UI.Adapter.Microsoft.Extensions.Logging` to all your project heads (Except `.Desktop` for WinUI projects and `.Windows` for UWP projects)
- In your `ConfigureLogging` method, add the following block at the end:

  ```csharp
  #if HAS_UNO
  global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
  #endif
  ```

- If you are using `ApiInformation.NotImplementedLogLevel`, use the following code instead:

  ```csharp
  global::Uno.UI.FeatureConfiguration.ApiInformation.NotImplementedLogLevel = global::Uno.Foundation.Logging.LogLevel.Debug; // Raise not implemented usages as Debug messages
  ```

- Many other smaller breaking changes that may have previously forced `#if HAS_UNO` conditionals, such as:
  - `FrameworkElement.DataContextChanged` signature
  - `FrameworkElement.Loading` signature
  - `Popup` is now correctly in the `Primitives` namespace
  - `FlyoutBase` event signatures
- Uno.UI packages no longer depend on `Uno.Core`.
  If you did depend on types or extensions provided by this package, you can take a direct dependency on it or
  use one of the sub-packages created to limit the number of transitive dependencies.
- The `Uno.UI.DualScreen` package is now renamed as`Uno.UI.Foldable`

## Uno Platform 3.6

### Optional upgrade for Microsoft.Extension.Logging

Uno Platform 3.6 templates provide an updated version of the loggers to allow the use of updated `Microsoft.Extension.Logging.*` logging packages. It is not required for applications to upgrade to these newer loggers, yet those provide additional features, particularly for iOS and WebAssembly.

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
- `Uno.Extensions.Logging.WebAssembly.Console` which provides thread-safe and colored logging to the browser debugger console

#### Migrating WebAssembly projects to .NET 5

If your WebAssembly project is using the `netstandard2.0` TargetFramework, migrating to `net5.0` can be done as follows:

- Change `<TargetFramework>netstandard2.0</TargetFramework>` to `<TargetFramework>net5.0</TargetFramework>`
- Upgrade `Uno.Wasm.Bootstrap` and `Uno.Wasm.Bootstrap.DevServer` to `2.0.0` or later
- Add a reference to the `Microsoft.Windows.Compatibility` package to `5.0.1`

You may also want to apply the changes from the section above (logger updates) to benefit from the update to .NET 5.

### Uno Platform 2.x to Uno 3.0

Migrating from Uno 2.x to Uno 3.0 requires a small set of changes in the code and configuration.

- **Android 8.0** is not supported anymore. You'll need to update to **Android 9.0** or **10.0**.
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
