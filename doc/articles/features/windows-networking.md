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


**Note**: On Android, the `android.permission.ACCESS_NETWORK_STATE` permission is required. It can be added to the application manifest or with the following attribute in the Android platform head:
```
[assembly: UsesPermission(\"android.permission.ACCESS_NETWORK_STATE\")]
```