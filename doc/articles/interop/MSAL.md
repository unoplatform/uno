# Azure Authentication with MSAL

Uno can be used to build applications using authentication. A popular mechanism is Azure Authentication (Azure AD, Azure AD B2C or ADFS) and it can be used directly using the Microsoft Authentication Library for .NET (MSAL.NET) available from NuGet.

> MSAL.NET is the successor of ADAL.NET library which shouldn't be used for new apps. If you are migrating an application to Uno using ADAL.NET, you should first [migrate it to MSAL.NET](https://docs.microsoft.com/azure/active-directory/develop/msal-net-migration).

## Windows - UWP

Use the [`Microsoft.Identity.Client`](https://www.nuget.org/packages/Microsoft.Identity.Client/) NuGet package and follow Microsoft documentation.

Microsoft documentation: <https://docs.microsoft.com/azure/active-directory/develop/msal-overview>.

Quickstart: https://docs.microsoft.com/azure/active-directory/develop/quickstart-v2-uwp

## Android

Simply follow Microsoft's documentation:

* Official documentation <https://docs.microsoft.com/azure/active-directory/develop/msal-net-xamarin-android-considerations>

* Wiki https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Xamarin-Android-specifics

* You need to set the _ParentActivity_ this way in your code:

  ``` csharp
  _app = PublicClientApplicationBuilder
      // Add this line
      .WithParentActivityOrWindow(() => Uno.UI.ContextHelper.Current as Activity)
      [...]
      .Build();
  ```

## iOS & macOS

Follow Microsoft's documentation: <https://docs.microsoft.com/azure/active-directory/develop/msal-net-xamarin-ios-considerations>.

* You need to set the _ParentWindow_ this way in your code:

  ``` csharp
  _app = PublicClientApplicationBuilder
      // Add this line
      .WithParentActivityOrWindow(() => Window.RootViewController)
      [...]
      .Build();
  ```

## WebAssembly

Add a reference to [`Uno.Microsoft.Identity.Client`](https://www.nuget.org/packages/Uno.Microsoft.Identity.Client/) Nuget package into the Wasm head of your project.

> Important: `Uno.Microsoft.Identity.Client` is a [Fork of the MSAL.NET](https://github.com/unoplatform/Uno.Microsoft.Identity.Client) package. It is not an integration of the `MSAL.js` library. The application will have more control over the authentication process and ported application code from UWP should compile & work.

Particularities for WASM:

- Because of browser security requirements, the `redirectUri` must be on the same **protocol** (http/https), **hostname** & **port** as your application. The path is not particularly important and there's a good practice to set the callback URI to some static content defined in your `wwwroot` folder (could be an empty page). For example:

  - Define this *Redirect URI* in Azure AD: `http://localhost:5000/authentication/login-callback.htm` - for local development using the port  `5000` with `http` protocol. (Azure AD accepts non-`https` redirect URIs for localhost to simplify development - `https` will work too).

    > Note about the port number: If you're using IISExpress to run your application from VisualStudio, it could be on another port. That's the default port for Kestrel. Make sure to register the right port in Azure AD and provide the right uri at runtime.
    >
    > You'll also need to register addresses for the other environments and adjust the code to use the right IDs & URIs. The redirect Uri must always be on the same hostname & port or otherwise it won't work.

  - Optionally, a file in the Wasm project `wwwroot/authentication/login-callback.htm` with empty content (you could display a message like « _Please wait while the authentication process completes_ » for slower browsers).

- Token cache is _in-memory_ for now­. The library is not persisting the token anywhere in the browser yet. The app can save it.

## Other things

Take note of these 'do nots' in the MSAL Wiki:

<https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/do-not#do-not>

Guidance on handling errors and exceptions:

<https://docs.microsoft.com/azure/active-directory/develop/msal-handling-exceptions?tabs=dotnet>
