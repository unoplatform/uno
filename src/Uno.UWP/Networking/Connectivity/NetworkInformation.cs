namespace Windows.Networking.Connectivity
{
	public partial class NetworkInformation
	{
		private static object _syncLock = new object();
		private static NetworkStatusChangedEventHandler _networkStatusChanged = null;

		private static readonly ConnectionProfile _internetConnectionProfile =
			new ConnectionProfile();

		public static ConnectionProfile GetInternetConnectionProfile() =>
			_internetConnectionProfile;

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

		private static void OnNetworkStatusChanged() =>
			_networkStatusChanged?.Invoke(null);
	}
}
