using System;
using Foundation;

namespace Uno.Helpers
{
	internal static class DeviceHelper
	{
		public static Version OperatingSystemVersion => Environment.OSVersion.Version;
	}
}
