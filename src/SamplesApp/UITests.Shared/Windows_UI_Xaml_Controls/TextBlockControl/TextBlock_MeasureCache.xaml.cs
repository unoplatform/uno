using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Uno.UI;

namespace UITests.Windows_UI_Xaml_Controls.TextBlockControl
{
	[Sample("TextBlock")]
	public sealed partial class TextBlock_MeasureCache : Page
	{
		public TextBlock_MeasureCache()
		{
			this.InitializeComponent();

#if __WASM__
			long? initialHits = default;
			long? initialMisses = default;

			textBorder.SizeChanged += (sender, e) => { UpdateCacheMetrics(); };
			padding.SelectionChanged += (sender, e) => { UpdateCacheMetrics(); };
			fontSize.SelectionChanged += (sender, e) => { UpdateCacheMetrics(); };
			fontWeight.SelectionChanged += (sender, e) => { UpdateCacheMetrics(); };
			characterSpacing.SelectionChanged += (sender, e) => { UpdateCacheMetrics(); };
			style.SelectionChanged += (sender, e) => { UpdateCacheMetrics(); };

			async void UpdateCacheMetrics()
			{
				if (initialHits == null)
				{
					initialHits = UnoMetrics.TextBlock.MeasureCacheHits;
					initialMisses = UnoMetrics.TextBlock.MeasureCacheMisses;
					input.Text = "Uno is awesome.";
				}
				await Task.Delay(10); // give time to UI to update
				width.Text = textBorder.ActualWidth.ToString("F2");
				height.Text = textBorder.ActualHeight.ToString("F2");
				hits.Text = (UnoMetrics.TextBlock.MeasureCacheHits - initialHits).ToString();
				misses.Text = (UnoMetrics.TextBlock.MeasureCacheMisses - initialMisses).ToString();
			}

			UpdateCacheMetrics();
#endif
		}
	}
}
