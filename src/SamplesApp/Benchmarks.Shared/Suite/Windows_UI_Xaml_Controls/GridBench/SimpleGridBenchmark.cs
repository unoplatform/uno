using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace SamplesApp.Benchmarks.Suite.Windows_UI_Xaml_Controls.GridBench
{
	public class SimpleGridBenchmark
	{
		[Params(0, 1, 5)]
		public int ItemsCount { get; set; }


		[Benchmark()]
		public void Superposition_Measure()
		{
			var SUT = new Grid();

			for (int i = 0; i < ItemsCount; i++)
			{
				SUT.Children.Add(new Border() { Width = 10, Height = 10 });
			}

			SUT.Measure(new Size(20, 20));
		}
	}
}
