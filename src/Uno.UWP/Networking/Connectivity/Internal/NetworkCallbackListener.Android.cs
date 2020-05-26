#if __ANDROID__
using System;
using Android.Net;
using AndroidConnectivityManager = Android.Net.ConnectivityManager;

namespace Windows.Networking.Connectivity.Internal
{
	internal class NetworkCallbackListener : AndroidConnectivityManager.NetworkCallback
	{
		public NetworkCallbackListener()
		{			
		}

		public override void OnAvailable(Network network)
		{
			base.OnAvailable(network);
			NetworkInformation.OnDelayedNetworkStatusChanged();
		}

		public override void OnCapabilitiesChanged(Network network, NetworkCapabilities networkCapabilities)
		{
			base.OnCapabilitiesChanged(network, networkCapabilities);
			NetworkInformation.OnDelayedNetworkStatusChanged();
		}

		public override void OnLinkPropertiesChanged(Network network, LinkProperties linkProperties)
		{
			base.OnLinkPropertiesChanged(network, linkProperties);
			NetworkInformation.OnDelayedNetworkStatusChanged();
		}

		public override void OnLosing(Network network, int maxMsToLive)
		{
			base.OnLosing(network, maxMsToLive);
			NetworkInformation.OnDelayedNetworkStatusChanged();
		}

		public override void OnLost(Network network)
		{
			base.OnLost(network);
			NetworkInformation.OnDelayedNetworkStatusChanged();
		}

		public override void OnUnavailable()
		{
			base.OnUnavailable();
			NetworkInformation.OnDelayedNetworkStatusChanged();
		}
	}
}
#endif
