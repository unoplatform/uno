#if __WASM__

namespace Windows.Networking.Connectivity
{
	public partial class ConnectionProfile
    {
		private const string JsType = "Windows.Networking.Connectivity.ConnectionProfile";
		private readonly bool _isInternetProfile;

		internal static ConnectionProfile GetInternetConnectionProfile() => new ConnectionProfile();

		internal ConnectionProfile(bool isInternetProfile)
		{
			_isInternetProfile = isInternetProfile;
		}

		private NetworkConnectivityLevel GetNetworkConnectivityLevelImpl()
		{
			var command = $"{JsType}.hasInternetAccess()";
			var result = Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
			return bool.Parse(result) ? NetworkConnectivityLevel.InternetAccess : NetworkConnectivityLevel.None;
		}
	}
}
#endif
