#nullable disable

namespace Windows.Networking.Connectivity;

internal interface INetworkInformationExtension
{
	void StartObservingNetworkStatus();

	void StopObservingNetworkStatus();
}
