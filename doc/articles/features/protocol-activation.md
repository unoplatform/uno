---
uid: Uno.Features.ProtocolActivation
---

# Custom protocol activation

A custom URI scheme lets a browser, another app, or the operating system start your app and hand it a
URI such as `my-scheme://orders/42`. Uno Platform surfaces those activations through the Windows App
SDK `Microsoft.Windows.AppLifecycle.AppInstance` API, so the handling code is the same one a Windows
App SDK app would write.

## Platform support

|Platform|Availability|Notes|
|---|---|---|
|**Android**|✅ Available|On a cold start, and while the app is already running.|
|**iOS**|✅ Available|On a cold start, and while the app is already running — through the scene delegate for apps declaring a scene manifest, and through the application delegate otherwise.|
|**tvOS**|✅ Available|As iOS.|
|**WebAssembly**|✅ Available|Cold start only: `registerProtocolHandler` navigates the browser, so the app is always started fresh and `AppInstance.Activated` never fires for a protocol activation.|
|**Skia Desktop (Windows, macOS, Linux)**|❌ Not yet|No activation is delivered. `GetActivatedEventArgs()` reports an `ExtendedActivationKind.Launch` over the process command line.|
|**Windows (Windows App SDK head)**|✅ Available|Delivered by the Windows App SDK itself.|

`Microsoft.Windows.AppLifecycle.ActivationRegistrationManager` is not implemented on any Uno Platform
target, so a custom scheme is declared in the platform manifest as described below, never registered at
runtime.

## Registering custom scheme

### iOS & tvOS

Declare your custom URL scheme in `info.plist` in the platform head:

```xml
<key>CFBundleURLTypes</key>
<array>
    <dict>
    <key>CFBundleURLName</key>
    <string>My Useful Scheme</string>
    <key>CFBundleURLSchemes</key>
    <array>
        <string>my-scheme</string>
    </array>
    </dict>
</array>
```

The same declaration is how a macOS bundle registers a scheme, but the Skia desktop head does not yet
deliver the resulting activation to the app.

### Android

Register your protocol on the `MainActivity` with the `[IntentFilter]` attribute:

```csharp
[IntentFilter(
    new [] {
        Android.Content.Intent.ActionView
    },
    Categories = new[] {
        Android.Content.Intent.CategoryDefault,
        Android.Content.Intent.CategoryBrowsable
    },
    DataScheme = "my-scheme")]
```

If your target framework is Android 12, you must also add `Exported = true` to the `[Activity]` attribute.

`CategoryDefault` is required (must be included for all implicit intents) and `CategoryBrowsable` is optional (allows opening the custom URI from the browser).

### WebAssembly

The WebAssembly implementation uses the [`Navigator.registerProtocolHandler` API](https://developer.mozilla.org/en-US/docs/Web/API/Navigator/registerProtocolHandler).

This has several limitations when using a custom scheme:

- The custom scheme's name must begin with `web+`
- The custom scheme's name must include at least 1 letter after the `web+` prefix
- The custom scheme must have only lowercase ASCII letters in its name.

You can also use one of the following supported schemes instead:

- `bitcoin`
- `ftp`
- `ftps`
- `geo`
- `im`
- `irc`
- `ircs`
- `magnet`
- `mailto`
- `matrix`
- `mms`
- `news`
- `nntp`
- `openpgp4fpr`
- `sftp`
- `sip`
- `sms`
- `smsto`
- `ssh`
- `tel`
- `urn`
- `webcal`
- `wtai`
- `xmpp`

To register the custom scheme, call the WebAssembly-specific `Uno.Helpers.ProtocolActivation` API when appropriate to let the user confirm URI handler association:

```csharp
#if __WASM__
   Uno.Helpers.ProtocolActivation.RegisterCustomScheme(
      "web+myscheme",
      new System.Uri("http://localhost:55838/"), 
      "Can we handle web+myscheme links?");
#endif
```

The first argument is the scheme name, the second is the base URL of your application (it must match the current domain to be registered successfully), and the third is a text prompt, which will be displayed to the user to ask for permission (this does not work on all browsers e.g. edge).

When a link with the custom scheme gets executed, the browser navigates to that URL with an additional
`unoprotocolactivation` query string key carrying the custom URI. Uno Platform recognizes that key,
reports a `Protocol` activation for it, and strips the key from the launch arguments the app sees, so
`LaunchActivatedEventArgs.Arguments` only ever contains your own arguments.

### Windows

Works according to Windows docs. For more information, see [Handle URI activation | Microsoft Docs](https://learn.microsoft.com/windows/uwp/launch-resume/handle-uri-activation).

## Handling protocol activation

`OnLaunched` always runs, whatever started the app, and it always receives plain
`ActivationKind.Launch` arguments — matching WinUI, which synthesizes them regardless of the actual
activation. The activation the app was started with is read from `AppInstance`:

```csharp
var activation = AppInstance.GetCurrent().GetActivatedEventArgs();
if (activation.Kind == ExtendedActivationKind.Protocol &&
    activation.Data is ProtocolActivatedEventArgs protocol)
{
    var uri = protocol.Uri;
}
```

`GetActivatedEventArgs()` never returns `null` and never throws: an app started with no activation
payload gets an `ExtendedActivationKind.Launch` over the process command line. It always describes the
activation the process *started* with, and deliberately does not change afterwards.

An activation that arrives while the app is already running is raised on
`AppInstance.GetCurrent().Activated` instead. Subscribe in the `App` constructor, which runs before
`OnLaunched`, so that no activation can slip past.

A complete `App.xaml.cs` handling both paths:

```csharp
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;

// Microsoft.UI.Xaml.LaunchActivatedEventArgs (what OnLaunched receives) and
// Windows.ApplicationModel.Activation.LaunchActivatedEventArgs (what AppActivationArguments.Data
// carries) are distinct types, so the WinRT namespace is aliased rather than imported.
using Activation = Windows.ApplicationModel.Activation;

namespace MyApp;

public partial class App : Application
{
    private readonly Frame _rootFrame = new();

    private Window? _mainWindow;

    public App()
    {
        // Subscribed before OnLaunched runs, so a later activation cannot be missed.
        AppInstance.GetCurrent().Activated += OnAppInstanceActivated;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _mainWindow = new Window { Content = _rootFrame };

        // OnLaunched always gets plain Launch arguments, so ask AppInstance how the app really started.
        HandleActivation(AppInstance.GetCurrent().GetActivatedEventArgs());

        _mainWindow.Activate();
    }

    private void OnAppInstanceActivated(object? sender, AppActivationArguments args)
        => HandleActivation(args);

    private void HandleActivation(AppActivationArguments args)
    {
        switch (args.Kind)
        {
            case ExtendedActivationKind.Protocol when args.Data is Activation.ProtocolActivatedEventArgs protocol:
                _rootFrame.Navigate(typeof(DetailPage), protocol.Uri.AbsoluteUri);
                break;

            // A plain launch, and also a jump list item, whose Arguments arrive here.
            case ExtendedActivationKind.Launch when args.Data is Activation.LaunchActivatedEventArgs launch:
                _rootFrame.Navigate(typeof(MainPage), launch.Arguments);
                break;
        }
    }
}
```

Points worth knowing:

- **Android, iOS, tvOS and WebAssembly** deliver protocol activation; **Skia Desktop (Windows, macOS
  and Linux) does not yet**. On desktop the code above still compiles and runs — it simply only ever
  sees a `Launch` activation.
- **On WebAssembly the activation is always a cold start**, because the browser navigates to handle the
  URI. `Activated` therefore never fires for a protocol activation on that target, so read it from
  `GetActivatedEventArgs()`.
- **On a Windows App SDK head**, `AppInstance` is the Windows App SDK's own implementation and the
  `Data` payloads are the ones it produces, which are not the concrete WinRT classes. Both
  implementations do expose the matching `Windows.ApplicationModel.Activation` interfaces
  (`IProtocolActivatedEventArgs`, `ILaunchActivatedEventArgs`), so type-test against those if one
  `App.xaml.cs` has to serve the Windows head as well. See
  [rich activation](https://learn.microsoft.com/windows/apps/windows-app-sdk/applifecycle/applifecycle-rich-activation)
  for what the Windows App SDK delivers.
- **A jump list item's `Arguments` arrive as a `Launch` activation** through the same two entry points.
  See [Start Screen](xref:Uno.Features.WinUIStartScreen).
