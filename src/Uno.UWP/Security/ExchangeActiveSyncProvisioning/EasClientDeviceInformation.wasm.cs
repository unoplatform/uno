using static Uno.Foundation.WebAssemblyRuntime;

#if NET7_0_OR_GREATER
using NativeMethods = __Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation.NativeMethods;
#endif

namespace Windows.Security.ExchangeActiveSyncProvisioning
{
	public partial class EasClientDeviceInformation
	{
		private const string BrowserVersionFallback = "Unknown";
		private const string JsType = "Windows.System.Profile.AnalyticsVersionInfo";

		partial void Initialize()
		{
			OperatingSystem = "Browser";
			SystemSku = GetUserAgent();
		}

		private string GetUserAgent()
		{
			var userAgent = InvokeJS(JsType + ".getUserAgent()");
			if (!string.IsNullOrEmpty(userAgent))
			{
				return userAgent;
			}
			return BrowserVersionFallback;
		}
	}
}
