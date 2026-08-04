---
uid: Uno.Features.CustomizingAndroidApplication
---

# Customizing the `Application` class on Android

Every Uno Platform target builds its host through `UnoPlatformHostBuilder`. On most targets that happens in a `Main` method:

```csharp
var host = UnoPlatformHostBuilder.Create()
    .App(() => new App())
    .UseWin32()
    .Build();

host.Run();
```

Android has no managed entry point to put that in. The .NET for Android SDK rewrites `OutputType` from `Exe` to `Library`, and application startup is driven entirely by the Android runtime instantiating the class named in `AndroidManifest.xml`. A `Main` method would compile, but nothing would ever call it.

Android therefore builds the same host from a virtual method instead of a `Main`. Your Android head declares an `Android.App.Application` subclass deriving from `Microsoft.UI.Xaml.NativeApplication`, and overrides `CreateHost()`:

```csharp
using Uno.UI.Hosting;

[global::Android.App.ApplicationAttribute(
    Label = "@string/ApplicationName",
    Icon = "@mipmap/icon",
    LargeHeap = true,
    HardwareAccelerated = true,
    Theme = "@style/AppTheme"
)]
public class Application : Microsoft.UI.Xaml.NativeApplication
{
    public Application(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

    protected override UnoPlatformHost CreateHost() =>
        UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseAndroid()
            .Build();
}
```

`CreateHost()` is called once, when the first `ApplicationActivity` starts. That is deliberately late: it is the earliest point at which `Uno.UI.ContextHelper.Current` is populated, which the window and display extensions depend on.

## Android-specific options

`UseAndroid()` accepts a configuration callback, in the same shape as `UseWin32()` or `UseAppleUIKit()`:

```csharp
protected override UnoPlatformHost CreateHost() =>
    UnoPlatformHostBuilder.Create()
        .App(() => new App())
        .UseAndroid(android => android.UseVulkan().UseOpenGL(false))
        .Build();
```

| Option | Description |
|--------|-------------|
| `UseVulkan(bool)` | Use the Vulkan render view when the device reports Vulkan support. Default `true`. |
| `UseOpenGL(bool)` | Accelerate the canvas render view — used whenever Vulkan is disabled or unavailable — with OpenGL ES. Default `true`. |

These are two independent switches rather than a single backend enum, because the Vulkan path can fail at runtime and fall back to the canvas render view. See [Vulkan rendering](xref:Uno.Skia.Vulkan).

## Registering API extensions

`ApiExtensibility` registrations are **first-wins**: the first registration for a given type is kept, and later ones are ignored. Register your own extensions from `Application.OnCreate()`, which runs before the host is created, so that they take precedence over the framework defaults:

```csharp
public override void OnCreate()
{
    base.OnCreate();

    ApiExtensibility.Register(typeof(IMyExtension), o => new MyExtension(o));
}
```

> [!IMPORTANT]
> Do not use the host builder's `AfterInit()` callback for this. It runs *after* the framework has registered its own extensions, so your registration would be silently discarded.

Note that `Application.OnCreate()` also runs for process entries that have no activity, such as background services and broadcast receivers. Keep it free of UI work.

## Customizing the activity

The launcher activity is declared at compile time through `[Activity(MainLauncher = true)]`, so it is not configurable from the host builder. Derive from `Microsoft.UI.Xaml.ApplicationActivity` to customize it:

```csharp
[Activity(
    MainLauncher = true,
    ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges
)]
public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
{
    protected override void OnCreate(Bundle bundle)
    {
        // Your custom handling, before the Uno Platform host starts.

        base.OnCreate(bundle);
    }
}
```

> [!IMPORTANT]
> Make sure to call the `base` methods when you override key application lifecycle methods, so that the internals of Uno Platform are still properly executed.
