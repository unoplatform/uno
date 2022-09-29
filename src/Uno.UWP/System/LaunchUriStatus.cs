#nullable disable

using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.System
{
	public enum LaunchUriStatus
	{
		Success = 0,
		AppUnavailable = 1,
		ProtocolUnavailable = 2,
		Unknown = 3
	}
}