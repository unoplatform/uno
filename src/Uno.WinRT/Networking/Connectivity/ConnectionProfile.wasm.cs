using NativeMethods = __Windows.Networking.Connectivity.ConnectionProfile.NativeMethods;

namespace Windows.Networking.Connectivity
{
	public partial class ConnectionProfile
	{
		internal static ConnectionProfile GetInternetConnectionProfile() =>
			new ConnectionProfile();

		private ConnectionProfile()
		{
		}

		private NetworkConnectivityLevel GetNetworkConnectivityLevelImpl()
		{
			return NativeMethods.HasInternetAccess() ? NetworkConnectivityLevel.InternetAccess : NetworkConnectivityLevel.None;
		}
	}
}
