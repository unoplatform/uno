#nullable enable

using System;
using Uno.Foundation.Extensibility;

namespace Windows.ApplicationModel.Background;

internal static class BackgroundTaskScheduler
{
	internal static bool TryGetExtension(
		[global::System.Diagnostics.CodeAnalysis.NotNullWhen(true)]
		out IBackgroundTaskSchedulerExtension? extension)
		=> ApiExtensibility.CreateInstance(typeof(BackgroundTaskBuilder), out extension)
			&& extension.IsSupported;

	internal static IBackgroundTaskSchedulerExtension GetRequiredExtension()
	{
		if (TryGetExtension(out var extension))
		{
			return extension;
		}

		throw new PlatformNotSupportedException(
			"Background tasks require launchd on macOS or a systemd user manager on Linux.");
	}
}
