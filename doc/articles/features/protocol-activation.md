# Custom protocol activation

## iOS

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

Then handle it by overriding the `OnActivated` method in `App.xaml.cs`:

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

## UWP

Works according to Windows docs, see [Microsoft Docs](https://docs.microsoft.com/en-us/windows/uwp/launch-resume/handle-uri-activation)
