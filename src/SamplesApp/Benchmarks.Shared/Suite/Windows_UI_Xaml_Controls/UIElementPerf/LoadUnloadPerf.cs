using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using Benchmarks.Shared.Controls;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace SamplesApp.Benchmarks.Suite.Windows_UI_Xaml_Controls.GridBench
{
	public class LoadUnloadBenchmark
	{
		[Params(5, 10, 30)]
		public int Depth { get; set; }

		private Grid SUT;

		[GlobalSetup]
		public void Setup()
		{
			SUT = CreateHierachy();
		}

		[Benchmark()]
		public void BasicLoadUnload()
		{
			var item = CreateHierachy();

			for (int i = 0; i < 50; i++)
			{
				BenchmarkUIHost.Root.Content = item;
				BenchmarkUIHost.Root.Content = null;
			}
		}

		private Grid CreateHierachy()
		{
			var top = new Grid();
			var current = top;

			for (int i = 0; i < Depth; i++)
			{
				var n = new Grid();
				current.Children.Add(n);
				current = n;
			}

			current.Children.Add(new TextBlock() { Text = "test" });

			return top;
		}
	}
}
