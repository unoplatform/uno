---
uid: Uno.Interop.MSAL
---

# Azure Authentication with MSAL

Uno can be used to build applications using authentication. A popular mechanism is Azure Authentication (Azure AD, Azure AD B2C or ADFS) and it can be used directly using the Microsoft Authentication Library for .NET (MSAL.NET) available from NuGet.

> MSAL.NET is the successor of ADAL.NET library which shouldn't be used for new apps. If you are migrating an application to Uno using ADAL.NET, you should first [migrate it to MSAL.NET](https://docs.microsoft.com/azure/active-directory/develop/msal-net-migration).

Quick-start for MSAL: https://docs.microsoft.com/azure/active-directory/develop/quickstart-v2-uwp

## General usage

To use MSAL into an Uno project, follow the following steps:

1. Add a reference to [`Uno.WinUI.MSAL`](https://www.nuget.org/packages/Uno.UI.MSAL) (or `Uno.UI.MSAL` for UWP) package to all your heads - including UWP.

2. Follow [Initialize client applications using MSAL.NET](https://learn.microsoft.com/azure/active-directory/develop/msal-net-initializing-client-applications) to integrate with your app.

3. Change the `IPublicCLientApplication` initialization to add a call to `.WithUnoHelpers()` like this:

   ``` csharp
   IPublicClientApplication _app = PublicClientApplicationBuilder.Create(clientId)
       [...]
       .WithUnoHelpers() // Add this line before the .Build()
       .Build();
   ```

4. Where you are using the *Interactive* mode (`_app.AcquireTokenInteractive`), add another call to `.WithUnoHelpers()` like this:

   ``` csharp
   var authResult = await _app.AcquireTokenInteractive(scopes)
       .WithPrompt(Prompt.SelectAccount)
       [...]
       .WithUnoHelpers() // Add this line on interactive token acquisition flow
       .ExecuteAsync();
   ```

By adding those helpers, Uno will correctly add required initializations to MSAL for all supported platforms.

## Windows - UWP

There is nothing to change for UWP. The `.WithUnoHelpers()` does nothing on UAP/UWP platforms, they are just there to allow the code to compile without introducing `#if` conditionals in your code.

## Android

You'll need to setup the return URI following the Microsoft documentation:

* [Configuration requirements and troubleshooting tips for Xamarin Android with MSAL.NET](https://learn.microsoft.com/entra/identity-platform/msal-net-xamarin-android-considerations)

* [Microsoft Authentication Library for .NET](https://learn.microsoft.com/entra/msal/dotnet/)

* There is no need to call `.WithParentActivity()` as it is already initialized by `.WithUnoHelpers()`.

## iOS & macOS

Follow [Considerations for using Xamarin iOS with MSAL.NET](https://learn.microsoft.com/entra/identity-platform/msal-net-xamarin-ios-considerations).

* There is no need to call `.WithParentActivity()` as it is already initialized by `.WithUnoHelpers()`.

## WebAssembly

Particularities for WASM:

* Because of browser security requirements, the `redirectUri` must be on the same **protocol** (http/https), **hostname** & **port** as your application. The path is not particularly important and there's a good practice to set the callback URI to some static content defined in your `wwwroot` folder (could be an empty page). For example:

  * Define this *Redirect URI* in Azure AD: `http://localhost:5000/authentication/login-callback.htm` - for local development using the port  `5000` with `http` protocol. (Azure AD accepts non-`https` redirect URIs for localhost to simplify development - `https` will work too).

    > Note about the port number: If you're using IISExpress to run your application from VisualStudio, it could be on another port. That's the default port for Kestrel. Make sure to register the right port in Azure AD and provide the right uri at runtime.
    >
    > You'll also need to register addresses for the other environments and adjust the code to use the right IDs & URIs. The redirect Uri must always be on the same hostname & port or otherwise it won't work.

  * Optionally, a file in the Wasm project `wwwroot/authentication/login-callback.htm` with empty content (you could display a message like « *Please wait while the authentication process completes* » for slower browsers).

* Token cache is *in-memory* for now­. The library is not persisting the token anywhere in the browser yet. The app can save it.

## More resources

* [Best practices for MSAL.NET](https://learn.microsoft.com/entra/msal/dotnet/getting-started/best-practices)
* [Handle errors and exceptions in MSAL.NET](https://learn.microsoft.com/entra/msal/dotnet/advanced/exceptions/msal-error-handling)
