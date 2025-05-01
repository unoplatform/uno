---
uid: Uno.Features.CustomizingUIApplicationDelegate
---

# Customizing the `UIApplicationDelegate` on iOS

Uno Platform provides the ability to provide custom behavior for `UIApplicationDelegate` on iOS in case of both native- and Skia-based rendering.

## Skia rendering

In Skia-based apps, the `App` type no longer derives from `UIApplicationDelegate`. Instead, Uno Platform provides `Uno.UI.Runtime.Skia.AppleUIKit.UnoUIApplicationDelegate`. If you decide to implement your own application lifecycle handling, create a new type that derives from it:

```csharp
public class MyApplicationDelegate : Uno.UI.Runtime.Skia.AppleUIKit.UnoUIApplicationDelegate
{
    // Your own code or overrides
}
```

You then need to inform Uno Platform to use this custom class instead of the built-in delegate by adjusting the host creation in `Main.iOS.cs`:

```csharp
using Uno.UI.Hosting;

var host = UnoPlatformHostBuilder.Create()
    .App(() => new SamplesApp.App())
    .UseAppleUIKit(builder => builder.UseUIApplicationDelegate<MyApplicationDelegate>())
    .Build();

host.Run();
```

> [!IMPORTANT]
> Make sure to call the `base` methods when you override key application lifecycle methods, so that the internals of Uno Platform are still properly executed.

## Native rendering

Your `App` class already derives from `UIApplicationDelegate`. This means you can directly override the UIKit methods this class provides:

```csharp
public App : Application
{
    // Existing code in App.xaml.cs

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        // Your custom handling

        return base.FinishedLaunching(application, launchOptions);
    }
}
```

> [!IMPORTANT]
> Make sure to call the `base` methods when you override key application lifecycle methods, so that the internals of Uno Platform are still properly executed.
