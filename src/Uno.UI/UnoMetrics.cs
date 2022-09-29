#nullable disable

namespace Uno.UI
{
	public static class UnoMetrics
	{
		public static class TextBlock
		{
			public static long MeasureCacheMisses { get; internal set; }
			public static long MeasureCacheHits { get; internal set; }
		}
	}
}
