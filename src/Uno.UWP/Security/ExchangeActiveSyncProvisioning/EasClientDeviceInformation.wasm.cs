#nullable disable

using static Uno.Foundation.WebAssemblyRuntime;

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
