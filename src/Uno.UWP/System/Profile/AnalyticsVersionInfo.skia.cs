using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Windows.System.Profile
{
    public partial class AnalyticsVersionInfo
    {


		partial void Initialize()
		{
			DeviceFamily = $"{Environment.OSVersion.Platform.ToString()}.{AnalyticsInfo.DeviceForm}";
		}
    }
}
