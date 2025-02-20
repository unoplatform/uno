---
uid: Uno.Features.AppLinks
---

# App Links

> [!TIP]
> This article covers a quick overview for enabling Universal/App Links. For a full description of the feature and instructions on using it in general, see the [Microsoft docs on Universal Links](https://learn.microsoft.com/dotnet/maui/macios/universal-links) and [App Links](https://learn.microsoft.com/dotnet/maui/android/app-links).

* Universal Links (iOS) and App Links (Android) are mechanisms that allow deep linking to specific content within your application directly from external sources, such as websites or other apps.

## Using Universal/App Links with Uno

### iOS Universal Links

1. **Update the `Main.iOS.cs` file:**
   * Add `OpenUrl` method override in your `Main` to handle Universal Links:

   ```csharp
   // ...

   public class App : UIApplication
    {
        public override void OpenUrl(NSUrl url, NSDictionary options, Action<bool>? completion)
        {
            base.OpenUrl(url, options, completion);

            Console.WriteLine($"Opened URL: {url}");
            completion?.Invoke(true);
        }
    }
   ```

1. **Enable Associated Domains:**
   * Add the `Associated Domains` entitlement to your app.
   * Update your Apple Developer Portal to include the domain in the App Identifier.

1. **Configure the apple-app-site-association file:**
   * Host this file on your server at `https://<yourdomain>/.well-known/apple-app-site-association`.
   * Example file content:

     ```json
     {
         "applinks": {
             "apps": [],
             "details": [
                 {
                     "appID": "<TEAMID>.<BUNDLEID>",
                     "paths": [ "*" ]
                 }
             ]
         }
     }
     ```

### Android App Links

1. **Update the `AndroidManifest.xml`:**
   * Add an intent filter in the manifest:

     ```xml
     <activity android:name="MainActivity">
         <intent-filter android:autoVerify="true">
             <action android:name="android.intent.action.VIEW" />
             <category android:name="android.intent.category.DEFAULT" />
             <category android:name="android.intent.category.BROWSABLE" />
             <data android:scheme="https" android:host="<yourdomain>" />
         </intent-filter>
     </activity>
     ```

1. **Verify the assetlinks.json file:**
   * Host this file on your server at `https://<yourdomain>/.well-known/assetlinks.json`.
   * Example file content:

     ```json
     [
         {
             "relation": ["delegate_permission/common.handle_all_urls"],
             "target": {
                 "namespace": "android_app",
                 "package_name": "<PACKAGE_NAME>",
                 "sha256_cert_fingerprints": [
                     "<CERTIFICATE_FINGERPRINT>"
                 ]
             }
         }
     ]
     ```

1. **Handle links in `MainActivity.Android.cs`:**
   * Override the `OnNewIntent` method in your `MainActivity` class:
  
     ```csharp
     protected override void OnNewIntent(Intent intent)
     {
         base.OnNewIntent(intent);
         
         var data = intent.DataString;
         Console.WriteLine($"Opened URL: {data}");
     }
     ```
