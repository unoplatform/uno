using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.TextBlockControl
{
	[Sample("TextBlock", Name = "SimpleText_MaxLines_One")]
	public sealed partial class SimpleText_MaxLines_One : UserControl
	{
		public SimpleText_MaxLines_One()
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
