using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.UI.Xaml
{
	public sealed partial class DebugSettings
	{
#if HAS_UNO_WINUI
		public LayoutCycleTracingLevel LayoutCycleTracingLevel { get; set; }
#endif

		[Uno.NotImplemented]
		public bool EnableFrameRateCounter { get; set; }

		[Uno.NotImplemented]
		public bool EnableRedrawRegions { get; set; }

		[Uno.NotImplemented]
		public bool IsBindingTracingEnabled { get; set; }

		[Uno.NotImplemented]
		public bool IsOverdrawHeatMapEnabled { get; set; }

		[Uno.NotImplemented]
		public bool IsTextPerformanceVisualizationEnabled { get; set; }

#pragma warning disable CS0067 // The event 'DebugSettings.BindingFailed' is never used
		public event BindingFailedEventHandler BindingFailed;
#pragma warning restore CS0067 // The event 'DebugSettings.BindingFailed' is never used
	}
}
