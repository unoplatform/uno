---
uid: Uno.SilverlightMigration.ImplementingIdentityServiceClient
---

# Implementing an identity service client

In the preceding task, the code was created to allow the application to retrieve a client access token to secure access to business APIs. In this task, the access token will be used to interact with a ASP.NET Core WebAPI service.

## Identity service endpoints

As mentioned earlier, the server-side of this service is out-of-scope for the article, however, here is the high-level definition for the identity service operations:

* **`/identity/getusers`** - GET
  * No parameters
  * Returns a JSON array containing a list of user names:

    ```json
    [
        {
            "Id": 1,
            "UserName": "aprill"
        },
        {
            "Id": 2,
            "UserName": "simond"
        },
        {
            "Id": 3,
            "UserName": "cameronc"
        }
    ]
    ```

* **`/identity/validateuser`** - POST
  * Request body:

    ```json
    {
        "userName": "string",
        "password": "string"
    }
    ```

  * Returns OK
    * "true" or "false"
* **`/identity/getauthenticateduser`** - POST
  * Request body:

    ```json
    {
        "userName": "string"
    }
    ```

  * Return OK:

    ```json
    {
        "Id": int,
        "IsAuthenticated": boolean,
        "Name": "string",
        "Roles": [
            "string",
            "string"
        ],
        "FriendlyName": "string",
        "DisplayName": "string",
        "AuthenticationType": "Custom"
    }
    ```

> [!NOTE]
> The implementation of the identity client API will be based heavily on the code discussed in the article below:
>
> * [How to consume a web service](https://platform.uno/docs/articles/howto-consume-webservices.html)

## Adding a WebApiBase class

This class is an abstract base class that every web service API class inherits from it.

1. In the **Shared** project, add a folder and name it **WebServices**.

1. Add a class to the **WebServices** folder and name it **WebApiBase**.

1. Replace the **WebApiBase.cs** file content with a copy of the **WebApiBase** class from the **Uno.Samples** GitHub repo. Update the namespace to reflect the one for this app.

    > [!TIP]
    > The **WebApiBase** source can be found below:
    >
    > * [WebApiBase.cs](https://github.com/unoplatform/Uno.Samples/blob/master/UI/TheCatApiClient/TheCatApiClient/TheCatApiClient.Shared/WebServices/WebApiBase.cs)

## Add a User class

In this task, a User model is created.

1. In the **Shared** project, add a folder and name it **Models**.

1. Add a class to the **WebServices** folder and name it **User**.

1. Update the class `using` statements as follows:

    ```csharp
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Principal;
    ```

1. Update the class definition as follows:

    ```csharp
    public class User : IPrincipal, IIdentity
    {

    }
    ```

    This class will implement the behaviors specified in the **System.Security.Principal.IPrincipal** and **System.Security.Principal.IIdentity** interfaces. A principal object represents the security context of the user on whose behalf the code is running, including that user's identity (**IIdentity**) and any roles to which they belong.

1. To add the properties required to represent the User, add the following code:

    ```csharp
    public int Id { get; set; }

    public bool IsAuthenticated { get; set; }

    public string Name { get; set; }

    public IEnumerable<string> Roles { get; set; }

    public bool IsInRole(string role)
    {
        return Roles.Contains(role);
    }
    public string FriendlyName { get; set; }

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrEmpty(this.FriendlyName))
            {
                return this.FriendlyName;
            }
            else
            {
                return this.Name;
            }
        }
    }

    public string AuthenticationType => "Custom";

    public IIdentity Identity => this;
    ```

## Add a IdentityApi class

To add a service that interacts with the Identity service, complete the following steps.

1. In the **Shared** project, add a class to the **WebServices** folder and name it **IdentityApi**.

1. Update the class `using` statements as follows:

    ```csharp
    using System.Collections.Generic;
    using System.Security.Principal;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;
    using TimeEntryUno.Shared.Models;
    ```

    > [!NOTE]
    > You may need to update the **TimeEntryUno.Shared.Models** namespace to reflect the current project.

1. Update the class definition as follows:

    ```csharp
    public sealed class IdentityApi : WebApiBase
    {

    }
    ```

    Notice that the class inherits from the **WebApiBase** class.

1. To maintain a collection of default headers to use for each HTTP operation, and maintains a reference to the service base address, add the following member variables:

    ```csharp
    private Dictionary<string, string> _defaultHeaders = new Dictionary<string, string> {
        {"accept", "application/json" }
    };

    private string _identityServiceBaseAddress;
    ```

    Notice the default **accept** header requests JSON results.

1. To implement a constructor that expects the service base address and an **Access Token**, add the following:

    ```csharp
    public IdentityApi(string identityServiceBaseAddress, string accessToken)
    {
        _identityServiceBaseAddress = identityServiceBaseAddress;
        _defaultHeaders.Add("Authorization", "Bearer " + accessToken);
    }
    ```

    Notice how the access token is added to the default headers as the **Authorization** header.

1. To implement a server that will validate a username/password pair, add the following method:

    ```csharp
    public async Task<bool> ValidateUser(string userName, string password)
    {
        var result = await this.PostAsync(
            $"{_identityServiceBaseAddress}/identity/validateuser",
            JsonSerializer.Serialize(
                new Dictionary<string, string>
                {
                    { "userName", userName },
                    { "password", password }
                }),
            _defaultHeaders);

        if (result != null)
        {
            return JsonSerializer.Deserialize<bool>(result);
        }

        return false;
    }
    ```

    Notice that the method uses the base class **PostAsync** method to send the data to the server. Also note that the username and password are passed within a custom JSON object, as well as the default headers.

    The result of the POST is deserialized and returned.

1. To retrieve more details of the validated user, add the following method:

    ```csharp
    public async Task<User> GetAuthenticatedUser(string userName)
    {
        var result = await this.PostAsync(
            $"{_identityServiceBaseAddress}/identity/getauthenticateduser",
            JsonSerializer.Serialize(
                new Dictionary<string, string>
                {
                    { "userName", userName },
                }),
            _defaultHeaders);

        if (result != null)
        {
            return JsonSerializer.Deserialize<User>(result);
        }

        return null;
    }
    ```

    This method behaviors similarly to the **ValidateUser** method above. The primary difference is the returned JSON is deserialized into the **User** model class created earlier.

## Adding an AuthenticationService

Similar to how the use of the **IdentityServerClient** class was encapsulated into a singleton **TokenService**, the **IdentityApi** service will also be encapsulated into a singleton **AuthenticationService**.

This service will not only provide the means to login and logout a user, it will maintain a reference to the current logged in user, as well as raise events based upon the current login status. These events will be used to synchronize aspects of the UI and navigation based upon the current login state.

1. In the **Shared** project, add a class to the **Services** folder and name it **IdentityApi**.

1. Update the class `using` statements as follows:

    ```csharp
    using Microsoft.Extensions.Logging;
    using System;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using TimeEntryUno.Shared.WebServices;
    using Uno.Extensions;
    ```

    > [!NOTE]
    > You may need to update the **TimeEntryUno.Shared.WebServices** namespace to reflect the current project.

1. Update the class definition as follows:

    ```csharp
    public sealed class AuthenticationService
        : SingletonBase<AuthenticationService>
    {

    }
    ```

1. To ensure the service has a private constructor, add the following:

    ```csharp
    private AuthenticationService()
    {
    }
    ```

1. To add events that are raised as the login status changes, add the following:

    ```csharp
    public event EventHandler LoggedIn;
    public event EventHandler LoggedOut;
    public event EventHandler LoginFailed;
    ```

1. To add a property to track the current logged in user, add the following:

    ```csharp
    public IPrincipal CurrentPrincipal { get; private set; }
    ```

1. To add a method to logout the current user, add the following:

    ```csharp
    public void Logout()
    {
        this.CurrentPrincipal = null;
        LoggedOut?.Invoke(this, EventArgs.Empty);
    }
    ```

    This simple method sets the current user to null and then raises the **LoggedOut** event.

1. To add a method to attempt to login with the supplied username and password, add the following:

    ```csharp
    public async Task<bool> LoginUser(string userName, string password)
    {
        var result = false;
        try
        {
            var api = new IdentityApi(
                "https://localhost:6001",
                await TokenService.Instance.GetAccessTokenAsync());

            var validUser = await api.ValidateUser(userName, password);
            if (validUser)
            {
                var authUser = await api.GetAuthenticatedUser(userName);
                if (authUser != null)
                {
                    this.CurrentPrincipal = authUser;
                    LoggedIn?.Invoke(this, EventArgs.Empty);
                    result = true;
                }
            }
            else
            {
                LoginFailed?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception e)
        {
            this.Log().LogError(e.Message);
        }

        return result;
    }
    ```

    This method uses a pattern that ensures **false** is returned if the login fails or any error occurs.

    The use of `try` and `catch` ensure any errors are logged and, in the case of an error, false is returned.

    Notice how the **IdentityApi** instance is created:

    ```csharp
    var api = new IdentityApi(
        "https://localhost:6001",
        await TokenService.Instance.GetAccessTokenAsync());
    ```

    The **IdentityApi** instance is constructed using the **TokenService** singleton. The use of `await` and the **GetAccessTokenAsync** method ensures that the **TokenService** singleton is fully initialized before retrieving the access token.

    Next, the **ValidateUser** is called - if valid, then the authenticated user is retrieved via **GetAuthenticatedUser**. The user is then assigned to the **CurrentPrincipal** property, the **LoggedIn** event is raised and **true** is returned. If the login fails, the **LoginFailed** event is raised.

## Consuming the AuthenticationService

As the **AuthenticationService** does not require asynchronous initialization, it doesn't require early initialization, therefore it can be accessed via the following code:

```csharp
// Login if we wish to wait for the login result
var isLoggedIn = await AuthenticationService.Instance.LoginUser(UserName, Password);

// Login if we are going to subscribe to login events
AuthenticationService.Instance.LoggedIn -= Instance_LoggedIn;
AuthenticationService.Instance.LoginFailed -= Instance_LoginFailed;

// discard the result
_ = AuthenticationService.Instance.LoginUser(UserName, Password);

// Logout
AuthenticationService.Instance.Logout();
```

Now that the authentication services are available, it is time to consider the authentication UI.

## Next unit: Migrate the Authentication UI

[![button](assets/NextButton.png)](12-migrate-auth-ui.md)
