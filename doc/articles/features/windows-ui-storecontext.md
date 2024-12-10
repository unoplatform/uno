---
uid: Uno.Features.StoreContext
---

# Store Context

> [!TIP]
> This article provides Uno Platform-specific information for the `Windows.Services.Store.StoreContext` namespace. For a comprehensive overview of this feature and detailed usage instructions, refer to the official documentation for [Windows.Services.Store.StoreContext Namespace](https://learn.microsoft.com/uwp/api/Windows.Services.Store.StoreContext).

## In-App Review

The in-app review feature is currently supported on iOS and Android through Google Play.

### Google Play Integration

#### References in a Single Project

In an Uno Platform Single Project, you'll need to add the `GooglePlay` [Uno Feature](xref:Uno.Features.Uno.Sdk#uno-platform-features) as follows:

```xml
<UnoFeatures>
    ...
    GooglePlay;
    ...
</UnoFeatures>
```

#### References in a Legacy Project

On all Uno Platform targets, you'll need the to add the `Uno.WinUI.GooglePlay` package to your project. This package is available on [nuget.org](https://www.nuget.org/packages/Uno.WinUI.GooglePlay).

### Usage

For iOS, no additional steps are needed—you can use the feature via the following snippet directly. On Android, ensure that you've added the above package to your project first.

```csharp
await Windows.Services.Store.StoreContext.GetDefault().RequestRateAndReviewAppAsync();
```
