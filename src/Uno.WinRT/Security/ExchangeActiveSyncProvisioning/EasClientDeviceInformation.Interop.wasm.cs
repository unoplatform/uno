using System.Runtime.InteropServices.JavaScript;

namespace __Windows.Security.ExchangeActiveSyncProvisioning
{
	internal partial class EasClientDeviceInformation
	{
		internal static partial class NativeMethods
		{
			[JSImport("globalThis.Windows.System.Profile.AnalyticsVersionInfo.getUserAgent")]
			internal static partial string GetUserAgent();
		}
	}
}
