#if __IOS__ || __MACOS__

using Uno.Networking.Connectivity.Internal;
#if __IOS__
using CoreTelephony;
#endif

namespace Windows.Networking.Connectivity
{
	public partial class ConnectionProfile
	{
		internal static ConnectionProfile GetInternetConnectionProfile() => new ConnectionProfile();

		private ConnectionProfile()
		{
			var statuses = Reachability.GetActiveConnectionType();
			foreach (var status in statuses)
			{
				if( status == NetworkStatus.ReachableViaCarrierDataNetwork)
				{
					IsWwanConnectionProfile = true;
				}
				else if (status == NetworkStatus.ReachableViaWiFiNetwork)
				{
					IsWlanConnectionProfile = true;
				}
			}
		}

		private NetworkConnectivityLevel GetNetworkConnectivityLevelImpl()
		{
			var mobileDataRestricted = false;
#if __IOS__
			mobileDataRestricted = NetworkInformation.CellularData.RestrictedState == CTCellularDataRestrictedState.Restricted;
#endif
			var internetStatus = Reachability.InternetConnectionStatus();
			if ((internetStatus == NetworkStatus.ReachableViaCarrierDataNetwork && !mobileDataRestricted) || internetStatus == NetworkStatus.ReachableViaWiFiNetwork)
			{
				return NetworkConnectivityLevel.InternetAccess;
			}

			var remoteHostStatus = Reachability.RemoteHostStatus();
			if ((remoteHostStatus == NetworkStatus.ReachableViaCarrierDataNetwork && !mobileDataRestricted) || remoteHostStatus == NetworkStatus.ReachableViaWiFiNetwork)
			{
				return NetworkConnectivityLevel.InternetAccess;
			}

			return NetworkConnectivityLevel.None;
		}
	}
}
#endif
