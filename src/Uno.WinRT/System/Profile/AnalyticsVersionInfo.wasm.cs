using System;
using System.Collections.Generic;
using System.Text;
using Uno;

namespace Windows.System.Profile;

public partial class AnalyticsVersionInfo
{
	private const string OsName = "Browser";

	partial void Initialize()
	{
		DeviceFamily = $"{OsName}.{AnalyticsInfo.DeviceForm}";
	}
}
