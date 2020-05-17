# Uno Support for Windows.Networking

## Checking for internet connectivity

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

Android can recognize all values of `NetworkConnectivityLevel`. iOS, macOS and WASM return either `None` or `InternetAccess`.
