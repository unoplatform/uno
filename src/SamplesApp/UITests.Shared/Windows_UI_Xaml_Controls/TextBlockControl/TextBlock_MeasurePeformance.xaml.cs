using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.TextBlockControl
{
	[Sample("TextBlock", "MeasurePerformance", ignoreInSnapshotTests: true)]
	public sealed partial class TextBlock_MeasurePeformance : UserControl
	{
		public TextBlock_MeasurePeformance()
		{
			this.InitializeComponent();

			Bench_TextMeasure_SameText(null, null);
		}

		private void Bench_TextMeasure_SameText(object sender, object parms)
		{
#if __WASM__
			Uno.UI.FeatureConfiguration.TextBlock.IsMeasureCacheEnabled = measureCacheEnabled.IsChecked.Value;
#endif

			var sw = Stopwatch.StartNew();

			for (int i = 0; i < 300; i++)
			{
				var tb = new TextBlock { Text = "42" };
				tb.Measure(new Size(100, 100));
			}

			result1.Text = "Bench_TextMeasure_SameText:" + sw.Elapsed;
		}
	}
}
