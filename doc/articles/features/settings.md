---
uid: Uno.Features.Settings
---

# Settings

> [!TIP]
> This article covers Uno-specific information for managing user preferences. For a full description of the feature and instructions on using it, see [Save and load settings in a WinUI app](https://learn.microsoft.com/windows/uwp/get-started/settings-learning-track).

* Settings API allows you to store the preferences of the user and preserve them across the launches of the application.

## Supported features

| Feature                                   | Windows | Android | iOS | Web (WASM) | Desktop (macOS) | Desktop (X11) | Desktop (Windows) |
|-------------------------------------------|---------|---------|-----|------------|-----------------|---------------|-------------------|
| `ApplicationData.Current.LocalSettings`   | ✔       | ✔       | ✔   | ✔          | ✔               | ✔             | ✔                 |
| `ApplicationData.Current.RoamingSettings` | ✔       | ✔       | ✔   | ✔          | ✔               | ✔             | ✔                 |

## Using Settings with Uno

* On each target platform, the native user preferences APIs are used for storage.
* `RoamingSettings` are not roamed across devices, as this feature has been disabled by WinUI.

## Examples

### Storing a Setting

```csharp
ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
localSettings.Values["MySettingName"] = "A user setting";
```

### Reading a setting

```csharp
ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
var localValue = localSettings.Values["MySettingName"] as string;
```
