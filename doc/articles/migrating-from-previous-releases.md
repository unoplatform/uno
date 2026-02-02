---
uid: Uno.Development.MigratingFromPreviousReleases
---

# Migrating from Previous Releases of Uno Platform

To upgrade to the latest version of Uno Platform, [follow our guide](xref:Uno.Development.UpgradeUnoNuget).

## Uno Platform 6.5

Uno Platform 6.5 does not contain breaking changes that require attention when upgrading

- Uno App MCP: if using Visual Studio 2022/2026, sometimes the Uno App MCP does not appear in the Visual Studio tools list. See [how to make the App MCP appear in Visual Studio](xref:Uno.UI.CommonIssues.AIAgents#the-app-mcp-does-not-appear-in-visual-studio).

## Uno Platform 6.4

Uno Platform 6.4 contains an Uno.Extensions breaking changes that require attention when upgrading:

- Uno.Extensions contains an OidcClient change in namespaces. See [the migration guide](xref:Uno.Extensions.Migration) for details.

### Visual Studio, Visual Studio Code, and Rider

When upgrading to Uno Platform 6.4, make sure to update your IDE extension or plugin to the latest stable version to ensure the Uno Platform development tooling connects properly.

- [Visual Studio extension](https://aka.platform.uno/vs-extension-marketplace)
- [Visual Studio Code extension](https://aka.platform.uno/vscode-extension-marketplace)
- [Rider plugin](https://aka.platform.uno/rider-extension-marketplace)

## Uno Platform 6.3

**Uno Platform 6.3** introduces support for **.NET 10 RC1** and removes **.NET 8** targets.

### Visual Studio, Visual Studio Code, and Rider

When upgrading to Uno Platform 6.3, make sure to update your IDE extension or plugin to the latest stable version to ensure the Uno Platform development tooling connects properly.

- [Visual Studio extension](https://aka.platform.uno/vs-extension-marketplace)
- [Visual Studio Code extension](https://aka.platform.uno/vscode-extension-marketplace)
- [Rider plugin](https://aka.platform.uno/rider-extension-marketplace)

### .NET 8 Support Removed

.NET 8 targets for apps are no longer supported by Uno Platform 6.3. However, NuGet library packages that are built with `net8.0 (and earlier) and Uno 6.0 (and later) continue to be compatible with Uno Platform apps built with .NET 9 and later.
While .NET 8 itself remains a Long-Term Support (LTS) release supported until [November 2026](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), the **.NET MAUI 8** mobile workload reached end of support in [May 2025](https://dotnet.microsoft.com/en-us/platform/support/policy/maui).

You can [upgrade your project to .NET 9](xref:Uno.Development.MigratingFromNet8ToNet9) using our migration guide.

If you need to stay on .NET 8, you can continue to use **Uno Platform 6.2 or earlier** and plan your migration to .NET 9 or .NET 10 at a later time.

### .NET 10 RC1

**Uno Platform 6.3** is now built with **.NET 10** support, allowing you to upgrade existing projects or create new ones using the project wizard or CLI.

A few considerations to keep in mind:

- Moving to .NET 10 or upgrading existing projects requires using **.NET 10 RC1** along with:
  - **Visual Studio** — the latest version of [Visual Studio 2026 Insiders](https://visualstudio.microsoft.com/insiders/), as recommended by Microsoft in their [announcement](https://devblogs.microsoft.com/dotnet/dotnet-10-rc-1/#🚀-get-started).
    Use version **18.0.0 [11104.47]** or later to ensure compatibility with the [latest stable Uno Platform extension](https://aka.platform.uno/vs-extension-marketplace).
  - **Visual Studio Code** — the latest version of [Visual Studio Code](https://code.visualstudio.com/Download) and the [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) extension, as also recommended by Microsoft in the same [announcement](https://devblogs.microsoft.com/dotnet/dotnet-10-rc-1/#🚀-get-started).
  - **Rider** — the latest stable version, as .NET 10 support has been available since [Rider 2025.1](https://www.jetbrains.com/rider/whatsnew/2025-1/).

- Uno Platform provides an updated [Visual Studio extension](https://aka.platform.uno/vs-extension-marketplace) that supports **Visual Studio 2026** and the new `.slnx` solution format.
- To migrate your project to .NET 10, see our [migration guide](xref:Uno.Development.MigratingFromNet9ToNet10).

For an up-to-date list of **known issues** when using **.NET 10** with Uno Platform, please refer to our [Health Status page](https://aka.platform.uno/health-status).

### API Changes for Android

Starting with Uno Platform **6.3**, the following methods now throw `NotSupportedException` under **.NET 10** or later:

- `Microsoft.UI.Xaml.ApplicationActivity.GetTypeAssemblyFullName(string)` (Android + Native renderer)
- `Microsoft.UI.Xaml.ApplicationActivity.GetTypeAssemblyFullName(string)` (Android + Skia renderer)
- `Microsoft.UI.Xaml.NativeApplication.GetTypeAssemblyFullName(string)`

This change was introduced in [PR #21199](https://github.com/unoplatform/uno/pull/21199) ([commit 4d84ee3](https://github.com/unoplatform/uno/commit/4d84ee31adb3e7ecd3fcbdc248b79fee78211d3e)).
Methods with the [`[Java.Interop.ExportAttribute]` custom attribute](https://learn.microsoft.com/dotnet/api/java.interop.exportattribute?view=net-android-34.0) are not supported within certain runtime environments.

If your application calls any of the affected methods:

- Remove any direct usage of `GetTypeAssemblyFullName`.
- Consider removing methods with the `[Export]` custom attribute, as they may not work in certain runtime environments. Consider using managed interop or dependency injection alternatives instead.
- If you relied on these APIs for reflection or interop, migrate that logic to managed code executed at runtime.
- Rebuild and test your Android and Android+Skia apps with .NET 10 or later to ensure compatibility.

These changes prevent runtime exceptions and improve compatibility for builds on Android.

## Uno Platform 6.2

Uno Platform 6.2 does not contain breaking changes that require attention when upgrading.

## Uno Platform 6.1

Uno Platform 6.1 does not contain breaking changes that require attention when upgrading.

## Uno Platform 6.0

Uno Platform 6.0 contains breaking changes required to provide a consistent experience when using the Skia rendering feature, as well as the removal of the UWP API set support and the GTK desktop runtime support.

Read additional information about the [migration to Uno Platform 6.0](xref:Uno.Development.MigratingToUno6).

## Uno Platform 5.6

Uno Platform 5.6 contains one breaking change around using `x:Load` to align the behavior to WinUI.

### Lazy loading

To align the behavior with WinUI, lazy loading using `x:Load="False"` and `x:DeferLoadStrategy="lazy"` is no longer affected by changes to the visibility of the lazily-loaded element. Previously, binding the `Visibility` property of the lazily-loaded element and then updating the binding source to make the element visible would cause the element to materialize (i.e. load). This is no longer the case. To load the element, add an `x:Name` to the element and call `FindName` with the given name.

## Uno Platform 5.5

Uno Platform 5.5 introduces support for .NET 9 RC2 and requires installation updates for WebAssembly.

### .NET 9 RC2

A few considerations to take into account:

- Moving to .NET 9 or upgrading .NET 9 projects now require the use of .NET 9 RC2 and Visual Studio 17.12 Preview 3.
- To migrate a project to .NET 9, [read the directions](xref:Uno.Development.MigratingFromNet8ToNet9) from our documentation.

### The EnableHotReload method is deprecated

When upgrading to Uno 5.5, in the `App.xaml.cs` file, the `EnableHotReload()` method is deprecated and must be replaced with `UseStudio()` instead.

## Uno Platform 5.4

Uno Platform 5.4 contains breaking changes for Uno.Extensions.

### WinAppSDK 1.6 considerations

Uno Platform 5.4 updates to WinAppSDK 1.6 if you are using the [`Uno.SDK`](xref:Uno.Features.Uno.Sdk), which requires a temporary version adjustment until newer versions of the .NET 8 SDKs are released.

In your project, you may need to add the following lines (or uncomment them if you kept them from our templates) to get the `net8.0-windowsXX` target to build:

```xml
<PropertyGroup>
    <!-- Remove when the .NET SDK 8.0.403 or later has been released -->
    <WindowsSdkPackageVersion>10.0.19041.38</WindowsSdkPackageVersion>
</PropertyGroup>
```

Additionally, you may also get the following error message:

```text
NETSDK1198: A publish profile with the name 'win-AnyCPU.pubxml' was not found in the project.
```

This issue is not yet fixed, but it does not cause any problems during deployment.

### UWP Support for Uno.Extensions

The [`Uno.Extensions`](https://aka.platform.uno/uno-extensions) compatibility with legacy UWP apps has been removed. If your app is UWP-based and uses Uno.Extensions, in order to migrate to Uno Platform 5.4, you can keep using [previous releases of Uno.Extensions](https://github.com/unoplatform/uno.extensions/releases), or [migrate your app to WinUI](https://platform.uno/docs/articles/updating-to-winui3.html).

All the other features of Uno Platform 5.4 continue to be compatible with both UWP and WinUI apps.

### Updates in Uno.Extensions.Reactive

The generated code has changed. Make sure to [review our docs to upgrade](xref:Uno.Extensions.Reactive.Upgrading) to Uno Platform 5.4.

## Uno Platform 5.3

Uno Platform 5.3 contains an improved template and Uno.SDK versioning, new default text font, and Rider support.

The support for .NET 7 has been removed, since it was a Standard Term Support (STS) release which [ended in May 2024](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core). You can [upgrade your project to .NET 8](xref:Uno.Development.MigratingFromNet7ToNet8) using our guide.

Make sure to [re-run Uno.Check](xref:UnoCheck.UsingUnoCheck) to get all the latest dependencies.

### Visual Studio

Update your [Uno Platform extension](https://aka.platform.uno/vs-extension-marketplace) in VS 2022 to 5.3.x or later. Earlier versions may automatically update to an incorrect version of the Uno.SDK.

### Single Project updates

Similar to version 5.2, using the new project template is entirely optional and previous project structures are fully supported.

If you want to migrate to the new Single Project, you can follow the [Migrating to Single Project](xref:Uno.Development.MigratingToSingleProject) guide.

If you are upgrading from an Uno Platform 5.2 project, you can remove the following section from your `Directory.Build.props`:

```xml
  <!-- See https://aka.platform.uno/using-uno-sdk#implicit-packages for more information regarding the Implicit Packages version properties. -->
  <PropertyGroup>
    <UnoExtensionsVersion>...</UnoExtensionsVersion>
    <UnoToolkitVersion>...</UnoToolkitVersion>
    <UnoThemesVersion>...</UnoThemesVersion>
    <UnoCSharpMarkupVersion>...</UnoCSharpMarkupVersion>
  </PropertyGroup>
```

### Default Text Font

Starting from Uno Platform 5.3, the [default text font](xref:Uno.Features.CustomFonts#default-text-font) has been changed to [Open Sans](https://github.com/unoplatform/uno.fonts#open-sans-font) in the templates. This is because Uno.Sdk sets `UnoDefaultFont` MSBuild property to `OpenSans` when it's not set to something else. For more information, see [Custom fonts](xref:Uno.Features.CustomFonts).

### Rider support

The new [Uno Platform Rider plugin](https://aka.platform.uno/rider-support) support provides C# and XAML Hot Reload support, and to benefit from this new support, new projects created with Uno Platform 5.3 templates contain a new `.run` folder which provides proper support.

If your project was created using an Uno Platform 5.2 or earlier template, you can create a new temporary template with the same name as your current project (e.g. `dotnet new unoapp -o MyExistingProject`), then copy over the `.run` folder in your project.

## Uno Platform 5.2

Uno Platform 5.2 contains a new project template that supports all target frameworks into a single project, based on the Uno.Sdk that was introduced in Uno Platform 5.1.

Using this new project template is entirely optional and previous project structures are fully supported.

If you want to migrate to the new Single Project, you can follow the [Migrating to 5.2 Single Project](xref:Uno.Development.MigratingToSingleProject) guide.

## Uno Platform 5.1

Uno Platform 5.1 does not contain breaking changes that require attention when upgrading.

This version however introduces the MSBuild Uno.SDK, which provides support for smaller project files and better Visual Studio integration. Using the Uno.Sdk is entirely optional and previous project templates are fully supported. If you want to migrate to Uno.Sdk based projects, you can follow the [Migrating Projects to Single Project](xref:Uno.Development.MigratingToSingleProject) guide.

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

  - You may need to add the following property to your Android csproj:

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
        #elif __IOS__ || __TVOS__
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

- `Uno.Extensions.Logging.OSLog` which provides the ability to log the iOS system logs
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
