using System;
#if __IOS__ && !__MACCATALYST__ // catalyst https://github.com/xamarin/xamarin-macios/issues/13931
using CoreTelephony;
#endif
using Uno.Networking.Connectivity.Internal;

#pragma warning disable BI1234 // 'CTCellularDataRestrictedState' is obsolete: 'Starting with ios14.0 Use the 'CallKit' API instead.'

namespace Windows.Networking.Connectivity
{
	public partial class NetworkInformation
	{
#if __IOS__ && !__MACCATALYST__ // catalyst https://github.com/xamarin/xamarin-macios/issues/13931
		private static readonly Lazy<CTCellularData> _cellularData = new Lazy<CTCellularData>(() => new CTCellularData());

		internal static CTCellularData CellularData => _cellularData.Value;
#endif

		private static ReachabilityListener? _reachabilityListener;

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
