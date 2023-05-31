using static Uno.Foundation.WebAssemblyRuntime;

#if NET7_0_OR_GREATER
using NativeMethods = __Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation.NativeMethods;
#endif

namespace Windows.Security.ExchangeActiveSyncProvisioning
{
	public partial class EasClientDeviceInformation
	{
		private const string BrowserVersionFallback = "Unknown";

#if !NET7_0_OR_GREATER
		private const string JsType = "Windows.System.Profile.AnalyticsVersionInfo";
#endif

		partial void Initialize()
		{
			OperatingSystem = "Browser";
			SystemSku = GetUserAgent();
		}

		private string GetUserAgent()
		{
			var userAgent =
#if NET7_0_OR_GREATER
				NativeMethods.GetUserAgent();
#else
				InvokeJS(JsType + ".getUserAgent()");
#endif

			if (!string.IsNullOrEmpty(userAgent))
			{
				return userAgent;
			}
			return BrowserVersionFallback;
		}
	}
}
