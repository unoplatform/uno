using System;
using System.Collections.Generic;
using System.Text;
using static Uno.Foundation.WebAssemblyRuntime;

namespace Windows.System.Profile
{
    public partial class AnalyticsVersionInfo
    {
        private const string OsName = "Browser";
        private const string BrowserVersionFallback = "Unknown";
        private const string JsType = "Windows.System.Profile.AnalyticsVersionInfo";

        internal AnalyticsVersionInfo()
        {
        }

        public string DeviceFamily => OsName + '.' + AnalyticsInfo.DeviceForm;

        public string DeviceFamilyVersion => GetUserAgent();

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
