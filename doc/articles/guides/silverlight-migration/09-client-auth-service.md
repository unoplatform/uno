---
uid: Uno.SilverlightMigration.ClientAuthentication
---

# Client Authentication

As noted above, the Silverlight Business Application template utilizes a WCF RIA Services backend that implements an **AuthenticationService** using ASP.NET web form authentication. The template also supported The TimeEntryRia sample application has extended the solution to use accounts stored in a custom database rather than use the default ASP.NET authentication schema - a common practice with enterprise applications.

As WCF RIA Services is no longer available, and the general approach to web services have moved on, there are a number of alternate approaches to implementing services and integrating authentication.

> [!NOTE]
> There are lots of authentication options out there - you can learn more about some of them from the resources below:
>
> * [Overview of ASP.NET Core authentication](https://learn.microsoft.com/aspnet/core/security/authentication/?view=aspnetcore-5.0)
> * [Authentication and authorization in gRPC for ASP.NET Core](https://learn.microsoft.com/aspnet/core/grpc/authn-and-authz?view=aspnetcore-5.0)
> * [IdentityServer4 Big Picture](https://identityserver4.readthedocs.io/en/latest/intro/big_picture.html)
> * [Auth0 Get Started](https://auth0.com/docs/get-started)

In the sample migration, ASP.NET Core Web APIs are used and secured using an app level client credential using IdentityServer4. The implementation of these server-side services is beyond the scope of this article, however the source can be reviewed in the sample project. This and the following tasks will walk through the client-side implementation of authentication, with the intent to show how the baseline Silverlight capability can be replicated.

## IdentityServer4 Client-side service overview

As discussed earlier, the UWP implementation of the **TimeEntryUno** application will use **IdentityServer4** to secure access to the data service APIs via a client access token. This token is then used to access the data services, such as the authentication service that validates user logins.

> [!TIP]
> The IdentityServer4 server-side implementation used in the sample mirrors the QuickStart tutorial shown below. :
>
> * [Protecting an API using Client Credentials](http://docs.identityserver.io/en/latest/quickstarts/1_client_credentials.html)

The code to retrieve the access token is encapsulated within the a class **IdentityServerClient** and uses the **HttpClient** class as well as the **IdentityModel** NuGet package.

## Install the nuget packages

1. To install the, **IdentityModel** NuGet package, right-click the solution, and select **Manage NuGet packages for solution...**

1. In the **Manage Packages for Solution** UI, select the **Browse** tab, search for **IdentityModel** and select it in the search results.

1. On the right-side of the **Manage Packages for Solution** UI, ensure the UWP and WASM projects are selected, and then click **Install**.

1. Repeat the above process, adding **System.Text.Json**.

## Implement the IdentityServerClient class

1. In the **Shared** project, create a new folder and name it **WebServices**.

1. Within the **WebServices** folder, add a new class and name it **IdentityServerClient**.

1. Replace the **using** statements with the following:

    ```csharp
    using IdentityModel.Client;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Uno.Extensions;
    ```

1. Add the following member variables:

    ```csharp
    private static HttpClient _client;
    private string _identityServerBaseAddress;
    private string _clientId;
    private string _clientSecret;
    private string _scope;
    ```

1. To create the static instance of the **HttpClient**, add following static constructor:

    ```csharp
        static IdentityServerClient()
        {
            _client = new HttpClient();
        }
    ```

    Uno allows you to reuse views and business logic across platforms. Sometimes though you may want to write different code per platform. You may need to access platform-specific native APIs and 3rd-party libraries, or want your app to look and behave differently depending on the platform. In this case, when targeting WASM, the application needs to use an alternate **HttpHandler** when running under WASM, so we have conditional code that runs only on WASM that instantiates **Uno.UI.Wasm.WasmHttpHandler()**. All other platforms (in this app we only have UWP), use an instance of **HttpClientHandler**.

    > [!TIP]
    > You can learn more about platform-specific C# and XAML here:
    >
    > * [Platform-specific C# code in Uno](/articles/platform-specific-csharp.md)
    > * [Platform-specific XAML markup in Uno](/articles/platform-specific-xaml.md)

1. To supply the required parameters to the **IdentityServerClient** class, add the following constructor:

    ```csharp
    public IdentityServerClient(string identityServerBaseAddress, string clientId, string clientSecret, string scope)
    {
        _identityServerBaseAddress = identityServerBaseAddress;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _scope = scope;
    }
    ```

    Here is an example of the constructor in use:

    ```csharp
    _identityServerClient = new IdentityServerClient(
        identityServerBaseAddress: "https://localhost:5001",
        clientId: "TimeEntryUno",
        clientSecret: "A2W7aQVFQWRX",
        scope: "TimeEntryApi");
    ```

    The **ClientId**, **ClientSecret** and **Scope** values are defined in the configuration of the **IdentityServer4** instance that is used.

1. In order to retrieve an access token from the **IdentityServer4** API, add the following method:

    ```csharp
    public async Task<string> GetAccessTokenAsync()
    {
        var discoveryResponse = await _client.GetDiscoveryDocumentAsync(address: _identityServerBaseAddress);

        if (discoveryResponse.IsError)
        {
            this.Log().LogError(discoveryResponse.Error);
            throw new Exception(discoveryResponse.Error);
        }

        var tokenResponse = await _client.RequestClientCredentialsTokenAsync(
            new ClientCredentialsTokenRequest
            {
                Address = discoveryResponse.TokenEndpoint,
                ClientId = _clientId,
                ClientSecret = _clientSecret,
                Scope = _scope
            });

        if (tokenResponse.IsError)
        {
            this.Log().LogError(tokenResponse.Error);
            throw new Exception(tokenResponse.Error);
        }

        return tokenResponse.AccessToken;
    }
    ```

    The **IdentityModel** NuGet package includes an extension method **GetDiscoveryDocumentAsync** that works with the **HttpClient** instance constructed earlier. This method sends a discovery document request to the specified **IdentityServer4** and returns a **DiscoveryDocumentResponse**.

    > [!TIP]
    > You can learn more about the **Discovery Endpoint** below:
    >
    > * [Discovery Endpoint](https://identitymodel.readthedocs.io/en/latest/client/discovery.html)

    If there is an error, it is logged and an exception is thrown (production implementations may retry, etc.), otherwise the **RequestClientCredentialsTokenAsync** extension method uses a **ClientCredentialsTokenRequest** constructed with the retrieved **DiscoveryDocumentResponse.TokenEndpoint**, **ClientId**, **ClientSecret** and **Scope**, to retrieve a **TokenResponse**. If an error occurs, it is logged and an exception thrown - again, production apps my choose to retry, etc., otherwise the access token is returned.

    > [!TIP]
    > You can learn more about the **Token Endpoint** below:
    >
    > * [Token Endpoint](https://identitymodel.readthedocs.io/en/latest/client/token.html)

    At this point the **IdentityServerClient** class implements the bare minimum necessary to retrieve an access token. It does not include retry logic or any code to manage token expiration, key rotation etc.

1. In order to authentication in the UWP project, the following capabilities must be added to the **Package.appxmanifest**:

   * EnterpriseAuthentication
   * PrivateNetwork
   * Shared User Certificates

1. The WebAssembly linker can be overly aggressive when it comes to trimming the linked code-base to minimize the application size. To ensure the code for the **IdentityModel** and **System.Text.Json** packages are not removed, open the **LinkerConfig.xml** file in the **WASM** project.

1. Update the **LinkerConfig.xml** file to match the following:

    ```xml
    <linker>
        <assembly fullname="TimeEntryUno.Wasm" />
        <assembly fullname="Uno.UI" />
        <assembly fullname="System.Text.Json" />
        <assembly fullname="IdentityModel" />
        <assembly fullname="System.Core">
            <!-- This is required by JSon.NET and any expression.Compile caller -->
            <type fullname="System.Linq.Expressions*" />
        </assembly>
    </linker>
    ```

To simplify the use of the **IdentityServerClient** class, it can be encapsulated into a singleton service. The next task will show the implementation used in the **TimeEntryUno** app.

## Next unit: Implementing a singleton token service

[![button](assets/NextButton.png)](10-implementing-singleton-token-service.md)
