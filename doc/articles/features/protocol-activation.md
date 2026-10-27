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
|**iOS**|✅ Available|On a cold start, and while the app is already running. Under the UIKit scene lifecycle, a cold-start activation arrives after `OnLaunched`, see below.|
|**tvOS**|✅ Available|As iOS.|
|**WebAssembly**|✅ Available|Cold start only: `registerProtocolHandler` navigates the browser, so the app is always started fresh and `AppInstance.Activated` never fires for a protocol activation.|
|**Skia Desktop (macOS)**|✅ Available|On a cold start, and while the app is already running. Requires the app to run as an app bundle.|
|**Skia Desktop (Windows)**|✅ Available|Cold start only. The registered command must pass the URI as `"----ms-protocol:%1"`. A further activation starts a second instance, see the single-instancing note below.|
|**Skia Desktop (Linux, X11)**|✅ Available|Cold start only, from a desktop entry passing the URI as `"----ms-protocol:%u"` or a bare `%u`. A further activation starts a second instance, see the single-instancing note below.|
|**Skia Desktop (Linux FrameBuffer)**|❌ Not yet|No activation is delivered. `GetActivatedEventArgs()` reports an `ExtendedActivationKind.Launch` over the process command line.|
|**Windows (Windows App SDK head)**|✅ Available|Delivered by the Windows App SDK itself.|

`Microsoft.Windows.AppLifecycle.ActivationRegistrationManager` is not implemented on any Uno Platform
target, so a custom scheme is declared in the platform manifest, or registered by your installer on
Windows and Linux, as described below — never registered at runtime.

## Registering custom scheme

### iOS, tvOS & macOS

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

On the Skia desktop head on macOS, add the same entry to a custom `Info.plist` and publish the app as an
app bundle with `-p:PackageFormat=app -p:UnoMacOSCustomInfoPlist=path/to/Info.plist`, as described in
[macOS advanced publishing](xref:uno.publishing.desktop.macos.advanced). LaunchServices only routes a URL
to a registered bundle, so an app started with `dotnet run` does not receive protocol activations.

Under the UIKit scene lifecycle, which apps with a scene manifest use and Uno Platform adopts for apps built
with the version 27 SDKs (see [Scene lifecycle](xref:Uno.Features.CustomizingUIApplicationDelegate#scene-lifecycle)),
UIKit delivers the activation an app starts with once its scene connects, after `OnLaunched`. In `OnLaunched`,
`GetActivatedEventArgs()` then still reports a launch, and the activation is raised on `AppInstance.Activated`
right after: handle both, as shown in [Handling protocol activation](#handling-protocol-activation).

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

> [!IMPORTANT]
> The activity must also declare the `SingleTask` launch mode, so an incoming intent reuses the running
> instance:
>
> ```csharp
> [Activity(
>     Exported = true,
>     MainLauncher = true,
>     LaunchMode = Android.Content.PM.LaunchMode.SingleTask)]
> ```
>
> Android's default `standard` launch mode creates a **new** activity instance for every incoming
> intent. A deep link arriving while your app is running therefore stacks a second copy of the activity
> on top of the first. The app's window moves to the new copy, and when that copy is closed, for example
> with Back, the window closes with it and leaves the first copy empty. `SingleTask` keeps a single
> instance and delivers the intent to it through `OnNewIntent`, which reaches the app as
> `AppInstance.Activated`.
>
> `SingleTop` is not a substitute: it only reuses the activity when it is already on top of the task the
> intent opens in. A link tapped in another app, such as a mail or chat client, usually opens in that
> app's task, which still creates a second copy.
>
> `SingleTask` has one side effect: when an intent brings the app back, including a tap on its launcher
> icon, Android finishes every activity stacked above it in its task. An activity your app started into
> its own task, such as an external camera opened for a result, is closed if the user leaves the app and
> returns through the launcher icon, and its result is lost.

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

### Skia Desktop on Windows

Windows starts the app with the URI on its command line. Uno Platform reads it in the Windows App SDK
format, `App.exe "----ms-protocol:<uri>"`, so a scheme registered by Windows App SDK tooling works
unchanged. Register the scheme for the current user under `HKEY_CURRENT_USER\Software\Classes`, for
example from your installer:

```reg
Windows Registry Editor Version 5.00

[HKEY_CURRENT_USER\Software\Classes\my-scheme]
@="URL:my-scheme"
"URL Protocol"=""

[HKEY_CURRENT_USER\Software\Classes\my-scheme\shell\open\command]
@="\"C:\Program Files\MyApp\MyApp.exe\" \"----ms-protocol:%1\""
```

The `----ms-protocol:` argument is removed from `LaunchActivatedEventArgs.Arguments`, so `OnLaunched`
only sees your own arguments. A bare `"%1"` is not treated as an activation on Windows, because it
cannot be told apart from an argument the user typed.

### Skia Desktop on Linux

Linux launchers start the app through the `Exec=` line of a desktop entry that declares the
`x-scheme-handler/<scheme>` MIME type. Save one as `~/.local/share/applications/my-app.desktop`:

```ini
[Desktop Entry]
Type=Application
Name=My App
Exec=/opt/my-app/MyApp "----ms-protocol:%u"
MimeType=x-scheme-handler/my-scheme;
```

and make it the scheme's default handler:

```bash
update-desktop-database ~/.local/share/applications
xdg-mime default my-app.desktop x-scheme-handler/my-scheme
```

A bare `Exec=/opt/my-app/MyApp %u`, as distro packages and Flatpak manifests usually write it, works
too: on Linux a single argument that is an absolute, non-`file:` URI is reported as a protocol
activation. Either way the URI is removed from `LaunchActivatedEventArgs.Arguments`.

### Windows (Windows App SDK head)

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
            // Type-test the interface, not the concrete class: on a Windows App SDK head the payload
            // is the one the Windows App SDK produced.
            case ExtendedActivationKind.Protocol when args.Data is Activation.IProtocolActivatedEventArgs protocol:
                // The URI comes from outside the app, so check it before acting on it.
                if (protocol.Uri.Scheme is "my-scheme")
                {
                    _rootFrame.Navigate(typeof(DetailPage), protocol.Uri.AbsoluteUri);
                }
                break;

            // A plain launch, and also a jump list item, whose Arguments arrive here.
            case ExtendedActivationKind.Launch when args.Data is Activation.ILaunchActivatedEventArgs launch:
                _rootFrame.Navigate(typeof(MainPage), launch.Arguments);
                break;
        }
    }
}
```

> [!IMPORTANT]
> An activation payload is untrusted input. A URI arrives from a browser or another app, and on Android
> a jump list item's arguments can be supplied by any app that starts yours with the same intent extra.
> Validate the scheme, and treat the rest as data — never pass it somewhere that would execute it, and
> do not assume a page or resource named by it should be reachable.

Points worth knowing:

- **Android, iOS, tvOS, WebAssembly, macOS, Windows and Linux X11** deliver protocol activation; **Linux
  FrameBuffer does not yet**. There the code above still compiles and runs — it simply only ever sees a
  `Launch` activation.
- **On WebAssembly the activation arrives as a cold start**, because `registerProtocolHandler` makes the
  browser navigate. `Activated` therefore does not fire for a protocol activation on that target today,
  so read it from `GetActivatedEventArgs()`. An installed progressive web app can ask the browser to
  reuse a running client instead, through the Launch Handler API; Uno Platform does not yet read that
  channel, so such an activation is currently missed.
- **On a Windows App SDK head**, `AppInstance` is the Windows App SDK's own implementation and the
  `Data` payloads are the ones it produces, which are not the concrete WinRT classes. Both
  implementations do expose the matching `Windows.ApplicationModel.Activation` interfaces
  (`IProtocolActivatedEventArgs`, `ILaunchActivatedEventArgs`), so type-test against those if one
  `App.xaml.cs` has to serve the Windows head as well. See
  [rich activation](https://learn.microsoft.com/windows/apps/windows-app-sdk/applifecycle/applifecycle-rich-activation)
  for what the Windows App SDK delivers.
- **A jump list item's `Arguments` arrive as a `Launch` activation** through the same two entry points.
  See [Start Screen](xref:Uno.Features.WinUIStartScreen).
- **Single-instancing is not implemented yet.** `AppInstance.FindOrRegisterForKey` always claims the key
  for, and returns, the current instance, and `RedirectActivationToAsync` does nothing. The canonical
  Windows App SDK pattern therefore compiles and behaves as it would on a platform that can only run one
  instance — which is correct on Android, iOS, WebAssembly and a macOS app bundle (LaunchServices hands
  the URL to the running bundle), but means a second launch on Windows or Linux starts a second instance
  rather than redirecting into the first.
- **An activation is not attributed to a window.** `AppInstance` holds one activation for the process,
  matching the Windows App SDK, so an app that opens several windows decides for itself which one should
  act on it.
- **`AppInstance` outlives any one application object.** Subscribing in the `App` constructor is right for
  an ordinary app, whose lifetime is the process's. A host that loads applications into collectible
  `AssemblyLoadContext`s — Hot Design, or anything using the secondary-application launcher — must detach
  its handler when the application is retired, or the handler keeps that context alive.
