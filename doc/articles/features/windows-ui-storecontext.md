---
uid: Uno.Features.StoreContext
---

# Store Context

> [!TIP]
> This article provides Uno-specific information for the `Windows.Services.Store.StoreContext` namespace. For a comprehensive overview of this feature and detailed usage instructions, refer to the official documentation for [Windows.Services.Store.StoreContext Namespace](https://learn.microsoft.com/uwp/api/Windows.Services.Store.StoreContext).

## In-App Review

The in-app review feature is currently supported only on Android through Google Play.

### Google Play Integration

For Google Play support, make sure to add the `Uno.WinUI.GooglePlay` package to your project. This package is available on [nuget.org](https://www.nuget.org/packages/Uno.WinUI.GooglePlay).

### Usage

Once you added the above package to your project, you can prompt users to rate and review your appby using the following code snippet:

```csharp
await Windows.Services.Store.StoreContext.GetDefault().RequestRateAndReviewAppAsync();
```
