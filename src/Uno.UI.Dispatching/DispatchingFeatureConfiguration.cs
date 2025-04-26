using System;
using System.ComponentModel;

namespace Uno;

public static class DispatchingFeatureConfiguration
{
	internal static class DispatcherQueue
	{
		public static TimeSpan FrameDuration { get; set; } = TimeSpan.FromMilliseconds(1 / 60.0);
	}
}
