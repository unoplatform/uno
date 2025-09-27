using System;
using System.ComponentModel;

namespace Uno;

public static class DispatchingFeatureConfiguration
{
	internal static class DispatcherQueue
	{
		/// <summary>
		/// This property is used to throttle the number of frames dispatched
		/// to the UI thread under webassembly.
		/// </summary>
		public static double WebAssemblyFrameRate { get; set; } = 60.0;
	}
}
