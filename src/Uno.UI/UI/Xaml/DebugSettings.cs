using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Microsoft.UI.Xaml
{
	public sealed partial class DebugSettings
	{
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

		public event BindingFailedEventHandler BindingFailed;

		private void OnBindingFailed(BindingFailedEventArgs args)
		{
			BindingFailed?.Invoke(this, args);
		}
	}
}
