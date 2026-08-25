#nullable enable

using System;

namespace Uno.UI.Runtime.Skia;

internal static class LinuxBackgroundTaskExtensions
{
	internal static void Register()
	{
		if (OperatingSystem.IsLinux())
		{
			LinuxBackgroundTaskSchedulerExtension.RegisterExtension();
		}
	}
}
