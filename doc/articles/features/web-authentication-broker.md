# Web Authentication Broker

* The timeout is set by default to 5 minutes. You can change it using `WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout`.

## WebAssembly

* The _redirect URI_ **MUST** be with the origin (protocol + hostname + port) of the application. It is not possible to use a custom scheme URI.
* When using the `<iframe>` mode (see _advanced usages_ below), the server must allow for using CSP (Content Security Policy).
* Default _redirect URI_ is `<origin>/authentication-callback`. For example `http://localhost:5000/authentication-callback`.
* It is not possible for applications to clear cookies for the authentication server when this one is from another origin. The only way clear cookies is to deploy the app and the authentication server on the same site (sharing the same origin).
* You can change the size and the initial title of the open window by setting corresponding settings in `WinRTFeatureConfiguration.WebAuthenticationBroker` .

## iOS & MacOS

* The _redirect URI_ **MUST** use a custom scheme URI and this one must be registered in the `Info.plist` of the application.
* Default _redirect URI_ will be `<scheme>:/authentication-callback`. Ex: `my-app-auth:/authentication-callback`
* The default _redirect URI_ will be automatic if there's only one custom scheme defined in the application.  If there are more that one scheme, the _default redirect URI_ must be set in the  `WinRTFeatureConfiguration.WebAuthenticationBroker.DefaultReturnUri` configuration.

## Advanced Usages

### Custom implementation

For special needs, it is possible to create a custom implementation of the Web Authentication Broker by using the `[ApiExtension]` mechanism of Uno and implementing the `IWebAuthenticationBrokerProvider` interface:

``` csharp
[assembly: ApiExtension(typeof(MyNameSpace.MyBrokerImplementation), typeof(Uno.AuthenticationBroker.IWebAuthenticationBrokerProvider))]

public class MyBrokerImplementation : IWebAuthenticationBrokerProvider
{
	Uri GetCurrentApplicationCallbackUri() => [TODO]

	Task<WebAuthenticationResult> AuthenticateAsync(WebAuthenticationOptions options, Uri requestUri, Uri callbackUri, CancellationToken ct)
    {
		[TODO]
    }
}
```

This implementation can also published as a NuGet package and it will be discovered automatically by the Uno tooling during compilation.

### WebAssembly: Use `<iframe>` instead of a browser window

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
* If you want to use a _silent_ `<iframe>`, you don't need to create a control, you can simply use the `WebAuthenticationOptions.SilentMode` as the first parameter to `WebAuthenticationBroker.AuthenticateAsync()`.
