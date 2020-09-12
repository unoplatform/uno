#if __WASM__

namespace Windows.Networking.Connectivity
{
	public partial class ConnectionProfile
    {
		private const string JsType = "Windows.Networking.Connectivity.ConnectionProfile";

		internal static ConnectionProfile GetInternetConnectionProfile() =>
			new ConnectionProfile();

		private ConnectionProfile()
		{
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
