using static Uno.Foundation.WebAssemblyRuntime;

using NativeMethods = __Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation.NativeMethods;

namespace Windows.Security.ExchangeActiveSyncProvisioning
{
	public partial class EasClientDeviceInformation
	{
		private const string BrowserVersionFallback = "Unknown";

		partial void Initialize()
		{
			OperatingSystem = "Browser";
			SystemSku = GetUserAgent();
		}

		private string GetUserAgent()
		{
			var userAgent = NativeMethods.GetUserAgent();

			if (!string.IsNullOrEmpty(userAgent))
			{
				return userAgent;
			}
			return BrowserVersionFallback;
		}
	}
}
