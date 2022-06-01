#nullable enable

using System.Net.NetworkInformation;
using Uno;
using Uno.Foundation.Extensibility;

namespace Windows.Networking.Connectivity
{
	public partial class ConnectionProfile
	{
		private IConnectionProfileExtension _connectionProfileExtension;

		internal static ConnectionProfile GetInternetConnectionProfile() =>
			new ConnectionProfile();

		private ConnectionProfile() =>
			ApiExtensibility.CreateInstance(this, out _connectionProfileExtension);

		private NetworkConnectivityLevel GetNetworkConnectivityLevelImpl()
		{
			if (_connectionProfileExtension is null)
			{
				try
				{
					using (var ping = new Ping())
					{
						var reply = ping.Send(WinRTFeatureConfiguration.NetworkInformation.ReachabilityHostname);
						return reply?.Status == IPStatus.Success ? NetworkConnectivityLevel.InternetAccess : NetworkConnectivityLevel.None;
					}
				}
				catch
				{
					// In case exception is thrown, assume
					// connection was not possible
					return NetworkConnectivityLevel.None;
				}
			}

			return _connectionProfileExtension.GetNetworkConnectivityLevel();
		}
	}
}
