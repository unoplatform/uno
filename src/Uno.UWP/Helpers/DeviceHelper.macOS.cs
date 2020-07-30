using System;
using Foundation;

namespace Uno.Helpers
{
	internal static class DeviceHelper
	{
		private static Version _version = null;

		public static Version OperatingSystemVersion => _version ??= GetOperatingSystemVersion();

		private static Version GetOperatingSystemVersion()
		{
			using var info = new NSProcessInfo();
			var version = info.OperatingSystemVersion.ToString();
			if (Version.TryParse(version, out var systemVersion))
			{
				return systemVersion;
			}
			else if (int.TryParse(version, out var major))
			{
				return new Version(major, 0);
			}
			return new Version(0, 0);
		}
	}
}
