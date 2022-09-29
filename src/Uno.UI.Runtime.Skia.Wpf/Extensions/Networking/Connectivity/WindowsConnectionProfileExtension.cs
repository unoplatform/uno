using System;
using System.Runtime.InteropServices;
using Windows.Networking.Connectivity;

namespace Uno.Extensions.Networking.Connectivity
{
	internal class WindowsConnectionProfileExtension : IConnectionProfileExtension
	{
		private readonly object _connectionProfile;
		public WindowsConnectionProfileExtension(object owner)
		{
			if (!(owner is ConnectionProfile connectionProfile))
			{
				throw new InvalidOperationException($"Owner of {nameof(WindowsConnectionProfileExtension)} must be a {nameof(ConnectionProfile)}.");
			}

			_connectionProfile = connectionProfile;
		}

		[DllImport("wininet.dll")]
		extern static bool InternetGetConnectedState(out int connectionDescription, int reservedValue);

		public NetworkConnectivityLevel GetNetworkConnectivityLevel() =>
			InternetGetConnectedState(out _, 0) ?
				NetworkConnectivityLevel.InternetAccess : NetworkConnectivityLevel.None;
	}
}
