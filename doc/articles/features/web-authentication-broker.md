---
uid: Uno.Features.WAB
---

# Web Authentication Broker

* The timeout is set by default to 5 minutes. You can change it using `WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout`.

## Usage on WebAssembly

* The *redirect URI* **MUST** be with the origin (protocol + hostname + port) of the application. It is not possible to use a custom scheme URI.
* When using the `<iframe>` mode (see *advanced usages* below), the server must allow for using CSP (Content Security Policy).
* Default *redirect URI* is `<origin>/authentication-callback`. For example `http://localhost:5000/authentication-callback`.
* It is not possible for applications to clear cookies for the authentication server when this one is from another origin. The only way clear cookies is to deploy the app and the authentication server on the same site (sharing the same origin).
* You can change the size and the initial title of the open window by setting corresponding settings in `WinRTFeatureConfiguration.WebAuthenticationBroker` .

## Usage on iOS & macOS

* The *redirect URI* **MUST** use a custom scheme URI and this scheme must be registered in the `Info.plist` of the application.
* Default *redirect URI* will be `<scheme>:/authentication-callback`. Ex: `my-app-auth:/authentication-callback`
* The default *redirect URI* will be automatic if there's only one custom scheme defined in the application. If there are more than one scheme, the first one will be used. You may want to set the right one using the `WinRTFeatureConfiguration.WebAuthenticationBroker.DefaultReturnUri` property.

## Usage on Android

* The *redirect URI* **MUST** use a custom scheme URI. This one will launch a special *Activity* declared in the application.

* You **MUST** declare an activity inheriting from `WebAuthenticationBrokerActivityBase` in the Android head. Note the `[Activity]` attribute needs to include `Exported = true` if you are targeting Android 12.

  ``` csharp
  // Android: add this class near the MainActivity, in the head project
  [Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
  [IntentFilter(
      new[] {Android.Content.Intent.ActionView},
      Categories = new[] {Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable},
      // To be changed for a scheme specific to the application
      DataScheme = "myapplication")]
  public class WebAuthenticationBrokerActivity : WebAuthenticationBrokerActivityBase
  {
      // Note: the name of this class is not important
  }
  ```

* To use the automatic discovery of the *redirect URI*, it is required to set the `IntentFilter` using the attributes like in the previous point. If you put it in the manifest, you'll need to set the URI using the `WinRTFeatureConfiguration.WebAuthenticationBroker.DefaultReturnUri` property.

* Default *redirect URI* will be `<scheme>:/authentication-callback`. Ex: `my-app-auth:/authentication-callback`

* The default implementation of the `WebAuthenticationBroker` on Android will launch the system browser and the result will come back through the custom scheme of the *Return Uri*. The *AndroidX Chrome Custom Tabs* may also be used. *Advanced* section below contains instructions about this.

## Advanced Usages

### Custom Implementation

For special needs, it is possible to create a custom implementation of the Web Authentication Broker by using the `[ApiExtension]` mechanism of Uno and implementing the `IWebAuthenticationBrokerProvider` interface:

``` csharp
[assembly: ApiExtension(typeof(MyNameSpace.MyBrokerImplementation), typeof(Uno.AuthenticationBroker.IWebAuthenticationBrokerProvider))]

public class MyBrokerImplementation : Uno.AuthenticationBroker.IWebAuthenticationBrokerProvider
{
    Uri GetCurrentApplicationCallbackUri() => [TODO]

    Task<WebAuthenticationResult> AuthenticateAsync(WebAuthenticationOptions options, Uri requestUri, Uri callbackUri, CancellationToken ct)
    {
        [TODO]
    }
}
```

This implementation can also published as a NuGet package and it will be discovered automatically by the Uno tooling during compilation.

### WebAssembly: How to use `<iframe>` instead of a browser window

On WebAssembly, it is possible to use an in-application `<iframe>` instead of opening a new window. Beware **the authentication server must support this mode**.

1. Create an `<iframe>` control:

   ``` csharp
   [HtmlElement("iframe")]
   public class LoginIFrame : Control
   {
       public LoginIFrame()
       {
           // A background is required to allow interactions
           // with the control
           Background = new SolidColorBrush(Colors.Transparent);
       }
   }
   ```

2. Use the `LoginIFrame` control in the page:

   ``` xml
   <Page ...>
       <Grid>
           [...]
           <controls:LoginIFrame x:name="loginWebView" />
       </Grid>
   </Page>
   ```

3. Set the `HtmlId` before calling the `WebAuthenticationBroker`:

   ``` csharp
   private async void LoginHidden_Click(object sender, RoutedEventArgs e)
   {
       // Set configuration to use the control as the iframe control
       WinRTFeatureConfiguration.WebAuthenticationBroker.IFrameHtmlId = loginWebView.GetHtmlId();
       try
       {
           var userResult = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, _startUri);
           [...]
       }
       finally
       {
           // Don't forget to reset it when finished
           WinRTFeatureConfiguration.WebAuthenticationBroker.IFrameHtmlId = null;
       }
   }
   ```

NOTES:

* The IFrame control should be present in the visual tree or the user won't see it.
* If you want to use a *silent* `<iframe>`, you don't need to create a control, you can simply use the `WebAuthenticationOptions.SilentMode` as the first parameter to `WebAuthenticationBroker.AuthenticateAsync()`.
