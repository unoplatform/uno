#if __IOS__ || __MACOS__
using System;
#if __IOS__
using CoreTelephony;
#endif
using Uno.Networking.Connectivity.Internal;

namespace Windows.Networking.Connectivity
{
	public partial class NetworkInformation
	{
#if __IOS__
		private static readonly Lazy<CTCellularData> _cellularData = new Lazy<CTCellularData>(() => new CTCellularData());

		internal static CTCellularData CellularData => _cellularData.Value;
#endif

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
