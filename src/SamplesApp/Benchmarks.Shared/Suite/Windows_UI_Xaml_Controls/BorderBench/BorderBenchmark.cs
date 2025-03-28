using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace SamplesApp.Benchmarks.Suite.Windows_UI_Xaml_Controls.BorderBench
{
	public class BorderBenchmark
	{
		private Border SUT;
		private SolidColorBrush MyBrush;

		[GlobalSetup]
		public void Setup()
		{
			SUT = new Border();
			MyBrush = new SolidColorBrush();
		}

		[Benchmark()]
		public void Toggle_BorderBrush()
		{
			SUT.BorderBrush = MyBrush;
			SUT.BorderBrush = null;
		}

		[Benchmark()]
		public void Toggle_Style()
		{
			SUT.Style = new Windows.UI.Xaml.Style();
			SUT.ClearValue(Border.StyleProperty);
		}
	}
}
