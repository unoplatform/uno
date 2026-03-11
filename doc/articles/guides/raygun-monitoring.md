---
uid: Monitoring.Raygun
---

# Error Monitoring & Crash Reporting with Raygun

> [!TIP]
> This article covers the basic setup and Uno-specific information for using Raygun with Uno. For a full description of the feature and instructions on using it, see the [official .NET 6+ | Raygun documentation](https://raygun.com/documentation/language-guides/dotnet/crash-reporting/net-core/).

Crash reporting is crucial for developing mobile and desktop applications. It ensures you are alerted when exceptions occur, whether they are unhandled or caught exceptions you want to report. With Visual Studio App Center's retirement on the horizon, what diagnostic tool should we turn to?

Raygun provides various products, including crash reporting, real user monitoring, and application performance monitoring (APM). It supports a wide range of frameworks, including .NET.

## Setting Up Raygun

Raygun offers a free trial, allowing you to explore its features before committing to a subscription. Follow these steps to get started:

### Create a Raygun Account

Visit the [Raygun website](https://raygun.com) and sign up for an account.

### Create Your Application

1. Log in to your Raygun account.
2. Create a new application and name it.
3. Select C#/.NET as the language and choose .NET 6+ as the framework.

## Integrating Raygun into Your Uno Platform Application

Let's start with a new Uno Platform application using the default template provided by the Uno Template Wizard.

### Step 1 – Create a New Uno Platform Project

Start by creating a new Uno Platform project using your preferred development environment.

### Step 2 - Install the NuGet Package

Install the Raygun package in your Uno Platform application. Use the Manage NuGet Packages option or the following dotnet CLI command:

```sh
dotnet add package Mindscape.Raygun4Net.NetCore --project [YourProjectName]
```

### Step 3 - Create a RaygunClient

Create an instance of `RaygunClient` by passing it a `RaygunSettings` object with your application API key. You can also enable automatic catching of unhandled exceptions:

```csharp
using Mindscape.Raygun4Net;

private static RaygunClient _raygunClient = new RaygunClient(new RaygunSettings()
{
    ApiKey = "YOUR_API_KEY_HERE",
    CatchUnhandledExceptions = true // Enable to log all unhandled exceptions
});
```

Replace `"YOUR_API_KEY_HERE"` with the actual API key provided by Raygun when you created your application.

### Step 4 - Release and Test

Deploy Raygun into your production environment for optimal results. To test the integration, you can raise a test exception:

```csharp
try
{
    throw new Exception("Temporary example exception to send to Raygun");
}
catch (Exception ex)
{
    _raygunClient.SendInBackground(ex);
}
```

> [!IMPORTANT]
> If you are setting up Raygun on Wasm, you need to either enable support for multithreading (see the [Wasm threading documentation](https://platform.uno/docs/articles/external/uno.wasm.bootstrap/doc/features-threading.html) for more information) or use the `SendAsync` method instead of `SendInBackground`.

### Step 5 - Observe the Website

Once Raygun detects your first error event, the dashboard will automatically update, allowing you to start monitoring.
