#if __WASM__

namespace Windows.Networking.Connectivity
{
	public partial class ConnectionProfile
    {
		private const string JsType = "Windows.Networking.Connectivity.ConnectionProfile";
		private readonly bool _isInternetProfile;

		internal ConnectionProfile(bool isInternetProfile)
		{
			_isInternetProfile = isInternetProfile;
		}

		public NetworkConnectivityLevel GetNetworkConnectivityLevel()
		{
			var command = $"{JsType}.hasInternetAccess()";
			var result = Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
			return bool.Parse(result) ? NetworkConnectivityLevel.InternetAccess : NetworkConnectivityLevel.None;
		}
	}
}
#endif
