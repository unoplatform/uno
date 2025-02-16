#if __WASM__ || __SKIA__

using System.Diagnostics;

namespace Windows.UI.Xaml.Controls;

partial class FlipView
{
	private static bool QueryPerformanceCounter(out long timestamp)
	{
		timestamp = Stopwatch.GetTimestamp();
		return true;
	}

	private static bool QueryPerformanceFrequency(out long frequency)
	{
		frequency = Stopwatch.Frequency;
		return true;
	}
}
#endif
