---
uid: Uno.SilverlightMigration.ImplementingSingletonTokenService
---

# Implementing a singleton token service

It is common to use the **Singleton** pattern when creating services that are consumed in multiple places in an application's codebase. The singleton pattern helps enforce the notion that only one instance of a service should exist. The following steps will walk through the implementation of generic abstract base class for a singleton and the use of it in the creation of the **TokenService**.

> [!TIP]
> If you want to learn more about the **Singleton** pattern and how it can be implemented in C#, review the following resource:
>
> * [Implementing the Singleton Pattern in C#](https://csharpindepth.com/Articles/Singleton)

As detailed in the document above, singletons share common characteristics:

* A single constructor, which is private and parameter-less.
* The class is sealed.
* A static variable which holds a reference to the single created instance, if any.
* A public static means of getting the reference to the single created instance, creating one if necessary.

> [!NOTE]
> When using Dependency Injection (DI), it is often possible to register services as singletons and have the DI container inject the instances as required.

The next steps will discuss a Singleton base class.

## Singleton base

1. In the **Shared** project, create a new folder and name it **Services**.

1. Within the **Services** folder, create a new class and name it **SingletonBase**.

1. Update the using statements to match:

    ```csharp
    using System;
    using System.Reflection;
    ```

1. Replace the class definition with the following:

    ```csharp
    public abstract class SingletonBase<T> where T : class
    {
        private static readonly Lazy<T> _instance = new Lazy<T>(() =>
            {
                var constructor = typeof(T).GetConstructor(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic,
                    null, Type.EmptyTypes, null);
                return (T)constructor.Invoke(null);
            });

        public static T Instance => _instance.Value;
    }
    ```

    The class defines a private instance variable referencing an instance of the  `Lazy<T>` class. `Lazy<T>` class is instantiated with a constructor that accepts an initialization function definition. This function returns an instance of type **T** and the `Lazy<T>` class ensures this function is executed in a thread-safe manner, ensuring only one instance exists.

    As the type **T;** that this class will be instantiating must support the singleton pattern, it will have *a single constructor, which is private and parameterless*. So, in order to instantiate **T**, reflection must be used.

    The **Instance** property returns the instance of **T;**.

## TokenService implementation

The **TokenService** simplifies the use of the **IdentityServerClient** and exposes the access token. As the retrieval of the access toke is asynchronous, the class also exposes a task that is completed once the initialization is completed.

1. Within the **Services** folder, create a new class and name it **TokenService**.

1. Update the using statements to match:

    ```csharp
    using System.Threading.Tasks;
    using TimeEntryUno.Shared.WebServices;
    ```

    > [!NOTE]
    > The **TimeEntryUno.Shared.WebServices** namespace may differ depending on the project name.

1. Replace the class definition with the following:

    ```csharp
    public sealed class TokenService
        : SingletonBase<TokenService>
    {
    }
    ```

    Notice that the class inherits from **SingletonBase**.

1. Add the following member to hold an instance of the **IdentityServerClient** class created earlier:

    ```csharp
    private IdentityServerClient _identityServerClient;
    ```

1. Add the following properties:

    ```csharp
    public string AccessToken { get; private set; }

    // To ensure initialized, use await TokenService.Instance.Initialization;
    public Task Initialization { get; private set; }
    ```

    The first will expose the access token, whereas the second exposes the **Task** that tracks the completion of initialization.

1. As this class must support the singleton pattern, add the following private constructor:

    ```csharp
    private TokenService()
    {
        _identityServerClient = new IdentityServerClient(
            identityServerBaseAddress: "https://localhost:5001",
            clientId: "TimeEntryUno",
            clientSecret: "A2W7aQVFQWRX",
            scope: "TimeEntryApi");

        // starts the initialization
        Initialization = InitializeAsync();
    }
    ```

    Notice the construction of the **IdentityServerClient** instance, using the server properties, and the assignment of the **Initialization** property.

    > [!NOTE]
    > **Initialization** is assigned the **Task** returned by the **InitializeAsync** call, rather than the result of an await. This allows a caller to await the initialization by using code similar to `await TokenService.Instance.Initialization`.

1. Next, add the asynchronous initialization task:

    ```csharp
    private async Task InitializeAsync()
    {
        AccessToken = await _identityServerClient.GetAccessTokenAsync();
    }
    ```

    As can be seen, this starts the process of retrieving the access token

1. Finally, add an async method that ensures the service is initialized and then returns the **AccessToken**:

    ```csharp
    public async Task<string> GetAccessTokenAsync()
    {
        await Initialization;
        if (Initialization.IsCompleted && Initialization.Status == TaskStatus.RanToCompletion)
        {
            return AccessToken;
        }

        throw new InvalidOperationException("AccessToken is unavailable");
    }
    ```

    This method provides the means for a consumer to ensure initialization has completed and then retrieve the access token. It first awaits the completion of the **Initialization** task, then checks to see if it **RanToCompletion** (i.e. was successful). If successful, the retrieved access token is returned, otherwise an exception is thrown.

    Here is an example of the usage:

    ```csharp
    var api = new IdentityApi("https://localhost:6001", await TokenService.Instance.GetAccessTokenAsync());
    ```

The next task will discuss an approach to initializing the service.

## TokenService initialization

As the **TokenService** utilizes asynchronous initialization, there is an advantage to starting the initialization process as early as feasible in the application lifecycle. An example would be adding it to the `App.cs` `OnLaunched` method, as shown below:

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    // start helpers
    var tokenTask = TokenService.Instance;
    tokenTask.Initialization.ContinueWith(
        async ct =>
        {
            this.Log().LogCritical($"Unable to initialize token service - {ct.Exception.Message}");
            await ErrorDialogHelper.ShowFatalErrorAsync<FatalErrorPage>("FatalErrorTitle", "FatalInitializeError");
        },
        TaskContinuationOptions.OnlyOnFaulted);
```

Notice the use of the **ContinueWith** method that logs an error and displays the fatal error dialog should the initialization fail.

In the next task, the token service will be leveraged within the identity service.

## Next unit: Implementing an identity service client

[![button](assets/NextButton.png)](11-implementing-identity-service-client.md)
