---
uid: Uno.Features.WNetworking
---

# Networking

> [!TIP]
> This article provides specific information about Uno within the `Windows.Networking` namespace. For a comprehensive overview of the feature and guidance on how to use it, visit [Windows.Networking](https://learn.microsoft.com/uwp/api/windows.networking).

The `Windows.Networking` namespace offers classes to access and manage network connections within your app.

## NetworkInformation

The `Windows.Networking.Connectivity.NetworkInformation` class provides access to network connection information and allows you to monitor changes in network connectivity. For more details, refer to the official documentation: [NetworkInformation Class](https://learn.microsoft.com/uwp/api/windows.networking.connectivity.networkinformation).

### Supported features

| Feature                        | Windows | Android | iOS | Web (WASM) | macOS | Linux (Skia) | Win 7 (Skia) |
|--------------------------------|---------|---------|-----|------------|-------|--------------|--------------|
| `GetInternetConnectionProfile` | ✔       | ✔       | ✔   | ✔          | ✔     | ✔            | ✔            |
| `NetworkStatusChanged`         | ✔       | ✔       | ✔   | ✔          | ✔     | ✔            | ✔            |

### Checking Network Connectivity in Uno

For more detailed guidance on network connectivity, watch our video tutorial:

> [!Video https://www.youtube-nocookie.com/embed/sK9IbkBAXIo]

### Platform-specific

#### Android

Android recognizes all values of the `NetworkConnectivityLevel` [enum](https://learn.microsoft.com/uwp/api/windows.networking.connectivity.networkconnectivitylevel). In contrast, iOS, macOS, and WASM only return either `None` or `InternetAccess`.

The `android.permission.ACCESS_NETWORK_STATE` permission is necessary and must be included in the application manifest or specified using the following attribute in the Android platform head:

```csharp
[assembly: UsesPermission("android.permission.ACCESS_NETWORK_STATE")]
```

#### iOS/macOS reachability host name

iOS and macOS use a 'ping' request to check internet connectivity. The default domain is `www.example.com`; however, you can change it to any other domain by setting the `WinRTFeatureConfiguration.NetworkInformation.ReachabilityHostname` property.

```csharp
WinRTFeatureConfiguration.NetworkInformation.ReachabilityHostname = "platform.uno";
```

### Example

#### Checking for internet connectivity

Use the snippet below to check internet connectivity levels in a cross-platform manner.

```csharp
using Windows.Networking.Connectivity;

var profile = NetworkInformation.GetInternetConnectionProfile();
var level = profile?.GetNetworkConnectivityLevel();
if (level is NetworkConnectivityLevel.InternetAccess)
{
    // Connected to the internet
}
```

#### Observing changes in connectivity

Use the following snippet to observe changes in connectivity:

```csharp
using Windows.Networking.Connectivity;

NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;

private void NetworkInformation_NetworkStatusChanged(object sender)
{
    // read the current connectivity state/level
    var profile = NetworkInformation.GetInternetConnectionProfile();
    var level = profile?.GetNetworkConnectivityLevel();
}
```

#### Unsubscribing from the changes

```csharp
NetworkInformation.NetworkStatusChanged -= NetworkInformation_NetworkStatusChanged;
```
