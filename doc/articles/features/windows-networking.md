---
uid: Uno.Features.WNetworking
---

# Networking

> [!TIP]
> This article covers Uno-specific information for `Windows.Networking` namespace. For a full description of the feature and instructions on using it, consult the UWP documentation: https://learn.microsoft.com/en-us/uwp/api/windows.networking

* The `Windows.Networking` namespace provides classes for accessing and managing network connections from your app.

## Supported features

| Feature                        | Windows | Android | iOS | Web (WASM) | macOS | Linux (Skia) | Win 7 (Skia) |
|--------------------------------|---------|---------|-----|------------|-------|--------------|--------------|
| `GetInternetConnectionProfile` | ✔       | ✔       | ✔   | ✔          | ✔     | ✔            | ✔            |
| `NetworkStatusChanged`         | ✔       | ✔       | ✔   | ✔          | ✔     | ✔            | ✔            |

## Platform-specific

### Android

Android can recognize all values of `NetworkConnectivityLevel`. iOS, macOS and WASM return either `None` or `InternetAccess`.

The `android.permission.ACCESS_NETWORK_STATE` permission is required. It can be added to the application manifest or with the following attribute in the Android platform head:

```csharp
[assembly: UsesPermission(\"android.permission.ACCESS_NETWORK_STATE\")]
```

### iOS/macOS reachability host name

On iOS and macOS, it is required to make an actual "ping" request, to verify that internet connection is accessible. The default domain that is checked is `www.example.com`, but you can change this to be any other domain by setting the `WinRTFeatureConfiguration.NetworkInformation.ReachabilityHostname` property.

## Example

### Checking for internet connectivity

You can use the following snippet to check for internet connectivity level in a cross-platform manner:

``` C#
var profile = NetworkInformation.GetInternetConnectionProfile();
if (profile == null)
{
    // No connection
}
else
{
    var level = profile.GetNetworkConnectivityLevel();
    // level is a value of NetworkConnectivityLevel enum
}
```

### Observing changes in connectivity

You can use the following snippet to observe changes in connectivity:

``` C#
var profile = NetworkInformation.GetInternetConnectionProfile();
profile.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
```

### Unsubscribing from the changes

``` C#
profile.NetworkStatusChanged -= NetworkInformation_NetworkStatusChanged;
```
