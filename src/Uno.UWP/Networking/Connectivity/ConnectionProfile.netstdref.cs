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
			return NetworkConnectivityLevel.None;
		}
	}
}
