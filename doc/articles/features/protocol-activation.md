# Custom protocol activation

## Registering custom scheme

### iOS

Declare your custom URL scheme in `info.plist`:

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

Note both `CategoryDefault` and `CategoryBrowsable` should be listed.

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