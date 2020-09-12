#if !NET461

namespace Windows.Networking.Connectivity
{
	/// <summary>
	/// Represents a network connection, which includes either the currently
	/// connected network or prior network connections. Provides information
	/// about the connection status and connectivity statistics.
	/// </summary>
	public partial class ConnectionProfile
    {
		/// <summary>
		/// Gets the network connectivity level for this connection.
		/// This value indicates what network resources, if any, are currently available.
		/// </summary>
		/// <returns>The level of network connectivity.</returns>
		public NetworkConnectivityLevel GetNetworkConnectivityLevel() =>
			GetNetworkConnectivityLevelImpl();

#if !__WASM__
		/// <summary>
		/// Gets a value that indicates if connection profile is a WWAN (mobile) connection.
		/// </summary>
		/// <remarks>If this cannot be determined on the given platform, it remains false.</remarks>
		public bool IsWwanConnectionProfile { get; }

		/// <summary>
		/// Gets a value that indicates if connection profile is a WLAN (WiFi) connection.
		/// </summary>
		/// <remarks>If this cannot be determined on the given platform, it remains false.</remarks>		
		public bool IsWlanConnectionProfile { get; }
#endif
	}
}
#endif
