#if !NET461

namespace Windows.Networking.Connectivity
{
	public static partial class NetworkInformation
	{
		private static readonly object _syncLock = new object();

		private static NetworkStatusChangedEventHandler _networkStatusChanged = null;

		/// <summary>
		/// Gets the connection profile associated with the internet connection currently used by the local machine.
		/// </summary>
		/// <returns>The profile for the connection currently used to connect the machine to the Internet,
		/// or null if there is no connection profile with a suitable connection.</returns>
		public static ConnectionProfile GetInternetConnectionProfile() => ConnectionProfile.GetInternetConnectionProfile();

		public static event NetworkStatusChangedEventHandler NetworkStatusChanged
		{
			add
			{
				lock (_syncLock)
				{
					var isFirstSubscriber = _networkStatusChanged == null;
					_networkStatusChanged += value;
					if (isFirstSubscriber)
					{
						StartNetworkStatusChanged();
					}
				}
			}
			remove
			{
				lock (_syncLock)
				{
					_networkStatusChanged -= value;
					if (_networkStatusChanged == null)
					{
						StopNetworkStatusChanged();
					}
				}
			}
		}		

		internal static void OnNetworkStatusChanged() => _networkStatusChanged?.Invoke(null);
	}
}
#endif
