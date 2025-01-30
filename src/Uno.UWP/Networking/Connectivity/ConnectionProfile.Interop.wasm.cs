using System.Runtime.InteropServices.JavaScript;

namespace __Windows.Networking.Connectivity
{
	internal partial class ConnectionProfile
	{
		internal static partial class NativeMethods
		{
			[JSImport("globalThis.Windows.Networking.Connectivity.ConnectionProfile.hasInternetAccess")]
			internal static partial bool HasInternetAccess();
		}
	}
}
