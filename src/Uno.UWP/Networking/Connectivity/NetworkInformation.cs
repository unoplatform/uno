#if !NET461

using Uno.Helpers;

namespace Windows.Networking.Connectivity
{
	public static partial class NetworkInformation
	{
		private static readonly object _syncLock = new object();

		private static StartStopEventWrapper<NetworkStatusChangedEventHandler> _networkStatusChangedWrapper = null;

		static NetworkInformation()
		{
			_networkStatusChangedWrapper = new StartStopEventWrapper<NetworkStatusChangedEventHandler>(
				() => StartNetworkStatusChanged(),
				() => StopNetworkStatusChanged());
		}

		/// <summary>
		/// Gets the connection profile associated with the internet connection currently used by the local machine.
		/// </summary>
		/// <returns>The profile for the connection currently used to connect the machine to the Internet,
		/// or null if there is no connection profile with a suitable connection.</returns>
		public static ConnectionProfile GetInternetConnectionProfile() => ConnectionProfile.GetInternetConnectionProfile();

		public static event NetworkStatusChangedEventHandler NetworkStatusChanged
		{
			add => _networkStatusChangedWrapper.AddHandler(value);
			remove => _networkStatusChangedWrapper.RemoveHandler(value);
		}		

		internal static void OnNetworkStatusChanged() =>
			_networkStatusChangedWrapper.Event?.Invoke(null);
	}
}
#endif
