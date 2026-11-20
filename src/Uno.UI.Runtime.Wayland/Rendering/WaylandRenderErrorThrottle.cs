#nullable enable

namespace Uno.UI.Runtime.Wayland.Rendering;

// A lost context fails every frame; report the first failure, then a periodic summary instead of a flood.
internal sealed class WaylandRenderErrorThrottle
{
	internal const long SummaryIntervalMilliseconds = 5000;

	private long _lastReport;
	private int _suppressed;
	private bool _reported;

	// Render thread only.
	internal bool ShouldReport(long nowMilliseconds, out int suppressed)
	{
		if (_reported && nowMilliseconds - _lastReport < SummaryIntervalMilliseconds)
		{
			_suppressed++;
			suppressed = 0;
			return false;
		}
		suppressed = _suppressed;
		_suppressed = 0;
		_reported = true;
		_lastReport = nowMilliseconds;
		return true;
	}
}
