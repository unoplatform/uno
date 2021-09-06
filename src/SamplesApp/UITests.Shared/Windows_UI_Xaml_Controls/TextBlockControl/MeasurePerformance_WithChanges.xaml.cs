using System;
using System.Diagnostics;
using Uno.UI.Samples.Controls;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Shared.Windows_UI_Xaml_Controls.TextBlockControl
{
	[Sample("TextBlockControl", "MeasurePerformance_WithChanges", IgnoreInSnapshotTests = true)]
	public sealed partial class MeasurePerformance_WithChanges : Page
	{
		public MeasurePerformance_WithChanges()
		{
			InitializeComponent();
			Bench(null, null);
		}

		private void Bench(object sender, object parms)
		{
#if __WASM__
			Uno.UI.FeatureConfiguration.TextBlock.IsMeasureCacheEnabled = measureCacheEnabled.IsChecked.Value;
#endif

			var rnd = new Random();

			var sw = Stopwatch.StartNew();
			var tb = new TextBlock();
			for (var i = 0; i < 300; i++)
			{
				tb.FontSize = rnd.Next(10, 70);
				tb.Padding = new Thickness(rnd.Next(5, 100));
				tb.LineHeight = rnd.Next(10, 20);
				tb.TextAlignment = (TextAlignment)rnd.Next((int)TextAlignment.Center, (int)TextAlignment.Right + 1);
				tb.TextDecorations = (TextDecorations)rnd.Next((int)TextDecorations.None, (int)TextDecorations.Underline + 1);
				tb.Measure(new Windows.Foundation.Size(100, 100));
			}

			result1.Text = "Bench_TextMeasure_SameText:" + sw.Elapsed;
		}
	}
}
