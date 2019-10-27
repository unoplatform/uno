using System;
using System.Collections.Generic;
using System.Text;
using static Uno.Foundation.WebAssemblyRuntime;

namespace Windows.System.Profile
{
	public partial class AnalyticsVersionInfo
	{
		private const string BrowserTypeFallback = "Browser";
		private const string BrowserVersionFallback = "Unknown";
		private const string JsType = "Windows.System.Profile.AnalyticsVersionInfo";

		internal AnalyticsVersionInfo()
		{
		}

		public string DeviceFamily => $"{GetBrowserName()}.{AnalyticsInfo.DeviceForm}";

		public string DeviceFamilyVersion => GetUserAgent();

		private string GetBrowserName()
		{
			var detectedBrowser = InvokeJS($"{JsType}.getBrowserName()");
            if (!string.IsNullOrEmpty(detectedBrowser))
            {
	            return detectedBrowser;
            }
            return BrowserTypeFallback;
		}

		private string GetUserAgent()
		{
			var userAgent = InvokeJS($"{JsType}.getUserAgent()");
			if (!string.IsNullOrEmpty(userAgent))
			{
				return userAgent;
			}
			return BrowserVersionFallback;
		}
	}
}
