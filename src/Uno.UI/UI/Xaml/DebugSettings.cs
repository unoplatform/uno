using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Microsoft.UI.Xaml
{
	public sealed partial class DebugSettings
	{
		public LayoutCycleTracingLevel LayoutCycleTracingLevel { get; set; }

#if !UNO_REFERENCE_API
		[Uno.NotImplemented]
#endif
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
