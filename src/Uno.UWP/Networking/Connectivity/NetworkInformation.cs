#if !IS_UNIT_TESTS
#nullable enable

using System.Collections.Generic;
using Uno.Helpers;

namespace Windows.Networking.Connectivity;

/// <summary>
/// Provides access to network connection information for the local machine.
/// </summary>
public static partial class NetworkInformation
{
	private static StartStopEventWrapper<NetworkStatusChangedEventHandler> _networkStatusChanged;

	static NetworkInformation()
	{
		_networkStatusChanged = new(
			StartNetworkStatusChanged,
			StopNetworkStatusChanged);
	}

	/// <summary>
	/// Gets the connection profile associated with the internet connection currently used by the local machine.
	/// </summary>
	/// <returns>The profile for the connection currently used to connect the machine to the Internet,
	/// or null if there is no connection profile with a suitable connection.</returns>
	public static ConnectionProfile GetInternetConnectionProfile() => ConnectionProfile.GetInternetConnectionProfile();

	/// <summary>
	/// Gets a list of profiles for connections, active or otherwise, on the local machine.
	/// </summary>
	/// <returns>An array of <see cref="ConnectionProfile" /> objects.</returns>
	public static IReadOnlyList<ConnectionProfile> GetConnectionProfiles() => [ConnectionProfile.GetInternetConnectionProfile()];

	/// <summary>
	/// Occurs when the network status changes for a connection.
	/// </summary>
	public static event NetworkStatusChangedEventHandler NetworkStatusChanged
	{
		add => _networkStatusChanged.AddHandler(value);
		remove => _networkStatusChanged.RemoveHandler(value);
	}

	internal static void OnNetworkStatusChanged() =>
		_networkStatusChanged.Event?.Invoke(null);
}
#endif
