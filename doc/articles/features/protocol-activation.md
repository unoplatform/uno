# Custom protocol activation

## Registering custom scheme

### iOS & macOS

Declare your custom URL scheme in `info.plist` in the platform head:

```
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

```
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

`CategoryDefault` is required (must be included for all implicit intents) and `CategoryBrowsable` is optional (allows opening the custom URI from the browser).

### WASM

WASM implementation uses the [`Navigator.registerProtocolHandler` API](https://developer.mozilla.org/en-US/docs/Web/API/Navigator/registerProtocolHandler). This has several limitations for the custom scheme:

- The custom scheme's name must begin with `web+`
- The custom scheme's name must include at least 1 letter after the `web+` prefix
- The custom scheme must have only lowercase ASCII letters in its name.

To register the custom theme, call the WASM specific `Uno.Helpers.ProtocolActivation` API when appropriate to let the user confirm URI handler association:

```
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

Custom URI activation can be handled by overriding the `OnActivated` method in `App.xaml.cs`:

```
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

Note that in line with UWP, if the application is not running, the `OnLaunched` method is not called and only `OnActivated` is executed instead. You must perform similar initialization of root app frame if it is not yet set. If the application was running, this initialization can be skipped.