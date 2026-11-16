---
uid: Uno.Features.CustomizingUIApplicationDelegate
---

# Customizing the `UIApplicationDelegate` on iOS

Uno Platform provides the ability to provide custom behavior for `UIApplicationDelegate` on iOS.

The `App` type does not derive from `UIApplicationDelegate`. Instead, Uno Platform provides `Uno.UI.Runtime.AppleUIKit.UnoUIApplicationDelegate`. If you decide to implement your own application lifecycle handling, create a new type that derives from it:

```csharp
public class MyApplicationDelegate : Uno.UI.Runtime.AppleUIKit.UnoUIApplicationDelegate
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

## Scene lifecycle

Apps built with the version 27 SDKs must use the UIKit scene lifecycle, or iOS and tvOS 27 refuse to launch them.

An app that declares a scene manifest (`UIApplicationSceneManifest`) in `Info.plist` uses it, with a scene delegate deriving from `Uno.UI.Runtime.AppleUIKit.UnoUISceneDelegate`, or `UnoUISceneDelegate` itself when the manifest names no delegate class. Secondary windows require a manifest, see [Support for secondary windows](xref:Uno.Features.WinUIWindow#support-for-secondary-windows).

An app without a manifest gets the scene lifecycle from Uno Platform when it is built with a version 27 SDK or later, whatever its .NET version, or when the SDK version can't be read: `UnoUIApplicationDelegate` assigns an Uno Platform scene delegate to the app's scene, which hosts the app's single window. Apps built with an older SDK keep the app lifecycle. To choose explicitly, set `UnoUseSceneLifecycle` in `Info.plist`:

```xml
<key>UnoUseSceneLifecycle</key>
<true/>
```

Setting it to `false` keeps the app lifecycle, which only SDKs older than 27 support. The key is ignored when the app declares a scene manifest, or when its value is neither a boolean nor a number.

At startup, Uno Platform logs which lifecycle the app runs and what decided it, at the `Information` level of the `Uno.UI.Runtime.AppleUIKit.AppleUIKitLifecycle` logger. The default [logging setup](xref:Uno.Development.Logging) only shows warnings from `Uno` loggers, so add a filter to see it:

```csharp
builder.AddFilter("Uno.UI.Runtime.AppleUIKit.AppleUIKitLifecycle", LogLevel.Information);
```

Under the scene lifecycle, UIKit no longer calls the application delegate's URL, user activity, home-screen shortcut, and foreground and background methods:

- In an app without a manifest, Uno Platform forwards them to the application delegate, so overrides such as `OpenUrl`, `ContinueUserActivity`, `PerformActionForShortcutItem`, `OnActivated` or `DidEnterBackground` keep working. The URL, shortcut or user activity the app was launched with is no longer in `FinishedLaunching`'s launch options: it reaches `OpenUrl`, `PerformActionForShortcutItem` or `ContinueUserActivity` once the scene connects, after `OnLaunched`.
- In an app with a manifest, these overrides are not called. Uno Platform reports the activations through `AppInstance`, see [Protocol activation](xref:Uno.Features.ProtocolActivation).
