---
uid: Uno.Features.ProtocolActivation
---

# Custom protocol activation

## Registering custom scheme

### iOS & macOS

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

### Android

Register your protocol on the `MainActivity` with the `[IntentFilter]` attribute:

```xml
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

### WASM

WASM implementation uses the [`Navigator.registerProtocolHandler` API](https://developer.mozilla.org/en-US/docs/Web/API/Navigator/registerProtocolHandler). This has several limitations for the custom scheme:

- The custom scheme's name must begin with `web+`
- The custom scheme's name must include at least 1 letter after the `web+` prefix
- The custom scheme must have only lowercase ASCII letters in its name.

To register the custom theme, call the WASM specific `Uno.Helpers.ProtocolActivation` API when appropriate to let the user confirm URI handler association:

```csharp
#if __WASM__
   Uno.Helpers.ProtocolActivation.RegisterCustomScheme(
      "web+myscheme", 
      new System.Uri("http://localhost:55838/"), 
      "Can we handle web+myscheme links?");
```

The first argument is the scheme name, the second is the base URL of your application (it must match the current domain to be registered successfully), and the third is a text prompt, which will be displayed to the user to ask for permission.

When a link with the custom scheme gets executed, the browser will navigate to a your URL with additional `unoprotocolactivation` query string key, which will contain the custom URI. Uno internally recognizes this query string key and executes `OnActivated` appropriately.

### UWP

Works according to Windows docs, see [Microsoft Docs](https://docs.microsoft.com/en-us/windows/uwp/launch-resume/handle-uri-activation)

## Handling protocol activation

Custom URI activation can be handled by overriding the `OnActivated` method in `App.cs` or `App.xaml.cs`:

```csharp
protected override void OnActivated(IActivatedEventArgs e)
{
    // Note: Ensure the root frame is created
    
    if (e.Kind == ActivationKind.Protocol)
    {
        var protocolActivatedEventArgs = (ProtocolActivatedEventArgs)e;
        var uri = protocolActivatedEventArgs.Uri;
        // do something
    }
}
```

Note that in line with UWP, if the application is not running, the `OnLaunched` method is not called and only `OnActivated` is executed instead. You must perform similar initialization of root app frame and activate the current `Window` at the end. If the application was running, this initialization can be skipped.

A full application lifecycle handling with shared logic between `OnLaunched` and `OnActivated` could look as follows:

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var rootFrame = GetOrCreateRootFrame(e);
    if (e.PrelaunchActivated == false)
    {
        if (rootFrame.Content == null)
        {
            rootFrame.Navigate(typeof(MainPage), e.Arguments);
        }
        Window.Current.Activate();
    }
}

protected override void OnActivated(IActivatedEventArgs args)
{
    var rootFrame = GetOrCreateRootFrame(args);
    if (args.Kind == ActivationKind.Protocol)
    {
        var protocolActivatedEventArgs = (ProtocolActivatedEventArgs)args;
        var uri = protocolActivatedEventArgs.Uri;

        rootFrame.Navigate(typeof(DetailPage), uri.AbsoluteUri);
        Window.Current.Activate();
    }
}

private Frame GetOrCreateRootFrame(IActivatedEventArgs eventArgs)
{
    var rootFrame = Window.Current.Content as Frame;

    if (rootFrame == null)
    {
        rootFrame = new Frame();
        rootFrame.NavigationFailed += OnNavigationFailed;

        if (eventArgs.PreviousExecutionState == ApplicationExecutionState.Terminated)
        {
            // Load state from previously suspended application
        }

        Window.Current.Content = rootFrame;
    }

    return rootFrame;
}
```
