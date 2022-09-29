#nullable disable

namespace Windows.Networking.Connectivity;

internal interface IConnectionProfileExtension
{
	NetworkConnectivityLevel GetNetworkConnectivityLevel();
}
