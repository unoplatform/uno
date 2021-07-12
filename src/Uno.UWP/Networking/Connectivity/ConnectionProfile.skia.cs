#nullable enable

using Uno.Foundation.Extensibility;

namespace Windows.Networking.Connectivity
{
	public partial class ConnectionProfile
    {
		private IConnectionProfileExtension _connectionProfileExtension;

		internal static ConnectionProfile GetInternetConnectionProfile() =>
			new ConnectionProfile();

		private ConnectionProfile() =>
			ApiExtensibility.CreateInstance(this, out _connectionProfileExtension);

		private NetworkConnectivityLevel GetNetworkConnectivityLevelImpl() =>
			_connectionProfileExtension?.GetNetworkConnectivityLevel() ?? NetworkConnectivityLevel.None;
	}
}
