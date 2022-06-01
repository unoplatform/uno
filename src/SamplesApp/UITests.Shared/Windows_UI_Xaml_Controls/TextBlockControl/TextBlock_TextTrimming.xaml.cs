using Windows.UI.Xaml.Controls;
using Uno.UI;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.TextBlockControl
{
	[Sample("TextBlock")]
	public sealed partial class TextBlock_TextTrimming : Page
	{
		public TextBlock_TextTrimming()
		{
			this.InitializeComponent();

#if __WASM__
			var initialHits = UnoMetrics.TextBlock.MeasureCacheHits;
			var initialMisses = UnoMetrics.TextBlock.MeasureCacheMisses;

			border1.SizeChanged += (sender, e) =>
			{
				hits.Text = (UnoMetrics.TextBlock.MeasureCacheHits - initialHits).ToString();
				misses.Text = (UnoMetrics.TextBlock.MeasureCacheMisses - initialMisses).ToString();
			};
#endif
		}
	}
}
