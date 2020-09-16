using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Windows.System.Profile
{
    public partial class AnalyticsVersionInfo
    {
        internal AnalyticsVersionInfo()
        {
        }

        public string DeviceFamily => Environment.OSVersion.Platform.ToString();

        public string DeviceFamilyVersion => Environment.OSVersion.VersionString;
    }
}
