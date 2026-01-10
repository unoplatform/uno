using System.Threading.Tasks;
using Windows.UI.Core;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.TextBlockControl
{
	[Sample("TextBlock", "SimpleText_MaxLines_Two_With_Wrap", description: "This sample shows a very long line that should wrap on a maximum of two lines.")]
	public sealed partial class SimpleText_MaxLines_Two_With_Wrap : Page
	{
		public SimpleText_MaxLines_Two_With_Wrap()
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
