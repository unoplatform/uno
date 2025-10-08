---
uid: Uno.Development.MigratingToUno5
---
# Migrating to Uno Platform 5.0

Uno Platform 5.0 contains binary-breaking changes in order to further align our API surface with the Windows App SDK. Most of these changes are binary-breaking changes but do not introduce behavior changes. You can find a list of these changes below.

Additionally, this version:

- Adds support for .NET 8 for iOS, Android, Mac Catalyst, and macOS. [Follow this guide](xref:Uno.Development.MigratingFromNet7ToNet8) to upgrade from .NET 7 to .NET 8.
- Removes the support for Xamarin.iOS, Xamarin.Android, Xamarin.Mac, .NET 6 (iOS/Android/Catalyst), and netstandard2.0 for WebAssembly.
- .NET 7.0 support for iOS, Android, Mac Catalyst, and macOS remains unchanged.
- Updates the base Windows SDK version from 18362 to 19041.

Uno Platform 5.0 continues to support both UWP ([Uno.UI](https://www.nuget.org/packages/uno.ui) and WinUI ([Uno.WinUI](https://www.nuget.org/packages/uno.winui)) API sets.

## List of common manual changes for Uno Platform 5.0

### Xaml generator now always uses strict search

This change ensures that the XAML parser will only look for types in an explicit way, and avoids fuzzy matching that could lead to incorrect type resolution.

In order to resolve types properly in a conditional XAML namespace, make use of the [new syntax introduced in Uno 4.8](https://platform.uno/docs/articles/platform-specific-xaml.html?q=condition#specifying-namespaces).

### Enabling Hot Reload

Hot Reload support has changed in Uno Platform 5.0 and a new API invocation is needed to restore the feature in your existing app.

- If your project is built using a shared class library, you'll need to add the following lines to the `csproj`:

    ```xml
    <ItemGroup>
        <PackageReference Include="Uno.WinUI.DevServer" Version="$UnoWinUIVersion$" Condition="'$(Configuration)'=='Debug'" />
    </ItemGroup>
    ```

    > [!NOTE]
    > If your application is using the UWP API set (Uno.UI packages) you'll need to use the `Uno.UI.DevServer` package instead.
- Then, in your `App.cs` file, add the following:

    ```csharp
    using Uno.UI;

    //... in the OnLaunched method

    #if DEBUG
            MainWindow.UseStudio();
    #endif
    ```

Note that Hot Reload has changed to be based on C#, which means that changes done XAML files will need the use of C# Hot Reload feature to be applied to the running app. See [this documentation](xref:Uno.Features.HotReload) for more details.

### Migrating from Xamarin to net7.0-* targets

If your current project is built on Xamarin.* targets, you can upgrade by [following this guide](xref:Uno.Development.MigratingFromXamarinToNet6).

### Migrating from net6.0-*to net7.0-*

There are no significant changes needed to upgrade to .NET 7 for applications (iOS, Android, Catalyst), aside from replacing all occurrences of `net6.0` to `net7.0` or `net8.0` in all your `csproj` files.

For libraries that are depending on `net6.0-android/ios/maccatalyst`, depending on the impact of the breaking changes listed below, you will need to build your libraries with Uno Platform 5.0 packages, as well as target `net7.0-*` or `net8.0-*`.

### Migrating WebAssembly from `netstandard2.0` to `net7.0` or `net8.0`

Migrating from `netstandard2.0` WebAssembly apps to `net7.0` or `net8.0` requires:

- Replacing all occurrences of `netstandard2.0` to `net7.0` or `net8.0` in all your `csproj` files
- Upgrading `Uno.Wasm.Bootstrap*` packages to version `7.0.x` or `8.0.x`, depending on your chosen target framework.

### Migrating to the new `WpfHost`

The `WpfHost` was previously hosted within a WPF `Window` class, but now it takes care of creating individual windows on its own. To migrate, follow these steps:

- Remove the `StartupUri="Wpf/MainWindow.xaml"` attribute from `App.xaml`
- Delete both `MainWindow.xaml` and `MainWindow.xaml.cs`
- In `App.xaml.cs` add the following constructor:

```csharp
public App()
{
    var host = new WpfHost(Dispatcher, () => new AppHead());
    host.Run();
}
```

### Migrating `ApplicationData` on Skia targets

Previously, `ApplicationData` was stored directly in `Environment.SpecialFolder.LocalApplicationData` folder, and all Uno Platform apps shared this single location. Starting with Uno Platform 5.0, application data are stored in application-specific folders under the `LocalApplicationData` root. For more details see the [docs](features/applicationdata.md). To perform the initial migration of existing data you need to make sure to copy the files from the root of the `LocalApplicationData` folder to `ApplicationData.Current.LocalFolder` manually using `System.IO`.

### Updating the Windows SDK from 18362 to 19041

If your existing libraries or UWP/WinAppSDK projects are targeting the Windows SDK 18362, you'll need to upgrade to 19041. A simple way of doing so is to replace all occurrences of `18362` to `19041` in all your solution's `csproj`, `.targets`, `.props`, and `.wapproj` files.

### The Uno.[UI|WinUI].RemoteControl package is deprecated

The `Uno.[UI|WinUI].RemoteControl` is deprecated and should be replaced by `Uno.WinUI.DevServer` (or `Uno.WinUI.DevServer` if you're using UWP).

This is not a required change, as the package `Uno.[UI|WinUI].RemoteControl` transitively uses `Uno.WinUI.DevServer`, but the package will eventually be phased out.

## List of other breaking changes

### `ShouldWriteErrorOnInvalidXaml` now defaults to true

Invalid XAML, such as unknown properties or unknown x:Bind targets will generate a compiler error. Those errors must now be fixed as they are no longer ignored.

### ResourceDictionary now requires an explicit Uri reference

Resources dictionaries are now required to be explicitly referenced by URI to be considered during resource resolution. Applications that are already running properly on WinAppSDK should not be impacted by this change.

The reason for this change is the alignment of the inclusion behavior with WinUI, which does not automatically place dictionaries as ambiently available.

This behavior can be disabled by using `FeatureConfiguration.ResourceDictionary.IncludeUnreferencedDictionaries`, by setting the value `true`.

### `IsEnabled` property is moved from `FrameworkElement` to `Control`

This property was incorrectly located on `FrameworkElement` but its behavior has not changed.

### `RegisterLoadActions` has been removed

This method has been removed as it is not available in WinUI. You can migrate code using this method to use the `FrameworkElement.Loaded` event instead.

### Move `SwipeControl`, `SwipeItem`, `SwipeItemInvokedEventArgs`, `SwipeMode`, `SwipeItems`, `SwipeBehaviorOnInvoked`, `MenuBar`, `MenuBarItem`, and `MenuBarItemFlyout` implementation from WUX namespace to MUX namespace

These controls were present in both the `Windows.UI.Xaml` and `Microsoft.UI.Xaml`. Those are now located in the `Microsoft.UI.Xaml` namespace for the UWP version of Uno (Uno.UI).

### Move `AnimatedVisualPlayer`, `IAnimatedVisualSource`, and `IThemableAnimatedVisualSource` from WUX to MUX and `Microsoft.Toolkit.Uwp.UI.Lottie` namespace to `CommunityToolkit.WinUI.Lottie`

This change moves the `AnimatedVisualPlayer` to the appropriate namespace for WinUI, aligned with the WinAppSDK version of the Windows Community Toolkit.

### `SimpleOrientationSensor` should not schedule on `Dispatcher`

`SimpleOrientationSensor` on Android now raises events from a background thread and needs to be explicitly dispatched to the UI thread as needed.

### The following types are removed from public API: `DelegateCommand`, `DelegateCommand<T>`, `IResourceService`, `IndexPath`, and `SizeConverter`

These legacy classes have been removed. Use the [`CommunityToolkit.Mvvm`](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) package instead.

### `SolidColorBrushHelper` isn't available in UWP, so we are making it internal

This type is not present in WinUI, use `new SolidColorBrush(xxx)` instead.

### Application.OnWindowCreated does not exist in WinAppSDK

The method has been removed.

### `DependencyObjectGenerator` no longer generated an empty ApplyCompiledBindings method

These methods have been unused since Uno 3.0 and have been removed.

### `EasingFunctionBase` API is now aligned with WinUI

The easing functions classes are now inheriting properly from `EasingFunctionBase`.

### Remove `ResourcesService` and `ResourceHelper`

The legacy `ResourcesService` and `ResourceHelper` have been removed.

### Implicit conversion from string to `Point` is removed

The implicit conversion has been removed but a conversion can be performed using the `XamlBindingHelper` class.

### Rename `CreateTheamableFromLottieAsset` to `CreateThemableFromLottieAsset`

This change is a rename for an identifier typo. Renaming the invocation to the new name is enough.

### Timeline shouldn't implement IDisposable

Timeline does not implement `IDisposable` anymore. This class was not supposed to be disposable and has been removed.

### `GridExtensions` and `PanelExtensions` are removed

These legacy classes have been removed.

### `GetLeft`, `GetTop`, `SetLeft`, `SetTop`, `GetZIndex`, and `SetZIndex` overloads that take DependencyObject are now removed

Those legacy overloads are now removed and the UIElement overloads should be used instead.

### BaseFragment is not needed and is now removed

The legacy BaseFragment class has been removed. Use `Page` navigation instead.

### `ViewHelper.GetScreenSize` method on iOS/macOS is now internal

Use the `DisplayInformation` class instead.

### `FrameworkElement` constructors are now protected instead of public

Use the `Control` class instead.

### `ViewHelper.MainScreenScale` and `ViewHelper.IsRetinaDisplay` are removed in iOS and macOS

Use the `DisplayInformation` class instead.

### WebView.NavigateWithHttpRequestMessage parameter type is now `Windows.Web.Http.HttpRequestMessage` instead of `System.Net.Http.HttpRequestMessage`

Use the new types instead.

### `OnVerticalOffsetChanged` and `OnHorizontalOffsetChanged` are now private instead of virtual protected

Use the related events instead.

### `FrameBufferHost` and `GtkHost` constructors that take `args` are now removed. `args` were already unused

You can remove the last argument of the constructor invocation. The parameters are read by the host directly.

### `RegisterDefaultStyleForType` methods were removed

This legacy method was deprecated in Uno 3.x

### Xaml code generator was generating an always-null-returning FindResource method. This method is no longer generated

This legacy method was deprecated in Uno 3.x

### The type `Windows.Storage.Streams.InMemoryBuffer` is removed

Use `Windows.Storage.Streams.Buffer` instead.

### `ContentPropertyAttribute.Name` is now a field to match UWP

This change has no effect on Control's behavior.

### Remove `FontWeightExtensions` and `CssProviderExtensions`

`Uno.UI.Runtime.Skia.GTK.UI.Text.FontWeightExtensions` and `Uno.UI.Runtime.Skia.GTK.Extensions.Helpers.CssProviderExtensions` don't exist in UWP/WinUI. So they are made internal.

### Change `GtkHost`, `WpfHost`, `FrameBufferHost` namespaces

`GtkHost`, `WpfHost`, and `FrameBufferHost` are now `Uno.UI.Runtime.Skia.Gtk.GtkHost`, `Uno.UI.Runtime.Skia.Wpf.WpfHost`, and `Uno.UI.Runtime.Skia.Linux.FrameBuffer.FrameBufferHost` instead of `Uno.UI.Runtime.Skia.GtkHost`, `Uno.UI.Skia.Platform.WpfHost`, and `Uno.UI.Runtime.Skia.FrameBufferHost`, respectively.

### Change `RenderSurfaceType` namespace

There used two be two `RenderSurfaceType`s, `Uno.UI.Runtime.Skia.RenderSurfaceType` (for Gtk) and `Uno.UI.Skia.RenderSurfaceType` (for Skia). They are now `Uno.UI.Runtime.Skia.Gtk.RenderSurfaceType` and `Uno.UI.Runtime.Skia.Wpf.RenderSurfaceType` respectively.

### `Panel`s no longer measure or arrange any children in `MeasureOverride` or `ArrangeOverride`, respectively

`Panel`s used to measure and arrange the first child in `MeasureOverride` or `ArrangeOverride`, respectively. This is no longer the case. Now, to match WinUI, `Panel`s just return an empty size in `MeasureOverride`, and the `finalSize` as is in `ArrangeOverride`. You should override these layout-override methods in `Panel`-derived subclasses instead.

### Remove `Windows.UI.Xaml.Controls.NavigationView` in Uno.WinUI

Uno.WinUI used to have NavigationView both in `Microsoft.UI.Xaml.Controls` and `Windows.UI.Xaml.Controls` namespaces. It's now removed from `Windows.UI.Xaml.Controls` and kept only in `Microsoft.UI.Xaml.Controls` namespace.
