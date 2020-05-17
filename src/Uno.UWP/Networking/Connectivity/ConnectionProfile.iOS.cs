#if __IOS__
using Uno.Networking.Connectivity.Helpers;

namespace Windows.Networking.Connectivity
{
	public partial class ConnectionProfile
	{
		internal static ConnectionProfile GetInternetConnectionProfile() => new ConnectionProfile();

		private NetworkConnectivityLevel GetNetworkConnectivityLevelImpl()
		{
			var internetStatus = Reachability.InternetConnectionStatus();
			if (internetStatus == NetworkStatus.ReachableViaCarrierDataNetwork ||
			    internetStatus == NetworkStatus.ReachableViaWiFiNetwork)
			{
				return NetworkConnectivityLevel.InternetAccess;
			}

			var remoteHostStatus = Reachability.RemoteHostStatus();
			if (remoteHostStatus == NetworkStatus.ReachableViaCarrierDataNetwork ||
			    remoteHostStatus == NetworkStatus.ReachableViaWiFiNetwork)
			{
				return NetworkConnectivityLevel.InternetAccess;
			}

			return NetworkConnectivityLevel.None;
		}
	}
}
#endif
