---
uid: Uno.Development.MigratingToUno6
---
# Migrating to Uno Platform 6.0

Uno Platform 6.0 contains some breaking changes required to provide a consistent experience when using the Skia rendering feature, as well as **the removal of the UWP API set support** and the **removal of the GTK desktop runtime support**.

You can find a list of these changes below.

## Removal of the UWP API Set

Uno Platform started off with UWP as the common API set, then added WinUI/WinAppSDK. Both API sets were kept as a convenience for upgrading to WinUI and now that WinUI is sufficiently advanced, Uno Platform 6.0 removes the UWP API set entirely.

This makes Uno Platform easier to maintain in the long run and easier to contribute to.

If you're still using the UWP API set, you can see our [migration guides on how to move to WinUI](xref:Uno.Development.UpdatingToWinUI3).

## Removal of Skia GTK Desktop support

Over the years, we're provided desktop platform abstractions through different technologies. While desktop support started with a GTK3 shell as a common ground, Uno Platform 5.6 introduced [specialized shell support for X11, WPF and AppKit](xref:Uno.Skia.Desktop).

Now that the support for these targets is stable, we're removing the support for the GTK shell for project that did not yet use the Uno.SDK.

If your project is still using the GTK support, you can keep using Uno Platform 5.6, or [migrate to the Uno.SDK style project](https://platform.uno/docs/articles/migrating-from-previous-releases.html), which uses the latest platform support, including the new Win32 shell feature.

## List of common manual changes for Uno Platform 6.0

### Updating the Desktop Builder

Upgrading the Desktop platform builder is required when moving to Uno Platform 6.0, in order to align builders for all platforms, for all renderers.

To upgrade, in your `Platforms/Desktop/Program.cs`:

- Replace `using Uno.UI.Runtime.Skia;` with `using Uno.UI.Hosting;`
- Replace `SkiaHostBuilder` with `UnoPlatformHostBuilder`

### Use the new Win32 desktop support

Uno Platform 6.0 provides the support for a new, faster and leaner Windows support which does not depend on additional libraries (like WPF in previous Uno Platform versions). This new support also enables IL, XAML and Resource Trimming for smaller published app packages.

To upgrade to the Win32 support, in your `Platforms/Desktop/Program.cs`, replace `.UseWindows()` with `.UseWin32()`.

### Uno Extensions

Uno Platform 6.0 introduces a breaking change to the `Http` Uno Feature:

- The existing `Http` feature still exists, but no longer includes the `Uno.Extensions.Http.Refit` package by default.
- Two new features have been added:
  - **HttpRefit** – Configures `Uno.Extensions.Http.Refit` so you can register and consume REST APIs using Refit’s type-safe client generation.
  - **HttpKiota** – Configures `Uno.Extensions.Http.Kiota` so you can register and consume OpenAPI-defined endpoints using Kiota’s HTTP client generator.

> [!NOTE] If you’re upgrading and previously relied on Refit from the `Http` feature, remove `Http` from your `<UnoFeatures>` and add `HttpRefit` instead. See the [HTTP overview](xref:Uno.Extensions.Http.Overview) for more information.

### Optional use of Skia Rendering for iOS, Android and WebAssembly

In order to use Skia Rendering for these targets, we're adding new APIs that make the bootstrapping of iOS and WebAssembly platforms using common builders.

To upgrade:

- Make suer that your project is using the Uno.SDK.
- In your `.csproj`, add the following to enable Skia rendering:

    ```diff
    <UnoFeatures>
    <!-- Existing features -->
    +  SkiaRenderer;
    </UnoFeatures>
    ```

- In your `Platforms/WebAssembly/Program.cs` change this:

    ```csharp
    public static int Main(string[] args)
    {
        App.InitializeLogging();

        Microsoft.UI.Xaml.Application.Start(_ => _app = new App());

        return 0;
    }
    ```

    with the following:

    ```csharp
    using Uno.UI.Hosting;

    ...

    public static Task Main(string[] args)
    {
        App.InitializeLogging();

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseWebAssembly()
            .Build();

        await host.RunAsync();
    }
    ```

    If your startup code contained a static `_app` field, you can remove it as well. You can also use the top-level program code, as found in the default uno platform templates.

- In your `Platforms/iOS/Main.iOS.cs` change the following:

    ```csharp
    using UIKit;

    ...

    public static int Main(string[] args)
    {
        App.InitializeLogging();

        Microsoft.UI.Xaml.Application.Start(_ => _app = new App());

        return 0;
    }
    ```

    with the following:

    ```csharp
    using Uno.UI.Hosting;

    ...

    public static int Main(string[] args)
    {
        App.InitializeLogging();

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseAppleUIKit()
            .Build();

        return 0;
    }
    ```

    You can also use the top-level program code, as found in the default uno platform templates.

- In your `Platforms/Android/Main.Android.cs`, change the following:

    ```diff
    public Application(IntPtr javaReference, JniHandleOwnership transfer)
        : base(() => new App(), javaReference, transfer)
    {
    -    ConfigureUniversalImageLoader();
    }

    -private static void ConfigureUniversalImageLoader()
    -{
    -    var config = new ImageLoaderConfiguration.Builder(Context).Build();
    -    ImageLoader.Instance.Init(config);
    -    ImageSource.DefaultImageLoader = ImageLoader.Instance.LoadImageAsync;
    -}
    ```

    Uno Platform handles image loading entirely with the Skia Rendering mode.
