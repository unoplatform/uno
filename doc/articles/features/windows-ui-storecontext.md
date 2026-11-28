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

### Testing on Android

Google Play only shows the review dialog to apps installed from Google Play, so a debug build deployed from your IDE won't display it. To exercise your integration without publishing, enable test mode before requesting a review:

```csharp
Uno.WinRTFeatureConfiguration.StoreContext.TestMode = true;
```

In test mode, Uno Platform uses Google's `FakeReviewManager`: no dialog is shown, and `RequestRateAndReviewAppAsync()` completes with `StoreRateAndReviewStatus.Succeeded` once the review flow has run. Use it to validate your code path, on an emulator or device.

To see the real dialog, install the app from an internal test track in Google Play Console. With internal app sharing, the dialog appears but its submit button is disabled. See [Test in-app reviews](https://developer.android.com/guide/playcore/in-app-review/test) for the account and device requirements.
