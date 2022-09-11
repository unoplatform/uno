#nullable enable

using System;
using System.Net.NetworkInformation;

namespace Windows.Networking.Connectivity;

public partial class NetworkInformation
{
	private static void StartNetworkStatusChanged()
	{
		NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
		NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
	}

	private static void StopNetworkStatusChanged()
	{
		NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
		NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
	}


	private static void OnNetworkAddressChanged(object? sender, EventArgs e) =>
		OnNetworkStatusChanged();

	private static void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e) =>
		OnNetworkStatusChanged();
}
