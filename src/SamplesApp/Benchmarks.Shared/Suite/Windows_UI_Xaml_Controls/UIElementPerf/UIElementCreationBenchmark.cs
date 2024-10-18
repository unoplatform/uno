using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;

namespace SamplesApp.Benchmarks.Suite.Windows_UI_Xaml_Controls.UIElementPerf
{
	public class UIElementCreationBenchmark
	{
		[Benchmark]
		public void BorderCreation()
		{
			var SUT = new Grid();

			for (int i = 0; i < 20; i++)
			{
				SUT.Children.Add(new Border());
			}
		}

		[Benchmark]
		public void RectangleCreation()
		{
			var SUT = new Grid();

			for (int i = 0; i < 20; i++)
			{
				SUT.Children.Add(new Rectangle());
			}
		}
	}
}
