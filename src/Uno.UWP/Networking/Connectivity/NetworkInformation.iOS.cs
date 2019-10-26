#if __IOS__
using Uno.Networking.Connectivity.Helpers;

namespace Windows.Networking.Connectivity
{
	public partial class NetworkInformation
	{
		private static ReachabilityListener _reachabilityListener;

		private static void StartNetworkStatusChanged()
		{
			_reachabilityListener = new ReachabilityListener();
			_reachabilityListener.ReachabilityChanged += OnNetworkStatusChanged;
		}

		static void StopNetworkStatusChanged()
		{
			if (_reachabilityListener == null)
			{
				return;
			}

			_reachabilityListener.ReachabilityChanged -= OnNetworkStatusChanged;
			_reachabilityListener.Dispose();
			_reachabilityListener = null;
		}
	}
}
#endif
