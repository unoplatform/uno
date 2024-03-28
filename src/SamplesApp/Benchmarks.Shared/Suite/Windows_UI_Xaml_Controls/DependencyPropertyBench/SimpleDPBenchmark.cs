using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace SamplesApp.Benchmarks.Suite.Windows_UI_Xaml_Controls.GridBench
{
	public class SimpleDPBenchmark
	{
		private Grid SUT;

		[GlobalSetup]
		public void Setup()
		{
			SUT = new Grid();
		}

		[Benchmark()]
		public void DP_Write()
		{
			for (int i = 0; i < 100; i++)
			{
				SUT.Width = i;
			}
		}

		[Benchmark()]
		public void DP_Read()
		{
			for (int i = 0; i < 100; i++)
			{
				var r = SUT.Width;
			}
		}
	}
}
