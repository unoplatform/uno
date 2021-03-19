# Web Authentication Broker

* The timeout is set by default to 5 minutes. You can change it using `WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout`.

## WebAssembly

* The _redirect uri_ **MUST** be with the origin (protocol + hostname + port) of the application. It's not possible to use a custom scheme uri.
* If you use the `<iframe>` mode (see _advanced usages_ below), the server **MUST** allows it using the CSP (Content Security Policy).
* Default _redirect uri_ is `<origin>/authentication-callback`. Ex: `http://localhost:5000/authentication-callback`.
* There is **NO WAY** for applications to clear cookies for the authentication server when this one is on another origin. The only way to do that is to deploy the app and the authentication server on the same site (sharing the same origin).
* You can change the size and the initial title of the opened window by setting corresponding settings in `WinRTFeatureConfiguration.WebAuthenticationBroker` .

## iOS & MacOS

* The _redirect uri_ **MUST** use a custom scheme uri and this one must be registered in the `Info.plist` of the application.
* Default _redirect uri_ will be `<scheme>:/authentication-callback`. Ex: `my-app-auth:/authentication-callback`
* The default _redirect uri_ will be automatic if there's only one custom scheme defined in the application.  If there's more that one scheme, you must specify the _default redirect uri_ by setting the  `WinRTFeatureConfiguration.WebAuthenticationBroker.DefaultReturnUri` configuration.

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

On WebAssembly, it is possible to use an in-application `<iframe>` instead of opening a new window. Beware **YOUR AUTHENTICATION SERVER MUST SUPPORT THIS**.

1. Create a `<iframe>` control:

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

2. Use your control in your page:

   ``` xml
   <Page ...>
       <Grid>
           [...]
           <controls:LoginIFrame x:name="loginWebView" />
       </Grid>
   </Page>
   ```

3. Set the HtmlId before calling the AuthenticationBroker:

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

* Your IFrame control should be present in the VisualTree or the user won't see anything.
* If you want to use a _silent_ `<iframe>`, you don't need to create a control, you can simply use the `WebAuthenticationOptions.SilentMode` as first parameter to `WebAuthenticationBroker.AuthenticateAsync()`.











